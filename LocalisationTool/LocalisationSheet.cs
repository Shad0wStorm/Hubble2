using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Office.Interop.Excel;

using ClientSupport;

namespace LocalisationTool
{
    /// <summary>
    /// Represents the contents of a localisation spreadsheet.
    /// 
    /// The supported languages and individual strings for localisation are
    /// extracted from the source spreadsheet and stored internally.
    /// 
    /// They can then be used to export a subset of entries which need to be
    /// translated.
    /// 
    /// The localisation sheet is the master definition of which localisation
    /// strings are available and for which languages. It includes additional
    /// information such as flags indicating special properties (e.g. that a
    /// value is never localised) and a hash to detect text changes.
    /// </summary>
    class LocalisationSheet
    {
        private String SpreadSheet = null;
        private List<String> m_header = null;
        private List<String> m_languages = null;
        private Dictionary<String, LanguageEntry> m_languageColumns = null;
        private Dictionary<String, LocalisationEntry> m_contents = null;

        public Dictionary<String, LanguageEntry> LanguageDetails
        {
            get
            {
                return m_languageColumns;
            }
        }

        public const String ENGLISH = "en";
        const String NOTES = "Notes";

        /// <summary>
        /// Load an excel spreadsheet.
        /// 
        /// Load the sheet contents into the internal structures requiring that
        /// the sheet corresponds to the expected format.
        /// </summary>
        /// <param name="path">Path to the spreadsheet to load.</param>
        /// <param name="readOnly">
        /// Whether to open the sheet in read only mode. The sheet is never
        /// modified by this method so there is little need to open it in non
        /// read only mode.
        /// </param>
        public void Load(String path, bool readOnly = true)
        {
            SpreadSheet = path;
            SheetHelper.Load(path, readOnly, LoadSheet);
        }

        public void LoadSheet(Worksheet sheet)
        {
            ReadColumnHeader(sheet);
            ExtractLanguageData();

            ReadLocalisationData(sheet);
            UpdateLanguageRequirements();
        }

        private void ReadColumnHeader(Worksheet sheet)
        {
            m_header = new List<String>();

            int column = 1;
            int previous = 0;
            while (column > previous)
            {
                previous = column;
                Range r = (Range)(sheet.Cells[1, column]);
                if (r != null)
                {
                    if (r.Value!=null)
                    {
                        String header = r.Value.ToString();
                        if (!String.IsNullOrEmpty(header))
                        {
                            Console.WriteLine(header);
                            m_header.Add(header);
                            ++column;
                        }
                    }
                }
            }
        }

        private void ReadLocalisationData(Worksheet sheet)
        {
            m_contents = new Dictionary<String, LocalisationEntry>();

            ReadLocalisationDataToDictionary(sheet, m_contents, m_header.ToArray());
        }

        public void ReadLocalisationDataToDictionary(
            Worksheet sheet,
            Dictionary<String, LocalisationEntry> target,
            String[] labels
            )
        {
            int row = 2;

            bool added = true;
            while (added)
            {
                added = false;
                LocalisationEntry entry = new LocalisationEntry();
                if (SheetHelper.ReadRowToDictionary(sheet, row, entry.Values,labels) > 0)
                {
                    if (entry.Values.ContainsKey(LocalisationEntry.NAME_KEY))
                    {
                        target[entry.Values[LocalisationEntry.NAME_KEY]] = entry;
                        added = true;
                    }
                }
                ++row;
            }
        }

        private void ExtractLanguageData()
        {
            bool found = false;
            foreach (String header in m_header)
            {
                if (found==false)
                {
                    if (header.ToLowerInvariant()=="english")
                    {
                        m_languages = new List<String>();
                        m_languageColumns = new Dictionary<string,LanguageEntry>();
                        m_languages.Add(ENGLISH);
                        LanguageEntry le = new LanguageEntry();
                        le.Culture = ENGLISH;
                        le.Language = header;
                        le.Active = true;
                        m_languageColumns[ENGLISH] = le;
                        found = true;
                    }
                }
                else
                {
                    try
                    {
                        String[] lang = header.Split('\\');
                        m_languages.Add(lang[1]);
                        LanguageEntry le = new LanguageEntry();
                        le.Culture = lang[1];
                        le.Language = header;
                        le.Active = true;
                        m_languageColumns[lang[1]] = le;
                    }
                    catch (System.Exception)
                    {
                    	
                    }
                }
            }
        }

        public String[] Languages
        {
            get
            {
                if (m_languages!=null)
                {
                    return m_languages.ToArray();
                }
                return new String[0];
            }
        }

