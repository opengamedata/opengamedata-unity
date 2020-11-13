using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using BeauUtil;

namespace FieldDay
{
    /// <summary>
    /// Contains various helper functions for use when logging within SimpleLog.
    /// </summary>
    public class SimpleLogUtils
    {
        #region JavaScript Functions

        [DllImport("__Internal")]
        public static extern string GetCookie(string name);

        [DllImport("__Internal")]
        public static extern void SetCookie(string name, string val, int days);

        #endregion // JavaScript Functions
        
        private const int ISOEncodingId = 28591;
        private static StringBuilder stringBuilder = new StringBuilder();

        /// <summary>
        /// Generates a 17 digit integer for use as a unique session id.
        /// </summary>
        public static long UUIDint()
        {
            DateTime dt = DateTime.Now;
            string id = ("" + dt.Year).Substring(2);

            if (dt.Month < 10) id += "0";
            id += dt.Month;

            if (dt.Day < 10) id += "0";
            id += dt.Day;

            if (dt.Hour < 10) id += "0";
            id += dt.Hour;

            if (dt.Minute < 10) id += "0";
            id += dt.Minute;

            if (dt.Second < 10) id += "0";
            id += dt.Second;

            System.Random rand = new System.Random();

            for (int i = 0; i < 5; ++i)
            {
                id += Math.Floor(rand.NextDouble() * 10);
            }

            return Int64.Parse(id);
        }

        /// <summary>
        /// Takes a list of LogEvents and returns a single JSON string containing all logged elements.
        /// </summary>
        public static string BuildPostDataString(List<ILogEvent> log)
        {
            foreach (ILogEvent logEvent in log)
            {
                foreach (KeyValuePair<string, string> kvp in logEvent.Data)
                {
                    stringBuilder.AppendFormat("{{\"{0}\":\"{1}\"}},", kvp.Key, kvp.Value);
                }
            }

            // Remove trailing comma
            stringBuilder.Length--;
            return btoa(stringBuilder.Flush());
        }

        /// <summary>
        /// Builds a string with a specified array of arguments.
        /// </summary>
        public static string BuildUrlString(string formatString, params object[] args)
        {
            stringBuilder.AppendFormat(formatString, args);
            return stringBuilder.Flush();
        }

        /// <summary>
        /// Uses ISO encoding and converts the given string into a Base64 string.
        /// </summary>
        public static string btoa(string str)
        {
            return System.Convert.ToBase64String(Encoding.GetEncoding(ISOEncodingId).GetBytes(str));
        }
    }
}
