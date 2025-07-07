using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientSupport.ProjectUpdater
{
    class PartialDownloadProgress
    {
        ProgressMonitor m_monitor;
        long m_initial;
        String m_name;

        public PartialDownloadProgress(ProgressMonitor monitor, String name)
        {
            m_monitor = monitor;
            m_name = name;
            m_initial = 0;
        }

        public void SetInitial(long initial)
        {
            m_initial = initial;
        }

        public void UpdateProgress(long progress, long total)
        {
            if (progress <= total)
            {
                m_monitor.ReportActionProgress(m_name, m_initial + progress);
            }
        }
    }
}
