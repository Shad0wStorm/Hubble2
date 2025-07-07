using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

using ClientSupport;

namespace ManifestTool
{
	public class FileStore : INotifyPropertyChanged
	{
		public delegate void ProgressUpdate(String action, int current, int total);

		public const String FileStoreName = "FileStore.cfg";

		public class ImportException : Exception
		{
			public ImportException() { }
			public ImportException(String message) : base(message) { }
			public ImportException(String message, Exception inner) : base(message, inner) { }
		}

		ObservableCollection<ManifestFileView> m_manifestFiles = new ObservableCollection<ManifestFileView>();
		public ObservableCollection<ManifestFileView> ManifestList
		{
			get
			{
				return m_manifestFiles;
			}
			set
			{
				if (value != m_manifestFiles)
				{
					m_manifestFiles = value;
					RaisePropertyChanged("ManifestList");
				}
			}
		}

		private String m_root;
		private String m_manifestRoot;
		private String m_fileRoot;
		private FilterSet m_filters;

		private String m_fileSize;
		public String FileSize
		{
			get { return m_fileSize; }
			set
			{
				if (m_fileSize != value)
				{
					m_fileSize = value;
					RaisePropertyChanged("FileSize");
				}
			}
		}

		private long m_fileCount;
		public long FileCount
		{
			get { return m_fileCount; }
			set
			{
				if (m_fileCount != value)
				{
					m_fileCount = value;
					RaisePropertyChanged("FileCount");
				}
			}
		}

		private String m_totalSize;
		public String TotalFileSize
		{
			get { return m_totalSize; }
			set
			{
				if (m_totalSize != value)
				{
					m_totalSize = value;
					RaisePropertyChanged("TotalSize");
				}
			}
		}

		private long m_totalCount;
		public long TotalFileCount
		{
			get { return m_totalCount; }
			set
			{
				if (m_totalCount != value)
				{
					m_totalCount = value;
					RaisePropertyChanged("TotalCount");
				}
			}
		}

		public String ConfigurationErrors;

		public FileStore(String root)
		{
			m_root = root;
			m_manifestFiles.Clear();

			m_manifestRoot = Path.Combine(root, "manifest");
			m_fileRoot = Path.Combine(root, "files");

			FileSize = PrettyByteCount(0);
			FileCount = 0;

			LoadConfigFile();

			ScanManifestFiles();
			ScanFileStore();

		}

		private void LoadConfigFile()
		{
			m_filters = new FilterSet();

			String configPath = Path.Combine(m_root, FileStoreName);
			String[] lines = File.ReadAllLines(configPath);
			foreach (String line in lines)
			{
				m_filters.AddFilter(line);
			}
			ConfigurationErrors = m_filters.Errors;
		}

		private void ScanManifestFiles()
		{
			long fc = 0;
			long fs = 0;
			if (Directory.Exists(m_manifestRoot))
			{
				String[] files = Directory.GetFiles(m_manifestRoot);
				foreach (String file in files)
				{
					String ext = Path.GetExtension(file);
					if (ext == ".xml")
					{
						ManifestFileView mfv = new ManifestFileView(file);
						if (mfv.HasManifest)
						{
							m_manifestFiles.Add(mfv);
							fc = fc + mfv.FileCount;
							fs = fs + mfv.TotalSize;
						}
					}
				}
			}
			TotalFileCount = fc;
			TotalFileSize = PrettyByteCount(fs);

			Dictionary<String, int> usageCounter = new Dictionary<String, int>();
			foreach (ManifestFileView mfv in m_manifestFiles)
			{
				foreach (ManifestFile.ManifestEntry mfe in mfv.Entries)
				{
					String hash = mfe.Hash;
					if (usageCounter.ContainsKey(mfe.Hash))
					{
						usageCounter[mfe.Hash]++;
					}
					else
					{
						usageCounter[mfe.Hash] = 1;
					}
				}
			}
			foreach (ManifestFileView mfv in m_manifestFiles)
			{
				mfv.DetermineLocalContent(usageCounter);
			}
		}

		private void ScanFileStore()
		{
			if (Directory.Exists(m_fileRoot))
			{
				String[] directories = Directory.GetDirectories(m_fileRoot);
				long fc = 0;
				long fs = 0;
				IncludeDirectoryFiles(m_fileRoot, ref fc, ref fs);
				foreach (String directory in directories)
				{
					IncludeDirectoryFiles(directory, ref fc, ref fs);
				}
				FileCount = fc;
				FileSize = PrettyByteCount(fs);
			}
		}

		private void IncludeDirectoryFiles(String path, ref long count, ref long size)
		{
			String[] files = Directory.GetFiles(path);
			count += files.Length;
			foreach (String file in files)
			{
				FileInfo fi = new FileInfo(file);
				size += fi.Length;
			}
		}

		public bool HasManifestForVersion(String version)
		{
			String manifestName = ManifestFileNameForVersion(version);
			foreach (ManifestFileView mfv in ManifestList)
			{
				if (mfv.FileName == manifestName)
				{
					return true;
				}
			}
			return false;
		}

		public String ManifestFileNameForVersion(String version)
		{
			String filename = "manifest";
			String[] items = version.Split('.');
			foreach (String item in items)
			{
				filename = filename + "_" + item;
			}
			filename = filename + ".xml";
			return filename;
		}

