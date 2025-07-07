//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! RequestGameDescriptionQuery, requests the game description.
//
//! Author:     Alan MacAree
//! Created:    28 Jan 2023
//----------------------------------------------------------------------

using ClientSupport;
using System;
using System.Diagnostics;
using System.Net;

namespace FORCServerSupport.Queries
{
    /// <summary>
    /// RequestGameDescriptionQuery. this requests a game description from the 
    /// server API.
    /// </summary>
    internal class RequestGameDescriptionQuery : JSONWebQuery
    {
        /// <summary>
        /// Default Constructor, we pass an empty string to the base cass because
        /// the base call appends this value to the Uri passed from this class. However
        /// we need to further append the project name. Hence we do all of that in this
        /// class and stop the base class from providing that functionality
        /// </summary>
        public RequestGameDescriptionQuery()
            : base( string.Empty )
        {
        }

        /// <summary>
        /// Runs the Query to get the game description
        /// </summary>
        /// <param name="_state">The FORCServerState</param>
        /// <param name="_project">The project to get the featured products for</param>
        /// <returns>The Json string containing the reply from the server</returns>
        public string Run( FORCServerState _state, Project _project )
        {
            string serverResponse = null;

            Debug.Assert( _project != null );
            Debug.Assert( _state != null );

            if ( _project != null )
            {
                Uri apiUri = _state.GetServerAPI( FORCServerState.APIVersion.V3_0 );
                Debug.Assert( apiUri != null && !string.IsNullOrWhiteSpace( apiUri.ToString() ) );

                if ( apiUri != null && !string.IsNullOrWhiteSpace( apiUri.ToString() ) )
                {
                    // Now we build the whole Uri, this consists of:
                    // The Server API + Base Uri + Project Name + language string
                    string apiUriString = apiUri.AbsoluteUri;
                    apiUriString += c_baseUri;
                    apiUriString += _project.Name;

                    // Now add the language string
                    if ( _state != null )
                    {
                        if ( !String.IsNullOrEmpty( _state.Language ) )
                        {
                            string languageCode = CheckAndReplacePortugueseLanguageCode( _state.Language );
                            apiUriString += c_forwardSlash;
                            apiUriString += languageCode;
                        }
                    }

                    apiUri = new Uri( apiUriString );

                    String message = null;
                    HttpStatusCode response = Execute(apiUri, out serverResponse, out message );
                }
            }

            return serverResponse;
        }

        /// <summary>
        /// Checks and replaces the Portuguese Language Code of c_replacePortugueseCode
        /// with c_replacePortugueseCodeWithEliteVersion
        /// </summary>
        /// <param name="_languageCode"></param>
        /// <returns></returns>
        private string CheckAndReplacePortugueseLanguageCode( string _languageCode )
        {
            string replacedLanguageString = _languageCode;
            if ( _languageCode.CompareTo( c_replacePortugueseCode ) == 0 )
            {
                replacedLanguageString = c_replacePortugueseCodeWithEliteVersion;
            }

            return replacedLanguageString;
        }

        /// <summary>
        /// We have to do something special with the base Uri. This
        /// is normally passed to the base class and that is appened 
        /// to the server Uri just before performing the server call.
        /// But in the case of GameDescription, we have to append the
        /// project name at the end of this uri. Hence we build the 
        /// most of the Uri in this class and do not pass a base Uri
        /// to the base class.
        /// </summary>
        private const string c_baseUri = "store/game/";

        /// <summary>
        /// A forward slash char.
        /// </summary>
        private const string c_forwardSlash = "/";

        /// <summary>
        /// The language field for featured products
        /// </summary>
        private const string c_languageField = "language";

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