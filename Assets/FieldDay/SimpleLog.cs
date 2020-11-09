using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BeauUtil;
using UnityEngine;
using UnityEngine.Networking;

namespace FieldDay
{
    /// <summary>
    /// Simple Logger
    /// </summary>
    public class SimpleLog
    {
        private string playerId;
        Regex playerIdRegex = new Regex("/^([a-zA-Z][0-9]{3})$/");

        private bool flushing = false;
        private List<ILogEvent> accruedLog = new List<ILogEvent>();
        private int flushedTo = 0;
        private int flushIndex = 0;

        private string appId;
        private int appVersion;
        private long sessionId;
        private string persistentSessionId;
        private string reqUrl;

        public SimpleLog(string inAppId, int inAppVersion, QueryParams queryParams)
        {
            appId = inAppId;
            appVersion = inAppVersion;

            if (queryParams != null)
            {
                playerId = queryParams.Get("player_id");
            }

            if (playerId != null && playerIdRegex.IsMatch(playerId))
            {
                Application.OpenURL("https://fielddaylab.wisc.edu/studies/" + Uri.EscapeDataString(appId.ToLower()));
                playerId = null;
            }

            sessionId = SimpleLogUtils.UUIDint();

            #if UNITY_EDITOR
            
            persistentSessionId = "";

            #else

            persistentSessionId = SimpleLogUtils.GetCookie("persistent_session_id");

            if (persistentSessionId == null || persistentSessionId == "")
            {
                persistentSessionId = sessionId.ToString();
                SimpleLogUtils.SetCookie("persistent_session_id", persistentSessionId, 100);
            }

            #endif // UNITY_EDITOR

            string playerIdStr = "";

            if (playerId != null)
            {
                playerIdStr = SimpleLogUtils.BuildUrlString("&playerId={0}", Uri.EscapeDataString(playerId.ToString()));
            }

            reqUrl = SimpleLogUtils.BuildUrlString("https://fielddaylab.wisc.edu/logger/log.php?app_id={0}&app_version={1}&session_id={2}&persistent_session_id={3}{4}",
                                                    Uri.EscapeDataString(appId), Uri.EscapeDataString(appVersion.ToString()),
                                                    Uri.EscapeDataString(sessionId.ToString()), Uri.EscapeDataString(persistentSessionId), playerIdStr);
        }

        /// <summary>
        /// Logs a new event.
        /// </summary>
        public void Log(ILogEvent inData)
        {
            inData.Data["session_n"] = flushIndex.ToString();
            inData.Data["client_time"] = DateTime.Now.ToString();
            flushIndex++;
            accruedLog.Add(inData);

            DebugData(inData.Data);
            
            Flush();
        }

        /// <summary>
        /// Flushes all queued events
        /// </summary>
        public void Flush()
        {
            if (flushing || accruedLog.Count == 0) return;
            flushing = true;

            string postUrl = SimpleLogUtils.BuildUrlString("{0}&req_id={1}", reqUrl, Uri.EscapeDataString(SimpleLogUtils.UUIDint().ToString()));

            // Write the AccruedLog to a JSON string and convert it to base64
            // TODO: Ensure ASCII from Jo Wilder SimpleLog (if necessary)
            string postData = SimpleLogUtils.BuildUrlString("data={0}", Uri.EscapeDataString(SimpleLogUtils.BuildPostDataString(accruedLog)));

            // Send a POST request to https://fielddaylab.wisc.edu/logger/log.php with the proper content type
            UnityWebRequest req = UnityWebRequest.Post(postUrl, postData);
            req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            UnityWebRequestAsyncOperation reqOperation = req.SendWebRequest();

            reqOperation.completed += obj => 
            {
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

        // Temporary helper to print log values to console
        private void DebugData(Dictionary<string, string> data)
        {
            foreach (KeyValuePair<string, string> kvp in data)
            {
                Debug.Log("key: " + kvp.Key + " value: " + kvp.Value);
            }
        }
    }

    public interface ILogEvent
    {
        Dictionary<string, string> Data { get; set; }
    }
}
