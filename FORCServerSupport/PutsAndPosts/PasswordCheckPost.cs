//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! PasswordCheckPost, calls the server to check if a password
//! passes Frontier's password rules.
//
//! Author:     Alan MacAree
//! Created:    28 Nov 2022
//----------------------------------------------------------------------

using ClientSupport;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FORCServerSupport.PutsAndPosts
{
    class PasswordCheckPost : JSONWebPutAndPost
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_forcServerConnection">Used to create logs</param>
        /// <param name="_userDetails">The users details, used to create logs</param>
        public PasswordCheckPost( FORCServerConnection _forcServerConnection,
                                  UserDetails _userDetails ) : 
            base( _forcServerConnection, _userDetails )
        {
        }

        /// <summary>
        /// Runs the query to atempt to create a new FD account
        /// </summary>
        /// <param name="_state">The server state</param>
        /// <param name="_password">the password to check</param>
        /// <returns>The result of the password rule check</returns>
        public JSONWebPutsAndPostsResult Run( FORCServerState _state,
                                              string _password )
        {
            JSONWebPutsAndPostsResult jsonNWebPostResult = new JSONWebPutsAndPostsResult();

            Debug.Assert( _state != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( _password ) );

            // Make sure we have some form of valid data before we try this.
            if ( _state != null &&
                 !string.IsNullOrWhiteSpace( _password ) )
            {
                // Create a Dictionary and use that to create a json string
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary.Add( c_passwordField, _password );

                // Create the json string
                string jsonString = DictionaryToJsonString( dictionary );

#if DEVELOPMENT || DEBUG

                // Development Uri to use.
                Uri serverUri = new Uri( "https://staging-api.zaonce.net/3.0/user/frontier/passwordcheck" );
#else
                // Production Uri to use.
                Uri serverUri = new Uri( _state.GetServerAPI( FORCServerState.APIVersion.V3_0 ), c_subUri );
#endif

                // If we have a json string, then try and send it.
                if ( jsonString != null )
                {
                    // Do not decode the json here, it is too complex and must be decoded
                    // using the JsonConverters.
                    bool decodeJson = false;
                    jsonNWebPostResult = ExecutePost( serverUri, jsonString, decodeJson );
                }
            }

            return jsonNWebPostResult;
        }

        /// <summary>
        /// The sub part of the server uri
        /// </summary>
        private const string c_subUri = "user/frontier/passwordcheck";

        /// <summary>
        /// The password json fild name
        /// </summary>
        private const String c_passwordField = "password";
    }

}

