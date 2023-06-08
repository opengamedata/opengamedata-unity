#if UNITY_2021_1_OR_NEWER
#define HAS_UPLOAD_NATIVE_ARRAY
#endif // UNITY_2021_OR_NEWER

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
#if HAS_UPLOAD_NATIVE_ARRAY
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
#endif // HAS_UPLOAD_NATIVE_ARRAY

namespace FieldDay {
    /// <summary>
    /// Handles communication with the OpenGameData server's logging features.
    /// Also handles communication with Firebase.
    /// </summary>
    public sealed partial class OGDLog : IDisposable {
        #region Consts

        private const int EventStreamMinimumSize = 4096;
        private const int EventStreamBufferPadding = 512;
        private const int EventStreamBufferInitialSize = 4096;
        private const int EventCustomParamsBufferSize = 4096;
        private const int AdditionalStateBufferSize = 2048;

        static private readonly byte[] DataHeaderRawBytes = Encoding.UTF8.GetBytes("data=\"");
        static private readonly byte[] DataFooterRawBytes = Encoding.UTF8.GetBytes("\"");
        static private readonly int DataHeaderRawByteSize = DataHeaderRawBytes.Length;
        static private readonly int DataAdditionalByteCount = DataHeaderRawBytes.Length + DataFooterRawBytes.Length;

        #endregion // Consts

        /// <summary>
        /// Memory usage configuration.
        /// </summary>
        [Serializable]
        public struct MemoryConfig {
            public int EventParameterBufferSize;
            public int GameStateBufferSize;
            public int PlayerDataBufferSize;

            public MemoryConfig(int eventParamBufferSize, int additionalBufferSize) {
                EventParameterBufferSize = eventParamBufferSize;
                GameStateBufferSize = PlayerDataBufferSize = additionalBufferSize;
            }

            public MemoryConfig(int eventParamBufferSize, int gameStateBufferSize, int playerDataBufferSize) {
                EventParameterBufferSize = eventParamBufferSize;
                GameStateBufferSize = gameStateBufferSize;
                PlayerDataBufferSize = playerDataBufferSize;
            }

            /// <summary>
            /// Default buffer size configuration.
            /// </summary>
            static public readonly MemoryConfig Default = new MemoryConfig() {
                EventParameterBufferSize = EventCustomParamsBufferSize,
                GameStateBufferSize = AdditionalStateBufferSize,
                PlayerDataBufferSize = AdditionalStateBufferSize
            };
        }

        [Flags]
        private enum StatusFlags {
            Initialized = 0x01,
            WritingEvent = 0x02,
            WritingEventCustomData = 0x04,
            Flushing = 0x08,
            WritingUserData = 0x10,
            WritingGameState = 0x20,
            Disposed = 0x40
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
        private unsafe char* m_DataBufferHead;
        private FixedCharBuffer m_EventCustomParamsBuffer;
        private FixedCharBuffer m_UserDataParamsBuffer;
        private FixedCharBuffer m_GameStateParamsBuffer;

        // submit buffers
        private char[] m_EventStreamEncodingChars = new char[EventStreamBufferInitialSize];
        private byte[] m_EventStreamEncodingBytes = new byte[EventStreamBufferInitialSize];
        private byte[] m_EventStreamEncodingEscaped = new byte[EventStreamBufferInitialSize];
        #if HAS_UPLOAD_NATIVE_ARRAY
        private unsafe byte* m_SubmitBufferHead;
        #endif // HAS_UPLOAD_NATIVE_ARRAY

        // static
        static private OGDLog s_Instance;

        /// <summary>
        /// Creates a new OpenGameData logger.
        /// </summary>
        public OGDLog()
            :this(EventCustomParamsBufferSize, AdditionalStateBufferSize, AdditionalStateBufferSize)
        { }

