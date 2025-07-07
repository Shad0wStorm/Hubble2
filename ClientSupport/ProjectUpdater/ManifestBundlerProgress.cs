using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using LocalResources;

namespace ClientSupport.ProjectUpdater
{
    class ManifestBundlerProgress
    {
        private CommandPriorityQueue m_queue;
        private ManifestBundler m_bundler;
        private ProgressMonitor m_monitor;
        private String m_name;
        private String m_vname;
        private long Progress;
        private long Validated;
        private bool IncludeValidatedProgress = true;
        private System.Timers.Timer m_monitorUpdate;
        private bool m_updateRequired = false;
        private Mutex m_timerMutex = null;
        private DateTime m_lastUpdate;

        public ManifestBundlerProgress(CommandPriorityQueue queue,
            ManifestBundler bundler,
            ProgressMonitor monitor,
            String name)
        {
            m_queue = queue;
            m_bundler = bundler;
            m_monitor = monitor;
            m_name = name;
            m_vname = m_name + "Secondary";
            m_timerMutex = new Mutex();
            m_lastUpdate = DateTime.Now;

            Progress = 0;
            Validated = 0;

            m_monitor.StartProgressAction(m_name,
                LocalResources.Properties.Resources.PU_ManifestContentsSync,
                bundler.DataSize, true);
            if (IncludeValidatedProgress)
            {
                m_monitor.StartProgressAction(m_vname,
                    LocalResources.Properties.Resources.PU_ManifestContentsSync,
                    bundler.DataSize, true);
            }

            m_queue.CommandCompletionEvent += ItemCompleted;
        }

        public void ItemCompleted(object sender)
        {
            m_timerMutex.WaitOne();
            DateTime now = DateTime.Now;
            TimeSpan diff = now - m_lastUpdate;
            if (diff.TotalSeconds > 1)
            {
                UpdateProgress();
                m_lastUpdate = now;
            }
            m_timerMutex.ReleaseMutex();
        }

        public void ItemCompletedTimer(object sender)
        {
            if (m_monitorUpdate == null)
            {
                m_timerMutex.WaitOne();
                // Make sure only the first thread through creates the timer.
                if (m_monitorUpdate==null)
                {
                    m_monitorUpdate = new System.Timers.Timer();
                    m_monitorUpdate.Elapsed += m_monitorUpdate_Elapsed;
                    m_monitorUpdate.Interval = 1000.0;
                    m_monitorUpdate.Start();
                }
                m_timerMutex.ReleaseMutex();
            }
            m_updateRequired = true;
        }

        void m_monitorUpdate_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (m_updateRequired)
            {
                UpdateProgress();
                m_updateRequired = false;
            }
        }

        void UpdateProgress()
        {
            long validatedProgress = m_bundler.ValidatedProgress;
            if ((validatedProgress > Validated) && (IncludeValidatedProgress))
            {
                Validated = validatedProgress;
                m_monitor.ReportActionProgress(m_name, Validated);
            }
            long progress = m_bundler.Progress;
            if (progress > Progress)
            {
                Progress = progress;
                m_monitor.ReportActionProgress(m_vname, Progress);
            }
        }

        public void CompleteAction()
        {
            // Make sure any outstanding/unreported progress is reported before
            // signaling completion.
            UpdateProgress();

            if (m_monitorUpdate!=null)
            {
                m_monitorUpdate.Stop();
                m_monitorUpdate.Elapsed -= m_monitorUpdate_Elapsed;
                m_monitorUpdate = null;
            }

            m_monitor.CompleteAction(m_name);
            if (IncludeValidatedProgress)
            {
                m_monitor.CompleteAction(m_vname);
            }
        }
    }
}
