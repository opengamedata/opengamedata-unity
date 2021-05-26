using System;
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
        private static extern string FetchSurvey();
    
        #region Inspector

        [Header("Survey Dependencies")]
        [SerializeField] private TextAsset m_DefaultJSON = null;

        [Header("UI Dependencies")]
        [SerializeField] private GameObject m_QuestionGroupPrefab = null;
        [SerializeField] private Transform m_QuestionGroupRoot = null;
        [SerializeField] private Button m_SubmitButton = null;

        [Header("Settings")]
        [SerializeField] private bool m_DisplaySkipButton = false;

        #endregion // Inspector

        [NonSerialized] private List<string> m_DefaultAnswers;

        private ISurveyHandler m_SurveyHandler = null;

        private Dictionary<string, string> m_SelectedAnswers = new Dictionary<string, string>();
        private List<SurveyQuestion> m_Questions = new List<SurveyQuestion>();
        private List<GameObject> m_QuestionGroups = new List<GameObject>();
        private int m_QuestionIndex = 0;

        private void Awake()
        {
            #if UNITY_EDITOR
            Initialize(new TestHandler());
            #else
            FetchSurvey();
            #endif
        }

        public void LoadSurvey(string json)
        {
            Initialize(new TestHandler(), json);
        }

        private void Initialize(ISurveyHandler inSurveyHandler, string inSurveyString = null)
        {
            m_SurveyHandler = inSurveyHandler;
            SurveyData surveyData = null;

            if (inSurveyString == null)
            {
                surveyData = Serializer.Read<SurveyData>(m_DefaultJSON);
            }
            else
            {
                surveyData = Serializer.Read<SurveyData>(inSurveyString);
            }

            m_Questions = surveyData.Questions;

            m_SubmitButton.onClick.AddListener(OnSubmit);
            if (m_DisplaySkipButton) m_SubmitButton.gameObject.SetActive(true);

            DisplayNextQuestion();
        }

        private void DisplayNextQuestion()
        {
            GameObject go = Instantiate(m_QuestionGroupPrefab, m_QuestionGroupRoot);
            m_QuestionGroups.Add(go);

            QuestionGroup group = go.GetComponent<QuestionGroup>();
            SurveyQuestion surveyQuestion = m_Questions[m_QuestionIndex];

            group.Initialize(surveyQuestion, OnAnswerChosen);
            m_QuestionIndex++;
        }

        private void OnAnswerChosen(QuestionGroup inQuestionGroup)
        {
            if (!m_SelectedAnswers.ContainsKey(inQuestionGroup.Id))
            {
                m_SelectedAnswers[inQuestionGroup.Id] = inQuestionGroup.SelectedAnswer;

                if (m_SelectedAnswers.Count != m_Questions.Count)
                {
                    DisplayNextQuestion();
                }
                else
                {
                    m_SubmitButton.GetComponentInChildren<TextMeshProUGUI>().text = "Submit";
                    m_SubmitButton.gameObject.SetActive(true);
                }
            }
        }

        private void OnSubmit()
        {
            m_SurveyHandler.HandleSurveyResponse(m_SelectedAnswers);
            Reset();
        }

        private void Reset()
        {
            foreach (GameObject group in m_QuestionGroups)
            {
                Destroy(group);
            }

            m_SelectedAnswers.Clear();
            m_QuestionIndex = 0;
            m_SubmitButton.GetComponentInChildren<TextMeshProUGUI>().text = "Skip";
            this.gameObject.SetActive(false);
        }
    }

    public class TestHandler : ISurveyHandler
    {
        public void HandleSurveyResponse(Dictionary<string, string> surveyResponses)
        {
            foreach (string id in surveyResponses.Keys)
            {
                Debug.Log(id + " " + surveyResponses[id]);
            }
        }
    }
}
