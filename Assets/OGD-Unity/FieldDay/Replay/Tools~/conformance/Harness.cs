// Conformance harness: drives the REAL ObserveInputRecorder.cs and writes the
// fixture consumed by input.test.mjs. The simulated pointer track below is the
// only synthetic part; the encoding under test is production code.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FieldDay.Replay;

class Harness {
 static string JsonArray(List<string> lines) {
  var sb = new StringBuilder("[");
  for (int i = 0; i < lines.Count; i++) {
   if (i > 0) sb.Append(',');
   sb.Append('"').Append(lines[i].Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"');
  }
  return sb.Append(']').ToString();
 }

 static int Main() {
  // 10 Hz sampled motion, 1920x1080 reference space, whole-pixel grid.
  var rec = new ObserveInputRecorder(1920, 1080, 100, 1);
  var header = rec.Header();
  var all = new List<string>();
  void Drain() { all.AddRange(rec.Drain()); }

  // Phase 1 (0..1000ms): pointer sweeps across the board. Offered every 16ms
  // like a 60fps frame callback would; the recorder must thin this to 10 Hz.
  for (long t = 0; t <= 1000; t += 16) {
   float x = 200f + t * 0.5f, y = 400f + t * 0.2f;
   rec.Pointer(t, x, y, true);
  }
  Drain();

  // Phase 2 (1000..1200ms): a click with press and release 40ms apart, which
  // is far below the sample cadence and must survive verbatim.
  rec.Button(1040, "mouse", "left", true);
  rec.Action(1040, "tile_7_3", "select");
  rec.Button(1080, "mouse", "left", false);
  rec.Scroll(1150, 0f, -3f);
  Drain();

  // Phase 3 (1200..2000ms): more motion, then the pointer leaves the surface.
  for (long t = 1200; t <= 1900; t += 16) {
   rec.Pointer(t, 900f - (t - 1200) * 0.3f, 500f + (t - 1200) * 0.1f, true);
  }
  rec.Pointer(1950, -40f, 500f, false);
  Drain();

  // Phase 4 (2000..2600ms): pointer is PERFECTLY STILL across six cadence
  // boundaries. A correct recorder emits zero samples for this window.
  for (long t = 2000; t <= 2600; t += 16) {
   rec.Pointer(t, 640f, 360f, true);
  }
  Drain();

  // Phase 5: keyboard, then a window resize, then motion in the new space.
  rec.Button(2700, "key", "Space", true);
  rec.Button(2760, "key", "Space", false);
  rec.Space(2800, 1280, 720);
  for (long t = 2900; t <= 3600; t += 16) {
   rec.Pointer(t, 100f + (t - 2900) * 0.4f, 200f + (t - 2900) * 0.25f, true);
  }
  rec.Button(3650, "mouse", "right", true);
  rec.Button(3700, "mouse", "right", false);
  rec.Action(3700, "end_turn", "click");
  Drain();
  rec.Close();

  var outDir = Environment.GetEnvironmentVariable("OUT_DIR") ?? ".";
  var json = "{\"header\":" + JsonArray(new List<string> { header }).Substring(1).TrimEnd(']').Insert(0, "") +
             ",\"lines\":" + JsonArray(all) + "}";
  File.WriteAllText(Path.Combine(outDir, "fixture.json"), json);

  // Negative checks on the recorder's own guard rails.
  int guarded = 0;
  var g = new ObserveInputRecorder(800, 600, 100, 1);
  try { g.Button(10, "mouse", "bad button!", true); } catch (ArgumentException) { guarded++; }
  try { g.Button(10, "mouse", "left", true); g.Button(5, "mouse", "left", false); } catch (ArgumentException) { guarded++; }
  try { g.Pointer(20, float.NaN, 0f, true); } catch (ArgumentException) { guarded++; }
  try { new ObserveInputRecorder(1920, 1080, 0, 1); } catch (ArgumentException) { guarded++; }
  var c = new ObserveInputRecorder(800, 600); c.Close();
  try { c.Button(10, "mouse", "left", true); } catch (InvalidOperationException) { guarded++; }

  Console.WriteLine($"events={all.Count} guards_enforced={guarded}/5");
  if (guarded != 5) { Console.Error.WriteLine("RECORDER GUARD FAILURE"); return 1; }
  return 0;
 }
}
