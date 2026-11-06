// Replay input channel: decode + analyse the stream produced by
// ObserveInputRecorder.cs. Pure functions over the recorded lines; no I/O.
//
// Contract mirrored from the C# recorder (schema 1):
//   input_header  {kind,schema,intervalMs,precision,space:{w,h}}
//   pointer       {t,sequence,kind,x,y,inside}   quantized ABSOLUTE grid coords
//   button        {t,sequence,kind,device,button,down}
//   scroll        {t,sequence,kind,dx,dy}
//   action        {t,sequence,kind,target,action}
//   space         {t,sequence,kind,w,h}
//
// Sampled pointer motion is emitted only on cadence boundaries and only when
// the quantized value changed, so a decoder MUST hold the last value forward.

const fail = (ok, message) => { if (!ok) throw Error(message); };
const int = x => Number.isSafeInteger(x);

const KINDS = new Set(['pointer', 'button', 'scroll', 'action', 'space']);
const TOKEN = /^[A-Za-z0-9_.-]{1,64}$/;

export function parseHeader(line) {
  const h = typeof line === 'string' ? JSON.parse(line) : line;
  fail(h && h.kind === 'input_header', 'Not an input header');
  fail(h.schema === 1, `Unsupported input schema ${h.schema}`);
  fail(int(h.intervalMs) && h.intervalMs >= 1 && h.intervalMs <= 60000, 'Bad interval');
  fail(int(h.precision) && h.precision >= 1 && h.precision <= 1000, 'Bad precision');
  fail(h.space && int(h.space.w) && int(h.space.h) && h.space.w >= 1 && h.space.h >= 1, 'Bad space');
  return { schema: h.schema, intervalMs: h.intervalMs, precision: h.precision, space: { ...h.space } };
}

// Strict decode: enforces sequence continuity and non-decreasing time, which is
// what makes a truncated or reordered upload detectable instead of silently
// producing a plausible-looking replay.
export function decode(header, lines) {
  const h = parseHeader(header);
  const events = [];
  let expected = 0, lastT = 0;
  for (const raw of lines) {
    const e = typeof raw === 'string' ? JSON.parse(raw) : raw;
    fail(e && KINDS.has(e.kind), `Unknown input kind ${e && e.kind}`);
    fail(int(e.t) && e.t >= 0, 'Bad event time');
    fail(int(e.sequence), 'Bad sequence');
    fail(e.sequence === expected, `Sequence gap: expected ${expected}, got ${e.sequence}`);
    fail(e.t >= lastT, `Time regression at sequence ${e.sequence}`);
    if (e.kind === 'pointer') {
      fail(int(e.x) && int(e.y) && typeof e.inside === 'boolean', 'Bad pointer sample');
    } else if (e.kind === 'button') {
      fail(TOKEN.test(e.device || '') && TOKEN.test(e.button || '') && typeof e.down === 'boolean', 'Bad button event');
    } else if (e.kind === 'scroll') {
      fail(int(e.dx) && int(e.dy), 'Bad scroll event');
    } else if (e.kind === 'action') {
      fail(TOKEN.test(e.target || '') && TOKEN.test(e.action || ''), 'Bad action event');
    } else {
      fail(int(e.w) && int(e.h) && e.w >= 1 && e.h >= 1, 'Bad space event');
    }
    events.push(e); expected++; lastT = e.t;
  }
  return { header: h, events };
}

// Resolve pointer state at a time, in reference-space units (grid / precision).
// Holds the last sample forward; returns null before the first sample. Pointer
// samples are absolute, so this is a seek, not a replay from zero.
export function pointerAt(decoded, t) {
  fail(int(t) && t >= 0, 'Bad seek time');
  const { precision } = decoded.header;
  let space = { ...decoded.header.space };
  let current = null;
  for (const e of decoded.events) {
    if (e.t > t) break;
    if (e.kind === 'space') { space = { w: e.w, h: e.h }; current = null; continue; }
    if (e.kind !== 'pointer') continue;
    current = { x: e.x / precision, y: e.y / precision, inside: e.inside, at: e.t, space: { ...space } };
  }
  return current;
}

// Buttons held at a time, derived from down/up transitions.
export function buttonsAt(decoded, t) {
  const held = new Set();
  for (const e of decoded.events) {
    if (e.t > t) break;
    if (e.kind !== 'button') continue;
    const key = `${e.device}/${e.button}`;
    if (e.down) held.add(key); else held.delete(key);
  }
  return [...held].sort();
}

// Verify the recorded stream honours the declared sampled cadence.
// Two separate claims, both load-bearing:
//   - no two pointer samples closer together than intervalMs (minus tolerance)
//   - discrete events are NOT rate-limited and may appear at any time
export function verifyCadence(decoded, { toleranceMs = 0 } = {}) {
  const { intervalMs } = decoded.header;
  const samples = decoded.events.filter(e => e.kind === 'pointer');
  const violations = [];
  for (let i = 1; i < samples.length; i++) {
    const gap = samples[i].t - samples[i - 1].t;
    if (gap < intervalMs - toleranceMs) {
      violations.push({ sequence: samples[i].sequence, gap, intervalMs });
    }
  }
  return { ok: violations.length === 0, sampleCount: samples.length, violations };
}

// Byte cost of the input channel, reported per class so a bandwidth regression
// can be attributed instead of guessed at.
export function cost(header, lines) {
  const bytes = { header: Buffer.byteLength(typeof header === 'string' ? header : JSON.stringify(header)) };
  const counts = {};
  for (const raw of lines) {
    const line = typeof raw === 'string' ? raw : JSON.stringify(raw);
    const kind = (typeof raw === 'string' ? JSON.parse(raw) : raw).kind;
    bytes[kind] = (bytes[kind] || 0) + Buffer.byteLength(line);
    counts[kind] = (counts[kind] || 0) + 1;
  }
  const total = Object.values(bytes).reduce((a, b) => a + b, 0);
  return { total, bytes, counts };
}

// Worst rolling window of input-channel bytes, for the 1-second / 20-client
// classroom standard. Reported, never used to retime the source.
export function peakWindow(lines, windowMs = 1000) {
  const items = lines.map(raw => {
    const line = typeof raw === 'string' ? raw : JSON.stringify(raw);
    const e = typeof raw === 'string' ? JSON.parse(raw) : raw;
    return { t: e.t, bytes: Buffer.byteLength(line) };
  }).sort((a, b) => a.t - b.t);
  let worst = 0, start = 0, sum = 0, at = 0;
  for (let i = 0; i < items.length; i++) {
    sum += items[i].bytes;
    while (items[i].t - items[start].t >= windowMs) { sum -= items[start].bytes; start++; }
    if (sum > worst) { worst = sum; at = items[start].t; }
  }
  return { bytes: worst, windowMs, at, kbps: (worst * 8) / 1000 / (windowMs / 1000) };
}
