using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using LocalResources;

namespace ClientSupport.ProjectUpdater
{
    /// <summary>
    /// Mid level file operations.
    /// 
    /// Offer capabilities beyond what the basic IO classes allow to make the
    /// code more readable.
    /// </summary>
    class FileOps
    {
        ProjectUpdateLog m_pulog = null;

        public FileOps(ProjectUpdateLog log)
        {
            m_pulog = log;
        }

        public long CountContents(String path, UpdateStatus status, bool showProgress)
        {
            long rv = 0;

            if (Directory.Exists(path))
            {
                string[] entries = Directory.GetDirectories(path);
                rv = rv + Directory.GetFiles(path).Length;
                long progress = 0;
                foreach (String directory in entries)
                {
                    rv = rv + CountContents(directory, status, false) + 1;
                    if (showProgress)
                    {
                        ++progress;
                        status.Monitor.ReportActionProgress(status.Project.Name, (progress * 100) / entries.Length);
                    }
                }
            }

            return rv;
        }

        public String RemoveDirectory(String path, UpdateStatus status, ref long progress)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    foreach (String directory in Directory.GetDirectories(path))
                    {
                        RemoveDirectory(directory, status, ref progress);
                    }
                    foreach (String file in Directory.GetFiles(path))
                    {
                        RemoveFile(file);
                        ++progress;
                    }
                    Directory.Delete(path);
                    ++progress;
                    status.Monitor.ReportActionProgress(status.Project.Name, progress);
                }
            }
            catch (System.Exception ex)
            {
                return String.Format(LocalResources.Properties.Resources.Project_UninstallTidyException,
                    ex.Message);
            }
            return null;
        }

        public void RemoveFile(String path)
        {
            if (File.Exists(path))
            {
                FileAttributes attr = File.GetAttributes(path);
                if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    attr = attr & ~FileAttributes.ReadOnly;
                    File.SetAttributes(path, attr);
                }
                File.Delete(path);
            }
        }

        public void EnsureDirectory(String path)
        {
            if (System.IO.Directory.Exists(path))
            {
                return;
            }
            EnsureDirectory(System.IO.Path.GetDirectoryName(path));
            m_pulog.Log("CreateDirectory", path);
            System.IO.Directory.CreateDirectory(path);
        }

        public void CheckWritable(UpdateStatus error)
        {
            String projectDir = error.Project.ProjectDirectory;
            try
            {
                EnsureDirectory(projectDir);
                String fileName = "edl_writable.txt";
                String writeFile = Path.Combine(projectDir, fileName);
                const String c_writeValue = "WriteTest";
                using (StreamWriter sw = new StreamWriter(writeFile))
                {
                    sw.WriteLine(c_writeValue);
                }
                String ciFile = Path.Combine(projectDir, fileName.ToUpperInvariant());
                bool caseSensitive = false;
                try
                {
                    using (StreamReader sr = new StreamReader(ciFile))
                    {
                        String text = sr.ReadLine().Trim();
                        if (text != c_writeValue)
                        {
                            caseSensitive = true;
                        }
                    }
                }
                catch (System.IO.IOException)
                {
                    caseSensitive = true;
                }
                if (caseSensitive)
                {
                    error.SetError(String.Format(LocalResources.Properties.Resources.PU_UpdateCaseSensitivityException));
                }
                RemoveFile(writeFile);
            }
            catch (Exception ex)
            {
                if ((ex is System.IO.IOException) || (ex is System.UnauthorizedAccessException))
                {
                    error.SetError(String.Format(LocalResources.Properties.Resources.PU_UpdateWriteException,
                        ex.Message));
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
