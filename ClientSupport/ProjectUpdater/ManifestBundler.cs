using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientSupport.ProjectUpdater
{
    public class ManifestBundler
    {
        public int Count { get { return m_bundles.Count; } }

        public int FileCount
        {
            get
            {
                int total = 0;
                foreach (ManifestBundle bundle in m_bundles)
                {
                    total = total + bundle.Entries.Count;
                }
                return total;
            }
        }

        public long DataSize
        {
            get
            {
                long size = 0;
                foreach (ManifestBundle bundle in m_bundles)
                {
                    size = size + bundle.TotalSize;
                }
                return size;
            }
        }

        public long Progress
        {
            get
            {
                long progress = 0;
                foreach (ManifestBundle bundle in m_bundles)
                {
                    progress = progress + bundle.Progress;
                }
                return progress;
            }
        }

        public long ValidatedProgress
        {
            get
            {
                long progress = 0;
                foreach (ManifestBundle bundle in m_bundles)
                {
                    progress = progress + bundle.ValidatedProgress;
                }
                return progress;

            }
        }

        private List<ManifestBundle> m_bundles = new List<ManifestBundle>();
        public List<ManifestBundle> Bundles { get { return m_bundles; } }

      
        public void AddEntry(ManifestFile.ManifestEntry entry)
        {
            int count = m_bundles.Count;
            String eHash = entry.Hash;
            for( int bi = 0; bi<count; ++bi)
            {
                ManifestBundle bundle = m_bundles[bi];
                if ((bundle.Hash == eHash) &&
                    (bundle.Size == entry.Size))
                {
                    bundle.AddEntry(entry);
                    return;
                }
            }
            ManifestBundle newbundle = new ManifestBundle(entry);
            m_bundles.Add(newbundle);
        }

        /// <summary>
        /// Set the number of times a file should be re-downloaded in case of
        /// errors before giving up completely.
        /// 
        /// This should be called after all entries have been added to ensure
        /// all bundles receive the correct value.
        /// </summary>
        /// <param name="retries"></param>
        public void SetRetries(int retries)
        {
            foreach (ManifestBundle bundle in m_bundles)
            {
                bundle.SetRetries(retries);
            }
        }
    }
}
