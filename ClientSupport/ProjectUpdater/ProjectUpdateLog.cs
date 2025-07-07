using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ClientSupport.ProjectUpdater
{
    /// <summary>
    /// Class responsible for logging detailed information about the update
    /// process for debugging purposes.
    /// </summary>
    class ProjectUpdateLog
    {
        private FileLogger m_updateLog = null;
        private UserDetails m_user = null;
		private Mutex m_logAccess = null;

        public ProjectUpdateLog(String updateLogPath, UserDetails user)
        {
            if (updateLogPath != null)
            {
                m_user = user;
                m_updateLog = new FileLogger();
				m_logAccess = new Mutex();
                m_updateLog.SetLogFile(updateLogPath, 0);
                m_updateLog.Log(m_user, new LogEntry("CreatedProjectUpdater"));
            }
        }

		private void SendToLog(LogEntry entry)
		{
			if (!String.IsNullOrEmpty(Thread.CurrentThread.Name))
			{
				entry.AddValue("ThreadName", Thread.CurrentThread.Name);
			}
			m_logAccess.WaitOne();
			m_updateLog.Log(m_user, entry);
			m_logAccess.ReleaseMutex();
		}

        public void Log(LogEntry entry)
        {
            if (m_updateLog == null)
            {
                return;
            }

			SendToLog(entry);
        }

        public void Log(String action, String file)
        {
            if (m_updateLog == null)
            {
                return;
            }

            LogEntry le = new LogEntry(action);
            if (!String.IsNullOrEmpty(file))
            {
                le.AddValue("File", file);
            }
			SendToLog(le);
        }
    }
}
