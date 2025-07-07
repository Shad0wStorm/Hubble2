//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! FeaturedProductsQuery, requests featured products from FORC API.
//
//! Author:     Alan MacAree
//! Created:    28 Oct 2022
//----------------------------------------------------------------------

using ClientSupport;
using System;
using System.Diagnostics;
using System.Net;

namespace FORCServerSupport.Queries
{
    /// <summary>
    /// FeaturedProductsQuery that requests featured productsfrom the server
    /// </summary>
    internal class FeaturedProductsQuery : JSONWebQuery
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public FeaturedProductsQuery()
            : base( "store/featured" )
        {
        }

        /// <summary>
        /// Runs the Query to get featured products
        /// </summary>
        /// <param name="_state">The FORCServerState</param>
        /// <param name="_project">The project to get the featured products for</param>
        /// <returns>The Json string containing the reply from the server</returns>
        public string Run( FORCServerState _state, Project _project )
        {
            string serverResponse = null;

            Debug.Assert( _project != null );
            Debug.Assert( _state != null ) ;

            if ( _state != null )
            {
                if ( !String.IsNullOrEmpty( _state.Language ) )
                {
                    string languageCode = CheckAndReplacePortugueseLanguageCode( _state.Language );
                    AddParameter( c_languageField, languageCode );
                }
            }

            if ( _project != null )
            {
                Uri apiUri = _state.GetServerAPI( FORCServerState.APIVersion.V3_0 );
                Debug.Assert( apiUri != null && !string.IsNullOrWhiteSpace( apiUri.ToString() ) );

                if ( apiUri != null && !string.IsNullOrWhiteSpace( apiUri.ToString() ) )
                {
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
