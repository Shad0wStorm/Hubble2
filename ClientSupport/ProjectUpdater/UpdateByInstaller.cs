using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using LocalResources;

namespace ClientSupport.ProjectUpdater
{
    class UpdateByInstaller
    {
        private UpdateStatus m_status;
        private TransferManager m_transfer;
        private ProgressMonitor m_monitor;

        public UpdateByInstaller(UpdateStatus status,
            TransferManager transfer,
            ProgressMonitor monitor)
        {
            m_status = status;
            m_transfer = transfer;
            m_monitor = monitor;
        }

        public void ProcessInstaller()
        {
            EnsureInstaller();
            m_status.CheckForCancellation();
            RunInstaller();
            m_status.CheckForCancellation();
        }

        /// <summary>
        /// Ensure that the required installer directory exists by creating it
        /// if it is not already present.
        /// </summary>
        public void EnsureInstallerDirectory()
        {
            if (m_status.m_success)
            {
                m_monitor.StartAction(m_status.Project.Name,
                    LocalResources.Properties.Resources.PU_CreateInstallerDir);

                try
                {
                    String directory = m_status.Project.InstallationDirectory;
                    if (!Directory.Exists(directory))
                    {
                        DirectoryInfo d = Directory.CreateDirectory(directory);
                        if (d == null)
                        {
                            m_status.SetError(LocalResources.Properties.Resources.PU_CreateInstallerDirFail);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    m_status.SetError(String.Format(LocalResources.Properties.Resources.PU_CreateInstallerDirExc,
                        ex.Message));
                }

                m_monitor.CompleteAction(m_status.Project.Name);
            }
        }

        /// <summary>
        /// Ensure the most recent installer is available.
        /// </summary>
        public void EnsureInstaller()
        {
            if (m_status.m_success)
            {
                EnsureInstallerDirectory();
                EnsureInstallerDownloaded();
            }
        }

        /// <summary>
        /// Ensure that the most recent installer has been downloaded ready
        /// to be installed.
        /// </summary>
        public void EnsureInstallerDownloaded()
        {
            if (m_status.m_success)
            {
                bool downloadInstaller = true;
                if (CheckForExistingInstaller())
                {
                    downloadInstaller = !ValidateExistingInstaller();
                }
                if (downloadInstaller)
                {
                    DownloadInstaller();
                    if (m_status.m_success)
                    {
                        bool valid = ValidateExistingInstaller();
                        if (!valid)
                        {
                            m_status.SetError(LocalResources.Properties.Resources.PU_ValidInstallerFail);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check whether the installer has been downloaded previously.
        /// </summary>
        /// <returns>
        /// True if there is an existing file with the name of the installer to
        /// be downloaded.
        /// </returns>
        public bool CheckForExistingInstaller()
        {
            bool result = false;

            if (m_status.m_success)
            {
                m_monitor.StartAction(m_status.Project.Name,
                    LocalResources.Properties.Resources.PU_InstallerCheck);

                m_status.InstalledFromFile(Path.Combine(m_status.Project.InstallationDirectory,
                    m_status.Remote.LocalFileName));
                result = File.Exists(m_status.InstallerFile);

                m_monitor.CompleteAction(m_status.Project.Name);
            }
            return result;
        }

        /// <summary>
        /// Validate the local copy of the installer.
        /// 
        /// Currently this just checks that the file exists and the size
        /// matches the expected size. It can be extended in the future to
        /// support MD5 hashes or another more accurate methods.
        /// </summary>
        /// <returns>
        /// True if the installer exists and is valid, false otherwise.
        /// </returns>
        public bool ValidateExistingInstaller()
        {
            bool result = false;
            if (m_status.m_success)
            {
                m_transfer.Log(new LogEntry("ValidateInstaller"));
                m_monitor.StartAction(m_status.Project.Name,
                    LocalResources.Properties.Resources.PU_ValidInstaller);
                FileInfo file = new FileInfo(m_status.InstallerFile);
                if (file.Exists)
                {
                    if (file.Length == m_status.Remote.FileSize)
                    {
                        // File exists, and has the correct size, assume it is
                        // correct.
                        result = true;

                        if (m_status.Remote.CheckSum != null)
                        {
                            // If we have a checksum we cannot currently
                            // validate it so assume we have invalid data.
                            long length;
                            DecoderRing ring = new DecoderRing();
                            String textHash = ring.MD5EncodeFile(m_status.InstallerFile, out length);
                            result = (textHash == m_status.Remote.CheckSum);
                        }
                    }
                }

                m_monitor.CompleteAction(m_status.Project.Name);
            }
            return result;
        }

        /// <summary>
        /// Download the installer from the remote server to the local
        /// installation directory.
        /// </summary>
        public void DownloadInstaller()
        {
            if (m_status.m_success)
            {
                m_transfer.Log(new LogEntry("DownloadInstaller"));

                m_monitor.StartProgressAction(m_status.Project.Name,
                    LocalResources.Properties.Resources.PU_DownloadInstaller,
                    m_status.Remote.FileSize, true);

                try
                {
                    DownloadManagerBase.DownloadStatus dls = new DownloadManagerBase.DownloadStatus();
                    m_transfer.DownloadFile(ref dls, m_status.Remote, true, m_status.UpdateMonitorProgress);
                    if (!dls.Success)
                    {
                        m_status.SetError(dls.Error);
                    }
                }
                catch (System.Exception se)
                {
                    // If any errors occurred during transfer report them.
                    m_status.SetError(String.Format(LocalResources.Properties.Resources.PU_DownloadInstallerExc,
                        se.Message));
                    m_transfer.Log(new LogEntry("DownloadInstallerException"));
                }

                m_monitor.CompleteAction(m_status.Project.Name);
                m_transfer.Log(new LogEntry("DownloadInstallerCompleted"));
            }
        }

        /// <summary>
        /// Run the local version of the installer.
        /// </summary>
        public void RunInstaller()
        {
            if (m_status.m_success)
            {
                m_monitor.StartAction(m_status.Project.Name,
                    LocalResources.Properties.Resources.PU_InstallationExecute);
                m_transfer.Log(new LogEntry("RunInstaller"));

                try
                {
                    ProcessStartInfo pstart = new ProcessStartInfo();
                    String extension = Path.GetExtension(m_status.InstallerFile);
                    pstart.FileName = m_status.InstallerFile;
                    if ((extension == "exe") || (extension == "msi"))
                    {
                        pstart.WorkingDirectory = m_status.Project.ProjectDirectory;
                        pstart.UseShellExecute = false;
                        pstart.EnvironmentVariables.Add("TARGETDIRECTORY",
                            m_status.Project.ProjectDirectory);
                    }
                    else
                    {
                        pstart.UseShellExecute = true;
                    }
                    pstart.Arguments = m_status.Remote.LaunchArguments;

                    Process pid = Process.Start(pstart);
                    pid.WaitForExit();
                    if (pid.ExitCode != 0)
                    {
                        m_status.SetError(String.Format(LocalResources.Properties.Resources.PU_InstallationFailed,
                            pid.ExitCode));
                        LogEntry lf = new LogEntry("InstallerFailed");
                        lf.AddValue("exitcode", pid.ExitCode);
                        m_transfer.Log(lf);
                    }
                }
                catch (System.Exception ex)
                {
                    m_status.SetError(String.Format(LocalResources.Properties.Resources.PU_InstallationExc,
                        ex.Message));
                    LogEntry lf = new LogEntry("InstallerException");
                    lf.AddValue("exception", ex.Message);
                    m_transfer.Log(lf);
                }

                m_monitor.CompleteAction(m_status.Project.Name);
                m_transfer.Log(new LogEntry("InstallerCompleted"));
            }
        }
    }
}
