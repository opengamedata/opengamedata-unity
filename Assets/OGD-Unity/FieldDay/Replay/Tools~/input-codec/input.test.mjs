// Cross-language conformance: the fixture consumed here is produced by the
// REAL ObserveInputRecorder.cs via `dotnet run` (see conformance-harness).
// Decoding a hand-written fixture would only prove the decoder agrees with
// itself, so these tests fail if the C# encoder and JS decoder ever drift.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { decode, parseHeader, pointerAt, buttonsAt, verifyCadence, cost, peakWindow } from './input.mjs';

const fixture = JSON.parse(readFileSync(new URL('./fixture.json', import.meta.url)));
const { header, lines } = fixture;

test('C# encoder output decodes under the JS contract', () => {
  const d = decode(header, lines);
  assert.equal(d.header.schema, 1);
  assert.equal(d.header.intervalMs, 100, '10 Hz cadence as requested');
  assert.equal(d.events.length, lines.length);
});

test('sampled pointer motion honours the 10 Hz cadence', () => {
  const d = decode(header, lines);
  const v = verifyCadence(d);
  assert.ok(v.ok, `cadence violations: ${JSON.stringify(v.violations)}`);
  assert.ok(v.sampleCount >= 5, `expected real motion, got ${v.sampleCount} samples`);
});

test('a still pointer emits no samples', () => {
  // The C# side offered the same position repeatedly across many boundaries
  // in the 2000..2600 window; a correct recorder emits nothing there.
  const d = decode(header, lines);
  const still = d.events.filter(e => e.kind === 'pointer' && e.t > 2000 && e.t <= 2600);
  assert.equal(still.length, 0, 'idle pointer must not cost bandwidth');
});

test('discrete events are not rate-limited by the sample cadence', () => {
  const d = decode(header, lines);
  const buttons = d.events.filter(e => e.kind === 'button');
  assert.ok(buttons.length >= 2);
  const gaps = buttons.slice(1).map((e, i) => e.t - buttons[i].t);
  assert.ok(gaps.some(g => g < 100), `clicks must survive sub-cadence timing, gaps=${gaps}`);
});

test('pointer samples are absolute, enabling seek without replay', () => {
  const d = decode(header, lines);
  const samples = d.events.filter(e => e.kind === 'pointer');
  const mid = samples[Math.floor(samples.length / 2)];
  const direct = pointerAt(d, mid.t);
  assert.equal(direct.x, mid.x / d.header.precision);
  assert.equal(direct.y, mid.y / d.header.precision);
  // Seeking backwards then forwards must land on the identical value.
  pointerAt(d, 0);
  const again = pointerAt(d, mid.t);
  assert.deepEqual(again, direct, 'seek must be order-independent');
});

test('pointer state holds forward between samples', () => {
  const d = decode(header, lines);
  const samples = d.events.filter(e => e.kind === 'pointer');
  const s = samples[1];
  const next = samples[2];
  const between = pointerAt(d, Math.floor((s.t + next.t) / 2));
  assert.equal(between.at, s.t, 'must hold the last sample, not interpolate');
});

test('button hold state reconstructs from transitions', () => {
  const d = decode(header, lines);
  const downs = d.events.filter(e => e.kind === 'button' && e.down);
  const first = downs[0];
  const held = buttonsAt(d, first.t);
  assert.ok(held.includes(`${first.device}/${first.button}`));
  const afterAll = buttonsAt(d, 1e9);
  assert.deepEqual(afterAll, [], 'every press was released');
});

test('a reference-space change invalidates held position', () => {
  const d = decode(header, lines);
  const space = d.events.find(e => e.kind === 'space');
  assert.ok(space, 'fixture exercises a resize');
  assert.equal(pointerAt(d, space.t), null, 'stale coords must not survive a resize');
  const after = d.events.find(e => e.kind === 'pointer' && e.t > space.t);
  assert.deepEqual(pointerAt(d, after.t).space, { w: space.w, h: space.h });
});

test('truncated or reordered uploads are rejected', () => {
  assert.throws(() => decode(header, [lines[0], lines[2]]), /Sequence gap/);
  assert.throws(() => decode(header, [...lines].reverse()), /Sequence gap|Time regression/);
});

test('schema mismatch is refused rather than guessed', () => {
  const bad = { ...JSON.parse(header), schema: 2 };
  assert.throws(() => parseHeader(JSON.stringify(bad)), /Unsupported input schema/);
});

test('input channel cost stays within the classroom budget', () => {
  const c = cost(header, lines);
  const peak = peakWindow(lines);
  const span = JSON.parse(lines.at(-1)).t - JSON.parse(lines[0]).t;
  const perClientKbps = (c.total * 8) / 1000 / (span / 1000);
  // 20 concurrent clients on the live wall is the standard this project holds.
  assert.ok(peak.kbps * 20 < 2000,
    `20-client peak ${(peak.kbps * 20).toFixed(1)} kbps exceeds budget`);
  console.log(`      input channel: ${c.total} B over ${span} ms = ${perClientKbps.toFixed(2)} kbps/client`);
  console.log(`      peak 1s window: ${peak.kbps.toFixed(2)} kbps -> x20 clients = ${(peak.kbps * 20).toFixed(1)} kbps`);
  console.log(`      counts: ${JSON.stringify(c.counts)}`);
});
