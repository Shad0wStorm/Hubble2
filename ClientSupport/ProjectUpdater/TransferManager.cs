using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using ClientSupport.Utils;
using LocalResources;

namespace ClientSupport.ProjectUpdater
{
    class TransferManager
    {
        /// <summary>
        /// The downloader to use to perform the update.
        /// </summary>
        private DownloadManagerBase m_downloader;
        public DownloadManagerBase Downloader { get { return m_downloader; } }
        public bool HasDownloader { get { return m_downloader != null; } }

        /// <summary>
        /// The authenticated user requesting the update.
        /// </summary>
        private UserDetails m_user;
        public UserDetails User { get { return m_user; } }

        private UpdateStatus m_status;
        private ProgressMonitor m_monitor;

        private Utils.BufferManager m_buffer = null;
        public BufferManager Buffer { get { return m_buffer; } }

        private ProjectUpdateLog m_pulog;
        private FileOps m_fileops;

        public TransferManager(DownloadManagerBase downloader,
            UserDetails user,
            UpdateStatus status,
            ProgressMonitor monitor,
            ProjectUpdateLog updateLog,
            FileOps ops)
        {
            m_downloader = downloader;
            m_user = user;
            m_status = status;
            m_monitor = monitor;
            m_pulog = updateLog;
            m_fileops = ops;
        }

        public void Log(LogEntry entry)
        {
            if (HasDownloader)
            {
                m_downloader.LogValues(m_user, entry);
            }
        }

        public void PULog(LogEntry entry)
        {
            if (m_pulog != null)
            {
                m_pulog.Log(entry);
            }
        }

        /// <summary>
        /// Determine the most recent installer details from the server.
        /// </summary>
        public void DetermineInstaller()
        {
            m_monitor.StartAction(m_status.Project.Name,
                LocalResources.Properties.Resources.PU_DetermineVersion);

            DownloadManagerBase.InstallerVersionResult result;
            result = Downloader.GetInstallerPath(User, m_status.Project.Name,
                m_status.Project.Version, ref m_status.Remote);
			LogEntry installer = new LogEntry("DetermineInstaller");
			installer.AddValue("Project", m_status.Project.Name);
			installer.AddValue("Installer", m_status.Remote.LocalFileName);
			int index = m_status.Remote.RemotePath.LastIndexOf('/');
			if (index>=0)
			{
				installer.AddValue("Remote", m_status.Remote.RemotePath.Substring(index));
			}
			Log(installer);
            bool valid = result != DownloadManagerBase.InstallerVersionResult.Failed;
            if (!valid)
            {
                m_status.SetError(Downloader.GetLastResponse());
            }

            m_monitor.CompleteAction(m_status.Project.Name);
        }

        public void StartDownload(long maxSize)
        {
            if (maxSize > 0)
            {
                m_buffer = new Utils.BufferManager(maxSize);
            }
            else
            {
                m_buffer = null;
            }
        }

        /// <summary>
        /// Download the requested file into a memory stream ready for reading.
        /// </summary>
        /// <param name="details">Details of file to download</param>
        /// <returns>Memory stream containing the downloaded data ready for reading.</returns>
        public MemoryStream DownloadStream(ref DownloadManagerBase.DownloadStatus status,
            DownloadManagerBase.RemoteFileDetails details)
        {
            byte[] buffer = new byte[details.FileSize];
            MemoryStream download = new MemoryStream(buffer, true);
            PerformTransfer(ref status, download, null, details, null, false);
            download.Seek(0, SeekOrigin.Begin);
            return download;
        }

