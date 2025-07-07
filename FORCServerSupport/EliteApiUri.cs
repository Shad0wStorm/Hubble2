//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! EliteApiUri, builds the URI for the Elite API
//
//! Author:     Alan MacAree
//! Created:    28 Sept 2022
//----------------------------------------------------------------------

using ClientSupport;
using System;
using System.Diagnostics;

namespace FORCServerSupport
{
    /// <summary>
    /// Builds the full URI to accss the EliteApi:
    /// example http://trunk.api.elite.onsrvdev1.corp.frontier.co.uk/2.0/launcher/galnet?lang=en
    /// 
    /// This is split into 4 parts
    /// Project Specific EliteApiURI - http://trunk.api.elite.onsrvdev1.corp.frontier.co.uk/
    /// EliteApi version             - 2.0
    /// EliteApiUri                  - /launcher/galnet
    /// Languag specific Uri         - ?lang=en
    /// </summary>
    public class EliteApiUri
    {
        /// <summary>
        /// Returns the project specific part of the Elite URI.
        /// This is not the whole URI path.
        /// </summary>
        /// <param name="_project"></param>
        /// <returns>The Project specific Elite API URI</returns>
        static protected string GetProjectPartEliteUri( Project _project )
        {
            Debug.Assert( _project != null );

            string eliteUri = null;

            if ( _project != null )
            {
                eliteUri = _project.GameApi;
            }

            return eliteUri;
        }

        /// <summary>
        /// Returns the version of the API we should be using
        /// </summary>
        /// <returns>The Elite API version</returns>
        static protected string GetEliteAPIVersion()
        {
            return c_eliteAPIVersion;
        }

        /// <summary>
        /// Returns the Galnet part of the URI
        /// </summary>
        /// <returns>The galnet part of the URI</returns>
        static protected string GalnetSubSystemUri()
        {
            return c_galnetUri;
        }

        /// <summary>
        /// Returns the Community Goals part of the URI
        /// </summary>
        /// <returns>The Community Goals part of the URI</returns>
        static protected string CommunityGoalsSubSystemUri()
        {
            return c_communityGoalsUri;
        }

        /// <summary>
        /// Returns the Community Goals part of the URI
        /// </summary>
        /// <returns>The Community Goals part of the URI</returns>
        static protected string CommunityNewsSubSystemUri()
        {
            return c_communityNewsUri;
        }


        /// <summary>
        /// Returns the Livery part of the URI
        /// </summary>
        /// <returns>The Livery part of the URI</returns>
        static protected string LiveryUri()
        {
            return c_liveryUri;
        }

        /// <summary>
        /// Returns the language URI string, this is appeneded to a URI
        /// </summary>
        /// <returns></returns>
        static protected string GetLanguageUriString( string _languageCode )
        {
            Debug.Assert( !string.IsNullOrWhiteSpace( _languageCode ) );
            string result = c_languageUriString;

            if ( !string.IsNullOrWhiteSpace( _languageCode ) )
            {
                // Check and replace Portuguese Language Code for the Elite API
                _languageCode = CheckAndReplacePortugueseLanguageCode( _languageCode );
                result += _languageCode;
            }
            else
            {
                result += c_defaultLanguageCode;
            }

            return result;
        }

        /// <summary>
        /// Builds the Elite API Uri from a number of different sources
        /// Including:
        ///     Project
        ///     Elite API Version
        ///     System Uri
        ///     Language
        /// </summary>
        /// <param name="_project">The project to get the Elite Api Url for</param>
        /// <param name="_subSystemUri">The system to get the Elite Api Url for</param>
        /// <param name="_languageCode">The language code to get the Elite Api Url for</param>
        /// <returns>A Uri to the Elite API or null</returns>
        static private Uri GetUri( Project _project, string _subSystemUri, string _languageCode )
        {
            Debug.Assert( _project != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( _languageCode ) );

            string uriString = null;

            Uri uri = null;

            if ( _project != null && !string.IsNullOrWhiteSpace( _languageCode ) )
            {
                // Build the URI up from:

                // The project specific Uri, passed via the project
                uriString = GetProjectPartEliteUri( _project );
                // The API version we are using
                uriString += GetEliteAPIVersion();
                // The subsystem Uri
                uriString += _subSystemUri;
                // The language portion of the Uri
                uriString += GetLanguageUriString( _languageCode );

                try
                {
                    uri = new Uri( uriString );
                }
                catch( Exception )
                {
                    uri = null;
                }
            }

            return uri;
        }

