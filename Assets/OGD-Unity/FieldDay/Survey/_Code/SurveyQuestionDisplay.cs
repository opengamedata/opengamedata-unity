using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OGD
{
    public class SurveyQuestionDisplay : MonoBehaviour
    {
        private struct ToggleCache {
            public Toggle Toggle;
            public TMP_Text Label;
        }

        private enum MultiSelectMode {
            None,
            Normal,
            Nullable
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
        [NonSerialized] private MultiSelectMode m_MultiSelect;

        static private List<string> s_ResponseTextWorkList = new List<string>(8);
        static private List<string> s_ResponseFlagsWorkList = new List<string>(8);

        /// <summary>
        /// Retrieves the current response.
        /// </summary>
        public SurveyQuestionResponse GetResponse() {
            SurveyQuestionResponse response;
            response.Prompt = m_CurrentQuestion.Prompt;
            response.Type = m_CurrentQuestion.Type;

            for(int i = 0; i < m_TogglesInUse; i++) {
                ToggleCache cache = m_InstantiatedToggles[i];
                if (cache.Toggle.isOn) {
                    s_ResponseTextWorkList.Add(cache.Label.text);
                    if (m_CurrentQuestion.ResponseFlags != null && i < m_CurrentQuestion.ResponseFlags.Length) {
                        s_ResponseFlagsWorkList.Add(m_CurrentQuestion.ResponseFlags[i]);
                    }
                    if (m_MultiSelect == MultiSelectMode.None) {
                        break;
                    }
                }
            }

            response.Responses = s_ResponseTextWorkList.ToArray();
            response.Flags = s_ResponseFlagsWorkList.ToArray();

            s_ResponseFlagsWorkList.Clear();
            s_ResponseTextWorkList.Clear();

            return response;
        }

        /// <summary>
        /// Returns if the player has input an answer.
        /// </summary>
        public bool HasAnswer() {
            if (m_MultiSelect == MultiSelectMode.Nullable) {
                return true;
            }

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

            m_ToggleGroup.allowSwitchOff = false;
            
            if (state) {
                OnAnswerUpdated?.Invoke();
            }
        }

        #region Responses

        public void LoadQuestion(SurveyQuestion question, int questionIndex, Toggle responsePrefab) {
            m_CurrentQuestion = question;

            if (question.Type == SurveyPromptTypes.MultiSelect) {
                m_MultiSelect = MultiSelectMode.Normal;
            } else if (question.Type == SurveyPromptTypes.MultiSelectNullable) {
                m_MultiSelect = MultiSelectMode.Nullable;
            } else {
                m_MultiSelect = MultiSelectMode.None;
            }

            string prompt = question.Prompt;
            if (!string.IsNullOrEmpty(m_PrefixFormat)) {
                prompt = string.Format(m_PrefixFormat, questionIndex + 1) + prompt;
            }
            m_PromptText.SetText(prompt);

            AxisLayoutGroup asAxis = m_ToggleLayout as AxisLayoutGroup;
            if (asAxis) {
                asAxis.IsVertical = question.UseVerticalLayout;
            }

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
                m_InstantiatedToggles.Add(cache);
            }

            ToggleGroup targetGroup;
            if (m_MultiSelect != MultiSelectMode.None) {
                targetGroup = null;
            } else {
                targetGroup = m_ToggleGroup;
            }

            for (int i = 0; i < m_InstantiatedToggles.Count; i++) {
                m_InstantiatedToggles[i].Toggle.gameObject.SetActive(i < capacity);
                m_InstantiatedToggles[i].Toggle.group = targetGroup;
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
