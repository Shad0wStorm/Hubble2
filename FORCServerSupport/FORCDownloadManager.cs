using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;

using ClientSupport;

namespace FORCServerSupport
{
    class FORCDownloadManager : DownloadManagerBase
    {
        private FORCServerState m_state;
        private ServerInterface m_server;
        private int m_filecount = 0;

        /// <summary>
        /// Handle used to track the download from a web server.
        /// </summary>
        private class FORCHandle : Handle
        {
            public long m_expected;
            public long m_received;
            public HttpWebRequest m_client;
            public HttpWebResponse m_response;
            public Stream m_stream;

            public void Close()
            {
                if (m_response != null)
                {
                    m_response.Close();
                }
                if (m_stream != null)
                {
                    m_stream.Close();
                }
                IDisposable disposable = m_stream as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
                disposable = m_response as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }

            }
        }

		private class IQCacheEntry
		{
			public RemoteFileDetails m_details = null;
			public InstallerVersionResult m_result = DownloadManagerBase.InstallerVersionResult.Failed;
		}
		private Dictionary<String, IQCacheEntry> m_IQCache;


        public FORCDownloadManager(FORCServerState state, ServerInterface server)
        {
            m_state = state;
            m_server = server;
			m_IQCache = new Dictionary<String, IQCacheEntry>();
        }

		public override void ResetCache()
		{
			m_IQCache.Clear();
		}

        /// <summary>
        /// Return details of a remote file to be downloaded and installed
        /// before the user can execute a particular project.
        /// </summary>
        /// <param name="user">Details of the user making the request.</param>
        /// <param name="project">
        /// Project for which the installer is being requested.
        /// </param>
        /// <param name="version">
        /// Currently installed version of the project, or null if no version
        /// is currently installed.
        /// </param>
        /// <param name="details">
        /// Details of the remote file that will be filled in by the server.
        /// </param>
        /// <returns>
        /// True if an installer is available, false otherwise.
        /// </returns>
        public override InstallerVersionResult GetInstallerPath(UserDetails user, String project, String version, ref RemoteFileDetails details)
        {
			String queryKey = user.EmailAddress + "~" + project;
			if (m_IQCache.ContainsKey(queryKey))
			{
				IQCacheEntry entry = m_IQCache[queryKey];
				details = new RemoteFileDetails(entry.m_details);
				return entry.m_result;
			}
            Queries.InstallerQuery iq = new Queries.InstallerQuery();
            m_filecount = 0;
			InstallerVersionResult result = iq.Run(m_state, user, project, version, ref details);
			if (result != InstallerVersionResult.Failed)
			{
				IQCacheEntry add = new IQCacheEntry();
				add.m_details = new RemoteFileDetails(details);
				add.m_result = result;
				m_IQCache[queryKey] = add;
			}
			else
			{
				// Blank out the details since whatever we had was invalid.
				details = new RemoteFileDetails();
			}
			return result;
        }

        public override void BeginDownloadBatch(ref DownloadStatus status)
        {
        }

        private CookieContainer GenerateCookies(Dictionary<String, String> cookieDictionary, String domain)
        {
            CookieContainer cookies = new CookieContainer();

            foreach (String key in cookieDictionary.Keys)
            {
                cookies.Add(new Cookie(key, cookieDictionary[key]) { Domain = domain });
            }

            return cookies;
        }

        private bool BuildHandle(FORCHandle handle, RemoteFileDetails details, out String Error)
        {
            try
            {
                FORCManager.EnsureIsUsingSecureTlsProtocol();
                handle.m_client = (HttpWebRequest)WebRequest.Create(details.RemotePath);
                // MCH - 20150505 - OSV-721
                // Search for above string to find related changes.
                // Disable keep alive since there appears to be a bug in .NET
                // that causes connections to be unexpectedly dropped resulting
                // in exceptions.
                handle.m_client.KeepAlive = false;
                handle.m_client.UserAgent = JSONWebQuery.UserAgent;
                handle.m_client.CachePolicy = new HttpRequestCachePolicy(Properties.Settings.Default.BypassCache ? HttpRequestCacheLevel.BypassCache : HttpRequestCacheLevel.Default);
                handle.m_client.Proxy = Proxy == null ? WebRequest.DefaultWebProxy : Proxy;

                if (m_state.m_manager.EnableGzipCompression) // Use Gzip if possible
                {
                    handle.m_client.Headers.Add("Accept-Encoding", "gzip");
                    handle.m_client.AutomaticDecompression = DecompressionMethods.GZip;
                }
                if (details.HasCookies())
                {
                    Uri remotePath = new Uri(details.RemotePath);
                    CookieContainer cookies = GenerateCookies(details.AccessCookies, remotePath.Host);
                    handle.m_client.CookieContainer = cookies;
                }
                if (details.Existing != 0)
                {
                    handle.m_client.AddRange((int)details.Existing, (int)details.FileSize);
                }

                handle.m_response = (HttpWebResponse)handle.m_client.GetResponse();
            }
            catch (System.Exception ex)
            {
                Error = String.Format(LocalResources.Properties.Resources.FSSDM_BeginDownloadX,
                    ex.Message);
                return false;
            }
            Error = null;
            return true;
        }

