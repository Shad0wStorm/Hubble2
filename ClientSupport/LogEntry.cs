using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientSupport
{
    /// Helper class used to send reports back to the server on client
    /// activity.
    /// 
    /// All values will be sent via a query string to the logging endpoint
    /// on the server. Therefore values should be encoded appropriately.
    /// 
    /// This is not done internally since the values will be logged
    /// directly on the server so it is important that anything logging
    /// information and anything using the information agree on the
    /// representation. Any processing done at this level would confuse the
    /// situation requiring the reader to perform processing not done by
    /// the sender. Where possible it is better to use keys/values that do
    /// not require encoding since that will allow the logs to be read
    /// without any processing.
    public class LogEntry
    {
        private Dictionary<String, object> m_values;
        public String Command = null;
        public bool IsCommand { get { return Command != null; } }

        /// <summary>
        /// Create a new log entry suitable for passing to LogValues.
        /// </summary>
        /// <param name="action">Name of the action occuring.</param>
        public LogEntry(String action)
        {
            m_values = new Dictionary<string, object>();
            m_values["action"] = action;
            if (action[0] == '@')
            {
                Command = action;
            }
        }

        public void AddValue(String key, object value)
        {
            m_values[key] = value;
        }

        public Dictionary<String, object> GetValues()
        {
            return m_values;
        }
    }
}
