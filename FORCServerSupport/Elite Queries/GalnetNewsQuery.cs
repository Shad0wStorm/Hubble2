//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! GalnetNewsQuery, requests news from Galnet.
//
//! Author:     Alan MacAree
//! Created:    28 Sept 2022
//----------------------------------------------------------------------

using ClientSupport;
using System;
using System.Diagnostics;
using System.Net;

namespace FORCServerSupport.Elite_Queries
{
    /// <summary>
    /// JSONWebQuery that requests Galnet news from the server
    /// </summary>
    public class GalnetNewsQuery : JSONWebQuery
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public GalnetNewsQuery()
            : base( "", true )
        {
        }

        /// <summary>
        /// Runs the Query to get Galnet news
        /// </summary>
        /// <param name="_project">The project to get the news for</param>
        /// <param name="_languageCode">The language code to get the news in</param>
        /// <returns>The Json string containing the reply from the server</returns>
        public string Run( Project _project, string _languageCode )
        {
            string galnetNewsResponse = null;

            Debug.Assert( _project != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( _languageCode ) );

            if ( _project != null && !string.IsNullOrWhiteSpace( _languageCode ) )
            {
                Uri apiUri = EliteApiUri.GetGalnetUri( _project, _languageCode );

                if ( apiUri != null && !string.IsNullOrWhiteSpace( apiUri.ToString() ) )
                {
                    String message = null;
                    HttpStatusCode response = Execute(apiUri, out galnetNewsResponse, out message );
                }
            }

            return galnetNewsResponse;
        }
    }
}