        /// <summary>
        /// Start the download of a file previously returned from
        /// GetInstallerPath.
        /// </summary>
        /// <param name="user">Details of the user making the request.</param>
        /// <param name="path">
        /// Path to the remote file as returned via the details parameter to
        /// GetInstallerPath.
        /// </param>
        /// <returns>
        /// A handle that may be used to continue the download
        /// </returns>
        public override Handle BeginDownload(ref DownloadStatus status,
            UserDetails user, RemoteFileDetails details)
        {
            FORCHandle handle = null;
            handle = new FORCHandle();
            handle.m_expected = details.FileSize;
            handle.m_received = 0;
            String Error = null;

            if (!BuildHandle(handle, details, out Error))
            {
                if (details.Existing > 0)
                {
                    details.Existing = 0;
                    BuildHandle(handle, details, out Error);
                }
            }

            if (!String.IsNullOrEmpty(Error))
            {
                throw new IOException(Error);
            }

            try
            {
                if (handle.m_response.StatusCode != HttpStatusCode.PartialContent)
                {
                    details.Existing = 0;
                }
                handle.m_stream = handle.m_response.GetResponseStream();
            }
            catch (System.Exception ex)
            {
                status.Error = String.Format(LocalResources.Properties.Resources.FSSDM_BeginDownloadResponseX,
                    ex.Message);
            }

            ++m_filecount;
            return handle;
        }

        /// <summary>
        /// Retrieve a chunk of data for a download previously started with a
        /// call to BeginDownload.
        /// </summary>
        /// <param name="handle">Handle returnd from BeginDownload.</param>
        /// <param name="buffer">Array of bytes to hold returned data.</param>
        /// <param name="bufferSize">
        /// Amount of data to download which must be less than the size of the
        /// passed byte array.
        /// </param>
        /// <returns>The number of bytes actually downloaded.</returns>
        public override int DownloadChunk(ref DownloadStatus status,
            Handle handle, Byte[] buffer, int bufferSize)
        {
            FORCHandle forchandle = handle as FORCHandle;
            if (forchandle != null)
            {
                long remaining = forchandle.m_expected - forchandle.m_received;
                int limit = bufferSize;
                if (remaining < bufferSize)
                {
                    limit = (int)remaining;
                }
                int received = forchandle.m_stream.Read(buffer, 0, limit);
                forchandle.m_received += received;
                return received;
            }
            return 0;
        }

        /// <summary>
        /// Finish a download previously started with a call to BeginDownload.
        /// 
        /// The server may release resources associated with the download and
        /// further requests using the same handle will fail.
        /// </summary>
        /// <param name="handle">
        /// The handle returned from a call to BeginDownload.
        /// </param>
        public override void EndDownload(ref DownloadStatus status,
            Handle handle)
        {
            FORCHandle forchandle = handle as FORCHandle;
            if (forchandle != null)
            {
                forchandle.Close();
            }
        }

        public override void LogValues(UserDetails details, LogEntry entry)
        {
            if (m_server!=null)
            {
                m_server.LogValues(details, entry);
            }
        }

        public override void EndDownloadBatch(ref DownloadStatus status)
        {
            if (m_server!=null)
            {
                LogEntry result = new LogEntry("FORCDownloadManagerReport");
                result.AddValue("filecount", m_filecount.ToString());
                m_server.LogValues(m_state.m_manager.UserDetails, result);
            }
        }

        public override String GetLastResponse()
        {
            if (m_server != null)
            {
                return m_server.GetLastContent();
            }
            return null;
        }
    }
}
