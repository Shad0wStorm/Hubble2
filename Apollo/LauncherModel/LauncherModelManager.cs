//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! LauncherModel, used to provide data for the launcher UI
//
//! Author:     Alan MacAree
//! Created:    18 Oct 2022
//----------------------------------------------------------------------

using CBViewModel;
using ClientSupport;
using JSONConverters;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace LauncherModel
{
    /// <summary>
    /// A model that wraps up the new launcher specific functionality.

    /// </summary>
    public class LauncherModelManager
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_cobraBayView"></param>
        public LauncherModelManager( CobraBayView _cobraBayView )
        {
            Debug.Assert( _cobraBayView != null );
            m_cobraBayView = _cobraBayView;
        }

        /// <summary>
        /// Returns the Dynamic Content class which gives access
        /// to dynamic content data.
        /// </summary>
        /// <returns></returns>
        public DynamicContentModel GetDynamicContent()
        {
            if ( m_dynamicContent == null )
            {
                m_dynamicContent = new DynamicContentModel( m_cobraBayView );
            }
            return m_dynamicContent;
        }

        /// <summary>
        /// Checks the password complexity and returns a PasswordScoreAndFeedback (along with a score of how
        /// good it is).
        /// </summary>
        /// <param name="_password">The password to check</param>
        /// <returns>A PasswordScoreAndFeedback to indicate the score of the password, can be null</returns>
        public PasswordScoreAndFeedback GetPasswordScore( string _password )
        {
            Debug.Assert( m_cobraBayView != null );

            PasswordScoreAndFeedback passwordScore = null;

            if ( m_cobraBayView != null )
            {
                FORCManager forcManager = m_cobraBayView.Manager();
                Debug.Assert( forcManager != null );

                if ( forcManager != null )
                {
                    JSONWebPutsAndPostsResult result = forcManager.CheckPasswordComplexity( _password );

                    if ( result != null )
                    {
                        switch ( result.HttpStatusResult )
                        {
                            case HttpStatusCode.OK:
                                {
                                    if ( !string.IsNullOrWhiteSpace( result.RawJsonResult ) )
                                    {
                                        passwordScore = JsonConverter.JsonToPasswordScoreAndFeedback( result.RawJsonResult, m_cobraBayView );
                                    }
                                    else
                                    {
                                        // We did not get back any json string
                                        LogEvent( MethodBase.GetCurrentMethod().Name, "Missing result.RawJsonResult" );
                                    }
                                    break;
                                }
                            case (HttpStatusCode)422:
                                {
                                    // Missing password
                                    LogEvent( MethodBase.GetCurrentMethod().Name, "Server reports Missing Password" );
                                    break;
                                }
                            default:
                                {
                                    // Catch any unexpected problems
                                    LogEvent( MethodBase.GetCurrentMethod().Name, string.Format( "Unexpected HttpStatusResult from server {0}", result.HttpStatusResult ) );
                                    break;
                                }
                        }
                    }
                    else
                    {
                        // We did not get a JSONWebPutsAndPostsResult
                        LogEvent( MethodBase.GetCurrentMethod().Name, "JSONWebPutsAndPostsResult is null" );
                    }
                }
                else
                {
                    // forcManager is null
                    LogEvent( MethodBase.GetCurrentMethod().Name, "forcManager is null" );
                }
            }
            else
            {
                // m_cobraBayView is null
                LogEvent( MethodBase.GetCurrentMethod().Name, "m_cobraBayView is null" );
            }

            return passwordScore;
        }

        /// <summary>
        /// Logs an Event, adds the project and current user details.
        /// </summary>
        /// <param name="_methodName">The method name to log</param>
        /// <param name="_cobraBayView">The CobraBayView object used to log the event</param>
        /// <param name="_issue">The issue to log</param>
        private void LogEvent( string _methodName, string _issue )
        {
            Debug.Assert( _methodName != null );
            Debug.Assert( m_cobraBayView != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( _issue ) );

            LogEntry logEntry = new LogEntry( MethodBase.GetCurrentMethod().DeclaringType + c_classMethodSeparator + _methodName );

            if ( m_cobraBayView != null )
            {
                if ( !string.IsNullOrWhiteSpace( _issue ) )
                {
                    logEntry.AddValue( "Issue", _issue );
                }

                FORCManager forcManager = m_cobraBayView.Manager();
                Debug.Assert( forcManager != null );
                if ( forcManager != null )
                {
                    ServerInterface serverInterface = forcManager.ServerConnection;
                    Debug.Assert( serverInterface != null );
                    if ( serverInterface != null )
                    {
                        serverInterface.LogValues( forcManager.UserDetails, logEntry );
                    }
                }
            }
        }

        /// <summary>
        /// The DynamicContentModel that is created and return for clients
        /// to use to gain access to dynamic content information
        /// </summary>
        private DynamicContentModel m_dynamicContent = null;

        /// <summary>
        /// The CobraBayView model used to gain access to the server classes
        /// in order to get the dynamic data form the server APIs.
        /// </summary>
        private CobraBayView m_cobraBayView = null;

        /// <summary>
        /// The separator used between class and method name
        /// </summary>
        private static string c_classMethodSeparator = ".";

    }
}
