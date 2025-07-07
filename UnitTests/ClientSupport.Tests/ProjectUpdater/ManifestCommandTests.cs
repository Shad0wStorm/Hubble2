using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using ClientSupport;
using ClientSupport.ProjectUpdater;

namespace ClientSupport.Tests.ProjectUpdater
{
    [TestClass]
    public class ManifestCommandTests
    {
        private class CommandQueueEventHandler
        {
            public long Total;
            public long Progress;
            public bool TotalChanged = false;
            public bool Regressed = false;
            public bool OverProgress = false;

            public ManifestBundler Bundler = null;

            public void CommandCompleted(object sender)
            {
                if (Bundler != null)
                {
                    long total = Bundler.DataSize;

                    if (Total != 0)
                    {
                        if (Total != total)
                        {
                            TotalChanged = true;
                        }
                    }
                    Total = total;

                    long progress = Bundler.Progress;
                    if (Progress > progress)
                    {
                        Regressed = true;
                    }
                    Progress = progress;
                    if (Progress > Total)
                    {
                        OverProgress = true;
                    }
                }
            }

        }

        private ManifestFileOps_Mock m_fileOps = null;
        private CommandQueueEventHandler m_events;

        [TestInitialize]
        public void SetUp()
        {
            m_fileOps = new ManifestFileOps_Mock();
            m_events = null;
        }

        [TestMethod]
        public void NoCommandsNoFileOps()
        {
            Assert.AreEqual(0, m_fileOps.Count);
        }

        [TestMethod]
        public void ValidateExistingFileSucceeds()
        {
            ManifestEntryContext entry = new ManifestEntryContext();
            entry.Hash = "A";
            entry.Size = 1;
            entry.Path = "Test\\Test.txt";
            entry.Download = "A";

            CommandQueue queue = new CommandQueue();

            ManifestBundle bundle = new ManifestBundle(entry);
            entry.Context = bundle;

            queue.AddCommand(new ValidateCommand(bundle, m_fileOps, null));

            m_fileOps.ExistingFile(entry);

            queue.Process(1);

            Assert.AreEqual(1, m_fileOps.Count);
            Assert.AreEqual("A:V==Test\\Test.txt", m_fileOps.Action[0]);
        }

        [TestMethod]
        public void ValidateExistingBundleSucceeds()
        {
            ManifestEntryContext entry = new ManifestEntryContext();
            entry.Hash = "A";
            entry.Size = 1;
            entry.Path = "Test\\Test.txt";
            entry.Download = "A";

            CommandQueue queue = new CommandQueue();

            ManifestBundle bundle = new ManifestBundle(entry);
            entry.Context = bundle;

            ManifestEntryContext copy = new ManifestEntryContext();
            copy.Hash = entry.Hash;
            copy.Size = entry.Size;
            copy.Path = "Test\\Copy.txt";
            copy.Download = "A";
            bundle.AddEntry(copy);
            copy.Context = bundle;

            queue.AddCommand(new ValidateCommand(bundle, m_fileOps, null));

            m_fileOps.ExistingFile(entry);
            m_fileOps.ExistingFile(copy);

            queue.Process(1);

            Assert.AreEqual(2, m_fileOps.Count);
            Assert.AreEqual("A:V==Test\\Test.txt", m_fileOps.Action[0]);
            Assert.AreEqual("A:V==Test\\Copy.txt", m_fileOps.Action[1]);
        }

