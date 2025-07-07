using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace ClientSupport
{
    public class ManifestFile
    {
        public class ManifestEntry : IComparable
        {
            public String Hash;
            public Int64 Size;
            public String Path;
            public String Download;
            public Dictionary<String, String> AccessCookies;

            public int CompareTo(Object obj)
            {
                ManifestEntry entry = obj as ManifestEntry;
                if (entry!=null)
                {
                    if (entry.Size != Size)
                    {
                        return (Size > entry.Size) ? 1 : -1;
                    }
                    if (entry.Hash != Hash)
                    {
                        return Hash.CompareTo(entry.Hash);
                    }
                    if (entry.Path != Path)
                    {
                        return Path.CompareTo(entry.Path);
                    }
                }
                return 0;
            }
        }

        protected Dictionary<String, ManifestEntry> m_files = new Dictionary<String, ManifestEntry>();
        public IEnumerable<ManifestEntry> Entries
        {
            get
            {
                return m_files.Values;
            }
        }

        public int EntryCount
        {
            get { return m_files.Values.Count; }
        }

        protected String m_fileName;
        public String FileName
        {
            get
            {
                return m_fileName;
            }
        }

        protected String m_productVersion;
        public String ProductVersion
        {
            get { return m_productVersion; }
        }

        protected String m_productTitle;
        public String ProductTitle
        {
            get { return m_productTitle; }
        }


        public ManifestFile()
        {

        }

        public bool LoadFile(String path)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            return LoadDocument(doc);
        }

        public bool LoadString(String content)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(content);
            return LoadDocument(doc);
        }

        public bool LoadStream(Stream stream)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(stream);
            return LoadDocument(doc);
        }

        private bool LoadDocument(XmlDocument doc)
        {
            XmlElement root = doc.DocumentElement;
            if (root.Name!="Manifest")
            {
                return false;
            }
            foreach (XmlAttribute attribute in root.Attributes)
            {
                if (attribute.Name == "title")
                {
                    m_productTitle = attribute.Value;
                }
                else
                {
                    if (attribute.Name == "version")
                    {
                        m_productVersion = attribute.Value;
                    }
                }
            }
            foreach (XmlElement child in root.ChildNodes)
            {
                if (child.Name == "File")
                {
                    ManifestEntry entry = new ManifestEntry();
                    foreach (XmlElement data in child.ChildNodes)
                    {
                        if (data.Name == "Path")
                        {
                            entry.Path = data.InnerText.Trim();
                        }
                        else
                        {
                            if (data.Name == "Hash")
                            {
                                entry.Hash = data.InnerText.Trim();
                            }
                            else
                            {
                                if (data.Name == "Size")
                                {
                                    long size;
                                    if (long.TryParse(data.InnerText.Trim(), out size))
                                    {
                                        entry.Size = size;
                                    }
                                }
                                else
                                {
                                    if (data.Name == "Download")
                                    {
                                        entry.Download = data.InnerText.Trim();
                                    }
                                }
                            }
                        }
                    }
                    if (entry.Download == null)
                    {
                        // If no explicit download link is given assume the
                        // downloader can handle download via hash.
                        entry.Download = entry.Hash;
                    }
                    m_files[entry.Path] = entry;
                }
            }
            return true;
        }
    }
}
