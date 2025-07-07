//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! FrontPage, this is first page we display
//
//! Author:     Alan MacAree
//! Created:    19 Aug 2022
//----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CBViewModel;
using ClientSupport;
using FDUserControls;
using System.Diagnostics;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for HomeLoginPage.xaml
    /// </summary>
    public partial class HomeLoginPage : Page, IVerification
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_launcherWindow">The launcherWindow</param>
        /// <param name="_displayAccountLinkChoice">Should we display the Link Account choice after we login and verify any codes?</param>
        /// <param name="_requestNewLogon">Forces the logon prcoess to ignore any auto logon via stores</param>
        public HomeLoginPage( LauncherWindow _launcherWindow, 
                              bool _displayAccountLinkChoice,
                              bool _requestNewLogon )
        {
            InitializeComponent();
            m_launcherWindow = _launcherWindow;
            m_displayAccountLinkChoice = _displayAccountLinkChoice;
            m_requestNewLogon = _requestNewLogon;

            // If we allow linking, then this is via a store and we
            // don't allow the user to remember the account. This is
            // the account remember is automatic via the linking
            // functionality. Remembering a linked account details
            // could cause issues if the account becomes unlinked.
            if ( m_displayAccountLinkChoice )
            {
                PART_RememberMeCB.Visibility = Visibility.Hidden;
            }

            // The main window should not be null.
            Debug.Assert( m_launcherWindow != null );
        }
        
        /// <summary>
        /// Called when the page is loaded.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPageLoaded( object sender, RoutedEventArgs e )
        {
            // Create out list of controls that need to be aligned with each other.
            List<EditBoxUserCtrl> editBoxUserCtrlList = new List<EditBoxUserCtrl>
            {
                PART_PasswordEditBox
            };

            // Make one of the controls the master by joining the other(s) to it.
            PART_EmailEditBox.JoinAndAlignControls( editBoxUserCtrlList );

            // Make sure we know when the email or password changes
            PART_EmailEditBox.TextChangedEventHandler += OnTextChangedEventHandler;
            PART_PasswordEditBox.TextChangedEventHandler += OnTextChangedEventHandler;

            // This page may have already been loaded and unloaded, and left 
            // in a state that it not ready for the user to login. Therefore 
            // setup the controls to ensure the user can login.

            // Make sure we set the spinner and password to the initial values
            PART_WaitSpinner.Visibility = Visibility.Hidden;
            PART_PasswordEditBox.Password( "" );

            // Enable the controls
            EnableControls( true );
        }

        /// <summary>
        /// Called when the page is unloaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPageUnloaded( object sender, RoutedEventArgs e )
        {
            // Unregister the events that we registered.
            PART_EmailEditBox.TextChangedEventHandler -= OnTextChangedEventHandler;
            PART_PasswordEditBox.TextChangedEventHandler -= OnTextChangedEventHandler;
        }

        /// <summary>
        /// Called by the EditBoxControls when text changes. We use this to determine 
        /// if we can enable the login button or not.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTextChangedEventHandler( object sender, TextChangedEventArgs e )
        {
            string emailAddress = PART_EmailEditBox.TextBoxText;
            string password =  PART_PasswordEditBox.Password();

            if ( !string.IsNullOrWhiteSpace( emailAddress ) &&
                 !string.IsNullOrWhiteSpace( password ) )
            {
                PART_LogInBtn.IsEnabled = true;
            }
            else
            {
                PART_LogInBtn.IsEnabled = false;
            }
        }

        /// <summary>
        /// Called when the user clicks on the LOG IN button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PART_LogInBtn_Click( object sender, RoutedEventArgs e )
        {
            // Do we need to remember the account details?
            Debug.Assert( m_launcherWindow != null );
            if ( m_launcherWindow != null )
            {
                m_launcherWindow.StoreUserDetails = ( PART_RememberMeCB.IsChecked == true );
                // Do we need to remember the account details?
            }
            _ = LogInAsync();
        }

        /// <summary>
        /// Enables (or not) the controls on the page.
        /// </summary>
        /// <param name="_enableControls">If true, then controls are enabled, else disabled.</param>
        private void EnableControls( bool _enableControls )
        {
            PART_LogInBtn.IsEnabled         = _enableControls;
            PART_EmailEditBox.IsEnabled     = _enableControls;
            PART_PasswordEditBox.IsEnabled  = _enableControls;
            PART_RememberMeCB.IsEnabled     = _enableControls;
            PART_ForgotPasswordHL.IsEnabled = _enableControls;
            PART_CreateAccHLL.IsEnabled     = _enableControls;
        }

        /// <summary>
        /// Try and login asynchronously, this makes sure the UI stays
        /// responsive.
        /// </summary>
        /// <returns></returns>
        private async Task LogInAsync( bool _displayVerificationPage = true )
        {
            EnableControls( false );
            bool handledLogin = false;

            // Just in case we have an existing error displayed.
            HideUserError();

            string emailAddress = PART_EmailEditBox.TextBoxText;
            string password =  PART_PasswordEditBox.Password();

            // Recover the email & password if we have them to
            // recover from
            if ( string.IsNullOrWhiteSpace( emailAddress ) &&
                 !string.IsNullOrWhiteSpace( m_emailAddress ) )
            {
                emailAddress = m_emailAddress;
            }
            if ( string.IsNullOrWhiteSpace( password ) &&
                 !string.IsNullOrWhiteSpace( m_password ) )
            {
                password = m_password;
            }

            bool isAuthorised = false;

            // Check if we have a valid email format and password
            if ( ValidateEmailPasswordText( emailAddress, password ) )
            {
                // If we have an email and a password, try and login.
                if ( !string.IsNullOrWhiteSpace( emailAddress ) && !string.IsNullOrWhiteSpace( password ) )
                {
                    // Long process, display our wait spinner
                    PART_WaitSpinner.Visibility = Visibility.Visible;

                    Debug.Assert( m_launcherWindow != null );
                    if ( m_launcherWindow != null )
                    {
                        CobraBayView cobraBayView = m_launcherWindow.GetCobraBayView();
                        Debug.Assert( cobraBayView != null );
                        if ( cobraBayView != null )
                        {
                            // The async bit
                            // Try and login, if we do then jump to the FrontPage,
                            // otherwise we will have to request the verification code
                            // from the user.

                            FORCManager manager = null;

                            await Task.Run( () =>
                            {
                                manager = cobraBayView.Manager();
                                Debug.Assert( manager != null );
                                
                                if ( manager != null )
                                {
                                    if ( m_requestNewLogon )
                                    {
                                        // A new logon has been requested, logout the current user
                                        manager.LogoutUser();
                                    }

                                    if ( !manager.Authorised || m_requestNewLogon )
                                    {
                                        manager.UserDetails.EmailAddress = emailAddress;
                                        manager.UserDetails.Password = password;
                                        manager.UserDetails.TwoFactor = "";
                                        manager.Authenticate( false );
                                        isAuthorised = manager.Authorised;

                                        LogEntry entry = new LogEntry("LoginSubmitted");
                                        entry.AddValue( "Authorised", isAuthorised ? "True" : "False" );
                                        manager.Log( entry );
                                    }
                                    else
                                    {
                                        isAuthorised = true;
                                    }
                                }
                            } );

                            // Is the user authorised
                            if ( isAuthorised )
                            {
                                // Perform any post login processing (link accounts etc)
                                PostUserIdentifiedProcess();
                            }
                            else
                            {
                                // See if the user needs to provide Two Factor authentication
                                if ( manager.RequiresTwoFactor )
                                {
                                    if ( _displayVerificationPage )
                                    {
                                        // Keep the email address and password, in case we
                                        // need to to request the verification code again
                                        m_emailAddress = emailAddress;
                                        m_password = password;
                                        // We need to do verification for this login
                                        m_launcherWindow.DisplayHomeVerificationPage( emailAddress, 
                                                                                      password,
                                                                                      this );
                                        EnableControls( true );
                                        PART_WaitSpinner.Visibility = Visibility.Hidden;
                                    }
                                }
                                else
                                {
                                    // We must have invalid credentials
                                    PART_WaitSpinner.Visibility = Visibility.Hidden;
                                    EnableControls( true );
                                    DisplayUserError( manager.Status );
                                }
                            }
                            handledLogin = true;
                        }
                    }
                }
            }

            // If we did not handle the login (missing login data etc)
            if ( !handledLogin )
            {
                EnableControls( true );
            }
        }

        /// <summary>
        /// Continues some of the process after a user has logged in.
        /// </summary>
        private void PostUserIdentifiedProcess()
        {
            Debug.Assert( m_launcherWindow != null );

            CobraBayView cobraBayView = m_launcherWindow.GetCobraBayView();
            Debug.Assert( cobraBayView != null );
            if ( cobraBayView != null )
            {
                FORCManager manager = cobraBayView.Manager();
                Debug.Assert( manager != null );
                if ( manager != null )
                {
                    if ( manager.Authorised )
                    {
                        // We either need to:
                        // Ask user to link accounts (if via store and account not linked).
                        // Ask the user to redeem a code if the user does not own elite.
                        // Just display the FrontPage (the main page).
                        if ( m_displayAccountLinkChoice )
                        {
                            // We need to prompt the user to link store and FD accounts.
                            m_launcherWindow.DisplayHomeFirstOpenUnlinkedPage();
                        }
                        else
                        {
                            if ( manager.ServerConnection.GetLastErrorCode() == c_DoesNotOwnEliteCode )
                            {
                                // User does not own elite
                                m_launcherWindow.DisplayHomeDoesNotOwnElitePage();
                            }
                            else
                            {
                                // We use an async method because this can be
                                // a lengthy process.
                                _ = m_launcherWindow.DisplayFrontPageAsync( );
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check for blank email and then blank password, display the first
        //  issue we find.
        /// </summary>
        /// <param name="_emailAddress">The email address to check</param>
        /// <param name="_password">The password to check</param>
        /// <returns>true if the email and password pass the basic validation checks</returns>
        private bool ValidateEmailPasswordText( string _emailAddress , string _password )
        {
            bool validated = true;

            if ( string.IsNullOrWhiteSpace( _emailAddress ) )
            {
                // No email address
                DisplayUserError( LocalResources.Properties.Resources.MissingEmailAddress );
                validated = false;
            }
            else if ( !Utils.IsEmailAddressFormatValid( _emailAddress ) )
            {
                // Invalid email address format
                DisplayUserError( LocalResources.Properties.Resources.MSG_InvalidEmailAddress );
                validated = false;
            }
            else if (string.IsNullOrWhiteSpace( _password ) )
            {
                // No password
                DisplayUserError( LocalResources.Properties.Resources.MissingPassword );
                validated = false;
            }

            return validated;
        }

        /// <summary>
        /// Displays a user error message
        /// </summary>
        /// <param name="_errorMessage">The error message to display</param>
        private void DisplayUserError( string _errorMessage )
        {
            // Log the issue
            LogEntry logEntry = new LogEntry( Consts.c_preLoginLogAction );
            logEntry.AddValue( "User error during login", _errorMessage );
            m_launcherWindow.Log( logEntry );

            // Display a note to the user
            PART_ErrorMessageTB.ErrorMessage = _errorMessage;
            PART_ErrorMessageTB.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Hides the user error message
        /// </summary>
        private void HideUserError()
        {
            PART_ErrorMessageTB.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Handles the Forgot Password click on a hyperlink.
        /// Starts the default web browser and navigates to the specified web page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PART_ForgotPasswordHL_RequestNavigate( object sender, RequestNavigateEventArgs e )
        {
            Debug.Assert( !string.IsNullOrWhiteSpace( e.Uri.AbsoluteUri ) );
            if ( !string.IsNullOrWhiteSpace( e.Uri.AbsoluteUri ) )
            {
                Process.Start( new ProcessStartInfo( e.Uri.AbsoluteUri ) );
            }
            e.Handled = true;
        }

        /// <summary>
        /// Handles the Signup click on a hyperlink.
        /// This hyperlink does not have a URI, but instead is used like a button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPART_CreateAccHLL_RequestNavigate( object sender, RequestNavigateEventArgs e )
        {
            Debug.Assert( m_launcherWindow != null );
            if ( m_launcherWindow != null )
            {
                m_launcherWindow.DisplayHomeCreateAccountPage( m_displayAccountLinkChoice );
            }
            e.Handled = true;
        }

        /// <summary>
        /// Returns the page title, part of the IVerification interface
        /// </summary>
        /// <returns>The page title</returns>
        public string PageTitle()
        {
            return LocalResources.Properties.Resources.LB_FrontierLogonTitle;
        }

        /// <summary>
        /// Part of the IVerification interface.
        /// Called to validate a code entered by the user 
        /// and perform any further processing.
        /// Should return true if okay, else false.
        /// This functionality is performed here because the validation
        /// of the code is Login specific.
        /// </summary>
        /// <param name="_email">The users email address</param>
        /// <param name="_password">The users password</param>
        /// <param name="_code">The verification code to check</param>
        /// <returns>true if the code was validated okay</returns>
        public bool ValidateCode( string _email,
                                  string _password,
                                  string _code )
        {
            return ValidateCode( _code );
        }


        /// <summary>
        /// Validates the verification code for logon
        /// </summary>
        /// <param name="_code">The code to validate</param>
        /// <returns>Returns true if the code was validated okay, else false</returns>
        private bool ValidateCode( string _code )
        {
            bool isAuthorised = false;

            if ( _code.Length >= c_MinNumberOfCharsInCode )
            {
                Debug.Assert( m_launcherWindow != null );
                if ( m_launcherWindow != null )
                {
                    CobraBayView cobraBayView = m_launcherWindow.GetCobraBayView();
                    Debug.Assert( cobraBayView != null );
                    if ( cobraBayView != null )
                    {
                        FORCManager manager = cobraBayView.Manager();

                        if ( !manager.Authorised )
                        {
                            // User has provided the verification code login information.
                            manager.UserDetails.TwoFactor = _code;
                            manager.Authenticate( false );
                            isAuthorised = manager.Authorised;

                            // Check to see if we failed because user does not own elite
                            if ( manager.ServerConnection.GetLastErrorCode() == c_DoesNotOwnEliteCode )
                            {
                                LogEntry entry = new LogEntry( "Validate Email Code Submitted" );
                                entry.AddValue( "Registered Name", manager.UserDetails.RegisteredName );
                                entry.AddValue( "Email", manager.UserDetails.EmailAddress );
                                entry.AddValue( "Denied Due To No Elite Ownership", "True" );
                                manager.Log( entry );
                            }
                            else
                            {
                                LogEntry entry = new LogEntry("Validate Email Code Submitted");
                                entry.AddValue( "Registered Name", manager.UserDetails.RegisteredName );
                                entry.AddValue( "Email", manager.UserDetails.EmailAddress );
                                entry.AddValue( "Authorised", manager.Authorised ? "True" : "False" );
                                manager.Log( entry );
                            }
                        }

                        PostUserIdentifiedProcess();
                    }
                }
            }

            return isAuthorised;
        }

        /// <summary>
        /// Part of the IVerification interface.
        /// The class should request a new code to be sent
        /// to the user.
        /// </summary>
        /// <returns>A developer code, but only if we are in developer mode, else null</returns>
        public string ResendCode()
        {
            bool displayVerificationPage = false;
            _ = LogInAsync( displayVerificationPage );
            return null;
        }

        /// <summary>
        /// Defines the code for User Does Not Own Elite
        /// </summary>
        private const int c_DoesNotOwnEliteCode = 257;

        // Our main Window. The main window is
        // responsible for changing the pages
        // displayed.
        private readonly LauncherWindow m_launcherWindow;

        /// <summary>
        /// The number of chars within the code
        /// </summary>
        private const int c_MinNumberOfCharsInCode = 5;

        /// <summary>
        /// Should we display the Link Account choice
        /// after we login and verify any codes?
        /// </summary>
        private bool m_displayAccountLinkChoice = false;

        /// <summary>
        /// The users email address
        /// </summary>
        private string m_emailAddress = null;

        /// <summary>
        /// The users password
        /// </summary>
        private string m_password = null;

        /// <summary>
        /// Forces the logon process to request a new login and ignore
        /// any existing ones.
        /// </summary>
        private bool m_requestNewLogon = false;

    }
}
