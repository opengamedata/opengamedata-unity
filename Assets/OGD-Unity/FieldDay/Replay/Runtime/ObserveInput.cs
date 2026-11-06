using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FieldDay.Replay {
 // Input capture for replay. Two distinct channels, never mixed:
 //
 //  1. SAMPLED pointer motion at a fixed cadence (default 10 Hz). Position is
 //     quantized to an absolute integer grid in a declared reference space, so
 //     a sample is byte-identical when the pointer has not moved and can be
 //     dropped by the delta codec. Never a difference from the previous value.
 //  2. DISCRETE events (button/key down/up, scroll, pointer enter/exit) stamped
 //     with the real time they were observed, never resampled to the cadence.
 //
 // The recorder is a pure encoder: it owns no clock, no Input polling and no
 // frame callback. The caller supplies observed values and the monotonic
 // session time in milliseconds, which keeps it testable off-device and keeps
 // WebGL frame-order semantics the caller's business.
 public sealed class ObserveInputRecorder {
  public const int Schema = 1;

  readonly int precision;      // grid divisions per reference unit
  readonly long intervalMs;    // sampled-motion cadence
  readonly int width, height;  // declared reference space
  readonly List<string> pending = new List<string>();

  long sequence;               // monotonic event sequence within the channel
  long lastSampleAt = long.MinValue;
  long lastEventTime;
  bool haveSample;
  int lastX, lastY;
  bool lastInside;
  bool closed;

  public long Sequence { get { return sequence; } }
  public int PendingCount { get { return pending.Count; } }

  public ObserveInputRecorder(int referenceWidth, int referenceHeight, long sampleIntervalMs = 100, int gridPrecision = 1) {
   if (referenceWidth < 1 || referenceWidth > 65535 || referenceHeight < 1 || referenceHeight > 65535)
    throw new ArgumentException("Input reference space");
   if (sampleIntervalMs < 1 || sampleIntervalMs > 60000) throw new ArgumentException("Input sample interval");
   if (gridPrecision < 1 || gridPrecision > 1000) throw new ArgumentException("Input grid precision");
   width = referenceWidth; height = referenceHeight; intervalMs = sampleIntervalMs; precision = gridPrecision;
  }

  // Declared once per session so a decoder can resolve the coordinate space
  // without inferring it from the samples.
  public string Header() {
   return "{\"kind\":\"input_header\",\"schema\":" + Schema +
    ",\"intervalMs\":" + intervalMs +
    ",\"precision\":" + precision +
    ",\"space\":{\"w\":" + width + ",\"h\":" + height + "}}";
  }

  static void Order(long t, long last) {
   if (t < 0 || t > 9007199254740991L) throw new ArgumentException("Input time range");
   if (t < last) throw new ArgumentException("Input time order");
  }

  static string Str(string value) {
   if (value == null) throw new ArgumentNullException("value");
   if (value.Length < 1 || value.Length > 64) throw new ArgumentException("Input token length");
   for (int i = 0; i < value.Length; i++) {
    char c = value[i];
    bool ok = c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_' || c == '-' || c == '.';
    if (!ok) throw new ArgumentException("Input token charset");
   }
   return "\"" + value + "\"";
  }

  int Grid(float value, int extent) {
   if (float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentException("Input coordinate finite");
   double scaled = Math.Round((double)value * precision, MidpointRounding.AwayFromZero);
   // Clamping is deliberate: an out-of-surface pointer is reported as inside=false,
   // so the coordinate only has to stay representable, not accurate past the edge.
   double limit = (double)extent * precision;
   if (scaled < -limit) scaled = -limit;
   if (scaled > 2 * limit) scaled = 2 * limit;
   return (int)scaled;
  }

  void Emit(string body, long t) {
   if (closed) throw new InvalidOperationException("Input recorder closed");
   Order(t, lastEventTime);
   pending.Add("{\"t\":" + t + ",\"sequence\":" + sequence + "," + body + "}");
   sequence++; lastEventTime = t;
  }

  // Offer an observed pointer position. Returns true when a sample was emitted.
  // A sample is emitted only on a cadence boundary AND only when the quantized
  // value or inside-ness actually changed, so a still pointer costs nothing.
  public bool Pointer(long t, float x, float y, bool inside) {
   if (closed) throw new InvalidOperationException("Input recorder closed");
   Order(t, lastEventTime);
   if (haveSample && t - lastSampleAt < intervalMs) return false;
   int gx = Grid(x, width), gy = Grid(y, height);
   bool first = !haveSample;
   if (!first && gx == lastX && gy == lastY && inside == lastInside) { lastSampleAt = t; return false; }
   Emit("\"kind\":\"pointer\",\"x\":" + gx.ToString(CultureInfo.InvariantCulture) +
        ",\"y\":" + gy.ToString(CultureInfo.InvariantCulture) +
        ",\"inside\":" + (inside ? "true" : "false"), t);
   lastSampleAt = t; lastX = gx; lastY = gy; lastInside = inside; haveSample = true;
   return true;
  }

  // Discrete button/key transition. Never deduplicated and never resampled.
  public void Button(long t, string device, string button, bool down) {
   Emit("\"kind\":\"button\",\"device\":" + Str(device) + ",\"button\":" + Str(button) +
        ",\"down\":" + (down ? "true" : "false"), t);
  }

  public void Scroll(long t, float dx, float dy) {
   Emit("\"kind\":\"scroll\",\"dx\":" + Grid(dx, width).ToString(CultureInfo.InvariantCulture) +
        ",\"dy\":" + Grid(dy, height).ToString(CultureInfo.InvariantCulture), t);
  }

  // A semantic UI action the game itself recognised (button press, menu choice).
  // Recorded alongside raw input because raw coordinates alone do not survive
  // a layout change, and the pair is what makes a replay interpretable.
  public void Action(long t, string target, string action) {
   Emit("\"kind\":\"action\",\"target\":" + Str(target) + ",\"action\":" + Str(action), t);
  }

  // Reference-space change (window resize / canvas rescale). Later samples are
  // in the new space; the decoder must not rescale earlier ones.
  public void Space(long t, int w, int h) {
   if (w < 1 || w > 65535 || h < 1 || h > 65535) throw new ArgumentException("Input reference space");
   Emit("\"kind\":\"space\",\"w\":" + w + ",\"h\":" + h, t);
   haveSample = false;
  }

  // Hand the buffered events to the session sink. The caller appends each
  // returned line through ObserveLiveSession.Record in order.
  public string[] Drain() {
   var output = pending.ToArray();
   pending.Clear();
   return output;
  }

  public void Close() { closed = true; }
 }
}
