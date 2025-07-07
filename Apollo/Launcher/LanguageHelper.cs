//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! LanguageHelper, this is a help class which retrieves the languages
//  and sets them up for display.
//
//! Author:     Alan MacAree
//! Created:    19 Aug 2022
//----------------------------------------------------------------------

using CBViewModel;
using ClientSupport;
using FDUserControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Launcher
{
    /// Specify our LanguageDictionary
    using LanguageDictionary = Dictionary<string, string>;

    class LanguageHelper
    {
        /// <summary>
        /// Returns a LanguageDictionary that contains a KeyValuePair of
        /// language code and language strings for display.
        /// </summary>
        /// <param name="_CobraBayView">The _CobraBayView to get the available languages from</param>
        /// <param name="_logEventInterface">An ILogEvent interface to log any issues</param>
        /// <returns>The LanguageDictionary that contains language code and language strings</returns>
        static public LanguageDictionary GetLanguageDictionary( CobraBayView _CobraBayView, ILogEvent _logEventInterface )
        {
            Debug.Assert( _CobraBayView != null );
            Debug.Assert( _logEventInterface != null );

            LanguageDictionary languageDictionary = new LanguageDictionary();

            if ( _CobraBayView != null )
            {
                // Get the available languages via FORCManager, send this list of
                // languages to the LanguagesCtrl, including the currently used one.
                FORCManager forcManager = _CobraBayView.Manager();

                if ( forcManager != null )
                {
                    List<string> availableLanguages = forcManager.GetAvailableLanguages();

                    // Add "Use System Default"
                    string useSystemDefault = LocalResources.Properties.Resources.SLW_System;
                    languageDictionary.Add( "", useSystemDefault );
                    foreach ( string language in availableLanguages )
                    {
                        if ( !string.IsNullOrWhiteSpace( language ) )
                        {
                            try
                            {
                                CultureInfo CI = CultureInfo.GetCultureInfo( language );
                                string fullLanguageName = CI.NativeName;

                                // NativeName returns some languages starting with a lower case letter.
                                // Convert the 1st char to Uppercase and add the rest of the name before 
                                // adding our new language to the list of available languages.
                                if ( !string.IsNullOrWhiteSpace( fullLanguageName ) )
                                {
                                    string displayLanguageName = (char.ToUpper( fullLanguageName[0] )).ToString();
                                    if ( fullLanguageName.Length > 1 )
                                    {
                                        displayLanguageName += fullLanguageName.Substring( 1 );
                                    }
                                    else
                                    {
                                        // We should have a language with more than 1 char,
                                        // hence this is an odd error.
                                        LogEvent( _logEventInterface, "LanguageHelper", "Trying to identify current language", "Language length is zero." );
                                    }
                                    languageDictionary.Add( language, displayLanguageName );
                                }
                                else
                                {
                                    // NativeName returned Null or blank, this is unexpected
                                    // because it should be caught by the exception try//catch
                                    LogEvent( _logEventInterface, "LanguageHelper", "Trying to identify current language", "Failed to identify current language." );
                                }
                            }
                            catch ( ArgumentNullException ex )
                            {
                                // We should not get here, log the exception
                                LogEvent( _logEventInterface, "LanguageHelper", "Trying to identify current language", "ArgumentNullException caught." );
                                LogEvent( _logEventInterface, "LanguageHelper", "Trying to identify current language", ex.ToString() );
                            }
                            catch ( CultureNotFoundException ex )
                            {
                                // We should not get here, log the exception
                                LogEvent( _logEventInterface, "LanguageHelper", "Trying to identify current language", "CultureNotFoundException caught." );
                                LogEvent( _logEventInterface, "LanguageHelper", "Trying to identify current language", ex.ToString() );
                            }
                            catch ( Exception ex )
                            {
                                // Unknow exception
                                LogEvent( _logEventInterface, "LanguageHelper", "Trying to identify current language", "Execption caught." );
                                LogEvent( _logEventInterface, "LanguageHelper", "Trying to identify current language", ex.ToString() );
                            }
                        }
                        else
                        {
                            // The language retrived is empty.
                            LogEvent( _logEventInterface, "LanguageHelper", "Trying to identify current language", "No available languages identified" );
                        }
                    }
                }
                else
                {
                    // forcManager is null, this shows an incorrect state of the system
                    LogEvent( _logEventInterface, "LanguageHelper", "Trying to identify current language", "FORCManager is null, logic error." );
                }
            }
            else
            {
                // m_CobraBayView is null, this shows an incorrect state of the system
                LogEvent( _logEventInterface, "LanguageHelper", "Trying to identify current language", "CobraBayView  is null, logic error" );
            }

            return languageDictionary;
        }

        /// <summary>
        /// Returns the default language code to use for the current local settings
        /// </summary>
        /// <returns></returns>
        static public string GetDefaultLanguageString()
        {
            string defaultLanguageCode = "en";

            CultureInfo culture = CultureInfo.CurrentCulture;

            switch ( culture.Name )
            {
                case "fr-BE":
                case "fr-CA":
                case "fr-LU":
                case "fr-MC":
                case "fr-CH":
                case "fr-FR":
                case "fr-029":
                case "fr":
                    defaultLanguageCode = "fr";
                    break;
                case "es-AR":
                case "es-BO":
                case "es-CL":
                case "es-CO":
                case "es-CR":
                case "es-DO":
                case "es-EC":
                case "es-SV":
                case "es-GT":
                case "es-HN":
                case "es-419":
                case "es-ES":
                case "es":
                case "es-MX":
                case "es-NI":
                case "es-PA":
                case "es-PE":
                case "es-PR":
                case "es-UY":
                case "es-VE":
                case "es-US":
                    defaultLanguageCode = "es";
                    break;
                case "ru-RU":
                case "ru":
                case "ru-BY":
                case "ru-KZ":
                case "ru-KG":
                case "ru-MD":
                case "ru-UA":
                    defaultLanguageCode = "ru";
                    break;
                case "de-DE":
                case "de":
                case "de-AT":
                case "de-LI":
                case "de-LU":
                case "de-CH":
                    defaultLanguageCode = "de";
                    break;
                case "pt-BR":
                case "pt":
                case "pt-BT":
                    defaultLanguageCode = "pt-BR";
                    break;
            }

            return defaultLanguageCode;
        }

        /// <summary>
        /// Returns the current Culture String
        /// </summary>
        /// <returns></returns>
        static public string GetCurrentCultureString()
        {
            CultureInfo culture = CultureInfo.CurrentCulture;
            return culture.Name;
        }

        /// <summary>
        /// Attempts to log an event
        /// </summary>
        /// <param name="_logEventInterface">The interface to log the event to</param>
        /// <param name="_action">The action to log</param>
        /// <param name="_key">The key to log</param>
        /// <param name="_description">The description to log</param>
        static private void LogEvent( ILogEvent _logEventInterface,
                                      string _action,
                                      string _key,
                                      string _description )
        {
            Debug.Assert( _logEventInterface != null );

            if ( _logEventInterface != null )
            {
                _logEventInterface.LogEvent( _action, _key, _description );
            }
        }

    }
}
