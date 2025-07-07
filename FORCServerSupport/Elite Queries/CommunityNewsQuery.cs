//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! CommunityNewsQuery, requests Community News from Elite API.
//
//! Author:     Alan MacAree
//! Created:    13 Oct 2022
//----------------------------------------------------------------------

using ClientSupport;
using System;
using System.Diagnostics;
using System.Net;

namespace FORCServerSupport.Elite_Queries
{
    /// <summary>
    /// JSONWebQuery that requests Community News from the server
    /// </summary>
    public class CommunityNewsQuery : JSONWebQuery
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public CommunityNewsQuery()
            : base( "", true )
        {
        }

        /// <summary>
        /// Runs the Query to get Community News
        /// </summary>
        /// <param name="_project">The project to get the news for</param>
        /// <param name="_languageCode">The language code to get the news in</param>
        /// <returns>The Json string containing the reply from the server</returns>
        public string Run( Project _project, string _languageCode )
        {
            string serverResponse = null;

            Debug.Assert( _project != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( _languageCode ) );

            if ( _project != null && !string.IsNullOrWhiteSpace( _languageCode ) )
            {
                Uri apiUri = EliteApiUri.GetCommunityNewsUri( _project, _languageCode );

                if ( apiUri != null && !string.IsNullOrWhiteSpace( apiUri.ToString() ) )
                {
                    String message = null;
                    HttpStatusCode response = Execute(apiUri, out serverResponse, out message );
                }
            }

            return serverResponse;
        }
    }
}