        [TestMethod]
        public void ValidateWithExistingFileNoTargetCopies()
        {
            ManifestEntryContext entry = new ManifestEntryContext();
            entry.Hash = "A";
            entry.Size = 1;
            entry.Path = "Test\\Test.txt";
            entry.Download = "A";

            CommandQueue queue = new CommandQueue();

            ManifestBundle bundle = new ManifestBundle(entry);
            entry.Context = bundle;

            ManifestEntryContext other = new ManifestEntryContext();
            other.Hash = "A";
            other.Size = 1;
            other.Path = "Test\\Other.txt";
            other.Download = "A";
            other.Context = bundle;
            bundle.AddEntry(other);

            queue.AddCommand(new ValidateCommand(bundle, m_fileOps, null));

            m_fileOps.ExistingFile(entry);

            queue.Process(1);

            Assert.AreEqual(4, m_fileOps.Count);
            Assert.AreEqual("A:V==Test\\Test.txt", m_fileOps.Action[0]);
            Assert.AreEqual("A:V=!Test\\Other.txt", m_fileOps.Action[1]);
            Assert.AreEqual("A:V+C+Test\\Test.txt>Test\\Other.txt", m_fileOps.Action[2]);
            Assert.AreEqual("A:V==Test\\Other.txt", m_fileOps.Action[3]);
        }

        [TestMethod]
        public void ValidateWithExistingFileDifferentTargetReplaces()
        {
            ManifestEntryContext entry = new ManifestEntryContext();
            entry.Hash = "A";
            entry.Size = 1;
            entry.Path = "Test\\Test.txt";
            entry.Download = "A";

            CommandQueue queue = new CommandQueue();

            ManifestBundle bundle = new ManifestBundle(entry);
            entry.Context = bundle;

            ManifestEntryContext other = new ManifestEntryContext();
            other.Hash = "A";
            other.Size = 1;
            other.Path = "Test\\Other.txt";
            other.Download = "A";
            bundle.AddEntry(other);
            other.Context = bundle;

            queue.AddCommand(new ValidateCommand(bundle, m_fileOps, null));

            m_fileOps.ExistingFile(entry);
            ManifestFile.ManifestEntry different = new ManifestFile.ManifestEntry();
            different.Hash = "B";
            different.Size = 10;
            different.Path = "Test\\Other.txt";
            different.Download = "B";
            m_fileOps.ExistingFile(different);

            queue.Process(1);

            Assert.AreEqual(5, m_fileOps.Count);
            Assert.AreEqual("A:V==Test\\Test.txt", m_fileOps.Action[0]);
            Assert.AreEqual("A:V=!Test\\Other.txt", m_fileOps.Action[1]);
            Assert.AreEqual("A:V--Test\\Other.txt", m_fileOps.Action[2]);
            Assert.AreEqual("A:V+C+Test\\Test.txt>Test\\Other.txt", m_fileOps.Action[3]);
            Assert.AreEqual("A:V==Test\\Other.txt", m_fileOps.Action[4]);
        }

        [TestMethod]
        public void ValidateWithExistingFileNoTargetCorruptedCopy()
        {
            ManifestEntryContext entry = new ManifestEntryContext();
            entry.Hash = "A";
            entry.Size = 1;
            entry.Path = "Test\\Test.txt";
            entry.Download = "A";

            CommandQueue queue = new CommandQueue();

            ManifestBundle bundle = new ManifestBundle(entry);
            entry.Context = bundle;

            ManifestEntryContext other = new ManifestEntryContext();
            other.Hash = "A";
            other.Size = 1;
            other.Path = "Test\\Other.txt";
            other.Download = "A";
            other.Context = bundle;
            bundle.AddEntry(other);

            queue.AddCommand(new ValidateCommand(bundle, m_fileOps, null));

            m_fileOps.ExistingFile(entry);
            ManifestFile.ManifestEntry different = new ManifestFile.ManifestEntry();
            different.Hash = "B";
            different.Size = 10;
            different.Path = "Test\\Other.txt";
            different.Download = "B";
            m_fileOps.CopySource(different);

            //System.Diagnostics.Debugger.Break();
            queue.Process(1);

            Assert.AreEqual(5, m_fileOps.Count);
            Assert.AreEqual("A:V==Test\\Test.txt", m_fileOps.Action[0]);
            Assert.AreEqual("A:V=!Test\\Other.txt", m_fileOps.Action[1]);
            Assert.AreEqual("A:V+C+Test\\Test.txt>Test\\Other.txt", m_fileOps.Action[2]);
            Assert.AreEqual("A:V=!Test\\Other.txt", m_fileOps.Action[3]);
            Assert.AreEqual("!!!Copy failed for \"Test\\Other.txt\" (validation error)", m_fileOps.Action[4]);
        }

