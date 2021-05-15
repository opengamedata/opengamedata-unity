using System;
using System.Collections.Generic;
using BeauPools;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay
{
    public class Survey : MonoBehaviour
    {
        [Serializable] private class GroupPool : SerializablePool<QuestionGroup> {  }

        [SerializeField] private SurveyDataManager m_SurveyDataManager = null;

        [Header("Pools")]
        [SerializeField] private GroupPool m_GroupPool = null;

        [Header("UI")]
        [SerializeField] private Button m_SubmitButton = null;

        private Dictionary<string, string> m_SelectedAnswers = new Dictionary<string, string>();

        private List<string> m_SurveyQuestions = new List<string>();
        private Dictionary<string, List<string>> m_SurveyAnswers = new Dictionary<string, List<string>>();
        private List<string> m_DefaultAnswers;

        private void Awake()
        {
            m_SurveyDataManager.Apply();
            SurveyDataPackage package = m_SurveyDataManager.GetPackage("Sample");
            m_DefaultAnswers = package.DefaultAnswers;

            foreach(string id in package.Questions.Keys)
            {
                m_SurveyQuestions.Add(package.Questions[id].Question);
                m_SurveyAnswers[package.Questions[id].Question] = package.Questions[id].Answers;
            }

            foreach(string question in m_SurveyQuestions)
            {
                QuestionGroup group = m_GroupPool.Alloc();

                if (m_SurveyAnswers[question].Count == 0)
                {
                    group.Initialize(OnAnswerChosen, question, m_DefaultAnswers);
                }
                else
                {
                    group.Initialize(OnAnswerChosen, question, m_SurveyAnswers[question]);
                }
            }

            m_SubmitButton.onClick.AddListener(OnSubmit);
        }

        private void OnAnswerChosen(QuestionGroup inQuestionGroup)
        {
            m_SelectedAnswers[inQuestionGroup.Question] = inQuestionGroup.SelectedAnswer;

            foreach(string question in m_SelectedAnswers.Keys)
            {
                Debug.Log(question + " " + m_SelectedAnswers[question]);
            }
        }

        private void OnSubmit()
        {
            Debug.Log("submit");
        }
    }
}
