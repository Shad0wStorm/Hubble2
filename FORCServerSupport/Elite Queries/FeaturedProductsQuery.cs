//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! FeaturedProductsQuery, requests featured livery products from Elite API.
//
//! Created:    13 Oct 2022
//----------------------------------------------------------------------

using ClientSupport;
using System;
using System.Diagnostics;
using System.Net;

namespace FORCServerSupport.Elite_Queries
{
    /// <summary>
    /// JSONWebQuery that requests Featured Products from the server
    /// </summary>
    public class FeaturedProductsQuery : JSONWebQuery
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public FeaturedProductsQuery()
            : base( "", true )
        {
        }

        /// <summary>
        /// Runs the Query to get Livery
        /// </summary>
        /// <param name="_project">The project to get the featured products for</param>
        /// <param name="_languageCode">The language code to get the featured products for</param>
        /// <returns>The Json string containing the reply from the server</returns>
        public string Run( Project _project, string _languageCode )
        {
            string serverResponse = null;

            Debug.Assert( _project != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( _languageCode ) );

            if ( _project != null && !string.IsNullOrWhiteSpace( _languageCode ) )
            {
                Uri apiUri = EliteApiUri.GetLiveryUri( _project, _languageCode );

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

