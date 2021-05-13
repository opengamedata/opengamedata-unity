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
        
        [Header("Pools")]
        [SerializeField] private GroupPool m_GroupPool = null;

        [Header("UI")]
        [SerializeField] private Button m_SubmitButton = null;

        private Dictionary<string, string> m_SelectedAnswers = new Dictionary<string, string>();

        private List<string> m_SurveyQuestions = new List<string>()
        {
            "one",
            "two",
            "three"
        };

        private void Awake()
        {
            foreach(string question in m_SurveyQuestions)
            {
                QuestionGroup group = m_GroupPool.Alloc();
                group.Initialize(OnAnswerChosen, question);
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
