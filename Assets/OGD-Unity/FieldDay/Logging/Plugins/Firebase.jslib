var OGDLogFirebaseLib = {
    $FirebaseCache: {

        /**
         * Session constants.
         */
        sessionConsts: {
            /** @type {string} */ user_id: null,
            /** @type {string} */ user_data: null
        },

        /**
         * Application constants.
         */
        appConsts: {
            /** @type {string} */ app_version: null,
            /** @type {string} */ app_flavor: null,
            /** @type {number} */ log_version: 1
        },

        /**
         * Legacy configuration options
         */
        legacyConfig: {
            /** @type {string} */ copyUserIdToParameters: "user_code"
        },

        /**
         * App instance.
         * @type {FirebaseApp}
         */
        appInstance: null,

        /**
         * Analytics instance.
         * @type {FirebaseAnalytics}
         */
        analyticsInstance: null,

        /**
         * Current event instance.
         * @type {object}
         */
        currentEventInstance: { },

        /**
         * Current event identifier.
         * @type {string}
         */
        currentEventId: null,

        /**
         * Tracks if analytics is loading.
         * @type {string}
         */
        analyticsState: null,

        /**
         * Default logging parameters.
         * @type {object}
         */
        defaultParameters: { },

        /**
         * Syncs constants.
         */
        SyncSettings: function() {
            if (!FirebaseCache.analyticsInstance) {
                return;
            }

            // copy user properties
            FirebaseCache.analyticsInstance.setUserId(FirebaseCache.sessionConsts.user_id || "");
            FirebaseCache.analyticsInstance.setUserProperties({
                user_data: FirebaseCache.sessionConsts.user_data || ""
            });

            // copy app constants to default parameters
            Object.assign(FirebaseCache.defaultParameters, FirebaseCache.appConsts);
            
            // if we're using legacy logging, copy user id to the parameters too
            if (FirebaseCache.legacyConfig.copyUserIdToParameters != null) {
                FirebaseCache.defaultParameters[FirebaseCache.copyUserIdToParameters] = FirebaseCache.sessionConsts.user_id;
            }
        }
    },

    /**
     * Prepares the analytics module.
     * @param {string} apiKey 
     * @param {string} projectId 
     * @param {string} storageBucket 
     * @param {string} messagingSenderId 
     * @param {string} appId 
     * @param {string} measurementId 
     * @param {Function} onFinished
     * @returns 
     */
    OGDLog_FirebasePrepare: function(apiKey, projectId, storageBucket, messagingSenderId, appId, measurementId, onFinished) {
        if (FirebaseCache.appInstance || FirebaseCache.analyticsState) {
            return;
        }

        FirebaseCache.analyticsState = "loading";

        var appConfig = {
            apiKey: Pointer_stringify(apiKey),
            projectId: Pointer_stringify(projectId),
            storageBucket: Pointer_stringify(storageBucket),
            messagingSenderId: Pointer_stringify(messagingSenderId),
            appId: Pointer_stringify(appId),
            measurementId: Pointer_stringify(measurementId)
        };

        var scriptsLoadedCount = 0;
        var finishInitializing = function() {
            FirebaseCache.analyticsState = "loaded";  
            FirebaseCache.appInstance = firebase.initializeApp(appConfig);
            FirebaseCache.analyticsInstance = FirebaseCache.appInstance.analytics();
            gtag('config', 'ID', {
                cookie_flags: "max-age=7200;secure;samesite=none"
            });
            if (!FirebaseCache.analyticsInstance) {
                onScriptError();
            } else {
                FirebaseCache.SyncSettings();
                if (onFinished) {
                    dynCall_vi(onFinished, 0);
                }
            }
        };

        var onScriptLoaded = function() {
            scriptsLoadedCount++;
            if (scriptsLoadedCount >= 2 && FirebaseCache.analyticsState == "loading") {
                finishInitializing();
            }
        };
        var onScriptError = function() {
            if (FirebaseCache.analyticsState != "error") {
                FirebaseCache.analyticsState = "error";
                console.error("[Firebase] Initialization failed");
                if (onFinished) {
                    dynCall_vi(onFinished, 1);
                }
            }
        };

        var appLoad = document.createElement("script");
        appLoad.src = "https://www.gstatic.com/firebasejs/8.9.1/firebase-app.js";
        appLoad.onload = onScriptLoaded;
        appLoad.onerror = onScriptError;

        var analyticsLoad = document.createElement("script");
        analyticsLoad.src = "https://www.gstatic.com/firebasejs/8.9.1/firebase-analytics.js";
        analyticsLoad.onload = onScriptLoaded;
        analyticsLoad.onerror = onScriptError;

        document.head.appendChild(appLoad);
        document.head.appendChild(analyticsLoad);
    },

    /**
     * Returns if Firebase Logging is ready.
     * @returns {boolean}
     */
    OGDLog_FirebaseReady: function() {
        return !!FirebaseCache.analyticsInstance;
    },

    /**
     * Returns if Firebase Logging is loading.
     * @returns {boolean}
     */
     OGDLog_FirebaseLoading: function() {
        return FirebaseCache.analyticsState == "loading";
    },

    /**
     * Sets the session constants.
     * @param {string} userId
     * @param {string} userData
     */
    OGDLog_FirebaseSetSessionConsts: function(userId, userData) {
        var sessionConsts = FirebaseCache.sessionConsts;
        sessionConsts.user_id = Pointer_stringify(userId);
        sessionConsts.user_data = Pointer_stringify(userData);
        FirebaseCache.SyncSettings();
    },

    /**
     * Sets application constants.
     * @param {string} appVersion 
     * @param {string} appFlavor 
     * @param {number} logVersion
     */
    OGDLog_FirebaseSetAppConsts: function(appVersion, appFlavor, logVersion) {
        var appConsts = FirebaseCache.appConsts;
        appConsts.app_version = Pointer_stringify(appVersion);
        appConsts.app_flavor = Pointer_stringify(appFlavor);
        appConsts.log_version = logVersion;
        FirebaseCache.SyncSettings();
    },

    /**
     * Configures a legacy option.
     * @param {string} optionId
     * @param {string} value 
     */
    OGDLog_FirebaseConfigureLegacyOption: function(optionId, value) {
        FirebaseCache.legacyConfig[Pointer_stringify(optionId)] = value;
        FirebaseCache.SyncSettings();
    },

    /**
     * Begins a new Firebase event.
     * @param {string} eventName
     * @param {number} sequenceIndex 
     */
    OGDLog_FirebaseNewEvent: function(eventName, sequenceIndex) {
        FirebaseCache.currentEventId = Pointer_stringify(eventName);
        FirebaseCache.currentEventInstance = {
            event_sequence_index: sequenceIndex
        };
        Object.assign(FirebaseCache.currentEventInstance, FirebaseCache.defaultParameters);
    },

    /**
     * Adds a number parameter to the current event.
     * @param {string} paramName 
     * @param {number} numValue 
     */
    OGDLog_FirebaseEventNumberParam: function(paramName, numValue) {
        FirebaseCache.currentEventInstance[Pointer_stringify(paramName)] = numValue;
    },

    /**
     * Adds a string parameter to the current event.
     * @param {string} paramName 
     * @param {string} stringVal 
     */
    OGDLog_FirebaseEventStringParam: function(paramName, stringVal) {
        FirebaseCache.currentEventInstance[Pointer_stringify(paramName)] = Pointer_stringify(stringVal);
    },

    /**
     * Adds a default number parameter.
     * @param {string} paramName 
     * @param {number} numValue 
     */
     OGDLog_FirebaseDefaultNumberParam: function(paramName, numValue) {
        FirebaseCache.defaultParameters[Pointer_stringify(paramName)] = numValue;
    },

    /**
     * Adds a default string parameter.
     * @param {string} paramName 
     * @param {string} stringVal 
     */
     OGDLog_FirebaseDefaultStringParam: function(paramName, stringVal) {
        FirebaseCache.defaultParameters[Pointer_stringify(paramName)] = Pointer_stringify(stringVal);
    },

    /**
     * Submits the current event instance.
     */
    OGDLog_FirebaseSubmitEvent: function() {
        if (FirebaseCache.currentEventId) {
            FirebaseCache.analyticsInstance.logEvent(FirebaseCache.currentEventId, FirebaseCache.currentEventInstance);
            FirebaseCache.currentEventId = null;
            FirebaseCache.currentEventInstance = {};
        }
    }
}

// const FirebaseCache = OGDLogFirebaseLib.$FirebaseCache;
// /**
//  * @param ptr 
//  * @return {string}
//  */
// function Pointer_stringify(ptr);

autoAddDeps(OGDLogFirebaseLib, '$FirebaseCache');
mergeInto(LibraryManager.library, OGDLogFirebaseLib);