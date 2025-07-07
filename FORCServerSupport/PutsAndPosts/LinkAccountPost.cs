//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! LinkAccountPost, Links accounts
//
//! Author:     Alan MacAree
//! Created:    29 Nov 2022
//----------------------------------------------------------------------

using ClientSupport;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FORCServerSupport.PutsAndPosts
{
    class LinkAccountPost : JSONWebPutAndPost
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_forcServerConnection">Used to create logs</param>
        /// <param name="_userDetails">The users details, used to create logs</param>
        public LinkAccountPost( FORCServerConnection _forcServerConnection,
                                  UserDetails _userDetails ) :
            base( _forcServerConnection, _userDetails )
        {
        }

        /// <summary>
        /// Attempts to link the steam account to the Frontier account
        /// </summary>
        /// <param name="_state"></param>
        /// <param name="_storeAuthorisation">The stores authorisations code</param>
        /// <param name="_storeClientId">The Steam client id</param>
        /// <param name="_email">The users email</param>
        /// <param name="_password">The users password</param>
        /// <param name="_linkStoreAccountPost">Which store are we doing this for</param>
        /// <param name="_overRideAnyExistingLinks">Should existing links be ignored? if not then
        /// the call can produce a HttpStatusCode.PreconditionFailed, meaning linked to
        /// another account..</param>
        /// <returns></returns>
        public JSONWebPutsAndPostsResult Run( FORCServerState _state,
                                              string _storeAuthorisation,
                                              string _storeClientId,
                                              string _email,
                                              string _password,
                                              LinkStoreAccountPost _linkStoreAccountPost,
                                              bool _overRideAnyExistingLinks )
        {
            JSONWebPutsAndPostsResult jsonNWebPostResult = new JSONWebPutsAndPostsResult();
            string storePrefix = GetStorePrefix(_linkStoreAccountPost);

            Debug.Assert( _state != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( _email ) );
            Debug.Assert( !string.IsNullOrWhiteSpace( _password ) );

            // Make sure we have some form of valid data before we try this.
            if ( _state != null &&
                 !string.IsNullOrWhiteSpace( _storeAuthorisation ) &&
                 !string.IsNullOrWhiteSpace( _email ) &&
                 !string.IsNullOrWhiteSpace( _password ) &&
                 !string.IsNullOrWhiteSpace( storePrefix ) )
            {
                string prefixedAuthorisation = storePrefix + _storeAuthorisation;

                AddHeader( c_authorisationField, prefixedAuthorisation );

                // We only add a client ID if we have one to add
                if ( !string.IsNullOrWhiteSpace( _storeClientId ) )
                {
                    AddHeader( c_clientidField, _storeClientId );
                }

                // Create a Dictionary and use that to create a json string
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                dictionary.Add( c_emailField, _email );
                dictionary.Add( c_passwordField, _password );
                dictionary.Add( c_forceLinkField, _overRideAnyExistingLinks );

                // Create the json string
                string jsonString = DictionaryToJsonString( dictionary );

                // Production Uri to use.
                Uri serverUri = new Uri( _state.GetServerAPI( FORCServerState.APIVersion.V3_0 ), c_subUri );

                // If we have a json string, then try and send it.
                if ( jsonString != null )
                {
                    jsonNWebPostResult = ExecutePost( serverUri, jsonString );
                }
            }

            return jsonNWebPostResult;
        }

        /// <summary>
        /// The sub part of the server uri
        /// </summary>
        private const string c_subUri = "user/frontier/link";

        /// <summary>
        /// The authorisation header field name
        /// </summary>
        private const string c_authorisationField = "Authorization";

        /// <summary>
        /// The clientid header field name
        /// </summary>
        private const string c_clientidField = "clientid";

        /// <summary>
        /// The email json field name
        /// </summary>
        private const string c_emailField = "email";

        /// <summary>
        /// The password json field name
        /// </summary>
        private const string c_passwordField = "password";

        /// <summary>
        /// The forceLink json field name
        /// </summary>
        private const string c_forceLinkField = "forceLink";
    }

}