        /// <summary>
        /// Creates a new OpenGameData logger, specifying the size of the parameter buffers.
        /// </summary>
        /// <param name="eventParamsBufferSize">Size of the event custom parameters buffer, in bytes.</param>
        /// <param name="gameStateParamsBufferSize">Size of the game_state buffer, in bytes.</param>
        /// <param name="playerDataParamsBufferSize">Size of the game_state buffer, in bytes.</param>
        public OGDLog(int eventParamsBufferSize, int gameStateParamsBufferSize, int playerDataParamsBufferSize) {
            if (eventParamsBufferSize < 1024) {
                throw new ArgumentException("Event parameter buffer must be at least 1k", "eventParamsBufferSize");
            }
            if (gameStateParamsBufferSize < 256) {
                throw new ArgumentException("Game state parameter buffer must be at least 256b", "gameStateParamsBufferSize");
            }
            if (playerDataParamsBufferSize < 256) {
                throw new ArgumentException("Player data parameter buffer must be at least 256b", "playerDataParamsBufferSize");
            }
            m_SessionConsts.SessionId = OGDLogUtils.UUIDint();

            unsafe {
                m_DataBufferHead = (char*) Marshal.AllocHGlobal((eventParamsBufferSize + gameStateParamsBufferSize + playerDataParamsBufferSize) * sizeof(char));
                m_EventCustomParamsBuffer = new FixedCharBuffer("event_data", m_DataBufferHead, eventParamsBufferSize);
                m_UserDataParamsBuffer = new FixedCharBuffer("player_data", m_EventCustomParamsBuffer.Tail, playerDataParamsBufferSize);
                m_GameStateParamsBuffer = new FixedCharBuffer("game_state", m_UserDataParamsBuffer.Tail, gameStateParamsBufferSize);

                #if HAS_UPLOAD_NATIVE_ARRAY
                m_SubmitBufferHead = (byte*) Marshal.AllocHGlobal(EventStreamBufferInitialSize);
                #endif // HAS_UPLOAD_NATIVE_ARRAY
            }

            m_Settings = SettingsFlags.Default;
            
            SetModuleStatus(ModuleId.OpenGameData, ModuleStatus.Preparing);
        }

