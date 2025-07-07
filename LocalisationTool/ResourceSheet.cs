using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace LocalisationTool
{
    /// <summary>
    /// The ResourceSheet stores a set of localisation information as used by
    /// C# applications.
    /// 
    /// Since each language actually gets placed in a different resource file
    /// this is backed by multiple underlying files. Since the name of the
    /// language files are dependent on the name of the default language file
    /// it is only necessary to specify the name of the default file.
    /// </summary>
    class ResourceSheet
    {
        private Dictionary<String,LanguageResource> m_contents;
        private String m_primary;
        /// <summary>
        /// Load the given resource file.
        /// 
        /// Determine which 
        /// </summary>
        /// <param name="path"></param>
        public void Load(String path)
        {
            if (File.Exists(path))
            {
                m_contents = new Dictionary<String,LanguageResource>();
                LanguageResource en = new LanguageResource();
                en.Load(path);
                m_contents[en.Name] = en;
                m_primary = path;

                String filename = Path.GetFileName(path);
                String dirname = Path.GetDirectoryName(path);
                String[] parts = filename.Split('.');
                if (parts.Length==2)
                {
                    String[] files = Directory.GetFiles(dirname);
                    foreach (String file in files)
                    {
                        String testfilename = Path.GetFileName(file);
                        String[] testparts = testfilename.Split('.');
                        if (testparts.Length == 3)
                        {
                            if ((testparts[0] == parts[0]) &&
                                (testparts[2] == parts[1]))
                            {
                                LanguageResource add = new LanguageResource();
                                add.Load(file);
                                m_contents[testparts[1]] = add;
                            }
                        }
                    }
                }
            }
        }

        public bool Supports(String language)
        {
            if (m_contents == null)
            {
                return false;
            }
            return m_contents.ContainsKey(language);
        }

        public String[] UpdateFromEntries(IEnumerable<LocalisationEntry> entries)
        {
            List<String> result = new List<String>();

            foreach (LanguageResource resource in m_contents.Values)
            {
                resource.UpdateFromEntries(entries, result);
            }

            foreach (LanguageResource resource in m_contents.Values)
            {
                resource.Save(result);
            }

            UpdateDesignerFile(result);

            return result.ToArray();
        }

        private String DetermineClassName(String path)
        {
            if (!path.Contains("Properties"))
            {
                return "Resources.Properties";
            }

            String remainder = Path.GetDirectoryName(path);
            String node = Path.GetFileName(path);
            String result = node;
            while (node != "Properties")
            {
                node = Path.GetFileName(remainder);
                result = node + "." + result;
                remainder = Path.GetDirectoryName(remainder);
            }
            node = Path.GetFileName(remainder);
            result = node + "." + result;

            return result;
        }

        public void UpdateDesignerFile(List<String> messages)
        {
            String file = Path.GetFileNameWithoutExtension(m_primary);
            String path = Path.GetDirectoryName(m_primary);
            String className = DetermineClassName(path);
            String designer = Path.Combine(path, file + ".Designer.cs");
            if (File.Exists(designer))
            {
                FileInfo resxInfo = new FileInfo(m_primary);
                FileInfo desInfo = new FileInfo(designer);
                if (resxInfo.LastWriteTimeUtc > desInfo.LastWriteTimeUtc)
                {
                    String resgen = Properties.Resources.ResGen;
                    if (!File.Exists(resgen))
                    {
                        messages.Add("Failed to find ResGen");
                        return;
                    }
                    String arguments = "";
                    arguments += "/useSourcePath";
                    arguments += " /publicClass ";
                    arguments += m_primary;
                    arguments += " /str:cs,"+className+",Resources," + designer;

                    ProcessStartInfo start = new ProcessStartInfo();
                    start.FileName = resgen;
                    start.Arguments = arguments;
                    try
                    {
                        Process process = Process.Start(start);
                        process.WaitForExit();
                    }
                    catch (System.Exception ex)
                    {
                        messages.Add("Exception running ResGen : " + ex.Message);                    	
                    }
                }
            }
            else
            {
                messages.Add("No designer file to update.");
            }
            /*
        result = subprocess.check_output(command)
        Log( result )
        (file,ext) = os.path.splitext(source)
        resources = file+".resources"
        if os.path.exists(resources):
            # we do not want the binary resources since the cs file will be
            # compiled as part of the application build anyway.
            Log("Removing unwanted binary resources")
            os.remove(resources)
             */
        }

        public void SetLanguageName(String culture, String name)
        {
            foreach (LanguageResource resource in m_contents.Values)
            {
                if (resource.Name == culture)
                {
                    resource.LanguageName = name;
                }
            }
        }
    }
}
