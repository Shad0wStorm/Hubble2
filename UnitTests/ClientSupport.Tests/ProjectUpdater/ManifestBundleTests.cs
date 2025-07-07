using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using ClientSupport;

namespace ClientSupport.Tests.ProjectUpdater
{
    [TestClass]
    public class ManifestBundleTests
    {
        private ClientSupport.ProjectUpdater.ManifestBundler DefaultBundle()
        {
            return new ClientSupport.ProjectUpdater.ManifestBundler();
        }

        private ClientSupport.ProjectUpdater.ManifestBundler SingleEntryBundle()
        {
            ClientSupport.ProjectUpdater.ManifestBundler bundler = new ClientSupport.ProjectUpdater.ManifestBundler();
            ManifestFile.ManifestEntry entry = new ManifestFile.ManifestEntry();
            entry.Hash = "A";
            entry.Size = 1;
            entry.Path = "YellowBrickRoad";
            bundler.AddEntry(entry);
            return bundler;
        }

        private ClientSupport.ProjectUpdater.ManifestBundler CompatibleEntryBundle(ClientSupport.ProjectUpdater.ManifestBundler bundler)
        {
            ManifestFile.ManifestEntry entry = new ManifestFile.ManifestEntry();
            entry.Hash = "A";
            entry.Size = 1;
            entry.Path = "RedBrickRoad";
            bundler.AddEntry(entry);
            return bundler;
        }

        private ClientSupport.ProjectUpdater.ManifestBundler IncompatibleEntryBundle(ClientSupport.ProjectUpdater.ManifestBundler bundler)
        {
            ManifestFile.ManifestEntry entry = new ManifestFile.ManifestEntry();
            entry.Hash = "B";
            entry.Size = 1;
            entry.Path = "BlueBrickRoad";
            bundler.AddEntry(entry);
            return bundler;
        }

        [TestMethod]
        public void DefaultBundleEmpty()
        {
            ClientSupport.ProjectUpdater.ManifestBundler bundle = DefaultBundle();
            Assert.AreEqual(0, bundle.Count);
        }

        [TestMethod]
        public void DefaultBundleEmptyFileCount()
        {
            ClientSupport.ProjectUpdater.ManifestBundler bundle = DefaultBundle();
            Assert.AreEqual(0, bundle.FileCount);
        }

        [TestMethod]
        public void SingleEntryGeneratesSingleBundle()
        {
            ClientSupport.ProjectUpdater.ManifestBundler bundler = SingleEntryBundle();
            Assert.AreEqual(1, bundler.Count);
        }

        [TestMethod]
        public void SingleEntryGeneratesSingleBundleFileCount()
        {
            ClientSupport.ProjectUpdater.ManifestBundler bundler = SingleEntryBundle();
            Assert.AreEqual(1, bundler.FileCount);
        }

        [TestMethod]
        public void CompatibleEntryGeneratesSingleBundle()
        {
            ClientSupport.ProjectUpdater.ManifestBundler bundler = CompatibleEntryBundle(SingleEntryBundle());
            Assert.AreEqual(1, bundler.Count);
        }

        [TestMethod]
        public void CompatibleEntryGeneratesSingleBundleFileCount()
        {
            ClientSupport.ProjectUpdater.ManifestBundler bundler = CompatibleEntryBundle(SingleEntryBundle());
            Assert.AreEqual(2, bundler.FileCount);
        }

        [TestMethod]
        public void IncompatibleEntryGeneratesSecondBundle()
        {
            ClientSupport.ProjectUpdater.ManifestBundler bundler = IncompatibleEntryBundle(SingleEntryBundle());
            Assert.AreEqual(2, bundler.Count);
        }

        [TestMethod]
        public void IncompatibleEntryGeneratesSecondBundleFileCount()
        {
            ClientSupport.ProjectUpdater.ManifestBundler bundler = IncompatibleEntryBundle(SingleEntryBundle());
            Assert.AreEqual(2, bundler.FileCount);
        }

        [TestMethod]
        public void CompatibleEntryMergesBundleAfterIncompatible()
        {
            ClientSupport.ProjectUpdater.ManifestBundler bundler = SingleEntryBundle();
            bundler = IncompatibleEntryBundle(bundler);
            bundler = CompatibleEntryBundle(bundler);
            Assert.AreEqual(2, bundler.Count);
        }

        [TestMethod]
        public void CompatibleEntryMergesBundleAfterIncompatibleFileCount()
        {
            ClientSupport.ProjectUpdater.ManifestBundler bundler = SingleEntryBundle();
            bundler = IncompatibleEntryBundle(bundler);
            bundler = CompatibleEntryBundle(bundler);
            Assert.AreEqual(3, bundler.FileCount);
        }
    }
}
