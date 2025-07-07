//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! Information Class, data holding class
//
//! Author:     Alan MacAree
//! Created:    02 Nov 2022
//----------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows.Controls;

namespace FDUserControls
{
    /// <summary>
    /// Data holding class
    /// </summary>
    public class Information
    {
        /// <summary>
        /// Holds the title
        /// </summary>
        public string Title { get; set; } = null;

        /// <summary>
        /// Holds the sub title
        /// </summary>
        public string SubTitle { get; set; } = null;

        /// <summary>
        /// Holds the description (or main text)
        /// </summary>
        public string Description { get; set; } = null;

        /// <summary>
        /// Holds a HTTPLink
        /// </summary>
        public string HTTPLink { get; set; } = null;

        /// <summary>
        /// Holds a HTTPLink Text
        /// </summary>
        public string HTTPLinkText { get; set; } = null;

        /// <summary>
        /// The parent page
        /// </summary>
        public Page ParentPage { get; private set; }

        /// <summary>
        /// The ILogEvent interface, used to log issues
        /// </summary>
        public ILogEvent LogEventInterface { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_parentPage">The parent page to return to, can be null</param>
        /// <param name="_iLogEvent">The interface used to log issues</param>
        public Information( Page _parentPage,
                            ILogEvent _iLogEvent )
        {
            ParentPage = _parentPage;
            LogEventInterface = _iLogEvent;
        }
    }
}
