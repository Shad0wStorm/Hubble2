//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! CreateAccountPost, attempts to create an account
//
//! Author:     Alan MacAree
//! Created:    19 Nov 2022
//----------------------------------------------------------------------

using ClientSupport;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FORCServerSupport.PutsAndPosts
{
    /// <summary>
    /// Attempts to create an account, once created, it needs to
    /// be confirmed via a verification API call.
    /// </summary>
    class CreateAccountPost : JSONWebPutAndPost
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_forcServerConnection">Used to create logs</param>
        /// <param name="_userDetails">The users details, used to create logs</param>
        public CreateAccountPost( FORCServerConnection _forcServerConnection,
                                  UserDetails _userDetails ) :
            base( _forcServerConnection, _userDetails )
        {
        }

        /// <summary>
        /// Runs the query to attempt to create a new FD account
        /// </summary>
        /// <param name="_state">The server state</param>
        /// <param name="_firstName">the users first name</param>
        /// <param name="_lastName">the users last name</param>
        /// <param name="_email">the users email</param>
        /// <param name="_password">the users password</param>
        /// <param name="_passwordConfirm">the users conformation password</param>
        /// <param name="_didTheCallSucceed">set to true if the call succeeded</param>
        /// <param name="_newsAndPromoSignUp">does the user signup for news and promotions</param>
        /// <returns></returns>
        public JSONWebPutsAndPostsResult Run( FORCAuthorisationManager auth,
                                      FORCServerState _state,
                                      string _firstName,
                                      string _lastName,
                                      string _email,
                                      string _password,
                                      string _passwordConfirm,
                                      bool _newsAndPromoSignUp )
        {
            JSONWebPutsAndPostsResult jsonNWebPostResult = new JSONWebPutsAndPostsResult();

            Debug.Assert( !string.IsNullOrWhiteSpace( _firstName ) );
            Debug.Assert( !string.IsNullOrWhiteSpace( _lastName ) );
            Debug.Assert( !string.IsNullOrWhiteSpace( _email ) );
            Debug.Assert( !string.IsNullOrWhiteSpace( _password ) );
            Debug.Assert( !string.IsNullOrWhiteSpace( _passwordConfirm ) );

            // Make sure we have some form of valid data before we try this.
            if ( !string.IsNullOrWhiteSpace( _firstName ) &&
                 !string.IsNullOrWhiteSpace( _lastName ) &&
                 !string.IsNullOrWhiteSpace( _email ) &&
                 !string.IsNullOrWhiteSpace( _password ) &&
                 !string.IsNullOrWhiteSpace( _passwordConfirm ) )
            {
                // Create a Dictionary and use that to create a json string
                Dictionary<string, object> dictionary = new Dictionary<string, object>();
                dictionary.Add( c_emailField, _email );
                dictionary.Add( c_firstNameField, _firstName );
                dictionary.Add( c_lastNameField, _lastName );
                dictionary.Add( c_passwordField, _password );
                dictionary.Add( c_passwordConfirmField, _passwordConfirm );


                dictionary.Add( c_newletterField, _newsAndPromoSignUp );

                dictionary.Add( c_termsField, true );

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
        private const string c_subUri = "user/frontier/registration";

        /// <summary>
        /// The email field name
        /// </summary>
        private const string c_emailField= "email";

        /// <summary>
        /// The password field name
        /// </summary>
        private const string c_passwordField = "password";

        /// <summary>
        /// The password confirm field name
        /// </summary>
        private const string c_passwordConfirmField = "password_confirm";

        /// <summary>
        /// The first name field name
        /// </summary>
        private const string c_firstNameField = "firstname";

        /// <summary>
        /// The lastname field name
        /// </summary>
        private const string c_lastNameField = "lastname";

        /// <summary>
        /// The newsletter (and promotions) field name.
        /// Pass true or false for this field
        /// </summary>
        private const string c_newletterField = "newsletter";

        /// <summary>
        /// The terms field name.
        /// Must be set to true
        /// </summary>
        private const string c_termsField = "terms";

        /// <summary>
        /// Used to pass true values within the server call
        /// </summary>
        private const string c_true = "true";

        /// <summary>
        /// Used to pass false values within the server call
        /// </summary>
        private const string c_false = "false";
    }
}

