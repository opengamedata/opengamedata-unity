using System;
using System.Collections.Generic;
using BeauUtil.Blocks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay
{
    public class Survey : MonoBehaviour
    {
        // temp
        [SerializeField] private SurveyDataPackage m_SurveyAsset = null;

        [Header("UI Dependencies")]
        [SerializeField] private GameObject m_QuestionGroupPrefab = null;
        [SerializeField] private Transform m_QuestionGroupRoot = null;
        [SerializeField] private Button m_SubmitButton = null;

        [Header("Settings")]
        [SerializeField] private bool m_DisplaySkipButton = false;

        [NonSerialized] private List<string> m_DefaultAnswers;

        private static SurveyDataPackage.Generator m_Generator = new SurveyDataPackage.Generator();

        private ISurveyHandler m_SurveyHandler = null;

        private Dictionary<string, string> m_SelectedAnswers = new Dictionary<string, string>();
        private List<string> m_Ids = new List<string>();
        private Dictionary<string, SurveyQuestion> m_Questions = new Dictionary<string, SurveyQuestion>();
        private List<GameObject> m_QuestionGroups = new List<GameObject>();
        private int m_Index = 0;

        private bool IsCompleted { get { return m_SelectedAnswers.Count == m_Questions.Count; } }

        private void Awake()
        {
            this.gameObject.SetActive(false);
            Initialize(m_SurveyAsset, new TestHandler());
        }

        private void Initialize(SurveyDataPackage inPackage, ISurveyHandler inSurveyHandler)
        {
            this.gameObject.SetActive(true);
            inPackage.Parse(BlockParsingRules.Default, m_Generator);

            foreach (string id in inPackage.Questions.Keys)
            {
                m_Ids.Add(id);
            }

            m_Questions = inPackage.Questions;
            m_DefaultAnswers = inPackage.DefaultAnswers;
            m_SurveyHandler = inSurveyHandler;

            m_SubmitButton.onClick.AddListener(OnSubmit);
            if (m_DisplaySkipButton) m_SubmitButton.gameObject.SetActive(true);

            DisplayNextQuestion();
        }

        private void DisplayNextQuestion()
        {
            GameObject go = Instantiate(m_QuestionGroupPrefab, m_QuestionGroupRoot);
            m_QuestionGroups.Add(go);

            QuestionGroup group = go.GetComponent<QuestionGroup>();
            string id = m_Ids[m_Index];
            string question = m_Questions[id].Question;
            List<string> answers = m_Questions[id].Answers;

            group.Initialize(OnAnswerChosen, id, question, answers.Count == 0 ? m_DefaultAnswers : answers);
            m_Index++;
        }

        private void OnAnswerChosen(QuestionGroup inQuestionGroup)
        {
            if (!m_SelectedAnswers.ContainsKey(inQuestionGroup.Id))
            {
                m_SelectedAnswers[inQuestionGroup.Id] = inQuestionGroup.SelectedAnswer;

                if (!IsCompleted)
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
            this.gameObject.SetActive(false);
            Reset();
        }

        private void Reset()
        {
            foreach (GameObject group in m_QuestionGroups)
            {
                Destroy(group);
            }

            m_Ids.Clear();
            m_SelectedAnswers.Clear();
            m_Index = 0;
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
