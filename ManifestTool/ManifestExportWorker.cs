using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

using ClientSupport;

namespace ManifestTool
{
    public class ManifestExportWorker : WorkerBase
    {
        public enum Mode { Wipe, Tidy, Merge };

        public Mode ExportMode { get; set; }
        public int NumCopythreads { get; set; }
        public ManifestFile Source { get; set; }
        public String TargetDirectory { get; set; }
        public FileStore ActiveFileStore { get; set; }

        public long FilesUpdated;
        public long FilesAdded;
        public long FilesRemoved;
        public long DirectoriesRemoved;

        public ManifestExportWorker()
        {
            m_progressWindow.Title = "Export Manifest";
            NumCopythreads = 1;
        }

        public override void Run()
        {
            String info = "Exporting ";
            if (Source.ProductTitle != null)
            {
                info += Source.ProductTitle;
            }
            else
            {
                info += Source.FileName;
            }
            if (Source.ProductVersion != null)
            {
                info += " (" + Source.ProductVersion + ")";
            }
            info += " to directory '" + TargetDirectory + "'.";
            m_progressWindow.Information = info;
            m_progressWindow.Action = "Initialising";
            base.Run();
        }

        public override void ExecuteTask(DoWorkEventArgs e)
        {
            FilesUpdated = 0;
            FilesRemoved = 0;
            FilesAdded = 0;
            DirectoriesRemoved = 0;

            if (ExportMode == Mode.Wipe)
            {
                WipeDirectory();
            }

            ExportFiles();

            if (ExportMode == Mode.Tidy)
            {
                RemoveUnwantedFiles();
            }
        }

        private void ScanDirectory(String path, List<String> result)
        {
            String[] files = Directory.GetFiles(path);
            foreach (String file in files)
            {
                result.Add(file.ToLowerInvariant());
            }
            String[] directories = Directory.GetDirectories(path);
            foreach (String directory in directories)
            {
                ScanDirectory(directory, result);
                result.Add(directory.ToLowerInvariant());
            }
        }

        private void WipeDirectory()
        {
            m_action = "Scanning existing files";
            m_worker.ReportProgress(0);

            List<String> scanResult = new List<String>();

            ScanDirectory(TargetDirectory, scanResult);

            m_action = "Removing files";
            m_worker.ReportProgress(0);

            int progress = 0;
            int total = scanResult.Count();
            foreach (String item in scanResult)
            {
                if (File.Exists(item))
                {
                    File.Delete(item);
                    ++FilesRemoved;
                }
                if (Directory.Exists(item))
                {
                    Directory.Delete(item);
                    ++DirectoriesRemoved;
                }
                ++progress;
                m_worker.ReportProgress((100*progress)/total);
            }
        }

        private void ExportFiles()
        {
            m_action = "Exporting...";
            m_worker.ReportProgress(0);

            int progress = 0;
            int total = Source.EntryCount;

            var allTasks = new HashSet<Task>();
            var sourceEntryQueue = new Queue<ManifestFile.ManifestEntry>(Source.Entries);

            while (sourceEntryQueue.Count > 0 || allTasks.Count > 0)
            {
                // if numCopythreads are running, wait for one to finish
                if (allTasks.Count >= NumCopythreads)
                {
                    Task.WaitAny(allTasks.ToArray());
                }

                // remove all finished tasks and report their progress
                foreach(Task t in allTasks.Where(t=>t.IsCompleted).ToArray())
                {
                    ++progress;
                    m_worker.ReportProgress((progress * 100) / total);
                    allTasks.Remove(t);
                }

                // if there's source entries left and less then NumCopythreads running threads, try to make a task for them
                if (sourceEntryQueue.Count > 0 && allTasks.Count < NumCopythreads)
                {
                    var entry = sourceEntryQueue.Dequeue();
                    String fileTarget = System.IO.Path.Combine(TargetDirectory, entry.Path);
                    bool replace = true;
                    System.IO.FileInfo fi = new System.IO.FileInfo(fileTarget);
                    if (fi.Exists)
                    {
                        if (fi.Length == entry.Size)
                        {
                            DecoderRing ring = new DecoderRing();
                            long length;
                            String localHash = ring.SHA1EncodeFile(fileTarget, out length);
                            if (length == entry.Size)
                            {
                                if (localHash == entry.Hash)
                                {
                                    // Already have the file so do not process.
                                    replace = false;
                                }
                            }
                        }
                        if (replace)
                        {
                            System.IO.File.Delete(fileTarget);
                            // Did exist, but replacing so this is an update.
                            ++FilesUpdated;
                        }
                    }
                    else
                    {
                        String fileDirectory = System.IO.Path.GetDirectoryName(fileTarget);
                        EnsureDirectory(fileDirectory);
                        // Did not exist, new file.
                        ++FilesAdded;
                    }
                    if (replace)
                    {
                        allTasks.Add(Task.Run(() => ActiveFileStore.Export(fileTarget, entry.Hash)));
                        m_action = String.Format("Exporting... ({0}/{1} threads running)", allTasks.Count, NumCopythreads);
                    }
                }
            }
        }

        private void RemoveUnwantedFiles()
        {
            m_action = "Scanning existing files";
            m_worker.ReportProgress(0);

            List<String> scanResult = new List<String>();

            ScanDirectory(TargetDirectory, scanResult);

            m_action = "Detecting wanted files.";
            m_worker.ReportProgress(0);

            int progress = 0;
            int total = Source.Entries.Count();
            foreach (ManifestFile.ManifestEntry entry in Source.Entries)
            {
                String path = System.IO.Path.Combine(TargetDirectory, entry.Path).ToLowerInvariant();
                if (scanResult.Contains(path))
                {
                    scanResult.Remove(path);
                }
                ++progress;
                m_worker.ReportProgress((100 * progress) / total);
            }

            m_action = "Removing files";
            m_worker.ReportProgress(0);

            progress = 0;
            total = scanResult.Count();
            String[] contents;
            foreach (String item in scanResult)
            {
                if (File.Exists(item))
                {
                    File.Delete(item);
                    ++FilesRemoved;
                }
                if (Directory.Exists(item))
                {
                    contents = Directory.GetFiles(item);
                    if (contents.Length == 0)
                    {
                        contents = Directory.GetDirectories(item);
                    }
                    if (contents.Length == 0)
                    {
                        Directory.Delete(item);

                        ++DirectoriesRemoved;
                    }
                }
                ++progress;
                m_worker.ReportProgress((100 * progress) / total);
            }

        }

        private void EnsureDirectory(String directory)
        {
            if (System.IO.Directory.Exists(directory))
            {
                return;
            }
            String parent = System.IO.Path.GetDirectoryName(directory);
            if (parent != null)
            {
                EnsureDirectory(parent);
            }
            System.IO.Directory.CreateDirectory(directory);
        }
    }
}