        /// <summary>
        /// Creates a new OpenGameData logger, specifying the size of the parameter buffers.
        /// </summary>
        public OGDLog(MemoryConfig memConfig)
            : this(memConfig.EventParameterBufferSize, memConfig.GameStateBufferSize, memConfig.PlayerDataBufferSize)
            {  }

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
        public OGDLog(string appId, int appVersion, MemoryConfig memConfig) : this(memConfig) {
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
        public OGDLog(string appId, string appVersion, MemoryConfig memConfig) : this(memConfig) {
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
        /// Creates a new OGDLog object.
        /// </summary>
        public OGDLog(OGDLogConsts constants, MemoryConfig memConfig) : this(memConfig) {
            Initialize(constants);
        }

        /// <summary>
        /// Returns if the logger is fully ready to log events.
        /// </summary>
        public bool IsReady() {
            for(int i = 0; i < m_ModuleStatus.Length; i++) {
                if (m_ModuleStatus[i] == ModuleStatus.Preparing) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Cleans up resources.
        /// </summary>
        public void Dispose() {
            unsafe {
                if (m_DataBufferHead != null) {
                    Marshal.FreeHGlobal((IntPtr) m_DataBufferHead);
                    m_EventCustomParamsBuffer = default(FixedCharBuffer);
                    m_GameStateParamsBuffer = default(FixedCharBuffer);
                    m_UserDataParamsBuffer = default(FixedCharBuffer);
                }

                #if HAS_UPLOAD_NATIVE_ARRAY
                if (m_SubmitBufferHead != null) {
                    Marshal.FreeHGlobal((IntPtr) m_SubmitBufferHead);
                }
                #endif // HAS_UPLOAD_NATIVE_ARRAY
            }

            if (m_FlushDispatcher) {
                GameObject.Destroy(m_FlushDispatcher.gameObject);
                m_FlushDispatcher = null;
            }

            if (s_Instance == this) {
                s_Instance = null;
            }

            m_StatusFlags |= StatusFlags.Disposed;
            m_StatusFlags &= ~StatusFlags.Initialized;
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
        /// Changes the UserId.
        /// This information is submitted with every event.
        /// </summary>
        public void SetUserId(string userId) {
            if (m_SessionConsts.UserId != userId) {
                m_SessionConsts.UserId = userId;
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
        /// Resets the session id to a new session.
        /// This will also reset the event sequence index.
        /// </summary>
        public void ResetSessionId() {
            m_SessionConsts.SessionId = OGDLogUtils.UUIDint();

            // since we can't manually reset firebase session,
            // we should at least make sure event sequence indices will not overlap
            s_FirebaseEventSequenceOffset += m_EventSequence;
            m_EventSequence = 0;
            
            m_Endpoint = BuildOGDUrl(m_OGDConsts, m_SessionConsts);
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
                WriteStream(m_EventStream, "event_name", eventName);
                WriteStream(m_EventStream, "event_sequence_index", eventSequenceIndex);
                WriteStream(m_EventStream, "client_time", nowTime);
                WriteStream(m_EventStream, "client_offset", clientOffset);
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
        /// Logs an event with no event_data json.
        /// </summary>
        public void Log(string eventName) {
            Log(eventName, "{}");
        }

        /// <summary>
        /// Logs an event with the given custom event_data json object.
        /// </summary>
        public void Log(string eventName, string eventJSON) {
            BeginEvent(eventName);
            EndEventCustomParamsFromString(eventJSON);
            SubmitEvent();
        }

        /// <summary>
        /// Logs an event with the given custom event_data json object.
        /// </summary>
        public void Log(string eventName, StringBuilder eventJSON) {
            BeginEvent(eventName);
            EndEventCustomParamsFromString(eventJSON);
            SubmitEvent();
        }

        /// <summary>
        /// Writes a custom event string parameter.
        /// </summary>
        public void EventParam(string parameterName, string parameterValue) {
            if ((m_StatusFlags & StatusFlags.WritingEventCustomData) == 0) {
                throw new InvalidOperationException("No unsubmitted event to add an event parameter to");
            }

            if (ModuleReady(ModuleId.OpenGameData)) {
                WriteBuffer(ref m_EventCustomParamsBuffer, parameterName, parameterValue);
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
                WriteBuffer(ref m_EventCustomParamsBuffer, parameterName, parameterValue);
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
                WriteBuffer(ref m_EventCustomParamsBuffer, parameterName, parameterValue);
            }

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_SetEventParam(parameterName, parameterValue);
            }
        }

        /// <summary>
        /// Writes a custom event float parameter.
        /// </summary>
        public void EventParam(string parameterName, double parameterValue, int precision = 3) {
            if ((m_StatusFlags & StatusFlags.WritingEventCustomData) == 0) {
                throw new InvalidOperationException("No unsubmitted event to add an event parameter to");
            }

            if (ModuleReady(ModuleId.OpenGameData)) {
                WriteBuffer(ref m_EventCustomParamsBuffer, parameterName, parameterValue, precision);
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
                WriteBuffer(ref m_EventCustomParamsBuffer, parameterName, parameterValue);
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
        [Obsolete("LogEvent is obsolete. Recommend that you use NewEvent", false)]
        public void Log(LogEvent data) {
            BeginEvent(data.EventName);
            foreach(var kv in data.EventParameters) {
                EventParam(kv.Key, kv.Value);
            }
            SubmitEvent();
        }

        #region State

        /// <summary>
        /// Finishes any unclosed event data strings.
        /// </summary>
        private void FinishEventData() {
            EndEventCustomParams();

            if ((m_StatusFlags & StatusFlags.WritingEvent) != 0) {
                if (ModuleReady(ModuleId.OpenGameData)) {
                    
                    // if we're not currently writing user data, and we have user data, then write it here
                    if ((m_StatusFlags & StatusFlags.WritingUserData) == 0 && m_UserDataParamsBuffer.Length > 0) {
                        WriteStream(m_EventStream, "user_data", ref m_UserDataParamsBuffer, false);
                    }

                    // similar deal, except for writing game state
                    if ((m_StatusFlags & StatusFlags.WritingGameState) == 0 && m_GameStateParamsBuffer.Length > 0) {
                        WriteStream(m_EventStream, "game_state", ref m_GameStateParamsBuffer, false);
                    }
                    
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
                BeginBuffer(ref m_EventCustomParamsBuffer);
            }
        }

        /// <summary>
        /// Ends the custom parameters section of an event and adds it to the event data.
        /// </summary>
        private void EndEventCustomParams() {
            if ((m_StatusFlags & StatusFlags.WritingEventCustomData) != 0) {
                if (ModuleReady(ModuleId.OpenGameData)) {
                    EndBuffer(ref m_EventCustomParamsBuffer, false);
                    WriteStream(m_EventStream, "event_data", ref m_EventCustomParamsBuffer, true);
                    m_EventCustomParamsBuffer.Clear();
                }

                m_StatusFlags &= ~StatusFlags.WritingEventCustomData;
            }
        }

        /// <summary>
        /// Ends the custom parameters section of an event and adds the given json string to the event data.
        /// </summary>
        private void EndEventCustomParamsFromString(string json) {
            if ((m_StatusFlags & StatusFlags.WritingEventCustomData) != 0) {
                if (ModuleReady(ModuleId.OpenGameData)) {
                    WriteStream(m_EventStream, "event_data", json);
                    m_EventCustomParamsBuffer.Clear();
                }

                m_StatusFlags &= ~StatusFlags.WritingEventCustomData;
            }
        }

        /// <summary>
        /// Ends the custom parameters section of an event and adds the given json string to the event data.
        /// </summary>
        private void EndEventCustomParamsFromString(StringBuilder json) {
            if ((m_StatusFlags & StatusFlags.WritingEventCustomData) != 0) {
                if (ModuleReady(ModuleId.OpenGameData)) {
                    WriteStream(m_EventStream, "event_data", json);
                    m_EventCustomParamsBuffer.Clear();
                }

                m_StatusFlags &= ~StatusFlags.WritingEventCustomData;
            }
        }

        #endregion // State

        #endregion // Events

        #region Game State

        /// <summary>
        /// Begins writing the shared game state event parameter.
        /// </summary>
        public void BeginGameState() {
            if ((m_StatusFlags & StatusFlags.WritingGameState) != 0) {
                throw new InvalidOperationException("Game State already open for writing");
            }

            m_StatusFlags |= StatusFlags.WritingGameState;
            BeginBuffer(ref m_GameStateParamsBuffer);

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_ResetGameState();
            }
        }

        /// <summary>
        /// Begins setting shared game state.
        /// This returns a disposable GameStateScope object
        /// that can accept parameters. It will
        /// submit the game state on dispose. Recommend to use
        /// with the `using` keyword
        /// </summary>
        public GameStateScope OpenGameState() {
            BeginGameState();
            return new GameStateScope(this);
        }

        /// <summary>
        /// Writes a custom game state string parameter.
        /// </summary>
        public void GameStateParam(string parameterName, string parameterValue) {
            if ((m_StatusFlags & StatusFlags.WritingGameState) == 0) {
                throw new InvalidOperationException("Game State not open for writing");
            }

            if (ModuleReady(ModuleId.OpenGameData)) {
                WriteBuffer(ref m_GameStateParamsBuffer, parameterName, parameterValue);
            }

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_SetGameStateParam(parameterName, parameterValue);
            }
        }

        /// <summary>
        /// Writes a custom game state string parameter.
        /// </summary>
        public void GameStateParam(string parameterName, StringBuilder parameterValue) {
            if ((m_StatusFlags & StatusFlags.WritingGameState) == 0) {
                throw new InvalidOperationException("Game State not open for writing");
            }

            if (ModuleReady(ModuleId.OpenGameData)) {
                WriteBuffer(ref m_GameStateParamsBuffer, parameterName, parameterValue);
            }

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_SetGameStateParam(parameterName, parameterValue.ToString());
            }
        }

        /// <summary>
        /// Writes a custom game state integer parameter.
        /// </summary>
        public void GameStateParam(string parameterName, long parameterValue) {
            if ((m_StatusFlags & StatusFlags.WritingGameState) == 0) {
                throw new InvalidOperationException("Game State not open for writing");
            }

            if (ModuleReady(ModuleId.OpenGameData)) {
                WriteBuffer(ref m_GameStateParamsBuffer, parameterName, parameterValue);
            }

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_SetGameStateParam(parameterName, parameterValue);
            }
        }

        /// <summary>
        /// Writes a custom game state float parameter.
        /// </summary>
        public void GameStateParam(string parameterName, double parameterValue, int precision = 3) {
            if ((m_StatusFlags & StatusFlags.WritingGameState) == 0) {
                throw new InvalidOperationException("Game State not open for writing");
            }

            if (ModuleReady(ModuleId.OpenGameData)) {
                WriteBuffer(ref m_GameStateParamsBuffer, parameterName, parameterValue, precision);
            }

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_SetGameStateParam(parameterName, parameterValue);
            }
        }

        /// <summary>
        /// Writes a custom game state boolean parameter.
        /// </summary>
        public void GameStateParam(string parameterName, bool parameterValue) {
            if ((m_StatusFlags & StatusFlags.WritingGameState) == 0) {
                throw new InvalidOperationException("Game State not open for writing");
            }

            if (ModuleReady(ModuleId.OpenGameData)) {
                WriteBuffer(ref m_GameStateParamsBuffer, parameterName, parameterValue);
            }

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_SetGameStateParam(parameterName, parameterValue ? 1 : 0);
            }
        }

        /// <summary>
        /// Submits game state changes.
        /// </summary>
        public void SubmitGameState() {
            if ((m_StatusFlags & StatusFlags.WritingGameState) != 0) {
                EndBuffer(ref m_GameStateParamsBuffer, true);
                m_StatusFlags &= ~StatusFlags.WritingGameState;
            }
        }

        #endregion // Game State

        #region User Data

        /// <summary>
        /// Begins writing the shared user data event parameter.
        /// </summary>
        public void BeginUserData() {
            if ((m_StatusFlags & StatusFlags.WritingUserData) != 0) {
                throw new InvalidOperationException("User Data already open for writing");
            }

            m_StatusFlags |= StatusFlags.WritingUserData;
            BeginBuffer(ref m_UserDataParamsBuffer);

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_ResetUserData();
            }
        }

        /// <summary>
        /// Begins setting shared user data.
        /// This returns a disposable UserDataScope object
        /// that can accept parameters. It will
        /// submit the user data on dispose. Recommend to use
        /// with the `using` keyword
        /// </summary>
        public UserDataScope OpenUserData() {
            BeginUserData();
            return new UserDataScope(this);
        }

        /// <summary>
        /// Writes a custom user data string parameter.
        /// </summary>
        public void UserDataParam(string parameterName, string parameterValue) {
            if ((m_StatusFlags & StatusFlags.WritingUserData) == 0) {
                throw new InvalidOperationException("User Data not open for writing");
            }

            if (ModuleReady(ModuleId.OpenGameData)) {
                WriteBuffer(ref m_UserDataParamsBuffer, parameterName, parameterValue);
            }

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_SetUserDataParam(parameterName, parameterValue);
            }
        }

        /// <summary>
        /// Writes a custom user data string parameter.
        /// </summary>
        public void UserDataParam(string parameterName, StringBuilder parameterValue) {
            if ((m_StatusFlags & StatusFlags.WritingUserData) == 0) {
                throw new InvalidOperationException("User Data not open for writing");
            }

            if (ModuleReady(ModuleId.OpenGameData)) {
                WriteBuffer(ref m_UserDataParamsBuffer, parameterName, parameterValue);
            }

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_SetUserDataParam(parameterName, parameterValue.ToString());
            }
        }

        /// <summary>
        /// Writes a custom user data integer parameter.
        /// </summary>
        public void UserDataParam(string parameterName, long parameterValue) {
            if ((m_StatusFlags & StatusFlags.WritingUserData) == 0) {
                throw new InvalidOperationException("User Data not open for writing");
            }

            if (ModuleReady(ModuleId.OpenGameData)) {
                WriteBuffer(ref m_UserDataParamsBuffer, parameterName, parameterValue);
            }

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_SetUserDataParam(parameterName, parameterValue);
            }
        }

