using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using ClientSupport;
using ClientSupport.ProjectUpdater;

namespace ClientSupport.Tests.ProjectUpdater
{
    class ManifestEntryContext : ManifestFile.ManifestEntry
    {
        public object Context;
    }

    class ManifestFileOps_Mock : ManifestFileOps
    {
        public int Count
        {
            get
            {
                return Action.Count;
            }
        }

        public List<String> Action = new List<String>();
        private Dictionary<String, ManifestFile.ManifestEntry> m_fileSystem;
        private List<ManifestFile.ManifestEntry> m_alternateCopy;
        private Dictionary<String, ManifestFile.ManifestEntry> m_download;
        private List<ManifestFile.ManifestEntry> m_consumableDownload;

        private int m_mindelay;
        private int m_maxdelay;

        private Mutex m_lock;

        public enum SpecialActions
        {
            Skip = -1,
            IOException = -2
        };

        public ManifestFileOps_Mock(int delay = 0, int delay2 = 0)
        {
            m_fileSystem = new Dictionary<String, ManifestFile.ManifestEntry>();
            m_alternateCopy = new List<ManifestFile.ManifestEntry>();
            m_download = new Dictionary<String, ManifestFile.ManifestEntry>();
            m_consumableDownload = new List<ManifestFile.ManifestEntry>();
            if (delay > delay2)
            {
                m_mindelay = delay2;
                m_maxdelay = delay;
            }
            else
            {
                m_mindelay = delay;
                m_maxdelay = delay2;
            }
            m_lock = new Mutex();
        }

        public void ExistingFile(ManifestFile.ManifestEntry entry)
        {
            m_lock.WaitOne();
            if (!m_fileSystem.ContainsKey(entry.Path))
            {
                m_fileSystem[entry.Path] = entry;
            }
            m_lock.ReleaseMutex();
        }

        public void ReplaceFile(ManifestFile.ManifestEntry entry)
        {
            m_lock.WaitOne();
            m_fileSystem[entry.Path] = entry;
            m_lock.ReleaseMutex();
        }

        public void CopySource(ManifestFile.ManifestEntry entry)
        {
            m_lock.WaitOne();
            m_alternateCopy.Add(entry);
            m_lock.ReleaseMutex();
        }

        public void AddRemoteFile(ManifestFile.ManifestEntry entry)
        {
            m_lock.WaitOne();
            m_download[entry.Download] = entry;
            m_lock.ReleaseMutex();
        }

        public void AddRemoteFileInstance(ManifestFile.ManifestEntry entry)
        {
            m_lock.WaitOne();
            m_consumableDownload.Add(entry);
            m_lock.ReleaseMutex();
        }

        public void AddAction(ManifestFile.ManifestEntry entry, String action)
        {
            ManifestEntryContext context = entry as ManifestEntryContext;
            String prefix = "";
            if (context != null)
            {
                ManifestBundle bundle = context.Context as ManifestBundle;
                if (bundle != null)
                {
                    prefix = bundle.Information;
                }
            }
            // Pause for a random delay to add some uncertainty to proceedings
            if ((m_mindelay >= 0) && (m_maxdelay > 0))
            {
                Random r = new Random();
                Thread.Sleep(r.Next(m_mindelay, m_maxdelay));
            }
            m_lock.WaitOne();
            Action.Add(prefix + action);
            m_lock.ReleaseMutex();
        }

        #region ManifestFileOps_Implementation
        public void RemoveFile(ManifestFile.ManifestEntry entry)
        {
            m_lock.WaitOne();
            if (m_fileSystem.ContainsKey(entry.Path))
            {
                m_fileSystem.Remove(entry.Path);
                AddAction(entry, "--" + entry.Path);
            }
            m_lock.ReleaseMutex();
        }

        public bool Exists(ManifestFile.ManifestEntry entry)
        {
            m_lock.WaitOne();
            bool r = m_fileSystem.ContainsKey(entry.Path);
            m_lock.ReleaseMutex();

            return r;
        }

