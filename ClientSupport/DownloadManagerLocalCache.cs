using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace ClientSupport
{
    public class DownloadManagerLocalCache : DownloadManagerBase
    {
        class CacheEntry
        {
            public DownloadManagerBase source;
            public String name;
            public int count = 0;
        };
        DownloadManagerBase m_remote;
        FORCManager m_manager;
        List<CacheEntry> m_cache = null;
        Mutex m_downloadMutex = null;

        bool m_recheckProxy = false;
        private String m_lastMessage = null;

        public DownloadManagerLocalCache(DownloadManagerBase remote, FORCManager manager)
        {
            m_remote = remote;
            m_manager = manager;
            m_downloadMutex = new Mutex();
            m_cache = null;
            m_recheckProxy = true;
        }

        public static bool EnableCache
        {
            get
            {
                return Properties.Settings.Default.EnableCache;
            }
            set
            {
                if (value != Properties.Settings.Default.EnableCache)
                {
                    Properties.Settings.Default.EnableCache = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public static bool EnableVirtualCache
        {
            get
            {
                return Properties.Settings.Default.EnableVirtualCache;
            }
            set
            {
                if (value != Properties.Settings.Default.EnableVirtualCache)
                {
                    Properties.Settings.Default.EnableVirtualCache = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

		/// <summary>
		/// Pass the reset on to the remote server.
		/// </summary>
		public override void ResetCache()
		{
			m_downloadMutex.WaitOne();
			m_remote.ResetCache();
			m_downloadMutex.ReleaseMutex();
		}

        /// <summary>
        /// We are only ever interested in the current real version of the
        /// installer so always delegate to the actual server.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="project"></param>
        /// <param name="version"></param>
        /// <param name="details"></param>
        /// <returns></returns>
        public override InstallerVersionResult GetInstallerPath(UserDetails user, String project, String version, ref RemoteFileDetails details)
        {
            m_downloadMutex.WaitOne();
            InstallerVersionResult r = m_remote.GetInstallerPath(user, project, version, ref details);
            m_lastMessage = m_remote.GetLastResponse();
            m_downloadMutex.ReleaseMutex();
            return r;
        }

        public override void BeginDownloadBatch(ref DownloadStatus status)
        {
            bool useCache = true;
            if (Modifications != null)
            {
                useCache = Modifications.UseCache();
            }

            m_downloadMutex.WaitOne();
            if (Properties.Settings.Default.EnableCache && useCache)
            {
                PopulateCache();

                if (m_cache != null)
                {
                    foreach (CacheEntry ce in m_cache)
                    {
                        if (ce.source != null)
                        {
                            ce.source.BeginDownloadBatch(ref status);
                        }
                    }
                }
            }
            if (m_remote != null)
            {
                m_remote.BeginDownloadBatch(ref status);
            }
            m_downloadMutex.ReleaseMutex();
        }

        private class DMLCHandle : Handle
        {
            public Handle m_handle;
            public DownloadManagerBase m_delgate;
        }

        /// <summary>
        /// Find which source can satisfy the request and return enough
        /// information that we can delegate further requests to it.
        /// 
        /// If we have not previously determined the available caches then
        /// populate that first.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="details"></param>
        /// <returns></returns>
        public override Handle BeginDownload(ref DownloadStatus status,
            UserDetails user, RemoteFileDetails details)
        {
            m_downloadMutex.WaitOne();
            if (m_recheckProxy)
            {

                if (m_remote != null)
                {
                    Uri destination = new Uri(details.RemotePath);
                    Uri partial = new Uri(destination.GetLeftPart(UriPartial.Authority));
                    Uri activeProxy = WebRequest.DefaultWebProxy.GetProxy(partial);
                    if (activeProxy != partial)
                    {
                        m_remote.Proxy = new WebProxy(activeProxy);
                    }
                    else
                    {
                        m_remote.Proxy = null;
                    }
                }

                m_recheckProxy = false;
            }
            m_downloadMutex.ReleaseMutex();

            DMLCHandle dh = new DMLCHandle();
            if (m_cache != null)
            {
                foreach (CacheEntry ce in m_cache)
                {
                    try
                    {
                        DownloadManagerBase fs = ce.source;
                        dh.m_handle = fs.BeginDownload(ref status, user, details);
                        dh.m_delgate = fs;
                        ++ce.count;
                        return dh;
                    }
                    catch (System.Exception)
                    {
                        // cache did not contain file, check other caches
                        // or fall back to the remote source.
                    }
                }
            }
            dh.m_delgate = m_remote;
            dh.m_handle = m_remote.BeginDownload(ref status, user, details);
            return dh;
        }

        /// <summary>
        /// Add any cache directories to the pool so files in them can be found
        /// in preference to downloading the files remotely.
        /// 
        /// We require a manifest to exist to determine that it is a valid file
        /// store, but the contents are ignored as the required file list is
        /// always retrieved from the server and the file store will actually
        /// return any matching file, regardless of whether it is in the active
        /// manifest.
        /// </summary>
        private void PopulateCache()
        {
            if (m_cache != null)
            {
                return;
            }

            List<CacheEntry> newcache = null;
            try
            {
                newcache = new List<CacheEntry>();

                if (Properties.Settings.Default.EnableVirtualCache)
                {
                    DownloadManagerVirtualCache vcache = new DownloadManagerVirtualCache(m_manager);
                    CacheEntry vce = new CacheEntry();
                    vce.source = vcache;
                    vce.name = "virtual";
                    newcache.Add(vce);
                }

                DriveInfo[] allDrives = DriveInfo.GetDrives();
                String[] cacheDirs = { "FDLCache", "FDCache", "Data" };

                foreach (DriveInfo d in allDrives)
                {
                    if (d.IsReady)
                    {
                        if (d.TotalSize>(1024 * 1024 * 1024))
                        {
                            foreach (String location in cacheDirs)
                            {
                                String cachePath = Path.Combine(d.Name, location);
                                if (Directory.Exists(cachePath))
                                {
                                    DownloadManagerFileStore dmfs = new DownloadManagerFileStore();
                                    dmfs.SetFileStore(cachePath);
                                    String[] manifests = dmfs.GetAvailableManifests();
                                    if (manifests != null)
                                    {
                                        if (manifests.Length > 0)
                                        {
                                            dmfs.SetActiveManifest(manifests[0]);
                                            CacheEntry fsc = new CacheEntry();
                                            fsc.source = dmfs;
                                            fsc.name = cachePath;
                                            newcache.Add(fsc);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception)
            {
                // If we fail to set up a cache carry on without one.
            	// Any that were previously set up will be retained, and
                // everything should still work, data will just get pulled from
                // the server.
            }
            if (newcache != null)
            {
                if (newcache.Count == 0)
                {
                    m_cache = null;
                }
                else
                {
                    m_cache = newcache;
                }
            }
            else
            {
                m_cache = null;
            }
        }

        /// <summary>
        /// Delegate the call to the entity stored in the handle.
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="buffer"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        public override int DownloadChunk(ref DownloadStatus status,
            Handle handle, Byte[] buffer, int bufferSize)
        {
            DMLCHandle dh = handle as DMLCHandle;
            int r = dh.m_delgate.DownloadChunk(ref status, dh.m_handle, buffer, bufferSize);
            return r;
        }

        /// <summary>
        /// Delegate the call to the entity stored in the handle.
        /// </summary>
        /// <param name="handle"></param>
        public override void EndDownload(ref DownloadStatus status,
            Handle handle)
        {
            DMLCHandle dh = handle as DMLCHandle;
            dh.m_delgate.EndDownload(ref status, dh.m_handle);
        }

        public override void EndDownloadBatch(ref DownloadStatus status)
        {
            if (m_cache != null)
            {
                LogEntry results = new LogEntry("CacheResults");
                foreach (CacheEntry ce in m_cache)
                {
                    if (ce.source != null)
                    {
                        ce.source.EndDownloadBatch(ref status);
                        results.AddValue("Cache:" + ce.name, ce.count.ToString());
                    }
                }
                m_remote.LogValues(m_manager.UserDetails, results);
            }
            if (m_remote != null)
            {
                m_remote.EndDownloadBatch(ref status);
            }
            m_cache = null;
            m_recheckProxy = true;
        }

        /// <summary>
        /// Always delegate logging to the remote side.
        /// </summary>
        /// <param name="details"></param>
        /// <param name="entry"></param>
        public override void LogValues(UserDetails details, LogEntry entry)
        {
            m_remote.LogValues(details, entry);
        }

        public override String GetLastResponse()
        {
            return m_lastMessage;
        }
    }
}
