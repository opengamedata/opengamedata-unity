var OGDLogFirebaseLib = {
    $FirebaseCache: {

        /**
         * Session constants.
         */
        sessionConsts: { },

        /**
         * Analytics instance.
         */
        analytics: null,

        /**
         * Current event instance.
         */
        currentEventInstance: { },

        /**
         * Current event identifier.
         */
        currentEventId: null,
    },

    /**
     * Returns if Firebase Logging is ready.
     * @returns {boolean}
     */
    OGDLog_FirebaseReady: function() {
        return !!FirebaseCache.analytics;
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
     * Adds a boolean parameter to the current event.
     * @param {string} paramName 
     * @param {boolean} boolVal 
     */
    OGDLog_FirebaseEventBoolParam: function(paramName, boolVal) {
        FirebaseCache.currentEventInstance[Pointer_stringify(paramName)] = boolVal;
    },

    /**
     * Submits the current event instance.
     */
    OGDLog_FirebaseSubmitEvent: function() {
        if (FirebaseCache.currentEventId) {
            FirebaseCache.analytics.logEvent(FirebaseCache.currentEventId, FirebaseCache.currentEventInstance);
            FirebaseCache.currentEventId = null;
            FirebaseCache.currentEventInstance = {};
        }
    }
}

autoAddDeps(OGDLogFirebaseLib, '$FirebaseCache');
mergeInto(LibraryManager.library, OGDLogFirebaseLib);