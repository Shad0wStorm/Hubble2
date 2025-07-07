using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ClientSupport
{
    public class DownloadManagerVirtualCache : DownloadManagerBase
    {
        FORCManager m_manager;

        public class VCacheFileEntry
        {
            public String Path = null;
            public DateTime Stamp;
        }
        public class VCacheEntry
        {
            public String Hash = null;
            public long Size = 0;
            public List<VCacheFileEntry> Files = new List<VCacheFileEntry>();
        }

        Dictionary<String, VCacheEntry> m_cache = null;
        Dictionary<String, VCacheEntry> m_cacheFileIndex = null;
        String m_cacheFile = null;

        public DownloadManagerVirtualCache(FORCManager manager)
        {
            m_manager = manager;
        }

		public override void ResetCache()
		{

		}

        /// <summary>
        /// The virtual cache cannot provide a manifest it is simply a list of
        /// files on disk.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="project"></param>
        /// <param name="version"></param>
        /// <param name="details"></param>
        /// <returns></returns>
        public override InstallerVersionResult GetInstallerPath(UserDetails user, String project, String version, ref RemoteFileDetails details)
        {
            return InstallerVersionResult.Failed;
        }

        /// <summary>
        /// Set up the cache contents
        /// </summary>
        private void SetupCache()
        {
            // Have an existing cache, use that.
            if (m_cache!=null)
            {
                return;
            }

            LoadExistingCache();

            // Since we are using other projects as potential source files which
            // makes no sense in the larger scheme of things there is no real
            // concept of where the files we are interested in should come from.
            // For now we scan the known project directories and add any that
            // are installed.
            //Project[] projects = m_manager.AvailableProjects.GetProjectArray();
            //foreach (Project p in projects)
            //{
            //    if (p.Installed)
            //    {
            //        AddProjectFiles(p);
            //    }
            //}

            try
            {
                HashSet<String> searchDirs = new HashSet<String>();
                searchDirs.Add(m_manager.ProjectDirectory);

                foreach (Project p in m_manager.AvailableProjects.GetProjectArray())
                {
                    String pd = p.ProjectDirectory;
                    String cd = Path.GetDirectoryName(pd);
                    if (Directory.Exists(cd))
                    {
                        searchDirs.Add(cd);
                    }
                }

                foreach(String search in searchDirs)
                {
                    String[] sources = Directory.GetDirectories(search);
                    foreach (String source in sources)
                    {
                        try
                        {
                            AddDirectoryFiles(source);
                        }
                        catch (System.Exception)
                        {

                        }
                    }
                }
            }
            catch (System.Exception)
            {
            	
            }

            SaveCacheContents();
        }

        private void LoadExistingCache()
        {
            m_cache = new Dictionary<String, VCacheEntry>();
            m_cacheFileIndex = new Dictionary<String, VCacheEntry>();

            //Project[] projects = m_manager.AvailableProjects.GetProjectArray();
            //foreach (Project p in projects)
            //{
            //    if (p.Installed)
            //    {
            //        String parent = Path.GetDirectoryName(p.ProjectDirectory);
            //        if (!String.IsNullOrEmpty(parent))
            //        {
            //            m_cacheFile = Path.Combine(parent, "VirtualCache.xml");
            //            break;
            //        }
            //    }
            //}
            m_cacheFile = Path.Combine(m_manager.ProjectDirectory,"VirtualCache.xml");

            if (File.Exists(m_cacheFile))
            {
                List<VCacheEntry> list = null;
                XmlSerializer sz = new XmlSerializer(typeof(List<VCacheEntry>));
                using (StreamReader mr = new StreamReader(m_cacheFile))
                {
                    try
                    {
                        list = (List<VCacheEntry>)sz.Deserialize(mr);
                        foreach (VCacheEntry vce in list)
                        {
                            List<VCacheFileEntry> removeFiles = new List<VCacheFileEntry>();
                            foreach (VCacheFileEntry vcfe in vce.Files)
                            {
                                if (File.Exists(vcfe.Path))
                                {
                                    m_cacheFileIndex[vcfe.Path] = vce;
                                }
                                else
                                {
                                    removeFiles.Add(vcfe);
                                }
                            }
                            foreach (VCacheFileEntry remove in removeFiles)
                            {
                                vce.Files.Remove(remove);
                            }
                            if (vce.Files.Count>0)
                            {
                                m_cache[vce.Hash] = vce;
                            }
                        }
                    }
                    catch (System.Exception)
                    {
                        // Failed to read the cache file, treat it as if the
                        // cache did not exist. This means rebuilding the cache
                        // fully if the format changes, but we do not expect
                        // that to happen very often.
                    }
                }
            }
        }

        private void SaveCacheContents()
        {
            if (!String.IsNullOrEmpty(m_cacheFile))
            {
                VCacheEntry[] list = m_cache.Values.ToArray();
                XmlSerializer sz = new XmlSerializer(typeof(VCacheEntry[]));
                StreamWriter mw = new StreamWriter(m_cacheFile);
                sz.Serialize(mw, list);
                mw.Close();
            }
        }

        private void AddProjectFiles(Project p)
        {
            String root = p.ProjectDirectory;
            if (Directory.Exists(root))
            {
                AddDirectoryFiles(root);
            }
        }

        private void AddDirectoryFiles(String path)
        {
            DecoderRing dr = new DecoderRing();
            String[] files = Directory.GetFiles(path);
            foreach (String file in files)
            {
                VCacheEntry entry = null;
                bool changed = true;
                FileInfo info = new FileInfo(file);
                if (m_cacheFileIndex.ContainsKey(file))
                {
                    entry = m_cacheFileIndex[file];
                    if (info.Length==entry.Size)
                    {
                        // Have an existing entry for this file path
                        VCacheFileEntry remove = null;
                        foreach (VCacheFileEntry fileEntry in entry.Files)
                        {
                            if (fileEntry.Path==file)
                            {
                                if (info.LastWriteTimeUtc == fileEntry.Stamp)
                                {
                                    // Have the existing entry for the file
                                    // and the time stamp has not changed so
                                    // assume unchanged.
                                    changed = false;
                                }
                                else
                                {
                                    // Stamp does not match so the contents may
                                    // have changed. Remove the file from the
                                    // hash and re-add later if required.
                                    // Only flag the entry for removal as we
                                    // are iterating over the list and cannot
                                    // modify it.
                                    remove = fileEntry;
                                }
                                // found the matching entry so no need to
                                // consider further entries.
                                break;
                            }
                        }
                        if (remove != null)
                        {
                            entry.Files.Remove(remove);
                            if (entry.Files.Count == 0)
                            {
                                m_cache.Remove(entry.Hash);
                            }
                        }
                    }
                }
                if (changed)
                {
                    long fileSize = 0;
                    String fileHash = dr.SHA1EncodeFile(file, out fileSize);
                    if (m_cache.ContainsKey(fileHash))
                    {
                        entry = m_cache[fileHash];
                    }
                    else
                    {
                        entry = new VCacheEntry();
                        entry.Hash = fileHash;
                        entry.Size = fileSize;
                        m_cache[fileHash] = entry;
                    }
                    VCacheFileEntry fileEntry = new VCacheFileEntry();
                    fileEntry.Path = file;
                    fileEntry.Stamp = info.LastWriteTimeUtc;
                    entry.Files.Add(fileEntry);
                    m_cacheFileIndex[file] = entry;
                }
            }

            String[] directories = Directory.GetDirectories(path);
            foreach (String directory in directories)
            {
                AddDirectoryFiles(directory);
            }
        }

        public override void BeginDownloadBatch(ref DownloadStatus status)
        {
            try
            {
                SetupCache();
            }
            catch (System.Exception ex)
            {
                LogEntry vcs = new LogEntry("VirtualCacheSetup");
                vcs.AddValue("Exception", ex.Message);
                LogValues(m_manager.UserDetails, vcs);
            }
        }

        class VCHandle : Handle
        {
            public FileStream m_source;
        }

        /// <summary>
        /// See if the virtual cache contains a file with the requested hash
        /// and size.
        /// 
        /// If so double check that the file has not been modified since the
        /// cache was updated and if so use the existing file as a source.
        /// The file may have changed by the update process so we need to handle
        /// the case.
        /// 
        /// For example when loading/checking the cache file A has hash HA.
        /// An update for file A is downloaded with hash HAB. File C is
        /// requested with hash HA. This is in the cache due to the original
        /// version of A, but that has now been replaced with different
        /// contents so file C needs to be obtained from a different source.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="details"></param>
        /// <returns></returns>
        public override Handle BeginDownload(ref DownloadStatus status,
            UserDetails user, RemoteFileDetails details)
        {
            try
            {
                if (m_cache.ContainsKey(details.CheckSum))
                {
                    VCacheEntry entry = m_cache[details.CheckSum];
                    foreach (VCacheFileEntry fileEntry in entry.Files)
                    {
                        if (File.Exists(fileEntry.Path))
                        {
                            FileInfo cfi = new FileInfo(fileEntry.Path);
                            if ((cfi.Length == entry.Size) && (cfi.Length == details.FileSize))
                            {
                                if (cfi.LastWriteTimeUtc == fileEntry.Stamp)
                                {
                                    VCHandle handle = new VCHandle();
                                    handle.m_source = new FileStream(fileEntry.Path, FileMode.Open, FileAccess.Read);
                                    if (handle.m_source.Length == details.FileSize)
                                    {
                                        if (details.Existing != 0)
                                        {
                                            handle.m_source.Seek(details.Existing, SeekOrigin.Begin);
                                        }
                                        return handle;
                                    }
                                }
                            }
                        }
                    }
                }
                throw new System.IO.FileNotFoundException();    	
            }
            catch (System.Exception)
            {
                throw new System.IO.FileNotFoundException();    	
            }
        }

        public override int DownloadChunk(ref DownloadStatus status,
            Handle handle, Byte[] buffer, int bufferSize)
        {
            VCHandle vc = handle as VCHandle;
            if (vc != null)
            {
                return vc.m_source.Read(buffer, 0, bufferSize);
            }
            else
            {
                throw new System.IO.IOException("Invalid transfer handle");
            }
        }

        public override void EndDownload(ref DownloadStatus status,Handle handle)
        {
            VCHandle vc = handle as VCHandle;
            if (vc != null)
            {
                vc.m_source.Close();
            }
            else
            {
                throw new System.IO.IOException("Invalid transfer handle");
            }
        }

        public override void EndDownloadBatch(ref DownloadStatus status)
        {
        }

        public override String GetLastResponse()
        {
            return null;
        }

        public override void LogValues(UserDetails details, LogEntry entry) { }
    }
}
