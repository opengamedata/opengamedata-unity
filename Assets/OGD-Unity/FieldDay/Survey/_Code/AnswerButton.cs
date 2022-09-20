using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay
{
    public class AnswerButton : MonoBehaviour
    {
        #region Inspector

        [Header("UI Dependencies")]
        [SerializeField] private TextMeshProUGUI m_AnswerText = null;
        [SerializeField] private Toggle m_Toggle = null;

        #endregion // Inspector

        public string Answer { get { return m_AnswerText.text; } }

        private Action<AnswerButton> m_OnSelected;
        private Action<AnswerButton> m_OnDeselected;

        private void Awake()
        {
            m_Toggle.onValueChanged.AddListener(OnToggle);

            if (m_Toggle.isOn)
            {
                m_Toggle.isOn = false;
            }
        }

        public void Initialize(string answer, Action<AnswerButton> inSelectedCallback, Action<AnswerButton> inDeselectedCallback, ToggleGroup inGroup)
        {
            m_AnswerText.text = answer;
            m_Toggle.group = inGroup;
            m_OnSelected = inSelectedCallback;
            m_OnDeselected = inDeselectedCallback;
        }

        private void OnToggle(bool inValue)
        {
            if (inValue)
            {
                m_AnswerText.faceColor = new Color32(255, 255, 255, 255);
                m_OnSelected(this);
            }
            else
            {
                m_AnswerText.faceColor = new Color32(0, 0, 0, 255);
                if (m_OnDeselected != null) m_OnDeselected(this);
            }
        }
    }
}
