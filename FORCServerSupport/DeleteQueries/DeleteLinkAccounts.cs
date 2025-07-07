//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! AtemptCreateAccountQuery, attempts to create an account
//
//! Author:     Alan MacAree
//! Created:    19 Nov 2022
//----------------------------------------------------------------------

using ClientSupport;
using System;

namespace FORCServerSupport.DeleteQueries
{
    /// <summary>
    /// Deletes the link that associates a store account with an FD user account.
    /// </summary>
    class DeleteLinkAccounts : JSONWebPutAndPost
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_forcServerConnection">Used to create logs</param>
        /// <param name="_userDetails">User details used to create logs</param>
        public DeleteLinkAccounts( FORCServerConnection _forcServerConnection,
                                   UserDetails _userDetails ) : 
            base( _forcServerConnection, _userDetails, JSONWebPutAndPost.JSONWebType.Delete )
        {
        }

        /// <summary>
        /// Runs the query to atempt to delete the link that associates a store account 
        /// with an FD user account.
        /// </summary>
        /// <param name="_state">The server state</param>

        /// <returns></returns>
        public JSONWebPutsAndPostsResult Run( FORCServerState _state,
                                              string _storeAuthorisation,
                                              string _storeClientId,
                                              LinkStoreAccountPost _linkStoreAccountPost )
        {
            JSONWebPutsAndPostsResult jsonNWebPostResult = new JSONWebPutsAndPostsResult();
            string storePrefix = GetStorePrefix(_linkStoreAccountPost);

            switch( _linkStoreAccountPost )
            {
                case LinkStoreAccountPost.Steam:
                    AddHeader( c_platformField, c_platformSteamName );
                    break;
                case LinkStoreAccountPost.Epic:
                    AddHeader( c_platformField, c_platformEpicName );
                    break;
            }

            string prefixedAuthorisation = storePrefix + _storeAuthorisation;

            AddHeader( c_authorisationField, prefixedAuthorisation );

            // We only add a client ID if we have one to add
            if ( !string.IsNullOrWhiteSpace( _storeClientId ) )
            {
                AddHeader( c_clientidField, _storeClientId );
            }

            // Production Uri to use.
            Uri serverUri = new Uri( _state.GetServerAPI( FORCServerState.APIVersion.V3_0 ), c_subUri );

            jsonNWebPostResult = ExecutePost( serverUri, "" );


            return jsonNWebPostResult;
        }

        /// <summary>
        /// The sub part of the server uri
        /// </summary>
        private const string c_subUri = "user/frontier/unlink";

        private const string c_platformField = "platform";
        private const string c_platformSteamName = "steam";
        private const string c_platformEpicName = "epic";

        /// <summary>
        /// The authorisation header field name
        /// </summary>
        private const string c_authorisationField = "Authorization";

        /// <summary>
        /// The clientid header field name
        /// </summary>
        private const string c_clientidField = "clientid";
    }

}

