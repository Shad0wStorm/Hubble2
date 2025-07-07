using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientSupport
{
    public class MultiLogger : BaseLogger
    {
        List<BaseLogger> m_logs;

        public MultiLogger()
        {
            m_logs = new List<BaseLogger>();
        }

        public void AddLogger(BaseLogger logger)
        {
            if (!m_logs.Contains(logger))
            {
                m_logs.Add(logger);
            }
        }

        public void RemoveLogger(BaseLogger logger)
        {
            if (m_logs.Contains(logger))
            {
                m_logs.Remove(logger);
            }
        }

        public override void Log(UserDetails user, LogEntry entry)
        {
            foreach (BaseLogger logger in m_logs)
            {
                logger.Log(user, entry);
            }
        }
    }
}
