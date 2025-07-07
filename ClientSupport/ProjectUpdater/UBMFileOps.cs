using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ClientSupport.ProjectUpdater
{
    /// <summary>
    /// Implementation of ManifestFileOps interface used during an actual
    /// update.
    /// 
    /// Most operations do not require guarding since one thread handles all
    /// the files in a bundle so for a clash to occur the same file would have
    /// to be in multiple bundles which should only occur if the manifest was
    /// invalid - it would need to have multiple entries for the same file with
    /// different hashes/sizes.
    /// 
    /// The exception to this is directories which are created on demand and
    /// since different bundles could contain files in the same directory we
    /// need to be certain we are not creating a directory which another thread
    /// is attempting to create at the same time.
    /// </summary>
    class UBMFileOps : ManifestFileOps
    {

        Mutex m_dirlock = new Mutex();

        UpdateStatus m_status;
        FileOps m_fileOps;
        TransferManager m_transfer;

        private int m_validations;
        private int m_copies;
        private int m_downloads;

        public UBMFileOps(UpdateStatus status, FileOps ops,
            TransferManager transfer)
        {
            m_status = status;
            m_fileOps = ops;
            m_transfer = transfer;
            m_validations = 0;
            m_copies = 0;
            m_downloads = 0;
        }

        public void ReportStatistics()
        {
            LogEntry stats = new LogEntry("FileOperationStatistics");
            stats.AddValue("Validations", m_validations);
            stats.AddValue("Copies",m_copies);
            stats.AddValue("Downloads", m_downloads);
            Log(stats);
        }

        private void EnsureDirectory(String path)
        {
            m_dirlock.WaitOne();
            m_fileOps.EnsureDirectory(path);
            m_dirlock.ReleaseMutex();
        }

        public void RemoveFile(ManifestFile.ManifestEntry entry)
        {
            DownloadManagerBase.RemoteFileDetails details = GetRemoteFileDetails(entry);
            m_fileOps.RemoveFile(details.LocalFileName);
        }

        public bool Exists(ManifestFile.ManifestEntry entry)
        {
            DownloadManagerBase.RemoteFileDetails details = GetRemoteFileDetails(entry);
            return System.IO.File.Exists(details.LocalFileName);
        }

        public void CopyFile(ManifestFile.ManifestEntry source, ManifestFile.ManifestEntry target)
        {
            DownloadManagerBase.RemoteFileDetails sourced = GetRemoteFileDetails(source);
            DownloadManagerBase.RemoteFileDetails targetd = GetRemoteFileDetails(target);
            String parent = System.IO.Path.GetDirectoryName(targetd.LocalFileName);
            EnsureDirectory(parent);
            System.IO.File.Copy(sourced.LocalFileName, targetd.LocalFileName);
            ++m_copies;
        }

        public bool ValidateFile(ManifestFile.ManifestEntry entry)
        {
            ++m_validations;
            DownloadManagerBase.RemoteFileDetails details = GetRemoteFileDetails(entry);
            if (System.IO.File.Exists(details.LocalFileName))
            {
                DecoderRing ring = new DecoderRing();
                long length;
                String hash = ring.SHA1EncodeFile(details.LocalFileName, out length);
                if ((length == entry.Size) && (hash == entry.Hash))
                {
                    return true;
                }
            }
            return false;
        }

        public bool DownloadFile(ManifestFile.ManifestEntry entry, UpdateStatus.ProgressUpdate update,
            ref DownloadManagerBase.DownloadStatus dls)
        {
            DownloadManagerBase.RemoteFileDetails details = GetRemoteFileDetails(entry);
            String parent = System.IO.Path.GetDirectoryName(details.LocalFileName);
            EnsureDirectory(parent);
            m_transfer.DownloadFile(ref dls, details, true, update);
            ++m_downloads;
            return dls.Success;
        }

        public long GetFreeSpace(String path)
        {
            string drive = System.IO.Path.GetPathRoot(System.IO.Path.GetFullPath(path));
            System.IO.DriveInfo targetDrive = new System.IO.DriveInfo(drive);
            return targetDrive.AvailableFreeSpace;
        }

        public DownloadManagerBase.RemoteFileDetails GetRemoteFileDetails(ManifestFile.ManifestEntry entry)
        {
            DownloadManagerBase.RemoteFileDetails details = new DownloadManagerBase.RemoteFileDetails();
            details.CheckSum = entry.Hash;
            details.FileSize = entry.Size;
            details.RemotePath = entry.Download;

            if (entry.AccessCookies != null)
            {
                details.AccessCookies = entry.AccessCookies;
            }

            String localFile = System.IO.Path.Combine(m_status.Project.ProjectDirectory, entry.Path);
            if (entry.Path.ToLowerInvariant() == "versioninfo.txt")
            {
                localFile += ".new";
            }
            details.LocalFileName = localFile;
            return details;
        }

        public bool CancelRequested()
        {
            return m_status.Monitor.CancellationRequested();
        }

        public void SetError(String error)
        {
            m_status.SetError(error);
        }

        public void Log(LogEntry entry)
        {
			if (!String.IsNullOrEmpty(Thread.CurrentThread.Name))
			{
				entry.AddValue("Thread", Thread.CurrentThread.Name);
			}
            m_transfer.PULog(entry);
        }
    }
}
