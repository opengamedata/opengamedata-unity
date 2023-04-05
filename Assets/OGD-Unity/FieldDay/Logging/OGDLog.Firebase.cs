#if UNITY_EDITOR
    #define FIREBASE_EDITOR
    #if UNITY_WEBGL
        #define FIREBASE_EDITOR_JS
    #elif (UNITY_ANDROID || UNITY_IOS) || FIREBASE_INSTALLED
        #define FIREBASE_UNITY
    #endif
#elif UNITY_WEBGL
    #define FIREBASE_JS
#elif FIREBASE_INSTALLED || (UNITY_ANDROID || UNITY_IOS)
    #define FIREBASE_UNITY
#else
    #warning The Firebase Unity package is not supported for this platform. 
#endif

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Scripting;

#if FIREBASE_UNITY
using Firebase;
using Firebase.Analytics;
#endif // FIREBASE_UNITY

#if UNITY_WEBGL
using AOT;
#endif // UNITY_WEBGL

namespace FieldDay {
    public sealed partial class OGDLog {
        #if FIREBASE_JS || FIREBASE_EDITOR_JS

        private delegate void FirebaseInitializeCallback(int result); // return 0 for success, 1 for error

        [DllImport("__Internal")]
        static private extern bool OGDLog_FirebasePrepare(string apiKey, string projectId, string storageBucket, string messagingSenderId, string appId, string measurementId, FirebaseInitializeCallback onError);

        [DllImport("__Internal")]
        static private extern bool OGDLog_FirebaseReady();

        [DllImport("__Internal")]
        static private extern bool OGDLog_FirebaseLoading();

        [DllImport("__Internal")]
        static private extern void OGDLog_FirebaseSetSessionConsts(string userId);

        [DllImport("__Internal")]
        static private extern void OGDLog_FirebaseSetAppConsts(string appVersion, string appFlavor, int logVersion);

        [DllImport("__Internal")]
        static private extern void OGDLog_FirebaseConfigureLegacyOption(string optionId, string value);

        [DllImport("__Internal")]
        static private extern bool OGDLog_FirebaseNewEvent(string eventName, uint sequenceIndex);

        [DllImport("__Internal")]
        static private extern bool OGDLog_FirebaseEventNumberParam(string paramName, double numValue);

        [DllImport("__Internal")]
        static private extern bool OGDLog_FirebaseEventStringParam(string paramName, string stringVal);

        [DllImport("__Internal")]
        static private extern void OGDLog_FirebaseDefaultNumberParam(string paramName, double numValue);

        [DllImport("__Internal")]
        static private extern void OGDLog_FirebaseDefaultStringParam(string paramName, string stringVal);

        [DllImport("__Internal")]
        static private extern bool OGDLog_FirebaseSubmitEvent();

        [DllImport("__Internal")]
        static private extern void OGDLog_FirebaseResetGameState();

        [DllImport("__Internal")]
        static private extern void OGDLog_FirebaseGameStateNumberParam(string paramName, double numValue);

        [DllImport("__Internal")]
        static private extern void OGDLog_FirebaseGameStateStringParam(string paramName, string stringVal);

        [DllImport("__Internal")]
        static private extern void OGDLog_FirebaseResetUserData();

        [DllImport("__Internal")]
        static private extern void OGDLog_FirebaseUserDataNumberParam(string paramName, double numValue);

        [DllImport("__Internal")]
        static private extern void OGDLog_FirebaseUserDataStringParam(string paramName, string stringVal);

        #endif // FIREBASE_JS

        static private ModuleStatus s_QueuedFirebaseStatus = ModuleStatus.Uninitialized;
        static private uint s_FirebaseEventSequenceOffset = 0;

        #if FIREBASE_UNITY

