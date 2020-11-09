using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using BeauUtil;

namespace FieldDay
{
    public class SimpleLogUtils
    {
        [DllImport("__Internal")]
        public static extern string GetCookie(string name);

        [DllImport("__Internal")]
        public static extern void SetCookie(string name, string val, int days);
        
        private const int ISOEncodingId = 28591;
        private static StringBuilder stringBuilder = new StringBuilder();

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

        // https://stackoverflow.com/questions/39111586/stringbuilder-appendformat-ienumarble
        public static string BuildUrlString(string formatString, params object[] args)
        {
            stringBuilder.AppendFormat(formatString, args);
            return stringBuilder.Flush();
        }

        // https://stackoverflow.com/questions/46093210/c-sharp-version-of-the-javascript-function-btoa
        public static string btoa(string str)
        {
            return System.Convert.ToBase64String(Encoding.GetEncoding(ISOEncodingId).GetBytes(str));
        }
    }
}
