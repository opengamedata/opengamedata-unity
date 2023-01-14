using UnityEngine;
using FieldDay;
using System.Collections.Generic;
using System.Collections;

public class FirebaseLoggingExample : MonoBehaviour {
    public string appId;
    public string appVersion;
    public int clientLogVersion;
    public FirebaseConsts firebase;

    private OGDLog m_Logger;

    private IEnumerator Start() {
        m_Logger = new OGDLog(appId, appVersion);
        m_Logger.SetUserId("default");
        m_Logger.UseFirebase(firebase);
        m_Logger.SetDebug(true);

        while(!m_Logger.IsReady())
            yield return null;

        using(var g = m_Logger.WriteGameState()) {
            g.Param("platform", Application.platform.ToString());
        }

        using(var u = m_Logger.WriteUserData()) {
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
            }
        }
    }
}