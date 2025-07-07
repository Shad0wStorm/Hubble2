using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

using ClientSupport;

namespace ManifestTool
{
    public class ManifestFileWorker : WorkerBase
    {
        private ManifestFileBuilder m_builder;
        private List<String> m_files;
        private String m_root;
        private FileStore m_fileStore;
        private String m_version;
        private String m_title;
        private bool m_useSlash = false;

        public class Statistics
        {
            public long Added;
            public long Size;
            public long Total;
        }

        public Statistics Summary = new Statistics();

        public ManifestFileWorker()
        {
            m_progressWindow.Title = "Run Import";
        }

        public void Run(FileStore fileStore, String directory, String version, String title, bool useSlash)
        {
            m_fileStore = fileStore;
            String manifest = fileStore.ManifestFileNameForVersion(version);
            m_builder = new ManifestFileBuilder(manifest);
            m_version = version;
            m_title = title;
            m_root = directory;
            m_useSlash = useSlash;
            m_progressWindow.Information = "Importing product release from '"+directory + "'.";
            m_progressWindow.Action = "Initialising";

            Run();
        }

        public override void ExecuteTask(DoWorkEventArgs e)
        {
            PerformFileScan(m_root);
            GenerateManifestContents();
            ImportFiles();
            SaveManifest();
        }

        private void PerformFileScan(String directory)
        {
            m_action = "Scanning for files.";
            m_files = new List<String>();
            Stack<String> directories = new Stack<String>();
            directories.Push(directory);
            while ((directories.Count>0) && !m_worker.CancellationPending)
            {
                String scan = directories.Pop();

                // No useful progress at this stage, we do not know how many
                // files there are to process until we have scanned them.
                m_worker.ReportProgress(0);

                String[] files = System.IO.Directory.GetFiles(scan);
                foreach (String file in files)
                {
                    if (m_fileStore.Allow(file))
                    {
                        m_files.Add(file);
                    }
                }

                String[] children = System.IO.Directory.GetDirectories(scan);
                foreach (String child in children)
                {
                    directories.Push(child);
                }
            }
        }

        private void GenerateManifestContents()
        {
            if (m_worker.CancellationPending)
            {
                return;
            }
            m_action = "Hashing files.";

            int progress = 0;
            int total = m_files.Count;
            foreach (String file in m_files)
            {
                m_builder.AddFile(file, m_root);
                ++progress;
                m_worker.ReportProgress((progress * 100) / total);
                if (m_worker.CancellationPending)
                {
                    break;
                }
            }
        }

        private void ImportFiles()
        {
            if (m_worker.CancellationPending)
            {
                return;
            }
            m_action = "Importing files.";
            m_worker.ReportProgress(0);

            Summary.Total = m_files.Count;
            Summary.Added = 0;
            Summary.Size = 0;
            int progress = 0;
            int total = m_files.Count;
            foreach (ManifestFile.ManifestEntry entry in m_builder.Entries)
            {
                String path = System.IO.Path.Combine(m_root, entry.Path);
                if (m_fileStore.Import(path, entry.Hash))
                {
                    ++Summary.Added;
                    FileInfo fi = new FileInfo(path);
                    Summary.Size += fi.Length;
                }
                ++progress;
                m_worker.ReportProgress((progress * 100) / total);
                if (m_worker.CancellationPending)
                {
                    break;
                }
            }
        }

        private void SaveManifest()
        {
            if (m_worker.CancellationPending)
            {
                return;
            }
            m_action = "Saving manifest.";
            m_worker.ReportProgress(0);
            m_builder.SaveFile(m_fileStore.EnsureManifestDirectory(), m_title, m_version, m_useSlash);
        }
    }
}
