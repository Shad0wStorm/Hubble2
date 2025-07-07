//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! RedeemCodePost, redeems a purchased code
//
//! Author:     Alan MacAree
//! Created:    15 Nov 2022
//----------------------------------------------------------------------


using ClientSupport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FORCServerSupport.PutsAndPosts
{
    class RedeemCodePost : JSONWebPutAndPost
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="_forcServerConnection">Used to create logs</param>
        /// <param name="_userDetails">The users details, used to create logs</param>
        public RedeemCodePost( FORCServerConnection _forcServerConnection,
                                  UserDetails _userDetails ) :
            base( _forcServerConnection, _userDetails )
        {
        }

        /// <summary>
        /// Runs the query to redeem a code
        /// </summary>
        /// <param name="_state">The server state</param>
        /// <param name="_machine">The machine id interface</param>
        /// <param name="_code">The code to redeem</param>
        /// <returns>The result of the redeem code</returns>
        public JSONWebPutsAndPostsResult Run( FORCServerState _state,
                                              MachineIdentifierInterface _machine,
                                              UserDetails user,
                                              string _code )
        {
            JSONWebPutsAndPostsResult jsonNWebPostResult = new JSONWebPutsAndPostsResult();

            Debug.Assert( _state != null );
            Debug.Assert( _machine != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( _code ) );

            // Make sure we have some form of valid data before we try this.
            if ( _state != null &&
                 _machine != null &&
                 !string.IsNullOrWhiteSpace( _code ) )
            {
                AddParameter( FORCServerState.c_machineIdQuery, _machine.GetMachineIdentifier() );

                String authHeader = "bearer " + user.SessionToken;

                if ( !String.IsNullOrEmpty( _state.Language ) )
                {
                    AddParameter( FORCServerState.c_language, _state.Language );
                }
                if ( _state.m_manager.IsSteam )
                {
                    //AddParameter(FORCServerState.c_steam, "true");
                }
                if ( _state.m_manager.OculusEnabled )
                {
                    //AddParameter(FORCServerState.c_oculus, "true");
                }
                switch ( user.AuthenticationType )
                {
                    case ServerInterface.AuthenticationType.Epic:
                        {
                            authHeader = "epic " + user.EpicAccessToken;
                            AddHeader( FORCServerState.c_headerAuthToken, authHeader );
                            break;
                        }
                    case ServerInterface.AuthenticationType.Steam:
                        {
                            AddHeader( FORCServerState.c_headerAuthToken, authHeader );
                            break;
                        }
                    case ServerInterface.AuthenticationType.FORC:
                    default:
                        {
                            AddParameter( FORCServerState.c_authToken, user.SessionToken );
                            AddParameter( FORCServerState.c_machineToken, user.AuthenticationToken );
                            break;
                        }
                }

                // Create a Dictionary and use that to create a json string
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary.Add( c_keyField, _code );

                // Create the json string
                string jsonString = DictionaryToJsonString( dictionary );

                // Uri to use.
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
        private const string c_subUri = "user/thirdparty/redeem";

        /// <summary>
        /// The password json fild name
        /// </summary>
        private const String c_keyField = "key";
    }

}

