//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! Utils, a place for collection of useful util type methods
//
//! Author:     Alan MacAree
//! Created:    18 Nov 2022
//----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Launcher
{
    internal class Utils
    {
        /// <summary>
        /// Checks if the passed email address is in a valid email address format
        /// </summary>
        /// <param name="_emailAddress">The email address to check</param>
        /// <returns>Returns true if the email address is in a valid format</returns>
        internal static bool IsEmailAddressFormatValid( string _emailAddress )
        {
            bool validAddress = false;

            try
            {
                // Attempt to validate the email address using a regex
                // not using EmailAddressAttribute because that only has very cursory validation rules
                // nor using MailAddress because its docs (https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.mailaddress?view=netframework-4.8) suggests it specifically allows "Consecutive and trailing dots in user names. For example, user...name..@host.", which isn't valid in RFC-5322

                validAddress = Regex.IsMatch(_emailAddress, c_emailValidationRegEx);
#if DEBUG
                Debug.WriteLine("email " + _emailAddress + " is valid " + validAddress.ToString());
#endif
            }
            catch ( Exception )
            {
                validAddress = false;
            }

            return validAddress;
        }

        /// <summary>
        /// Formats a string that can contain underscores for use in UI Labels. Failure to
        /// replace a single underscore with a double underscore results
        /// in the underscore not being displayed in UI Labels.
        /// </summary>
        /// <param name="_labelStringToFormat">The string that may contain an underscore</param>
        /// <returns>A formatted version of the _labelStringToFormat which allows underscores to be 
        /// displayed in UI Labels</returns>
        internal static string FormatLabelString( string _labelStringToFormat )
        {
            return _labelStringToFormat.Replace( c_underscore, c_doubleUnderscore );
        }

        /// <summary>
        /// Email validation patterns, used to determine if an email address seems to be valid or not according to RFC 5322
        /// uses ^ and $ to match the entire line to avoid matching substrings
        /// todo: add unicode support throughout
        /// - Local part looks for a pattern between 1 and 64 characters long, each character must at least match one of these components
        ///  1) List of allowable printable non-special characters (from frontierstore.net's javascript validation of us-ascii + uppercase) 
        ///  2) a dot that is both followed and preceded by a character from the class from test 1 (a single . allowed inside an unquoted local-part but no double .s)
        ///  3) matches a " but only when preceded by start of line or . (positive lookbehind, this and the next component permit "s surrounding the full local-part but not within it)
        ///  4) matches a " but only when followed by end of line or . (positive lookahead)
        ///  5) either a space or. but only when both followed by and preceded by ", any distance away  (positive lookbehind and lookahead - any number of spaces and dots are allowed when inside "s)
        /// - Domain part taken from https://regexr.com/2rhq7
        ///                                                     local-part test 1            |  local-part test 2                                                      | localpart3| localpart4 | localpart test 5  | 64 chars|@| domain test
        /// </summary>
        private const string c_emailValidationRegEx = @"^((?:[A-Za-z0-9!#$%&'*+\-\/=?^_`{|}~]|(?<=[A-Za-z0-9!#$%&'*+/=?^_`{|}~\-])\.(?=[A-Za-z0-9!#$%&'*+/=?^_`{|}~\-])|(?<=^|\.)""|""(?=$|\.|@)|(?<="".*)[ .](?=.*"")){1,64})(@)((?:[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?\.)+[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9]))$";


        /// <summary>
        /// Defines a underscore
        /// </summary>
        private const string c_underscore = "_";

        /// <summary>
        /// Defines a double underscore, these are used in place of 
        /// underscores in text that is displayed on labels. Failure to
        /// replace a single underscore with a double underscore results
        /// in the underscore not being displayed in UI Labels.
        /// </summary>
        private const string c_doubleUnderscore = "__";
    }
}
