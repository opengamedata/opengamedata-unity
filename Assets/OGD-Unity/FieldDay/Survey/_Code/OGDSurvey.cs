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
    /// Survey manager.
    /// </summary>
    public class OGDSurvey
    {
        /// <summary>
        /// Invoked when a survey is displayed.
        /// </summary>
        public SurveyPanel.CallbackDelegate OnSurveyBegin;

        /// <summary>
        /// Invoked when a survey is completed.
        /// </summary>
        public SurveyPanel.CallbackDelegate OnSurveyEnd;
        
        private SurveyPanel m_SurveyPrefab;
        private SurveyPanel m_InstantiatedSurvey;
        private Action m_CachedResumeCallback;
        private SurveyPackage m_CurrentSurveyPackage;
        private OGDLog m_Logger;

        public OGDSurvey(SurveyPanel surveyPrefab, OGDLog logger) {
            if (surveyPrefab == null) {
                throw new ArgumentNullException("surveyPrefab", "Cannot instantiate an OGDSurvey without a prefab.");
            }
            if (logger == null) {
                Debug.LogWarning("[OGDSurvey] No OGDLog instance was passed to constructor - survey results will not be logged to server");
            }

            m_SurveyPrefab = surveyPrefab;
            m_Logger = logger;
        }

        #region Package Management

        /// <summary>
        /// Loads an existing SurveyPackage instance.
        /// </summary>
        public void LoadSurveyPackage(SurveyPackage package) {
            if (package != null && !SurveyPackage.Validate(package)) {
                Debug.LogError("[OGDSurvey] Survey package contains invalid data");
                return;
            }

            m_CurrentSurveyPackage = package;
        }

        /// <summary>
        /// Parses the given JSON string into a SurveyPackage instance.
        /// </summary>
        public void LoadSurveyPackageFromString(string packageJSON) {
            m_CurrentSurveyPackage = SurveyPackage.Parse(packageJSON);
            if (m_CurrentSurveyPackage == null) {
                Debug.LogError("[OGDSurvey] Survey package was unable to be parsed");
            } else if (!SurveyPackage.Validate(m_CurrentSurveyPackage)) {
                Debug.LogError("[OGDSurvey] Survey package contains invalid data");
                m_CurrentSurveyPackage = null;
            }
        }

        /// <summary>
        /// Parses the given JSON file into a SurveyPackage instance.
        /// </summary>
        public void LoadSurveyPackageFromString(TextAsset packageJSONFile) {
            LoadSurveyPackageFromString(packageJSONFile.text);
        }

        #endregion // Package Management

        #region Survey Display

        /// <summary>
        /// Attempts to display a survey with the given event id.
        /// </summary>
        public bool TryDisplaySurvey(string displayEventId) {
            if (m_InstantiatedSurvey != null) {
                Debug.LogErrorFormat("[OGDSurvey] Survey '{0}' is still displaying - cannot display a survey for '{1}'", m_InstantiatedSurvey.CurrentSurvey.DisplayEventId, displayEventId);
                return false;
            }

            if (m_CurrentSurveyPackage == null) {
                Debug.LogWarningFormat("[OGDSurvey] No survey package has been loaded");
                return false;
            }

            SurveyData survey = Array.Find(m_CurrentSurveyPackage.Surveys, (d) => StringComparer.Ordinal.Equals(d.DisplayEventId, displayEventId));
            if (survey == null) {
                return false;
            }

            m_InstantiatedSurvey = GameObject.Instantiate(m_SurveyPrefab);
            m_InstantiatedSurvey.OnFinished += OnSurveyFinished;
            m_InstantiatedSurvey.LoadSurvey(m_CurrentSurveyPackage.PackageConfigId, survey, m_Logger);
            OnSurveyBegin?.Invoke(m_InstantiatedSurvey);
            return true;
        }

        /// <summary>
        /// Attempts to display a survey with the given event id.
        /// Invokes a callback when the survey is finished, or if no survey was displayed.
        /// </summary>
        public bool DisplaySurvey(string displayEventId, Action resumeCallback) {
            bool displayed = TryDisplaySurvey(displayEventId);
            if (displayed) {
                m_CachedResumeCallback = resumeCallback;
                return true;
            } else {
                resumeCallback?.Invoke();
                return false;
            }
        }

        /// <summary>
        /// Attempts to display a survey with the given event id.
        /// This routine will wait until the survey is completed to resume.
        /// </summary>
        public IEnumerator DisplaySurveyAndWait(string displayEventId) {
            if (TryDisplaySurvey(displayEventId)) {
                while(m_InstantiatedSurvey != null) {
                    yield return null;
                }
            }
        }

        /// <summary>
        /// Cancels the currently running survey immediately.
        /// </summary>
        public bool CancelSurvey() {
            if (m_InstantiatedSurvey != null) {
                SurveyPanel cachedSurvey = m_InstantiatedSurvey;
                m_InstantiatedSurvey = null;

                OnSurveyEnd?.Invoke(cachedSurvey);
                m_CachedResumeCallback?.Invoke();
                m_CachedResumeCallback = null;
                
                GameObject.Destroy(cachedSurvey.gameObject);
                return true;
            }

            return false;
        }

        #endregion // Survey Display

        #region Handlers

        private void OnSurveyFinished(SurveyPanel panel) {
            if (panel != m_InstantiatedSurvey) {
                return;
            }

            OnSurveyEnd?.Invoke(panel);
            m_CachedResumeCallback?.Invoke();
            m_CachedResumeCallback = null;
            m_InstantiatedSurvey = null;
        }

        #endregion // Handlers
    }
}
