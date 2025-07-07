//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! FORCServerSupport, requests product specfic server status
//
//! Author:     Alan MacAree
//! Created:    21 Dec 2022
//----------------------------------------------------------------------

using ClientSupport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FORCServerSupport.Queries
{
    class ServerStatusQuery : JSONWebQuery
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ServerStatusQuery()
            : base( "", true )
        {
        }

        /// <summary>
        /// Runs the Query to get the server status
        /// </summary>
        /// <param name="_state">The FORCServerState</param>
        /// <param name="_project">The project to get the status for</param>
        /// <param name="_serverStatusText">An out parameter, the server status text in the current language, can be null</param>
        /// <param name="_serverMessage">An out parameter, server state message in the current language, can be null</param>
        /// <returns>An ID representing the server state, -1 = NOT OKAY, 0 = Maintainance, 1 = OK</returns>
        public int Run( FORCServerState _state, 
                        Project _project, 
                        out string _serverStatusText, 
                        out string _serverMessage  )
        {
            int serverState = -1;
            _serverMessage = null;
            _serverStatusText = null;

            Debug.Assert( _project != null );
            Debug.Assert( _state != null );

            if ( _state != null )
            {
                if ( !String.IsNullOrEmpty( _state.Language ) )
                {
                    AddParameter( FORCServerState.c_language, _state.Language );
                }
            }

            if ( _project != null )
            {
                AddParameter( c_productNameParameter, _project.Name );
            }

            Uri apiUri = new Uri( c_serverStatusURI );
            Debug.Assert( apiUri != null && !string.IsNullOrWhiteSpace( apiUri.ToString() ) );

            if ( apiUri != null && !string.IsNullOrWhiteSpace( apiUri.ToString() ) )
            {
                Dictionary<String, object> responseDictionary = new Dictionary<string, object>();
                String message = null;
                HttpStatusCode httpResult = Execute( apiUri, out responseDictionary, out message );
                _serverStatusText = GetResponseText( responseDictionary, c_serverStatusMsgKey, "" );
                serverState = GetResponseInt( responseDictionary, c_serverStatusKey, -1 );
                _serverMessage = GetResponseText( responseDictionary, c_serverMessageKey, "" );
            }

            return serverState;
        }

        /// <summary>
        /// The server URI for the query
        /// </summary>
        private const string c_serverStatusURI = "https://ed-server-status.orerve.net";

        /// <summary>
        /// The key into the serevr status
        /// </summary>
        private const string c_serverStatusKey = "code";

        /// <summary>
        /// The key into the serevr status message
        /// </summary>
        private const string c_serverStatusMsgKey = "status";

        /// <summary>
        /// The key into the 
        /// </summary>
        private const string c_serverMessageKey = "message";

        /// <summary>
        /// The product name parameter string
        /// </summary>
        private const string c_productNameParameter = "product";
    }
}
