//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ProductUserCtrl UserControl
//
//! Author:     Alan MacAree
//! Created:    15 Aug 2022
//----------------------------------------------------------------------

using CBViewModel;
using ClientSupport;
using FDUserControls;
using JSONConverters;
using LauncherModel;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Windows.Input;
using FORCServerSupport;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for ProductUserCtrl.xaml
    /// This handles the left side of the main UI for the launcher.
    /// Project icons, name, status, user info and project related
    /// updates, installs and menu choices are controlled via this class.
    /// </summary>
    public partial class ProductUserCtrl : UserControl, UserInterface
    {
        /// <summary>
        /// The current active project
        /// </summary>
        Project m_project = null;

        /// <summary>
        /// Information about the currently selected product(game version).
        /// </summary>
        List<ProductUpdateInformation> productUpdateInformationList = null;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public ProductUserCtrl()
        {
            InitializeComponent();
            PART_ProductManageBtnsUserCtrl.SetProductUserCtrl( this );
            DataContext = this;
            SetupDevAndDebugOptions();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_frontPage">The Front page</param>
        /// <param name="_cobraBayView">The CobraBayView to retrieve data from</param>
        public ProductUserCtrl( FrontPage _frontPage, CobraBayView _cobraBayView )
        {
            Debug.Assert( _frontPage != null );
            Debug.Assert( _cobraBayView != null );

            InitializeComponent();
            SetFrontPage( _frontPage );
            PART_ProductMainBtnsUserCtrl.SetCobraBayView( _cobraBayView );
            PART_ProductManageBtnsUserCtrl.SetProductUserCtrl( this );
            SetupDevAndDebugOptions();
        }

        /// <summary>
        /// Called when page is initialized
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnInitialized( object sender, EventArgs e )
        {
            m_updateTimer = new Timer( TimedUpdate, null, c_autoUpdate, c_autoUpdate );
        }

        /// <summary>
        /// Sets up developer or debug options
        /// </summary>
        private void SetupDevAndDebugOptions()
        {
            #if ( DEBUG || DEVELOPER )
                PART_RemoveLinkMenuItem.Visibility = Visibility.Visible;
            #endif
        }

        /// <summary>
        /// Called via a timer, this uses Invoke to call the UI to update
        /// the project (in case of server side changes, e.g. updates etc)
        /// </summary>
        /// <param name="data"></param>
        public void TimedUpdate( object data )
        {
            Dispatcher.Invoke( new Action( () => { TimedUpdateInternal(); } ) );
        }

        /// <summary>
        /// Used to see if we need to update the project. This is called via a timer
        /// whilst the user is logged in, in case the project changes!
        /// </summary>
        private void TimedUpdateInternal()
        {
            Project p = View.GetActiveProject();
            if ( p != null )
            {
                if ( (View.m_manager.Authorised) && (p.Action != Project.ActionType.Disabled) )
                {
                    Update();
                }
            }
        }

        /// <summary>
        /// Sets the FrontPage, this is needed as this control needs access other
        /// objects that FrontPage provides.
        /// </summary>
        /// <param name="_frontPage">The FrontPage that contains this object</param>
        public void SetFrontPage( FrontPage _frontPage )
        {
            Debug.Assert( _frontPage != null );
            m_frontPage = _frontPage;

            LauncherWindow launcherWindow = m_frontPage.GetLauncherWindow();
            Debug.Assert( launcherWindow != null );
            if ( launcherWindow != null )
            {
                PART_ProductMainBtnsUserCtrl.ManageButton = new ButtonActionManage( launcherWindow, this );
                View = launcherWindow.GetCobraBayView();
                PART_ProductMainBtnsUserCtrl.SetCobraBayView( View );
                DisplayUserDetails();

                m_frontPage = _frontPage;

                CobraBayView cobraBayView = launcherWindow.GetCobraBayView();
                if (m_project == null)
                {
                    m_project = cobraBayView.GetActiveProject();

                }

                Update();
            }
        }

        /// <summary>
        /// Displays the users details
        /// </summary>
        private void DisplayUserDetails()
        {
            LauncherWindow launcherWindow = m_frontPage.GetLauncherWindow();
            Debug.Assert( launcherWindow != null );
            if ( launcherWindow != null)
            {
                CobraBayView cobraBayView = launcherWindow.GetCobraBayView();
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
                                // We may have to reduce the name because we have limited space on the UI. If we do reduce the name, then make sure that
                                // Consts.c_eclipse is appended to the end. Note that the size this is reduced to is Consts.c_maxNumberOfCharsForName in
                                // total, including the Consts.c_eclipse.Length.
                                PART_RegisteredName.Content = FDUtils.ReduceStringToMaxLength( userDetails.RegisteredName, c_maxNumberOfCharsForName, c_userNameReducedExt );
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This is the main update method. This checks the status of the
        /// current project and then decides what needs to be displayed on the
        /// UI.
        /// </summary>
        public void Update()
        {
            if ( m_frontPage != null )
            {
                LauncherWindow launcherWindow = m_frontPage.GetLauncherWindow();
                Debug.Assert( launcherWindow != null );
                if ( launcherWindow != null )
                {
                    // Create our Versions button if we need to
                    if ( PART_ProductMainBtnsUserCtrl.SmallButton == null )
                    {
                        PART_ProductMainBtnsUserCtrl.SmallButton = new ButtonActionVersion( launcherWindow, m_frontPage, this, m_frontPage );
                    }

                    CobraBayView cobraBayView = launcherWindow.GetCobraBayView();
                    this.SetSelectedProject(cobraBayView.GetActiveProject());

                    Debug.Assert(cobraBayView != null);
                    if ( cobraBayView != null)
                    {
                        cobraBayView.UpdateExitStatus();

                        if ( cobraBayView.ShouldExit )
                        {
                            CloseIfSafe();
                        }

                        // Continue if we have a cobraBayView and we
                        // are not closing. 
                        if ( cobraBayView != null && !m_isClosing )
                        {
                            Project product = cobraBayView.GetActiveProject();
                            if ( product != null )
                            {
                                if ( !string.IsNullOrWhiteSpace( product.LogoImageURI ) )
                                {
                                    PART_ProjectImage.Source = new BitmapImage( new Uri( product.LogoImageURI, UriKind.Absolute ) );
                                }

                                if ( product != null )
                                {
                                    Project.ActionType action = product.Action;

                                    // Depending on the state of the project...
                                    switch ( action )
                                    {
                                        case Project.ActionType.Disabled:
                                            {
                                                EnableButtons( false );
                                                break;
                                            }
                                        case Project.ActionType.Invalid:
                                            {
                                                PART_ProductMainBtnsUserCtrl.EnableBigBtn( false );
                                                PART_ProductMainBtnsUserCtrl.EnableSmallBtn( true );
                                                break;
                                            }
                                        case Project.ActionType.Install:
                                            {
                                                EnableButtons();
                                                // Disable the manage button, the user
                                                // cannot manage a game that is not installed.
                                                PART_ProductMainBtnsUserCtrl.EnableManageBtn( false );
                                                PART_ProductMainBtnsUserCtrl.BigButton = new ButtonActionInstall( launcherWindow, this );
                                                break;
                                            }
                                        case Project.ActionType.Update:
                                            {
                                                EnableButtons();
                                                PART_ProductMainBtnsUserCtrl.BigButton = new ButtonActionUpdate( launcherWindow, this );
                                                break;
                                            }
                                        case Project.ActionType.Play:
                                            {
                                                EnableButtons();
                                                PART_ProductMainBtnsUserCtrl.BigButton = new ButtonActionPlay( launcherWindow, this );
                                                break;
                                            }
                                        default:
                                            {
                                                PART_ProductMainBtnsUserCtrl.EnableBigBtn( false );
                                                break;
                                            }
                                    }

                                    // Update the Button text
                                    UpdateButtonTextAndSettings();
                                   
                                    // Update Age Rating
                                    this.SetAgeRating(cobraBayView.LanguageOverride, m_project.ESRBRating, m_project.PEGIRating);

                                    // Update Release Notes
                                    this.LoadReleaseNotes();
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// User clicks on the Release Notes, navigates to a browser page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnReleaseNotesClick(object sender, MouseButtonEventArgs e)
        {
            Debug.Assert(sender != null);

            if (this.productUpdateInformationList != null && sender != null)
            {
                // We should only have one item, more means we have a problem
                if (this.productUpdateInformationList.Count == 1)
                {
                    ProductUpdateInformation productUpdateDetails = this.productUpdateInformationList[0];

                    Dictionary<string, string> dataToLog = new Dictionary<string, string>();
                    dataToLog.Add("release_notes_title", productUpdateDetails.Title);
                    dataToLog.Add("release_notes_link", productUpdateDetails.GetHttpLink());
                    LogEvent("OnReleaseNotesClick", dataToLog);

                    Process.Start(HTMLStringUtils.StripHTMLFromString(productUpdateDetails.GetHttpLink()));
                }
            }
        }

        /// <summary>
        /// LogEvent to FORC telemetry
        /// </summary>
        /// <param name="eventToLog"></param>
        /// <param name="dataToLog"></param>
        /// <returns>bool</returns>
        private bool LogEvent(string eventToLog, Dictionary<string, string> dataToLog)
        {
            if (m_frontPage != null)
            {
                LauncherWindow launcherWindow = m_frontPage.GetLauncherWindow();
                Debug.Assert(launcherWindow != null);
                if (launcherWindow != null)
                {
                    CobraBayView cobraBayView = launcherWindow.GetCobraBayView();
                    Debug.Assert(cobraBayView != null);
                    if (cobraBayView != null)
                    {
                        FORCManager fORCManager = cobraBayView.Manager();
                        if (fORCManager != null)
                        {
                            FORCServerConnection fORCServerConnection = (FORCServerConnection)fORCManager.ServerConnection;
                            LogEntry sg = new LogEntry(eventToLog);
                            // add project name
                            sg.AddValue("product", cobraBayView.GetActiveProject().Name);
                            // pass data from event triggered
                            foreach (KeyValuePair<string, string> entry in dataToLog)
                            {
                                sg.AddValue(entry.Key, entry.Value);
                            }

                            fORCServerConnection.LogValues(fORCManager.UserDetails, sg);
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Enables (or not) the buttons within this control
        /// </summary>
        /// <param name="_enableAndShow">If true (default), then buttons are enabled and show, otherwise disabled</param>
        public void EnableButtons( bool _enable = true )
        {
            PART_ProductMainBtnsUserCtrl.Enable( _enable );
            PART_MenuItemSettings.IsEnabled = _enable;
            PART_LogOutMenuItem.IsEnabled = _enable;
        }

        /// <summary>
        /// Updates the button text from the held ButtonActions.
        /// </summary>
        private void UpdateButtonTextAndSettings()
        {
            PART_ProductMainBtnsUserCtrl.UpdateButtonText();
        }

        /// <summary>
        /// Returns the OS identification
        /// </summary>
        /// <returns>The OS identification</returns>
        public string GetOSIdent()
        {
            return ClientSupport.Utils.OSIdent.GetOSIdent();
        }

        /// <summary>
        /// Forces the launcher to update the displayed information
        /// </summary>
        public void MarkForUpdate()
        {
            Dispatcher.Invoke( new Action( () => { Update(); } ) );
        }

        /// <summary>
        /// Displays a popup information message
        /// </summary>
        /// <param name="_description">The popup message to display</param>
        public void PopupMessage( string _description )
        {
            string title = LocalResources.Properties.Resources.DialogTitle;

            Dispatcher.Invoke( new Action( () =>
            {
                MessageBox.Show( Window.GetWindow( this ), _description, title,
                    MessageBoxButton.OK, MessageBoxImage.Warning );
            } ) );
        }

        /// <summary>
        /// Displays a warning message to the user
        /// </summary>
        /// <param name="_description">The description of the warning mesage</param>
        /// <param name="_title">The title of the warning message</param>
        public void WarningMessage( string _description, string _title )
        {
            Dispatcher.Invoke( new Action( () =>
            {
                MessageBox.Show( Window.GetWindow( this ), _description, _title,
                    MessageBoxButton.OK, MessageBoxImage.Warning );
            } ) );
        }

        /// <summary>
        /// Displays an error message to the user
        /// </summary>
        /// <param name="_description">The description of the error message</param>
        /// <param name="_title">The title of the error message</param>
        public void ErrorMessage( string _description, string _title )
        {
            Dispatcher.Invoke( new Action( () =>
            {
                MessageBox.Show( Window.GetWindow( this ), _description, _title,
                    MessageBoxButton.OK, MessageBoxImage.Warning );
            } ) );
        }

        /// <summary>
        /// Displays a Yes/No message box to the user and returns the result.
        /// </summary>
        /// <param name="_description">The description of the message</param>
        /// <param name="_title">The title of the message</param>
        /// <returns>returns true if the user selected Yes from the pop up message</returns>
        public bool YesNoQuery( string _description, string _title )
        {
            if ( MessageBox.Show( _description, _title, MessageBoxButton.YesNo, MessageBoxImage.Exclamation ) == MessageBoxResult.Yes )
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Called from the library to get the UI to update the project list.
        /// This is no longer used in the new launcher, but remains here in case
        /// of future changes.
        /// </summary>
        public void UpdateProjectList()
        {
            // Not used
        }

        ///
        /// Called from the library to display updated information on the project.
        /// This is no longer used in the new launcher, but remains here in case
        /// of future changes.
        /// </summary>
        public void UpdateSelectedProject()
        {
            // Not used
        }

        /// <summary>
        /// Updates the selected project
        /// </summary>
        public void SetSelectedProject(Project _project)
        {
            if (m_project != _project)
            {
                m_project = _project;
            }
        }

        /// <summary>
        /// Called by the library to indicate that the monitor (used to display
        /// download information) has completed.
        /// </summary>
        public void MonitorCompleted()
        {
            LauncherWindow launcherWindow = m_frontPage.GetLauncherWindow();
            Debug.Assert( launcherWindow != null );
            if ( launcherWindow != null )
            {
                CobraBayView cobraBayView = launcherWindow.GetCobraBayView();
                Debug.Assert( cobraBayView != null );
                if ( cobraBayView != null )
                {
                    if ( cobraBayView != null )
                    {
                        FORCManager manager = cobraBayView.Manager();
                        if ( manager != null )
                        {
                            manager.UpdateProjectList();
                        }
                    }
                }
            }
            
            Dispatcher.Invoke( new Action( () =>
            {
                View.Monitor = null;

                Update();
            } ) );
        }

        /// <summary>
        /// Called by the library to indicate that the monitor cancel
        /// button should be enabled (or displayed) or not.
        /// </summary>
        /// <param name="_show">If true, the cancel button is enabled, else disabled</param>
        public void ShowMonitorCancelButton( bool _show )
        {
            if ( _show )
            {
                if ( m_frontPage != null )
                {
                    LauncherWindow launcherWindow = m_frontPage.GetLauncherWindow();
                    Debug.Assert( launcherWindow != null );
                    if ( launcherWindow != null )
                    {
                        PART_ProductMainBtnsUserCtrl.BigButton = new ButtonActionProgressCancel( launcherWindow, this );
                        PART_ProductMainBtnsUserCtrl.EnableBigBtn( true );
                        Debug.WriteLine( "ShowMonitorCancelButton" );

                        // Update the Button text
                        UpdateButtonTextAndSettings();
                    }
                }
            }
            else
            {
                Update();
            }
        }

        /// <summary>
        /// Moves this application to the front
        /// </summary>
        /// <param name="_process"></param>
        public void MoveToFront( Process _process )
        {
            SetForegroundWindow( _process.MainWindowHandle );
        }

        /// <summary>
        /// Called by the library to close the launcher. This is no
        /// longer used, but remains in case of future changes.
        /// </summary>
        public void CloseWindow()
        {
            CloseIfSafe();
        }

        /// <summary>
        /// Called when the user clicks in the menu area menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMenuMouseDown( object sender, System.Windows.Input.MouseButtonEventArgs e )
        {
            DisplayPopupMenu();
        }

        /// <summary>
        /// Called when the user clicks on the Connected As Text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PART_ConnectedAsMouseDown( object sender, System.Windows.Input.MouseButtonEventArgs e )
        {
            DisplayPopupMenu();
        }

        /// <summary>
        /// Called when the user clicks on the Registered name
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PART_RegisteredNameMouseDown( object sender, System.Windows.Input.MouseButtonEventArgs e )
        {
            DisplayPopupMenu();
        }

        /// <summary>
        /// Displays the popup menu
        /// </summary>
        private void DisplayPopupMenu()
        {
            ContextMenu contextMenu = ContextMenuService.GetContextMenu( PART_MenuArrowDown as DependencyObject );
            if ( contextMenu != null )
            {
                contextMenu.PlacementTarget = PART_ConnectedAsBorder;
                contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                contextMenu.Width = PART_ConnectedAsBorder.ActualWidth;
                contextMenu.IsOpen = true;
            }
        }
        /// <summary>
        /// Called when the user selects Settings form the menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PART_OnClickMenuItemSettings( object sender, RoutedEventArgs e )
        {
            LauncherWindow launcherWindow = m_frontPage.GetLauncherWindow();

            Debug.Assert( launcherWindow != null );
            if ( launcherWindow != null )
            {
                NavigationService ns = NavigationService.GetNavigationService( this );
                _ = ns.Navigate( new SettingsPage( launcherWindow ) );
            }
        }

        /// <summary>
        /// Called when the user selects Log Out form the menu.
        /// Any user details that is store is cleared
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PART_OnClickMenuItemLogOut( object sender, RoutedEventArgs e )
        {
            LauncherWindow launcherWindow = m_frontPage.GetLauncherWindow();

            Debug.Assert( launcherWindow != null );
            if ( launcherWindow != null )
            {
                launcherWindow.DisplayWaitPage();

                // Log out
                bool forgetUserDetails = true;
                _ = launcherWindow.Logout( forgetUserDetails );;
            }
        }

        /// <summary>
        /// Closes the window if it is safe to do so.
        /// </summary>
        private void CloseIfSafe()
        {
            LauncherWindow launcherWindow = m_frontPage.GetLauncherWindow();

            Debug.Assert( launcherWindow != null );
            if ( launcherWindow != null )
            {
                if ( launcherWindow.CanIClose() )
                {
                    m_isClosing = true;
                    System.Windows.Application.Current.Shutdown();
                }
            }
        }

        /// <summary>
        /// Displays the Manage buttons
        /// </summary>
        public void DisplayManageButtons()
        {
            m_displayManagedControls = true;
            DisplayManagedControls( m_displayManagedControls );
        }

        /// <summary>
        /// Displays or hides the manage controls
        /// </summary>
        /// <param name="_displayManagedControls">If true then manage ctrls are displayed, else normal ctrls are displayed</param>
        public void DisplayManagedControls( bool _displayManagedControls )
        {
            m_displayManagedControls = _displayManagedControls;
            if ( m_displayManagedControls )
            {
                PART_ProductMainBtnsUserCtrl.Visibility = Visibility.Collapsed;
                PART_ProductManageBtnsUserCtrl.Visibility = Visibility.Visible;
            }
            else
            {
                PART_ProductMainBtnsUserCtrl.Visibility = Visibility.Visible;
                PART_ProductManageBtnsUserCtrl.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Started the process of validating the game files
        /// </summary>
        public void ValidateGameFiles()
        {
            if ( View != null )
            {
                DisplayManagedControls( false );
                View.CommonDownload();
            }
        }

        /// <summary>
        /// Uninstalls the current product
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void UninstallProduct()
        {
            if ( View != null )
            {
                DisplayManagedControls( false );
                View.UninstallSelectedProduct();
            }
        }

        /// <summary>
        /// Checks for updates for the current product
        /// </summary>

        public void CheckForUpdates()
        {
            if ( View != null )
            {
                DisplayManagedControls( false );

                View.ResetDMCache();
                Update();

                // This can be updated to check the status of a furutre dev option
                // called DevIgnoreUpdates
                bool ignore = false; 
                string message = View.UpdateCheckMessage(ignore);
                if ( !String.IsNullOrEmpty( message ) )
                {
                    MessageBox.Show( message, LocalResources.Properties.Resources.UpdateCheckTitle );
                }
            }
        }

        /// <summary>
        /// Returns the image for the passed PEGI Rating text
        /// </summary>
        /// <param name="_pegiRating">The PEGI text to get the rating for</param>
        /// <returns>The location & name of the image for the PEGI rating</returns>
        private string GetPEGIRatingImage(string _pegiRating)
        {
            string pegiImageSource = c_pegiImageLoc;

            Debug.Assert(!String.IsNullOrEmpty(_pegiRating));

            if (!String.IsNullOrEmpty(_pegiRating))
            {
                switch (_pegiRating)
                {
                    case c_pegiEighteenImg:
                        pegiImageSource += c_pegiEighteenImg;
                        break;
                    case c_pegiSixteenText:
                        pegiImageSource += c_pegiSixteenImg;
                        break;
                    case c_pegiTwelveText:
                        pegiImageSource += c_pegiTwelveImg;
                        break;
                    case c_pegiSevenText:
                        pegiImageSource += c_pegiSevenImg;
                        break;
                    case c_pegiSixText:
                        pegiImageSource += c_pegiSixImg;
                        break;
                    case c_pegiFourText:
                        pegiImageSource += c_pegiFourImg;
                        break;
                    case c_pegiThreeText:
                        pegiImageSource += c_pegiThreeImg;
                        break;
                    default:
                        pegiImageSource += c_pegiSixteenImg;
                        break;
                }
            }

            return pegiImageSource;
        }

        /// <summary>
        /// Returns true if the passed language string is
        /// European
        /// </summary>
        /// <param name="_languageString">The language string to check</param>
        /// <returns>true if European language, else false</returns>
        private bool IsLanguageEuropean(string _languageString)
        {
            bool isEuropean = false;
            switch (_languageString)
            {
                case "de":
                case "en":
                case "es":
                case "fr":
                case "pt-BR":
                case "":
                    isEuropean = true;
                    break;
                case "ru":
                    isEuropean = false;
                    break;
                default:
                    isEuropean = false;
                    break;
            }

            return isEuropean;
        }

        /// <summary>
        /// Determines if we should use the ERSB rating or not.
        /// URSB is used United States, Canada, and Mexico, this
        /// check only returns true if:
        /// The launcher language is set to English to default language and
        ///    if the PC language is set to United States, Canada, or Mexico.
        /// </summary>
        /// <returns>true if ESRB should be used, else false</returns>
        private bool UseESRBRating(string _languageString)
        {
            bool useERSB = false;

            bool checkPCLanguage = false;
            switch (_languageString)
            {
                case "en":
                case "es":
                case "":
                    checkPCLanguage = true;
                    break;
                default:
                    useERSB = false;
                    break;
            }

            if (checkPCLanguage)
            {
                CultureInfo culture = CultureInfo.CurrentCulture;
                string languageName = culture.Name;

                switch (languageName)
                {
                    case "en-US":
                    case "en-CA":
                    case "es-MX":
                        useERSB = true;
                        break;
                    default:
                        useERSB = false;
                        break;
                }
            }

            return useERSB;
        }

        /// <summary>
        /// Returns the language specific teen ESRB image
        /// </summary>
        /// <returns></returns>
        private string GetESRBTeenImage()
        {
            string esrbImageSource = c_esrbEnTeenImg;
            CultureInfo culture = CultureInfo.CurrentCulture;
            switch (culture.Name)
            {
                case "en-US":
                case "en-CA":
                    esrbImageSource = c_esrbEnTeenImg;
                    break;
                case "es-MX":
                    esrbImageSource = c_esrbEsTeenImg;
                    break;
                default:
                    esrbImageSource = c_esrbEnTeenImg;
                    break;
            }

            return esrbImageSource;
        }

        /// <summary>
        /// Returns the image for the passed ESRB Rating text
        /// </summary>
        /// <param name="_esrbRating">The ESRB text to get the rating for</param>
        /// <returns>The location & name of the image for the ESRB rating</returns>
        private string GetESRBRatingImage(string _esrbRating)
        {
            string esrbImageSource = c_esrbImageLoc;

            Debug.Assert(!String.IsNullOrEmpty(_esrbRating));

            if (!String.IsNullOrEmpty(_esrbRating))
            {
                switch (_esrbRating)
                {
                    case c_esrbTeenText:
                        esrbImageSource += GetESRBTeenImage();
                        break;
                    default:
                        esrbImageSource += GetESRBTeenImage();
                        break;
                }
            }
            return esrbImageSource;
        }

        /// <summary>
        /// Loads the latest release note title and link to on-line notes
        /// </summary>
        public void LoadReleaseNotes()
        {

            if (m_project.NoDetails)
            {
                PART_ReleaseNotes.Visibility = Visibility.Collapsed;
            } else
            {
                this.setProductUpdateInformation();

                if (this.productUpdateInformationList != null)
                {
                    // We should only have one item, more means we have a problem
                    if (this.productUpdateInformationList.Count == 1)
                    {
                        ProductUpdateInformation productUpdateDetails = this.productUpdateInformationList[0];
                        PART_ReleaseNotes_Title.Text = HTMLStringUtils.StripHTMLFromString(productUpdateDetails.Title);
                        PART_ReleaseNotes.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private void setProductUpdateInformation()
        {
            // detect version - reload notes
            DynamicContentModel dynamicContent = this.m_frontPage.GetLauncherModelManager().GetDynamicContent();            

            this.productUpdateInformationList = dynamicContent.GetProductUpdateInformation();
        }

        public void SetAgeRating(string _languageString, string _esrbRratingString, string _pegiRatingString)
        {
            if (m_project.NoDetails)
            {
                PART_AgeRatingImage.Visibility = Visibility.Collapsed;
                PART_AgeRatingImageESRBOverlay.Visibility = Visibility.Collapsed;
            } else
            {
                PART_AgeRatingImage.Visibility = Visibility.Visible;
                PART_AgeRatingImageESRBOverlay.Visibility = Visibility.Hidden;

                if (IsLanguageEuropean(_languageString))
                {
                    if (UseESRBRating(_languageString))
                    {
                        if (!String.IsNullOrEmpty(_esrbRratingString))
                        {
                            // If this is English(US) use a ESRB image
                            string esrbImageSource = GetESRBRatingImage(_esrbRratingString);
                            Debug.Assert(!String.IsNullOrEmpty(esrbImageSource));
                            if (!String.IsNullOrEmpty(esrbImageSource))
                            {
                                PART_AgeRatingImage.Source = new BitmapImage(new Uri(esrbImageSource, UriKind.Absolute));
                                PART_AgeRatingImage.Visibility = Visibility.Visible;
                                PART_AgeRatingImageESRBOverlay.Visibility = Visibility.Visible;
                                PART_AgeRatingImageESRBOverlay.Source = new BitmapImage(new Uri(esrbImageSource, UriKind.Absolute)); ;
                            }
                        }
                    }
                    else if (!String.IsNullOrEmpty(_pegiRatingString))
                    {
                        string pegiImageSource = GetPEGIRatingImage(_pegiRatingString);
                        Debug.Assert(!String.IsNullOrEmpty(pegiImageSource));
                        if (!String.IsNullOrEmpty(pegiImageSource))
                        {
                            // We don't use the PART_AgeRatingImageESRBOverlay for PEGI Ratings
                            PART_AgeRatingImageESRBOverlay.Visibility = Visibility.Hidden;
                            PART_AgeRatingImage.Source = new BitmapImage(new Uri(pegiImageSource, UriKind.Absolute));
                            PART_AgeRatingImage.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Developer method to remove a linked store account, this is not available
        /// to normal users.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnRemoveStoreLinkClick( object sender, RoutedEventArgs e )
        {
            bool removedSteamLinkOkay = false;

            LauncherWindow launcherWindow = m_frontPage.GetLauncherWindow();
            Debug.Assert( launcherWindow != null );
            if ( launcherWindow != null )
            {
                launcherWindow.DisplayWaitPage();

                await Task.Run( () =>
                {
                    removedSteamLinkOkay = View.DeleteLinkAccounts();
                } );

                if ( removedSteamLinkOkay )
                {
                    MessageBox.Show( "Removal Okay, exiting launcher.", "Remove Store link" );

                    if ( launcherWindow.CanIClose() )
                    {
                        System.Windows.Application.Current.Shutdown();
                    }
                }
                else
                {
                    MessageBox.Show( "Failed to remove link", "Remove Store link" );
                    _ = launcherWindow.DisplayFrontPageAsync();
                }
            }
        }

        // Should the managed controls be displayed?
        private bool m_displayManagedControls = false;

        /// <summary>
        /// Our FrontPage
        /// </summary>
        private FrontPage m_frontPage = null;

        /// <summary>
        /// Our CobraBayView
        /// </summary>
        public CobraBayView View { get; set; }

        /// <summary>
        /// Time used to check the status of the 
        /// current project (in case it changes 
        /// whilst user is logged in.
        /// </summary>
        const int c_autoUpdate = 10 * 60 * 1000; // Ten minutes in milliseconds.
        private Timer m_updateTimer;

        /// <summary>
        /// Used to set the foreground window
        /// </summary>
        /// <param name="hWnd">The handle of the window to set</param>
        /// <returns>None-zero the window was set to the foreground, 0 the window was not set</returns>
        [DllImportAttribute( "User32.dll" )]
        private static extern IntPtr SetForegroundWindow( IntPtr hWnd );

        /// <summary>
        ///  Are we closing?
        /// </summary>
        private bool m_isClosing = false;

        /// <summary>
        /// The maximum number of chars of a persons name (first+surname) that we will display.
        /// </summary>
        public const int c_maxNumberOfCharsForName = 35;

        /// <summary>
        /// The chars added to the end of the users name if we end up reducing it.
        /// </summary>
        public const string c_userNameReducedExt = "...";

        /// <summary>
        /// Half the slope width (the bar slope at the bottom of the heroimage, 
        /// used in calculations, therefore calculated here rather than dynamically.
        /// </summary>
        private const double c_halfSlopeWidth = c_slopeWidth / 2;

        /// <summary>
        /// The bar slope X axis distance (width)
        /// </summary>
        private const double c_slopDifferenceX = 50d;

        /// <summary>
        /// The English(US) language name, this used as a special case for age rating
        /// </summary>
        private const string c_usCultureString = "en-US";

        /// <summary>
        /// Location of the ESRB images
        /// </summary>
        private const string c_esrbImageLoc = "pack://application:,,,/FDUserControls;component/Images/ESRB/";

        /// <summary>
        /// The slope width (the bar slope at the bottom of the hero image)
        /// </summary>
        private const double c_slopeWidth = 500d;

        /// <summary>
        /// ESRB text and associated image
        /// </summary>
        private const string c_esrbTeenText = "teen";
        private const string c_esrbEnTeenImg = "ESRB_teenEn.png";
        private const string c_esrbEsTeenImg = "ESRB_teenEs.png";

        /// <summary>
        /// Location of the PEGI image
        /// </summary>
        private const string c_pegiImageLoc = "pack://application:,,,/FDUserControls;component/Images/PEGI/";

        /// <summary>
        /// PEGI text and associated image
        /// </summary>
        private const string c_pegiEighteenText = "18";
        private const string c_pegiEighteenImg = "age-18-white.png";
        private const string c_pegiSixteenText = "16";
        private const string c_pegiSixteenImg = "age-16-white.png";
        private const string c_pegiTwelveText = "12";
        private const string c_pegiTwelveImg = "age-12-white.png";
        private const string c_pegiSevenText = "7";
        private const string c_pegiSevenImg = "age-7-white.png";
        private const string c_pegiSixText = "6";
        private const string c_pegiSixImg = "age-6-white.png";
        private const string c_pegiFourText = "4";
        private const string c_pegiFourImg = "age-4-white.png";
        private const string c_pegiThreeText = "3";
        private const string c_pegiThreeImg = "age-3-white.png";
    }
}
