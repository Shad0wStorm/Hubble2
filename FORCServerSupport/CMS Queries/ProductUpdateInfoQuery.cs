//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ProductUpdateInfoQuery.
//
//! Author:     Alan MacAree
//! Created:    01 Nov 2022
//----------------------------------------------------------------------

using ClientSupport;
using System;
using System.Diagnostics;
using System.Net;

namespace FORCServerSupport.CMS_Queries
{
    /// <summary>
    /// Provides a JSONWebQuery for  Product Update Information
    /// </summary>
    public class ProductUpdateInfoQuery : JSONWebQuery
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public ProductUpdateInfoQuery()
            : base( "", true )
        {
        }

        /// <summary>
        /// Runs the Query to get the product update information
        /// </summary>
        /// <param name="_project">The project to get the product update information for</param>
        /// <param name="_languageCode">The language code to get the product update information in</param>
        /// <returns>The Json string containing the reply from the server</returns>
        public string Run( Project _project, string _languageCode )
        {
            string serverResponse = null;

            Debug.Assert( _project != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( _languageCode ) );

            if ( _project != null && !string.IsNullOrWhiteSpace( _languageCode ) )
            {
                Uri apiUri = CMSApiUri.GetProductUpdateUri( _project, _languageCode );

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
