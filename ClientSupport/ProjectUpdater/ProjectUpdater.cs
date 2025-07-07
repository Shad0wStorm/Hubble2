using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using LocalResources;

namespace ClientSupport.ProjectUpdater
{
    public class UpdateOptions
    {
        public bool DisableFastDownload = false;
    }

    /// <summary>
    /// Perform the tasks necessary to update a project to the latest version
    /// provided by the server.
    /// </summary>
    class ProjectUpdater : ProgressMonitorTask
    {
        /// <summary>
        /// Status object used to track the current state of the update process.
        /// </summary>
        private UpdateStatus m_status;

        /// <summary>
        /// An error message giving details of any problem that caused the
        /// update to fail.
        /// </summary>
        public String Error { get { return m_status.m_error; } }

        /// <summary>
        /// Internal indicator as to whether the update has completed
        /// successfully.
        /// 
        /// This is used to skip subsequent steps in the install process if an
        /// earlier step has failed.
        /// </summary>
        public bool Success { get { return m_status.m_success; } }

        private int m_changes = 0;
        public int Changes { get { return m_changes; } }

        private ProjectUpdateLog m_pulog = null;
        private FileOps m_fileops = null;
        private TransferManager m_transfer = null;

        UpdateOptions m_options;

        /// <summary>
        /// Constructor.
        /// 
        /// Store references to the external information required to perform
        /// the update.
        /// </summary>
        /// <param name="project">The project being updated.</param>
        /// <param name="user">The user performing the update.</param>
        /// <param name="server">The server connection to use.</param>
        /// <param name="monitor">The monitor to use for feedback.</param>
        public ProjectUpdater(Project project, UserDetails user,
            DownloadManagerBase downloader,
            ProgressMonitor monitor,
            String updateLogPath,
            UpdateOptions options) : base(monitor)
        {
            m_options = options;
            m_status = new UpdateStatus(monitor,project);
            m_status.m_mode = UpdateStatus.UpdateMode.DownloadAndInstall;
            m_status.Remote = new DownloadManagerBase.RemoteFileDetails();

            m_pulog = new ProjectUpdateLog(updateLogPath, user);
            m_fileops = new FileOps(m_pulog);
            m_transfer = new TransferManager(downloader, user, m_status,
                m_monitor,m_pulog,m_fileops);
        }

        public void SetMode(UpdateStatus.UpdateMode mode)
        {
            m_status.m_mode = mode;
        }

        /// <summary>
        /// Ensure the project is ready to run.
        /// 
        /// Note that this is a blocking task and does not run in the
        /// background.
        /// 
        /// Insert some evilness. If we use the full upgrade then the first
        /// play after an install/update will trigger a rebuild of the cache.
        /// For a minor update the cost may not be high but for an install
        /// this can take a while and is not strictly necessary.
        /// Therefore disable the cache for the duration and renable afterwards.
        /// </summary>
        public void PreFlightCheck()
        {
            m_status.m_mode = UpdateStatus.UpdateMode.PreFlightCheck;
            DownloadManagerBase.BehaviourModifications previous = m_transfer.Downloader.Modifications;
            m_transfer.Downloader.Modifications = new DownloadManagerBase.BehaviourModifications(false);
            RunTask();
            m_transfer.Downloader.Modifications = previous;
        }

        /// <summary>
        /// Static method used as the thread entry point for the thread queued
        /// in the Go method.
        /// </summary>
        /// <param name="updaterObject">
        /// The ProjectUpdate object that is performing the update.
        /// </param>
        public override void RunTask()
        {
            DateTime start = DateTime.Now;
            m_pulog.Log("UpdateThreadStarted", null);
            m_status.Begin();
            if (m_transfer.HasDownloader)
            {
                m_pulog.Log("StartBeginDownloadBatch", null);
                m_monitor.StartAction(m_status.Project.Name,
                    LocalResources.Properties.Resources.PU_PrepareForUpdate);
                m_transfer.BeginDownloadBatch();
                m_monitor.CompleteAction(m_status.Project.Name);
                m_pulog.Log("FinishBeginDownloadBatch", null);
            }
            try
            {
                switch (m_status.m_mode)
                {
                    case UpdateStatus.UpdateMode.DownloadAndInstall:
                    case UpdateStatus.UpdateMode.PreFlightCheck:
                        {
                            DownloadAndInstall();
                            break;
                        }
                    case UpdateStatus.UpdateMode.Uninstall:
                    case UpdateStatus.UpdateMode.UninstallSilent:
                        {
                            UninstallManager uninstaller = new UninstallManager(m_status, m_monitor, m_fileops, m_pulog);
                            uninstaller.Uninstall(m_status.m_mode);
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            catch (System.Exception ex)
            {
                m_status.SetError(String.Format(LocalResources.Properties.Resources.PU_UpdateException,
                    ex.Message));
            }
            if (m_transfer.Downloader != null)
            {
                m_pulog.Log("StartEndDownloadBatch", null);
                m_monitor.StartAction(m_status.Project.Name,
                    LocalResources.Properties.Resources.PU_FinaliseUpdate);
                m_transfer.EndDownloadBatch();
                m_monitor.CompleteAction(m_status.Project.Name);
                m_pulog.Log("FinishEndDownloadBatch", null);
            }
            m_status.Finalise();
            DateTime end = DateTime.Now;
            TimeSpan difference = end - start;
            LogEntry complete = new LogEntry("UpdateThreadFinished");
            complete.AddValue("Duration",difference.TotalSeconds);
            m_pulog.Log(complete);
        }

        private void DownloadAndInstall()
        {
            m_fileops.CheckWritable(m_status);
            if (m_status.m_success)
            {
                m_status.CheckForCancellation();
                m_transfer.DetermineInstaller();
                m_status.CheckForCancellation();
                UninstallManager uninstaller = new UninstallManager(m_status,
                    m_monitor, m_fileops, m_pulog);
                uninstaller.UninstallIfRequired();
                m_status.CheckForCancellation();
                if (m_status.IsInstaller())
                {
                    ProcessInstaller();
                }
                else
                {
                    ProcessManifest();
                }
            }
        }

        private void ProcessInstaller()
        {
            UpdateByInstaller installer = new UpdateByInstaller(m_status, m_transfer, m_monitor);
            installer.ProcessInstaller();
        }

        public void ProcessManifest()
        {
            UpdateByManifest ubm = new UpdateByManifest(m_status, m_transfer,
                m_monitor, m_pulog, m_fileops);

            ubm.ProcessManifest(m_options);
            m_changes = ubm.Changes;
        }
    }
}