        /// <summary>
        /// Test whether the returned stream corresponds to the remote file
        /// details provided.
        /// 
        /// This will test that the length of the stream and the MD5 checksum
        /// both match.
        /// 
        /// The data will be tested from the start of the stream, and the
        /// stream will be reset to the start on completion.
        /// </summary>
        /// <param name="source">Stream to check</param>
        /// <param name="details">Details to match against</param>
        /// <returns>true on match, false otherwise.</returns>
        public bool ValidateStream(Stream source, DownloadManagerBase.RemoteFileDetails details)
        {
            m_pulog.Log("StartValidateManifest", m_status.Remote.LocalFileName);
            DecoderRing check = new DecoderRing();
            long length = 0;
            String manifestHash = check.MD5EncodeStream(source, out length);
            source.Position = 0;
            if (length != m_status.Remote.FileSize)
            {
                m_status.Equals(String.Format(LocalResources.Properties.Resources.PU_ManifestValidateSizeFail,
                    length, m_status.Remote.FileSize));
                if (HasDownloader)
                {
                    LogEntry sizemismatch = new LogEntry("ManifestSizeError");
                    sizemismatch.AddValue("Expected", m_status.Remote.FileSize.ToString());
                    sizemismatch.AddValue("Received", length.ToString());
                    Log(sizemismatch);
                    m_pulog.Log(sizemismatch);
                }
            }
            else
            {
                if (manifestHash != m_status.Remote.CheckSum)
                {
                    m_status.SetError(LocalResources.Properties.Resources.PU_ManifestValidateCheckSumFail);
                    if (HasDownloader)
                    {
                        LogEntry sizemismatch = new LogEntry("ManifestChecksumError");
                        sizemismatch.AddValue("Expected", m_status.Remote.CheckSum.ToString());
                        sizemismatch.AddValue("Received", manifestHash.ToString());
                        Log(sizemismatch);
                        m_pulog.Log(sizemismatch);
                    }
                }
            }
            m_pulog.Log("FinishValidateManifest", m_status.Remote.LocalFileName);
            return m_status.m_success;
        }

        public void DownloadFile(ref DownloadManagerBase.DownloadStatus status,
            DownloadManagerBase.RemoteFileDetails details,
            bool allowPartialDownload, UpdateStatus.ProgressUpdate reportProgress)
        {
            // Use a temporary file so we do not leave something lying
            // around that looks like a valid file.
            m_pulog.Log("StartDownload", details.LocalFileName);

            String targetName = details.LocalFileName;
            if (!System.IO.Path.IsPathRooted(targetName))
            {
                targetName = Path.Combine(m_status.Project.InstallationDirectory, details.LocalFileName);
            }
            String partFile = targetName + ".part";
            String progressFile = null;

            if (allowPartialDownload)
            {
                progressFile = targetName + ".progress";
            }

			try
			{
				using (FileStream download = new FileStream(partFile, FileMode.OpenOrCreate,
					FileAccess.Write))
				{
					// Set the length of the file to preallocate the space
					// to ensure the space is available and reduce
					// fragmentation.
					download.SetLength(details.FileSize);

					// Make sure we start writing at the start of the file.
					download.Seek(0, SeekOrigin.Begin);

					ASCIIEncoding uni = new ASCIIEncoding();

					if (progressFile != null)
					{
						try
						{
							if (File.Exists(progressFile))
							{
								// We have an existing partial download so see if
								// we can make use of it.
								using (FileStream progress = new FileStream(progressFile,
									FileMode.Open, FileAccess.Read))
								{
									byte[] progressBuffer = new byte[progress.Length];
									char[] splitchars = { '/' };
									if (progress.Read(progressBuffer, 0,
										(int)progress.Length) == progress.Length)
									{
										String content = uni.GetString(progressBuffer);
										String[] values = content.Split(splitchars, 3);
										int received;
										int expected;
										int.TryParse(values[0], out received);
										int.TryParse(values[1], out expected);
										if ((expected == details.FileSize) && (received != expected))
										{
											if ((details.RemoteVersion == values[2]) ||
												(String.IsNullOrEmpty(details.RemoteVersion) &&
												 String.IsNullOrEmpty(values[2])))
											{
												details.Existing = received;
											}
										}
									}
								}
							}
						}
						catch (Exception)
						{
							// Ignore the exception, just treat it as if the
							// progress file did not exist.
						}
					}

					try
					{
						PerformTransfer(ref status, download, progressFile, details, reportProgress, true);
					}
					catch (System.Exception ex)
					{
						status.Error = String.Format(LocalResources.Properties.Resources.PU_DownloadFileExc,
							ex.Message);
						LogEntry lf = new LogEntry("DownloadException");
						lf.AddValue("exception", ex.Message);
						try
						{
							Log(lf);
						}
						catch (System.Exception)
						{
							// If the network is down logging may fail too so guard
							// against that eventuality, but do not report it
							// again.
						}
					}
				}
			}
			catch (System.Exception ex)
			{
				status.Error = String.Format(LocalResources.Properties.Resources.PU_CreateFileExc,
					ex.Message);
				LogEntry lf = new LogEntry("CreateFileException");
				lf.AddValue("exception", ex.Message);
				try
				{
					Log(lf);
				}
				catch (System.Exception)
				{
					// If the network is down logging may fail too so guard
					// against that eventuality, but do not report it
					// again.
				}
			}

            if (status.Success)
            {
                // Download completed so remove any existing installer
                // and move the downloaded file into the installer
                // name. This would happen if the file has been
                // previously downloaded but corrupted or otherwise
                // failed verification.
                m_fileops.RemoveFile(targetName);

                // Download completed so remove the file used to
                // indicate a partial download.
                m_fileops.RemoveFile(progressFile);

                // Move the downloaded file to the name expected for
                // installation.
                File.Move(partFile, targetName);
                m_pulog.Log("RenameFile", targetName);
            }
            m_pulog.Log("FinishDownload", details.LocalFileName);
        }

