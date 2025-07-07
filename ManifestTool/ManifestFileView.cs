using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

using ClientSupport;

namespace ManifestTool
{
	public class ManifestFileView : ClientSupport.ManifestFile, INotifyPropertyChanged
	{
		public ManifestFileView(String path)
		{
			m_fileName = Path.GetFileName(path);
			ManifestName = Path.GetFileNameWithoutExtension(path);
			FileInfo info = new FileInfo(path);
			Written = info.LastWriteTimeUtc;
			FileCount = 0;
			UniqueCount = 0;
			TotalSize = 0;
			m_hasManifest = LoadFile(path);
			String manifestTitle = ProductTitle;
			if (String.IsNullOrEmpty(manifestTitle))
			{
				manifestTitle = m_fileName;
			}
			if (ProductVersion != null)
			{
				manifestTitle += " (version " + ProductVersion + ")";
			}
			ManifestTitle = manifestTitle;
			FileCount = m_files.Count;
			long totalSize = 0;
			long unique = 0;
			long uniqueSize = 0;
			HashSet<String> hashes = new HashSet<String>();

			foreach (ManifestEntry entry in m_files.Values)
			{
				totalSize += entry.Size;
				if (!hashes.Contains(entry.Hash))
				{
					++unique;
					uniqueSize += entry.Size;
					hashes.Add(entry.Hash);
				}
			}
			UniqueCount = unique;
			UniqueSize = uniqueSize;
			TotalSize = totalSize;
		}

		private bool m_hasManifest = false;

		public bool HasManifest
		{
			get
			{
				return m_hasManifest;
			}
		}

		public DateTime Written;

		private String m_manifestName;
		public String ManifestName
		{
			get
			{
				return m_manifestName;
			}
			set
			{
				if (m_manifestName != value)
				{
					m_manifestName = value;
					RaisePropertyChanged("ManifestName");
				}
			}
		}

		private String m_manifestTitle;
		public String ManifestTitle
		{
			get { return m_manifestTitle; }
			set
			{
				if (m_manifestTitle != value)
				{
					m_manifestTitle = value;
					RaisePropertyChanged("ManifestTitle");
				}
			}
		}

		private long m_fileCount;
		public long FileCount
		{
			get
			{
				return m_fileCount;
			}
			set
			{
				if (m_fileCount != value)
				{
					m_fileCount = value;
					RaisePropertyChanged("FileCount");
				}
			}
		}

		private long m_uniqueCount;
		public long UniqueCount
		{
			get { return m_uniqueCount; }
			set
			{
				if (m_uniqueCount != value)
				{
					m_uniqueCount = value;
					RaisePropertyChanged("UniqueCount");
				}
			}
		}

		private long m_uniqueSize;
		public long UniqueSize
		{
			get
			{
				return m_uniqueSize;
			}
			set
			{
				if (m_uniqueSize!=value)
				{
					m_uniqueSize = value;
					UniqueSizePretty = FileStore.PrettyByteCount(value);
					RaisePropertyChanged("UniqueSize");
				}
			}
		}

		private String m_uniqueSizePretty;
		public String UniqueSizePretty
		{
			get { return m_uniqueSizePretty; }
			set
			{
				if (m_uniqueSizePretty!=value)
				{
					m_uniqueSizePretty = value;
					RaisePropertyChanged("UniqueSizePretty");
				}
			}
		}

		private long m_totalSize;
		public long TotalSize
		{
			get { return m_totalSize; }
			set
			{
				if (m_totalSize != value)
				{
					m_totalSize = value;
					TotalSizePretty = FileStore.PrettyByteCount(value);
					RaisePropertyChanged("TotalSize");
				}
			}
		}

		private String m_totalSizePretty;
		public String TotalSizePretty
		{
			get { return m_totalSizePretty; }
			set
			{
				if (m_totalSizePretty != value)
				{
					m_totalSizePretty = value;
					RaisePropertyChanged("TotalSizePretty");
				}
			}
		}

		public void DetermineLocalContent(Dictionary<String, int> hashes)
		{
			if (hashes == null)
			{
				LocalFileCount = "unknown";
				LocalSizePretty = "unknown";
			}
			else
			{
				long count = 0;
				long size = 0;
				Dictionary<String, int> localHashes = new Dictionary<String, int>();
				foreach (String entryName in m_files.Keys)
				{
					String hash = m_files[entryName].Hash;
					if (localHashes.ContainsKey(hash))
					{
						localHashes[hash] += 1;
					}
					else
					{
						localHashes[hash] = 1;
					}
				}
				foreach (String entryName in m_files.Keys)
				{
					String hash = m_files[entryName].Hash;
					if (hashes.ContainsKey(hash))
					{
						if (localHashes.ContainsKey(hash))
						{
							if (hashes[hash] == localHashes[hash])
							{
								// All references to this hash were from this
								// manifest so include it in the unique totals.
								count++;
								size += m_files[entryName].Size;

								// Remove the local hash since the file has
								// already been counted
								localHashes.Remove(hash);
							}
						}
					}
				}
				LocalFileCount = count.ToString();
				LocalSizePretty = FileStore.PrettyByteCount(size);
			}
		}

		private String m_localFileCount;
		public String LocalFileCount
		{
			get { return m_localFileCount; }
			set
			{
				if (m_localFileCount != value)
				{
					m_localFileCount = value;
					RaisePropertyChanged("LocalFileCount");
				}
			}
		}

		private String m_localSizePretty;
		public String LocalSizePretty
		{
			get { return m_localSizePretty; }
			set
			{
				if (m_localSizePretty != value)
				{
					m_localSizePretty = value;
					RaisePropertyChanged("LocalSizePretty");
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void RaisePropertyChanged(String property)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(property));
			}
		}
	}
}
