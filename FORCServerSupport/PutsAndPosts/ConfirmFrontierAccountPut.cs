//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ConfirmFrontierAccountPut, Confirms an account creation
//
//! Author:     Alan MacAree
//! Created:    25 Nov 2022
//----------------------------------------------------------------------

using ClientSupport;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FORCServerSupport.PutsAndPosts
{
    class ConfirmFrontierAccountPut : JSONWebPutAndPost
    {
        public const String c_emailField= "email";
        public const String c_passwordField = "password";
        public const String c_otpField = "otp";
        public const String c_machineField = "machine_id";

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_forcServerConnection">Used to create logs</param>
        /// <param name="_userDetails">The users details, used to create logs</param>
        public ConfirmFrontierAccountPut( FORCServerConnection _forcServerConnection,
                                  UserDetails _userDetails ) :
            base( _forcServerConnection, _userDetails, JSONWebType.Put )
        {

        }
        /// <summary>
        /// Runs the query to atempt to create a new FD account
        /// </summary>
        /// <param name="_state">The server state</param>
        /// <param name="_firstName">the users first name</param>
        /// <param name="_lastName">the users last name</param>
        /// <param name="_email">the users email</param>
        /// <param name="_password">the users password</param>
        /// <param name="_passwordConfirm">the users conformation password</param>
        /// <param name="_didTheCallSucceed">set to true if the call succeeded</param>
        /// <returns></returns>
        public JSONWebPutsAndPostsResult Run( FORCAuthorisationManager auth,
                                              FORCServerState _state,
                                              string _email,
                                              string _password,
                                              string _otp,
                                              string _machineId )
        {
            JSONWebPutsAndPostsResult jsonNWebPostResult = new JSONWebPutsAndPostsResult();

            Debug.Assert( !string.IsNullOrWhiteSpace( _email ) );
            Debug.Assert( !string.IsNullOrWhiteSpace( _password ) );
            Debug.Assert( !string.IsNullOrWhiteSpace( _otp ) );
            Debug.Assert( !string.IsNullOrWhiteSpace( _machineId ) );

            // Make sure we have some form of valid data before we try this.
            if ( !string.IsNullOrWhiteSpace( _email ) &&
                 !string.IsNullOrWhiteSpace( _password ) &&
                 !string.IsNullOrWhiteSpace( _otp ) &&
                 !string.IsNullOrWhiteSpace( _machineId ) )
            {
                // Create a Dictionary and use that to create a json string
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                dictionary.Add( c_emailField, _email );
                dictionary.Add( c_passwordField, _password );
                dictionary.Add( c_otpField, _otp );
                dictionary.Add( c_machineField, _machineId );

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
        private const string c_subUri = "user/frontier/registration/confirm";
    }

}

