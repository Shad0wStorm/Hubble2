//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! CommunityGoalsQuery, requests CommunityGoals from Elite API.
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
    /// JSONWebQuery that requests CommunityGoals from the server
    /// </summary>
    public class CommunityGoalsQuery : JSONWebQuery
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public CommunityGoalsQuery()
            : base( "", true )
        {
        }

        /// <summary>
        /// Runs the Query to get Community Goals
        /// </summary>
        /// <param name="_project">The project to get the goals for</param>
        /// <param name="_languageCode">The language code to get the goals in</param>
        /// <returns>The Json string containing the reply from the server</returns>
        public string Run( Project _project, string _languageCode )
        {
            string serverResponse = null;

            Debug.Assert( _project != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( _languageCode ) );

            if ( _project != null && !string.IsNullOrWhiteSpace( _languageCode ) )
            {
                Uri apiUri = EliteApiUri.GetCommunityGoalsUri( _project, _languageCode );

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
