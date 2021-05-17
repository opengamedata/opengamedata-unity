using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay
{
    public class QuestionGroup : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject m_AnswerButtonPrefab = null;
        [SerializeField] private Transform m_AnswerButtonRoot = null;
        [SerializeField] private TextMeshProUGUI m_QuestionText = null;
        [SerializeField] private ToggleGroup m_AnswerToggle = null;

        private AnswerButton m_SelectedAnswerButton = null;
        private string m_Id = null;
        private Action<QuestionGroup> m_OnAnswered;
        
        public string Id { get { return m_Id; } }
        public string Question { get { return m_QuestionText.text; } }
        public string SelectedAnswer { get { return m_SelectedAnswerButton.Answer; } }

        public void Initialize(Action<QuestionGroup> inAnsweredCallback, string id, string question, List<string> answers)
        {
            m_OnAnswered = inAnsweredCallback;
            m_QuestionText.text = question;
            m_Id = id;

            foreach (string answer in answers)
            {
                AnswerButton button = Instantiate(m_AnswerButtonPrefab, m_AnswerButtonRoot).GetComponent<AnswerButton>();
                button.Initialize(m_AnswerToggle, OnButtonSelected, answer);
            }
        }

        private void OnButtonSelected(AnswerButton inAnswerButton)
        {
            m_SelectedAnswerButton = inAnswerButton;
            m_OnAnswered(this);
        }
    }
}
