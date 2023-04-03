// Comment out the following line if we're no longer using the testing url
// #define USE_TESTING_ENDPOINT

using System;

namespace FieldDay {

    /// <summary>
    /// OpenGameData logging constants.
    /// </summary>
    [Serializable]
    public struct OGDLogConsts {
        /// <summary>
        /// Identifier for the app. Should match the name of the game in the database.
        /// </summary>
        public string AppId;

        /// <summary>
        /// The current version of the app.
        /// </summary>
        public string AppVersion;

        /// <summary>
        /// (Optional) The current branch of the app.
        /// </summary>
        public string AppBranch;

        /// <summary>
        /// Client log version.
        /// </summary>
        public int ClientLogVersion;

        /// <summary>
        /// The version of the logging code.
        /// </summary>
        public const string LogVersion = "opengamedata";

        /// <summary>
        /// Endpoint base.
        /// </summary>
        public const string LogEndpoint = 
        #if USE_TESTING_ENDPOINT
            "https://ogdlogger.fielddaylab.wisc.edu/logger-testing/log.php";
        #else
            "https://ogdlogger.fielddaylab.wisc.edu/logger/log.php";
        #endif // USE_TESTING_ENDPOINT
    }

    /// <summary>
    /// Session-level constants.
    /// </summary>
    internal struct SessionConsts {
        /// <summary>
        /// Unique identifier for this session.
        /// </summary>
        public long SessionId;

        /// <summary>
        /// (Optional) The player's personal ID.
        /// </summary>
        public string UserId;
    }

    /// <summary>
    /// Firebase configuration parameters.
    /// </summary>
    [Serializable]
    public struct FirebaseConsts {
        public string ApiKey;
        public string ProjectId;
        public string StorageBucket;
        public string MessagingSenderId;
        public string AppId;
        public string MeasurementId;
    }
}
