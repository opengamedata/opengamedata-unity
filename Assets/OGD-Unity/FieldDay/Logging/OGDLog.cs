using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace FieldDay {
    /// <summary>
    /// Handles communication with the OpenGameData server's logging features.
    /// Also handles communication with Firebase.
    /// </summary>
    public sealed partial class OGDLog : IDisposable {
        #region Consts

        private const int EventStreamMinimumSize = 4096;
        private const int EventStreamBufferPadding = 512;
        private const int EventStreamBufferInitialSize = 2048;
        private const int EventCustomParamsBufferSize = 512;

        static private readonly byte[] DataHeaderRawBytes = Encoding.UTF8.GetBytes("data=\"");
        static private readonly byte[] DataFooterRawBytes = Encoding.UTF8.GetBytes("\"");
        static private readonly int DataHeaderRawByteSize = DataHeaderRawBytes.Length;
        static private readonly int DataAdditionalByteCount = DataHeaderRawBytes.Length + DataFooterRawBytes.Length;

        #endregion // Consts

        [Flags]
        private enum StatusFlags {
            Initialized = 0x01,
            WritingEvent = 0x02,
            WritingEventCustomData = 0x04,
            Flushing = 0x08
        }

        /// <summary>
        /// Log behavior settings.
        /// </summary>
        [Flags]
        public enum SettingsFlags {
            Debug = 0x01,
            Base64Encode = 0x02,

            Default = Base64Encode
        }

        /// <summary>
        /// Identifiers for logging modules.
        /// </summary>
        public enum ModuleId {
            OpenGameData = 0,
            Firebase = 1,

            COUNT
        }

        /// <summary>
        /// Enum indicating the status of a module.
        /// </summary>
        private enum ModuleStatus {
            Uninitialized,
            Preparing,
            Ready,
            Error
        }

        // constants
        private OGDLogConsts m_OGDConsts;
        private FirebaseConsts m_FirebaseConsts;
        private SessionConsts m_SessionConsts;

        // state
        private Uri m_Endpoint;
        private uint m_EventSequence;
        private StatusFlags m_StatusFlags;
        private SettingsFlags m_Settings;
        private ModuleStatus[] m_ModuleStatus = new ModuleStatus[(int) ModuleId.COUNT];

        // dispatcher
        private FlushDispatcher m_FlushDispatcher;

        // data argument builder - this holds the event stream as a stringified json array, without the square brackets
        private readonly StringBuilder m_EventStream = new StringBuilder(EventStreamMinimumSize);
        private int m_SubmittedStreamLength;

        // custom event parameter json builder - this holds the custom event parameters
        private FixedCharBuffer m_EventCustomParamsBuffer;

        // submit buffers
        private char[] m_EventStreamEncodingChars = new char[EventStreamBufferInitialSize];
        private byte[] m_EventStreamEncodingBytes = new byte[EventStreamBufferInitialSize];
        private byte[] m_EventStreamEncodingEscaped = new byte[EventStreamBufferInitialSize];

        // static
        static private OGDLog s_Instance;

        /// <summary>
        /// Creates a new OpenGameData logger.
        /// </summary>
        public OGDLog() {
            m_SessionConsts.SessionId = OGDLogUtils.UUIDint();

            unsafe {
                m_EventCustomParamsBuffer = new FixedCharBuffer((char*) Marshal.AllocHGlobal(EventCustomParamsBufferSize * sizeof(char)), EventCustomParamsBufferSize);
            }

            m_Settings = SettingsFlags.Default;
            
            SetModuleStatus(ModuleId.OpenGameData, ModuleStatus.Preparing);
        }

        /// <summary>
        /// Creates a new OGDLog object.
        /// </summary>
        public OGDLog(string appId, int appVersion) : this() {
            Initialize(new OGDLogConsts() {
                AppId = appId,
                AppVersion = appVersion.ToString()
            });
        }

        /// <summary>
        /// Creates a new OGDLog object.
        /// </summary>
        public OGDLog(string appId, string appVersion) : this() {
            Initialize(new OGDLogConsts() {
                AppId = appId,
                AppVersion = appVersion
            });
        }

        /// <summary>
        /// Creates a new OGDLog object.
        /// </summary>
        public OGDLog(OGDLogConsts constants) : this() {
            Initialize(constants);
        }

        /// <summary>
        /// Cleans up resources.
        /// </summary>
        public void Dispose() {
            unsafe {
                if (m_EventCustomParamsBuffer.Base != null) {
                    Marshal.FreeHGlobal((IntPtr) m_EventCustomParamsBuffer.Base);
                    m_EventCustomParamsBuffer = default(FixedCharBuffer);
                }
            }

            if (m_FlushDispatcher) {
                GameObject.Destroy(m_FlushDispatcher.gameObject);
                m_FlushDispatcher = null;
            }

            if (s_Instance == this) {
                s_Instance = null;
            }
        }

        #region Configuration

        /// <summary>
        /// Set up the application constants.
        /// </summary>
        public void Initialize(OGDLogConsts constants) {
            if (s_Instance != null && s_Instance != this) {
                throw new InvalidOperationException("Cannot have multiple instances of OGDLog");
            }
            
            s_Instance = this;
            m_OGDConsts = constants;
            m_Endpoint = BuildOGDUrl(m_OGDConsts, m_SessionConsts);

            m_StatusFlags |= StatusFlags.Initialized;
            SetModuleStatus(ModuleId.OpenGameData, ModuleStatus.Ready);

            if (!m_FlushDispatcher) {
                GameObject hostGO = new GameObject("[OGDLog Dispatcher]");
                GameObject.DontDestroyOnLoad(hostGO);
                hostGO.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor;
                m_FlushDispatcher = hostGO.AddComponent<FlushDispatcher>();
                m_FlushDispatcher.Initialize(this);
            }

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_SetAppConsts(constants);
            }
        }

        /// <summary>
        /// Changes the UserId and UserData.
        /// This information is submitted with every event.
        /// </summary>
        public void SetUserId(string userId, string userData = null) {
            if (m_SessionConsts.UserId != userId || m_SessionConsts.UserData != userData) {
                m_SessionConsts.UserId = userId;
                m_SessionConsts.UserData = userData;
                m_Endpoint = BuildOGDUrl(m_OGDConsts, m_SessionConsts);

                if (ModuleReady(ModuleId.Firebase)) {
                    Firebase_SetSessionConsts(m_SessionConsts);
                }
            }
        }

        /// <summary>
        /// Sets the settings flags for the logger.
        /// This dictates debug output and base64 encoding.
        /// </summary>
        public void SetSettings(SettingsFlags settings) {
            m_Settings = settings;

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_ConfigureSettings(m_Settings);
            }
        }

        /// <summary>
        /// Sets the debug settings flag.
        /// </summary>
        public void SetDebug(bool debug) {
            if (debug) {
                SetSettings(m_Settings | SettingsFlags.Debug);
            } else {
                SetSettings(m_Settings & ~SettingsFlags.Debug);
            }
        }

        /// <summary>
        /// Indicates that this should also log to firebase.
        /// </summary>
        public void UseFirebase(FirebaseConsts constants) {
            Firebase_Prepare(constants, m_SessionConsts);
        }

        /// <summary>
        /// Indicates that this should also log to firebase.
        /// </summary>
        public void UseFirebase(string constantsJSON) {
            UseFirebase(JsonUtility.FromJson<FirebaseConsts>(constantsJSON));
        }

        /// <summary>
        /// Returns if this logger has the given module.
        /// </summary>
        [MethodImpl(256)]
        private bool ModuleReady(ModuleId module) {
            return m_ModuleStatus[(int) module] == ModuleStatus.Ready;
        }

        /// <summary>
        /// Gets the status of the given module.
        /// </summary>
        [MethodImpl(256)]
        private ModuleStatus GetModuleStatus(ModuleId module) {
            return m_ModuleStatus[(int) module];
        }

        /// <summary>
        /// Sets the status of the given module.
        /// </summary>
        [MethodImpl(256)]
        private void SetModuleStatus(ModuleId module, ModuleStatus status) {
            m_ModuleStatus[(int) module] = status;
        }

        #endregion // Configuration

        #region Events

        /// <summary>
        /// Begins logging an event with the given name.
        /// Provide arguments with EventParam calls.
        /// </summary>
        public void BeginEvent(string eventName) {
            if ((m_StatusFlags & StatusFlags.Initialized) == 0) {
                throw new InvalidOperationException("OGDLog must be initialized before any events are logged");
            }

            // finishes any pending event data
            FinishEventData();

            m_EventCustomParamsBuffer.Clear();

            DateTime nowTime = DateTime.UtcNow;
            TimeSpan clientOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);;
            uint eventSequenceIndex = m_EventSequence++;

            m_StatusFlags |= StatusFlags.WritingEvent;

            // if OpenGameData logging is enabled
            if (ModuleReady(ModuleId.OpenGameData)) {
                m_EventStream.Append('{');
                WriteEventParam("event_name", eventName);
                WriteEventParam("event_sequence_index", eventSequenceIndex);
                WriteEventParam("client_time", nowTime);
                WriteEventParam("client_offset", clientOffset);
            }

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_NewEvent(eventName, eventSequenceIndex);
            }

            BeginEventCustomParams();
        }

        /// <summary>
        /// Begins logging an event with the given name.
        /// This returns a disposable EventScope object
        /// that can accept event parameters. It will
        /// submit the event on dispose. Recommend to use
        /// with the `using` keyword
        /// </summary>
        public EventScope NewEvent(string eventName) {
            BeginEvent(eventName);
            return new EventScope(this);
        } 

        /// <summary>
        /// Writes a custom event string parameter.
        /// </summary>
        public void EventParam(string parameterName, string parameterValue) {
            if ((m_StatusFlags & StatusFlags.WritingEventCustomData) == 0) {
                throw new InvalidOperationException("No unsubmitted event to add an event parameter to");
            }

            if (ModuleReady(ModuleId.OpenGameData)) {
                m_EventCustomParamsBuffer.Write('"');
                m_EventCustomParamsBuffer.Write(parameterName);
                m_EventCustomParamsBuffer.Write("\":\"");
                OGDLogUtils.EscapeJSON(ref m_EventCustomParamsBuffer, parameterValue);
                m_EventCustomParamsBuffer.Write("\",");
            }

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_SetEventParam(parameterName, parameterValue);
            }
        }

        /// <summary>
        /// Writes a custom event string parameter.
        /// </summary>
        public void EventParam(string parameterName, StringBuilder parameterValue) {
            if ((m_StatusFlags & StatusFlags.WritingEventCustomData) == 0) {
                throw new InvalidOperationException("No unsubmitted event to add an event parameter to");
            }

            if (ModuleReady(ModuleId.OpenGameData)) {
                m_EventCustomParamsBuffer.Write('"');
                m_EventCustomParamsBuffer.Write(parameterName);
                m_EventCustomParamsBuffer.Write("\":\"");
                OGDLogUtils.EscapeJSON(ref m_EventCustomParamsBuffer, parameterValue);
                m_EventCustomParamsBuffer.Write("\",");
            }

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_SetEventParam(parameterName, parameterValue.ToString());
            }
        }

        /// <summary>
        /// Writes a custom event integer parameter.
        /// </summary>
        public void EventParam(string parameterName, long parameterValue) {
            if ((m_StatusFlags & StatusFlags.WritingEventCustomData) == 0) {
                throw new InvalidOperationException("No unsubmitted event to add an event parameter to");
            }

            if (ModuleReady(ModuleId.OpenGameData)) {
                m_EventCustomParamsBuffer.Write('"');
                m_EventCustomParamsBuffer.Write(parameterName);
                m_EventCustomParamsBuffer.Write("\":");
                m_EventCustomParamsBuffer.Write(parameterValue);
                m_EventCustomParamsBuffer.Write(',');
            }

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_SetEventParam(parameterName, parameterValue);
            }
        }

        /// <summary>
        /// Writes a custom event float parameter.
        /// </summary>
        public void EventParam(string parameterName, float parameterValue) {
            if ((m_StatusFlags & StatusFlags.WritingEventCustomData) == 0) {
                throw new InvalidOperationException("No unsubmitted event to add an event parameter to");
            }

            if (ModuleReady(ModuleId.OpenGameData)) {
                m_EventCustomParamsBuffer.Write('"');
                m_EventCustomParamsBuffer.Write(parameterName);
                m_EventCustomParamsBuffer.Write("\":");
                m_EventCustomParamsBuffer.Write(parameterValue);
                m_EventCustomParamsBuffer.Write(',');
            }

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_SetEventParam(parameterName, parameterValue);
            }
        }

        /// <summary>
        /// Writes a custom event boolean parameter.
        /// </summary>
        public void EventParam(string parameterName, bool parameterValue) {
             if ((m_StatusFlags & StatusFlags.WritingEventCustomData) == 0) {
                throw new InvalidOperationException("No unsubmitted event to add an event parameter to");
            }

            if (ModuleReady(ModuleId.OpenGameData)) {
                m_EventCustomParamsBuffer.Write('"');
                m_EventCustomParamsBuffer.Write(parameterName);
                m_EventCustomParamsBuffer.Write("\":");
                m_EventCustomParamsBuffer.Write(parameterValue);
                m_EventCustomParamsBuffer.Write(',');
            }

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_SetEventParam(parameterName, parameterValue ? 1 : 0);
            }
        }

        /// <summary>
        /// Submits the current event.
        /// </summary>
        public void SubmitEvent() {
            FinishEventData();
            m_FlushDispatcher.enabled = true;
        }

        /// <summary>
        /// Logs a new event.
        /// </summary>
        public void Log(LogEvent data) {
            BeginEvent(data.EventName);
            foreach(var kv in data.EventParameters) {
                EventParam(kv.Key, kv.Value);
            }
            SubmitEvent();
        }

        #region State

        /// <summary>
        /// Writes an event parameter.
        /// </summary>
        private void WriteEventParam(string parameterName, string value) {
            m_EventStream.Append('"').Append(parameterName).Append("\":\"")
                .EscapeJSON(value)
                .Append("\",");
        }

        /// <summary>
        /// Writes an event parameter.
        /// </summary>
        private void WriteEventParam(string parameterName, long value) {
            m_EventStream.Append('"').Append(parameterName).Append("\":").AppendInteger(value, 0).Append(',');
        }

        /// <summary>
        /// Writes an event parameter.
        /// </summary>
        private void WriteEventParam(string parameterName, DateTime value) {
            // format: yyyy-MM-dd HH:mm:ss.fffZ
            m_EventStream.Append('"').Append(parameterName).Append("\":\"")
                .AppendInteger(value.Year, 4).Append('-').AppendInteger(value.Month, 2).Append('-').AppendInteger(value.Day, 2)
                .Append(' ').AppendInteger(value.Hour, 2).Append(':').AppendInteger(value.Minute, 2).Append(':').AppendInteger(value.Second, 2)
                .Append('.').AppendInteger(value.Millisecond, 3).Append('Z')
                .Append("\",");
        }

        /// <summary>
        /// Writes an event parameter.
        /// </summary>
        private void WriteEventParam(string parameterName, TimeSpan value) {
            m_EventStream.Append('"').Append(parameterName).Append("\":\"")
                .AppendInteger(value.Hours, 2).Append(':').AppendInteger(value.Minutes, 2).Append(':').AppendInteger(value.Seconds, 2)
                .Append("\",");
        }

        /// <summary>
        /// Finishes any unclosed event data strings.
        /// </summary>
        private void FinishEventData() {
            EndEventCustomParams();

            if ((m_StatusFlags & StatusFlags.WritingEvent) != 0) {
                if (ModuleReady(ModuleId.OpenGameData)) {
                    OGDLogUtils.TrimEnd(m_EventStream, ',');
                    m_EventStream.Append("},");
                }
                if (ModuleReady(ModuleId.Firebase)) {
                    Firebase_SubmitEvent();
                }
                m_StatusFlags &= ~StatusFlags.WritingEvent;
            }

            Firebase_AttemptActivate();
        }

        /// <summary>
        /// Begins the custom parameters section of an event.
        /// </summary>
        private void BeginEventCustomParams() {
            if ((m_StatusFlags & StatusFlags.WritingEvent) == 0) {
                throw new InvalidOperationException("Cannot begin writing custom parameters without an unsubmitted event");
            }

            if ((m_StatusFlags & StatusFlags.WritingEventCustomData) == 0) {
                m_StatusFlags |= StatusFlags.WritingEventCustomData;
                m_EventCustomParamsBuffer.Clear();
                m_EventCustomParamsBuffer.Write('{');
            }
        }

        /// <summary>
        /// Ends the custom parameters section of an event and adds it to the event data.
        /// </summary>
        private void EndEventCustomParams() {
            if ((m_StatusFlags & StatusFlags.WritingEventCustomData) != 0) {
                if (ModuleReady(ModuleId.OpenGameData)) {
                    m_EventCustomParamsBuffer.TrimEnd(',');
                    m_EventCustomParamsBuffer.Write('}');
                    m_EventStream.Append("\"event_data\":\"")
                        .EscapeJSON(ref m_EventCustomParamsBuffer)
                        .Append("\",");
                    m_EventCustomParamsBuffer.Clear();
                }

                m_StatusFlags &= ~StatusFlags.WritingEventCustomData;
            }
        }

        #endregion // State

        #endregion // Events

        #region Flush

        /// <summary>
        /// Flushes all queued events to the server.
        /// </summary>
        public void Flush() {
            // if we're already flushing, or we don't have any events to flush, then ignore it
            if ((m_StatusFlags & StatusFlags.Flushing) != 0 || m_EventStream.Length <= 0) {
                return;
            }

            FinishEventData();

            m_StatusFlags |= StatusFlags.Flushing;
            m_SubmittedStreamLength = m_EventStream.Length;

            if (ModuleReady(ModuleId.OpenGameData)) {
                EnsureBufferSize((m_EventStream.Length * 4 / 3) + EventStreamBufferPadding); // with some padding to ensure encoding doesn't result in any buffer overflows

                // copy the current event stream into our buffer
                // since it's not enclosed with array brackets, we offset it by 1
                int eventStreamCharLength = m_EventStream.Length + 1;
                m_EventStream.CopyTo(0, m_EventStreamEncodingChars, 1, eventStreamCharLength - 1); // also it ends with a comma so we can ignore copying that
                m_EventStreamEncodingChars[0] = '[';
                m_EventStreamEncodingChars[eventStreamCharLength - 1] = ']';

                if ((m_Settings & SettingsFlags.Debug) != 0) {
                    UnityEngine.Debug.LogFormat("[OGDLog] Uploading event stream: {0}", new string(m_EventStreamEncodingChars, 0, eventStreamCharLength));
                }

                // encode data between buffers
                int dataByteLength = Encoding.UTF8.GetBytes(m_EventStreamEncodingChars, 0, eventStreamCharLength, m_EventStreamEncodingBytes, 0); // encode chars to bytes (UTF8)
                if ((m_Settings & SettingsFlags.Base64Encode) != 0) {
                    int base64Chars = Convert.ToBase64CharArray(m_EventStreamEncodingBytes, 0, dataByteLength, m_EventStreamEncodingChars, 0); // encode bytes to chars (base64)
                    dataByteLength = Encoding.UTF8.GetBytes(m_EventStreamEncodingChars, 0, base64Chars, m_EventStreamEncodingBytes, 0); // encode chars back to bytes (UTF8)
                }
                dataByteLength = OGDLogUtils.EscapePostData(m_EventStreamEncodingBytes, 0, dataByteLength, m_EventStreamEncodingEscaped, 0); // encode bytes to URI-escaped bytes

                // finally generate the actual post bytes
                byte[] encodedData = new byte[dataByteLength + DataAdditionalByteCount];
                OGDLogUtils.CopyArray(DataHeaderRawBytes, 0, DataHeaderRawByteSize, encodedData, 0); // copy header "data="
                OGDLogUtils.CopyArray(DataFooterRawBytes, 0, DataFooterRawBytes.Length, encodedData, DataHeaderRawByteSize + dataByteLength); // copy footer "
                OGDLogUtils.CopyArray(m_EventStreamEncodingEscaped, 0, dataByteLength, encodedData, DataHeaderRawByteSize); // copy escaped data

                UnityWebRequest request = new UnityWebRequest(m_Endpoint, UnityWebRequest.kHttpVerbPOST);
                if ((m_Settings & SettingsFlags.Debug) != 0) {
                    request.downloadHandler = new DownloadHandlerBuffer(); // we only need a download handler if we're in debug mode
                }
                request.uploadHandler = new UploadHandlerRaw(encodedData);
                request.uploadHandler.contentType = "application/x-www-form-urlencoded";

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                operation.completed += HandleOGDPostResponse;
            }
        }

        private void HandleOGDPostResponse(AsyncOperation op) {
            UnityWebRequest request = ((UnityWebRequestAsyncOperation) op).webRequest;
            string error = request.error;
            bool hadError = !string.IsNullOrEmpty(error);
            if (!hadError) {
                m_EventStream.Remove(0, m_SubmittedStreamLength); // if successful, basically remove the previously submitted data from the event stream
            }

            if ((m_Settings & SettingsFlags.Debug) != 0 && request.downloadHandler != null) {
                if (hadError) {
                    UnityEngine.Debug.LogWarningFormat("[OGDLog] Upload unsuccessful - error '{0}' with response code {1}", error, request.responseCode);
                } else {
                    UnityEngine.Debug.LogFormat("[OGDLog] Upload successful with response code {0} and response '{1}'", request.responseCode, request.downloadHandler.text);
                }
            }

            m_SubmittedStreamLength = 0;
            m_StatusFlags &= ~StatusFlags.Flushing;

            // if we still have events to submit, let's flush again
            if (m_EventStream.Length > 0) {
                Flush();
            }
        }

        private void EnsureBufferSize(int bufferSize) {
            if (m_EventStreamEncodingChars.Length >= bufferSize) {
                return;
            }

            int newBufferSize = (int) OGDLogUtils.AlignUp((uint) bufferSize, 512u);
            Array.Resize(ref m_EventStreamEncodingChars, newBufferSize);
            Array.Resize(ref m_EventStreamEncodingBytes, newBufferSize);
            Array.Resize(ref m_EventStreamEncodingEscaped, newBufferSize);
        }

        private class FlushDispatcher : MonoBehaviour {
            private OGDLog m_Logger;

            public void Initialize(OGDLog log) {
                m_Logger = log;
                enabled = false;
            }

            private void OnDestroy() {
                m_Logger = null;
            }

            private void LateUpdate() {
                m_Logger.Flush();
                enabled = false;
            }
        }

        #endregion // Flush

        #region String Assembly

        static private unsafe Uri BuildOGDUrl(OGDLogConsts ogdConsts, SessionConsts session) {
            char* buffer = stackalloc char[512];
            FixedCharBuffer charBuff = new FixedCharBuffer(buffer, 512);
            
            charBuff.Write(OGDLogConsts.LogEndpoint);
            charBuff.Write("?app_id=");
            charBuff.Write(Uri.EscapeDataString(ogdConsts.AppId.ToUpperInvariant()));
            charBuff.Write("&log_version=");
            charBuff.Write(ogdConsts.ClientLogVersion);
            charBuff.Write("&app_version=");
            charBuff.Write(Uri.EscapeDataString(ogdConsts.AppVersion));
            charBuff.Write("&session_id=");
            charBuff.Write(session.SessionId);
            if (!string.IsNullOrEmpty(ogdConsts.AppBranch)) {
                charBuff.Write("&app_branch=");
                charBuff.Write(Uri.EscapeDataString(ogdConsts.AppBranch));
            }
            if (!string.IsNullOrEmpty(session.UserId)) {
                charBuff.Write("&user_id=");
                charBuff.Write(Uri.EscapeDataString(session.UserId));
            }
            if (!string.IsNullOrEmpty(session.UserData)) {
                charBuff.Write("&user_data=");
                charBuff.Write(Uri.EscapeDataString(session.UserData));
            }

            string uriString = charBuff.ToString();
            return new Uri(uriString);
        }

        #endregion // String Assembly
    }

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
        public void Param(string paramName, float paramValue) {
            m_Logger.EventParam(paramName, paramValue);
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
    /// Deprecated.
    /// </summary>
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