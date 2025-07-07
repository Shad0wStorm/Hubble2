//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! HTMLStringUtils, provides utility methodes for HTML strings
//
//! Author:     Alan MacAree
//! Created:    03 Nov 2022
//----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace FDUserControls
{
    /// <summary>
    /// Provides HTML String Utility Methods
    /// </summary>
    public class HTMLStringUtils
    {
        /// <summary>
        /// Strips HTML tags from strings and returns the result.
        /// </summary>
        /// <param name="_stringWithHTML">The string with HTML tags</param>
        /// <param name="_alsoRemoveTabs">Causes tabs to also be removed, defaults to true</param>
        /// <returns>A string without HTML tags</returns>
        public static string StripHTMLFromString( string _stringWithHTML, bool _alsoRemoveTabs = true )
        {
            string stringWithNoHTML = _stringWithHTML;

            if ( !string.IsNullOrWhiteSpace( _stringWithHTML ) )
            {
                stringWithNoHTML = Regex.Replace( _stringWithHTML, c_removeHTMLRegexHTMLTags, string.Empty );

                // Should we also remove tabs (tabs can cause issues when displaying text within WPF ctrls)
                if ( _alsoRemoveTabs )
                {
                    stringWithNoHTML = Regex.Replace( stringWithNoHTML, "\t", string.Empty );
                }
            }

            return stringWithNoHTML;
        }

        /// <summary>
        /// Used to remove HTML tags from strings
        /// </summary>
        private static string c_removeHTMLRegexHTMLTags = @"<.*?>";
    }
}
