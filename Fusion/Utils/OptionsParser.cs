using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utils
{
    public abstract class OptionsHandler
    {
        public abstract void OptionsChanged(OptionsParser parser);
    }

    public class OptionsParser
    {
        Dictionary<String, String> m_options;
        Dictionary<String, String> m_alias;
        Dictionary<String, Boolean> m_flags;
        List<String> m_positional;
        List<String> m_errors;
        List<OptionsHandler> m_handlers;

        public bool HasErrors { get { return m_errors != null; } }
        public IEnumerable<String> Errors { get { return m_errors; } }

        public OptionsParser()
        {
            m_options = new Dictionary<String, String>();
            m_flags = new Dictionary<String, Boolean>();
            m_positional = new List<String>();
        }

        public void Parse()
        {
            string[] args = Environment.GetCommandLineArgs();

            for (int a = 1; a < args.Length; ++a)
            {
                String arg = args[a];
                if (m_alias!=null)
                {
                    while (m_alias.ContainsKey(arg))
                    {
                        arg = m_alias[arg];
                    }
                }
                if (m_options.ContainsKey(arg))
                {
                    ++a;
                    if (a >= args.Length)
                    {
                        AddError(arg, "Required value missing");
                    }
                    else
                    {
                        m_options[arg] = args[a];
                    }
                }
                else
                {
                    if (m_flags.ContainsKey(arg))
                    {
                        m_flags[arg] = true;
                    }
                    else
                    {
                        m_positional.Add(arg);
                    }
                }
            }

            foreach (OptionsHandler handler in m_handlers)
            {
                handler.OptionsChanged(this);
            }
        }

        public String ErrorSummary()
        {
            String message = "Option parsing reported the following errors:\n";
            foreach (String e in Errors)
            {
                message = message + "\t" + e + "\n";
            }
            return message;
        }

        public void AddError(String option, String message)
        {
            if (m_errors == null)
            {
                m_errors = new List<String>();
            }
            m_errors.Add(option + ": " + message);
        }

        public void AddFlag(String flag)
        {
            m_flags[flag] = false;
        }

        public void AddCommand(String option, String defaultValue)
        {
            m_options[option] = defaultValue;
        }

        public void AddAlias(String alias, String original)
        {
            if (alias == original)
            {
                return;
            }

            if (m_alias == null)
            {
                m_alias = new Dictionary<string, string>();
            }

            String next = original;
            while (next!=null)
            {
                if (m_alias.ContainsKey(next))
                {
                    next = m_alias[next];
                    if (next == alias)
                    {
                        AddError(alias,"Adding Alias would create an alias loop.");
                        return;
                    }
                }
                else
                {
                    next = null;
                }
            }
            m_alias[alias] = original;
        }

        public bool GetFlag(String flag)
        {
            if (m_flags.ContainsKey(flag))
            {
                return m_flags[flag];
            }
            return false;
        }

        public String Get(String option)
        {
            if (m_options.ContainsKey(option))
            {
                return m_options[option];
            }
            return null;
        }

        public bool GetAsDouble(String option, double missing, out double value)
        {
            value = missing;
            String v = Get(option);
            if (String.IsNullOrEmpty(v))
            {
                return false;
            }
            if (double.TryParse(v, out value))
            {
                return true;
            }
            return false;
        }

        public void AddHandler(OptionsHandler handler)
        {
            if (m_handlers == null)
            {
                m_handlers = new List<OptionsHandler>();
            }
            m_handlers.Add(handler);
        }
    }
}
