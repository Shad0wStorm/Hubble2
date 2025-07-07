//----------------------------------------------------------------------
//! Copyright(c) 2023 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! LocalisationHelper, this is a localisation helper class. 
//
//! Author:     Alan MacAree
//! Created:    20 April 2023
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Launcher
{
    /// <summary>
    /// This is a localisations helper class, it provides helper methods in order to encapsulate some of the 
    /// complexity which would otherwise by within UI code.
    /// </summary>
    public class LocalisationHelper
    {
        /// <summary>
        /// Converts a PasswordCode to a localised string. This is required because the servers do not provide
        /// a localised version of password issues (only English text and a hashed code), we use the hashed code
        /// here (PasswordCode) to get the translated form of the issue. If the server gets updated and generates 
        /// a new code (which is a possibility), then this will cause a new hash code to be generated, either  
        /// resulting in a default or an empty string being returned by this method.
        /// 
        /// Note that the end of the resource ID names match the _passwordCode, this is to assist in both manually 
        /// checking the resource strings, and to create a better link between the PasswordCode and the associated string.
        /// 
        /// </summary>
        /// <param name="_passwordCode">The password code to get the localised text for.</param>
        /// <param name="_UseADefaultString">If true, then a default string will be used if the _passwordCode is unknown.</param>
        /// <returns>A localised string for the passed _passwordCode, or a default value if unknown and _UseADefaultString set to true.</returns>
        public static string PasswordCodeToLocalisedString( string _passwordCode, bool _UseADefaultString = false )
        {
            string resultingResourceText = string.Empty;

            if ( !string.IsNullOrWhiteSpace( _passwordCode ) )
            {
                switch ( _passwordCode )
                {
                    case "9c7ac60c":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordDatesAreEasy_9c7ac60c;
                        break;
                    case "8260ff9b":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordAvoidDates_8260ff9b;
                        break;
                    case "5e48faf9":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordCaps_5e48faf9;
                        break;
                    case "d0f9e6e6":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordAllCaps_d0f9e6e6;
                        break;
                    case "d04fec50":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordTop10_d04fec50;
                        break;
                    case "d9cb0d3b":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordTop100_d9cb0d3b;
                        break;
                    case "5fefdb80":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordVeryCommon_5fefdb80;
                        break;
                    case "9847c65c":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordNearVeryCommon_9847c65c;
                        break;
                    case "fe6d3a04":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordSingleWord_fe6d3a04;
                        break;
                    case "ea60b49f":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordNameSurname_ea60b49f;
                        break;
                    case "fa93fb1d":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordCommonName_fa93fb1d;
                        break;
                    case "74d5697c":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordPredictableSub_74d5697c;
                        break;
                    case "61edf813":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordAddWords_61edf813;
                        break;
                    case "1fb918fb":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordRepeats_1fb918fb;
                        break;
                    case "23829bf0":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordRepeats_23829bf0;
                        break;
                    case "0fdf1c51":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordRepeatedWords_0fdf1c51;
                        break;
                    case "18b0045b":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordReversedWords_18b0045b;
                        break;
                    case "807fb9ae":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordSequenceLike_807fb9ae;
                        break;
                    case "f6e20121":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordAvoidSequence_f6e20121;
                        break;
                    case "ac4522c4":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordStraightKeyRows_ac4522c4;
                        break;
                    case "50a6280f":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordShortFBPatterns_50a6280f;
                        break;
                    case "1deedb29":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordLongKBPattern_1deedb29;
                        break;
                    case "823d07f3":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordRecentYears_823d07f3;
                        break;
                    case "727620c4":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordAvoidRecentYears_727620c4;
                        break;
                    case "71568b72":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordAvoidAssociatedYears_71568b72;
                        break;
                    case "8c4c0923":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordAvoidPhrases_8c4c0923;
                        break;
                    case "f8de7985":
                        resultingResourceText = LocalResources.Properties.Resources.TXT_PasswordMoreSymbols_f8de7985;
                        break;
                    default:
                        if ( _UseADefaultString )
                        {
                            resultingResourceText = LocalResources.Properties.Resources.TXT_PassWarnDefault;
                        }
                        break;
                }
            }

            return resultingResourceText;
        }
    }
}
