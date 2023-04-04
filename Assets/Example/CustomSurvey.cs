using UnityEngine;
using FieldDay;
using System.Collections;

[RequireComponent(typeof(SurveyPanel))]
public class CustomSurvey : MonoBehaviour {
    public CanvasGroup FullGroup;
    public CanvasGroup QuestionGroup;

    private void Awake() {
        SurveyPanel panel = GetComponent<SurveyPanel>();
        panel.ClosePageAnim = ClosePageAnim;
        panel.OpenPageAnim = OpenPageAnim;
        QuestionGroup.alpha = 0;
    }

    private IEnumerator OpenPageAnim(SurveyPanel panel) {
        QuestionGroup.alpha = 0;
        while(QuestionGroup.alpha < 1) {
            QuestionGroup.alpha += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator ClosePageAnim(SurveyPanel panel) {
        while(QuestionGroup.alpha > 0) {
            QuestionGroup.alpha -= Time.deltaTime;
            yield return null;
        }
    }
}