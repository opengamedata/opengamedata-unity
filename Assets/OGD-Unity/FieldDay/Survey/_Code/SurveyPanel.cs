#if UNITY_EDITOR
#define SURVEY_EDITOR
#elif UNITY_WEBGL
#define SURVEY_WEBGL
#else
#define SURVEY_DISABLED
#endif //

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OGD
{
    /// <summary>
    /// Survey display and response panel.
    /// </summary>
    public class SurveyPanel : MonoBehaviour
    {
        #region Inspector

        [SerializeField] private CanvasGroup m_InputControl = null;

        [Header("Header")]
        [SerializeField] private TMP_Text m_Header = null;
        [SerializeField] private TMP_Text m_Subheader = null;

        [Header("Questions")]
        [SerializeField] private LayoutGroup m_QuestionLayout = null;
        [SerializeField] private SurveyQuestionDisplay m_QuestionPrefab = null;
        [SerializeField] private Toggle m_ResponsePrefab = null;

        [Header("Controls")]
        [SerializeField] private CanvasGroup m_ButtonGroup = null;
        [SerializeField] private Button m_ContinueButton = null;
        [SerializeField] private Button m_FinishButton = null;
        [SerializeField] private float m_ButtonGroupDisabledAlpha = 0.5f;

        [Header("Extra")]
        [SerializeField, Tooltip("Optional. Will display the current page along with the total page count.")] private TMP_Text m_PageCountDisplay = null;
        [SerializeField] private string m_PageCountFormat = "{0}/{1}";

        #endregion // Inspector

        #region Callbacks

        public delegate void CallbackDelegate(SurveyPanel panel);
        public delegate IEnumerator AnimationDelegate(SurveyPanel panel);
        public delegate void PageLayoutDelegate(SurveyPanel panel, SurveyPage page);
        public delegate void PopulateQuestionDelegate(SurveyQuestionDisplay question, SurveyQuestion data, SurveyPage page);

        /// <summary>
        /// Called when data is loaded.
        /// </summary>
        public CallbackDelegate OnLoaded;

        /// <summary>
        /// Animation for opening a new page.
        /// </summary>
        public AnimationDelegate OpenPageAnim;

        /// <summary>
        /// Animation for closing the current page.
        /// </summary>
        public AnimationDelegate ClosePageAnim;

        /// <summary>
        /// Animation for submitting survey results and closing.
        /// </summary>
        public AnimationDelegate FinishedAnim;

        /// <summary>
        /// Delegate invoked when before page layout.
        /// </summary>
        public PageLayoutDelegate OnPageLayout;

        /// <summary>
        /// Delegate invoked when a question display is populated.
        /// </summary>
        public PopulateQuestionDelegate OnPopulateQuestion;

        /// <summary>
        /// Function invoked when the next/finish button changes state.
        /// </summary>
        public Action<bool> OnNextButtonState;

        /// <summary>
        /// Function invoked when the survey is completed and closed.
        /// </summary>
        public CallbackDelegate OnFinished;

        #endregion // Callbacks

        [NonSerialized] private string m_SurveyPackageId;
        [NonSerialized] private SurveyData m_CurrentSurvey;
        private OGDLog m_Logger;
        [NonSerialized] private int m_CurrentPageIndex;
        [NonSerialized] private List<SurveyQuestionResponse> m_AccumulatedResponses = new List<SurveyQuestionResponse>(8);
        [NonSerialized] private List<SurveyQuestionDisplay> m_InstantiatedQuestions = new List<SurveyQuestionDisplay>(4);
        [NonSerialized] private int m_QuestionsInUse = 0;
        [NonSerialized] private List<int> m_PageOffsets = new List<int>(4);
        [NonSerialized] private HashSet<string> m_Flags = new HashSet<string>();
        private StringBuilder m_CachedBuilder = new StringBuilder(256);
        private Action m_OnClosed;

        private Coroutine m_CurrentRoutine;

        /// <summary>
        /// The current survey data.
        /// </summary>
        public SurveyData CurrentSurvey {
            get { return m_CurrentSurvey; }
        }

        /// <summary>
        /// Enumerable set of active questions.
        /// </summary>
        public IEnumerable<SurveyQuestionDisplay> ActiveQuestions() {
            for(int i = 0; i < m_QuestionsInUse; i++) {
                yield return m_InstantiatedQuestions[i];
            }
        }

        /// <summary>
        /// Header label.
        /// </summary>
        public TMP_Text HeaderLabel {
            get { return m_Header; }
        }

        /// <summary>
        /// Subheader label.
        /// </summary>
        public TMP_Text SubheaderLabel {
            get { return m_Subheader; }
        }

        /// <summary>
        /// Sets if input is currently allowed.
        /// </summary>
        public void SetInputActive(bool active) {
            m_InputControl.interactable = active;
        }

        #region Unity Events

        private void Awake() {
            m_ContinueButton.onClick.AddListener(OnNextClicked);
            m_FinishButton.onClick.AddListener(OnNextClicked);
        }

        #endregion // Unity Events

        #region Loading

        /// <summary>
        /// Loads the survey and prompts the player to fill it out.
        /// </summary>
        public void LoadSurvey(string packageId, SurveyData data, OGDLog logger) {
            m_SurveyPackageId = packageId;
            m_CurrentSurvey = data;
            m_Logger = logger;

            m_Header.SetText(data.Header);

            m_AccumulatedResponses.Clear();
            m_Flags.Clear();
            GeneratePageQuestionOffsets(data);
            OnLoaded?.Invoke(this);

            LoadPage(0);
        }

        private void GeneratePageQuestionOffsets(SurveyData data) {
            m_PageOffsets.Clear();
            int offset = 0;
            for(int i = 0; i < data.Pages.Length; i++) {
                offset += data.Pages[i].Questions.Length;
                m_PageOffsets.Add(offset);
            }
        }

        private void LoadPage(int pageIndex) {
            m_CurrentPageIndex = pageIndex;
            int totalPages = m_CurrentSurvey.Pages.Length;
            if (pageIndex >= totalPages) {
                FlushData();
                DeclareFinished();
                return;
            }

            SurveyPage page = m_CurrentSurvey.Pages[pageIndex];

            OnPageLayout?.Invoke(this, page);

            if (m_PageCountDisplay != null) {
                if (totalPages > 1) {
                    m_PageCountDisplay.gameObject.SetActive(true);
                    m_PageCountDisplay.SetText(string.Format(m_PageCountFormat, pageIndex + 1, totalPages));
                } else {
                    m_PageCountDisplay.gameObject.SetActive(false);
                }
            }

            bool lastPage = pageIndex >= totalPages - 1;
            m_ContinueButton.gameObject.SetActive(!lastPage);
            m_FinishButton.gameObject.SetActive(lastPage);

            if (m_Subheader) {
                if (string.IsNullOrEmpty(page.SubHeader)) {
                    m_Subheader.gameObject.SetActive(false);
                } else {
                    m_Subheader.gameObject.SetActive(true);
                    m_Subheader.SetText(page.SubHeader);
                }
            }

            PrepareQuestions(page.Questions.Length);

            int questionIndexOffset = pageIndex > 0 ? m_PageOffsets[pageIndex - 1] : 0;

            for(int i = 0; i < page.Questions.Length; i++) {
                m_InstantiatedQuestions[i].LoadQuestion(page.Questions[i], i + questionIndexOffset, m_ResponsePrefab);
                OnPopulateQuestion?.Invoke(m_InstantiatedQuestions[i], page.Questions[i], page);
            }

            RecursiveLayoutRebuild((RectTransform) m_QuestionLayout.transform);
            
            m_ButtonGroup.interactable = m_QuestionsInUse == 0;
            m_ButtonGroup.alpha = m_QuestionsInUse == 0 ? 1 : m_ButtonGroupDisabledAlpha;

            KillOngoingRoutine();

            if (OpenPageAnim != null) {
                m_CurrentRoutine = StartCoroutine(OpenPageRoutine());
            }
        }

        private void PrepareQuestions(int capacity) {
            while(m_InstantiatedQuestions.Count < capacity) {
                SurveyQuestionDisplay display = Instantiate(m_QuestionPrefab, m_QuestionLayout.transform);
                m_InstantiatedQuestions.Add(display);
                display.OnAnswerUpdated += UpdateControls;
            }

            for(int i = 0; i < m_InstantiatedQuestions.Count; i++) {
                m_InstantiatedQuestions[i].gameObject.SetActive(i < capacity);
            }

            m_QuestionsInUse = capacity;
        }

        #endregion // Loading
    
        #region Handlers

        private void UpdateControls() {
            bool fullyAnswered = true;
            for(int i = 0; i < m_QuestionsInUse; i++) {
                fullyAnswered &= m_InstantiatedQuestions[i].HasAnswer();
            }

            bool prev = m_ButtonGroup.interactable;
            m_ButtonGroup.interactable = fullyAnswered;
            m_ButtonGroup.alpha = fullyAnswered ? 1 : m_ButtonGroupDisabledAlpha;

            if (prev != fullyAnswered) {
                OnNextButtonState?.Invoke(fullyAnswered);
            }
        }

        private void OnNextClicked() {
            SubmitPage();

            do {
                m_CurrentPageIndex++;
            } while (m_CurrentPageIndex < m_CurrentSurvey.Pages.Length && ShouldSkipPageAtIndex(m_CurrentPageIndex));

            KillOngoingRoutine();

            m_ButtonGroup.interactable = false;
            m_ButtonGroup.alpha = m_ButtonGroupDisabledAlpha;

            if (m_CurrentPageIndex >= m_CurrentSurvey.Pages.Length) {
                FlushData();
                if (FinishedAnim != null) {
                    m_CurrentRoutine = StartCoroutine(FinishRoutine());
                } else {
                    DeclareFinished();
                }
            } else {
                if (ClosePageAnim != null) {
                    m_CurrentRoutine = StartCoroutine(NextPageRoutine());
                } else {
                    LoadPage(m_CurrentPageIndex);
                }
            }
        }

        private bool ShouldSkipPageAtIndex(int index) {
            SurveyPage page = m_CurrentSurvey.Pages[index];
            string[] conditions = page.SkipConditions;
            if (conditions == null || conditions.Length == 0) {
                return false;
            }

            for(int i = 0; i < conditions.Length; i++) {
                bool pass;
                string condition = conditions[i];
                if (condition.StartsWith("!")) {
                    pass = !m_Flags.Contains(condition.Substring(1));
                } else {
                    pass = m_Flags.Contains(condition);
                }
                if (pass) {
                    return true;
                }
            }

            return false;
        }

        private void FlushData() {
            SubmitAllResponses();
            m_SurveyPackageId = null;
            m_CurrentSurvey = null;
            m_Logger = null;
            m_QuestionsInUse = 0;
        }

        private void DeclareFinished() {
            OnFinished?.Invoke(this);
            Destroy(gameObject);
        }

        #endregion // Handlers

        #region Submission

        private void SubmitPage() {
            for(int i = 0; i < m_QuestionsInUse; i++) {
                SurveyQuestionResponse response = m_InstantiatedQuestions[i].GetResponse();
                if (response.Responses != null) {
                    m_AccumulatedResponses.Add(response);
                }

                if (response.Flags != null && response.Flags.Length > 0) {
                    foreach(var flag in response.Flags) {
                        m_Flags.Add(flag);
                    }
                }
            }
        }

        private void SubmitAllResponses() {
            if (m_Logger == null) {
                m_AccumulatedResponses.Clear();
                return;
            }

            if (m_AccumulatedResponses.Count <= 0) {
                return;
            }

            m_CachedBuilder.Clear()
                .Append("{\"package_config_id\":\"").EscapeJSON(m_SurveyPackageId).Append("\",")
                .Append("\"display_event_id\":\"").EscapeJSON(m_CurrentSurvey.DisplayEventId).Append("\",")
                .Append("\"responses\":[");

            for(int i = 0; i < m_AccumulatedResponses.Count; i++) {
                SurveyQuestionResponse response = m_AccumulatedResponses[i];
                m_CachedBuilder.Append("{\"prompt\":\"").EscapeJSON(response.Prompt).Append("\",")
                    .Append("\"response\":");
                if (response.Responses.Length > 1 || response.Type != SurveyPromptTypes.Default) {
                    m_CachedBuilder.Append('[');
                    foreach(var responseText in response.Responses) {
                        m_CachedBuilder.Append('\"').EscapeJSON(responseText).Append("\",");
                    }
                    OGDLogUtils.TrimEnd(m_CachedBuilder, ',');
                    m_CachedBuilder.Append(']');
                } else if (response.Responses.Length == 0) {
                    m_CachedBuilder.Append("null");
                } else {
                    m_CachedBuilder.Append('\"').EscapeJSON(response.Responses[0]).Append('\"');
                }
                m_CachedBuilder.Append("},");
            }

            OGDLogUtils.TrimEnd(m_CachedBuilder, ',');
            m_CachedBuilder.Append("]}");

            m_Logger.Log("survey_submitted", m_CachedBuilder);
            m_CachedBuilder.Clear();
            m_AccumulatedResponses.Clear();
        }

        #endregion // Submission

        #region Sequencing

        private void KillOngoingRoutine() {
            if (m_CurrentRoutine != null) {
                StopCoroutine(m_CurrentRoutine);
                m_CurrentRoutine = null;
            }
        }

        private IEnumerator NextPageRoutine() {
            SetInputActive(false);
            yield return ClosePageAnim(this);
            SetInputActive(true);
            LoadPage(m_CurrentPageIndex);
        }

        private IEnumerator OpenPageRoutine() {
            return OpenPageAnim(this);
        }

        private IEnumerator FinishRoutine() {
            yield return FinishedAnim(this);
            DeclareFinished();
        }

        #endregion // Sequencing

        static internal void RecursiveLayoutRebuild(RectTransform root) {
            int childCount = root.childCount;
            for(int i = 0; i < childCount; i++) {
                RecursiveLayoutRebuild((RectTransform) root.GetChild(i));
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        }
    }
}
