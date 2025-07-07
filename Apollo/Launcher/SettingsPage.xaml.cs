//----------------------------------------------------------------------
// Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
// SettingsPage, this displays the settings that a user can select
// to change. The left side of the settings page is static, the right
// side changes depending on the settings selected on the left side.
//
// Author:     Alan MacAree
// Created:    19 Aug 2022
//----------------------------------------------------------------------

using FDUserControls;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using CBViewModel;
using ClientSupport;

namespace Launcher
{
    /// Specify our LanguageDictionary
    using LanguageDictionary = Dictionary<string, string>;

    /// <summary>
    /// Interaction logic for SettingsPage.xaml
    /// </summary>
    public partial class SettingsPage : Page, ISettingsCtrlUI
    {
        /// <summary>
        /// Constrcutor
        /// </summary>
        /// <param name="_language">The LauncherWindow which provides access to the underlying settings</param>
        public SettingsPage( LauncherWindow _launcherWindow )
        {
            InitializeComponent();
            Debug.Assert( _launcherWindow != null );

            m_launcherWindow = _launcherWindow;
            SettingsCtrl.RegisterInterface( this );

            // Force the Support User Ctrl to be displayed first on the right hand side
           OnSupportBtnClicked();
        }



        /// <summary>
        /// Helper method to get the CobraBayView
        /// </summary>
        /// <returns>The CobraBayView</returns>
        private CobraBayView GetCobraBayView()
        {
            CobraBayView cobraBayView = null;
            Debug.Assert( m_launcherWindow != null );

            if ( m_launcherWindow != null )
            {
                cobraBayView = m_launcherWindow.GetCobraBayView();
                Debug.Assert( cobraBayView != null );
            }

            return cobraBayView;
        }

        /// <summary>
        /// Returns the current users name.
        /// </summary>
        /// <returns>Users name or a blank string</returns>
        public string GetUsersName()
        {
            string usersName = "";
            CobraBayView cobraBayView = GetCobraBayView();

            Debug.Assert( cobraBayView != null );

            if ( cobraBayView != null )
            {
                FORCManager manager = cobraBayView.Manager();
                if ( manager != null )
                {
                    UserDetails userDetails = manager.UserDetails;
                    if ( userDetails != null )
                    {
                        usersName = userDetails.RegisteredName;
                    }
                }
            }

            return usersName;
        }

        /// <summary>
        /// Returns the current users email address
        /// </summary>
        /// <returns>Users email address or a blank string</returns>
        public string GetUsersEmail()
        {
            string usersEmail = "";
            CobraBayView cobraBayView = GetCobraBayView();

            Debug.Assert( cobraBayView != null );
            if ( cobraBayView != null )
            {
                if ( cobraBayView != null )
                {
                    FORCManager manager = cobraBayView.Manager();
                    if ( manager != null )
                    {
                        UserDetails userDetails = manager.UserDetails;
                        if ( userDetails != null )
                        {
                            usersEmail = userDetails.EmailAddress;
                        }
                    }
                }
            }

            return usersEmail;
        }

        /// <summary>
        /// Back button clicked, displays the FrontPage.
        /// </summary>
        public void OnBackBtnClicked()
        {
            if ( m_launcherWindow != null )
            {
                _ = m_launcherWindow.DisplayFrontPageAsync();
            }
        }

        /// <summary>
        /// Frontier Links clicked, displays the Frontier links
        /// </summary>
        public void OnFrontierLinksBtnClicked()
        {
            FrontierLinksCtrl frontierLinksCtrl = new FrontierLinksCtrl();
            DynSettingsFrame.Content = frontierLinksCtrl;
        }

        /// <summary>
        /// Language Clicked, display the language ctrl and fills it
        /// with the available languages.
        /// </summary>
        public void OnLanguageBtnClicked()
        {
            LanguageUserCtrl languageUserCtrl = new LanguageUserCtrl();

            // Set the delegate so that we know if the language has changed and what it
            // has been changed to.
            languageUserCtrl.OnLanguageChange = OnLanguageChange;

            CobraBayView cobraBayView = GetCobraBayView();

            if ( cobraBayView != null 
                 && m_launcherWindow != null )
            {
                LanguageDictionary languageDictionary = LanguageHelper.GetLanguageDictionary( cobraBayView, m_launcherWindow );

                if ( languageDictionary != null )
                {
                    // Set the list of languages, plus pass the key of the one that should
                    // be selected.
                    languageUserCtrl.SetLanguageDictionary( languageDictionary, cobraBayView.LanguageOverride );
                }
            }

            // Display the LanguageCtrl that we just setup on the screen.
            DynSettingsFrame.Content = languageUserCtrl;
        }

        /// <summary>
        /// Called when the user selects a language to use.
        /// </summary>
        /// <param name="_languageCode">The language code that should not be used.</param>
        private void OnLanguageChange( string _languageCode )
        {
            if ( _languageCode == null )
            {
                _languageCode = "";
            }

            CobraBayView cobraBayView = GetCobraBayView();

            if ( cobraBayView  != null )
            {
                if ( cobraBayView.LanguageOverride != _languageCode )
                {
                    cobraBayView.LanguageOverride = _languageCode;
                    App app = Application.Current as App;
                    if ( app != null)
                    {
                        
                        // If we started via Epic, make sure we get a new Epic Token when restarting
                        if ( cobraBayView.m_manager.IsEpic )
                        {
                            app.RestartExtraArgs.Add( c_epicRefreshTokenArg );
                            //app.RestartExtraArgs.Add( cobraBayView.m_manager.GetRefreshToken() );
                        }
                        else
                        {
                            // until we get Epic's refresh token authentication working properly, automatically quit the launcher but do not restart it, instead leaving the player to launch it again from the Epic Store
                            app.Restart = true;
                        }

                        app.Shutdown();
                    }
                }
            }
        }

        /// <summary>
        /// Options Clicked, displays the options on the right side
        /// </summary>
        public void OnOptionsBtnClicked()
        {
            OptionsUserCtrl optionsUserCtrl = new OptionsUserCtrl( m_launcherWindow.GetCobraBayView() );
            DynSettingsFrame.Content = optionsUserCtrl;
        }

        /// <summary>
        /// Support Clicked, displays the support options on the right side
        /// </summary>
        public void OnSupportBtnClicked()
        {
            SupportUserCtrl supportUserCtrl = new SupportUserCtrl();
            DynSettingsFrame.Content = supportUserCtrl;
        }

        /// <summary>
        /// The LauncherWindow object
        /// </summary>
        private LauncherWindow m_launcherWindow;

        /// <summary>
        /// The Epic Arg that must be used when restarting if we were started via Epic
        /// </summary>
        private const string c_epicRefreshTokenArg = "/EpicRefreshToken";
    }
}