        private void UpdateLanguageRequirements()
        {
            if (m_languages == null)
            {
                return;
            }
            if (m_languageColumns == null)
            {
                return;
            }

            LocalisationEntry gameLanguage = null;
            if (m_contents.ContainsKey("GameLanguage"))
            {
                gameLanguage = m_contents["GameLanguage"];
            }

            foreach (String language in m_languages)
            {
                LanguageEntry le = m_languageColumns[language];
                if (gameLanguage != null)
                {
                    if (gameLanguage.Values.ContainsKey(le.Language))
                    {
                        le.GameLanguage = gameLanguage.Values[le.Language];
                        if (le.GameLanguage == "#")
                        {
                            le.GameLanguage = le.Language;
                        }
                    }
                    else
                    {
                        le.GameLanguage = le.Language;
                    }
                }
                UpdateLanguageDetails(le);
            }
        }

        private void UpdateLanguageDetails(LanguageEntry details)
        {
            details.LocalisedEntries = 0;
            details.Missing = new List<String>();
            details.OutOfDate = new List<String>();
            details.Unwanted = new List<String>();

            foreach (LocalisationEntry entry in m_contents.Values)
            {
                String flags = entry.Flags;
                flags = flags.ToLowerInvariant();
                if (flags.Contains("nolocexternal"))
                {
                    // Not externally localised so ignore for the
                    // purposes of language identification, the value
                    // may be a placeholder for when the language is
                    // localised.
                    continue;
                }
                bool found = false;
                if (entry.Values.ContainsKey(details.Language))
                {
                    String loc = entry.Values[details.Language];
                    if (!String.IsNullOrEmpty(loc))
                    {
                        details.LocalisedEntries++;
                        found = true;
                        if (details.Culture == ENGLISH)
                        {
                            if (!flags.Contains("noloc"))
                            {
                                // Localisation is required so check that the
                                // contents have not changed since the hash
                                // was generated on the last import.
                                DecoderRing r = new DecoderRing();
                                String calc = r.SHA1Encode(loc);
                                if (calc != entry.Hash)
                                {
                                    details.OutOfDate.Add(entry.Name);
                                    entry.NeedsUpdate = true;
                                }
                            }
                        }
                    }
                }

                if (flags.Contains("noloc"))
                {
                    if (found)
                    {
                        if (details.Culture == ENGLISH)
                        {
                            // English represents all possible strings in the
                            // application, but non-localised strings do not
                            // count so we want to 'uncount' them.
                            --details.LocalisedEntries;
                        }
                        else
                        {
                            // Marked as non-localised, but a value was
                            // included which may mean the value will be
                            // ignored or causes unwanted side effects.
                            details.Unwanted.Add(entry.Name);
                        }
                    }
                }
                else
                {
                    if (!found)
                    {
                        if (!flags.Contains("unused"))
                        {
                            // Note we do not mark the entry as needing an update
                            // since the entry will be missing for a language which
                            // is not yet localised so we do not want to mark the
                            // entry for update unless the language is active.
                            details.Missing.Add(entry.Name);
                        }
                    }
                }
            }
            if (details.Culture != ENGLISH)
            {
                if (details.LocalisedEntries == 0)
                {
                    // No localised entries so we are not interested in the
                    // language at this time.
                    details.Active = false;
                }
            }
        }

        public String LanguageUsage(String language)
        {
            if (m_languages == null)
            {
                return null;
            }
            if (!m_languages.Contains(language))
            {
                return null;
            }
            if (m_languageColumns == null)
            {
                return null;
            }
            LanguageEntry english = m_languageColumns[ENGLISH];
            LanguageEntry column = m_languageColumns[language];
            String result = language;
            if (column.LocalisedEntries > 0)
            {
                result += String.Format(" : {0}%", (100 * column.LocalisedEntries) / english.LocalisedEntries);
                result += PartialList(column.OutOfDate, "Out Of Date", english.LocalisedEntries);
                result += PartialList(column.Missing, "Missing", english.LocalisedEntries);
                result += PartialList(column.Unwanted, "NOLOC", english.LocalisedEntries);
            }
            else
            {
                result += " : Not available";
            }
            return result;
        }

