#if UNITY_EDITOR
    #define FIREBASE_EDITOR
    #if UNITY_WEBGL
        #define FIREBASE_EDITOR_JS
    #else
        #define FIREBASE_UNITY
    #endif
#elif UNITY_WEBGL
    #define FIREBASE_JS
#elif UNITY_ANDROID || UNITY_IOS
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
        static private extern void OGDLog_FirebaseSetSessionConsts(string userId, string userData);

        [DllImport("__Internal")]
        static private extern void OGDLog_FirebaseSetAppConsts(string appId, string appFlavor, int logVersion);

        [DllImport("__Internal")]
        static private extern void OGDLog_FirebaseConfigureLegacyOption(string optionId, string value);

        [DllImport("__Internal")]
        static private extern bool OGDLog_FirebaseNewEvent(string eventName, uint sequenceIndex);

        [DllImport("__Internal")]
        static private extern bool OGDLog_FirebaseEventNumberParam(string paramName, float numValue);

        [DllImport("__Internal")]
        static private extern bool OGDLog_FirebaseEventStringParam(string paramName, string stringVal);

        [DllImport("__Internal")]
        static private extern void OGDLog_FirebaseDefaultNumberParam(string paramName, float numValue);

        [DllImport("__Internal")]
        static private extern void OGDLog_FirebaseDefaultStringParam(string paramName, string stringVal);

        [DllImport("__Internal")]
        static private extern bool OGDLog_FirebaseSubmitEvent();

        #endif // FIREBASE_JS

        static private ModuleStatus s_QueuedFirebaseStatus = ModuleStatus.Uninitialized;

        #if FIREBASE_UNITY

        private FirebaseApp m_FirebaseApp;
        private string m_CachedFirebaseEventId;
        private List<Firebase.Analytics.Parameter> m_CachedFirebaseEventParameters;

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

        #if UNITY_WEBGL
        [MonoPInvokeCallback(typeof(FirebaseInitializeCallback)), Preserve]
        #endif // UNITY_WEBGL
        static private void Firebase_PrepareFinish(int error) {
            if (error != 0) {
                s_QueuedFirebaseStatus = ModuleStatus.Error;
                UnityEngine.Debug.LogWarningFormat("[OGDLog.Firebase] Firebase could not be initialized");
            } else {
                s_QueuedFirebaseStatus = ModuleStatus.Ready;
            }

            s_Instance.Firebase_AttemptActivate();
        }

        private void Firebase_Prepare(FirebaseConsts firebaseConsts, SessionConsts sessionConsts) {
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
            #if UNITY_EDITOR // in editor we can just create it normally
                m_FirebaseApp = FirebaseApp.Create(options, "editor");
                Firebase_PrepareFinish(0);
            #elif UNITY_ANDROID // on android we need to make sure our dependencies are resolved first
                FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
                    var dependencyStatus = task.Result;
                    if (dependencyStatus == Firebase.DependencyStatus.Available) {
                        m_FirebaseApp = FirebaseApp.Create(options);
                        Firebase_PrepareFinish(0);
                    } else {
                        UnityEngine.Debug.LogErrorFormat(("[OGDLog.Firebase] Could not resolve Firebase dependencies: {0}", dependencyStatus);
                        Firebase_PrepareFinish(1);
                    }
                });
            #else // otherwise we can just create it
                m_FirebaseApp = FirebaseApp.Create(options);
                Firebase_PrepareFinish(0);
            #endif // UNITY_EDITOR
            
            #endif // FIREBASE_JS
        }

        private void Firebase_SetSessionConsts(SessionConsts sessionConsts) {
            #if FIREBASE_JS
            OGDLog_FirebaseSetSessionConsts(sessionConsts.UserId, sessionConsts.UserData);
            #elif FIREBASE_UNITY
            FirebaseAnalytics.SetUserId(sessionConsts.UserId);
            if (!string.IsNullOrEmpty(sessionConsts.UserData)) {
                FirebaseAnalytics.SetUserProperty("user_data", sessionConsts.UserData);
            } else {
                FirebaseAnalytics.SetUserProperty("user_data", "");
            }
            #endif // FIREBASE_JS
        }

        private void Firebase_SetAppConsts(OGDLogConsts appConsts) {
            #if FIREBASE_JS
            OGDLog_FirebaseSetAppConsts(appConsts.AppId, appConsts.AppBranch, appConsts.ClientLogVersion);
            #endif // FIREBASE_JS
        }

        private void Firebase_ConfigureSettings(SettingsFlags flags) {

        }

        private void Firebase_NewEvent(string eventName, uint eventSequenceIndex) {
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

        private void Firebase_SetEventParam(string paramName, float paramValue) {
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
                FirebaseAnalytics.LogEvent(m_CachedFirebaseEventId, m_CachedFirebaseEventParameters.ToArray());
                m_CachedFirebaseEventId = null;
                m_CachedFirebaseEventParameters.Clear();
            }
            #endif // FIREBASE_UNITY
        }
    }
}