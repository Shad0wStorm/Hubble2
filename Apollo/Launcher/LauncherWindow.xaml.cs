//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! Main Launcher Window, this uses user controls to provide product 
//! specific information and control.
//
//! Author:     Alan MacAree
//! Created:    12 Aug 2022
//----------------------------------------------------------------------

using CBViewModel;
using ClientSupport;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using FDUserControls;
using System.IO;
using System.Net;
using System.Reflection;
using LauncherModel;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Input;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for LauncherWindow.xaml
    /// </summary>
    public partial class LauncherWindow : Window, UserInterface, ILogEvent, IRetrieveFileFromUri
    {

        /// <summary>
        /// Used to make sure the user details are stored, this
        /// is tracked here because we have a number of routes
        /// where the user can login and hit remember me before
        /// we decide it is okay to remember the details.
        /// E.g. Does not own elite, does not link accounts etc
        /// </summary>
        public bool StoreUserDetails { private get; set; } = false;

        /// <summary>
        /// Called when the window is initialised
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnInitialised( object sender, EventArgs e )
        {
            Title = LocalResources.Properties.Resources.TITLE_ApplicationName;

            // Setup the PART_ServerStatusCtrl, m_cobraBayView can
            // be null, and that is okay
            PART_ServerStatusCtrl.SetUpObject( this, m_cobraBayView );

            // Disable the forward and back navigation, failing to do this
            // will allow the user to navigate between pages with no software
            // control and can cause hangs (because we are not expecting it).
            NavigationCommands.BrowseBack.InputGestures.Clear();
            NavigationCommands.BrowseForward.InputGestures.Clear();

            // Display the initial wait page, because the next process may take a while,
            // especially if we have been started via a 3rd party store.
            DisplayWaitPage();
        }

        /// <summary>
        /// Called when the window is shown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnShown( object sender, EventArgs e )
        {
#if DEBUG
            // Debug message used to be able to attach a debugger to the launcher at startup.
            MessageBox.Show( "Debug Interception Chance", "Elite Launcher", MessageBoxButton.OK );
#endif

            if ( !m_previouslyShown )
            {
                m_previouslyShown = true;

                // Create the model
                await CreatModelsAsync();

                // Create the UI
                await CreateUI();
            }
        }

        /// <summary>
        /// Logs the user out and forgets everything, unless
        /// we are logged in via a store, then just close the app, 
        /// otherwise the steam/epic login and linking may
        /// become compromised.
        /// </summary>
        /// <param name="_forgetUserDetails">Should the user details be forgotten, defaults to false</param>
        public async Task Logout( bool _forgetUserDetails = false )
        {
            if ( m_cobraBayView != null )
            {
                await Task.Run( () =>
                {
                    if ( _forgetUserDetails )
                    {
                        ForgetUsersDetails();
                    }

                    m_frontPage = null;
                } );

                // Create the model
                await CreatModelsAsync();

                // Special case
                // If we are Epic, then logout and don't allow them to return to the Login 
                // With Epic. This is because Epic will refuse the reattempt at using the 
                // Epic login token.
                if ( m_cobraBayView.IsEpic() )
                {
                    // We do this by clearing the command line parameters and starting
                    // the process of logging in again.
                    FORCManager forcManager = m_cobraBayView.Manager();
                    Debug.Assert( forcManager != null );
                    if ( forcManager != null )
                    {
                        m_cobraBayView.Manager().IgnoreEpic = true;
                    }
                }

                // Create the UI
                await CreateUI();
            }
        }

        /// <summary>
        /// Checks to see if the application can close, otherwise display a 
        /// message to the user to say we cannot close.
        /// </summary>
        /// <returns></returns>
        public bool CanIClose()
        {
            bool allowAppToClose = true;
            if ( m_cobraBayView != null )
            {
                if ( m_cobraBayView.Monitor != null )
                {
                    ErrorMessage( LocalResources.Properties.Resources.InstallationInProgress,
                                  LocalResources.Properties.Resources.InstallationInProgressTitle );
                    allowAppToClose = false;
                }
            }

            return allowAppToClose;
        }

        /// <summary>
        /// Create the Models
        /// </summary>
        /// <returns>A Task, this can be ignored</returns>
        private async Task CreatModelsAsync()
        {
            await Task.Run( () =>
            {
                // This can take a while...
                m_cobraBayView = new CobraBayView( this );
                m_cobraBayView.ResetManager();
                m_launcherModelManager = new LauncherModelManager( m_cobraBayView );
            } );

            Debug.Assert( m_cobraBayView != null );
            if ( m_cobraBayView != null )
            {
                FORCManager fORCManager = m_cobraBayView.Manager();
                Debug.Assert( fORCManager != null );
                if ( fORCManager != null )
                {
                    if ( fORCManager.IsSteam )
                    {
                        HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;
                        HwndTarget hwndTarget = hwndSource.CompositionTarget;
                        hwndTarget.RenderMode = RenderMode.SoftwareOnly;
                    }
                }
            }

            // Update the server information
            PART_ServerStatusCtrl.SetUpObject( this, m_cobraBayView );
        }

        /// <summary>
        /// Create the UI
        /// </summary>
        /// <returns>A Task, this can be ignored</returns>
        private async Task CreateUI()
        {
            FORCManager fORCManager = m_cobraBayView.Manager();

            Debug.Assert( fORCManager != null );

            // Check to see if we are using Oculus
            RequiresInteraction();

            // Keep an eye on power mode changes
            Microsoft.Win32.SystemEvents.PowerModeChanged += this.PowerModeChanged;

            // Check the status, we may have logged in automatically.
            if ( fORCManager != null )
            {
                if ( !fORCManager.Authorised )
                {
                    if ( fORCManager.IsSteam ||
                         fORCManager.IsEpic )
                    {
                        if ( fORCManager.UserDetails.SteamRegistrationLink != null ||
                             fORCManager.UserDetails.SteamLinkLink != null )
                        {
                            DisplayStoreFirstOpenUnlinked();
                        }
                        else
                        {
                            // We are not authorised, and we don't need to register/link the
                            // account, so why are we here?
                            if ( !fORCManager.UserDetails.SteamUnavailable )
                            {
                                if ( fORCManager.IsSteam )
                                {
                                    HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;
                                    HwndTarget hwndTarget = hwndSource.CompositionTarget;
                                    hwndTarget.RenderMode = RenderMode.SoftwareOnly;
                                }
                                await DisplayFrontPageAsync();
                            }
                        }
                    }
                    else
                    {
                        // We need the user to provide login details
                        bool displayAccountLinkChoice = false;
                        DisplayHomeLoginPage( displayAccountLinkChoice );
                    }
                }
                else
                {
                    if ( fORCManager.IsSteam ||
                         fORCManager.IsEpic )
                    {
                        if ( fORCManager.UserDetails.SteamRegistrationLink == null &&
                             fORCManager.UserDetails.SteamLinkLink == null )
                        {
                            if ( fORCManager.ActiveVRMode == FORCManager.VRMode.Enabled )
                            {
                                // If we are in VR mode, go to the front page
                                _ = DisplayFrontPageAsync();
                            }
                            else
                            {
                                // If we are not in VR mode, go to the StoreFirstOpenLinkedPage
                                DisplayStoreFirstOpenLinkedPage();
                            }
                        }
                    }
                    else
                    {
                        // Just go straight to front page as we are already authorised
                        await DisplayFrontPageAsync();
                    }
                }
            }

            CheckXInputDLL();
        }

        /// <summary>
        /// Test whether interaction is required from the user before starting
        /// the game.
        /// 
        /// This will trigger if the game was not started automatically.
        /// </summary>
        private void RequiresInteraction()
        {
            FORCManager fORCManager = m_cobraBayView.Manager();
            if ( fORCManager.OculusEnabled )
            {
                String message = null;
                if ( !fORCManager.Authorised )
                {
                    message = "login";
                }
                else
                {
                    Project p = m_cobraBayView.GetActiveProject();
                    if ( p != null )
                    {
                        if ( (p.Action == Project.ActionType.Update) ||
                            (p.Action == Project.ActionType.Install) )
                        {
                            message = "upgrade";
                        }
                    }
                }
                if ( message != null )
                {
                    Assembly an = Assembly.GetEntryAssembly();
                    String prompt = System.IO.Path.GetDirectoryName(an.Location);
                    prompt = System.IO.Path.Combine( prompt, "ORPrompt.exe" );
                    if ( System.IO.File.Exists( prompt ) )
                    {
                        Process.Start( prompt, message );
                    }
                }
            }
        }

        /// <summary>
        /// Check to see if we find the XInput DLL on the path 
        /// </summary>
        private void CheckXInputDLL()
        {
            if ( Properties.Settings.Default.DXCheck )
            {
                if ( m_cobraBayView != null )
                {
                    try
                    {
                        String paths = Environment.GetEnvironmentVariable( c_pathEnvironmentName );
                        String[] pathArray = paths.Split(';');
                        bool found = false;
                        for ( int pathIdx = 0; pathIdx < pathArray.Length && !found; pathIdx++ )
                        {
                            string path = pathArray[pathIdx];
                            if ( !string.IsNullOrWhiteSpace( path ) )
                            {
                                String xinputPath = System.IO.Path.Combine( path, c_xInputDLLFile );
                                found = System.IO.File.Exists( xinputPath );
                            }
                        }

                        if ( !found )
                        {
                            m_cobraBayView.OpenDirectXLink();
                        }
                    }
                    catch ( System.Exception )
                    {
                        // Don't do anything
                    }
                }
            }
        }

        /// <summary>
        /// Returns the CobraBayView object
        /// </summary>
        /// <returns>A CobraBayView</returns>
        public CobraBayView GetCobraBayView()
        {
            Debug.Assert( m_cobraBayView != null );
            return m_cobraBayView;
        }

        /// <summary>
        /// Updates the Launcher window mwith the server status
        /// </summary>
        /// <param name="_serverStatusState">The server status</param>
        /// <param name="_serverStatusText">The server status text</param>
        /// <param name="_serverStatusMessage">The server status message</param>
        public void ServerStatusUpdate( InfoUserCtrl.ServerStatusState _serverStatusState, string _serverStatusText, string _serverStatusMessage )
        {
            if ( m_frontPage != null )
            {
                m_frontPage.ServerStatusUpdate( _serverStatusState, _serverStatusText, _serverStatusMessage );
            }
        }

        /// <summary>
        /// Updates the server status on the screen via
        /// the InfoUserCtrl
        /// </summary>
        public void UpdateServerStatusOnScreen()
        {
            PART_ServerStatusCtrl.UpdateServerStatus();
        }

        /// <summary>
        /// Displays the HomeDoesNotOwnElitePage
        /// </summary>
        public void DisplayHomeDoesNotOwnElitePage()
        {
            MainFrame.Content = new HomeDoesNotOwnElitePage( this );
        }

        /// <summary>
        /// Displays the StoreFirstOpenUnlinkedPage
        /// </summary>
        public void DisplayStoreFirstOpenUnlinked()
        {
            MainFrame.Content = new StoreFirstOpenUnlinkedPage( this );
        }

        /// <summary>
        /// Displays the StoreFirstOpenLinkedPage
        /// </summary>m>
        public void DisplayStoreFirstOpenLinkedPage()
        {
            MainFrame.Content = new StoreFirstOpenLinkedPage( this );
        }

        /// <summary>
        /// Displays the HomeFirstOpenLinkingPage
        /// </summary>
        public void DisplayHomeFirstOpenLinkingPage()
        {
            MainFrame.Content = new HomeFirstOpenLinkingPage( this );
        }

        /// <summary>
        /// Displays the HomeFirstOpenUnlinkedPage
        /// </summary>
        public void DisplayHomeFirstOpenUnlinkedPage()
        {
            MainFrame.Content = new HomeFirstOpenUnlinkedPage( this );
        }

        /// <summary>
        /// Displays the HomeLinkedToAnotherStorePage
        /// </summary>
        public void DisplayHomeLinkedToAnotherStorePage()
        {
            MainFrame.Content = new HomeLinkedToAnotherStorePage( this );
        }

        /// <summary>
        /// Displays the home login page
        /// </summary>
        /// <param name="_displayAccountLinkChoice">Show the account linking option be displayed</param>
        /// <param name="_requestNewLogon">Forces the logon prcoess to ignore any auto logon via store, defaults to false</param>
        public void DisplayHomeLoginPage( bool _displayAccountLinkChoice, bool _requestNewLogon = false )
        {
            UpdateServerStatusOnScreen();

            MainFrame.Content = new HomeLoginPage( this, _displayAccountLinkChoice, _requestNewLogon );
        }

        /// <summary>
        /// Displays the FrontPage
        /// </summary>
        public async Task DisplayFrontPageAsync()
        {
            bool ownsAGame = true;

            // If we have the FrontPage, don't recreate it, because it
            // takes too long.
            if ( m_frontPage == null )
            {
                await Task.Run( () =>
                {
                    if ( m_frontPage == null )
                    {
                        Debug.Assert( m_cobraBayView != null );
                        if ( m_cobraBayView != null )
                        {
                            FORCManager fORCManager = m_cobraBayView.Manager();
                            Debug.Assert( fORCManager != null );
                            if ( fORCManager != null )
                            {
                                fORCManager.UpdateProjectList();
                                ownsAGame = !fORCManager.AvailableProjects.IsEmpty();
                            }
                        }
                    }
                } );

                if ( ownsAGame )
                {
                    m_frontPage = new FrontPage( this, m_launcherModelManager );

                    // Do we need to store the users details
                    if ( StoreUserDetails )
                    {
                        SaveUsersDetails();
                        StoreUserDetails = false;
                    }
                }
            }

            // If the user owns elite, then continue, otherwise display a page
            // allowing them to redeem a code (if they want).
            if ( ownsAGame )
            {
                UpdateServerStatusOnScreen();
                MainFrame.Content = m_frontPage;
            }
            else
            {
                DisplayHomeDoesNotOwnElitePage();
            }
        }

        /// <summary>
        /// Saves the user details, so that the user will not have to provide
        /// user details to login
        /// </summary>
        private void SaveUsersDetails()
        {
            Debug.Assert( m_cobraBayView != null );
            if ( m_cobraBayView != null )
            {
                FORCManager fORCManager = m_cobraBayView.Manager();
                Debug.Assert( fORCManager != null );
                if ( fORCManager != null )
                {
                    bool savePassword = true;
                    fORCManager.SaveUserDetails( savePassword );
                }
            }
        }

        /// <summary>
        /// Clears any saved user details, the user will need to provide login information 
        /// the next time the launcher is started.
        /// </summary>
        public void ForgetUsersDetails()
        {
            Debug.Assert( m_cobraBayView != null );
            if ( m_cobraBayView != null )
            {
                FORCManager fORCManager = m_cobraBayView.Manager();
                Debug.Assert( fORCManager != null );
                if ( fORCManager != null )
                {
                    fORCManager.ClearUserDetails();
                }
            }
        }

        /// <summary>
        /// Displays the home verification page
        /// </summary>
        /// <param name="_emailAddress">The users email address</param>
        /// <param name="_password">The users password</param>
        /// <param name="_pageToReturnTo">The page to return to, should the user wish to</param>
        /// <param name="_developerVerificationCode">An optional developer Verification code, can be null.</param>
        public void DisplayHomeVerificationPage( string _emailAddress,
                                                 string _password,
                                                 Page _pageToReturnTo,
                                                 string _developerVerificationCode = null )
        {
            MainFrame.Content = new HomeVerificationPage( this,
                                                          _emailAddress,
                                                          _password,
                                                          _pageToReturnTo,
                                                          _developerVerificationCode );
        }

        /// <summary>
        /// Displays a popup Page with:
        /// Title
        /// Main Message
        /// Sub Message
        /// 
        /// Navigation is returned to _previousPage
        /// </summary>
        /// <param name="_previousPage">The page to return to when the PopupPage closes</param>
        /// <param name="_title">The title to display</param>
        /// <param name="_mainMsg">The main message to display</param>
        /// <param name="_subMsg">The sub message to display</param>
        public void DisplayPopupPage( Page _previousPage, string _title, string _mainMsg, string _subMsg )
        {
            PopupPage popupPage = new PopupPage( _previousPage, _title, _mainMsg, _subMsg );

            MainFrame.Content = popupPage;
        }

        /// <summary>
        /// Displays the WaitPage
        /// </summary>
        public void DisplayWaitPage()
        {
            MainFrame.Content = new WaitPage();
        }

        /// <summary>
        /// Displays the SignUpPage.
        /// </summary>
        /// <param name="_displayAccountLinkChoice">Show the account linking option be displayed</param>
        public void DisplayHomeCreateAccountPage( bool _displayAccountLinkChoice )
        {
            MainFrame.Content = new HomeCreateAccountPage( this, _displayAccountLinkChoice );
        }

        /// <summary>
        /// Returns the OS Identity
        /// </summary>
        /// <returns></returns>
        public string GetOSIdent()
        {
            return ClientSupport.Utils.OSIdent.GetOSIdent();
        }

        /// <summary>
        /// Returns the UserInterface of the ProductUserCtrl
        /// </summary>
        /// <returns>UserInterface of the ProductUserCtrl, this can be null</returns>
        private UserInterface GetProductUserCtrlUI()
        {
            UserInterface ui = null;

            if ( MainFrame.Content != null )
            {
                if ( MainFrame.Content is FrontPage fp )
                {
                    ui = fp.GetProductUserCtrl() as UserInterface;
                }
            }

            return ui;
        }

        /// <summary>
        /// Passed onto the current ProductUserCtrl to update the product information
        /// </summary>
        public void MarkForUpdate()
        {
            Dispatcher.BeginInvoke( new Action( () =>
            {
                UserInterface ui = GetProductUserCtrlUI();
                if ( ui != null )
                {
                    ui.MarkForUpdate();
                }
            } ) );
        }

        /// <summary>
        /// Passed onto the current ProductUserCtrl to display a popup message
        /// </summary>
        /// <param name="_description">The message to display</param>
        public void PopupMessage( string _description )
        {
            Dispatcher.BeginInvoke( new Action( () =>
            {
                UserInterface ui = GetProductUserCtrlUI();
                if ( ui != null )
                {
                    ui.PopupMessage( _description );
                }
            } ) );
        }

        /// <summary>
        /// Passed onto the current ProductUserCtrl to display a warning message
        /// </summary>
        /// <param name="_description">The message to display</param>
        /// <param name="_title">The title to display</param>
        public void WarningMessage( string _description, string _title )
        {
            Dispatcher.BeginInvoke( new Action( () =>
            {
                UserInterface ui = GetProductUserCtrlUI();
                if ( ui != null )
                {
                    ui.WarningMessage( _description, _title );
                }
            } ) );
        }

        /// <summary>
        /// Passed onto the current ProductUserCtrl to display an error message
        /// </summary>
        /// <param name="_description">The message to display</param>
        /// <param name="_title">The title to display</param>
        public void ErrorMessage( string _description, string _title )
        {
            Dispatcher.BeginInvoke( new Action( () =>
            {
                UserInterface ui = GetProductUserCtrlUI();
                if ( ui != null )
                {
                    ui.ErrorMessage( _description, _title );
                }
            } ) );
        }

        /// <summary>
        /// Passed onto the current ProductUserCtrl to display a yes no message
        /// </summary>
        /// <param name="_description">The message to display</param>
        /// <param name="_title">The title to display</param>
        /// <returns>returns true if the user selected Yes</returns>
        public bool YesNoQuery( string _description, string _title )
        {
            bool result = false;

            UserInterface ui = GetProductUserCtrlUI();
            if ( ui != null )
            {
                result = ui.YesNoQuery( _description, _title );
            }

            return result;
        }

        /// <summary>
        /// Passed onto the current ProductUserCtrl to update the product list
        /// </summary>
        public void UpdateProjectList()
        {
            Dispatcher.BeginInvoke( new Action( () =>
            {
                UserInterface ui = GetProductUserCtrlUI();
                if ( ui != null )
                {
                    ui.UpdateProjectList();
                }
            } ) );
        }

        /// <summary>
        /// Passed onto the current ProductUserCtrl to update the current selected product
        /// </summary>
        public void UpdateSelectedProject()
        {
            Dispatcher.BeginInvoke( new Action( () =>
            {
                UserInterface ui = GetProductUserCtrlUI();
                if ( ui != null )
                {
                    ui.UpdateSelectedProject();
                }
            } ) );
        }

        /// <summary>
        /// Passed onto the current ProductUserCtrl to indicate the monitor has completed
        /// </summary>
        public void MonitorCompleted()
        {
            Dispatcher.BeginInvoke( new Action( () =>
            {
                UserInterface ui = GetProductUserCtrlUI();
                if ( ui != null )
                {
                    ui.MonitorCompleted();
                }
            } ) );
        }

        /// <summary>
        /// Passed onto the current ProductUserCtrl to show or hide the cancel button
        /// </summary>
        /// <param name="show">If true then the cancel button should be displayed, else hidden</param>
        public void ShowMonitorCancelButton( bool show )
        {
            Dispatcher.BeginInvoke( new Action( () =>
            {
                UserInterface ui = GetProductUserCtrlUI();
                if ( ui != null )
                {
                    ui.ShowMonitorCancelButton( show );
                }

            } ) );
        }

        /// <summary>
        /// Passed onto the current ProductUserCtrl to move the passed process to the front
        /// </summary>
        /// /// <param name="process">The process to move to the front/param>
        public void MoveToFront( Process process )
        {
            Dispatcher.BeginInvoke( new Action( () =>
            {
                UserInterface ui = GetProductUserCtrlUI();
                if ( ui != null )
                {
                    ui.MoveToFront( process );
                }

            } ) );
        }

        /// <summary>
        /// Passed onto the current ProductUserCtrl to try and close the window
        /// </summary>
        public void CloseWindow()
        {
            Dispatcher.BeginInvoke( new Action( () =>
            {
                UserInterface ui = GetProductUserCtrlUI();
                if ( ui != null )
                {
                    ui.CloseWindow();
                }

            } ) );
        }

        /// <summary>
        /// Part of the LogEventInterface, logs an action
        /// </summary>
        /// <param name="_action">The action to log</param>
        public void LogEvent( string _action )
        {
            Log( new LogEntry( _action ) );
        }

        /// <summary>
        /// Part of the LogEventInterface, logs an action, key and description
        /// </summary>
        /// <param name="_action">The action to log</param>
        /// <param name="_key">The key for the description</param>
        /// <param name="_description">The description of the log</param>
        public void LogEvent( string _action, string _key, string _description )
        {
            LogEntry logEntry = new LogEntry( _action );
            logEntry.AddValue( _key, _description );
            Log( logEntry );
        }

        /// <summary>
        /// Helper method to Log the passed LogEntry
        /// </summary>
        /// <param name="entry">The Log information to log</param>
        public void Log( LogEntry _entry )
        {
            Debug.Assert( _entry != null );
            Debug.Assert( m_cobraBayView != null );

            if ( _entry != null )
            {
                if ( m_cobraBayView != null )
                {
                    FORCManager forcManager = m_cobraBayView.Manager();
                    if ( forcManager != null )
                    {
                        forcManager.Log( _entry );
                    }
                }
            }
        }

        /// <summary>
        /// Part of the IRetrieveFileFromUri interface, returns the 
        /// full path to the file specified by the Uri String (or null).
        /// </summary>
        /// <param name="_uriString">The Uri string to the file</param>
        /// <returns>Full path to the file, or null if the file could not be retrieved</returns>
        public string RetrieveFile( string _uriString )
        {
            // Currently unused, exclude from review
            Debug.Assert( false );

            Debug.Assert( m_cobraBayView != null );

            string retrievedFileFullPath = null;

            try
            {
                if ( !string.IsNullOrWhiteSpace( _uriString ) )
                {
                    string filename = System.IO.Path.GetFileName( _uriString );
                    if ( filename != null )
                    {
                        retrievedFileFullPath = "C:\\Temp\\" + filename;

                        // If we already have the file, don't download it again
                        if ( !File.Exists( retrievedFileFullPath ) )
                        {
                            WebRequest webRequest = WebRequest.Create( new Uri(_uriString) );
                            WebResponse webResponse = webRequest.GetResponse();

                            using ( Stream stream = webResponse.GetResponseStream() )
                            {
                                Byte[] buffer = new byte[webResponse.ContentLength];

                                int offset = 0;
                                int read = 0;

                                do
                                {
                                    read = stream.Read( buffer, offset, buffer.Length - offset );
                                    offset += read;
                                }
                                while ( read > 0 );
                                File.WriteAllBytes( retrievedFileFullPath, buffer );
                            }
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                string action = GetType().ToString() + "::" + MethodBase.GetCurrentMethod().Name;
                string description = _uriString;
                description += " : ";
                description += ex.Message;
                LogEvent( action, ex.GetType().ToString(), description );
                retrievedFileFullPath = null;
            }

            return retrievedFileFullPath;
        }

        /// <summary>
        /// Called on a power mode change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PowerModeChanged( object sender, Microsoft.Win32.PowerModeChangedEventArgs e )
        {
            if ( e.Mode == Microsoft.Win32.PowerModes.Resume )
            {
                if ( m_cobraBayView != null )
                {
                    m_cobraBayView.m_versionStatus = m_cobraBayView.m_manager.ServerConnection.CheckClientVersion( m_cobraBayView.CobraBayVersion, out m_cobraBayView.m_serverClientVersion );
                    m_cobraBayView.CheckLauncherVersion();
                    MarkForUpdate();
                }
            }
        }

        /// <summary>
        /// Called when the window is closing, this may cancel the
        /// close if the application is busy doing things.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClosing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            e.Cancel = !CanIClose();
        }

        /// <summary>
        /// Our CobraBayView, reused from CobraBay
        /// </summary>
        private CobraBayView m_cobraBayView;

        /// <summary>
        /// Our LauncherModelManager, used to access .Net 4.8 components.
        /// </summary>
        private LauncherModelManager m_launcherModelManager = null;

        /// <summary>
        /// Have we show the window or not?
        /// </summary>
        private bool m_previouslyShown = false;

        /// <summary>
        /// The Path Environment name
        /// </summary>
        private const string c_pathEnvironmentName = "PATH";

        /// <summary>
        /// The XInput DLL name
        /// </summary>
        private const string c_xInputDLLFile = "XINPUT1_3.DLL";

        /// <summary>
        /// Our front (main) page, we keep this because
        /// it can take some time to create it.
        /// </summary>
        private FrontPage m_frontPage = null;
    }
}
