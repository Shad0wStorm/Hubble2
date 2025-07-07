using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace ClientSupport
{
    public abstract class DownloadManagerBase : LogServer
    {
        /// <summary>
        /// Details of a remote file available on the server.
        /// </summary>
        public class RemoteFileDetails
        {
            /// <summary>
            /// Path to the file on the server, typically used as a URL to
            /// retrieve.
            /// </summary>
            public String RemotePath;
            /// <summary>
            /// The file name (without path) to use when saving the file
            /// locally.
            /// </summary>
            public String LocalFileName;
            /// <summary>
            /// Checksum. Currently unused, when provided by the server it can
            /// be used to confirm that the downloaded file contents match
            /// those on the server.
            /// </summary>
            public String CheckSum;
            /// <summary>
            /// Any arguments that must be passed to the installer.
            /// </summary>
            public String LaunchArguments;
            /// <summary>
            /// Size of the file in bytes.
            /// </summary>
            public Int64 FileSize;
            /// <summary>
            /// Number of bytes of the file already available locally.
            /// </summary>
            public Int64 Existing;
            /// <summary>
            /// Version on the server.
            /// </summary>
            public String RemoteVersion;
            /// <summary>
            /// Dictionary of cookies required when accessing the file from the server
            /// </summary>
            public Dictionary<String, String> AccessCookies;
            /// <summary>
            /// Constructor.
            /// </summary>
            public RemoteFileDetails()
            {
                RemotePath = null;
                LocalFileName = null;
                CheckSum = null;
                LaunchArguments = null;
                FileSize = 0;
                Existing = 0;
                RemoteVersion = null;
                AccessCookies = new Dictionary<String, String>();
            }

			public RemoteFileDetails(RemoteFileDetails other)
			{
				RemotePath = other.RemotePath;
				LocalFileName = other.LocalFileName;
				CheckSum = other.CheckSum;
				LaunchArguments = other.LaunchArguments;
				FileSize = other.FileSize;
				Existing = other.Existing;
				RemoteVersion = other.RemoteVersion;
                AccessCookies = new Dictionary<String, String>(other.AccessCookies);
			}

            public bool HasCookies()
            {
                return AccessCookies.Count > 0;
            }
        }

        public class BehaviourModifications
        {
            bool m_cache; 
            public BehaviourModifications(bool usecache)
            {
                m_cache = usecache;
            }

            public virtual bool UseCache() { return m_cache; }
        }

        public enum InstallerVersionResult { Current, Update, Failed };
        /// <summary>
        /// Base class for handles used by the server to manage downloads in
        /// pieces. The server is free to use it as it chooses, users of the
        /// ServerInterface simply pass it back in when continuing a task which
        /// is split into multiple chunks.
        /// </summary>
        public class Handle { }

        public BehaviourModifications Modifications = null;
        public WebProxy Proxy = null;

		/// <summary>
		/// Reset any internal caching
		/// </summary>
		public abstract void ResetCache();

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
        /// Current if the version on the server matches the local version.
        /// Update if the version on the server is different from the local version.
        /// Failed if an error occurred obtaining the path.
        /// </returns>
        public abstract InstallerVersionResult GetInstallerPath(UserDetails user, String project, String version, ref RemoteFileDetails details);

        public class DownloadStatus
        {
            private bool m_success = true;
            public bool Success
            {
                get { return m_success; }
                set { m_success = value; }
            }

            private String m_error = null;
            public String Error
            {
                get
                {
                    return m_error;
                }
                set
                {
                    m_error = value;
                    Success = false;
                }
            }
        }

        /// <summary>
        /// Give the download manager chance to some set up to allow
        /// optimisation over a series of file downloads.
        /// </summary>
        public abstract void BeginDownloadBatch(ref DownloadStatus status);

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
        public abstract Handle BeginDownload(ref DownloadStatus status,
            UserDetails user, RemoteFileDetails details);

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
        public abstract int DownloadChunk(ref DownloadStatus status,
            Handle handle, Byte[] buffer, int bufferSize);

        /// <summary>
        /// Finish a download previously started with a call to BeginDownload.
        /// 
        /// The server may release resources associated with the download and
        /// further requests using the same handle will fail.
        /// </summary>
        /// <param name="handle">
        /// The handle returned from a call to BeginDownload.
        /// </param>
        public abstract void EndDownload(ref DownloadStatus status,
            Handle handle);

        /// <summary>
        /// Tidy up any optimisations required for the download batch.
        /// </summary>
        public abstract void EndDownloadBatch(ref DownloadStatus status);

        /// <summary>
        /// Return any additional text regarding the last action.
        /// Used to return error messages where a download call fails.
        /// </summary>
        public abstract String GetLastResponse();
    }
}
