using UnityEngine;
using FieldDay;

public class BasicLoggingExample : MonoBehaviour {
    public string appId;
    public int appVersion;
    public int clientLogVersion;

    private OGDLog m_Logger;

    private void Start() {
        m_Logger = new OGDLog(appId, appVersion);
        m_Logger.SetUserId("default");
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