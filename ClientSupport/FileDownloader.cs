using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ClientSupport
{
    /// <summary>
    /// Download management class.
    /// 
    /// This is no longer used, it was used in the original Fusion application
    /// but is not used in the new Fission client.
    /// </summary>
    public class FileDownloader
    {
        private String m_name;
        private String m_directory;
        private String m_source;
        private System.Int64 m_size;
        private System.Int64 m_reservedSize;
        private System.Int64 m_downloadedSize;

        private enum ProgressState { NotStarted, Reserving, ReserveComplete, StartDownload, Downloading, Complete };
        private ProgressState m_state;

        public FileDownloader()
        {
            // For now this class manages the download directly since we only
            // need one file at a time downloaded and hopefully something else
            // will handle making sure the same file is not downloaded multiple
            // times at the same time.
            // At some point we may need a manager class the file downloaders
            // use to handle queuing and so on. We may not need this until
            // such time as we are handling the download/installation ourselves
            // at which point that will hopefully give us a nice interface to
            // use.
        }

        public void Download(String name, String downloadDirectory, String url, System.Int64 size)
        {
            // For now the size is passed in. If we start needing more
            // information such as a hash for validation it may be better to
            // invoke a script on the browser to obtain the information we are
            // interested in.
            m_name = name;
            m_directory = downloadDirectory;
            m_source = url;
            m_size = size;
            m_reservedSize = 0;
            m_downloadedSize = 0;
            m_state = ProgressState.NotStarted;
            ThreadPool.QueueUserWorkItem(DownloadThread, this);
        }

        public void DownloadThread(object context)
        {
            System.Int64 rate = m_size / 60;
            while (m_reservedSize < m_size)
            {
                m_reservedSize = m_reservedSize + rate;
                if (m_reservedSize > m_size)
                {
                    m_reservedSize = m_size;
                }
                Thread.Sleep(125);
            }
            while (m_downloadedSize < m_size)
            {
                m_downloadedSize = m_downloadedSize + rate;
                if (m_downloadedSize > m_size)
                {
                    m_downloadedSize = m_size;
                }
                Thread.Sleep(500);
            }
        }

        public bool Complete { get { return (m_state == ProgressState.Complete); } }

        public void UpdateStatus(ProgressMonitor monitor)
        {
            ProgressState oldState = ProgressState.Complete;
            while (m_state!=oldState)
            {
                oldState = m_state;
                switch (m_state)
                {
                    case ProgressState.NotStarted:
                        {
                            monitor.StartProgressAction(m_name, "Reserving", m_size);
                            m_state = ProgressState.Reserving;
                            break;
                        }
                    case ProgressState.Reserving:
                        {
                            monitor.ReportActionProgress(m_name, m_reservedSize);
                            if (m_reservedSize == m_size)
                            {
                                m_state = ProgressState.ReserveComplete;
                            }
                            break;
                        }
                    case ProgressState.ReserveComplete:
                        {
                            monitor.CompleteAction(m_name);
                            m_state = ProgressState.StartDownload;
                            break;
                        }
                    case ProgressState.StartDownload:
                        {
                            monitor.StartProgressAction(m_name, "Downloading", m_size);
                            m_state = ProgressState.Downloading;
                            break;
                        }
                    case ProgressState.Downloading:
                        {
                            monitor.ReportActionProgress(m_name, m_downloadedSize);
                            if (m_downloadedSize == m_size)
                            {
                                m_state = ProgressState.Complete;
                            }
                            break;
                        }
                    case ProgressState.Complete:
                        {
                            object[] key = new object[] { m_name };
                            monitor.CompleteAction(m_name);
                            monitor.Complete(m_name);
                            break;
                        }
                }
            }
        }
    }
}