        private FirebaseApp m_FirebaseApp;
        private string m_CachedFirebaseEventId;
        private List<Firebase.Analytics.Parameter> m_CachedFirebaseEventParameters;
        private List<Firebase.Analytics.Parameter> m_CachedFirebaseGameStateParameters;
        private List<Firebase.Analytics.Parameter> m_CachedFirebaseUserDataParameters;

        #endif // FIREBASE_UNITY

        private void Firebase_AttemptActivate() {
            // if we're in the middle of writing an event we can't finish initializing firebase
            if ((m_StatusFlags & StatusFlags.WritingEvent) != 0) {
                return;
            }

            // if we're not in the middle of loading, then ignore this
            if (GetModuleStatus(ModuleId.Firebase) != ModuleStatus.Preparing) {
                return;
            }

            if (s_QueuedFirebaseStatus > ModuleStatus.Preparing) {
                SetModuleStatus(ModuleId.Firebase, s_QueuedFirebaseStatus);
                if (s_QueuedFirebaseStatus == ModuleStatus.Ready) {
                    Firebase_SetSessionConsts(m_SessionConsts);
                    Firebase_SetAppConsts(m_OGDConsts);
                    Firebase_ConfigureSettings(m_Settings);
                }
            }
        }

        #if FIREBASE_JS || FIREBASE_EDITOR_JS
        [MonoPInvokeCallback(typeof(FirebaseInitializeCallback)), Preserve]
        #endif // FIREBASE_JS || FIREBASE_EDITOR_JS
        static private void Firebase_PrepareFinish(int error) {
            if (error != 0) {
                s_QueuedFirebaseStatus = ModuleStatus.Error;
                UnityEngine.Debug.LogWarningFormat("[OGDLog.Firebase] Firebase could not be initialized");
                s_Instance.SetModuleStatus(ModuleId.Firebase, ModuleStatus.Error);
            } else {
                s_QueuedFirebaseStatus = ModuleStatus.Ready;
            }

            s_Instance.Firebase_AttemptActivate();
        }

        private void Firebase_Prepare(FirebaseConsts firebaseConsts, SessionConsts sessionConsts) {
            SetModuleStatus(ModuleId.Firebase, ModuleStatus.Preparing);
            #if FIREBASE_JS
            OGDLog_FirebasePrepare(firebaseConsts.ApiKey, firebaseConsts.ProjectId, firebaseConsts.StorageBucket, firebaseConsts.MessagingSenderId, firebaseConsts.AppId, firebaseConsts.MeasurementId, Firebase_PrepareFinish);
            #elif FIREBASE_UNITY
            // unity initialization
            AppOptions options = new AppOptions() {
                ApiKey = firebaseConsts.ApiKey,
                ProjectId = firebaseConsts.ProjectId,
                MessageSenderId = firebaseConsts.MessagingSenderId,
                StorageBucket = firebaseConsts.StorageBucket,
                AppId = firebaseConsts.AppId
            };
            m_CachedFirebaseEventParameters = new List<Firebase.Analytics.Parameter>(8);
            m_CachedFirebaseGameStateParameters = new List<Firebase.Analytics.Parameter>(8);
            m_CachedFirebaseUserDataParameters = new List<Firebase.Analytics.Parameter>(8);
            #if UNITY_EDITOR // in editor we can just create it normally
                try {
                    m_FirebaseApp = FirebaseApp.Create(options);
                    Firebase_PrepareFinish(m_FirebaseApp != null ? 0 : 1);
                } catch(Exception e) {
                    UnityEngine.Debug.LogException(e);
                    Firebase_PrepareFinish(1);
                }
            #elif UNITY_ANDROID // on android we need to make sure our dependencies are resolved first
                FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
                    var dependencyStatus = task.Result;
                    if (dependencyStatus == Firebase.DependencyStatus.Available) {
                        try {
                            m_FirebaseApp = FirebaseApp.Create(options);
                            Firebase_PrepareFinish(m_FirebaseApp != null ? 0 : 1);
                        } catch(Exception e) {
                            UnityEngine.Debug.LogException(e);
                            Firebase_PrepareFinish(1);
                        }
                    } else {
                        UnityEngine.Debug.LogErrorFormat("[OGDLog.Firebase] Could not resolve Firebase dependencies: {0}", dependencyStatus);
                        Firebase_PrepareFinish(1);
                    }
                });
            #else // otherwise we can just create it
                try {
                    m_FirebaseApp = FirebaseApp.Create(options);
                    Firebase_PrepareFinish(m_FirebaseApp != null ? 0 : 1);
                } catch(Exception e) {
                    UnityEngine.Debug.LogException(e);
                    Firebase_PrepareFinish(1);
                }
            #endif // UNITY_EDITOR
            
