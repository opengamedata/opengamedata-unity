using UnityEngine;
using OGD;
using System.Collections;
using System.Text;

public class BasicLoggingExample : MonoBehaviour {
    public string appId;
    public int appVersion;
    public int clientLogVersion;
    public bool debugMode = true;

    private OGDLog m_Logger;

    private IEnumerator Start() {
        m_Logger = new OGDLog(appId, appVersion);
        m_Logger.SetUserId("default");
        m_Logger.SetDebug(debugMode);

        while(!m_Logger.IsReady())
            yield return null;

        Debug.LogFormat("current session id: {0}", m_Logger.GetSessionId().ToString("X16"));

        //m_Logger.ConfigureMirroring("// REDACTED //");

        //using(var g = m_Logger.OpenGameState()) {
        //    g.Param("platform", Application.platform.ToString());
        //}

        m_Logger.GameState("{\"platform\":16}");

        var sb = new StringBuilder();
        sb.Append("{\"platform\":16}");
        m_Logger.GameState(sb);

        using (var u = m_Logger.OpenUserData()) {
            u.Param("high_score", Random.Range(25, 68));
        }
    }

    private void Update() {
        if (!m_Logger.IsReady()) {
            return;
        }

        if (Input.GetMouseButtonDown(0)) {
            using(var e = m_Logger.NewEvent("test_event")) {
                e.Param("mouseX", Input.mousePosition.x);
                e.Param("mouseY", Input.mousePosition.y);
                e.Json("what_else", "{\"is_expected\": \"of me?\"}");
            }
        } else if (Input.GetMouseButtonDown(1)) {
            m_Logger.Log("test_structured", "{\"something\":[4,5,6,7,8,15],\"nesting\":{\"x\":15}}");
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            m_Logger.AttemptReconnect();
        }
    }
}