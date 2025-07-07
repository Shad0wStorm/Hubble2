using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ClientSupport
{
    public class DownloadManagerSingleFile : DownloadManagerBase
    {
        private String m_fileName;

        public DownloadManagerSingleFile(String path)
        {
            m_fileName = path;
        }

		public override void ResetCache()
		{}

        /// <summary>
        /// Return details of the file we were created with, calculating the
        /// checksum for verification purposes.
        /// </summary>
        /// <returns>
        /// True if an installer is available, false otherwise.
        /// </returns>
        public override InstallerVersionResult GetInstallerPath(UserDetails user, String project, String version, ref RemoteFileDetails details)
        {
            FileInfo fi = new FileInfo(m_fileName);
            if (!fi.Exists)
            {
                return InstallerVersionResult.Failed;
            }

            details.LocalFileName = Path.GetFileName(m_fileName);
            details.RemotePath = m_fileName;
            details.RemoteVersion = version + "U";
            details.LaunchArguments = null;
            details.FileSize = fi.Length;

            // If we have a checksum we cannot currently
            // validate it so assume we have invalid data.
            MD5 md5 = MD5.Create();
            using (Stream source = File.OpenRead(m_fileName))
            {
                Byte[] hash = md5.ComputeHash(source);
                details.CheckSum = DecoderRing.BytesToHex(hash);
            }

            return InstallerVersionResult.Update;
        }

        public override void BeginDownloadBatch(ref DownloadStatus status)
        {
        }

        class FISHandle : Handle
        {
            public FileStream m_source;
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
            FISHandle handle = new FISHandle();
            handle.m_source = new FileStream(details.RemotePath, FileMode.Open, FileAccess.Read);
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
            FISHandle fis = handle as FISHandle;
            if (fis != null)
            {
                fis.m_source.Close();
            }
            else
            {
                throw new System.IO.IOException("Invalid transfer handle");
            }
        }

        public override void EndDownloadBatch(ref DownloadStatus status)
        {
        }

        public override void LogValues(UserDetails details, LogEntry entry) { }

        public override String GetLastResponse() { return null; }
    }
}
