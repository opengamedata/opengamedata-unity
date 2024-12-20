using UnityEngine;
using OGD;
using System.Collections;

public class SurveyExample : MonoBehaviour {
    public string appId;
    public int appVersion;
    public int clientLogVersion;
    public SurveyPanel SurveyPrefab;
    public TextAsset SurveyText;
    public string SurveyId = "example";

    private OGDLog m_Logger;
    private OGDSurvey m_Survey;

    private IEnumerator Start() {
        m_Logger = new OGDLog(appId, appVersion);
        m_Logger.SetUserId("default");
        m_Logger.SetDebug(true);

        while(!m_Logger.IsReady())
            yield return null;

        m_Survey = new OGDSurvey(SurveyPrefab, m_Logger);
        m_Survey.LoadSurveyPackageFromString(SurveyText.text);

        yield return m_Survey.DisplaySurveyAndWait(SurveyId);

        Debug.Log("Finished survey!");
    }

    private void LateUpdate() {
        if (m_Survey == null) {
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.Escape)) {
            m_Survey.CancelSurvey();
        }
    }
}