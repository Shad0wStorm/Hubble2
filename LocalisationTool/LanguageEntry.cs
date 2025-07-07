using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LocalisationTool
{
    class LanguageEntry
    {
        public String Language;
        public String Culture;
        public String GameLanguage;
        public bool Active = false;
        public int LocalisedEntries = 0;
        public List<String> OutOfDate = null;
        public List<String> Unwanted = null;
        public List<String> Missing = null;
    }
}
