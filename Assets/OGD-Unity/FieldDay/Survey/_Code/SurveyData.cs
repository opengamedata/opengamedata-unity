using System;
using System.Collections.Generic;
using UnityEngine;

namespace OGD
{
    /// <summary>
    /// Package of survey data.
    /// </summary>
    [Serializable]
    public class SurveyPackage {
        [SerializeField] private string package_config_id;
        [SerializeField] private SurveyData[] surveys;

        /// <summary>
        /// Configuration id.
        /// </summary>
        public string PackageConfigId {
            get { return package_config_id; }
        }

        /// <summary>
        /// Array of surveys.
        /// </summary>
        public SurveyData[] Surveys {
            get { return surveys; }
        }

        /// <summary>
        /// Parses a package from a json string.
        /// </summary>
        static public SurveyPackage Parse(string json) {
            try {
                return JsonUtility.FromJson<SurveyPackage>(json);
            } catch(Exception e) {
                Debug.LogException(e);
                return null;
            }
        }

        /// <summary>
        /// Validates that the survey package has correct (not invalid) data.
        /// </summary>
        static public bool Validate(SurveyPackage package) {
            if (string.IsNullOrEmpty(package.package_config_id)) {
                Debug.LogErrorFormat("[SurveyData] Package has no config id");
                return false;
            }

            if (package.surveys == null) {
                Debug.LogErrorFormat("[SurveyData] Package '{0}' has no surveys", package.package_config_id);
                return false;
            }

            bool valid = true;
            for(int i = 0; i < package.surveys.Length; i++) {
                valid &= SurveyData.Validate(package.surveys[i]);
            }

            return valid;
        }
    }

    /// <summary>
    /// Data describing a single survey.
    /// </summary>
    [Serializable]
    public class SurveyData {
        [SerializeField] private string display_event_id;
        [SerializeField] private string header;
        [SerializeField] private SurveyPage[] pages;

        /// <summary>
        /// Event used to trigger the display event.
        /// </summary>
        public string DisplayEventId {
            get { return display_event_id; }
        }

        /// <summary>
        /// Header text to display for the entire survey.
        /// </summary>
        public string Header {
            get { return header; }
        }

        /// <summary>
        /// Sequence of pages.
        /// </summary>
        public SurveyPage[] Pages {
            get { return pages; }
        }

        /// <summary>
        /// Validates whether or not survey data is properly structured.
        /// </summary>
        static public bool Validate(SurveyData data) {
            if (data == null) {
                return false;
            }

            if (string.IsNullOrEmpty(data.display_event_id) || string.IsNullOrEmpty(data.header)) {
                Debug.LogErrorFormat("[SurveyData] Survey has null display event id or header text");
                return false;
            }

            if (data.pages == null || data.pages.Length <= 0) {
                Debug.LogErrorFormat("[SurveyData] Survey '{0}' has no pages", data.display_event_id);
                return false;
            }

            bool valid = true;
            for(int i = 0; i < data.pages.Length; i++) {
                valid &= Validate(data, data.pages[i]);
            }

            return valid;
        }

        static private bool Validate(SurveyData data, SurveyPage page) {
            if (page.Questions == null || page.Questions.Length <= 0) {
                Debug.LogErrorFormat("[SurveyData] Survey '{0}' has a page with no questions", data.display_event_id);
                return false;
            }

            bool valid = true;
            for(int i = 0; i < page.Questions.Length; i++) {
                valid &= Validate(data, page.Questions[i]);
            }

            return valid;
        }

        static private bool Validate(SurveyData data, SurveyQuestion question) {
            if (string.IsNullOrEmpty(question.Prompt)) {
                Debug.LogErrorFormat("[SurveyData] Survey '{0}' has a question with no prompt", data.display_event_id);
                return false;
            }

            if (question.Responses == null || question.Responses.Length <= 0) {
                Debug.LogErrorFormat("[SurveyData] Survey '{0}' has a question with no responses", data.display_event_id);
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Data describing a page of survey questions.
    /// </summary>
    [Serializable]
    public class SurveyPage {
        [SerializeField] private SurveyQuestion[] items;

        /// <summary>
        /// Array of questions.
        /// </summary>
        public SurveyQuestion[] Questions {
            get { return items; }
        }
    }

    /// <summary>
    /// Data describing a single survey question.
    /// </summary>
    [Serializable]
    public struct SurveyQuestion {
        [SerializeField] private string prompt;
        [SerializeField] private string type;
        [SerializeField] private string[] responses;

        /// <summary>
        /// Prompt string to display to the user.
        /// </summary>
        public string Prompt {
            get { return prompt; }
        }

        /// <summary>
        /// Question type. Null/empty string uses default multiple choice.
        /// </summary>
        public string Type {
            get { return type; }
        }

        /// <summary>
        /// Response strings to display to the user.
        /// </summary>
        public string[] Responses {
            get { return responses; }
        }
    }

    /// <summary>
    /// Data for a single response.
    /// </summary>
    public struct SurveyQuestionResponse {
        public string Prompt;
        public string Response;
    }
}
