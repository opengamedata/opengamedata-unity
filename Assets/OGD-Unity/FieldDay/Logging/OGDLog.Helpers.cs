using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace OGD {
    /// <summary>
    /// Event logging helper object.
    /// This automatically submits the current event
    /// when disposed.
    /// </summary>
    public struct EventScope : IDisposable {
        private OGDLog m_Logger;

        internal EventScope(OGDLog logger) {
            m_Logger = logger;
        }

        /// <summary>
        /// Appends a parameter with a string value.
        /// </summary>
        public void Param(string paramName, string paramValue) {
            m_Logger.EventParam(paramName, paramValue);
        }

        /// <summary>
        /// Appends a parameter with a string value.
        /// </summary>
        public void Param(string paramName, StringBuilder paramValue) {
            m_Logger.EventParam(paramName, paramValue);
        }

        /// <summary>
        /// Appends a parameter with an integer value.
        /// </summary>
        public void Param(string paramName, long paramValue) {
            m_Logger.EventParam(paramName, paramValue);
        }

        /// <summary>
        /// Appends a parameter with a floating point value.
        /// </summary>
        public void Param(string paramName, float paramValue, int precision = 3) {
            m_Logger.EventParam(paramName, paramValue, precision);
        }

        /// <summary>
        /// Appends a parameter with a boolean value.
        /// </summary>
        public void Param(string paramName, bool paramValue) {
            m_Logger.EventParam(paramName, paramValue);
        }

        public void Dispose() {
            if (m_Logger != null) {
                m_Logger.SubmitEvent();
                m_Logger = null;
            }
        }
    }

    /// <summary>
    /// Game state helper object.
    /// This automatically submits the game state
    /// when disposed.
    /// </summary>
    public struct GameStateScope : IDisposable {
        private OGDLog m_Logger;

        internal GameStateScope(OGDLog logger) {
            m_Logger = logger;
        }

        /// <summary>
        /// Appends a parameter with a string value.
        /// </summary>
        public void Param(string paramName, string paramValue) {
            m_Logger.GameStateParam(paramName, paramValue);
        }

        /// <summary>
        /// Appends a parameter with a string value.
        /// </summary>
        public void Param(string paramName, StringBuilder paramValue) {
            m_Logger.GameStateParam(paramName, paramValue);
        }

        /// <summary>
        /// Appends a parameter with an integer value.
        /// </summary>
        public void Param(string paramName, long paramValue) {
            m_Logger.GameStateParam(paramName, paramValue);
        }

        /// <summary>
        /// Appends a parameter with a floating point value.
        /// </summary>
        public void Param(string paramName, float paramValue, int precision = 3) {
            m_Logger.GameStateParam(paramName, paramValue, precision);
        }

        /// <summary>
        /// Appends a parameter with a boolean value.
        /// </summary>
        public void Param(string paramName, bool paramValue) {
            m_Logger.GameStateParam(paramName, paramValue);
        }

        public void Dispose() {
            if (m_Logger != null) {
                m_Logger.SubmitGameState();
                m_Logger = null;
            }
        }
    }

    /// <summary>
    /// User data helper object.
    /// This automatically submits the user data
    /// when disposed.
    /// </summary>
    public struct UserDataScope : IDisposable {
        private OGDLog m_Logger;

        internal UserDataScope(OGDLog logger) {
            m_Logger = logger;
        }

        /// <summary>
        /// Appends a parameter with a string value.
        /// </summary>
        public void Param(string paramName, string paramValue) {
            m_Logger.UserDataParam(paramName, paramValue);
        }

        /// <summary>
        /// Appends a parameter with a string value.
        /// </summary>
        public void Param(string paramName, StringBuilder paramValue) {
            m_Logger.UserDataParam(paramName, paramValue);
        }

        /// <summary>
        /// Appends a parameter with an integer value.
        /// </summary>
        public void Param(string paramName, long paramValue) {
            m_Logger.UserDataParam(paramName, paramValue);
        }

        /// <summary>
        /// Appends a parameter with a floating point value.
        /// </summary>
        public void Param(string paramName, float paramValue, int precision = 3) {
            m_Logger.UserDataParam(paramName, paramValue, precision);
        }

        /// <summary>
        /// Appends a parameter with a boolean value.
        /// </summary>
        public void Param(string paramName, bool paramValue) {
            m_Logger.UserDataParam(paramName, paramValue);
        }

        public void Dispose() {
            if (m_Logger != null) {
                m_Logger.SubmitUserData();
                m_Logger = null;
            }
        }
    }

    /// <summary>
    /// Deprecated.
    /// </summary>
    [Obsolete("LogEvent is obsolete. Recommend that you use NewEvent", false)]
    public struct LogEvent {
        public string EventName;
        public Dictionary<string, string> EventParameters;

        public LogEvent(Dictionary<string, string> data, Enum category) {
            EventName = category.ToString();
            EventParameters = data;
        }

        public LogEvent(Dictionary<string, string> data, string category) {
            EventName = category;
            EventParameters = data;
        }
    }
}