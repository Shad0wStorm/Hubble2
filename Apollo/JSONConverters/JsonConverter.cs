//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! JsonConverter, Converters JSON to a number of different classes
//
//! Author:     Alan MacAree
//! Created:    29 Sept 2022
//----------------------------------------------------------------------

using System;
using System.Text.Json;
using System.Diagnostics;
using System.Collections.Generic;
using ClientSupport;
using CBViewModel;
using System.Reflection;

namespace JSONConverters
{
    /// <summary>
    /// Converts JSON (from Elite API) to GalnetNews
    /// </summary>
    public class JsonConverter
    {
        /// <summary>
        /// Converts a JSON string into a GameDescription
        /// sub classes.
        /// </summary>
        /// <param name="_jsonString">The JSON string to convert</param>
        /// <param name="_cobraBayView">The CobraBayView object</param>
        /// <returns>The resulting GameDescription, this can be null</returns>
        public static GameDescription JsonToGameDescription( string _jsonString, CobraBayView _cobraBayView )
        {
            GameDescription gameDescription = null;

            Debug.Assert( !string.IsNullOrWhiteSpace( _jsonString ) );
            Debug.Assert( _cobraBayView != null );

            if ( !string.IsNullOrWhiteSpace( _jsonString ) )
            {
                try
                {
                    gameDescription = JsonSerializer.Deserialize<GameDescription>( _jsonString );
                }
                catch ( Exception ex )
                {
                    LogExceptionAddProjectAndUser( MethodBase.GetCurrentMethod().Name, _cobraBayView, _jsonString, ex );
                    // We can't error out, so just return what we have
                    Debug.Assert( false );
                }
            }

            return gameDescription;
        }

        /// <summary>
        /// Converts a JSON string into a GalnetNews and its
        /// sub classes.
        /// </summary>
        /// <param name="_jsonString">The JSON string to convert</param>
        /// <param name="_cobraBayView">The CobraBayView object</param>
        /// <returns>The resulting GalnetNews, this can be null</returns>
        public static GalnetNews JsonToGalnetNews( string _jsonString, CobraBayView _cobraBayView )
        {
            GalnetNews galnetNews = null;

            Debug.Assert( !string.IsNullOrWhiteSpace( _jsonString ) );
            Debug.Assert( _cobraBayView != null );

            if ( !string.IsNullOrWhiteSpace( _jsonString ) )
            {
                try
                {
                    galnetNews = JsonSerializer.Deserialize<GalnetNews>( _jsonString );
                }
                catch ( Exception ex )
                {
                    LogExceptionAddProjectAndUser( MethodBase.GetCurrentMethod().Name, _cobraBayView, _jsonString, ex );
                    // We can't error out, so just return what we have
                    Debug.Assert( false );
                }
            }

            return galnetNews;
        }

        /// <summary>
        /// Converts a JSON string into a CommunityGoals and its
        /// sub classes.
        /// </summary>
        /// <param name="_jsonString">The JSON string to convert</param>
        /// <param name="_cobraBayView">The CobraBayView object</param>
        /// <returns>The resulting CommunityGoals, this can be null</returns>
        public static CommunityGoals JsonToCommunityGoals( string _jsonString, CobraBayView _cobraBayView )
        {
            CommunityGoals communityGoals = null;

            Debug.Assert( !string.IsNullOrWhiteSpace( _jsonString ) );

            if ( !string.IsNullOrWhiteSpace( _jsonString ) )
            {
                try
                {
                    communityGoals = JsonSerializer.Deserialize<CommunityGoals>( _jsonString );
                }
                catch ( Exception ex )
                {
                    LogExceptionAddProjectAndUser( MethodBase.GetCurrentMethod().Name, _cobraBayView, _jsonString, ex );
                    // We can't error out, so just return what we have
                    Debug.Assert( false );
                }
            }

            return communityGoals;
        }

        /// <summary>
        /// Converts a JSON string into a CommunityNews and its
        /// sub classes.
        /// </summary>
        /// <param name="_jsonString">The JSON string to convert</param>
        /// <param name="_cobraBayView">The CobraBayView object</param>
        /// <returns>The resulting CommunityNews, this can be null</returns>
        public static CommunityNews JsonToCommunityNews( string _jsonString, CobraBayView _cobraBayView )
        {
            CommunityNews communityNews = null;

            Debug.Assert( !string.IsNullOrWhiteSpace( _jsonString ) );

            if ( !string.IsNullOrWhiteSpace( _jsonString ) )
            {
                try
                {
                    communityNews = JsonSerializer.Deserialize<CommunityNews>( _jsonString );
                }
                catch ( Exception ex )
                {
                    LogExceptionAddProjectAndUser( MethodBase.GetCurrentMethod().Name, _cobraBayView, _jsonString, ex );
                    // We can't error out, so just return what we have
                    Debug.Assert( false );
                }
            }

            return communityNews;
        }