        [TestMethod]
        public void ValidateWithExistingFileNoTargetCopyException()
        {
            ManifestEntryContext entry = new ManifestEntryContext();
            entry.Hash = "A";
            entry.Size = 1;
            entry.Path = "Test\\Test.txt";
            entry.Download = "A";

            CommandQueue queue = new CommandQueue();

            ManifestBundle bundle = new ManifestBundle(entry);
            entry.Context = bundle;

            ManifestEntryContext other = new ManifestEntryContext();
            other.Hash = "A";
            other.Size = 1;
            other.Path = "Test\\Other.txt";
            other.Download = "A";
            other.Context = bundle;
            bundle.AddEntry(other);

            queue.AddCommand(new ValidateCommand(bundle, m_fileOps, null));

            m_fileOps.ExistingFile(entry);
            ManifestFile.ManifestEntry different = new ManifestFile.ManifestEntry();
            different.Hash = "B";
            different.Size = (long)ManifestFileOps_Mock.SpecialActions.IOException;
            different.Path = "Test\\Other.txt";
            different.Download = "B";
            m_fileOps.CopySource(different);

            queue.Process(1);

            Assert.AreEqual(3, m_fileOps.Count);
            Assert.AreEqual("A:V==Test\\Test.txt", m_fileOps.Action[0]);
            Assert.AreEqual("A:V=!Test\\Other.txt", m_fileOps.Action[1]);
            Assert.AreEqual("!!!Exception copying file \"Test\\Other.txt\":\n\nCopyFailed", m_fileOps.Action[2]);
        }

        [TestMethod]
        public void DownloadUnavailableFileFails()
        {
            ManifestEntryContext entry = new ManifestEntryContext();
            entry.Hash = "A";
            entry.Size = 1;
            entry.Path = "Test\\Test.txt";
            entry.Download = "A";

            CommandQueue queue = new CommandQueue();

            ManifestBundle bundle = new ManifestBundle(entry);
            entry.Context = bundle;

            queue.AddCommand(new DownloadCommand(bundle, m_fileOps, null));

            queue.Process(1);

            Assert.AreEqual(1, m_fileOps.Count);
            Assert.AreEqual("A:D<!MissingFile:A", m_fileOps.Action[0]);
        }

        [TestMethod]
        public void DownloadAvailableFileSucceeds()
        {
            ManifestEntryContext entry = new ManifestEntryContext();
            entry.Hash = "A";
            entry.Size = 1;
            entry.Path = "Test\\Test.txt";
            entry.Download = "A";

            CommandQueue queue = new CommandQueue();

            ManifestBundle bundle = new ManifestBundle(entry);
            entry.Context = bundle;

            m_fileOps.AddRemoteFile(entry);

            queue.AddCommand(new DownloadCommand(bundle, m_fileOps, null));

            queue.Process(1);

            Assert.AreEqual(1, m_fileOps.Count);
            Assert.AreEqual("A:D<<A", m_fileOps.Action[0]);
        }

        [TestMethod]
        public void DownloadAvailableFileSucceedsAndValidates()
        {
            ManifestEntryContext entry = new ManifestEntryContext();
            entry.Hash = "A";
            entry.Size = 1;
            entry.Path = "Test\\Test.txt";
            entry.Download = "A";

            CommandQueue queue = new CommandQueue();

            ManifestBundle bundle = new ManifestBundle(entry);
            entry.Context = bundle;

            m_fileOps.AddRemoteFile(entry);

            queue.AddCommand(new DownloadCommand(bundle, m_fileOps, queue));

            queue.Process(1);

            Assert.AreEqual(2, m_fileOps.Count);
            Assert.AreEqual("A:D<<A", m_fileOps.Action[0]);
            Assert.AreEqual("A:DV==Test\\Test.txt", m_fileOps.Action[1]);
        }

