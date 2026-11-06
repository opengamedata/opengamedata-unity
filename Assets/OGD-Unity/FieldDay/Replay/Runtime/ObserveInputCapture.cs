using System;
using UnityEngine;

namespace FieldDay.Replay {
 // Unity driver for ObserveInputRecorder. This is the only piece that touches
 // UnityEngine: it owns the clock and the polling, the recorder owns encoding.
 //
 // Split deliberately so cadence/encoding stay unit-testable off-device (see
 // Tools~/input-codec) while this class stays thin enough to eyeball.
 //
 // Pointer motion is SAMPLED at sampleHz (default 10). Button and key
 // transitions are captured on the frame they occur and are never thinned,
 // because a click that lands between two samples still happened.
 [DisallowMultipleComponent]
 public sealed class ObserveInputCapture : MonoBehaviour {
  [Tooltip("Sampled pointer cadence in Hz. Discrete events ignore this.")]
  [Range(1, 60)] public int sampleHz = 10;

  [Tooltip("Grid divisions per pixel for quantized pointer coordinates.")]
  [Range(1, 10)] public int gridPrecision = 1;

  [Tooltip("Mouse buttons to watch (0=left, 1=right, 2=middle).")]
  public int[] watchedMouseButtons = new int[] { 0, 1, 2 };

  [Tooltip("Keys to watch as discrete press/release events.")]
  public KeyCode[] watchedKeys = new KeyCode[0];

  // Assigned by the host game: receives each encoded line in order.
  // Left null, capture still runs and drains to nothing, which keeps a
  // misconfigured scene from throwing in the middle of a play session.
  public Action<string> sink;

  ObserveInputRecorder recorder;
  int spaceW, spaceH;
  double startedAt;
  bool[] mouseHeld;
  bool[] keyHeld;

  long Now() { return (long)Math.Round((Time.realtimeSinceStartupAsDouble - startedAt) * 1000.0); }

  void OnEnable() {
   startedAt = Time.realtimeSinceStartupAsDouble;
   spaceW = Mathf.Max(1, Screen.width);
   spaceH = Mathf.Max(1, Screen.height);
   recorder = new ObserveInputRecorder(spaceW, spaceH, Mathf.RoundToInt(1000f / sampleHz), gridPrecision);
   mouseHeld = new bool[watchedMouseButtons != null ? watchedMouseButtons.Length : 0];
   keyHeld = new bool[watchedKeys != null ? watchedKeys.Length : 0];
   Send(recorder.Header());
  }

  void OnDisable() {
   if (recorder == null) return;
   Flush();
   recorder.Close();
   recorder = null;
  }

  void Send(string line) {
   var s = sink;
   if (s != null) s(line);
  }

  void Flush() {
   var lines = recorder.Drain();
   for (int i = 0; i < lines.Length; i++) Send(lines[i]);
  }

  void Update() {
   if (recorder == null) return;
   long t = Now();

   // A resize invalidates earlier coordinates rather than rescaling them.
   if (Screen.width != spaceW || Screen.height != spaceH) {
    spaceW = Mathf.Max(1, Screen.width);
    spaceH = Mathf.Max(1, Screen.height);
    recorder.Space(t, spaceW, spaceH);
   }

   Vector3 m = Input.mousePosition;
   bool inside = m.x >= 0f && m.y >= 0f && m.x <= spaceW && m.y <= spaceH;
   recorder.Pointer(t, m.x, m.y, inside);

   for (int i = 0; i < mouseHeld.Length; i++) {
    bool down = Input.GetMouseButton(watchedMouseButtons[i]);
    if (down == mouseHeld[i]) continue;
    mouseHeld[i] = down;
    recorder.Button(t, "mouse", MouseName(watchedMouseButtons[i]), down);
   }

   for (int i = 0; i < keyHeld.Length; i++) {
    bool down = Input.GetKey(watchedKeys[i]);
    if (down == keyHeld[i]) continue;
    keyHeld[i] = down;
    recorder.Button(t, "key", watchedKeys[i].ToString(), down);
   }

   Vector2 scroll = Input.mouseScrollDelta;
   if (scroll.sqrMagnitude > 0f) recorder.Scroll(t, scroll.x, scroll.y);

   Flush();
  }

  // Called by game code when a semantic UI action fires. Raw coordinates alone
  // do not survive a layout change, so the paired semantic action is what keeps
  // an old replay interpretable.
  public void RecordAction(string target, string action) {
   if (recorder == null) return;
   recorder.Action(Now(), target, action);
   Flush();
  }

  static string MouseName(int button) {
   if (button == 0) return "left";
   if (button == 1) return "right";
   if (button == 2) return "middle";
   return "b" + button;
  }
 }
}
