using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace LocalisationTool
{
    class LanguageResource
    {
        public String Name;
        public String LanguageName;
        public String ResourceFile;
        private XmlDocument m_document;
        private String m_loadFailException = null;
        private int m_changes = 0;

        public void Load(String path)
        {
            ResourceFile = path;
            String filename = Path.GetFileNameWithoutExtension(path);
            int dot = filename.IndexOf('.');
            if (dot > 0)
            {
                Name = filename.Substring(dot + 1);
            }
            else
            {
                Name = LocalisationSheet.ENGLISH;
            }

            m_document = new XmlDocument();
            try
            {
                m_document.Load(ResourceFile);
            }
            catch (System.Exception ex)
            {
                m_loadFailException = "Exception loading resource file " + ResourceFile + " : " + ex.Message;            	
            }
        }

        public void UpdateFromEntries(IEnumerable<LocalisationEntry> entries, List<String> messages)
        {
            if (m_loadFailException != null)
            {
                messages.Add(m_loadFailException);
                m_loadFailException = null;
                return;
            }

            List<String> added = new List<String>();
            List<String> unchanged = new List<String>();
            List<String> updated = new List<String>();
            List<String> removed = new List<String>();

            foreach (LocalisationEntry entry in entries)
            {
                if (!entry.Values.ContainsKey(LanguageName))
                {
                    // No value specified for this language for this entry so
                    // move on to the next one.
                    continue;
                }
                String text = entry.Values[LanguageName];
                if (String.IsNullOrEmpty(text))
                {
                    // There is a value but it is empty/null so move on to the
                    // next one.
                    continue;
                }
                String flags = entry.Flags;
                if (flags.Contains("UNUSED"))
                {
                    // Item is marked as unused so do not include it in the
                    // resources.
                    continue;
                }
                String name = entry.Name;
                bool found = false;
                foreach (XmlElement element in m_document.DocumentElement.GetElementsByTagName("data"))
                {
                    if (element.GetAttribute("name") == name)
                    {
                        // Update existing entry
                        XmlNodeList nodes = element.GetElementsByTagName("value");
                        if (nodes.Count != 1)
                        {
                            messages.Add("Multiple values in file for " + name);
                        }
                        else
                        {
                            if (nodes[0].InnerText != text)
                            {
                                // Value has changed so update it.
                                nodes[0].InnerText = text;
                                updated.Add(entry.Name);
                                found = true;
                                ++m_changes;
                            }
                            else
                            {
                                // Value has not changed so simply mark that it
                                // has been seen.
                                unchanged.Add(entry.Name);
                                found = true;
                            }
                        }
                    }
                }
                if (!found)
                {
                    // No existing value with this name so add a new one.
                    XmlElement add = m_document.CreateElement("data");
                    add.SetAttribute("name", name);
                    add.SetAttribute("xml:space", "preserve");
                    XmlElement value = m_document.CreateElement("value");
                    value.InnerText = text;
                    add.AppendChild(value);
                    m_document.DocumentElement.AppendChild(add);
                    added.Add(name);
                    ++m_changes;
                }
            }

            List<XmlElement> tbr = new List<XmlElement>();
            foreach (XmlElement element in m_document.DocumentElement.GetElementsByTagName("data"))
            {
                String name = element.GetAttribute("name");
                if (!(added.Contains(name) || unchanged.Contains(name) || updated.Contains(name)))
                {
                    tbr.Add(element);
                }
            }
            foreach (XmlElement element in tbr)
            {
                String name = element.GetAttribute("name");
                m_document.DocumentElement.RemoveChild(element);
                removed.Add(name);
                ++m_changes;
            }

            String report = "";
            report = ReportList(report, "Added", added);
            report = ReportList(report, "Updated", updated);
            report = ReportList(report, "Removed", removed);
            if (!String.IsNullOrEmpty(report))
            {
                messages.Add(Name + " : " + report);
            }
        }

        private String ReportList(String initial, String name, List<String> items)
        {
            if (items.Count < 1)
            {
                return initial;
            }
            String result = initial;
            if (result.Length > 0)
            {
                result += ", ";
            }
            result += name + " ";
            result += items.Count.ToString();
            result += " items";
            return result;
        }

        public void Save(List<String> messages)
        {
            if (m_changes > 0)
            {
                try
                {
                    FileInfo rfinfo = new FileInfo(ResourceFile);
                    if (rfinfo.IsReadOnly)
                    {
                        // Make sure the file is writable in case SVN has
                        // write protected it.
                        rfinfo.IsReadOnly = false;
                    }
                    m_document.Save(ResourceFile);
                }
                catch (System.Exception ex)
                {
                    messages.Add(Name + " : Exception saving file " + ResourceFile + " : " + ex.Message);
                }
            }
            else
            {
                messages.Add(Name + " : No changes to save");
            }
        }
    }
}
