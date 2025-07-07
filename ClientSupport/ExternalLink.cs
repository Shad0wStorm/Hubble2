using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientSupport
{
    /// <summary>
    /// Class for representing a link to an external web site.
    /// </summary>
    public class ExternalLink
    {
        /// <summary>
        /// Indicates whether the link is valid.
        /// </summary>
        public bool IsEnabled { get { return URL != null; } }
        /// <summary>
        /// Textual URL to use. If we start needing to break down the URL
        /// instead of simply passing it to the default web browser we may want
        /// to use a real URL class instead.
        /// </summary>
        public String URL { get; set; }
    }
}
