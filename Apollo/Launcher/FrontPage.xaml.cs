//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
// FrontPage, this is the main page we display after the user has 
// kogged in.
//
// Author:     Alan MacAree
// Created:    19 Aug 2022
//----------------------------------------------------------------------

using CBViewModel;
using LauncherModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for FrontPage.xaml
    /// </summary>
    public partial class FrontPage : Page
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_launcherWindow">The launcher Window</param>
        /// <param name="_launcherModelManager">The Launcher Model Manager to use</param>
        public FrontPage( LauncherWindow _launcherWindow, LauncherModelManager _launcherModelManager )
        {
            Debug.Assert( _launcherWindow != null );
            Debug.Assert( _launcherModelManager != null );

            m_launcherWindow = _launcherWindow;
            m_launcherModelManager = _launcherModelManager;

            InitializeComponent();
        }

        /// <summary>
        /// Called once the window has been initialised
        /// </summaryexcel
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnInitialised( object sender, EventArgs e )
        {
            PART_ProductUserCtrl.SetFrontPage( this );
            SetupUIControl();
        }

        /// <summary>
        /// Waits for any UI Updates
        /// </summary>
        public void WaitForAnyUIUpdates()
        {
            PART_DynContentUserCtrl.WaitForAnyUIUpdates();
        }

        /// <summary>
        /// Sets up the UI controls on this page
        /// </summary>
        private void SetupUIControl()
        {
            if ( m_launcherModelManager != null )
            {
                DynamicContentModel dynamicContentModel = m_launcherModelManager.GetDynamicContent();
                Debug.Assert( dynamicContentModel != null );
                if ( dynamicContentModel != null )
                {
                    PART_DynContentUserCtrl.SetupObject( this, dynamicContentModel );
                    CobraBayView cobraBayView = m_launcherWindow.GetCobraBayView();
                    if ( cobraBayView != null )
                    {
                        ClientSupport.Project activeProject = cobraBayView.GetActiveProject();
                        PART_DynContentUserCtrl.SetProduct(activeProject);
                        // only for Epic and only for legacy
                        if (cobraBayView.IsEpic() && (activeProject.Name == "FORC-FDEV-D-1010" || activeProject.Name == "FORC-FDEV-D-1013"))
                        {
                            PART_DynContentUserCtrl.PART_HeroImageUserCtrl.DisplayServerWarning("Legacy is deprecated", "Greetings Commander,\n\nYou are about to launch the legacy version of Elite Dangerous.If you'd like to play the up-to-date and most active version of the game, please download the live version from the launcher, selecting \"Versions\" and then choosing \"Elite Dangerous: Horizons\" or \"Elite Dangerous: Odyssey\" accordingly.\n");
                            PART_DynContentUserCtrl.PART_HeroImageUserCtrl.PersistentWarning = true;
                        }
                        else
                        {
                            PART_DynContentUserCtrl.PART_HeroImageUserCtrl.PersistentWarning = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the dynamic ctrl with the server status
        /// </summary>
        /// <param name="_serverStatusState">The server status</param>
        /// <param name="_serverStatusText">The server status text</param>
        /// <param name="_serverStatusMessage">The server status message</param>
        public void ServerStatusUpdate( InfoUserCtrl.ServerStatusState _serverStatusState, string _serverStatusText, string _serverStatusMessage )
        {
            PART_DynContentUserCtrl.ServerStatusUpdate( _serverStatusState, _serverStatusText, _serverStatusMessage );
        }

        /// <summary>
        /// Update the displayed project
        /// </summary>
        /// <returns>A Task, this can be ignored</returns>
        public async Task UpdateDisplayedProjectAsync()
        {
            await Task.Run( () =>
            {
                if ( m_launcherModelManager != null )
                {
                    CobraBayView cobraBayView = m_launcherWindow.GetCobraBayView();
                    if ( cobraBayView != null )
                    {
                        Dispatcher.Invoke( new Action( () =>
                        {
                            PART_ProductUserCtrl.Update();
                            ClientSupport.Project activeProject = cobraBayView.GetActiveProject();
                            PART_DynContentUserCtrl.SetProduct( activeProject );
                            // only for Epic and only for legacy
                            if (cobraBayView.IsEpic() && (activeProject.Name == "FORC-FDEV-D-1010" || activeProject.Name == "FORC-FDEV-D-1013"))
                            {
                                PART_DynContentUserCtrl.PART_HeroImageUserCtrl.DisplayServerWarning("Legacy is deprecated", "Greetings Commander,\n\nYou are about to launch the legacy version of Elite Dangerous.If you'd like to play the up-to-date and most active version of the game, please download the live version from the launcher, selecting \"Versions\" and then choosing \"Elite Dangerous: Horizons\" or \"Elite Dangerous: Odyssey\" accordingly.\n");
                                PART_DynContentUserCtrl.PART_HeroImageUserCtrl.PersistentWarning = true;
                            }
                            else
                            {
                                // normal flow
                                PART_DynContentUserCtrl.PART_HeroImageUserCtrl.PersistentWarning = false;
                                m_launcherWindow.UpdateServerStatusOnScreen();
                            }
                        } ) );
                    }

                }
            } );
        }

        /// <summary>
        /// Returns the ProductUserCtrl
        /// </summary>
        /// <returns>returns the ProductUserCtrl, this can be null</returns>
        public ProductUserCtrl GetProductUserCtrl()
        {
            return PART_ProductUserCtrl;
        }

        /// <summary>
        /// Returns the LauncherWindow
        /// </summary>
        /// <returns>Returns the LauncherWindow</returns>
        internal LauncherWindow GetLauncherWindow()
        {
            Debug.Assert( m_launcherWindow != null );
            return m_launcherWindow;
        }

        /// <summary>
        /// Returns the LauncherModelManager
        /// </summary>
        /// <returns>Returns the LauncherModelManager</returns>
        public LauncherModelManager GetLauncherModelManager()
        {
            Debug.Assert(m_launcherModelManager != null);
            return m_launcherModelManager;
        }

        /// <summary>
        /// Our main Launcher Window
        /// </summary>
        private LauncherWindow m_launcherWindow;

        /// <summary>
        /// Provides access to the launchers model
        /// </summary>
        private LauncherModelManager m_launcherModelManager;

        /// <summary>
        /// A Task list that is waited on to complete  UI updates
        /// </summary>
        private List<Task> m_waitUIUpdateTaskList = new List<Task>();

        private void PART_ProductUserCtrl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {

        }
    }
}
