using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay
{
    public class QuestionGroup : MonoBehaviour
    {
        #region Inspector

        [Header("UI Dependencies")]
        [SerializeField] private GameObject m_AnswerButtonPrefab = null;
        [SerializeField] private Transform m_AnswerButtonRoot = null;
        [SerializeField] private TextMeshProUGUI m_QuestionText = null;
        [SerializeField] private ToggleGroup m_AnswerToggle = null;

        #endregion // Inspector

        private string m_Id = null;
        private string m_SelectedAnswer = null;
        private Action<QuestionGroup> m_OnAnswered;

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
            
            foreach (string answer in inSurveyQuestion.Answers)
            {
                AnswerButton button = Instantiate(m_AnswerButtonPrefab, m_AnswerButtonRoot).GetComponent<AnswerButton>();
                button.Initialize(answer, m_AnswerToggle, OnButtonSelected);
            }
        }

        private void OnButtonSelected(AnswerButton inAnswerButton)
        {
            m_SelectedAnswer = inAnswerButton.Answer;
            m_OnAnswered(this);
        }
    }
}
