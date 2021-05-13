using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay
{
    public class AnswerButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_AnswerText = null;
        [SerializeField] private Toggle m_Toggle = null;

        public string Text { get { return m_AnswerText.text; } }

        private Action<AnswerButton> m_OnSelected;

        private void Awake()
        {
            m_Toggle.onValueChanged.AddListener(OnToggle);
        }

        public void Initialize(ToggleGroup inGroup, Action<AnswerButton> inSelectedCallback, string answer)
        {
            m_Toggle.group = inGroup;
            m_OnSelected = inSelectedCallback;
            m_AnswerText.text = answer;
        }

        private void OnToggle(bool inValue)
        {
            if (inValue)
            {
                m_OnSelected(this);
            }
        }
    }
}
