using System;
using System.Collections.Generic;
using System.Text;
using BeauUtil;

namespace FieldDay 
{
    public class LogEvent : ILogEvent
    {
        private static readonly StringBuilder stringBuilder = new StringBuilder();
        
        public Dictionary<string, string> Data { get; set; }

        public LogEvent(Dictionary<string, string> data, Enum category)
        {
            Data = new Dictionary<string, string>() 
            {
                { "event", "CUSTOM" },
                { "event_custom", Convert.ToInt32(category).ToStringLookup() },
                { "event_data_complex", BuildEventDataString(data) }
            };
        }

        private string BuildEventDataString(Dictionary<string, string> data)
        {
            foreach (KeyValuePair<string, string> kvp in data)
            {
                stringBuilder.AppendFormat("{{\"{0}\":\"{1}\"}},", kvp.Key, kvp.Value);
            }

            // Remove trailing comma
            stringBuilder.Length--;
            return stringBuilder.Flush();
        }
    }
}
