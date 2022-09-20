using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay
{
    public class QuestionGroup : MonoBehaviour
    {
        #region Inspector

        [Header("UI Dependencies")]
        [SerializeField] private TextMeshProUGUI m_QuestionText = null;
        [SerializeField] private GameObject m_AnswerButtonPrefab = null;
        [SerializeField] private Transform m_AnswerButtonRoot = null;
        [SerializeField] private ToggleGroup m_AnswerToggle = null;
        [SerializeField] private ShortAnswerField m_ShortAnswerPrefab = null;

        #endregion // Inspector

        private string m_Id = null;
        private string m_SelectedAnswer = null;
        private Action<QuestionGroup> m_OnAnswered;
        private List<string> m_MultipleChoiceAnswers = new List<string>();

        #region Accessors
        
        public string Id { get { return m_Id; } }
        public string Question { get { return m_QuestionText.text; } }
        public string SelectedAnswer { get { return m_SelectedAnswer; } }

        #endregion // Accessors

        public void Initialize(SurveyQuestion inSurveyQuestion, Action<QuestionGroup> inAnsweredCallback)
        {
            m_Id = inSurveyQuestion.Id;
            m_QuestionText.text = inSurveyQuestion.Text;
            m_OnAnswered = inAnsweredCallback;
            
            if (inSurveyQuestion.Type.Equals("single-choice"))
            {
                foreach (string answer in inSurveyQuestion.Answers)
                {
                    AnswerButton button = Instantiate(m_AnswerButtonPrefab, m_AnswerButtonRoot).GetComponent<AnswerButton>();
                    button.Initialize(answer, OnSingleChoiceSelected, null, m_AnswerToggle);
                }
            }
            else if (inSurveyQuestion.Type.Equals("multiple-choice"))
            {
                foreach (string answer in inSurveyQuestion.Answers)
                {
                    AnswerButton button = Instantiate(m_AnswerButtonPrefab, m_AnswerButtonRoot).GetComponent<AnswerButton>();
                    button.Initialize(answer, OnMultipleChoiceSelected, OnMultipleChoiceDeselected, null);
                }
            }
            else if (inSurveyQuestion.Type.Equals("short-answer"))
            {
                ShortAnswerField field = Instantiate(m_ShortAnswerPrefab, m_AnswerButtonRoot).GetComponent<ShortAnswerField>();
                field.Initialize(OnShortAnswerSubmitted);
            }
        }

        private void OnSingleChoiceSelected(AnswerButton inAnswerButton)
        {
            m_SelectedAnswer = inAnswerButton.Answer;
            m_OnAnswered(this);
        }

        private void OnMultipleChoiceSelected(AnswerButton inAnswerButton)
        {
            m_MultipleChoiceAnswers.Add(inAnswerButton.Answer);
            m_SelectedAnswer = string.Join(",", m_MultipleChoiceAnswers);
            m_OnAnswered(this);
        }

        private void OnMultipleChoiceDeselected(AnswerButton inAnswerButton)
        {
            m_MultipleChoiceAnswers.Remove(inAnswerButton.Answer);
            m_SelectedAnswer = string.Join(",", m_MultipleChoiceAnswers);
            m_OnAnswered(this);
        }

        private void OnShortAnswerSubmitted(ShortAnswerField inShortAnswerField)
        {
            m_SelectedAnswer = inShortAnswerField.Answer;
            m_OnAnswered(this);
        }
    }
}
