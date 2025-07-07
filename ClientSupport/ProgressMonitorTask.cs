using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace ClientSupport
{
    public abstract class ProgressMonitorTask
    {
        /// <summary>
        /// Monitor interface used to send feedback on progress of the update.
        /// </summary>
        protected ProgressMonitor m_monitor;
        protected CultureInfo m_currentCulture;
        protected CultureInfo m_currentUICulture;

        public ProgressMonitorTask(ProgressMonitor monitor)
        {
            m_monitor = monitor;
        }

        /// <summary>
        /// Start the update process by running it on another thread from the
        /// thread pool.
        /// </summary>
        public void Go()
        {
            m_currentCulture = Thread.CurrentThread.CurrentCulture;
            m_currentUICulture = Thread.CurrentThread.CurrentUICulture;

            ThreadPool.QueueUserWorkItem(new WaitCallback(StartTask), this);
        }

        /// <summary>
        /// Static method used as the thread entry point for the thread queued
        /// in the Go method.
        /// </summary>
        /// <param name="updaterObject">
        /// The ProjectUpdate object that is performing the update.
        /// </param>
        public static void StartTask(Object taskObject)
        {
            ProgressMonitorTask task = taskObject as ProgressMonitorTask;

            if (task != null)
            {
                Thread.CurrentThread.CurrentCulture = task.m_currentCulture;
                Thread.CurrentThread.CurrentUICulture = task.m_currentUICulture;
                task.RunTask();
            }
        }


        public abstract void RunTask();
    }
}
