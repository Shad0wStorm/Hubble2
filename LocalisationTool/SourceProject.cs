using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace LocalisationTool
{
    class SourceProject
    {
        public String SpreadSheet;
        public String ResourceFile;
        public String Languages;
        public String[] Messages;

        private LocalisationSheet m_source;

        public void Refresh()
        {
            Languages = "";
            if (!String.IsNullOrEmpty(SpreadSheet))
            {
                if (File.Exists(SpreadSheet))
                {
                    m_source = new LocalisationSheet();
                    m_source.Load(SpreadSheet);
                    ResourceSheet resources = new ResourceSheet();
                    resources.Load(ResourceFile);
                    foreach (String language in m_source.Languages)
                    {
                        if (resources.Supports(language))
                        {
                            Languages += "+";
                        }
                        else
                        {
                            Languages += "-";
                        }
                        Languages = Languages + m_source.LanguageUsage(language);
                        Languages += "\n";
                    }
                }
                else
                {
                    Languages = "File Missing";
                }
            }
        }

        public void ExportChanges(String target)
        {
            if (!String.IsNullOrEmpty(SpreadSheet))
            {
                if (File.Exists(SpreadSheet))
                {
                    m_source = new LocalisationSheet();
                    m_source.Load(SpreadSheet);
                    m_source.ExportChanges(target);
                }
            }
        }

        public void ImportChanges(String import)
        {
            if (!String.IsNullOrEmpty(SpreadSheet))
            {
                if (File.Exists(SpreadSheet))
                {
                    if (File.Exists(import))
                    {
                        m_source = new LocalisationSheet();
                        m_source.Load(SpreadSheet);
                        ImportSheet importSheet = new ImportSheet();
                        importSheet.Load(m_source, import);
                        Messages = m_source.ImportChanges(importSheet);
                    }
                }
            }
        }

        public void DiffChanges(String diffFile)
        {
            if (!String.IsNullOrEmpty(SpreadSheet))
            {
                if (File.Exists(SpreadSheet))
                {
                    if (File.Exists(diffFile))
                    {
                        m_source = new LocalisationSheet();
                        m_source.Load(SpreadSheet);
                        LocalisationSheet diffSheet = new LocalisationSheet();
                        diffSheet.Load(diffFile);
                        Messages = m_source.DiffChanges(diffSheet);
                    }
                }
            }
        }

        public void UpdateResX()
        {
            if (!String.IsNullOrEmpty(SpreadSheet))
            {
                if (File.Exists(SpreadSheet))
                {
                    if (!String.IsNullOrEmpty(ResourceFile))
                    {
                        if (File.Exists(ResourceFile))
                        {
                            m_source = new LocalisationSheet();
                            m_source.Load(SpreadSheet);

                            ResourceSheet resources = new ResourceSheet();
                            resources.Load(ResourceFile);

                            Messages = m_source.UpdateSheet(resources);
                        }
                    }
                }
            }
        }
    }
}
