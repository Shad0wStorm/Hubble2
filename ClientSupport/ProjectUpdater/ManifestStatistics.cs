using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ClientSupport.ProjectUpdater
{
    class ManifestStatistics
    {
        private List<String> m_everything = new List<String>();
        private List<String> m_added = new List<String>();
        private List<String> m_updated = new List<String>();
        private List<String> m_skipped = new List<String>();
        private List<String> m_removed = new List<String>();
        private List<String> m_kept = new List<String>();
        private List<String> m_downloaded = new List<String>();
        private List<String> m_copied = new List<String>();
        private int m_changeCount = 0;
        public int ChangeCount { get { return m_changeCount; } }


        public ManifestStatistics()
        {
        }

        public void ConsiderFile(String file)
        {
            m_everything.Add(file);
        }

        public void AddFile(String file)
        {
            m_added.Add(file);
            ++m_changeCount;
        }

        public void UpdatedFile(String file)
        {
            m_updated.Add(file);
            ++m_changeCount;
        }

        public void SkippedFile(String file)
        {
            m_skipped.Add(file);
        }

        public void RemovedFile(String file)
        {
            m_removed.Add(file);
            ++m_changeCount;
        }

        public void KeptFile(String file)
        {
            m_kept.Add(file);
        }

        public void DownloadedFile(String file)
        {
            m_downloaded.Add(file);
        }

        public void CopiedFile(String file)
        {
            m_copied.Add(file);
        }

        public void Write(String path)
        {
            using (StreamWriter writer = File.CreateText(path))
            {
                foreach (String added in m_added)
                {
                    writer.Write("Added ");
                    writer.WriteLine(added);
                }
                foreach (String updated in m_updated)
                {
                    writer.Write("Updated ");
                    writer.WriteLine(updated);
                }
                foreach (String skipped in m_skipped)
                {
                    writer.Write("Skipped ");
                    writer.WriteLine(skipped);
                }
                foreach (String removed in m_removed)
                {
                    writer.Write("Removed ");
                    writer.WriteLine(removed);
                }
                foreach (String kept in m_kept)
                {
                    writer.Write("Kept ");
                    writer.WriteLine(kept);
                }
                foreach (String downloaded in m_downloaded)
                {
                    writer.Write("Downloaded ");
                    writer.WriteLine(downloaded);
                }
                foreach (String copied in m_copied)
                {
                    writer.Write("Copied ");
                    writer.WriteLine(copied);
                }

                writer.WriteLine(String.Format("Added {0} new files", m_added.Count));
                writer.WriteLine(String.Format("Updated {0} existing files", m_updated.Count));
                writer.WriteLine(String.Format("Skipped {0} existing files", m_skipped.Count));
                writer.WriteLine(String.Format("Removed {0} expired files", m_removed.Count));
                writer.WriteLine(String.Format("Downloaded {0} files", m_downloaded.Count));
                writer.WriteLine(String.Format("Copied {0} files", m_copied.Count));
            }
        }

        public int Added { get { return m_added.Count; } }
        public int Updated { get { return m_updated.Count; } }
        public int Skipped { get { return m_skipped.Count; } }
        public int Removed { get { return m_removed.Count; } }
        public int Downloaded { get { return m_downloaded.Count; } }
        public int Copied { get { return m_copied.Count; } }
    }
}