        [TestMethod]
        public void DownloadAvailableFileSucceedsAndValidatesWithCopies()
        {
            ManifestEntryContext entry = new ManifestEntryContext();
            entry.Hash = "A";
            entry.Size = 1;
            entry.Path = "Test\\Test.txt";
            entry.Download = "A";

            CommandQueue queue = new CommandQueue();

            ManifestBundle bundle = new ManifestBundle(entry);
            entry.Context = bundle;

            ManifestEntryContext copy = new ManifestEntryContext();
            copy.Hash = "A";
            copy.Size = 1;
            copy.Path = "Test\\Other.txt";
            copy.Download = "A";
            copy.Context = bundle;
            bundle.AddEntry(copy);

            m_fileOps.AddRemoteFile(entry);

            queue.AddCommand(new DownloadCommand(bundle, m_fileOps, queue));

            queue.Process(1);

            Assert.AreEqual(5, m_fileOps.Count);
            Assert.AreEqual("A:D<<A", m_fileOps.Action[0]);
            Assert.AreEqual("A:DV==Test\\Test.txt", m_fileOps.Action[1]);
            Assert.AreEqual("A:DV=!Test\\Other.txt", m_fileOps.Action[2]);
            Assert.AreEqual("A:DV+C+Test\\Test.txt>Test\\Other.txt", m_fileOps.Action[3]);
            Assert.AreEqual("A:DV==Test\\Other.txt", m_fileOps.Action[4]);
        }

        private void ValidateDownloadCopy()
        {
            ManifestEntryContext entry = new ManifestEntryContext();
            entry.Hash = "A";
            entry.Size = 1;
            entry.Path = "Test\\Test.txt";
            entry.Download = "A";

            CommandQueue queue = new CommandQueue();

            ManifestBundle bundle = new ManifestBundle(entry);
            entry.Context = bundle;

            ManifestEntryContext copy = new ManifestEntryContext();
            copy.Hash = "A";
            copy.Size = 1;
            copy.Path = "Test\\Other.txt";
            copy.Download = "A";
            copy.Context = bundle;
            bundle.AddEntry(copy);

            m_fileOps.AddRemoteFile(entry);

            queue.AddCommand(new ValidateCommand(bundle, m_fileOps, queue));

            queue.Process(1);
        }

        [TestMethod]
        public void ValidateDownloadCopy_ExpectedNumberOfFileOperations()
        {
            ValidateDownloadCopy();
            Assert.AreEqual(7, m_fileOps.Count);
        }

        [TestMethod]
        public void ValidateDownloadCopy_FirstFileFailsValidation()
        {
            ValidateDownloadCopy();
            Assert.AreEqual("A:V=!Test\\Test.txt", m_fileOps.Action[0]);
        }

        [TestMethod]
        public void ValidateDownloadCopy_SecondFileFailsValidation()
        {
            ValidateDownloadCopy();
            Assert.AreEqual("A:V=!Test\\Other.txt", m_fileOps.Action[1]);
        }

        [TestMethod]
        public void ValidateDownloadCopy_DownloadFile()
        {
            ValidateDownloadCopy();
            Assert.AreEqual("A:VD<<A", m_fileOps.Action[2]);
        }

        [TestMethod]
        public void ValidateDownloadCopy_FirstFileValidatesAfterDownload()
        {
            ValidateDownloadCopy();
            Assert.AreEqual("A:VDV==Test\\Test.txt", m_fileOps.Action[3]);
        }

        [TestMethod]
        public void ValidateDownloadCopy_SecondFileFailsValidationAfterDownload()
        {
            ValidateDownloadCopy();
            Assert.AreEqual("A:VDV=!Test\\Other.txt", m_fileOps.Action[4]);
        }

        [TestMethod]
        public void ValidateDownloadCopy_CopyFirstFileToSecondFile()
        {
            ValidateDownloadCopy();
            Assert.AreEqual("A:VDV+C+Test\\Test.txt>Test\\Other.txt", m_fileOps.Action[5]);
        }

