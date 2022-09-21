using UnityEngine;
using FieldDay;
using System.Collections.Generic;

public class FirebaseLoggingExample : MonoBehaviour {
    public string appId;
    public string appVersion;
    public int clientLogVersion;
    public FirebaseConsts firebase;

    private OGDLog m_Logger;

    private void Start() {
        m_Logger = new OGDLog(appId, appVersion);
        m_Logger.SetUserId("default");
        m_Logger.UseFirebase(firebase);
        m_Logger.SetDebug(true);
    }

    private void Update() {
        if (Input.GetMouseButtonDown(0)) {
            using(var e = m_Logger.NewEvent("mouse_clicked")) {
                e.Param("mouseX", Input.mousePosition.x);
                e.Param("mouseY", Input.mousePosition.y);
            }
        }
    }
}