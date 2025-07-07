using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using LocalResources;

namespace ClientSupport.ProjectUpdater
{
    /// <summary>
    /// Class for managing the uninstall portions of an update as required.
    /// 
    /// Note that the actual uninstall is currently performed by the Project
    /// object. It may be neater to move the uninstall process to this class
    /// and/or associate it with the relevant update class when separate
    /// classes exist for Installer and Manifest based installations.
    /// </summary>
    class UninstallManager
    {
        private UpdateStatus m_status;
        private ProgressMonitor m_monitor;
        private FileOps m_fileops;
        private ProjectUpdateLog m_pulog;

        public UninstallManager(UpdateStatus status, ProgressMonitor monitor,
            FileOps ops, ProjectUpdateLog log)
        {
            m_status = status;
            m_monitor = monitor;
            m_fileops = ops;
            m_pulog = log;
        }

        public void Uninstall(UpdateStatus.UpdateMode mode)
        {
            if (m_status.m_success)
            {
                m_pulog.Log("StartUninstall", null);

                m_monitor.StartAction(m_status.Project.Name,
                    LocalResources.Properties.Resources.PU_UninstallingGame);

                // If we are using a replacement download manager (generally
                // only when testing) make sure the project thinks the new
                // version is the same type as we are actually going to
                // install rather than whatever the real server is telling us.
                bool silent = mode == UpdateStatus.UpdateMode.UninstallSilent;
                m_pulog.Log("StartRunUninstaller", null);
                String uninstallResult = m_status.Project.Uninstall(silent);
                m_pulog.Log("FinishRunUninstaller", null);
                m_monitor.CompleteAction(m_status.Project.Name);

                if (uninstallResult == null)
                {
                    if (Directory.Exists(m_status.Project.ProjectDirectory))
                    {
                        m_monitor.StartProgressAction(m_status.Project.Name, LocalResources.Properties.Resources.PU_UninstallingGame, 100);
                        m_pulog.Log("StartCountContents", null);
                        long found = m_fileops.CountContents(m_status.Project.ProjectDirectory, m_status, true);
                        m_pulog.Log("FinishCountContents", null);
                        m_monitor.StartProgressAction(m_status.Project.Name, LocalResources.Properties.Resources.PU_UninstallingGame, found);
                        long progress = 0;
                        m_pulog.Log("StartRemoveContents", null);
                        uninstallResult = m_fileops.RemoveDirectory(m_status.Project.ProjectDirectory, m_status, ref progress);
                        m_pulog.Log("FinishRemoveContents", null);
                        m_monitor.CompleteAction(m_status.Project.Name);
                    }
                }
                if (uninstallResult != null)
                {
                    m_status.SetError(uninstallResult);
                }

                m_pulog.Log("FinishUninstall", null);

            }

        }

        public void UninstallIfRequired()
        {
            if (m_status.m_success)
            {
                m_monitor.StartAction(m_status.Project.Name,
                    LocalResources.Properties.Resources.PU_UninstallPrevious);

                // If we are using a replacement download manager generally
                // only when testing make sure the project thinks the new
                // version is the same type as we are actually going to
                // install rather than whatever the real server is telling us.
                m_status.Project.SetRemoteInstallerType(m_status.Remote.LocalFileName);
                String uninstallResult = m_status.Project.UninstallIfRequired();
                if (!String.IsNullOrEmpty(uninstallResult))
                {
                    m_status.SetError(uninstallResult);
                }

                m_monitor.CompleteAction(m_status.Project.Name);
            }
        }

    }
}
