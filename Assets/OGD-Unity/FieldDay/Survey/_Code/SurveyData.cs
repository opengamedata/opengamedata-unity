using System;
using System.Collections.Generic;
using UnityEngine;

namespace FieldDay
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
