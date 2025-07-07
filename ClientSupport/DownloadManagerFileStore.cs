using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace ClientSupport
{
    public class DownloadManagerFileStore : DownloadManagerBase
    {
        public class DownloadManagerFileStoreException : Exception
        {
            public DownloadManagerFileStoreException() { }
            public DownloadManagerFileStoreException(String message) : base(message) { }
            public DownloadManagerFileStoreException(String message, Exception inner) : base(message, inner) { }
        }

        private String m_root;
        private ManifestFile m_activeManifest;
        private String m_manifestFile;
        private FileLogger m_log = null;
        private Mutex m_mutex = null;

        public DownloadManagerFileStore()
        {

        }

        public static DownloadManagerBase Create(String settingsPath)
        {
            DownloadManagerFileStore store = null;
            if (File.Exists(settingsPath))
            {
                String[] contents = File.ReadAllLines(settingsPath);
                if (contents.Length > 0)
                {
                    String fileStoreLocation = contents[0];
                    if (Directory.Exists(fileStoreLocation))
                    {
                        String fileStoreConfig = Path.Combine(fileStoreLocation, "FileStore.cfg");
                        if (File.Exists(fileStoreConfig))
                        {
                            // Close enough
                            store = new DownloadManagerFileStore();
                            store.SetFileStore(fileStoreLocation);
                            if (contents.Length>1)
                            {
                                store.SetActiveManifest(contents[1]);
                            }
                        }
                    }
                }
            }
            return store;
        }

        public void SetFileStore(String path)
        {
            m_root = path;
        }

        public void SetSerialiseAccess()
        {
            if (m_mutex == null)
            {
                m_mutex = new Mutex();
            }
        }

        public void EnableLog()
        {
            String fileStoreLog = Path.Combine(m_root, "DMFS.log");
            m_log = new FileLogger();
            m_log.SetLogFile(fileStoreLog);
            LogEntry connect = new LogEntry("Connect");
            connect.AddValue("Reason", "DownloadManagerFileStore connecting to FileStore");
            connect.AddValue("Active", m_manifestFile);
            m_log.Log(null, connect);
        }

        public void SetActiveManifest(String manifest)
        {
            String manifestPath = Path.Combine(m_root, "manifest");
            manifestPath = Path.Combine(manifestPath, manifest);

            if (!File.Exists(manifestPath))
            {
                throw new DownloadManagerFileStoreException("Manifest file " + manifestPath + " missing.");
            }
            m_activeManifest = new ManifestFile();
            using (FileStream manifestStream = new FileStream(manifestPath,FileMode.Open, FileAccess.Read))
            {
                Stream source = manifestStream;
                if (manifestPath.ToLowerInvariant().EndsWith(".gz"))
                {
                    GZipStream gzip = new GZipStream(manifestStream, CompressionMode.Decompress);
                    source = gzip;
                }
                if (!m_activeManifest.LoadStream(source))
                {
                    throw new DownloadManagerFileStoreException("Failed to load manifest " + manifestPath);
                }
                if (source != manifestStream)
                {
                    source.Dispose();
                }
            }
            m_manifestFile = manifestPath;
        }

        public String[] GetAvailableManifests()
        {
            String manifestPath = Path.Combine(m_root, "manifest");

            if (!Directory.Exists(manifestPath))
            {
                return null;
            }

            String[] files = Directory.GetFiles(manifestPath);
            List<String> availableManifests = new List<String>();
            foreach (String f in files)
            {
                String ext = Path.GetExtension(f);
                if (ext == ".gz")
                {
                    String p = Path.GetFileNameWithoutExtension(f);
                    ext = Path.GetExtension(p);
                }
                if (ext == ".xml")
                {
                    availableManifests.Add(Path.GetFileName(f));
                }
            }

            if (availableManifests.Count > 0)
            {
                return availableManifests.ToArray();
            }

            return null;
        }

		public override void ResetCache()
		{
			// No caching implemented, nothing to do.
		}

        public override InstallerVersionResult GetInstallerPath(UserDetails user, String project, String version, ref RemoteFileDetails details)
        {
            FileInfo info = new FileInfo(m_manifestFile);
            if ((!info.Exists) || (m_activeManifest == null))
            {
                return InstallerVersionResult.Failed;
            }
            details.LocalFileName = info.Name;
            details.Existing = 0;
            details.FileSize = info.Length;
            details.RemotePath = m_manifestFile;
            details.RemoteVersion = m_activeManifest.ProductVersion;
            DecoderRing ring = new DecoderRing();
            long length;
            details.CheckSum = ring.MD5EncodeFile(m_manifestFile, out length);
            return version==m_activeManifest.ProductVersion ? InstallerVersionResult.Current : InstallerVersionResult.Update;
        }

        public override void BeginDownloadBatch(ref DownloadStatus status)
        {
            
        }

        class FISHandle : Handle
        {
            public FileStream m_source;
        }

        public override Handle BeginDownload(ref DownloadStatus status,
            UserDetails user, RemoteFileDetails details)
        {

            FISHandle handle = new FISHandle();
            String filePath = details.RemotePath;
            if (!System.IO.File.Exists(filePath))
            {
                filePath = DetermineFilePath(filePath);
                if (!System.IO.File.Exists(filePath))
                {
                    filePath = DetermineFilePath(details.CheckSum);
                }
            }
            handle.m_source = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            if (handle.m_source.Length != details.FileSize)
            {
                throw new System.IO.FileNotFoundException();
            }
            if (details.Existing != 0)
            {
                handle.m_source.Seek(details.Existing, SeekOrigin.Begin);
            }
            if (m_mutex!=null)
            {
                m_mutex.WaitOne();
            }
            return handle;
        }

        private String DetermineFilePath(String path)
        {
            String filePath = path;
            String parent = System.IO.Path.Combine(m_root, "files");
            if (filePath.Length > 2)
            {
                parent = System.IO.Path.Combine(parent, path.Substring(0, 2));
                filePath = path.Substring(2);
            }
            filePath = System.IO.Path.Combine(parent, filePath);
            return filePath;
        }

        public override int DownloadChunk(ref DownloadStatus status,
            Handle handle, Byte[] buffer, int bufferSize)
        {
            FISHandle fis = handle as FISHandle;
            if (fis != null)
            {
                return fis.m_source.Read(buffer, 0, bufferSize);
            }
            else
            {
                throw new System.IO.IOException("Invalid transfer handle");
            }
        }

        public override void EndDownload(ref DownloadStatus status,
            Handle handle)
        {
            FISHandle fis = handle as FISHandle;
            if (fis != null)
            {
                fis.m_source.Close();
                if (m_mutex != null)
                {
                    m_mutex.ReleaseMutex();
                }
            }
            else
            {
                throw new System.IO.IOException("Invalid transfer handle");
            }
        }

        public override void EndDownloadBatch(ref DownloadStatus status)
        {

        }

        public override void LogValues(UserDetails details, LogEntry entry) 
        {
            if (m_log!=null)
            {
                m_log.Log(details, entry);
            }
        }

        public override String GetLastResponse() { return null; }
    }
}