        private String PartialList(List<String> items, String name, int total)
        {
            String result = "";
            if (items.Count > 0)
            {
                int pc = (100 * items.Count) / total;
                result = String.Format(", {0} ({1}%) : ",name,pc) + "" + items[0];
                if (items.Count > 1)
                {
                    result = result + ", " + items[1];
                    if (items.Count > 2)
                    {
                        result = result + String.Format(" and {0} more", items.Count - 2);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// The exported version of the spreadsheet is sent externally and must
        /// therefore conform to the expected format.
        /// 
        /// ID is used unchanged
        /// Flags are dropped as they are only relevant to this tool.
        /// Stat is calculated from the present text. Originally this used an
        /// incorrect formula (it failed to treat new lines as word breaks and
        /// undercalculated the number of words).
        /// Notes are included.
        /// Hash is not included (we can determine the difference between the
        /// two complete texts).
        /// Language columns are only included if Active. We use the
        /// "GameLanguage" entry , not the culture entry so the spreadsheets
        /// match the game spreadsheets because you can't have enough
        /// standards.
        /// </summary>
        /// <param name="target"></param>
        public void ExportChanges(String target)
        {
            SheetHelper.WriteSheet(target, WriteSheet);
        }

        public void WriteSheet(Worksheet sheet)
        {
            // Identify the languages we actually want to export.
            List<LanguageEntry> exportList = BuildExportLanguageList();

            WriteColumnHeaders(sheet, exportList);
            WriteUpdateRows(sheet, exportList);
        }

        private List<LanguageEntry> BuildExportLanguageList()
        {
            List<LanguageEntry> exportList = new List<LanguageEntry>();
            exportList.Add(m_languageColumns[ENGLISH]);
            foreach (LanguageEntry le in m_languageColumns.Values)
            {
                if (!exportList.Contains(le))
                {
                    if (le.Active)
                    {
                        exportList.Add(le);

                        // Ensure any missing entries are marked for update.
                        // We did not do this earlier since we did not know
                        // if the language was active. Now we know it is then
                        // any entries that are missing for this language but
                        // are otherwise up to date need to be included.
                        foreach (String missing in le.Missing)
                        {
                            LocalisationEntry entry = m_contents[missing];
                            entry.NeedsUpdate = true;
                        }
                    }
                }
            }
            return exportList;
        }

        private void WriteColumnHeaders(Worksheet sheet, List<LanguageEntry> languages)
        {
            int column = 1;

            Range r = (Range)(sheet.Cells[1, column++]);
            r.Value = "ID";
            r.EntireColumn.ColumnWidth = 40;
            r = (Range)(sheet.Cells[1, column++]);
            r.Value = "Stat";
            r = (Range)(sheet.Cells[1, column++]);
            r.Value = "Notes";
            r.EntireColumn.ColumnWidth = 40;

            foreach (LanguageEntry language in languages)
            {
                r = (Range)(sheet.Cells[1, column++]);
                String languageName = language.GameLanguage;
                if (languageName=="#")
                {
                    languageName = language.Language;
                }
                r.Value = languageName;
                r.EntireColumn.ColumnWidth = 40;
            }
        }

        private void WriteUpdateRows(Worksheet sheet, List<LanguageEntry> languages)
        {
            int row = 2;
            String[] keys = m_contents.Keys.ToArray();
            Array.Sort(keys);
            foreach (String key in keys)
            {
                LocalisationEntry entry = m_contents[key];
                if (!entry.NeedsUpdate)
                {
                    continue;
                }
                int column = 1;

                Range r = (Range)(sheet.Cells[row, column++]);
                r.Value = entry.Name;
                r.VerticalAlignment = XlVAlign.xlVAlignTop;

                String english = entry.Values[languages[0].Language];

                r = (Range)(sheet.Cells[row, column++]);
                r.Value = WordCount(english).ToString();
                r.VerticalAlignment = XlVAlign.xlVAlignTop;

                if (entry.Values.ContainsKey(NOTES))
                {
                    r = (Range)(sheet.Cells[row, column]);
                    r.Value = entry.Values[NOTES];
                    r.WrapText = true;
                    r.VerticalAlignment = XlVAlign.xlVAlignTop;
                }
                ++column;

                foreach (LanguageEntry language in languages)
                {
                    String languageName = language.Language;
                    String languageText = "";
                    if (entry.Values.ContainsKey(languageName))
                    {
                        languageText = entry.Values[languageName];
                        if (languageText.Length > 0)
                        {
                            if (languageText[0] == '\'')
                            {
                                languageText = "'" + languageText;
                            }
                        }
                    }
                    r = (Range)(sheet.Cells[row, column++]);
                    if (!String.IsNullOrEmpty(languageText))
                    {
                        r.Value = languageText;
                    }
                    r.WrapText = true;
                    r.VerticalAlignment = XlVAlign.xlVAlignTop;
                }
                ++row;
            }
        }

        private int WordCount(String text)
        {
            int result = 0;
            bool inword = false;
            for (int t = 0; t < text.Length; ++t)
            {
                if (Char.IsLetterOrDigit(text, t))
                {
                    if (!inword)
                    {
                        inword = true;
                        ++result;
                    }
                }
                else
                {
                    inword = false;
                }
            }
            return result;
        }

        private class ImportHelper
        {
            public String[] Messages = null;
            public ImportSheet Sheet;
        }

        public String[] ImportChanges(ImportSheet sheet)
        {
            ImportHelper helper = new ImportHelper();
            helper.Sheet = sheet;
            SheetHelper.UpdateSheet(SpreadSheet, UpdateSheet, helper);
            return helper.Messages;
        }

        public void UpdateSheet(Worksheet sheet, object oImport)
        {
            ImportHelper helper = oImport as ImportHelper;
            ImportSheet import = helper.Sheet;
            List<String> warnings = new List<String>();
            if (import == null)
            {
                return;
            }

            LocalisationEntry[] importing = import.Entries;

            String EnglishName = m_languageColumns[ENGLISH].Language;
            int IDColumn = HeaderColumnNumber(LocalisationEntry.NAME_KEY);
            int EnglishColumn = HeaderColumnNumber(EnglishName);
            int HashColumn = HeaderColumnNumber(LocalisationEntry.HASH_KEY);

            Dictionary<String, int> languageIndex = new Dictionary<String, int>();
            for (int i = EnglishColumn; i < m_header.Count; ++i)
            {
                languageIndex[m_header[i]] = i + 1;
            }

            foreach (LocalisationEntry ie in importing)
            {
                int row = 2;
                bool exists = true;
                while (exists)
                {
                    Range r = (Range)(sheet.Cells[row, IDColumn]);
                    if (r.Value == null)
                    {
                        break;
                    }
                    String name = r.Value.ToString();
                    if (String.IsNullOrEmpty(name))
                    {
                        break;
                    }
                    if (name == ie.Name)
                    {
                        // Found the entry in the sheet corresponding to the
                        // imported entry.
                        Range e = (Range)(sheet.Cells[row, EnglishColumn]);
                        if (e.Value == null)
                        {
                            break;
                        }
                        String et = e.Value.ToString();
                        if (et != ie.Values[EnglishName])
                        {
                            // English text changed since the file was exported
                            // so do not replace it with the old text (and the
                            // possible translations thereof). Ideally flag
                            // this up in some way rather than silently
                            // ignoring the problem and hoping someone notices.
                            warnings.Add("English text for " + name + " changed since export.");
                            break;
                        }
                        Range h = (Range)(sheet.Cells[row, HashColumn]);
                        DecoderRing ring = new DecoderRing();
                        String calc = ring.SHA1Encode(et);
                        h.Value = calc;

                        foreach (String language in languageIndex.Keys)
                        {
                            if (ie.Values.ContainsKey(language))
                            {
                                Range lv = (Range)(sheet.Cells[row, languageIndex[language]]);
                                String languageText = ie.Values[language];
                                if (languageText[0] == '\'')
                                {
                                    languageText = "'" + languageText;
                                }
                                lv.Value = languageText;
                            }
                        }
                        break;
                    }
                    ++row;
                }
            }

            if (warnings.Count > 0)
            {
                helper.Messages = warnings.ToArray();
            }
        }

        public String[] DiffChanges(LocalisationSheet sheet)
        {
            List<String> messages =  new List<String>();

            String english = m_languageColumns[ENGLISH].Language;
            foreach (LocalisationEntry entry in m_contents.Values)
            {
                String name = entry.Name;
                String expected = entry.Values[english];
                if (!String.IsNullOrEmpty(expected))
                {
                    bool found = false;
                    foreach (LocalisationEntry test in sheet.m_contents.Values)
                    {
                        if (test.Name == name)
                        {
                            String actual = test.Values[english];
                            if (actual != expected)
                            {
                                messages.Add(entry.Name + " : '" + expected + "' != '" + actual + "'");
                            }
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        messages.Add(entry.Name + " : '" + expected + "' MISSING");
                    }
                }
            }

            foreach (LocalisationEntry test in sheet.m_contents.Values)
            {
                bool found = false;
                String actual = test.Values[english];

                foreach (LocalisationEntry entry in m_contents.Values)
                {
                    if (test.Name == entry.Name)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    messages.Add(test.Name + " : '" + actual + "' : UNEXPECTED");
                }
            }

            return messages.ToArray();
        }

        private int HeaderColumnNumber(String name)
        {
            for (int c = 0; c < m_header.Count; ++c)
            {
                if (m_header[c] == name)
                {
                    return c + 1;
                }
            }
            return -1;
        }

        public String[] UpdateSheet(ResourceSheet target)
        {
            IEnumerable<LocalisationEntry> localisations = m_contents.Values;

            foreach (LanguageEntry entry in m_languageColumns.Values)
            {
                target.SetLanguageName(entry.Culture, entry.Language);
            }

            return target.UpdateFromEntries(localisations);
        }
    }
}