        /// <summary>
        /// Returns the full Galnet API URI
        /// </summary>
        /// <param name="_project">The project to return the URI for, must not be null</param>
        /// <param name="_languageCode">The language code o us, must not be null or empty</param>
        /// <returns>The full Galnet API URI, this can be null</returns>
        static public Uri GetGalnetUri( Project _project, string _languageCode )
        {
            Debug.Assert( _project != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( _languageCode ) );

            return GetUri( _project, GalnetSubSystemUri(), _languageCode );
        }

        /// <summary>
        /// Returns the full Community Goals API URI
        /// </summary>
        /// <param name="_project">The project to return the URI for, must not be null</param>
        /// <param name="_languageCode">The language code o us, must not be null or empty</param>
        /// <returns>The full Community Goals API URI, this can be null</returns>
        static public Uri GetCommunityGoalsUri( Project _project, string _languageCode )
        {
            Debug.Assert( _project != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( _languageCode ) );

            return GetUri( _project, CommunityGoalsSubSystemUri(), _languageCode );
        }

        /// <summary>
        /// Returns the full Community News API URI
        /// </summary>
        /// <param name="_project">The project to return the URI for, must not be null</param>
        /// <param name="_languageCode">The language code o us, must not be null or empty</param>
        /// <returns>The full Community News API URI, this can be null</returns>
        static public Uri GetCommunityNewsUri( Project _project, string _languageCode )
        {
            Debug.Assert( _project != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( _languageCode ) );

            return GetUri( _project, CommunityNewsSubSystemUri(), _languageCode );
        }

        /// <summary>
        /// Returns the full Livery API URI
        /// </summary>
        /// <param name="_project">The project to return the URI for, must not be null</param>
        /// <param name="_languageCode">The language code o us, must not be null or empty</param>
        /// <returns>The full Community News API URI, this can be null</returns>
        static public Uri GetLiveryUri(Project _project, string _languageCode)
        {
            Debug.Assert(_project != null);
            Debug.Assert(!string.IsNullOrWhiteSpace(_languageCode));

            return GetUri(_project, LiveryUri(), _languageCode);
        }

        /// <summary>
        /// Checks and replaces the Portuguese Language Code of c_replacePortugueseCode
        /// with c_replacePortugueseCodeWithEliteVersion
        /// </summary>
        /// <param name="_languageCode"></param>
        /// <returns></returns>
        static private string CheckAndReplacePortugueseLanguageCode( string _languageCode )
        {
            string replacedLanguageString = _languageCode;
            if ( _languageCode.CompareTo( c_replacePortugueseCode ) == 0 )
            {
                replacedLanguageString = c_replacePortugueseCodeWithEliteVersion;
            }

            return replacedLanguageString;
        }

        /// <summary>
        /// The version of the Elite API we should be using
        /// </summary>
        private const string c_eliteAPIVersion = "/2.0";

        /// <summary>
        /// The language part of the Elite API URI
        /// </summary>
        private const string c_languageUriString = "?lang=";

        /// <summary>
        /// Galnet URI part
        /// </summary>
        private const string c_galnetUri = "/launcher/galnet";

        /// <summary>
        /// Community Goals URI part
        /// </summary>
        private const string c_communityGoalsUri = "/launcher/community_goals";

        /// <summary>
        /// Community News URI part
        /// </summary>
        private const string c_communityNewsUri = "/launcher/community_news";

        /// <summary>
        /// Livery URI part
        /// </summary>
        private const string c_liveryUri = "/launcher/livery";

        /// <summary>
        /// Default language code, this is used in case none is 
        /// passed to the methods in this class.
        /// </summary>
        private const string c_defaultLanguageCode = "en";

        /// <summary>
        /// The Portuguese Code, if it is used as a language string.
        /// This must be replaced with c_replacePortugueseCodeWith.
        /// </summary>
        private const string c_replacePortugueseCode = "pt-br";

        /// <summary>
        /// The Portuguese Code to use as a language code
        /// </summary>
        private const string c_replacePortugueseCodeWithEliteVersion = "pt";
    }
}
