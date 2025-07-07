using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientSupport.ProjectUpdater
{
    /// <summary>
    /// Interface used by the manifest commands to make it easier to drop in
    /// alternate implementations.
    /// </summary>
    public interface ManifestFileOps
    {
        void RemoveFile(ManifestFile.ManifestEntry entry);
        bool Exists(ManifestFile.ManifestEntry entry);
        void CopyFile(ManifestFile.ManifestEntry source, ManifestFile.ManifestEntry target);
        bool ValidateFile(ManifestFile.ManifestEntry entry);
        bool DownloadFile(ManifestFile.ManifestEntry entry, UpdateStatus.ProgressUpdate update,
            ref DownloadManagerBase.DownloadStatus status);
        DownloadManagerBase.RemoteFileDetails GetRemoteFileDetails(ManifestFile.ManifestEntry entry);
        long GetFreeSpace(String path);
        bool CancelRequested();
        void SetError(String error);
        void Log(LogEntry entry);
    }
}