            #endif // FIREBASE_JS
        }

        private void Firebase_SetSessionConsts(SessionConsts sessionConsts) {
            #if FIREBASE_JS
            OGDLog_FirebaseSetSessionConsts(sessionConsts.UserId);
            #elif FIREBASE_UNITY
            FirebaseAnalytics.SetUserId(sessionConsts.UserId);
            #endif // FIREBASE_JS
        }

        private void Firebase_SetAppConsts(OGDLogConsts appConsts) {
            #if FIREBASE_JS
            OGDLog_FirebaseSetAppConsts(appConsts.AppVersion, appConsts.AppBranch, appConsts.ClientLogVersion);
            #endif // FIREBASE_JS
        }

        private void Firebase_ConfigureSettings(SettingsFlags flags) {

        }

        private void Firebase_NewEvent(string eventName, uint eventSequenceIndex) {
            eventSequenceIndex += s_FirebaseEventSequenceOffset;
            #if FIREBASE_JS
            OGDLog_FirebaseNewEvent(eventName, eventSequenceIndex);
            #elif FIREBASE_UNITY
            m_CachedFirebaseEventId = eventName;
            m_CachedFirebaseEventParameters.Add(new Parameter("event_sequence_index", eventSequenceIndex));
            m_CachedFirebaseEventParameters.Add(new Parameter("app_version", m_OGDConsts.AppVersion));
            m_CachedFirebaseEventParameters.Add(new Parameter("app_flavor", m_OGDConsts.AppBranch));
            m_CachedFirebaseEventParameters.Add(new Parameter("user_code", m_SessionConsts.UserId));
            #endif // FIREBASE_JS
        }

        private void Firebase_SetEventParam(string paramName, string paramValue) {
            #if FIREBASE_JS
            OGDLog_FirebaseEventStringParam(paramName, paramValue);
            #elif FIREBASE_UNITY
            m_CachedFirebaseEventParameters.Add(new Parameter(paramName, paramValue));
            #endif // FIREBASE_JS
        }

        private void Firebase_SetEventParam(string paramName, long paramValue) {
            #if FIREBASE_JS
            OGDLog_FirebaseEventNumberParam(paramName, paramValue);
            #elif FIREBASE_UNITY
            m_CachedFirebaseEventParameters.Add(new Parameter(paramName, paramValue));
            #endif // FIREBASE_JS
        }

        private void Firebase_SetEventParam(string paramName, double paramValue) {
            #if FIREBASE_JS
            OGDLog_FirebaseEventNumberParam(paramName, paramValue);
            #elif FIREBASE_UNITY
            m_CachedFirebaseEventParameters.Add(new Parameter(paramName, paramValue));
            #endif // FIREBASE_JS
        }

        private void Firebase_SubmitEvent() {
            #if FIREBASE_JS
            OGDLog_FirebaseSubmitEvent();
            #elif FIREBASE_UNITY
            if (m_CachedFirebaseEventId != null) {
                FirebaseAnalytics.LogEvent(m_CachedFirebaseEventId, BuildParameterArray(m_CachedFirebaseEventParameters, m_CachedFirebaseGameStateParameters, m_CachedFirebaseUserDataParameters));
                m_CachedFirebaseEventId = null;
                ClearParameterList(m_CachedFirebaseEventParameters);
            }
            #endif // FIREBASE_UNITY
        }

        private void Firebase_ResetGameState() {
            #if FIREBASE_JS
            OGDLog_FirebaseResetGameState();
            #elif FIREBASE_UNITY
            ClearParameterList(m_CachedFirebaseGameStateParameters);
            #endif // FIREBASE_JS
        }

        private void Firebase_SetGameStateParam(string paramName, string paramValue) {
            #if FIREBASE_JS
            OGDLog_FirebaseGameStateStringParam(paramName, paramValue);
            #elif FIREBASE_UNITY
            m_CachedFirebaseGameStateParameters.Add(new Parameter(paramName, paramValue));
            #endif // FIREBASE_JS
        }

        private void Firebase_SetGameStateParam(string paramName, long paramValue) {
            #if FIREBASE_JS
            OGDLog_FirebaseGameStateNumberParam(paramName, paramValue);
            #elif FIREBASE_UNITY
            m_CachedFirebaseGameStateParameters.Add(new Parameter(paramName, paramValue));
            #endif // FIREBASE_JS
        }

        private void Firebase_SetGameStateParam(string paramName, double paramValue) {
            #if FIREBASE_JS
            OGDLog_FirebaseGameStateNumberParam(paramName, paramValue);
            #elif FIREBASE_UNITY
            m_CachedFirebaseGameStateParameters.Add(new Parameter(paramName, paramValue));
            #endif // FIREBASE_JS
        }

        private void Firebase_ResetUserData() {
            #if FIREBASE_JS
            OGDLog_FirebaseResetUserData();
            #elif FIREBASE_UNITY
            ClearParameterList(m_CachedFirebaseUserDataParameters);
            #endif // FIREBASE_JS
        }

        private void Firebase_SetUserDataParam(string paramName, string paramValue) {
            #if FIREBASE_JS
            OGDLog_FirebaseUserDataStringParam(paramName, paramValue);
            #elif FIREBASE_UNITY
            m_CachedFirebaseUserDataParameters.Add(new Parameter(paramName, paramValue));
            #endif // FIREBASE_JS
        }

        private void Firebase_SetUserDataParam(string paramName, long paramValue) {
            #if FIREBASE_JS
            OGDLog_FirebaseUserDataNumberParam(paramName, paramValue);
            #elif FIREBASE_UNITY
            m_CachedFirebaseUserDataParameters.Add(new Parameter(paramName, paramValue));
            #endif // FIREBASE_JS
        }

        private void Firebase_SetUserDataParam(string paramName, double paramValue) {
            #if FIREBASE_JS
            OGDLog_FirebaseUserDataNumberParam(paramName, paramValue);
            #elif FIREBASE_UNITY
            m_CachedFirebaseUserDataParameters.Add(new Parameter(paramName, paramValue));
            #endif // FIREBASE_JS
        }

        #if FIREBASE_UNITY

        static private Parameter[] BuildParameterArray(List<Parameter> eventParams, List<Parameter> gameState, List<Parameter> userData) {
            int totalCount = eventParams.Count + gameState.Count + userData.Count;
            Parameter[] paramArr = new Parameter[totalCount];
            int i = 0;
            foreach(var param in eventParams) {
                paramArr[i++] = param;
            }
            foreach(var param in gameState) {
                paramArr[i++] = param;
            }
            foreach(var param in userData) {
                paramArr[i++] = param;
            }
            return paramArr;
        }

        static private void ClearParameterList(List<Parameter> parameters) {
            // TODO: Dispose of these if it's safe to do so
            // The intended lifespan of a Parameter object is poorly documented
            // in Firebase's plugin documentation
            
            // foreach(var param in parameters) {
            //     param.Dispose();
            // }
            parameters.Clear();
        }

        #endif // FIREBASE_UNITY
    }
}