using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ManifestTool
{
    public class WorkerBase
    {
        public event RunWorkerCompletedEventHandler TaskCompleted;

        protected BackgroundWorker m_worker;
        protected ProgressWindow m_progressWindow;
        protected String m_action;

        public WorkerBase()
        {
            m_worker = new BackgroundWorker();
            m_worker.DoWork += DoWork;
            m_worker.WorkerReportsProgress = true;
            m_worker.WorkerSupportsCancellation = true;
            m_worker.ProgressChanged += ProgressUpdated;
            m_worker.RunWorkerCompleted += CompletedTask;

            m_progressWindow = new ProgressWindow();
            m_progressWindow.Worker = m_worker;
        }

        public virtual void Run()
        {
            m_progressWindow.Show();

            m_worker.RunWorkerAsync();
        }

        public void DoWork(object sender, DoWorkEventArgs e)
        {
            if (sender == m_worker)
            {
                ExecuteTask(e);
            }
        }

        public virtual void ExecuteTask(DoWorkEventArgs e)
        {
        
        }

        public void ProgressUpdated(object sender, ProgressChangedEventArgs e)
        {
            m_progressWindow.Progress = e.ProgressPercentage;
            m_progressWindow.Action = m_action;
        }

        public virtual void CompletedTask(object sender, RunWorkerCompletedEventArgs e)
        {
            m_progressWindow.Close();
            if (TaskCompleted != null)
            {
                TaskCompleted(this, e);
            }
        }
    }
}
