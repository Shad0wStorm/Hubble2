using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ManifestTool
{
    class FilterSet
    {
        private class Filter
        {
            public bool Allow;
            public Regex Pattern;
        }

        private List<Filter> m_filters;
        
        public FilterSet()
        {
            m_filters = new List<Filter>();
        }

        public String Errors = "";

        public bool AddFilter(String line)
        {
            String[] rule = line.Split('\t');

            if (rule.Length != 2)
            {
                return false;
            }
            Filter f = new Filter();
            String command = rule[0].ToLowerInvariant();
            if (command == "allow")
            {
                f.Allow = true;
            }
            else
            {
                if (command == "deny")
                {
                    f.Allow = false;
                }
                else
                {
                    return false;
                }
            }
            try
            {
                String pattern = rule[1];
                if (pattern.Length > 0)
                {
                    if (pattern[0] == '$')
                    {
                        pattern = pattern.Substring(1);
                    }
                    else
                    {
                        // Assume user wants glob rules rather than full regex.
                        String[] segments = pattern.Split('.');
                        pattern = "";
                        foreach (String segment in segments)
                        {
                            String sr = segment.Replace("\\", "\\\\");
                            sr = sr.Replace("*", ".*");
                            sr = sr.Replace("?", ".");
                            sr = sr.Replace("/", "\\");
                            if (pattern.Length > 0)
                            {
                                pattern = pattern + "\\.";
                            }
                            pattern = pattern + sr;
                        }
                    }
                    f.Pattern = new Regex(pattern, RegexOptions.IgnoreCase);
                    m_filters.Add(f);
                    return true;
                }
            }
            catch (System.ArgumentException ex)
            {
            	Errors += "Invalid pattern '"+rule[1]+"' will be ignored : "+ex.Message+"\n";
            }
            return false;
        }

        public bool Allow(String item)
        {
            foreach (Filter f in m_filters)
            {
                if (f.Pattern.IsMatch(item))
                {
                    return f.Allow;
                }
            }
            // Implicit "Allow .*" at the end
            return true;
        }
    }
}