        public void PerformTransfer(ref DownloadManagerBase.DownloadStatus status,
            Stream target, String progressFile,
            DownloadManagerBase.RemoteFileDetails details, UpdateStatus.ProgressUpdate reportProgress,
            bool hashCheck)
        {
            // Perform the transfer in chunks so we can provide
            // decent progress reports.
            DownloadManagerBase.Handle transfer = m_downloader.BeginDownload(ref status, m_user, details);
            String activeProgressFile = progressFile;

            if (transfer != null)
            {
                target.Seek(details.Existing, SeekOrigin.Begin);

                Utils.BufferManager buffer = m_buffer;
                if (buffer == null)
                {
                    buffer = new Utils.BufferManager(details.FileSize);
                }

                if (buffer.Size > details.FileSize)
                {
                    // Only one chunk, so never use a progress file.
                    activeProgressFile = null;
                }
                Int64 downloaded = details.Existing;

                using (SHA1 sha = ((downloaded == 0) && hashCheck) ? new SHA1CryptoServiceProvider() : null)
                {
                    using (FileStream progress = !String.IsNullOrEmpty(activeProgressFile) ?
                        new FileStream(activeProgressFile, FileMode.OpenOrCreate, FileAccess.Write) : null)
                    {
                        ASCIIEncoding uni = null;
                        if (progress != null)
                        {
                            uni = new ASCIIEncoding();
                        }

                        // If we are not downloading from the start of the file
                        // do not bother reading back the previous data just
                        // assume there was a non-zero in the data somewhere.
                        bool nonZero = details.Existing != 0;
                        while (downloaded < details.FileSize)
                        {
                            int got = m_downloader.DownloadChunk(ref status, transfer, buffer.Data, buffer.Size);
                            if (got > 0)
                            {
                                if (!nonZero)
                                {
                                    if (got <= buffer.Size)
                                    {
                                        for (int b = 0; b < got; ++b)
                                        {
                                            if (buffer.Data[b] != 0)
                                            {
                                                nonZero = true;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LogEntry timeout = new LogEntry("ZeroFileDetectorFail");
                                        timeout.AddValue("BufferSize", buffer.Size);
                                        timeout.AddValue("Received", got);
                                        Log(timeout);
                                    }
                                }
                            }
                            else
                            {
                                status.Error = LocalResources.Properties.Resources.PU_TransferReadFailed;
                                LogEntry timeout = new LogEntry("TransferReadFailed");
                                Log(timeout);
                                break;
                            }
                            target.Write(buffer.Data, 0, got);
                            if (sha != null)
                            {
                                sha.TransformBlock(buffer.Data, 0, got, buffer.Data, 0);
                            }
                            downloaded += got;

                            if (progress != null)
                            {
                                String progressString = downloaded.ToString() + "/";
                                progressString += details.FileSize.ToString() + "/";
                                progressString += details.RemoteVersion;
                                progress.Seek(0, SeekOrigin.Begin);
                                byte[] bytes = uni.GetBytes(progressString);
                                progress.Write(bytes, 0, bytes.Length);
                                progress.SetLength(bytes.Length);
                            }
                            if (reportProgress != null)
                            {
                                reportProgress(downloaded, details.FileSize);
                            }
                            if (m_monitor.CancellationRequested())
                            {
                                status.Error = "Download cancelled";
                                Log(new LogEntry("DownloadFileCancelled"));
                                break;
                            }
                        }

                        if ((!nonZero) && (downloaded > 0))
                        {
                            LogEntry nullFile = new LogEntry("Null file detected");
                            nullFile.AddValue("LocalFile", details.LocalFileName);
                            nullFile.AddValue("LocalFileSize", details.FileSize);
                            Log(nullFile);
                        }
                        if (downloaded != details.FileSize)
                        {
                            LogEntry nullFile = new LogEntry("DownloadSizeMismatch");
                            nullFile.AddValue("Downloaded", downloaded);
                            nullFile.AddValue("ExpectedSize", details.FileSize);
                            Log(nullFile);
                        }
                    }
                    if (sha != null)
                    {
                        sha.TransformFinalBlock(buffer.Data, 0, 0);
                        String hashString = DecoderRing.BytesToHex(sha.Hash);
                        if (hashString != details.CheckSum)
                        {
                            LogEntry nullFile = new LogEntry("DownloadedDataMismatch");
                            nullFile.AddValue("Downloaded", hashString);
                            nullFile.AddValue("Expected", details.CheckSum);
                            nullFile.AddValue("LocalFile", details.LocalFileName);
                            nullFile.AddValue("RemoteFile", details.RemotePath);
                            Log(nullFile);
                        }
                    }
                }
                m_downloader.EndDownload(ref status, transfer);
            }
        }

        public void StopDownload()
        {
            m_buffer = null;
        }

        public void BeginDownloadBatch()
        {
            DownloadManagerBase.DownloadStatus dls = new DownloadManagerBase.DownloadStatus();
            m_downloader.BeginDownloadBatch(ref dls);
            if (!dls.Success)
            {
                m_status.SetError(dls.Error);
            }
        }

        public void EndDownloadBatch()
        {
            DownloadManagerBase.DownloadStatus dls = new DownloadManagerBase.DownloadStatus();
            m_downloader.EndDownloadBatch(ref dls);
            if (!dls.Success)
            {
                m_status.SetError(dls.Error);
            }
        }

        /*
         * Need to resolve issues with multi-threaded download handling errors
         * incorrectly.
         * 
         * The problem:
         * Currently m_status.success is used to determine whether an error has
         * occurred. This is part of the download state, and not specific to
         * any given thread, so setting the error on one thread immediately
         * marks every other thread as an error. This does not prevent the
         * threads continuing with other downloads, but does prevent the final
         * copy occurring so it is not possible to make use of the downloaded
         * data. As a secondary issue the current "GetLastReport" approach to
         * handling errors via DownloadManagerBase means that if several
         * threads report errors there is no assurance that the reported error
         * is the one that happened on the current thread.
         * 
         * Potential solution:
         * Pass in a local status object with the calls to TransferManager,
         * which in turn should pass it on to DownloadManagerBase. Use that
         * status object to track failures per thread. For the standard
         * synchronise process the results can be simply copied to the main
         * status object. For the multi-threaded approach the results can be
         * collated. We may want to group errors by type, and discard or
         * reduce the severity of those actions which are subsequently 
         * completed successfully due to the retry system. Since this changes
         * the DownloadManagerBase interface the effects will be wide ranging.
         */
    }
}
