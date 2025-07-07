using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientSupport
{
    public abstract class LogServer
    {
        /// <summary>
        /// Log a set of values with the server.
        /// 
        /// This can be used to record telemetry based on client actions such
        /// as start up/shutdown downloads and so on.
        /// 
        /// This hopefully goes without saying, but logs will be visible to the
        /// user so logged messages should be safe.
        /// </summary>
        /// <param name="details">User details for the logged on user.</param>
        /// <param name="entry">LogEntry object to send to the log.</param>
        public abstract void LogValues(UserDetails details, LogEntry entry);
    }
}
