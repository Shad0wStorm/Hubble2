using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ClientSupport
{
    public class FileLogger : BaseLogger
    {
        private String m_logPath;
        private const long c_maxLogSize = 1024 * 1024 * 128;
        private bool m_enabled = true;

        public FileLogger()
        {

        }

        /// <summary>
        /// Create a log writing to a specific path name.
        /// </summary>
        /// <param name="path">The path of the log file to use.</param>
        /// <param name="limit">
        /// The maximum length of the file if this is exceeded when the log is
        /// initialised the existing file will be deleted and a new one
        /// created. Set to zero to always create a new file.
        /// The file can exceed the limit during a run to ensure that all data
        /// for that run is always preserved.
        /// </param>
        public void SetLogFile(String path, long limit = c_maxLogSize)
        {
            m_logPath = path;
            bool reset = false;
            FileInfo info = new FileInfo(m_logPath);
            if (info.Exists)
            {
                if (info.Length > limit)
                {
                    File.Delete(m_logPath);
                    reset = true;
                }
            }
            Log(null, new LogEntry("LogStarted"));
            if (reset)
            {
                Log(null, new LogEntry("LogReset"));
            }
        }

        /// <summary>
        /// Set the log file for the common case where we have a manager that
        /// is responsible for locating files, and a module that will be used
        /// as the file leaf name to avoid each standard use performing the
        /// same processing.
        /// </summary>
        /// <param name="manager">
        /// Manager used to find the log directory.
        /// </param>
        /// <param name="module">
        /// Name of the log file within the log directory. No extension is
        /// added so if one is required it should be provided as part of the
        /// module name.
        /// </param>
        public void SetLogFile(FORCManager manager, String module)
        {
            try
            {
                String path = manager.GetLocalDirectory("logs", false);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                if (Directory.Exists(path))
                {
                    SetLogFile(Path.Combine(path, module));
                }
            }
            catch (System.Exception)
            {
            }
        }

        /// <summary>
        /// Write a log entry to the file.
        /// </summary>
        /// <param name="user">
        /// User details. May be null if the user has not yet logged in yet or
        /// otherwise been authenticated.
        /// </param>
        /// <param name="entry">Entry to send to the log.</param>
        public override void Log(UserDetails user, LogEntry entry)
        {
            if (!entry.IsCommand)
            {
                if (m_enabled)
                {
                    DateTime logTime = DateTime.UtcNow;

                    entry.AddValue("date", logTime.ToString("yyyyMMdd"));
                    entry.AddValue("time", logTime.ToString("HHmmss"));

                    String message = logTime.ToString("yyyyMMdd/HHmmss:");

                    Dictionary<String, object> values = entry.GetValues();

                    String valueBlock = "";
                    foreach (String key in values.Keys)
                    {
                        if (!String.IsNullOrEmpty(valueBlock))
                        {
                            valueBlock = valueBlock + ", ";
                        }
                        valueBlock = valueBlock + "\"" + key + "\" : \"" + values[key] + "\"";
                    }
                    message = message + " { " + valueBlock + " } ;";

                    try
                    {
                        using (StreamWriter sw = File.AppendText(m_logPath))
                        {
                            sw.WriteLine(message);
                        }
                    }
                    catch (System.Exception)
                    {
                    	// Silently fail to write log if it is unwritable,
                        // e.g. due to permissions issues.
                        // May prevent some 
                    }
                }
            }
            else
            {
                if (entry.Command == "@FileOff")
                {
                    m_enabled = false;
                }
                if (entry.Command == "@FileOn")
                {
                    m_enabled = true;
                }
            }
        }
    }
}
