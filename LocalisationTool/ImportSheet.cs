using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Office.Interop.Excel;

namespace LocalisationTool
{
    /// <summary>
    /// Class for handling import spreadsheets.
    /// 
    /// An import spreadsheet is similar to a LocalisationSheet and
    /// corresponds to the information written by the LocalisationSheet
    /// ExportChanges method. This sheet uses different language codes (to
    /// correspond with the codes used externally) only includes languages for
    /// which localisation is in place.
    /// </summary>
    class ImportSheet
    {
        LocalisationSheet m_source = null;
        private Dictionary<String, LocalisationEntry> m_contents = null;

        public LocalisationEntry[] Entries
        {
            get
            {
                return m_contents.Values.ToArray();
            }
        }

        public void Load(LocalisationSheet source, String path)
        {
            m_source = source;
            SheetHelper.Load(path, true, LoadSheet);
        }

        public void LoadSheet(Worksheet sheet)
        {
            String[] languageMap = BuildLanguageMap(sheet).ToArray();

            m_contents = new Dictionary<String, LocalisationEntry>();

            m_source.ReadLocalisationDataToDictionary(sheet, m_contents, languageMap);
        }

        /// <summary>
        /// Map the imported sheet headings to the headings used in the source
        /// sheet.
        /// 
        /// When the sheet was exported the headings were mapped from the
        /// internal names to the GameLanguage versions to match the in game
        /// language codes which do not match the standard.
        /// 
        /// Generate a list of headings one for each column in the imported
        /// sheet but using the name of the corresponding column in the source
        /// sheet.
        /// </summary>
        /// <param name="sheet"></param>
        /// <returns></returns>
        private List<String> BuildLanguageMap(Worksheet sheet)
        {
            List<String> lmap = new List<String>();
            int column = 1;
            int previous = 0;
            while (column > previous)
            {
                previous = column;
                Range r = (Range)(sheet.Cells[1, column]);
                if (r != null)
                {
                    if (r.Value != null)
                    {
                        String header = r.Value.ToString();
                        if (!String.IsNullOrEmpty(header))
                        {
                            foreach (LanguageEntry entry in m_source.LanguageDetails.Values)
                            {
                                if (entry.GameLanguage == header)
                                {
                                    header = entry.Language;
                                    break;
                                }
                            }
                            lmap.Add(header);
                            column = lmap.Count + 1;
                        }
                    }
                }
            }
            return lmap;
        }
    }
}
