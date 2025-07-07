using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientSupport.ProjectUpdater
{
    public class ManifestBundle
    {
        public class BundleEntry
        {
            public BundleEntry(ManifestFile.ManifestEntry entry)
            {
                Entry = entry;
                Validated = false;
                Downloaded = false;
                DownloadProgress = 0;
            }
            public ManifestFile.ManifestEntry Entry = null;
            public bool Validated = false;
            public bool Downloaded = false;
            public long DownloadProgress;
        }

        public List<BundleEntry> Entries = null;

        private String m_hash;
        public String Hash { get { return m_hash; } }

        private long m_size;
        public long Size { get { return m_size; } }

        public long TotalSize
        {
            get
            {
                return m_size * Entries.Count;
            }
        }

        public long ValidatedProgress
        {
            get
            {
                long validated = 0;

                foreach (BundleEntry entry in Entries)
                {
                    if (entry.Validated)
                    {
                        validated += m_size;
                    }
                }
                return validated;
            }
        }

        public long Progress
        {
            get
            {
                long progress = 0;
                foreach (BundleEntry entry in Entries)
                {
                    if (entry.Validated)
                    {
                        progress += m_size;
                    }
                    else
                    {
                        if (entry.Downloaded)
                        {
                            // Fiddle factor to give some indication that
                            // things are progressing before we have chance to
                            // validate the downloaded entity.
                            progress += m_size;
                        }
                        else
                        {
                            progress += entry.DownloadProgress;
                        }
                    }
                }
                return progress;
            }
        }

        public String Information
        {
            get
            {
                return Hash + ":" + Sequence;
            }
        }

        private String m_sequence;
        public String Sequence
        {
            get
            {
                return m_sequence;
            }
        }

        public ManifestFile.ManifestEntry Matching
        {
            get
            {
                foreach (BundleEntry entry in Entries)
                {
                    if (entry.Validated)
                    {
                        return entry.Entry;
                    }
                }
                return null;
            }
        }

        public bool Validated;

        public int Retries;
		public int OriginalRetries
		{
			get;
			private set;
		}
        public bool HasRetried
        {
            get
            {
                return Retries != OriginalRetries;
            }
        }

        public DownloadManagerBase.DownloadStatus DownloadStatus;

        public ManifestBundle(ManifestFile.ManifestEntry entry)
        {
            Validated = false;
            Entries = new List<BundleEntry>();
            m_hash = entry.Hash;
            m_size = entry.Size;
            Retries = 0;
            DownloadStatus = new DownloadManagerBase.DownloadStatus();
            AddEntry(entry);
        }

        public void AddEntry(ManifestFile.ManifestEntry entry)
        {
            if (entry.Hash != Hash)
            {
                throw new ArgumentException("Incompatible hash value passed.");
            }
            if (entry.Size != Size)
            {
                throw new ArgumentException("Incompatible size value passed.");
            }
            Entries.Add(new BundleEntry(entry));
        }

        public void Perform(String action)
        {
            m_sequence += action;
        }

        public void SetRetries(int retries)
        {
            Retries = OriginalRetries = retries;
        }
    }
}
