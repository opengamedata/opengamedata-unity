using System;
using System.Collections.Generic;
using System.Text;

namespace FieldDay 
{
    /// <summary>
    /// Wrapper class for logged data.
    /// </summary>
    public class LogEvent : ILogEvent
    {
        /// <value>The data arrange in the format that the database will expect.</value>
        public Dictionary<string, string> Data { get; set; }
        
        /// <value>The <c>StringBuilder</c> used to format the data into a JSON string.</value>
        private static readonly StringBuilder stringBuilder = new StringBuilder();

        /// <summary>
        /// Organizes data into a new dictionary in the format that the database expects.
        /// </summary>
        /// <param name="data">A Dictionary of strings containing the logged data values.</param>
        /// <param name="category">An enum value representing the event category for the given data.</param>
        public LogEvent(Dictionary<string, string> data, Enum category)
        {
            Data = new Dictionary<string, string>() 
            {
                { "event", "CUSTOM" },
                { "event_custom", category.ToString() },
                { "event_data_complex", BuildEventDataString(data) }
            };
        }

        /// <summary>
        /// Builds a single JSON string containing each logged element from the given data.
        /// </summary>
        /// <returns>
        /// The input data formatted as a single JSON string.
        /// </returns>
        /// <param name="data">A Dictionary of strings containing the logged data values.</param>
        private string BuildEventDataString(Dictionary<string, string> data)
        {
            foreach (KeyValuePair<string, string> kvp in data)
            {
                stringBuilder.AppendFormat("{{\"{0}\":\"{1}\"}},", kvp.Key, kvp.Value);
            }

            // Remove trailing comma
            stringBuilder.Length--;

            string eventDataString = stringBuilder.ToString();
            stringBuilder.Length = 0;

            return eventDataString;
        }
    }
}