        [TestMethod]
        public void ValidateDownloadCopy_SecondFileValidatesAfterCopy()
        {
            ValidateDownloadCopy();
            Assert.AreEqual("A:VDV==Test\\Other.txt", m_fileOps.Action[6]);
        }

        private ManifestEntryContext CreateEntry(String hash, String path, object context)
        {
            ManifestEntryContext entry = new ManifestEntryContext();
            entry.Hash = hash;
            entry.Size = 1;
            entry.Path = path;
            entry.Download = hash;
            entry.Context = context;
            return entry;
        }

        private void MultipleBundleInstallation(int threads)
        {
            if (threads > 1)
            {
                // More than one thread so introduce some delays
                m_fileOps = new ManifestFileOps_Mock(0, 10);
            }
            CommandQueue queue = new CommandQueue();

            ManifestEntryContext bundle1Entry = CreateEntry("00", "Resource.txt", null);
            ManifestBundle bundle = new ManifestBundle(bundle1Entry);
            bundle1Entry.Context = bundle;
            bundle.AddEntry(CreateEntry("00", "Alpha.txt", bundle));
            bundle.AddEntry(CreateEntry("00", "Beta.txt", bundle));
            m_fileOps.AddRemoteFile(bundle1Entry);
            queue.AddCommand(new ValidateCommand(bundle, m_fileOps, queue));

            ManifestEntryContext bundle2Entry = CreateEntry("01", "011.txt", null);
            bundle = new ManifestBundle(bundle2Entry);
            bundle2Entry.Context = bundle;
            bundle.AddEntry(CreateEntry("01", "012.txt", bundle));
            bundle.AddEntry(CreateEntry("01", "013.txt", bundle));
            m_fileOps.AddRemoteFile(bundle2Entry);
            queue.AddCommand(new ValidateCommand(bundle, m_fileOps, queue));

            ManifestEntryContext bundle3Entry = CreateEntry("02", "021.txt", null);
            bundle = new ManifestBundle(bundle3Entry);
            bundle3Entry.Context = bundle;
            m_fileOps.AddRemoteFile(bundle3Entry);
            queue.AddCommand(new ValidateCommand(bundle, m_fileOps, queue));

            queue.Process(threads);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_ExpectedOperationCount()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual(25, m_fileOps.Count);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B1F1ValidationFailed()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("00:V=!Resource.txt", m_fileOps.Action[0]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B1F2ValidationFailed()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("00:V=!Alpha.txt", m_fileOps.Action[1]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B1F3ValidationFailed()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("00:V=!Beta.txt", m_fileOps.Action[2]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B2F1ValidationFailed()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("01:V=!011.txt", m_fileOps.Action[3]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B2F2ValidationFailed()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("01:V=!012.txt", m_fileOps.Action[4]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B2F3ValidationFailed()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("01:V=!013.txt", m_fileOps.Action[5]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B3F1ValidationFailed()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("02:V=!021.txt", m_fileOps.Action[6]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B1Download()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("00:VD<<00", m_fileOps.Action[7]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B2Download()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("01:VD<<01", m_fileOps.Action[8]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B3Download()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("02:VD<<02", m_fileOps.Action[9]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B1F1ValidatedAfterDownload()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("00:VDV==Resource.txt", m_fileOps.Action[10]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B1F2NotValidatedAfterDownload()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("00:VDV=!Alpha.txt", m_fileOps.Action[11]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B1F3NotValidatedAfterDownload()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("00:VDV=!Beta.txt", m_fileOps.Action[12]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B1F2Copied()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("00:VDV+C+Resource.txt>Alpha.txt", m_fileOps.Action[13]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B1F2ValidatedAfterCopy()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("00:VDV==Alpha.txt", m_fileOps.Action[14]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B1F3Copied()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("00:VDV+C+Resource.txt>Beta.txt", m_fileOps.Action[15]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B1F3ValidatedAfterCopy()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("00:VDV==Beta.txt", m_fileOps.Action[16]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B2F1ValidatedAfterDownload()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("01:VDV==011.txt", m_fileOps.Action[17]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B2F2NotValidatedAfterDownload()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("01:VDV=!012.txt", m_fileOps.Action[18]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B2F3NotValidatedAfterDownload()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("01:VDV=!013.txt", m_fileOps.Action[19]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B2F2CopiedAfterDownload()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("01:VDV+C+011.txt>012.txt", m_fileOps.Action[20]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B2F2ValidatedAfterCopy()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("01:VDV==012.txt", m_fileOps.Action[21]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B2F3CopiedAfterDownload()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("01:VDV+C+011.txt>013.txt", m_fileOps.Action[22]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B2F3ValidatedAfterCopy()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("01:VDV==013.txt", m_fileOps.Action[23]);
        }

        [TestMethod]
        public void MultipleBundleSingleThread_B3F1ValidatedAfterDownload()
        {
            MultipleBundleInstallation(1);

            Assert.AreEqual("02:VDV==021.txt", m_fileOps.Action[24]);
        }

        [TestMethod]
        public void MultipleBundleDualThread_ExpectedOperationCount()
        {
            MultipleBundleInstallation(2);

            Assert.AreEqual(25, m_fileOps.Count);
        }

        int ConfirmOrder(String[] expected)
        {
            int next = 0;
            foreach (String action in m_fileOps.Action)
            {
                if (action == expected[next])
                {
                    ++next;
                    if (next == expected.Length)
                    {
                        break;
                    }
                }
            }
            return next;
        }

        [TestMethod]
        public void MultipleBundleDualThread_B1F1Validation()
        {
            MultipleBundleInstallation(2);

            // For the multi-threaded tests we cannot relay on the events
            // occurring in a specific order, but for a given file the relative
            // order should be preserved, e.g. no copying a file before
            // downloading it.
            String[] expected = { "00:V=!Resource.txt", "00:VD<<00", "00:VDV==Resource.txt" };
            Assert.AreEqual(3, ConfirmOrder(expected));
        }

        [TestMethod]
        public void MultipleBundleDualThread_B1F2Validation()
        {
            MultipleBundleInstallation(2);

            String[] expected = { "00:V=!Alpha.txt",
                                  "00:VD<<00",
                                  "00:VDV=!Alpha.txt",
                                  "00:VDV+C+Resource.txt>Alpha.txt", 
                                  "00:VDV==Alpha.txt" };
            Assert.AreEqual(5, ConfirmOrder(expected));
        }

        [TestMethod]
        public void MultipleBundleDualThread_B1F3Validation()
        {
            MultipleBundleInstallation(2);

            String[] expected = { "00:V=!Beta.txt",
                                  "00:VD<<00",
                                  "00:VDV=!Beta.txt",
                                  "00:VDV+C+Resource.txt>Beta.txt", 
                                  "00:VDV==Beta.txt" };
            Assert.AreEqual(5, ConfirmOrder(expected));
        }

        [TestMethod]
        public void MultipleBundleDualThread_B2F1Validation()
        {
            MultipleBundleInstallation(2);

            String[] expected = { "01:V=!011.txt",
                                  "01:VD<<01",
                                  "01:VDV==011.txt" };
            Assert.AreEqual(3, ConfirmOrder(expected));
        }

        [TestMethod]
        public void MultipleBundleDualThread_B2F2Validation()
        {
            MultipleBundleInstallation(2);

            String[] expected = { "01:V=!012.txt",
                                  "01:VD<<01",
                                  "01:VDV=!012.txt",
                                  "01:VDV+C+011.txt>012.txt",
                                  "01:VDV==012.txt"};
            Assert.AreEqual(5, ConfirmOrder(expected));
        }

        [TestMethod]
        public void MultipleBundleDualThread_B2F3Validation()
        {
            MultipleBundleInstallation(2);

            String[] expected = { "01:V=!013.txt",
                                  "01:VD<<01",
                                  "01:VDV=!013.txt",
                                  "01:VDV+C+011.txt>013.txt",
                                  "01:VDV==013.txt"};
            Assert.AreEqual(5, ConfirmOrder(expected));
        }

        [TestMethod]
        public void MultipleBundleDualThread_B3F1Validation()
        {
            MultipleBundleInstallation(2);

            String[] expected = { "02:V=!021.txt",
                                  "02:VD<<02",
                                  "02:VDV==021.txt" };
            Assert.AreEqual(3, ConfirmOrder(expected));
        }

        private void RetryDownloads(int failures, int threads)
        {
            if (threads > 1)
            {
                // More than one thread so introduce some delays
                m_fileOps = new ManifestFileOps_Mock(0, 10);
            }
            CommandQueue queue = new CommandQueue();

            ManifestEntryContext bundle1Entry = CreateEntry("00", "Resource.txt", null);
            ManifestBundle bundle = new ManifestBundle(bundle1Entry);
            bundle.Retries = 2;
            bundle1Entry.Context = bundle;
            m_fileOps.AddRemoteFile(bundle1Entry);
            queue.AddCommand(new ValidateCommand(bundle, m_fileOps, queue));

            ManifestEntryContext bundle1Failed = CreateEntry("01", "Resource.txt", null);
            bundle1Failed.Download = "00";
            for (int i = 0; i < failures; ++i)
            {
                m_fileOps.AddRemoteFileInstance(bundle1Failed);
            }

            queue.Process(threads);
        }

        [TestMethod]
        public void RetryDownloadsNoFailures_CorrectCount()
        {
            RetryDownloads(0, 1);

            Assert.AreEqual(3, m_fileOps.Count);
        }

        [TestMethod]
        public void RetryDownloadsNoFailures_FirstValidateFails()
        {
            RetryDownloads(0, 1);

            Assert.AreEqual("00:V=!Resource.txt", m_fileOps.Action[0]);
        }

        [TestMethod]
        public void RetryDownloadsNoFailures_FirstDownloadSucceeds()
        {
            RetryDownloads(0, 1);

            Assert.AreEqual("00:VD<<00", m_fileOps.Action[1]);
        }

        [TestMethod]
        public void RetryDownloadsNoFailures_SecondValidateSucceeds()
        {
            RetryDownloads(0, 1);

            Assert.AreEqual("00:VDV==Resource.txt", m_fileOps.Action[2]);
        }

        [TestMethod]
        public void RetryDownloadsSingleFailure_CorrectCount()
        {
            RetryDownloads(1, 1);

            Assert.AreEqual(5, m_fileOps.Count);
        }

        [TestMethod]
        public void RetryDownloadsSingleFailure_FirstValidateFails()
        {
            RetryDownloads(1, 1);

            Assert.AreEqual("00:V=!Resource.txt", m_fileOps.Action[0]);
        }

        [TestMethod]
        public void RetryDownloadsSingleFailure_FirstDownloadSucceeds()
        {
            RetryDownloads(1, 1);

            Assert.AreEqual("00:VD<<00", m_fileOps.Action[1]);
        }

        [TestMethod]
        public void RetryDownloadsSingleFailure_SecondValidateFails()
        {
            RetryDownloads(1, 1);

            Assert.AreEqual("00:VDV=!Resource.txt", m_fileOps.Action[2]);
        }

        [TestMethod]
        public void RetryDownloadsSingleFailure_SecondDownloadSucceeds()
        {
            RetryDownloads(1, 1);

            Assert.AreEqual("00:VDVD<<00", m_fileOps.Action[3]);
        }

        [TestMethod]
        public void RetryDownloadsSingleFailure_ThirdValidateSucceeds()
        {
            RetryDownloads(1, 1);

            Assert.AreEqual("00:VDVDV==Resource.txt", m_fileOps.Action[4]);
        }
    }
}
