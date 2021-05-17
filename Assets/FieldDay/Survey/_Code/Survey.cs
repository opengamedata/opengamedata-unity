using System;
using System.Collections.Generic;
using BeauUtil.Blocks;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay
{
    public class Survey : MonoBehaviour
    {
        // temp
        [Header("Assets")]
        [SerializeField] private SurveyDataPackage m_SurveyAsset = null;

        [Header("UI")]
        [SerializeField] private GameObject m_QuestionGroupPrefab = null;
        [SerializeField] private Transform m_QuestionGroupRoot = null;
        [SerializeField] private Button m_SubmitButton = null;

        [NonSerialized] private List<string> m_DefaultAnswers;

        private static SurveyDataPackage.Generator m_Generator = new SurveyDataPackage.Generator();

        private Dictionary<string, string> m_SelectedAnswers = new Dictionary<string, string>();

        private ISurveyHandler m_SurveyHandler = null;

        // temp
        private void Awake()
        {
            Initialize(m_SurveyAsset, new TestHandler());
        }

        private void Initialize(SurveyDataPackage inPackage, ISurveyHandler inSurveyHandler)
        {
            inPackage.Parse(BlockParsingRules.Default, m_Generator);
            m_DefaultAnswers = inPackage.DefaultAnswers;
            m_SurveyHandler = inSurveyHandler;

            foreach (string id in inPackage.Questions.Keys)
            {
                string question = inPackage.Questions[id].Question;
                List<string> answers = inPackage.Questions[id].Answers;

                QuestionGroup group = Instantiate(m_QuestionGroupPrefab, m_QuestionGroupRoot).GetComponent<QuestionGroup>();

                if (answers.Count == 0)
                {
                    group.Initialize(OnAnswerChosen, id, question, m_DefaultAnswers);
                }
                else
                {
                    group.Initialize(OnAnswerChosen, id, question, answers);
                }
            }

            m_SubmitButton.onClick.AddListener(OnSubmit);
        }

        private void OnAnswerChosen(QuestionGroup inQuestionGroup)
        {
            m_SelectedAnswers[inQuestionGroup.Id] = inQuestionGroup.SelectedAnswer;
        }

        private void OnSubmit()
        {
            m_SurveyHandler.HandleSurveyResponse(m_SelectedAnswers);
            // TODO: hide/reset survey
        }
    }

    // temp
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
