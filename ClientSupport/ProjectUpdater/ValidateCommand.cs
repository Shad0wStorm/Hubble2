using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LocalResources;

namespace ClientSupport.ProjectUpdater
{
    public class ValidateCommand : PriorityCommand
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


        public ValidateCommand(ManifestBundle bundle,
            ManifestFileOps ops,
            CommandQueue commands)
        {
            m_bundle = bundle;
            m_mfo = ops;
            m_commands = commands;
        }

        public ValidateCommand(ManifestBundle bundle,
            ManifestFileOps ops,
            CommandPriorityQueue commands,
            int priority = 0)
        {
            m_bundle = bundle;
            m_mfo = ops;
            m_priorityCommands = commands;
            SetPriority(priority);
        }

        public bool Execute()
        {
            m_bundle.Perform("V");
            ManifestBundle.BundleEntry matching = null;
            foreach (ManifestBundle.BundleEntry entry in m_bundle.Entries)
            {
                if (m_mfo.ValidateFile(entry.Entry))
                {
                    if (matching == null)
                    {
                        matching = entry;
                    }
                    entry.Validated = true;
                }
            }

            if (matching != null)
            {
                foreach (ManifestBundle.BundleEntry entry in m_bundle.Entries)
                {
                    if (!entry.Validated)
                    {
                        try
                        {
                            if (m_mfo.Exists(entry.Entry))
                            {
                                m_mfo.RemoveFile(entry.Entry);
                            }
                            m_mfo.CopyFile(matching.Entry, entry.Entry);
                            if (m_mfo.ValidateFile(entry.Entry))
                            {
                                entry.Validated = true;
                            }
                            else
                            {
                                DownloadManagerBase.RemoteFileDetails remote = m_mfo.GetRemoteFileDetails(entry.Entry);
                                m_mfo.SetError(
                                    String.Format(LocalResources.Properties.Resources.PU_ManifestFileCopyValidFail,
                                             remote.LocalFileName));

                            }
                        }
                        catch (System.Exception ex)
                        {
                            DownloadManagerBase.RemoteFileDetails remote = m_mfo.GetRemoteFileDetails(entry.Entry);
                            m_mfo.SetError(String.Format(LocalResources.Properties.Resources.PU_ManifestFileCopyExc,
                                remote.LocalFileName, ex.Message));
                        }
                    }
                }
            }
            else
            {
                if (m_bundle.HasRetried)
                {
                    LogEntry le = new LogEntry("ValidationFailedAfterDownload");
                    m_mfo.Log(le);
                }
                if (m_commands != null)
                {
                    m_commands.AddCommand(new DownloadCommand(m_bundle, m_mfo, m_commands));
                }
                else
                {
                    if (m_priorityCommands != null)
                    {
                        m_priorityCommands.AddCommand(new DownloadCommand(m_bundle, m_mfo, m_priorityCommands, CommandPriorityQueue.Normal));
                    }
                }
            }

            bool allValidated = true;
            foreach (ManifestBundle.BundleEntry entry in m_bundle.Entries)
            {
                if (!entry.Validated)
                {
                    allValidated = false;
                    break;
                }
            }
            m_bundle.Validated = allValidated;
            return !m_mfo.CancelRequested();
        }

    }
}
