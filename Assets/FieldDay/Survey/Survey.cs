using System;
using System.Collections;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using UnityEngine;

namespace FieldDay
{
    public class Survey : MonoBehaviour
    {
        [Serializable] private class GroupPool : SerializablePool<QuestionGroup> {  }
        [Serializable] private class ButtonPool : SerializablePool<AnswerButton> {  }

        [Header("Pools")]
        [SerializeField] private GroupPool m_GroupPool = null;
        [SerializeField] private ButtonPool m_ButtonPool = null;

        private List<string> m_SurveyQuestions = new List<string>()
        {
            "one",
            "two",
            "three",
            "four",
            "five"
        };

        private List<string> m_DefaultAnswers = new List<string>()
        {
            "Disagree",
            "Somewhat Disagree",
            "Neutral",
            "Somewhat Agree",
            "Agree"
        };

        private void Start()
        {
            foreach(string question in m_SurveyQuestions)
            {
                AllocateGroup(question);
            }
        }

        private void AllocateGroup(string question)
        {
            QuestionGroup group = m_GroupPool.Alloc();

        }

        private void AllocateButtons()
        {
            AnswerButton button;

            foreach(string answer in m_DefaultAnswers)
            {
                button = m_ButtonPool.Alloc();
            }
        }
    }
}