        /// <summary>
        /// Converts a JSON string into a FeaturedProducts and its
        /// sub classes.
        /// </summary>
        /// <param name="_jsonString">The JSON string to convert</param>
        /// <param name="_cobraBayView">The CobraBayView object</param>
        /// <returns>The resulting Featured Products, this can be null</returns>
        public static Featured JsonToFeaturedProducts( string _jsonString, CobraBayView _cobraBayView )
        {
            Featured featured = null;

            Debug.Assert( !string.IsNullOrWhiteSpace( _jsonString ) );

            if ( !string.IsNullOrWhiteSpace( _jsonString ) )
            {
                try
                {
                    featured = JsonSerializer.Deserialize<Featured>( _jsonString );
                }
                catch ( Exception ex )
                {
                    LogExceptionAddProjectAndUser( MethodBase.GetCurrentMethod().Name, _cobraBayView, _jsonString, ex );
                    // We can't error out, so just return what we have
                    Debug.Assert( false );
                }
            }

            return featured;
        }

        /// <summary>
        /// Converts a JSON string into a PasswordScoreAndFeedback and its
        /// sub classes.
        /// </summary>
        /// <param name="_jsonString">The JSON string to convert</param>
        /// <param name="_cobraBayView">The CobraBayView object</param>
        /// <returns>The resulting PasswordScoreAndFeedback, this can be null</returns>
        public static PasswordScoreAndFeedback JsonToPasswordScoreAndFeedback( string _jsonString, CobraBayView _cobraBayView )
        {
            PasswordScoreAndFeedback passwordScore = null;

            Debug.Assert( !string.IsNullOrWhiteSpace( _jsonString ) );

            if ( !string.IsNullOrWhiteSpace( _jsonString ) )
            {
                try
                {
                    passwordScore = JsonSerializer.Deserialize<PasswordScoreAndFeedback>( _jsonString );
                }
                catch ( Exception ex )
                {
                    LogExceptionAddProjectAndUser( MethodBase.GetCurrentMethod().Name, _cobraBayView, _jsonString, ex );
                    // We can't error out, so just return what we have
                    Debug.Assert( false );
                }
            }

            return passwordScore;
        }

        /// <summary>
        /// Converts a JSON string into a ProductUpdateInformation and its
        /// sub classes.
        /// </summary>
        /// <param name="_jsonString">The JSON string to convert</param>
        /// <param name="_cobraBayView">The CobraBayView object</param>
        /// <returns>The resulting ProductUpdateInformation, this can be null</returns>
        public static List<ProductUpdateInformation> JsonToProductUpdateInformation( string _jsonString, CobraBayView _cobraBayView )
        {
            List<ProductUpdateInformation> productUpdateInformationList = null;

            Debug.Assert( !string.IsNullOrWhiteSpace( _jsonString ) );

            if ( !string.IsNullOrWhiteSpace( _jsonString ) )
            {
                if ( _jsonString.CompareTo( c_noData ) != 0 )
                {
                    try
                    {
                        productUpdateInformationList = JsonSerializer.Deserialize< List<ProductUpdateInformation>>( _jsonString );
                    }
                    catch ( Exception ex )
                    {
                        LogExceptionAddProjectAndUser( MethodBase.GetCurrentMethod().Name, _cobraBayView, _jsonString, ex );
                        // We can't error out, so just return what we have
                        Debug.Assert( false );
                    }
                }
            }

            return productUpdateInformationList;
        }

        /// <summary>
        /// Logs an Event, adds the project and current user details.
        /// </summary>
        /// <param name="_methodName">The method name to log</param>
        /// <param name="_cobraBayView">The CobraBayView object used to log the event</param>
        /// <param name="_jsonString">The json string that caused the issue</param>
        /// <param name="_ex">The exception to log</param>
        private static void LogExceptionAddProjectAndUser( string _methodName, CobraBayView _cobraBayView, string _jsonString, Exception _ex )
        {
            Debug.Assert( _methodName != null );
            Debug.Assert( _cobraBayView != null );
            Debug.Assert( _ex != null );

            LogEntry logEntry = new LogEntry( MethodBase.GetCurrentMethod().DeclaringType + c_classMethodSeparator + _methodName );
            logEntry.AddValue( "Invalid json", _jsonString );
            logEntry.AddValue( "Exception caught", _ex.ToString() );

            if ( _cobraBayView != null )
            {
                Project project = _cobraBayView.GetActiveProject();
                Debug.Assert( project != null );
                if ( project != null )
                {
                    logEntry.AddValue( "Project", project.Name );
                }

                FORCManager forcManager = _cobraBayView.Manager();
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
        /// The servers sometimes return the following, meaning no data
        /// </summary>
        private static string c_noData = "[]";

        /// <summary>
        /// The separator used between class and method name
        /// </summary>
        private static string c_classMethodSeparator = ".";
    }
}