        /// <summary>
        /// Writes a custom user data float parameter.
        /// </summary>
        public void UserDataParam(string parameterName, double parameterValue, int precision = 3) {
            if ((m_StatusFlags & StatusFlags.WritingUserData) == 0) {
                throw new InvalidOperationException("User Data not open for writing");
            }

            if (ModuleReady(ModuleId.OpenGameData)) {
                WriteBuffer(ref m_UserDataParamsBuffer, parameterName, parameterValue, precision);
            }

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_SetUserDataParam(parameterName, parameterValue);
            }
        }

        /// <summary>
        /// Writes a custom user data boolean parameter.
        /// </summary>
        public void UserDataParam(string parameterName, bool parameterValue) {
            if ((m_StatusFlags & StatusFlags.WritingUserData) == 0) {
                throw new InvalidOperationException("User Data not open for writing");
            }

            if (ModuleReady(ModuleId.OpenGameData)) {
                WriteBuffer(ref m_UserDataParamsBuffer, parameterName, parameterValue);
            }

            if (ModuleReady(ModuleId.Firebase)) {
                Firebase_SetUserDataParam(parameterName, parameterValue ? 1 : 0);
            }
        }

        /// <summary>
        /// Submits user data changes.
        /// </summary>
        public void SubmitUserData() {
            if ((m_StatusFlags & StatusFlags.WritingUserData) != 0) {
                EndBuffer(ref m_UserDataParamsBuffer, true);

                m_StatusFlags &= ~StatusFlags.WritingUserData;
            }
        }

