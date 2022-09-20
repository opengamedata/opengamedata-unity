var OGDLogFirebaseLib = {
    $FirebaseCache: {

        /**
         * Session constants.
         */
        sessionConsts: { },

        /**
         * App instance.
         */
        appInstance: null,

        /**
         * Analytics instance.
         */
        analyticsInstance: null,

        /**
         * Current event instance.
         */
        currentEventInstance: { },

        /**
         * Current event identifier.
         */
        currentEventId: null,

        /**
         * Tracks if analytics is loading.
         */
        analyticsState: null
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
            if (FirebaseCache.analyticsState != "loading") {

            }
            return;
        }

        FirebaseCache.analyticsState = "loading";

        var config = {
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
            FirebaseCache.appInstance = initializeApp(config);
            FirebaseCache.analyticsInstance = initializeAnalytics(FirebaseCache.appInstance, {
                config: {
                    cookie_flags: "max-age=7200;secure;samesite=none"
                }
            });
            if (!FirebaseCache.analyticsInstance) {
                onScriptError();
            } else {
                setUserId(FirebaseCache.analytics, Firebase.sessionConsts.user_id || "");
                setUserProperties(FirebaseCache.analytics, {
                    user_data: Firebase.sessionConsts.user_data || ""
                });
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
                if (onFinished) {
                    dynCall_vi(onFinished, 1);
                }
            }
        };

        var appLoad = new HTMLScriptElement();
        appLoad.src = "https://www.gstatic.com/firebasejs/9.10.0/firebase-app.js";
        appLoad.onload = onScriptLoaded;
        appLoad.onerror = onScriptError;

        var analyticsLoad = new HTMLScriptElement();
        analyticsLoad.src = "https://www.gstatic.com/firebasejs/9.10.0/firebase-analytics.js";
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
        return !!FirebaseCache.analytics;
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
        Firebase.sessionConsts = {
            user_id: Pointer_stringify(userId),
            user_data: Pointer_stringify(userData)
        };
        if (FirebaseCache.analytics) {
            setUserId(FirebaseCache.analytics, Firebase.sessionConsts.user_id || "");
            setUserProperties(FirebaseCache.analytics, {
                user_data: Firebase.sessionConsts.user_data || ""
            });
        }
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
     * Submits the current event instance.
     */
    OGDLog_FirebaseSubmitEvent: function() {
        if (FirebaseCache.currentEventId) {
            logEvent(FirebaseCache.analyticsInstance, FirebaseCache.currentEventId, FirebaseCache.currentEventInstance);
            FirebaseCache.currentEventId = null;
            FirebaseCache.currentEventInstance = {};
        }
    }
}

autoAddDeps(OGDLogFirebaseLib, '$FirebaseCache');
mergeInto(LibraryManager.library, OGDLogFirebaseLib);