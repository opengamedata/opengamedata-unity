using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace FieldDay
{
    /// <summary>
    /// Handles communication with the OpenGameData server.
    /// </summary>
    public class SimpleLog
    {
        /// <value>A list of <c>LogEvent</c> objects before sent to the database.</value>
        private List<ILogEvent> accruedLog = new List<ILogEvent>();

        private string appId;
        private int appVersion;

        private long sessionId;
        private string reqUrl;

        private bool flushing = false;
        private int flushedTo = 0;
        private int flushIndex = 0;

        /// <summary>
        /// Creates a new SimpleLog object, finds persistent session id if specified, and builds
        /// the url string as the target for all POST requests.
        /// </summary>
        /// <param name="inAppId">An identifier for this app within the database.</param>
        /// <param name="inAppVersion">The current version of this app for all logging events.</param>
        public SimpleLog(string inAppId, int inAppVersion)
        {
            appId = inAppId;
            appVersion = inAppVersion;
            sessionId = SimpleLogUtils.UUIDint();

            reqUrl = SimpleLogUtils.BuildUrlString("https://fielddaylab.wisc.edu/logger/log.php?app_id={0}&app_version={1}&session_id={2}",
                                                    Uri.EscapeDataString(appId), Uri.EscapeDataString(appVersion.ToString()),
                                                    Uri.EscapeDataString(sessionId.ToString()));
        }

        /// <summary>
        /// Logs a new event.
        /// </summary>
        /// <param name="inData">The <c>LogEvent</c> object to send to the database.</param>
        /// <param name="debug">Optional parameter for printing HTTP response codes to the console (false by default).</param>
        public void Log(ILogEvent inData, bool debug=false)
        {
            inData.Data["session_n"] = flushIndex.ToString();
            inData.Data["client_time"] = DateTime.Now.ToString();
            flushIndex++;
            accruedLog.Add(inData);
            
            Flush(debug);
        }

        /// <summary>
        /// Flushes all queued events and sends a POST request to the database.
        /// </summary>
        /// <param name="debug">Optional parameter for printing HTTP response codes to the console (false by default).</param>
        public void Flush(bool debug=false)
        {
            if (flushing || accruedLog.Count == 0) return;
            flushing = true;

            string postUrl = SimpleLogUtils.BuildUrlString("{0}&req_id={1}", reqUrl, Uri.EscapeDataString(SimpleLogUtils.UUIDint().ToString()));

            // Write the AccruedLog to a JSON string and convert it to base64
            string postData = SimpleLogUtils.BuildUrlString("data={0}", Uri.EscapeDataString(SimpleLogUtils.BuildPostDataString(accruedLog)));

            // Send a POST request to https://fielddaylab.wisc.edu/logger/log.php with the proper content type
            UnityWebRequest req = UnityWebRequest.Post(postUrl, postData);
            req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            UnityWebRequestAsyncOperation reqOperation = req.SendWebRequest();

            reqOperation.completed += obj => 
            {
                if (debug) Debug.Log(req.responseCode);

                int flushed = Int32.Parse(accruedLog[accruedLog.Count - 1].Data["session_n"]);
                int cutoff = accruedLog.Count - 1;

                for (var i = accruedLog.Count - 1; i >= 0 && Int32.Parse(accruedLog[i].Data["session_n"]) > flushed; --i)
                {
                    cutoff = i - 1;
                }

                if (cutoff >= 0)
                {
                    accruedLog.RemoveRange(0, cutoff + 1);
                }

                flushing = false;
            };
        }
    }

    /// <summary>
    /// Interface implemented by the LogEvent class.
    /// </summary>
    public interface ILogEvent
    {
        Dictionary<string, string> Data { get; set; }
    }
}
