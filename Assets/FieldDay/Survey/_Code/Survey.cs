using System.Collections.Generic;
using System.Runtime.InteropServices;
using BeauData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay
{
    public class Survey : MonoBehaviour
    {
        [DllImport("__Internal")]
        public static extern string FetchSurvey(string surveyName);
    
        #region Inspector

        [Header("UI Dependencies")]
        [SerializeField] private GameObject m_QuestionGroupPrefab = null;
        [SerializeField] private Transform m_QuestionGroupRoot = null;
        [SerializeField] private Button m_SubmitButton = null;

        #endregion // Inspector

        private TextAsset m_DefaultJSON = null;
        private ISurveyHandler m_SurveyHandler = null;
        private Dictionary<string, string> m_SelectedAnswers = new Dictionary<string, string>();
        private List<SurveyQuestion> m_Questions = new List<SurveyQuestion>();
        private int m_QuestionIndex = 0;

        public void Initialize(string inSurveyName, TextAsset inDefaultJSON, ISurveyHandler inSurveyHandler, bool displaySkipButton = false)
        {
            m_DefaultJSON = inDefaultJSON;
            m_SurveyHandler = inSurveyHandler;

            m_SubmitButton.onClick.AddListener(OnSubmit);
            if (displaySkipButton) m_SubmitButton.gameObject.SetActive(true);

            #if UNITY_EDITOR
            ReadSurveyData(string.Empty);
            #else
            FetchSurvey(inSurveyName);
            #endif
        }

        private void ReadSurveyData(string inSurveyString)
        {
            SurveyData surveyData = null;

            if (inSurveyString.Equals(string.Empty))
            {
                surveyData = Serializer.Read<SurveyData>(m_DefaultJSON);
            }
            else
            {
                surveyData = Serializer.Read<SurveyData>(inSurveyString);
            }

            m_Questions = surveyData.Questions;
            DisplayNextQuestion();
        }

        private void DisplayNextQuestion()
        {
            SurveyQuestion surveyQuestion = m_Questions[m_QuestionIndex];
            QuestionGroup group = Instantiate(m_QuestionGroupPrefab, m_QuestionGroupRoot).GetComponent<QuestionGroup>();

            group.Initialize(surveyQuestion, OnAnswerChosen);
            m_QuestionIndex++;
        }

        private void OnAnswerChosen(QuestionGroup inQuestionGroup)
        {
            if (!m_SelectedAnswers.ContainsKey(inQuestionGroup.Id))
            {
                if (m_SelectedAnswers.Count < m_Questions.Count - 1)
                {
                    DisplayNextQuestion();
                }
                else
                {
                    m_SubmitButton.GetComponentInChildren<TextMeshProUGUI>().text = "Submit";
                    m_SubmitButton.gameObject.SetActive(true);
                }
            }

            m_SelectedAnswers[inQuestionGroup.Id] = inQuestionGroup.SelectedAnswer;
        }

        private void OnSubmit()
        {
            m_SurveyHandler.HandleSurveyResponse(m_SelectedAnswers);
            Destroy(this.gameObject);
        }
    }
}
