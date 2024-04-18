using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay
{
    public class SurveyQuestionDisplay : MonoBehaviour
    {
        private struct ToggleCache {
            public Toggle Toggle;
            public TMP_Text Label;
        }

        #region Inspector

        [Header("Prompt")]
        [SerializeField] private TMP_Text m_PromptText = null;
        [SerializeField, Tooltip("If set, this will be prepended to the prompt, with the number of the question replacing the '{0}'.\nUse <indent=x%> to indent the prompt itself.")]
        private string m_PrefixFormat = "Q{0}:<indent=10%>";

        [Header("Toggles")]
        [SerializeField] private ToggleGroup m_ToggleGroup = null;
        [SerializeField] private LayoutGroup m_ToggleLayout = null;

        #endregion // Inspector

        public Action OnAnswerUpdated;

        [NonSerialized] private SurveyQuestion m_CurrentQuestion;
        [NonSerialized] private List<ToggleCache> m_InstantiatedToggles = new List<ToggleCache>(8);
        [NonSerialized] private int m_TogglesInUse = 0;
        [NonSerialized] private bool m_Populating;

        /// <summary>
        /// Retrieves the current response.
        /// </summary>
        public SurveyQuestionResponse GetResponse() {
            SurveyQuestionResponse response;
            response.Prompt = m_CurrentQuestion.Prompt;
            
            // NOTE: will need modification to handle questions with multiple responses.
            for(int i = 0; i < m_TogglesInUse; i++) {
                ToggleCache cache = m_InstantiatedToggles[i];
                if (cache.Toggle.isOn) {
                    response.Response = cache.Label.text;
                    return response;
                }
            }

            response.Response = null;
            return response;
        }

        /// <summary>
        /// Returns if the player has input an answer.
        /// </summary>
        public bool HasAnswer() {
            for(int i = 0; i < m_TogglesInUse; i++) {
                if (m_InstantiatedToggles[i].Toggle.isOn) {
                    return true;
                }
            }

            return false;
        }

        private void OnToggleSet(bool state) {
            if (m_Populating) {
                return;
            }

            // NOTE: will need to be modified for questions with multiple allowed responses
            m_ToggleGroup.allowSwitchOff = false;
            
            if (state) {
                OnAnswerUpdated?.Invoke();
            }
        }

        #region Responses

        public void LoadQuestion(SurveyQuestion question, int questionIndex, Toggle responsePrefab) {
            m_CurrentQuestion = question;

            string prompt = question.Prompt;
            if (!string.IsNullOrEmpty(m_PrefixFormat)) {
                prompt = string.Format(m_PrefixFormat, questionIndex + 1) + prompt;
            }
            m_PromptText.SetText(prompt);

            PrepareResponses(question.Responses.Length, responsePrefab);
            for(int i = 0; i < question.Responses.Length; i++) {
                m_InstantiatedToggles[i].Label.SetText(question.Responses[i]);
            }

            SurveyPanel.RecursiveLayoutRebuild((RectTransform) m_ToggleLayout.transform);
        }

        private void PrepareResponses(int capacity, Toggle prefab) {
            m_Populating = true;
            m_ToggleGroup.allowSwitchOff = true;

            while(m_InstantiatedToggles.Count < capacity) {
                ToggleCache cache;
                cache.Toggle = Instantiate(prefab, m_ToggleLayout.transform);
                cache.Label = cache.Toggle.GetComponentInChildren<TMP_Text>();
                cache.Toggle.onValueChanged.AddListener(OnToggleSet);
                cache.Toggle.group = m_ToggleGroup;
                m_InstantiatedToggles.Add(cache);
            }

            for(int i = 0; i < m_InstantiatedToggles.Count; i++) {
                m_InstantiatedToggles[i].Toggle.gameObject.SetActive(i < capacity);
#if UNITY_2019_1_OR_NEWER                
                m_InstantiatedToggles[i].Toggle.SetIsOnWithoutNotify(false);
#else
                m_InstantiatedToggles[i].Toggle.isOn = false;
#endif
            }

            m_TogglesInUse = capacity;
            m_Populating = false;
        }

        #endregion // Responses
    }
}