        #endregion // User Data

        #region Flush

        /// <summary>
        /// Flushes all queued events to the server.
        /// </summary>
        public void Flush() {
            // if we're already flushing, or we don't have any events to flush, or we've been disposed, then ignore it
            if ((m_StatusFlags & StatusFlags.Flushing) != 0 || m_EventStream.Length <= 0 || (m_StatusFlags & StatusFlags.Disposed) != 0) {
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
                UploadHandlerRaw uploadHandler;
                #if HAS_UPLOAD_NATIVE_ARRAY
                unsafe {
                    int totalUploadByteLength = dataByteLength + DataAdditionalByteCount;
                    NativeArray<byte> encodedData = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(m_SubmitBufferHead, totalUploadByteLength, Allocator.None);
                    OGDLogUtils.CopyArray(DataHeaderRawBytes, 0, DataHeaderRawByteSize, m_SubmitBufferHead, 0, totalUploadByteLength); // copy header "data="
                    OGDLogUtils.CopyArray(DataFooterRawBytes, 0, DataFooterRawBytes.Length, m_SubmitBufferHead, DataHeaderRawByteSize + dataByteLength, totalUploadByteLength); // copy footer "
                    OGDLogUtils.CopyArray(m_EventStreamEncodingEscaped, 0, dataByteLength, m_SubmitBufferHead, DataHeaderRawByteSize, totalUploadByteLength); // copy escaped data
                    #if ENABLE_UNITY_COLLECTIONS_CHECKS
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref encodedData, AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
                    #endif
                    uploadHandler = new UploadHandlerRaw(encodedData, false);
                }
                #else
                byte[] encodedData = new byte[dataByteLength + DataAdditionalByteCount];
                OGDLogUtils.CopyArray(DataHeaderRawBytes, 0, DataHeaderRawByteSize, encodedData, 0); // copy header "data="
                OGDLogUtils.CopyArray(DataFooterRawBytes, 0, DataFooterRawBytes.Length, encodedData, DataHeaderRawByteSize + dataByteLength); // copy footer "
                OGDLogUtils.CopyArray(m_EventStreamEncodingEscaped, 0, dataByteLength, encodedData, DataHeaderRawByteSize); // copy escaped data
                uploadHandler = new UploadHandlerRaw(encodedData);
                #endif // HAS_UPLOAD_NATIVE_ARRAY

                UnityWebRequest request = new UnityWebRequest(m_Endpoint, UnityWebRequest.kHttpVerbPOST);
                if ((m_Settings & SettingsFlags.Debug) != 0) {
                    request.downloadHandler = new DownloadHandlerBuffer(); // we only need a download handler if we're in debug mode
                }
                request.uploadHandler = uploadHandler;
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
            if ((m_Settings & SettingsFlags.Debug) != 0) {
                UnityEngine.Debug.LogFormat("[OGDLog] Resizing internal stream encoding and upload buffers to {0} to accommodate size {1}", newBufferSize, bufferSize);
            }

            Array.Resize(ref m_EventStreamEncodingChars, newBufferSize);
            Array.Resize(ref m_EventStreamEncodingBytes, newBufferSize);
            Array.Resize(ref m_EventStreamEncodingEscaped, newBufferSize);

            #if HAS_UPLOAD_NATIVE_ARRAY
            unsafe {
                m_SubmitBufferHead = (byte*) Marshal.ReAllocHGlobal((IntPtr) m_SubmitBufferHead, (IntPtr) newBufferSize);
            }
            #endif // HAS_UPLOAD_NATIVE_ARRAY
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
            FixedCharBuffer charBuff = new FixedCharBuffer("url", buffer, 512);
            
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

            string uriString = charBuff.ToString();
            return new Uri(uriString);
        }

        // BUFFERS

        static private void BeginBuffer(ref FixedCharBuffer buffer) {
            buffer.Clear();
            buffer.Write('{');
        }

        static private void EndBuffer(ref FixedCharBuffer buffer, bool escape) {
            buffer.TrimEnd(',');
            buffer.Write('}');
            if (escape) {
                OGDLogUtils.EscapeJSONInline(ref buffer);
            }
        }

        static private void WriteBuffer(ref FixedCharBuffer buffer, string parameterName, string parameterValue) {
            buffer.Write('"');
            buffer.Write(parameterName);
            buffer.Write("\":\"");
            OGDLogUtils.EscapeJSON(ref buffer, parameterValue);
            buffer.Write("\",");
        }

        static private void WriteBuffer(ref FixedCharBuffer buffer, string parameterName, bool parameterValue) {
            buffer.Write('"');
            buffer.Write(parameterName);
            buffer.Write("\":");
            buffer.Write(parameterValue);
            buffer.Write(',');
        }

        static private void WriteBuffer(ref FixedCharBuffer buffer, string parameterName, StringBuilder parameterValue) {
            buffer.Write('"');
            buffer.Write(parameterName);
            buffer.Write("\":\"");
            OGDLogUtils.EscapeJSON(ref buffer, parameterValue);
            buffer.Write("\",");
        }

        static private void WriteBuffer(ref FixedCharBuffer buffer, string parameterName, long parameterValue) {
            buffer.Write('"');
            buffer.Write(parameterName);
            buffer.Write("\":");
            buffer.Write(parameterValue);
            buffer.Write(',');
        }

        static private void WriteBuffer(ref FixedCharBuffer buffer, string parameterName, double parameterValue, int precision) {
            buffer.Write('"');
            buffer.Write(parameterName);
            buffer.Write("\":");
            buffer.Write(parameterValue, precision);
            buffer.Write(',');
        }

        // arrays

        static private void WriteBuffer(ref FixedCharBuffer buffer, string parameterName, string[] parameterValue) {
            buffer.Write('"');
            buffer.Write(parameterName);
            buffer.Write("\":[");
            for(int i = 0; i < parameterValue.Length; i++) {
                buffer.Write('"');
                OGDLogUtils.EscapeJSON(ref buffer, parameterValue[i]);
                buffer.Write("\",");
            }
            buffer.TrimEnd(',');
            buffer.Write("],");
        }

        static private void WriteBuffer(ref FixedCharBuffer buffer, string parameterName, IList<string> parameterValue) {
            buffer.Write('"');
            buffer.Write(parameterName);
            buffer.Write("\":[");
            for(int i = 0; i < parameterValue.Count; i++) {
                buffer.Write('"');
                OGDLogUtils.EscapeJSON(ref buffer, parameterValue[i]);
                buffer.Write("\",");
            }
            buffer.TrimEnd(',');
            buffer.Write("],");
        }

        static private void WriteBuffer(ref FixedCharBuffer buffer, string parameterName, bool[] parameterValue) {
            buffer.Write('"');
            buffer.Write(parameterName);
            buffer.Write("\":[");
            for(int i = 0; i < parameterValue.Length; i++) {
                buffer.Write(parameterValue[i]);
                buffer.Write(',');
            }
            buffer.TrimEnd(',');
            buffer.Write("],");
        }

        static private void WriteBuffer(ref FixedCharBuffer buffer, string parameterName, IList<bool> parameterValue) {
            buffer.Write('"');
            buffer.Write(parameterName);
            buffer.Write("\":[");
            for(int i = 0; i < parameterValue.Count; i++) {
                buffer.Write(parameterValue[i]);
                buffer.Write(',');
            }
            buffer.TrimEnd(',');
            buffer.Write("],");
        }

        static private void WriteBuffer(ref FixedCharBuffer buffer, string parameterName, int[] parameterValue) {
            buffer.Write('"');
            buffer.Write(parameterName);
            buffer.Write("\":[");
            for(int i = 0; i < parameterValue.Length; i++) {
                buffer.Write(parameterValue[i]);
                buffer.Write(',');
            }
            buffer.TrimEnd(',');
            buffer.Write("],");
        }

        static private void WriteBuffer(ref FixedCharBuffer buffer, string parameterName, IList<int> parameterValue) {
            buffer.Write('"');
            buffer.Write(parameterName);
            buffer.Write("\":[");
            for(int i = 0; i < parameterValue.Count; i++) {
                buffer.Write(parameterValue[i]);
                buffer.Write(',');
            }
            buffer.TrimEnd(',');
            buffer.Write("],");
        }

        static private void WriteBuffer(ref FixedCharBuffer buffer, string parameterName, long[] parameterValue) {
            buffer.Write('"');
            buffer.Write(parameterName);
            buffer.Write("\":[");
            for(int i = 0; i < parameterValue.Length; i++) {
                buffer.Write(parameterValue[i]);
                buffer.Write(',');
            }
            buffer.TrimEnd(',');
            buffer.Write("],");
        }

        static private void WriteBuffer(ref FixedCharBuffer buffer, string parameterName, IList<long> parameterValue) {
            buffer.Write('"');
            buffer.Write(parameterName);
            buffer.Write("\":[");
            for(int i = 0; i < parameterValue.Count; i++) {
                buffer.Write(parameterValue[i]);
                buffer.Write(',');
            }
            buffer.TrimEnd(',');
            buffer.Write("],");
        }

        static private void WriteBuffer(ref FixedCharBuffer buffer, string parameterName, float[] parameterValue, int precision) {
            buffer.Write('"');
            buffer.Write(parameterName);
            buffer.Write("\":[");
            for(int i = 0; i < parameterValue.Length; i++) {
                buffer.Write(parameterValue[i], precision);
                buffer.Write(',');
            }
            buffer.TrimEnd(',');
            buffer.Write("],");
        }

        static private void WriteBuffer(ref FixedCharBuffer buffer, string parameterName, IList<float> parameterValue, int precision) {
            buffer.Write('"');
            buffer.Write(parameterName);
            buffer.Write("\":[");
            for(int i = 0; i < parameterValue.Count; i++) {
                buffer.Write(parameterValue[i], precision);
                buffer.Write(',');
            }
            buffer.TrimEnd(',');
            buffer.Write("],");
        }

        static private void WriteBuffer(ref FixedCharBuffer buffer, string parameterName, double[] parameterValue, int precision) {
            buffer.Write('"');
            buffer.Write(parameterName);
            buffer.Write("\":[");
            for(int i = 0; i < parameterValue.Length; i++) {
                buffer.Write(parameterValue[i], precision);
                buffer.Write(',');
            }
            buffer.TrimEnd(',');
            buffer.Write("],");
        }

        static private void WriteBuffer(ref FixedCharBuffer buffer, string parameterName, IList<double> parameterValue, int precision) {
            buffer.Write('"');
            buffer.Write(parameterName);
            buffer.Write("\":[");
            for(int i = 0; i < parameterValue.Count; i++) {
                buffer.Write(parameterValue[i], precision);
                buffer.Write(',');
            }
            buffer.TrimEnd(',');
            buffer.Write("],");
        }

        // EVENT STREAM

        static private void WriteStream(StringBuilder stream, string parameterName, long value) {
            stream.Append('"').Append(parameterName).Append("\":").AppendInteger(value, 0).Append(',');
        }

        static private void WriteStream(StringBuilder stream, string parameterName, DateTime value) {
            // format: yyyy-MM-dd HH:mm:ss.fffZ
            stream.Append('"').Append(parameterName).Append("\":\"")
                .AppendInteger(value.Year, 4).Append('-').AppendInteger(value.Month, 2).Append('-').AppendInteger(value.Day, 2)
                .Append(' ').AppendInteger(value.Hour, 2).Append(':').AppendInteger(value.Minute, 2).Append(':').AppendInteger(value.Second, 2)
                .Append('.').AppendInteger(value.Millisecond, 3).Append('Z')
                .Append("\",");
        }

        /// <summary>
        /// Writes an event parameter.
        /// </summary>
        static private void WriteStream(StringBuilder stream, string parameterName, TimeSpan value) {
            stream.Append('"').Append(parameterName).Append("\":\"")
                .AppendInteger(value.Hours, 2).Append(':').AppendInteger(value.Minutes, 2).Append(':').AppendInteger(value.Seconds, 2)
                .Append("\",");
        }

        static private void WriteStream(StringBuilder stream, string parameterName, ref FixedCharBuffer buffer, bool escape) {
            stream.Append('"').Append(parameterName).Append("\":\"");
            if (escape) {
                stream.EscapeJSON(ref buffer);
            } else {
                stream.AppendBuffer(ref buffer);
            }

            stream.Append("\",");
        }

        static private void WriteStream(StringBuilder stream, string parameterName, string json) {
            stream.Append('"').Append(parameterName).Append("\":\"")
                .EscapeJSON(json)
                .Append("\",");
        }

        static private void WriteStream(StringBuilder stream, string parameterName, StringBuilder json) {
            stream.Append('"').Append(parameterName).Append("\":\"")
                .EscapeJSON(json)
                .Append("\",");
        }

        #endregion // String Assembly
    }
}