		public String EnsureManifestDirectory()
		{
			if (!Directory.Exists(m_manifestRoot))
			{
				Directory.CreateDirectory(m_manifestRoot);
			}
			return m_manifestRoot;
		}

		String FileForHash(String hash, out String hashDir)
		{
			String dir = hash.Substring(0, 2);
			String file = hash.Substring(2);
			hashDir = Path.Combine(m_fileRoot, dir);
			String hashFile = Path.Combine(hashDir, file);
			return hashFile;
		}

		String FileForHash(String hash)
		{
			String hd;
			return FileForHash(hash, out hd);
		}

		public bool Import(String path, String hash)
		{
			bool result = false;
			if (!Directory.Exists(m_fileRoot))
			{
				Directory.CreateDirectory(m_fileRoot);
			}
			String hashDir;
			String hashFile = FileForHash(hash, out hashDir);
			if (!Directory.Exists(hashDir))
			{
				Directory.CreateDirectory(hashDir);
			}
			if (File.Exists(hashFile))
			{
				CheckFile(path, hashFile, hash);
			}
			else
			{
				File.Copy(path, hashFile);
				result = true;
			}
			return result;
		}

		public bool Export(String path, String hash)
		{
			String hashFile = FileForHash(hash);
			if (File.Exists(hashFile))
			{
				File.Copy(hashFile, path);
				return true;
			}
			return false;
		}

		public bool Contains(String hash)
		{
			String hashFile = FileForHash(hash);
			return File.Exists(hashFile);
		}

		public void CheckFile(String file, String against, String hash)
		{
			using (FileStream importing = new FileStream(file, FileMode.Open, FileAccess.Read))
			{
				using (FileStream existing = new FileStream(against, FileMode.Open, FileAccess.Read))
				{
					if (importing.Length != existing.Length)
					{
						String message = "Hash Collision : Size of existing file "+against+" for hash " + hash + " differs from";
						message += " size of file " + file + " with the same hash.";
						throw new ImportException(message);
					}
					const int c_bufferSize = 4096 * 256;
					byte[] importBuffer = new byte[c_bufferSize];
					byte[] existBuffer = new byte[c_bufferSize];
					long remaining = importing.Length;
					long total = 0;
					importing.Position = 0;
					existing.Position = 0;
					while (remaining > 0)
					{
						long length = c_bufferSize;
						if (length > remaining)
						{
							remaining = length;
						}
						importing.Read(importBuffer, 0, (int)length);
						existing.Read(existBuffer, 0, (int)length);
						for (int b = 0; b < length; ++b)
						{
							if (importBuffer[b] != existBuffer[b])
							{
								String message = "Hash Collision : File "+file + " differs from ";
								message += "existing entry for hash "+hash+" at offset "+(total+b).ToString();
								throw new ImportException(message);
							}
						}
						total = total + length;
						remaining = remaining - length;
					}
				}
			}
		}

		public String Tidy(ProgressUpdate update)
		{
			String result;
			String action = "Scan manifest ";
			HashSet<String> requiredHashes = new HashSet<String>();

			int current = 0;
			int total = 0;
			foreach (ManifestFileView mfv in m_manifestFiles)
			{
				action = "Scan manifest " + mfv.FileName;
				total = (int)mfv.FileCount;

				current = 0;
				foreach (ManifestFile.ManifestEntry entry in mfv.Entries)
				{
					if (update != null)
					{
						update(action, current, total);
					}
					String hash = FileForHash(entry.Hash);
					requiredHashes.Add(hash);
					++current;
				}
			}

			result = "Manifests remaining : " + m_manifestFiles.Count.ToString() + "\r\n";

			int removed = 0;
			int kept = 0;
			int failed = 0;

			if (Directory.Exists(m_fileRoot))
			{
				String[] directories = Directory.GetDirectories(m_fileRoot);
				total = directories.Length;
				current = 0;
				foreach (String dir in directories)
				{
					action = "Scan file store";
					if (update != null)
					{
						update(action, current, total);
					}
					String[] files = Directory.GetFiles(dir);
					foreach (String file in files)
					{
						if (!requiredHashes.Contains(file))
						{
							// found a file to delete
							try
							{
								System.IO.File.Delete(file);
								++removed;
							}
							catch (System.IO.IOException ex)
							{
								++failed;
							}
						}
						else
						{
							++kept;
						}
					}
					++current;
				}

				if (removed>0)
				{
					 result += "Removed " + removed.ToString() + " files.\r\n";
				}
				if (failed > 0)
				{
					result += "Failed to remove " + failed.ToString() + " files.\r\n";
				}
				if (kept > 0)
				{
					result += "Kept " + kept.ToString() + " files.\r\n";
				}
			}

			return result;
		}

		public bool Allow(String filepath)
		{
			return m_filters.Allow(filepath);
		}

		public static String PrettyByteCount(long bytes)
		{
			double size = bytes;
			String suffix = "B:KB:MB:GB:TB";
			int i;
			while (size > 1024.0)
			{
				i = suffix.IndexOf(':');
				if (i > 0)
				{
					suffix = suffix.Substring(i + 1);
				}
				else
				{
					break;
				}
				size = size / 1024;
			}
			i = suffix.IndexOf(':');
			if (i > 0)
			{
				suffix = suffix.Substring(0, i);
			}
			return size.ToString("F2") + suffix;
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