        public void CopyFile(ManifestFile.ManifestEntry source, ManifestFile.ManifestEntry target)
        {
            m_lock.WaitOne();
            ManifestFile.ManifestEntry found = null;
            if (m_alternateCopy.Count > 0)
            {
                // Check to see if we have an alternate source for the copy
                // so we can replicate file copy errors for testing.
                // Note that this uses the target path, not the source path
                // since we should write each target path once. We use a list
                // since if we move to support retrying in some form we may
                // retry the same file multiple times.
                if (target.Path == m_alternateCopy[0].Path)
                {
                    found = m_alternateCopy[0];
                    m_alternateCopy.RemoveAt(0);
                    if (found.Size < 0)
                    {
                        long action = found.Size;
                        found = null;
                        switch (action)
                        {
                            case (long)SpecialActions.Skip:
                                {
                                    break;
                                }
                            case (long)SpecialActions.IOException:
                                {
                                    m_lock.ReleaseMutex();
                                    throw new System.IO.IOException("CopyFailed");
                                }
                        }
                    }
                }
            }
            if (found == null)
            {
                if (!m_fileSystem.TryGetValue(source.Path, out found))
                {
                    found = null;
                }
            }
            if (found != null)
            {
                ManifestFile.ManifestEntry copy = new ManifestFile.ManifestEntry();
                copy.Hash = found.Hash;
                copy.Size = found.Size;
                copy.Path = target.Path;
                copy.Download = target.Download;
                AddAction(source, "+C+" + source.Path + ">" + target.Path);
                ExistingFile(copy);
            }
            else
            {
                AddAction(source, "+C-" + source.Path + ">" + target.Path);
            }
            m_lock.ReleaseMutex();
        }

        public bool ValidateFile(ManifestFile.ManifestEntry entry)
        {
            m_lock.WaitOne();
            ManifestFile.ManifestEntry found = null;

            if (m_fileSystem.TryGetValue(entry.Path, out found))
            {
                if ((found.Hash == entry.Hash) && (found.Size == entry.Size))
                {
                    AddAction(entry, "==" + entry.Path);
                    m_lock.ReleaseMutex();
                    return true;
                }
            }
            AddAction(entry, "=!" + entry.Path);
            m_lock.ReleaseMutex();
            return false;
        }

        public bool DownloadFile(ManifestFile.ManifestEntry entry, UpdateStatus.ProgressUpdate update,
            ref DownloadManagerBase.DownloadStatus dls)
        {
            m_lock.WaitOne();
            ManifestFile.ManifestEntry found = null;
            for (int d = 0; d < m_consumableDownload.Count; ++d)
            {
                if (m_consumableDownload[d].Download == entry.Download)
                {
                    ReplaceFile(m_consumableDownload[d]);
                    AddAction(entry, "<<" + entry.Download);
                    m_consumableDownload.RemoveAt(d);
                    m_lock.ReleaseMutex();
                    update(entry.Size, entry.Size);
                    return true;
                }
            }
            if (m_download.TryGetValue(entry.Download, out found))
            {
                ReplaceFile(found);
                AddAction(entry, "<<" + entry.Download);
                m_lock.ReleaseMutex();
                update(entry.Size, entry.Size);
                return true;
            }
            AddAction(entry, "<!MissingFile:" + entry.Download);
            m_lock.ReleaseMutex();
            return false;
        }

        public long GetFreeSpace(String path)
        {
            // Just assume we have all the space we need for now.
            return 1000000;
        }

        public DownloadManagerBase.RemoteFileDetails GetRemoteFileDetails(ManifestFile.ManifestEntry entry)
        {
            DownloadManagerBase.RemoteFileDetails details = new DownloadManagerBase.RemoteFileDetails();
            details.CheckSum = entry.Hash;
            details.FileSize = entry.Size;
            details.RemotePath = entry.Download;
            details.LocalFileName = entry.Path;
            return details;
        }

        public bool CancelRequested()
        {
            return false;
        }

        public void SetError(String error)
        {
            m_lock.WaitOne();
            Action.Add("!!!" + error);
            m_lock.ReleaseMutex();
        }

        public void Log(LogEntry entry)
        {

        }
        #endregion
    }
}
