using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LocalisationTool
{
    class LocalisationEntry
    {
        public Dictionary<String, String> Values = null;
        public bool NeedsUpdate = false;

        public const String NAME_KEY = "ID";
        public const String FLAG_KEY = "Flags";
        public const String HASH_KEY = "Hash";

        public LocalisationEntry()
        {
            Values = new Dictionary<String, String>();
        }

        public String Name
        {
            get
            {
                if (Values.ContainsKey(NAME_KEY))
                {
                    return Values[NAME_KEY];
                }
                return "<UNKNOWN>";
            }
        }

        public String Flags
        {
            get
            {
                if (Values.ContainsKey(FLAG_KEY))
                {
                    return Values[FLAG_KEY];
                }
                return "";
            }
        }

        public String Hash
        {
            get
            {
                if (Values.ContainsKey(HASH_KEY))
                {
                    return Values[HASH_KEY];
                }
                return "";
            }
        }
    }
}
