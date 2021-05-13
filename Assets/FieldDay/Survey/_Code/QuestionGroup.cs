using System;
using System.Collections.Generic;
using BeauPools;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay
{
    public class QuestionGroup : MonoBehaviour
    {
        [Serializable] private class ButtonPool : SerializablePool<AnswerButton> {  }

        [Header("Pools")]
        [SerializeField] private ButtonPool m_ButtonPool = null;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI m_QuestionText = null;
        [SerializeField] private ToggleGroup m_AnswerToggle = null;

        private AnswerButton m_SelectedAnswerButton = null;
        private Action<QuestionGroup> m_OnAnswered;

        private List<string> m_Answers = new List<string>()
        {
            "Disagree",
            "Somewhat Disagree",
            "Neutral",
            "Somewhat Agree",
            "Agree"
        };

        public void Initialize(Action<QuestionGroup> inAnsweredCallback, string question)
        {
            m_OnAnswered = inAnsweredCallback;
            m_QuestionText.text = question;

            foreach(string answer in m_Answers)
            {
                AnswerButton button = m_ButtonPool.Alloc();
                button.Initialize(m_AnswerToggle, OnButtonSelected, answer);
            }
        }

        private void OnButtonSelected(AnswerButton inAnswerButton)
        {
            m_SelectedAnswerButton = inAnswerButton;
            Debug.Log(inAnswerButton.Text);
        }
    }
}
