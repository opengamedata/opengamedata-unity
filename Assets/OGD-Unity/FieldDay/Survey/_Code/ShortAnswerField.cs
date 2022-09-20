using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay
{
    public class ShortAnswerField : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private TMP_InputField m_InputField = null;

        #endregion // Inspector

        public string Answer { get { return m_InputField.text; } }

        private Action<ShortAnswerField> m_OnSubmitted;

        private void Awake()
        {
            m_InputField.onValueChanged.AddListener(delegate{OnValueChanged(m_InputField);});
        }

        public void Initialize(Action<ShortAnswerField> inSubmittedCallback)
        {
            m_OnSubmitted = inSubmittedCallback;
        }

        private void OnValueChanged(TMP_InputField input)
        {
            m_OnSubmitted(this);
        }
    }
}
