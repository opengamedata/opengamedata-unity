using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FieldDay
{
    /// <summary>
    /// Contains various helper functions for use when logging within SimpleLog.
    /// </summary>
    public class SimpleLogUtils
    {
        #region JavaScript Functions

        [DllImport("__Internal")]
        /// <summary>
        /// Imports the <c>GetCookie</c> function from the <c>CookieUtils.jslib</c> plugin to retreive a given cookie.
        /// </summary>
        /// <returns>
        /// The value for the given cookie.
        /// </returns>
        /// <param name="name">The name of the cookie to retreive.</param>
        public static extern string GetCookie(string name);

        [DllImport("__Internal")]
        /// <summary>
        /// Imports the <c>SetCookie</c> function from the <c>CookieUtils.jslib</c> plugin to set the value of a given cookie.
        /// </summary>
        /// <param name="name">The name for the cookie to set.</param>
        /// <param name="val">The value to set the given cookie to.</param>
        /// <param name="days">Number of days to use when creating unique ID for this cookie.</param>
        public static extern void SetCookie(string name, string val, int days);

        #endregion // JavaScript Functions
        
        /// <value>ID corresponding to ISO encoding, used for converting a string to Base64.</value>
        private const int ISOEncodingId = 28591;

        /// <value>The StringBuilder used to format data into a JSON string.</value>
        private static StringBuilder stringBuilder = new StringBuilder();

        /// <summary>
        /// Generates a 17 digit number using the current datetime for use as a unique session id.
        /// </summary>
        /// <returns>
        /// A unique 17 digit long.
        /// </returns>
        public static long UUIDint()
        {
            DateTime dt = DateTime.Now;
            string id = ("" + dt.Year).Substring(2);

            // Add an extra 0 if any datetime values are only single digits
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

            // Extend the ID with random doubles
            for (int i = 0; i < 5; ++i)
            {
                id += Math.Floor(rand.NextDouble() * 10);
            }

            return Int64.Parse(id);
        }

        /// <summary>
        /// Takes a list of <c>LogEvent</c> objects and creates a single JSON string containing all logged events.
        /// </summary>
        /// <returns>
        /// A Base64 representation of the logged events in a JSON string.
        /// </returns>
        /// <param name="log">A list of LogEvent objects.</param>
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

            string postDataString = stringBuilder.ToString();
            stringBuilder.Length = 0;

            return btoa(postDataString);
        }

        /// <summary>
        /// Builds a string with a specified array of arguments with a given format.
        /// </summary>
        /// <returns>
        /// A single string of arguments in the given format.
        /// </returns>
        /// <param name="formatString">The format that <c>StringBuilder</c> will use to build the output string.</param>
        /// <param name="args">An object array of a variable length, where all arguments are used to build the output string.</param>
        public static string BuildUrlString(string formatString, params object[] args)
        {
            stringBuilder.AppendFormat(formatString, args);

            string urlString = stringBuilder.ToString();
            stringBuilder.Length = 0;

            return urlString;
        }

        /// <summary>
        /// Uses ISO encoding and converts the given string into a Base64 string.
        /// </summary>
        /// <returns>
        /// The given string converted to Base64.
        /// </returns>
        /// <param name="str">The string to convert to Base64.</param>
        public static string btoa(string str)
        {
            return System.Convert.ToBase64String(Encoding.GetEncoding(ISOEncodingId).GetBytes(str));
        }
    }
}
