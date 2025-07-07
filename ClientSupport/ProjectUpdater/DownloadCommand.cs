using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientSupport.ProjectUpdater
{
    public class DownloadCommand : PriorityCommand
    {
        ManifestBundle m_bundle;
        ManifestFileOps m_mfo;
        CommandQueue m_commands = null;
        CommandPriorityQueue m_priorityCommands = null;

        private int m_priority;

        public void SetPriority(int priority)
        {
            m_priority = priority;
        }

        public int Priority()
        {
            return m_priority;
        }

        public DownloadCommand(ManifestBundle bundle,
            ManifestFileOps ops,
            CommandQueue commands)
        {
            m_bundle = bundle;
            m_mfo = ops;
            m_commands = commands;
        }

        public DownloadCommand(ManifestBundle bundle,
            ManifestFileOps ops,
            CommandPriorityQueue commands,
            int priority = 0)
        {
            m_bundle = bundle;
            m_mfo = ops;
            m_priorityCommands = commands;
            SetPriority(priority);
        }

        class DownloadProgress
        {
            ManifestBundle.BundleEntry m_entry;
            CommandQueueProgress m_commands;

            public DownloadProgress(ManifestBundle.BundleEntry entry,
                CommandQueue commands,
                CommandPriorityQueue priorityCommands)
            {
                m_entry = entry;
                if (commands != null)
                {
                    m_commands = commands;
                }
                else
                {
                    if (priorityCommands != null)
                    {
                        m_commands = priorityCommands;
                    }
                }
            }

            public void UpdateProgress(long progress, long total)
            {
                long oldProgress = m_entry.DownloadProgress;
                if (progress <= m_entry.Entry.Size)
                {
                    m_entry.DownloadProgress = progress;
                }
                else
                {
                    if (progress > 0)
                    {
                        m_entry.DownloadProgress = m_entry.Entry.Size;
                    }
                    else
                    {
                        m_entry.DownloadProgress = 0;
                    }
                }
                if (oldProgress != m_entry.DownloadProgress)
                {
                    if (m_commands != null)
                    {
                        m_commands.ReportUpdate();
                    }
                }
            }
        }

        public bool Execute()
        {
            m_bundle.Perform("D");
            if (m_bundle.Entries.Count > 0)
            {
                ManifestBundle.BundleEntry first = m_bundle.Entries[0];

                DownloadManagerBase.RemoteFileDetails rfd = m_mfo.GetRemoteFileDetails(first.Entry);
                if (m_mfo.GetFreeSpace(rfd.LocalFileName) < first.Entry.Size)
                {
                    m_bundle.DownloadStatus.Error = String.Format(LocalResources.Properties.Resources.PU_InsufficientSpace,
                            rfd.LocalFileName, first.Entry.Size);
                    LogEntry le = new LogEntry("InsufficientSpace");
                    le.AddValue("File", first.Entry.Path);
                    le.AddValue("Target", rfd.LocalFileName);
                    le.AddValue("Message", m_bundle.DownloadStatus.Error);
                    m_mfo.Log(le);
                    return false;
                }

                DownloadProgress progressMarker = new DownloadProgress(first, m_commands, m_priorityCommands);

                bool allowRetry = true;
                if (m_bundle.Retries <= 0)
                {
                    // No retries remaining, so if download fails give up.
                    allowRetry = false;
                }
                else
                {
                    // retries remaining so allow the download to happen again
                    // if the download command fails.
                    --m_bundle.Retries;
                }

                // Discard any errors from previous attempts.
                m_bundle.DownloadStatus = new DownloadManagerBase.DownloadStatus();

                if (m_mfo.DownloadFile(first.Entry, progressMarker.UpdateProgress,
                    ref m_bundle.DownloadStatus))
                {
                    // Download succeeded, so revalidate the bundle
                    if (m_commands != null)
                    {
                        // If retries remain the validate will add a new
                        // download automatically if the validation fails.
                        // Make the validation high priority so we process the
                        // downloaded data ASAP after the download.
                        m_commands.AddCommand(new ValidateCommand(m_bundle, m_mfo, m_commands));
                    }
                    else
                    {
                        if (m_priorityCommands != null)
                        {
                            // If retries remain the validate will add a new
                            // download automatically if the validation fails.
                            // Make the validation high priority so we process the
                            // downloaded data ASAP after the download.
                            m_priorityCommands.AddCommand(new ValidateCommand(m_bundle, m_mfo, m_priorityCommands, CommandPriorityQueue.High));
                        }
                    }
                    first.Downloaded = true;
                }
                else
                {
                    first.DownloadProgress = 0;
                    first.Downloaded = false;
                    LogEntry le = new LogEntry("DownloadAttemptFailed");
                    le.AddValue("Retries", m_bundle.Retries);
                    le.AddValue("Message", m_bundle.DownloadStatus.Error);
                    m_mfo.Log(le);
                    if (allowRetry)
                    {
                        // If we had a download issue sleep before putting the
                        // download back on the queue for another attempt. If
                        // the network is down this will prevent generating
                        // lots of immediately failing downloads and using all
                        // the retries before the network comes back up.
						// As we use more retries, increase the timeout.
						// Ideally we would rework this so that the timeout is
						// on the dequeue rather than enqueue so that the
						// thread could get on with other work while waiting
						// for the timeout to expire
                        System.Threading.Thread.Sleep(5000 + (2500 * (m_bundle.OriginalRetries-m_bundle.Retries)));
                        // Retries remaining so queue another download attempt.
                        // Make the retry low priority to maximise the time
                        // for the error to be fixed before we try again.
                        if (m_commands != null)
                        {
                            m_commands.AddCommand(new DownloadCommand(m_bundle, m_mfo, m_commands));
                        }
                        else
                        {
                            if (m_priorityCommands!=null)
                            {
                                m_priorityCommands.AddCommand(new DownloadCommand(m_bundle, m_mfo, m_priorityCommands, CommandPriorityQueue.Lowest));
                            }
                        }
                    }
                }
            }
            return !m_mfo.CancelRequested();
        }
    }
}
