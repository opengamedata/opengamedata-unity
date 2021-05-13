using System;
using System.Collections.Generic;
using BeauPools;
using UnityEngine;

namespace FieldDay
{
    public class Survey : MonoBehaviour
    {
        [Serializable] private class GroupPool : SerializablePool<QuestionGroup> {  }
        
        [Header("Pools")]
        [SerializeField] private GroupPool m_GroupPool = null;

        private List<string> m_SurveyQuestions = new List<string>()
        {
            "one",
            "two",
            "three",
            "four",
            "five"
        };

        private void Awake()
        {
            foreach(string question in m_SurveyQuestions)
            {
                QuestionGroup group = m_GroupPool.Alloc();
                group.Initialize(OnAnswerChosen, question);
            }
        }

        private void OnAnswerChosen(QuestionGroup inQuestionGroup)
        {

        }
    }
}
