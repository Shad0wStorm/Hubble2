using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ClientSupport
{
    class DownloadManagerMock : DownloadManagerBase
    {
        String m_lastResponse;

        /// <summary>
        /// Directory used to store remote files provided by the server. In
        /// practice this is stored under m_connectionDir;
        /// </summary>
        private String m_fileStorePath;

        /// <summary>
        /// Directory used to store information about the MockServerConnection,
        /// such as the user configurations.
        /// </summary>
        private String m_connectionDir;

        /// <summary>
        /// Represent a file stored on the mock server which can be downloaded
        /// by the client.
        /// 
        /// Although file details are not currently loaded from a configuration
        /// file this may be required in future so normal naming conventions
        /// are suspended in favour of making the configuration easier to use.
        /// </summary>
        class MockFile
        {
            /// <summary>
            /// The project the file is associated with.
            /// </summary>
            public String project;

            /// <summary>
            /// The version of the project that must be installed for the file
            /// to be required, or null if no prior installation.
            /// </summary>
            public String version;

            /// <summary>
            /// Name of the source installer which is stored in the
            /// MockConnections/FileStore directory.
            /// </summary>
            public String remote;

            /// <summary>
            /// The name of the file when downloaded to the client. This may
            /// be different from the remote name.
            /// </summary>
            public String local;

            /// <summary>
            /// Any arguments that must be given to the installer when it is
            /// run.
            /// </summary>
            public String arguments;

            /// <summary>
            /// Checksum used to validate the file once it is downloaded.
            /// </summary>
            public String checksum;

            /// <summary>
            /// The size of the file. This is not stored in the configuration
            /// since it can be determined by inspection of the file on disk.
            /// </summary>
            public Int64 size;
            public MockFile(String p, String v, String r, String l, String a, String c)
            {
                project = p;
                version = v;
                remote = r;
                local = l;
                arguments = a;
                checksum = c;
                size = 0;
            }
        }

        /// <summary>
        /// List of files contained in the file store.
        /// </summary>
        private List<MockFile> m_fileStore;

        public DownloadManagerMock(String connection)
        {
            m_connectionDir = connection;
        }

		public override void ResetCache()
		{
			// Nothing to do
		}

        /// <summary>
        /// Get the installer associated with a user and project based on the
        /// currently installed version.
        /// 
        /// Checks the file store to see if there are any installers for the
        /// project and currently installed version.
        /// </summary>
        /// <param name="user">
        /// The user details for the user making the request.
        /// </param>
        /// <param name="project">
        /// The project being checked for updates.
        /// </param>
        /// <param name="version">
        /// The version currently installed if any.
        /// </param>
        /// <param name="details">
        /// The details of the version to install will be filled out if there
        /// is a new version available.
        /// </param>
        /// <returns>
        /// True if there is something to install, false if the project is up
        /// to date.
        /// </returns>
        public override InstallerVersionResult GetInstallerPath(UserDetails user, String project, String version, ref DownloadManagerBase.RemoteFileDetails details)
        {
            foreach (MockFile m in m_fileStore)
            {
                if ((m.project == project) && (m.version == version))
                {
                    details.RemotePath = m.remote;
                    details.LocalFileName = m.local;
                    details.FileSize = m.size;
                    details.LaunchArguments = m.arguments;
                    details.CheckSum = m.checksum;
                    return InstallerVersionResult.Update;
                }
            }
            details.RemotePath = null;
            details.LocalFileName = null;
            m_lastResponse = "File not available.";
            return InstallerVersionResult.Failed;
        }

        public override void BeginDownloadBatch(ref DownloadManagerBase.DownloadStatus status)
        {
        } 

        /// <summary>
        /// Handle used to keep track of an in progress download.
        /// </summary>
        class MockServerTransferHandle : DownloadManagerBase.Handle
        {
            public FileStream m_source;
        }

        /// <summary>
        /// Start a download.
        /// 
        /// Open the passed path storing the result in a newly created handle
        /// and return the handle for future use.
        /// </summary>
        /// <param name="user">Details for the authenticated user.</param>
        /// <param name="path">
        /// Path to open. This will have been set to the full path of the file
        /// during set up.
        /// </param>
        /// <returns>Created handle.</returns>
        public override DownloadManagerBase.Handle BeginDownload(ref DownloadManagerBase.DownloadStatus status,
            UserDetails user, DownloadManagerBase.RemoteFileDetails details)
        {
            MockServerTransferHandle handle = new MockServerTransferHandle();
            handle.m_source = new FileStream(details.RemotePath, FileMode.Open, FileAccess.Read);
            handle.m_source.Seek(details.Existing, SeekOrigin.Begin);
            return handle;
        }

        /// <summary>
        /// Download a chunk of data using the passed handle previously
        /// returned from BeginDownload.
        /// 
        /// The mock server reads the data from the current offset within the
        /// file stream returning it in the passed buffer.
        /// </summary>
        /// <param name="handle">
        /// Handle previously returned from BeginDownload.
        /// </param>
        /// <param name="buffer">Buffer to place the data in.</param>
        /// <param name="bufferSize">Size of the buffer to fill.</param>
        /// <returns>The number of bytes read.</returns>
        public override int DownloadChunk(ref DownloadManagerBase.DownloadStatus status,
            DownloadManagerBase.Handle handle, Byte[] buffer, int bufferSize)
        {
            MockServerTransferHandle msth = handle as MockServerTransferHandle;
            if (msth != null)
            {
                Thread.Sleep(1); // Introduce a delay
                return msth.m_source.Read(buffer, 0, bufferSize);
            }
            else
            {
                throw new System.IO.IOException("Invalid transfer handle");
            }
        }

        /// <summary>
        /// Finish an in progress download.
        /// 
        /// Close the opened stream.
        /// </summary>
        /// <param name="handle"></param>
        public override void EndDownload(ref DownloadManagerBase.DownloadStatus status,
            DownloadManagerBase.Handle handle)
        {
            MockServerTransferHandle msth = handle as MockServerTransferHandle;
            if (msth != null)
            {
                msth.m_source.Close();
            }
            else
            {
                throw new System.IO.IOException("Invalid transfer handle");
            }
        }

        public override void EndDownloadBatch(ref DownloadManagerBase.DownloadStatus status)
        {
        }

        public override String GetLastResponse() { return m_lastResponse;  }

        public override void LogValues(UserDetails details, LogEntry entry)
        {
            
        }

        /// <summary>
        /// Add the files we know about to the file store.
        /// 
        /// This should probably come from a file on disk to make it easier to
        /// modify without changing the source code, but since it is only
        /// relevant to mock server and we can use different projects to
        /// trigger different install paths this is sufficient for now.
        /// 
        /// We cannot trivially scan the disk to find the files since that
        /// would not provide the additional information such as dependent
        /// versions or the possibility of different local and remote names
        /// or installer command line arguments. Now we are using proper
        /// installers there is less opportunity to share installers between
        /// different projects. The explicit list approach also allows some
        /// files to be ignored if they are only wanted for some
        /// configurations.
        /// </summary>
        public void AddFiles()
        {
            m_fileStore = new List<MockFile>();
            m_fileStore.Add(new MockFile("Lave", null, "Lave.exe", "Lave.exe", null, "3039948641a17e2a2d2f2073bf386499"));
            m_fileStore.Add(new MockFile("Munchausen", null, "Munchausen.exe", "Munchausen.exe", null, "855cf836d0d99f4e262a98304c423d61"));
            m_fileStore.Add(new MockFile("Munchausen", "1.0.0.0", "Munchausen V1.1.exe", "Munchausen V1.1.exe", null, "f871deba1a5446e50ab9d2cd8144b381"));
            m_fileStore.Add(new MockFile("Munchausen", "1.1.0.0", "Munchausen V1.2.exe", "Munchausen V1.2.exe", null, "e813cc8ab74f262b6dc6187becbe28f2"));
        }

        /// <summary>
        /// Check the files that have been added to the file store actually
        /// exist and set the full path to the source file and set the expected
        /// size to be the size of the file on disk.
        /// </summary>
        public void ValidateFiles()
        {
            m_fileStorePath = Path.Combine(m_connectionDir, "FileStore");
            if (Directory.Exists(m_fileStorePath))
            {
                foreach (MockFile mf in m_fileStore)
                {
                    String targetPath = Path.Combine(m_fileStorePath, mf.remote);
                    FileInfo info = new FileInfo(targetPath);
                    if (info.Exists)
                    {
                        mf.remote = Path.GetFullPath(targetPath);
                        mf.size = info.Length;
                    }
                }
            }
        }


    }
}
