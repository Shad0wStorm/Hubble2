using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using LocalResources;

namespace ClientSupport.ProjectUpdater
{
    class UpdateByManifest
    {
        private UpdateStatus m_status;
        private TransferManager m_transfer;
        private ProgressMonitor m_monitor;
        private ProjectUpdateLog m_pulog;
        private FileOps m_fileops;
        private int m_changes;

        public int Changes { get { return m_changes; } }

        public UpdateByManifest(UpdateStatus status,
            TransferManager transfer,
            ProgressMonitor monitor,
            ProjectUpdateLog log,
            FileOps ops)
        {
            m_status = status;
            m_transfer = transfer;
            m_monitor = monitor;
            m_pulog = log;
            m_fileops = ops;
            m_changes = 0;
        }

        public void ProcessManifest(UpdateOptions options)
        {
            if (m_status.m_success)
            {
                m_transfer.Log(new LogEntry("DownloadManifest"));

                if (m_status.m_mode == UpdateStatus.UpdateMode.PreFlightCheck)
                {
                    if (m_status.Remote.RemoteVersion != m_status.Project.Version)
                    {
                        m_transfer.Log(new LogEntry("CannotPlayOutOfDateGame"));
                        m_status.m_success = false;
                        return;
                    }
                }

                m_monitor.StartProgressAction(m_status.Project.Name,
                    LocalResources.Properties.Resources.PU_ManifestDownload,
                    m_status.Remote.FileSize, true);

                ManifestFile manifest = null;
                try
                {
                    // Note we do not download the manifest to a file. Just
                    // because we left the door unlocked, no need to stick a
                    // sign down the end of the road telling everyone.
                    DownloadManagerBase.DownloadStatus dls = new DownloadManagerBase.DownloadStatus();
                    m_pulog.Log("StartDownloadManifest", m_status.Remote.LocalFileName);
                    MemoryStream memory = m_transfer.DownloadStream(ref dls, m_status.Remote);
                    m_pulog.Log("FinishDownloadManifest", m_status.Remote.LocalFileName);
                    if (!dls.Success)
                    {
                        m_status.SetError(dls.Error);
                    }


                    if (m_transfer.ValidateStream(memory, m_status.Remote))
                    {
                        Stream content = memory;
                        if (m_status.Remote.LocalFileName.ToLowerInvariant().EndsWith(".gz"))
                        {
                            content = new GZipStream(memory, CompressionMode.Decompress);
                        }
                        manifest = new ManifestFile();
                        m_pulog.Log("StartParseManifest", null);
                        if (!manifest.LoadStream(content))
                        {
                            m_status.SetError(LocalResources.Properties.Resources.PU_ManifestParseFailed);
                            manifest = null;
                        }
                        m_pulog.Log("FinishParseManifest", null);
#if DEVELOPMENT
                        memory.Seek(0, SeekOrigin.Begin);
                        String manifestPath = Path.GetDirectoryName(m_status.Project.ProjectDirectory);
                        using (FileStream save = new FileStream(Path.Combine(manifestPath, m_status.Remote.LocalFileName),
                            FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            byte[] buffer = new byte[memory.Length];
                            int read = memory.Read(buffer, 0, (int)memory.Length);
                            save.Write(buffer, 0, read);
                        }
#endif
                        if (content != memory)
                        {
                            content.Dispose();
                        }
                        memory.Dispose();
                    }
                }
                catch (System.Exception se)
                {
                    m_status.SetError(String.Format(LocalResources.Properties.Resources.PU_ManifestDownloadExc,
                        se.Message));
                    m_transfer.Log(new LogEntry("DownloadManifestException"));
                }

                m_transfer.Log(new LogEntry("DownloadManifestCompleted"));

                if (manifest != null)
                {


                    m_monitor.StartAction( m_status.Project.Name,
                                           LocalResources.Properties.Resources.PU_ManifestProcessing );
                    
                    // Reset the executable hash. If we find the executable in the
                    // manifest it will be updated, if not there is something odd going
                    // on.
                    m_status.Project.ExecutableHash = null;

                    if ((options==null) || (options.DisableFastDownload) || (m_status.Project.MaxDownloadThreads<0))
                    {
                        SynchroniseManifestContents(manifest);
                    }
                    else
                    {
                        FastSynchroniseManifestContents(manifest);
                    }

                    m_monitor.CompleteAction( m_status.Project.Name );
                }


                m_status.InstalledFromFile(System.IO.Path.GetFileName(m_status.Remote.LocalFileName));
            }
        }

        private void PrescanManifestEntries(ManifestFile manifest, PathLengthLimit limit, ManifestBundler bundler)
        {
            foreach (ManifestFile.ManifestEntry entry in manifest.Entries)
            {
                if (ProcessManifestEntry(entry))
                {
                    if (m_status.Remote.HasCookies())
                    {
                        entry.AccessCookies = m_status.Remote.AccessCookies;
                    }
                    m_status.IncludeFile(entry.Size);
                    if (bundler != null)
                    {
                        bundler.AddEntry(entry);
                    }
                }
                if (limit.Include(entry.Path))
                {
                    m_status.m_success = false;
                    m_status.m_error = LocalResources.Properties.Resources.PU_PathTooLong;
                    break;
                }

                // Check to see if the user cancelled the process
                m_status.CheckForCancellation();
                if ( !m_status.m_success )
                {
                    break;
                }
            }
        }

        private void SynchroniseManifestContents(ManifestFile manifest)
        {
            m_pulog.Log("StartSynchroniseManfiestContents", null);
            ManifestStatistics stats = new ManifestStatistics();
            m_transfer.Log(new LogEntry("SynchroniseStarted"));

            PathLengthLimit limit = new PathLengthLimit(m_status.Project.ProjectDirectory);

            PrescanManifestEntries(manifest, limit, null);

            if (m_status.m_success)
            {
                m_transfer.StartDownload(m_status.LargestFileSize);

                long progress = 0;

                m_monitor.StartProgressAction(m_status.Project.Name,
                    LocalResources.Properties.Resources.PU_ManifestContentsSync,
                    m_status.TotalSize, true);

                PartialDownloadProgress pdp = new PartialDownloadProgress(m_monitor, m_status.Project.Name);
                foreach (ManifestFile.ManifestEntry entry in manifest.Entries)
                {
                    if (ProcessManifestEntry(entry))
                    {
                        long startProgress = progress;
                        pdp.SetInitial(startProgress);
                        stats.ConsiderFile(entry.Path);
                        SynchroniseManifestEntry(manifest, entry, stats, pdp);
                        progress = startProgress + entry.Size;
                        m_monitor.ReportActionProgress(m_status.Project.Name, progress);
                        m_status.CheckForCancellation();
                        if (!m_status.m_success)
                        {
                            break;
                        }
                    }
                }

                if (m_status.m_mode == UpdateStatus.UpdateMode.DownloadAndInstall)
                {
                    // Only do a discard for an actual update.
                    if (m_status.m_success)
                    {
                        DiscardNonManifestFiles(manifest, stats);
                    }

                    UpdateVersion();

                    stats.Write(System.IO.Path.Combine(m_status.Project.ProjectDirectory, "Update.log"));
                }
                m_changes = stats.ChangeCount;

                m_monitor.CompleteAction(m_status.Project.Name);
                LogEntry syncComplete = new LogEntry("SynchroniseCompleted");
                syncComplete.AddValue("Added", stats.Added.ToString());
                syncComplete.AddValue("Updated", stats.Updated.ToString());
                syncComplete.AddValue("Skipped", stats.Skipped.ToString());
                syncComplete.AddValue("Removed", stats.Removed.ToString());
                syncComplete.AddValue("Copied", stats.Copied.ToString());
                syncComplete.AddValue("Downloaded", stats.Downloaded.ToString());
                m_transfer.Log(syncComplete);

                m_transfer.StopDownload();
            }
            m_pulog.Log("FinishSynchroniseManfiestContents", null);
        }

        private void FastSynchroniseManifestContents(ManifestFile manifest)
        {
            m_pulog.Log("StartFastSynchroniseManfiestContents", null);
            ManifestStatistics stats = new ManifestStatistics();
            m_transfer.Log(new LogEntry("FastSynchroniseStarted"));

            PathLengthLimit limit = new PathLengthLimit(m_status.Project.ProjectDirectory);

            ManifestBundler bundler = new ManifestBundler();
            m_pulog.Log("StartPopulateBundler",null);
            PrescanManifestEntries(manifest, limit, bundler);
            m_pulog.Log("FinishPopulateBundler", null);

            if (m_status.m_success)
            {
                bundler.SetRetries(5);

                // Do not specify the buffer size, other wise all the threads will
                // share a buffer. That would be bad.
                m_transfer.StartDownload(0);

                CommandPriorityQueue queue = new CommandPriorityQueue();
                UBMFileOps ubmfo = new UBMFileOps(m_status, m_fileops, m_transfer);

                ManifestBundlerProgress progress = new ManifestBundlerProgress(queue, bundler, m_monitor, m_status.Project.Name);

                foreach (ManifestBundle bundle in bundler.Bundles)
                {
                    // Add initial validates at a low priority so we favour
                    // downloading data where we determine it is necessary to do so.
                    queue.AddCommand(new ValidateCommand(bundle, ubmfo, queue, CommandPriorityQueue.Low));
                }

                // Allow the project to specify how many threads to use.
                int threadCount = m_status.Project.MaxDownloadThreads;
                if (threadCount == 0)
                {
                    threadCount = 8;
                }
                if (threadCount <= 0)
                {
                    // Should be unreachable, but just in case ensure there is
                    // always at least one thread, otherwise nothing will ever
                    // happen.
                    threadCount = 1;
                }
                LogEntry process = new LogEntry("FastSynchroniseStartProcess");
                process.AddValue("Threads", threadCount);
                process.AddValue("Bundles", bundler.Bundles.Count);
                m_pulog.Log(process);
                m_transfer.Log(process);
                queue.Process(threadCount);

                String message = "";
                Dictionary<String,int> messages = new Dictionary<String,int>();
                int overflow = 0;
                foreach (ManifestBundle bundle in bundler.Bundles)
                {
                    if (!bundle.DownloadStatus.Success)
                    {
                        if (!messages.ContainsKey(bundle.DownloadStatus.Error))
                        {
                            messages[bundle.DownloadStatus.Error] = 1;
                        }
                        else
                        {
                            messages[bundle.DownloadStatus.Error]++;
                        }
                    }
                }
                foreach (String messageKey in messages.Keys)
                {
                    if (message.Length < 1024)
                    {
                        message = message + messageKey;
                        if (messages[messageKey] > 1)
                        {
                            message += String.Format(" ({0})", messages[messageKey]);
                        }
                        message += "\n\n";
                    }
                    else
                    {
                        ++overflow;
                    }
                }

                if (overflow > 0)
                {
                    message = message + "With " + overflow.ToString() + " other errors.";
                }
                if (!String.IsNullOrEmpty(message))
                {
                    m_status.SetError(message);
                }

                if (m_status.m_mode == UpdateStatus.UpdateMode.DownloadAndInstall)
                {
                    // Only do a discard for an actual update.
                    if (m_status.m_success)
                    {
                        DiscardNonManifestFiles(manifest, stats);
                    }

                    UpdateVersion();

                    stats.Write(System.IO.Path.Combine(m_status.Project.ProjectDirectory, "Update.log"));
                }
                m_changes = stats.ChangeCount;

                progress.CompleteAction();
                LogEntry syncComplete = new LogEntry("FastSynchroniseCompleted");
                syncComplete.AddValue("Added", stats.Added.ToString());
                syncComplete.AddValue("Updated", stats.Updated.ToString());
                syncComplete.AddValue("Skipped", stats.Skipped.ToString());
                syncComplete.AddValue("Removed", stats.Removed.ToString());
                syncComplete.AddValue("Copied", stats.Copied.ToString());
                syncComplete.AddValue("Downloaded", stats.Downloaded.ToString());
                m_transfer.Log(syncComplete);

                m_transfer.StopDownload();

                ubmfo.ReportStatistics();
            }
            m_pulog.Log("FinishFastSynchroniseManfiestContents", null);
        }

        /// <summary>
        /// Test whether the entry should be considered during an update
        /// process.
        /// 
        /// The default is to consider all entries, consistent with an update.
        /// 
        /// When doing a pre flight check we only process files of interest.
        /// Currently this is hardwired to the executable path, though we could
        /// mark entries in the manifest if we want to extend the set of files
        /// covered.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        private bool ProcessManifestEntry(ManifestFile.ManifestEntry entry)
        {
            if (m_status.m_mode == UpdateStatus.UpdateMode.PreFlightCheck)
            {
                String fullPath = Path.Combine(m_status.Project.ProjectDirectory, entry.Path);
                if (fullPath.ToLowerInvariant() == m_status.Project.ExecutablePath.ToLowerInvariant())
                {
                    // Evilness. Store the entry hash in the project so we
                    // can check it later. This is fairly pointless, if the
                    // client is compromised this can easily be changed
                    // invalidating any assurances we have later that the
                    // hash we are checking against is in fact correct.
                    m_status.Project.ExecutableHash = entry.Hash;
                    return true;
                }
                if (entry.Path.ToLowerInvariant() == "versioninfo.txt")
                {
                    return false;
                }
                return false;
            }
            return true;
        }

        private bool ValidateFile(String path, ManifestFile.ManifestEntry entry)
        {
            DecoderRing ring = new DecoderRing();
            long length;
            String hash = ring.SHA1EncodeFile(path, out length);
            if ((length == entry.Size) && (hash == entry.Hash))
            {
                return true;
            }
            return false;
        }

        private void SynchroniseManifestEntry(ManifestFile manifest, ManifestFile.ManifestEntry entry,
            ManifestStatistics stats, PartialDownloadProgress pdp)
        {
            DownloadManagerBase.RemoteFileDetails details = new DownloadManagerBase.RemoteFileDetails();
            details.CheckSum = entry.Hash;
            details.FileSize = entry.Size;
            details.RemotePath = entry.Download;

            // Evilness
            // We can be pretty certain that there will be a versioninfo.txt
            // file since the rest of the system relies on it. If we overwrite
            // it then even if the update fails to complete later it will still
            // look like we are up to date, and since we use the installed
            // version number to decide whether to do an update the files on
            // disk would be wrong, but there would be no update. Therefore
            // actually download the VersionInfo.txt file to a different file
            // so the old version is kept, and then rename only when we have
            // successfully completed.
            // Note that this causes problems with the preflight checks since
            // they will disable the rename required for this to work. The
            // simple solution is to reenable the rename even for preflight
            // checks but this will require that versioninfo.txt will always
            // need to be processed (and will always be marked as changed).
            // Alternatively handle the update in a less evil way or flag the
            // change somehow and then perform the rename if versioninfo.txt
            // was in the preflight check set.
            String localFile = System.IO.Path.Combine(m_status.Project.ProjectDirectory, entry.Path);
            if (entry.Path.ToLowerInvariant() == "versioninfo.txt")
            {
                localFile += ".new";
            }

            m_pulog.Log("StartSynchroniseManifestEntry", localFile);

            FileInfo info = new FileInfo(localFile);
            if (info.Exists)
            {
                if (info.Length == details.FileSize)
                {
                    if (ValidateFile(localFile, entry))
                    {
                        // Existing file is correct as far as we can tell.
                        m_pulog.Log("NoChangeRequired", localFile);
                        stats.SkippedFile(entry.Path);
                        m_pulog.Log("FinishSynchroniseManifestEntry", localFile);
                        return;
                    }
                }
                m_pulog.Log("UpdateRequired", localFile);
                stats.UpdatedFile(entry.Path);
            }
            else
            {
                m_pulog.Log("NewFileRequired", localFile);
                stats.AddFile(entry.Path);
            }

            details.LocalFileName = localFile;

            try
            {
                SynchroniseFile(manifest, details, entry, stats, pdp);
            }
            catch (System.IO.IOException ex)
            {
                m_status.SetError(String.Format(LocalResources.Properties.Resources.PU_ManifestFileSyncExc,
                    entry.Path, ex.Message));
            }
            m_pulog.Log("FinishSynchroniseManifestEntry", localFile);
        }

        private void SynchroniseFile(ManifestFile manifest,
            DownloadManagerBase.RemoteFileDetails details,
            ManifestFile.ManifestEntry entry,
            ManifestStatistics stats, PartialDownloadProgress pdp)
        {
            // No existing file, or existing file failed match criteria.
            m_fileops.EnsureDirectory(System.IO.Path.GetDirectoryName(details.LocalFileName));

            foreach (ManifestFile.ManifestEntry existing in manifest.Entries)
            {
                if (existing == entry)
                {
                    // Stop when we reach the entry we are looking for since
                    // even if there is a match it will not have been processed
                    // yet.
                    break;
                }

                if (existing.Hash == entry.Hash)
                {
                    // We have already synchronised a file with the same hash
                    // so rather than downloading it again, copy the file
                    // we have already downloded to the new location. This
                    // means the same file appears in multiple locations within
                    // the file hierarchy.
                    String existingFile = System.IO.Path.Combine(m_status.Project.ProjectDirectory,
                        existing.Path);
                    try
                    {
                        m_pulog.Log("CopyExistingFile", existingFile);
                        m_fileops.RemoveFile(details.LocalFileName);
                        System.IO.File.Copy(existingFile, details.LocalFileName);
                        if (ValidateFile(details.LocalFileName, entry))
                        {
                            stats.CopiedFile(entry.Path);
                        }
                        else
                        {
                            m_status.SetError(
                                  String.Format(LocalResources.Properties.Resources.PU_ManifestFileCopyValidFail,
                                                                 details.LocalFileName));
                            LogEntry validationFile = new LogEntry("ValidationOfCopiedFileFailed");
                            DecoderRing r = new DecoderRing();
                            long size;
                            String hashString = r.SHA1EncodeFile(details.LocalFileName, out size);
                            validationFile.AddValue("ActualHash", hashString);
                            validationFile.AddValue("Expected", details.CheckSum);
                            validationFile.AddValue("LocalFile", details.LocalFileName);
                            validationFile.AddValue("RemoteFile", details.RemotePath);
                            validationFile.AddValue("CopiedFile", existing.Path);
                            validationFile.AddValue("CopiedHash", existing.Hash);
                            m_transfer.Log(validationFile);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        m_status.SetError(String.Format(LocalResources.Properties.Resources.PU_ManifestFileCopyExc,
                            details.LocalFileName, ex.Message));
                    }
                    return;
                }
            }

            DownloadManagerBase.DownloadStatus dls = new DownloadManagerBase.DownloadStatus();
            m_transfer.DownloadFile(ref dls, details, true, pdp.UpdateProgress);
            if (!dls.Success)
            {
                m_status.SetError(dls.Error);
            }
            if (m_status.m_success)
            {
                if (ValidateFile(details.LocalFileName, entry))
                {
                    stats.DownloadedFile(entry.Path);
                }
                else
                {
                    m_status.SetError(String.Format(LocalResources.Properties.Resources.PU_ManifestFileUpdateValidFail,
                        details.LocalFileName));
                    LogEntry validationFile = new LogEntry("ValidationOfDownloadedFileFailed");
                    DecoderRing r = new DecoderRing();
                    long size;
                    String hashString = r.SHA1EncodeFile(details.LocalFileName, out size);
                    validationFile.AddValue("ActualHash", hashString);
                    validationFile.AddValue("Expected", details.CheckSum);
                    validationFile.AddValue("LocalFile", details.LocalFileName);
                    validationFile.AddValue("RemoteFile", details.RemotePath);
                    m_transfer.Log(validationFile);
                }
            }
        }

        private void DiscardNonManifestFiles(ManifestFile manifest, ManifestStatistics stats)
        {
            HashSet<String> wanted = new HashSet<String>();

            foreach (ManifestFile.ManifestEntry entry in manifest.Entries)
            {
                String fullpath = System.IO.Path.Combine(m_status.Project.ProjectDirectory, entry.Path);
                wanted.Add(fullpath.ToLowerInvariant());
            }
            // Make sure we do not delete the new versioninfo.txt so we can put
            // it in place later.
            wanted.Add(System.IO.Path.Combine(m_status.Project.ProjectDirectory, VersionFileNew).ToLowerInvariant());

            DiscardFilesFromDirectory(m_status.Project.ProjectDirectory, wanted, stats);
        }

        private void DiscardFilesFromDirectory(String path,
            HashSet<String> wanted,
            ManifestStatistics stats)
        {
            String[] items = System.IO.Directory.GetFiles(path);
            foreach (String item in items)
            {
                String il = item.ToLowerInvariant();
                if (!wanted.Contains(il))
                {
                    // Some fudge rules to keep files we know we want to keep
                    // around until they are put somewhere more sensible or
                    // we come up with a more structured way of selecting them.
                    if (il.Contains("_screenshot"))
                    {
						continue;
					}
					if (il.Contains("appconfiglocal.xml"))
					{
						continue;
					}
					if (il.Contains("crashdump.zip"))
					{
						continue;
					}
					bool removed = false;
					String ext = Path.GetExtension(il);
					if ((ext != ".log") && (ext != ".dmp"))
					{
						try
						{
							FileAttributes attr = System.IO.File.GetAttributes(item);
							if ((attr & FileAttributes.Hidden) != FileAttributes.Hidden)
							{
								m_pulog.Log("RemoveExpiredFile", item);
								m_fileops.RemoveFile(item);
								stats.RemovedFile(item);
								removed = true;
							}
						}
						catch (System.Exception)
						{
						}
					}
					if (!removed)
					{
						stats.KeptFile(item);
					}
                }
            }
            items = System.IO.Directory.GetDirectories(path);
            foreach (String item in items)
            {
                DiscardFilesFromDirectory(item, wanted, stats);
            }
            items = System.IO.Directory.GetFiles(path);
            if (items.Length == 0)
            {
                items = System.IO.Directory.GetDirectories(path);
                if (items.Length == 0)
                {
                    try
                    {
                        m_pulog.Log("RemoveEmptyDirectory", path);
                        System.IO.Directory.Delete(path);
                    }
                    catch (System.IO.IOException)
                    {
                    }
                }
            }
        }

        private String VersionFile
        {
            get
            {
                return "VersionInfo.txt";
            }
        }

        private String VersionFileNew
        {
            get
            {
                return "VersionInfo.txt.new";
            }
        }

        private void UpdateVersion()
        {
            if (m_status.m_success)
            {
                String versionFile = System.IO.Path.Combine(m_status.Project.ProjectDirectory, VersionFile);
                m_fileops.RemoveFile(versionFile);
                String versionFileNew = System.IO.Path.Combine(m_status.Project.ProjectDirectory, VersionFileNew);
                System.IO.File.Move(versionFileNew, versionFile);
            }
        }

        /*
         =  foreach file in manifest:
         =      if no existing bundle for hash/size:
         =          create bundle
         =          add to bundle queue
         =      add file to bundle
         = 
         *  foreach bundle in bundle queue:
         *      bundle.matching = null
         *      foreach file in bundle:
         *          mark file as not up to date
         *          if file exists:
         *              if hash/size match:
         *                  if bundle.matching == null:
         *                      bundle.matching = file
         *                  mark file as up to date
         *      if bundle.matching:
         *          foreach file in bundle:
         *              if file not up to date:
         *                  copy file from bundle.matching
         *                  validate file
         *      else:
         *          add bundle to download queue
         *          
         *  foreach bundle in download queue:
         *      bundle.matching = download first file in bundle
         *      mark bundle.matching as up to date
         *      foreach file in bundle:
         *          if file not up to date:
         *              copy file from bundle.matching
         *              validate file
         *              
         * Iterative approach not easily run across multiple threads so turn
         * inside out.
         * 
         *  foreach bundle in bundle queue:
         *      add verify(bundle,true) to command queue.
         *      
         *  processor thread:
         *      while command = command queue.remove():
         *          command.execute(command queue)
         *          
         *  verify(bundle,download).execute:
         *      bundle.matching = null
         *      foreach file in bundle:
         *          mark file as not up to date
         *          if file exists:
         *              if hash/size match:
         *                  if bundle.matching == null:
         *                      bundle.matching = file
         *                  mark file as up to date
         *      if bundle.matching:
         *          foreach file in bundle:
         *              if file not up to date:
         *                  copy file from bundle.matching
         *                  validate file
         *      else:
         *          if download:
         *              add download(bundle) to command queue.
         *          
         *  download(bundle).execute:
         *      download first file in bundle
         *      add verify(bundle,false) to command queue
         *
         * Extract low level operation interface so commands can be written to
         * the interface and tested properly:
         * 
         *  RemoveFile(path) -> already in file ops
         *  CopyFile(source, target) -> System.IO
         *  ValidateFile(path, manifestentry) -> Local
         *  DownloadFile(remotefiledetails) -> TransferManager
         *  
         * Also need to consider the removal of old and out of date files which
         * are no longer included in the manifest. May be possible to reuse the
         * existing code (at least initially) but is there any benefit on using
         * the command buffer to perform the tidy up in parallel too?
         * 
         * Needed tests.
         * Support retries? Limited number of attempts per bundle?
         * 
         * Measure/Maintain/Report progress.
         * 
         * Current system uses one progress unit per byte of data in the 
         * manifest contents. In this model 'verified' data is not included,
         * but the file is considered complete when successfully verified.
         * For files that are downloaded the progress through the file is
         * updated as the file contents are received. This does not allow for
         * the subsequent verification of the file. If the verification fails
         * then the update will be aborted immediately so the lack of progress
         * for the work is not particularly obvious.
         * 
         * In current system the progress during download helps indicate that
         * work is actually being done while downloading a single large file
         * since only one action is performed at a time. This may be less
         * significant with parallel downloads, since if one thread stalls
         * other threads may still indicate work being done. The current
         * system also calculates the rate/remaining time on the fly from the
         * progress made. If the download rate was calculated from the
         * actual data downloaded (independent of remaining data) that would
         * also provide a working indication and the overall progress could be
         * updated whenever a file was successfully verified.
         * 
         * Current system does not support setting the rate directly as it was
         * previously calculated from the progress made.
         * 
         * Use a different approach to progress indication. Publish events that
         * can be fired when progress state changes. Use INotifyPropertyChange?
         * 
         * ProgressEntry
         *  Action
         *  Limit
         *  Progress
         *  Rate
         *
         * Base class. Derive additional classes for organisation that are
         * capable of combining the results of multiple entries to provide a
         * single result. How to represent things which have not happened yet.
         * Make the progress entry part of the bundle since the bundle
         * determines the maximum number of validate/copy/download attempts if
         * retries are ignored (or we allow for a maximum number of retries).
         * If the maximum number of retries is used then the progress bar is
         * not an accurate representation of the actual work to do since only
         * 1/retries of progress is indicated on the first attempt and once
         * successful the remaining 4/retries of progress for the item is added
         * as a single progress step on completion. If retries are ignored then
         * progress could go down as well as up if after downloading a file
         * another attempt needs to be made. Base progress on number of files
         * (or bundles) completed rather than size. Multi threaded support.
         * 
         */
    }
}
