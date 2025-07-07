//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! HomeCreateAccountPage, allows a user to sign up for an FD account
//
//! Author:     Alan MacAree
//! Created:    22 Aug 2022
//----------------------------------------------------------------------

using CBViewModel;
using ClientSupport;
using FDUserControls;
using JSONConverters;
using LauncherModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for HomeCreateAccountPage.xaml
    /// </summary>
    public partial class HomeCreateAccountPage : Page, IVerification
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_launcherWindow">The LauncherWindow</param>
        /// <param name="_displayAccountLinkChoice">Should the account link option be displayed</param>
        public HomeCreateAccountPage( LauncherWindow _launcherWindow, bool _displayAccountLinkChoice )
        {
            InitializeComponent();

            Debug.Assert( _launcherWindow != null );

            m_launcherWindow = _launcherWindow;
            m_displayAccountLinkChoice = _displayAccountLinkChoice;

            m_launcherModelManager = new LauncherModelManager( m_launcherWindow.GetCobraBayView() );

            // Setup the password score indicator
            SetupPasswordScoreIndicator();
        }

        /// <summary>
        /// Called on Page Load
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPageLoaded( object sender, RoutedEventArgs e )
        {
            // Create our list of controls that need to be aligned with each other.
            List<EditBoxUserCtrl> editBoxUserCtrlList = new List<EditBoxUserCtrl>
            {
                PART_FirstName,
                PART_Email,
                PART_Password
            };

            // Make one of the controls the master by joining the other(s) to it.
            PART_PasswordConfirm.JoinAndAlignControls( editBoxUserCtrlList );

            // Make sure we know when the user changes text
            PART_FirstName.TextChangedEventHandler += OnFirstNameChanged;
            PART_LastName.TextChangedEventHandler += OnLastNameChanged;
            PART_Email.TextChangedEventHandler += OnEmailChanged;
            PART_Password.TextChangedEventHandler += OnPasswordChanged;
            PART_PasswordConfirm.TextChangedEventHandler += OnConfirmPasswordChanged;
        }

        /// <summary>
        /// Called when the page is unloaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPageUnloaded( object sender, RoutedEventArgs e )
        {
            // Unregister the events that we registered.
            PART_FirstName.TextChangedEventHandler -= OnFirstNameChanged;
            PART_LastName.TextChangedEventHandler -= OnLastNameChanged;
            PART_Email.TextChangedEventHandler -= OnEmailChanged;
            PART_Password.TextChangedEventHandler -= OnPasswordChanged;
            PART_PasswordConfirm.TextChangedEventHandler -= OnConfirmPasswordChanged;
        }

        /// <summary>
        /// Called when the FirstName changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFirstNameChanged( object sender, TextChangedEventArgs e )
        {
            m_isFirstNameValid = !string.IsNullOrWhiteSpace( PART_FirstName.TextBoxText );
            CheckStatusToEnableSignUp();
        }

        /// <summary>
        /// Called when the LastName changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLastNameChanged( object sender, TextChangedEventArgs e )
        {
            m_isLastNameValid = !string.IsNullOrWhiteSpace( PART_LastName.TextBoxText );
            CheckStatusToEnableSignUp();
        }

        /// <summary>
        /// Called when the Email changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnEmailChanged( object sender, TextChangedEventArgs e )
        {
            m_isEmailValid = false;

            m_homeCreateAccountErrorManager.RemoveError( HomeCreateAccountErrorManager.ErrorSourceAndPriority.Email );

            string email = PART_Email.TextBoxText;
            if ( !string.IsNullOrWhiteSpace( email ) )
            {
                m_isEmailValid = Utils.IsEmailAddressFormatValid( email );
                if ( !m_isEmailValid )
                {
                    m_homeCreateAccountErrorManager.StoreError( HomeCreateAccountErrorManager.ErrorSourceAndPriority.Email, LocalResources.Properties.Resources.MSG_InvalidEmailAddress );
                }
            }

            CheckStatusToEnableSignUp();
            UpdateWarningMessage();

        }

        /// <summary>
        /// Called when the password changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPasswordChanged( object sender, TextChangedEventArgs e )
        {
            m_doWeHavePasswordText = !string.IsNullOrWhiteSpace( PART_Password.Password() );
            string password = PART_Password.Password();

            // We put the check for checking the password on a debounce. This does two things:
            // 1) It reduces the impact on the UI responsiveness.
            // 2) It reduces the calls to check the password and reduces the load on the server.
            m_passwordDebouncer.Debounce( c_checkPasswordDebounceInMS, param =>
            {
                Task.Run( () =>
                {
                    GetPasswordScore( password );
                    Dispatcher.BeginInvoke( new Action( () =>
                    {
                        OnConfirmPasswordChanged( sender, e );
                    } ));
                }
                );
            } );
        }

        /// <summary>
        /// Called when the confirm password changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnConfirmPasswordChanged( object sender, TextChangedEventArgs e )
        {
            m_isConfirmPasswordValid = false;
            m_homeCreateAccountErrorManager.RemoveError( HomeCreateAccountErrorManager.ErrorSourceAndPriority.ConfirmPassword );

            if ( m_doWeHavePasswordText )
            {
                string password = PART_Password.Password();
                string confirmPassword = PART_PasswordConfirm.Password();
                if ( !string.IsNullOrWhiteSpace( confirmPassword ) )
                {
                    m_isConfirmPasswordValid = (password.CompareTo( confirmPassword ) == 0);

                    // Do we have a Password Mismatch (i.e. does the password and confirm password match).
                    if ( !m_isConfirmPasswordValid  )
                    {
                        m_homeCreateAccountErrorManager.StoreError( HomeCreateAccountErrorManager.ErrorSourceAndPriority.ConfirmPassword, LocalResources.Properties.Resources.TXT_PasswordsMismatch );
                    }
                }
            }

            UpdateWarningMessage();
            CheckStatusToEnableSignUp();
        }

        /// <summary>
        /// Determines if all the fields are valid, each field
        /// is checked as it is changed, so no real validation
        /// of the data occurs in this method, we are just 
        /// checking the bools that are set when each value
        /// is changed.
        /// </summary>
        /// <returns>true if all the fields are valid</returns>
        private bool AreAllFieldsCompleted()
        {
            return ( m_isFirstNameValid &&
                     m_isLastNameValid &&
                     m_isEmailValid &&
                     m_doWeHavePasswordText &&
                     m_isConfirmPasswordValid &&
                     m_passwordScoreAcceptable &&
                     PART_AgreeToTandCs.IsChecked == true );
        }

        /// <summary>
        /// Checks the status of all of the fields, enables the 
        /// Sign Up button if the data that has been entered 
        /// "looks" okay.
        /// </summary>
        private void CheckStatusToEnableSignUp()
        {
            if ( AreAllFieldsCompleted() )
            {
                PART_SignUpButton.IsEnabled = true;
                PART_SignUpButton.IsDefault = true;
            }
            else
            {
                PART_SignUpButton.IsEnabled = false;
                PART_SignUpButton.IsDefault = false;
            }
        }

        /// <summary>
        /// Handles the "Have a logon" hyperlink.
        /// This hyperlink does not have a real URI, but instead is used like a button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHaveLogonAccount( object sender, RequestNavigateEventArgs e )
        {
            Debug.Assert( m_launcherWindow != null );
            if ( m_launcherWindow != null )
            {
                bool _displayAccountLinkChoice = false;
                m_launcherWindow.DisplayHomeLoginPage( _displayAccountLinkChoice );
            }
            e.Handled = true;
        }

        /// <summary>
        /// Handles the "T&C" hyperlink.
        /// This hyperlink does not have a real URI, but instead is used like a button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTandC( object sender, RequestNavigateEventArgs e )
        {
            Debug.Assert( m_launcherWindow != null );
            if ( !string.IsNullOrWhiteSpace( e.Uri.ToString() ) )
            {
                try
                {
                    Process.Start( e.Uri.ToString() );
                }
                catch ( Exception ex )
                {
                    // Exception whist opening T&C link, log it
                    if ( m_launcherWindow != null )
                    {
                        m_launcherWindow.LogEvent( "Accessing T&Cs", e.Uri.ToString(), ex.ToString() );
                    }
                }
            }
            else
            {
                // The T&C link is empty, this should not happen, log it
                if ( m_launcherWindow != null )
                {
                    m_launcherWindow.LogEvent( "Accessing T&Cs", "T&C link is empty", e.Uri.ToString() );
                }
            }

            e.Handled = true;
        }

        /// <summary>
        /// Handles the "Privacy Policy" hyperlink.
        /// This hyperlink does not have a real URI, but instead is used like a button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPrivaryPolicy( object sender, RequestNavigateEventArgs e )
        { 
            Debug.Assert( m_launcherWindow != null );
            if ( !string.IsNullOrWhiteSpace( e.Uri.ToString() ) )
            {
                try
                {
                    Process.Start( e.Uri.ToString() );
                }
                catch ( Exception ex )
                {
                    // Exception whist opening T&C link, log it
                    if ( m_launcherWindow != null )
                    {
                        m_launcherWindow.LogEvent( "Accessing Privacy Policy", e.Uri.ToString(), ex.ToString() );
                    }
                }
            }
            else
            {
                // The Privacy Policy link is empty, this should not happen, log it
                if ( m_launcherWindow != null )
                {
                    m_launcherWindow.LogEvent( "Accessing T&Cs", "Privacy Policy link is empty", e.Uri.ToString() );
                }
            }
            e.Handled = true;
        }

        /// <summary>
        /// Called when the user clicks on SignUp
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSignUp( object sender, RoutedEventArgs e )
        {
            ClearTheWarningMessage();
            HideErrorMessageControls();

            _ = SignUpAsync();
        }

        /// <summary>
        /// Hides the error message controls.
        /// </summary>
        /// <param name="_ErrorMessageControl">Defines which error message ctrl should be hidden, defaults to hide all error
        /// message controls</param>
        private void HideErrorMessageControls( ErrorMessageControls _ErrorMessageControl = ErrorMessageControls.AllErrorMessagesCtrls )
        {
            switch ( _ErrorMessageControl )
            {
                case ErrorMessageControls.EmailErrorMessageCtrl:
                    PART_EmailErrorCtrl.Visibility = Visibility.Hidden;
                    break;
                case ErrorMessageControls.PasswordErrorMessageCtrl:
                    PART_PasswordErrorCtrl.Visibility = Visibility.Hidden;
                    break;
                case ErrorMessageControls.ConfirmPasswordErrorMessageCtrl:
                    PART_ConfirmPasswordErrorCtrl.Visibility = Visibility.Hidden;
                    break; 
                case ErrorMessageControls.AllErrorMessagesCtrls:
                    PART_EmailErrorCtrl.Visibility = Visibility.Hidden;
                    PART_PasswordErrorCtrl.Visibility = Visibility.Hidden;
                    PART_ConfirmPasswordErrorCtrl.Visibility = Visibility.Hidden;
                    break;
                default:
                    // We should not get here, if we do then someone has added a new enum to 
                    // ErrorMessage and has not dealt with the possibility in the switch statement.
                    Debug.Assert( false );
                    break;
            }
        }

        /// <summary>
        /// Displays a Error Message Ctrl based on the passed ErrorMessageControls.
        /// </summary>
        /// <param name="_ErrorMessageControl">The error message ctrl to display, defaults to all.</param>
        private void ShowErrorMessageControl( ErrorMessageControls _ErrorMessageControl = ErrorMessageControls.AllErrorMessagesCtrls )
        {
            switch ( _ErrorMessageControl )
            {
                case ErrorMessageControls.EmailErrorMessageCtrl:
                    PART_EmailErrorCtrl.Visibility = Visibility.Visible;
                    break;
                case ErrorMessageControls.PasswordErrorMessageCtrl:
                    PART_PasswordErrorCtrl.Visibility = Visibility.Visible;
                    break;
                case ErrorMessageControls.ConfirmPasswordErrorMessageCtrl:
                    PART_ConfirmPasswordErrorCtrl.Visibility = Visibility.Visible;
                    break;
                case ErrorMessageControls.AllErrorMessagesCtrls:
                    PART_EmailErrorCtrl.Visibility = Visibility.Visible;
                    PART_PasswordErrorCtrl.Visibility = Visibility.Visible;
                    PART_ConfirmPasswordErrorCtrl.Visibility = Visibility.Visible;
                    break;
                default:
                    // We should not get here, if we do then someone has added a new enum to 
                    // ErrorMessage and has not dealt with the possibility in the switch statement.
                    Debug.Assert( false );
                    break;
            }
        }

        /// <summary>
        /// Called to Signup, or to get the verification code regenerated and sent to the user again.
        /// </summary>
        /// <returns>A developer code, but only if we are in developer mode, else null</returns>
        private async Task<string> SignUpAsync( bool _displayVerificationPage = true )
        {
            Debug.Assert( m_launcherWindow != null );

            EnableUI( false );
            PART_WaitSpinner.Visibility = Visibility.Visible;

            string developerVerificationCode = null;

            if ( m_launcherWindow != null )
            {
                CobraBayView cobraBayView = m_launcherWindow.GetCobraBayView();

                Debug.Assert( cobraBayView != null );
                if ( cobraBayView != null )
                {
                    FORCManager fORCManager = cobraBayView.Manager();

                    Debug.Assert( fORCManager != null );
                    if ( fORCManager != null )
                    {
                        string firstName = PART_FirstName.TextBoxText;
                        string lastName = PART_LastName.TextBoxText;
                        string email = PART_Email.TextBoxText;
                        string password = PART_Password.Password();
                        string passwordConfirm = PART_PasswordConfirm.Password();
                        bool newsAndPromoSignUp = (bool)PART_NewsAndPromoSignUp.IsChecked;

                        // We are either signing up or asking for a new code.
                        // If we are signing up, save the password, if we are
                        // asking for a new code, the password & passwordConfirm
                        // will be empty and m_password will have the password.
                        if ( string.IsNullOrWhiteSpace( password ) &&
                             string.IsNullOrWhiteSpace( passwordConfirm ) &&
                             !string.IsNullOrWhiteSpace( m_password ) )
                        {
                            password = m_password;
                            passwordConfirm = m_password;
                        }
                        else
                        {
                            m_password = password;
                        }

                        Debug.Assert( !string.IsNullOrWhiteSpace(firstName) );
                        Debug.Assert( !string.IsNullOrWhiteSpace(lastName) );
                        Debug.Assert( !string.IsNullOrWhiteSpace(email) );
                        Debug.Assert( !string.IsNullOrWhiteSpace(password) );
                        Debug.Assert( !string.IsNullOrWhiteSpace(passwordConfirm) );

                        if ( !string.IsNullOrWhiteSpace(firstName) &&
                             !string.IsNullOrWhiteSpace(lastName) &&
                             !string.IsNullOrWhiteSpace(email) &&
                             !string.IsNullOrWhiteSpace(password) &
                             !string.IsNullOrWhiteSpace(passwordConfirm))
                        {
                            JSONWebPutsAndPostsResult jsonWebPostResult = null;

                            await Task.Run(() =>
                            {
                                jsonWebPostResult = fORCManager.CreateFrontierAccount( firstName,
                                                                                       lastName,
                                                                                       email,
                                                                                       password,
                                                                                       passwordConfirm,
                                                                                       newsAndPromoSignUp);
                                if ( jsonWebPostResult != null )
                                {
                                    if ( jsonWebPostResult.HttpStatusResult != HttpStatusCode.OK &&
                                         jsonWebPostResult.HttpStatusResult != HttpStatusCode.Created )
                                    {
                                        // We add a delay here to stop the user from spamming the server with incorrect info.
                                        Thread.Sleep( c_signUpErrorDelayInMS );
                                    }
                                }

                            });

                            developerVerificationCode = CheckSignUpResult( jsonWebPostResult,
                                                                           email,
                                                                           password,
                                                                           _displayVerificationPage);
                        }
                    }
                }
            }

            PART_WaitSpinner.Visibility = Visibility.Hidden;
            EnableUI( true );

            return developerVerificationCode;
        }

        /// <summary>
        /// Enables or disables the controls on the UI, the SignUp button will not enable if we
        /// have any errors on the UI.
        /// </summary>
        /// <param name="_enable">If true then controls will be enabled, else they are disabled.</param>
        private void EnableUI( bool _enable = true )
        {
            PART_FirstName.IsEnabled = _enable;
            PART_LastName.IsEnabled = _enable;
            PART_Email.IsEnabled = _enable;
            PART_Password.IsEnabled = _enable;
            PART_PasswordConfirm.IsEnabled = _enable;
            PART_NewsAndPromoSignUp.IsEnabled = _enable;
            PART_AgreeToTandCs.IsEnabled = _enable;
            PART_AgreeToTandCs.IsEnabled = _enable;
            PART_HaveAnAccount.IsEnabled = _enable;

            // We do not enable this button if we have any errors
            if ( m_homeCreateAccountErrorManager.IsEmpty() )
            {
                PART_SignUpButton.IsEnabled = _enable;
            }
        }

        /// <summary>
        /// Resends the Signup, or to get the verification code regenerated and sent to the user again.
        /// </summary>
        /// <returns>A developer code, but only if we are in developer mode, else null</returns>
        private string ResendSignUp( bool _displayVerificationPage = true )
        {
            Debug.Assert( m_launcherWindow != null );

            string developerVerificationCode = null;

            if ( m_launcherWindow != null )
            {
                CobraBayView cobraBayView = m_launcherWindow.GetCobraBayView();

                Debug.Assert( cobraBayView != null );
                if ( cobraBayView != null )
                {
                    FORCManager fORCManager = cobraBayView.Manager();

                    Debug.Assert( fORCManager != null );
                    if ( fORCManager != null )
                    {
                        string firstName = PART_FirstName.TextBoxText;
                        string lastName = PART_LastName.TextBoxText;
                        string email = PART_Email.TextBoxText;
                        string password = PART_Password.Password();
                        string passwordConfirm = PART_PasswordConfirm.Password();
                        bool newsAndPromoSignUp = (bool)PART_NewsAndPromoSignUp.IsChecked;

                        // We are either signing up or asking for a new code.
                        // If we are signing up, save the password, if we are
                        // asking for a new code, the password & passwordConfirm
                        // will be empty and m_password will have the password.
                        if ( string.IsNullOrWhiteSpace( password ) &&
                             string.IsNullOrWhiteSpace( passwordConfirm ) &&
                             !string.IsNullOrWhiteSpace( m_password ) )
                        {
                            password = m_password;
                            passwordConfirm = m_password;
                        }
                        else
                        {
                            m_password = password;
                        }

                        Debug.Assert( !string.IsNullOrWhiteSpace( firstName ) );
                        Debug.Assert( !string.IsNullOrWhiteSpace( lastName ) );
                        Debug.Assert( !string.IsNullOrWhiteSpace( email ) );
                        Debug.Assert( !string.IsNullOrWhiteSpace( password ) );
                        Debug.Assert( !string.IsNullOrWhiteSpace( passwordConfirm ) );

                        if ( !string.IsNullOrWhiteSpace( firstName ) &&
                             !string.IsNullOrWhiteSpace( lastName ) &&
                             !string.IsNullOrWhiteSpace( email ) &&
                             !string.IsNullOrWhiteSpace( password ) &
                             !string.IsNullOrWhiteSpace( passwordConfirm ) )
                        {
                            JSONWebPutsAndPostsResult jsonWebPostResult = fORCManager.CreateFrontierAccount( firstName,
                                                                                                 lastName,
                                                                                                 email,
                                                                                                 password,
                                                                                                 passwordConfirm,
                                                                                                 newsAndPromoSignUp );
                            developerVerificationCode = CheckSignUpResult( jsonWebPostResult,
                                                                           email,
                                                                           password,
                                                                           _displayVerificationPage );
                        }
                    }
                }
            }

            return developerVerificationCode;
        }

        /// <summary>
        /// Handles the sign up result and decides what to do next.
        /// </summary>
        /// <param name="_jsonWebPostResult">The result of the signup call</param>
        /// <param name="_email">The users email address</param>
        /// <param name="_password">The users password</param>
        /// <param name="_displayVerificationPage">Should the verification page be displayed or not</param>
        /// <returns>A developer code, but only if we are in developer mode, else null</returns>
        private string CheckSignUpResult( JSONWebPutsAndPostsResult _jsonWebPostResult,
                                          string _email,
                                          string _password,
                                          bool _displayVerificationPage )
        {
            Debug.Assert( _jsonWebPostResult != null );

            string developerVerificationCode = null;

            if ( _jsonWebPostResult != null )
            {
                switch ( _jsonWebPostResult.HttpStatusResult )
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.Created:
                        // All is good, continue the signup process
                        developerVerificationCode = ContinueSignUpProcess( _jsonWebPostResult, _email, _password, _displayVerificationPage );
                        break;
                    default:
                        HandleSignUpError( _jsonWebPostResult, _email );
                        break;
                }
            }

            return developerVerificationCode;
        }

        /// <summary>
        /// Continues the Signup process after the initial API call to
        /// create the user account.
        /// </summary>
        /// <param name="_jsonWebPostResult">The result of the sign up API call</param>
        /// <param name="_email">The email address used in the signup</param>
        /// <param name="_password">The password used in the signup</param>
        /// <param name="_displayVerificationPage">Shsould we display the verification page</param>
        /// <returns>A developer code, or null if not in Developer mode</returns>
        private string ContinueSignUpProcess( JSONWebPutsAndPostsResult _jsonWebPostResult,
                                              string _email,
                                              string _password,
                                              bool _displayVerificationPage )
        {
            Debug.Assert( _jsonWebPostResult != null );

            string developerVerificationCode = null;

            Dictionary<string, string> httpResponseDictionary = _jsonWebPostResult.HttpResponseDictionary;

#if DEVELOPMENT || DEBUG
            // See if the reply has a developer code included
            Debug.Assert( httpResponseDictionary != null );
            if ( httpResponseDictionary != null )
            {
                _ = httpResponseDictionary.TryGetValue( c_developerCodeResonse, out developerVerificationCode );
            }
#endif

            if ( _displayVerificationPage )
            {
                m_launcherWindow.DisplayHomeVerificationPage( _email, _password, this, developerVerificationCode );
            }

            return developerVerificationCode;
        }

        /// <summary>
        /// Handles an identified signup error
        /// </summary>
        /// <param name="_jsonWebPostResult">The results containing details of the error</param>
        /// <param name="_email">The email that was being used</param>
        private void HandleSignUpError( JSONWebPutsAndPostsResult _jsonWebPostResult, string _email )
        {
            if ( _jsonWebPostResult != null )
            {
                switch ( _jsonWebPostResult.HttpStatusResult )
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.Created:
                        // All is good, so why are we here in an error method?
                        Debug.Assert( false );
                        break;
                    case HttpStatusCode.Unauthorized:
                        // Can't confirm the user's platform
                        if ( m_launcherWindow != null )
                        {
                            string title = LocalResources.Properties.Resources.ErrorTitle;
                            string msg = this.GetType().Name + " : " +
                                         MethodBase.GetCurrentMethod().Name +
                                         " : HttpStatusCode.Unauthorized for email " + _email;
                            string subMsg = LocalResources.Properties.Resources.BTNT_ContactSupport;
                            m_launcherWindow.LogEvent( title, msg, subMsg );
                            m_launcherWindow.DisplayPopupPage( this, title, msg, subMsg );
                        }
                        break;
                    case HttpStatusCode.Forbidden:
                        // Email address is in use
                        m_homeCreateAccountErrorManager.StoreError( HomeCreateAccountErrorManager.ErrorSourceAndPriority.Email, LocalResources.Properties.Resources.TXT_EmailAddressInUse );
                        m_launcherWindow.LogEvent( this.GetType().Name + " : " + MethodBase.GetCurrentMethod().Name,
                                                   "Error Creating User Account for email " + _email,
                                                   _jsonWebPostResult.HttpStatusResult.ToString() );
                        break;
                    case HttpStatusCode.PreconditionFailed:
                        // Passwords do not match 
                        ShowErrorMessageControl( ErrorMessageControls.AllErrorMessagesCtrls );

                        m_launcherWindow.LogEvent( this.GetType().Name + " : " + MethodBase.GetCurrentMethod().Name,
                                                   "Error Creating User Account for email " + _email,
                                                   _jsonWebPostResult.HttpStatusResult.ToString() );
                        break;
                    case (HttpStatusCode)c_specificationNotMatched:
                        // Request does not match specification 
                        m_launcherWindow.LogEvent( this.GetType().Name + " : " + MethodBase.GetCurrentMethod().Name,
                                                   "Error Creating User Account for email " + _email,
                                                   _jsonWebPostResult.HttpStatusResult.ToString() );
                        break;
                    default:
                        // Unexpected response
                        if ( m_launcherWindow != null )
                        {
                            string title = LocalResources.Properties.Resources.ErrorTitle;
                            string msg = this.GetType().Name + " : " + 
                                         MethodBase.GetCurrentMethod().Name + " : " + 
                                         _jsonWebPostResult.HttpStatusResult.ToString() + 
                                         " : email = " +  _email;
                            string subMsg = LocalResources.Properties.Resources.BTNT_ContactSupport;
                            m_launcherWindow.LogEvent( title, msg, subMsg );
                            m_launcherWindow.DisplayPopupPage( this, title, msg, subMsg );
                        }
                        break;
                }
            }
            UpdateWarningMessage();
        }

        /// <summary>
        /// Returns the page title, part of the IVerification interface
        /// </summary>
        /// <returns>The page title</returns>
        public string PageTitle()
        {
            return LocalResources.Properties.Resources.TITLE_CreateFrontierAccount;
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
            bool codeValidationResult = false;

            codeValidationResult = ValidateCodeAsync( _email, _password, _code );

            return codeValidationResult;
        }

        /// <summary>
        /// Async method to validate the verification code
        /// </summary>
        /// <param name="_email">The users email address</param>
        /// <param name="_password">The users password</param>
        /// <param name="_code">The verification code to check</param>
        /// <returns>true if the code was validated okay</returns>
        private bool ValidateCodeAsync( string _email,
                                        string _password,
                                        string _code )
        {
            bool isVerified = false;

            if ( _code.Length >= c_NumberOfCharsInCode )
            {
                Debug.Assert( m_launcherWindow != null );
                if ( m_launcherWindow != null )
                {
                    CobraBayView cobraBayView = m_launcherWindow.GetCobraBayView();
                    Debug.Assert( cobraBayView != null );
                    if ( cobraBayView != null )
                    {
                        FORCManager fORCManager = cobraBayView.Manager();
                        string machineId = fORCManager.MachineIdentifier.GetMachineIdentifier();
                        Debug.Assert( m_launcherWindow != null );

                        Debug.Assert( fORCManager != null );
                        if ( fORCManager != null )
                        {
                            JSONWebPutsAndPostsResult jsonWebPostResult =
                                fORCManager.ConfirmFrontierAccount( _email,
                                                                    _password,
                                                                    _code,
                                                                    machineId);
                            if ( jsonWebPostResult != null )
                            {
                                // Was the code verified?
                                isVerified = (jsonWebPostResult.HttpStatusResult == HttpStatusCode.OK);

                                if ( isVerified )
                                {
                                    StoreAuthenticationTokens( jsonWebPostResult );
                                    StoreUserDetails( _email, _password, jsonWebPostResult );
                                }
                            }
                        }
                    }
                    PostUserIdentifiedProcess();
                }
            }

            return isVerified;
        }

        /// <summary>
        /// Continues the process after a user has logged in.
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
                        if ( m_displayAccountLinkChoice )
                        {
                            m_launcherWindow.DisplayHomeFirstOpenUnlinkedPage();
                        }
                        else
                        {
                            if ( manager.ServerConnection.GetLastErrorCode() == c_DoesNotOwnEliteCode )
                            {
                                m_launcherWindow.DisplayHomeDoesNotOwnElitePage();
                            }
                            else
                            {
                                // We use an Async method because this can be
                                // a lengthy process.
                                _ = m_launcherWindow.DisplayFrontPageAsync();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Stores the authentication tokens returned as part of a ConfirmFrontierAccount call 
        /// </summary>
        /// <param name="_jsonWebPostResult">A JSONWebPutsAndPostsResult containing the authentication tokens</param>
        private void StoreAuthenticationTokens( JSONWebPutsAndPostsResult _jsonWebPostResult )
        {
            Debug.Assert( _jsonWebPostResult != null );
            Debug.Assert( _jsonWebPostResult.HttpStatusResult == HttpStatusCode.OK );
            Debug.Assert( _jsonWebPostResult.HttpResponseDictionary != null );

            if ( _jsonWebPostResult != null )
            {
                if ( _jsonWebPostResult.HttpStatusResult == HttpStatusCode.OK &&
                    _jsonWebPostResult.HttpResponseDictionary != null )
                {
                    string authorisationToken = null;
                    string machineToken = null;
                    _jsonWebPostResult.HttpResponseDictionary.TryGetValue( c_authToken, out authorisationToken );
                    _jsonWebPostResult.HttpResponseDictionary.TryGetValue( c_machineToken, out machineToken );

                    if ( m_launcherWindow != null )
                    {
                        CobraBayView cobraBayView = m_launcherWindow.GetCobraBayView();

                        Debug.Assert( cobraBayView != null );
                        if ( cobraBayView != null )
                        {
                            FORCManager forcManager = cobraBayView.Manager();

                            Debug.Assert( forcManager != null );
                            if ( forcManager != null )
                            {
                                Debug.Assert( authorisationToken != null );
                                if ( authorisationToken != null )
                                {
                                    forcManager.UserDetails.SessionToken = authorisationToken;
                                }
                                Debug.Assert( machineToken != null );
                                if ( machineToken != null )
                                {
                                    forcManager.UserDetails.AuthenticationToken = machineToken;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Stores the users details
        /// </summary>
        /// <param name="_email">The email to store</param>
        /// <param name="_password">The password to store</param>
        /// <param name="_jsonWebPostResult">The JSONWebPutsAndPostsResult object to get the RegisteredName from</param>
        private void StoreUserDetails( string _email, string _password, JSONWebPutsAndPostsResult _jsonWebPostResult )
        {
            Debug.Assert( _jsonWebPostResult != null );
            Debug.Assert( _jsonWebPostResult.HttpStatusResult == HttpStatusCode.OK );
            Debug.Assert( _jsonWebPostResult.HttpResponseDictionary != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( _email ) );
            Debug.Assert( !string.IsNullOrWhiteSpace( _password ) );

            if ( m_launcherWindow != null )
            {
                CobraBayView cobraBayView = m_launcherWindow.GetCobraBayView();

                Debug.Assert( cobraBayView != null );
                if ( cobraBayView != null )
                {
                    FORCManager forcManager = cobraBayView.Manager();

                    Debug.Assert( forcManager != null );
                    if ( forcManager != null )
                    {
                        UserDetails userDetails = forcManager.UserDetails;

                        Debug.Assert( userDetails != null );
                        if ( userDetails != null )
                        {
                            userDetails.EmailAddress = _email;
                            userDetails.Password = _password;

                            string registeredName = null;
                            _jsonWebPostResult.HttpResponseDictionary.TryGetValue( c_registeredName, out registeredName );

                            Debug.Assert( !string.IsNullOrWhiteSpace( registeredName ) );
                            if ( !string.IsNullOrWhiteSpace( registeredName ) )
                            {
                                forcManager.UserDetails.RegisteredName = registeredName;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Part of the IVerification interface.
        /// The class should request a new code to be sent
        /// to the user.
        /// </summary>
        /// <returns>A developer code, but only if we are in developer mode, else null</returns>
        public string ResendCode()
        {
            // Make sure we do not redisplay the verification page
            bool displayVerificationPage = false;
            return ResendSignUp( displayVerificationPage );
        }

        /// <summary>
        /// Called when the user clicks on Terms and Conditions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPART_AgreeToTandCsClick( object sender, RoutedEventArgs e )
        {
            CheckStatusToEnableSignUp();
        }

        /// <summary>
        /// Called when the mouse enters the T and C's and Privacy Policy.
        /// This makes sure all of the sub elements are changed to the same
        /// Colours.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPART_AgreeToTandCsMouseEnter( object sender, System.Windows.Input.MouseEventArgs e )
        {
            // Try and set the colour of the foreground for all of the elements within the 
            // T&C and Privacy Policy checkbox. This is done because this checkbox is made
            // up of TextBlocks and Hyperlinks, without this, each sub-element would try
            // and handle its own mouse over, and you end up with one control one colour
            // and another a different colour.
            try
            {
                m_currentTandCTextColour = (SolidColorBrush)Application.Current.Resources[c_hoverTextColorStr];
                // If this is disabled, then don't do anything except record the current colour
                if ( PART_AgreeToTandCs.IsEnabled )
                {
                    PART_AgreeToTandCs.Foreground = m_currentTandCTextColour;
                    PART_PPHL.Foreground = m_currentTandCTextColour;
                    PART_TCHL.Foreground = m_currentTandCTextColour;
                }
            }
            catch( Exception )
            {
                Debug.Assert( false );
            }
        }

        /// <summary>
        /// Called when the mouse leaves the T and C's and Privacy Policy.
        /// This makes sure all of the sub elements are changed to the same
        /// colours.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPART_AgreeToTandCsMouseLeave( object sender, System.Windows.Input.MouseEventArgs e )
        {
            // Try and set the colour of the foreground for all of the elements within the 
            // T&C and Privacy Policy checkbox. This is done because this checkbox is made
            // up of TextBlocks and Hyperlinks, without this, each sub-element would try
            // and handle its own mouse over, and you end up with one control one colour
            // and another a different colour.
            try
            {
                m_currentTandCTextColour = (SolidColorBrush)Application.Current.Resources[c_normalTextColorStr];
                // If this is disabled, then don't do anything except record the current colour
                if (PART_AgreeToTandCs.IsEnabled)
                {
                    PART_AgreeToTandCs.Foreground = m_currentTandCTextColour;
                    PART_PPHL.Foreground = m_currentTandCTextColour;
                    PART_TCHL.Foreground = m_currentTandCTextColour;
                }
            }
            catch ( Exception )
            {
                Debug.Assert( false );
            }
        }

        /// <summary>
        /// Called when the IsEnabled property is changed on the TandCs checkbox. 
        /// This checkbox is made up of several controls, and as we change the 
        /// colour ourself with mouse over and mouse leave, means we also have to
        /// deal with the disabled colour. 
        /// 
        /// If we are going from disabled to enabled, we must use the current colour
        /// which is set via the mouse over and mouse leave methods.
        /// </summary>
        /// <param name="sender">What sent this</param>
        /// <param name="e">The event args</param>
        private void OnPART_AgreeToTandCsIsEnabledChanged( object sender, DependencyPropertyChangedEventArgs e )
        {
            if ( PART_AgreeToTandCs.IsEnabled )
            {
                PART_AgreeToTandCs.Foreground = m_currentTandCTextColour;
                PART_PPHL.Foreground = m_currentTandCTextColour;
                PART_TCHL.Foreground = m_currentTandCTextColour;
            }
            else
            {
                SolidColorBrush disabledColour = new SolidColorBrush(SystemColors.ControlDarkColor);

                PART_AgreeToTandCs.Foreground = disabledColour;
                PART_PPHL.Foreground = disabledColour;
                PART_TCHL.Foreground = disabledColour;
            }
        }

        /// <summary>
        /// Sets up the password score indicator
        /// </summary>
        private void SetupPasswordScoreIndicator()
        {
            PART_PasswordStrength.Maximum = c_maxPasswordScore;
            PART_PasswordStrength.Value = c_minPasswordScore;
        }

        /// <summary>
        /// Gets a password score based on a password
        /// </summary>
        /// <param name="_password">The password to get the score for</param>
        private void GetPasswordScore( string _password )
        {
            Debug.Assert( m_launcherWindow != null );

            PasswordScoreAndFeedback passwordScore = null;

            if ( !string.IsNullOrWhiteSpace( _password ) )
            {
                if ( m_launcherModelManager != null )
                {
                    passwordScore = m_launcherModelManager.GetPasswordScore( _password );
                }
            }

            UpdatePasswordStrengthUserFeedback( passwordScore );
        }

        /// <summary>
        /// Sets the current password score indicator value. If we have
        /// an acceptable score, we set the password strength value to max.
        /// </summary>
        /// <param name="_passwordScore">The password score</param>
        private void UpdatePasswordStrengthUserFeedback( PasswordScoreAndFeedback _passwordScore )
        {
            Dispatcher.BeginInvoke( new Action( () =>
            {
                m_homeCreateAccountErrorManager.RemoveError( HomeCreateAccountErrorManager.ErrorSourceAndPriority.Password );

                string warningMessage = string.Empty;

                if ( _passwordScore != null )
                {
                    m_passwordScoreAcceptable = _passwordScore.Pass;

                    if ( m_passwordScoreAcceptable )
                    {
                        // Password is acceptable
                        PART_PasswordStrength.Foreground = new SolidColorBrush( m_acceptedPasswordColour );
                    }
                    else
                    {
                        // Password is not acceptable
                        ShowErrorMessageControl( ErrorMessageControls.PasswordErrorMessageCtrl );
                        PART_PasswordStrength.Foreground = new SolidColorBrush( m_unacceptedPasswordColour );
                        m_homeCreateAccountErrorManager.StoreError( HomeCreateAccountErrorManager.ErrorSourceAndPriority.Password, GetPasswordFeedbackText( _passwordScore ) );
                    }
                    PART_PasswordStrength.Value = c_maxPasswordScore;

                    CheckStatusToEnableSignUp();
                }
                else
                {
                    // No password score, set it as if the score is zero
                    m_passwordScoreAcceptable = false;
                    PART_PasswordStrength.Value = c_minPasswordScore;
                }

                // Update the warning message displayed 
                UpdateWarningMessage();
            } ) );
        }

        /// <summary>
        /// Gets the warning (if password did not pass the password check), and password suggestions 
        /// from the passed _passwordScore, and returns a localised version of the warning and
        /// suggestions as a space delimited string. 
        /// 
        /// This will not allow the return string to go over c_passwordFeedBackMaxChars, and will
        /// ditch suggestions if by adding a suggestion causes more than c_passwordFeedBackMaxChars chars.
        /// This ensures we never try and display strings in a place we do not have space for.
        /// </summary>
        /// <param name="_passwordScore">The PasswordScoreAndFeedback object that contains the feedback for a password</param>
        /// <returns>A localised string representing the feedback regarding a password. This can be empty.</returns>
        private string GetPasswordFeedbackText( PasswordScoreAndFeedback _passwordScore )
        {
            string result = string.Empty;

            // We add the warning if the password did not pass the password check that the _passwordScore holds the 
            // results for.
            if ( !_passwordScore.Pass )
            {
                // Get the localised string from the code.
                result = LocalisationHelper.PasswordCodeToLocalisedString( _passwordScore.FeedBack.WarningCode );

                // Add the suggestions
                foreach ( string suggestionCode in _passwordScore.FeedBack.SuggestionCodes )
                {
                    if ( !string.IsNullOrWhiteSpace( suggestionCode ) )
                    {
                        string suggestion = string.Empty;

                        // Do we need to add a space between feedback strings?
                        if ( result.Length > 0 )
                        {
                            suggestion = c_passwordFeedbackDelimiter;
                        }

                        // Get the localised string from the code.
                        suggestion += LocalisationHelper.PasswordCodeToLocalisedString( suggestionCode );

                        // Make sure we do not blow the limit to the number of chars we can display
                        if ( suggestion.Length + result.Length <= c_passwordFeedBackMaxChars )
                        {
                            result += suggestion;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Displays a warning message to the user. Intended to give the user feedback regarding
        /// invalid data that has been entered. If null or empty, the warning message is cleared from
        /// the UI.
        /// </summary>
        /// <param name="_warningMessageToDisplay">The text to display as the warning, can be null or empty</param>
        private void DisplayWarningMessage( string _warningMessageToDisplay )
        {
            if ( !string.IsNullOrWhiteSpace(_warningMessageToDisplay) )
            {
                PART_WarningMessage.Text = _warningMessageToDisplay;
            }
            else
            {
                ClearTheWarningMessage();
            }
        }

        /// <summary>
        /// Clears the warning message from the UI.
        /// </summary>
        private void ClearTheWarningMessage()
        {
            PART_WarningMessage.Text = string.Empty;
        }

        /// <summary>
        /// Updates the error ctrls and error message that is displayed to the user.
        /// </summary>
        private void UpdateWarningMessage()
        {
            if ( !m_homeCreateAccountErrorManager.IsEmpty() )
            {
                DisplayWarningMessage( m_homeCreateAccountErrorManager.GetHighestPriorityErrorString() );

                if ( m_homeCreateAccountErrorManager.DoesContainErrorSourceAndPriority( HomeCreateAccountErrorManager.ErrorSourceAndPriority.Email) )
                {
                    ShowErrorMessageControl( ErrorMessageControls.EmailErrorMessageCtrl );
                }
                else
                {
                    HideErrorMessageControls( ErrorMessageControls.EmailErrorMessageCtrl );
                }

                if ( m_homeCreateAccountErrorManager.DoesContainErrorSourceAndPriority( HomeCreateAccountErrorManager.ErrorSourceAndPriority.Password ) )
                {
                    ShowErrorMessageControl( ErrorMessageControls.PasswordErrorMessageCtrl );
                }
                else
                {
                    HideErrorMessageControls( ErrorMessageControls.PasswordErrorMessageCtrl );
                }

                if ( m_homeCreateAccountErrorManager.DoesContainErrorSourceAndPriority( HomeCreateAccountErrorManager.ErrorSourceAndPriority.ConfirmPassword ) )
                {
                    ShowErrorMessageControl( ErrorMessageControls.ConfirmPasswordErrorMessageCtrl );
                }
                else
                {
                    HideErrorMessageControls( ErrorMessageControls.ConfirmPasswordErrorMessageCtrl );
                }
            }
            else
            {
                ClearTheWarningMessage();
                HideErrorMessageControls();
            }
        }

        /// <summary>
        /// We have to save the password, because the PasswordBox clears
        /// the password when it is unloaded, and we may have to resend
        /// the request to the server with th password in order to get 
        /// a new code sent to the user.
        /// </summary>
        private string m_password;

        /// <summary>
        /// Used to keep track if the password score (complexity) is acceptable 
        /// or not.
        /// </summary>
        private bool m_passwordScoreAcceptable = false;

        /// <summary>
        /// This is the max password score we can have, but note that the
        /// passwordscore returned from the servers starts at 0 to 5, we use
        /// 6 because we always add one to the score returned from the servers.
        /// </summary>
        private const int c_maxPasswordScore = 6;

        /// <summary>
        /// This is the min password score we can have.
        /// </summary>
        private const int c_minPasswordScore = 0;

        /// <summary>
        /// The name of the HoverTextColour within the Checkbox Style.
        /// This must match the Checkbox style colour name.
        /// </summary>
        private const string c_hoverTextColorStr = "HoverTextColour";

        /// <summary>
        /// The name of the NormalTextColour within the Checkbox Style.
        /// This must match the Checkbox style colour name.
        /// </summary>
        private const string c_normalTextColorStr = "TextColour";

        /// <summary>
        /// Authorisation token field name
        /// </summary>
        private const string c_authToken = "authToken";

        /// <summary>
        /// Machine Authorisation token field name
        /// </summary>
        private const string c_machineToken = "machineToken";

        /// <summary>
        /// Users registered field name
        /// </summary>
        private const string c_registeredName = "registeredName";

        /// <summary>
        /// Our Debouncer used to check the password complexity
        /// </summary>
        private Debouncer m_passwordDebouncer = new Debouncer();

        /// <summary>
        /// The interval to check for password complexity changes
        /// </summary>
        private int c_checkPasswordDebounceInMS = 250;

        /// <summary>
        /// Acceptable password colour for our password indicator.
        /// Cannot be a const
        /// </summary>
        private Color m_acceptedPasswordColour = Colors.Green;

        /// <summary>
        /// Unacceptable password colour for our password indicator.
        /// Cannot be a const
        /// </summary>
        private Color m_unacceptedPasswordColour = Colors.Red;

        /// <summary>
        /// Defines the code for User Does Not Own Elite
        /// </summary>
        private const int c_DoesNotOwnEliteCode = 257;

        /// <summary>
        /// Our LauncherWindow
        /// </summary>
        private LauncherWindow m_launcherWindow = null;

        /// <summary>
        /// Group of bools to show if fields are valid or not
        /// </summary>
        private bool m_isFirstNameValid         = false;
        private bool m_isLastNameValid          = false;
        private bool m_isEmailValid             = false;
        private bool m_isConfirmPasswordValid   = false;
        private bool m_doWeHavePasswordText     = false;

        /// <summary>
        /// The min numbers of chars in a verification code
        /// </summary>
        private const int c_NumberOfCharsInCode = 5;

        /// <summary>
        /// A developer returned verification code field name
        /// </summary>
        private const string c_developerCodeResonse = "dev_otp";

        /// <summary>
        /// Should we display the Account Link page?
        /// </summary>
        private bool m_displayAccountLinkChoice = false;

        /// <summary>
        /// A none C# defined HttpStatusCode which can be
        /// returned from the server API call.
        /// </summary>
        private const int c_specificationNotMatched = 422;

        /// <summary>
        /// Keeps the current text colour for the T&C control, this is used
        /// when we enable or disable the colour and need to handle the colour
        /// change ourself rather than using the standard isEnabled property.
        /// </summary>
        private SolidColorBrush m_currentTandCTextColour = (SolidColorBrush)Application.Current.Resources[c_normalTextColorStr];

        /// <summary>
        /// A delay in MS that is added to the SIGNUP process if an error occurs. This
        /// stops the user spamming the server and email address used.
        /// </summary>
        private const int c_signUpErrorDelayInMS = 2000;

        /// <summary>
        /// Our LauncherModelManager object
        /// </summary>
        LauncherModelManager m_launcherModelManager = null;


        /// <summary>
        /// The Password feedback Delimiter, used between warning and password
        /// suggestions.
        /// </summary>
        private const string c_passwordFeedbackDelimiter = " ";

        /// <summary>
        /// The maximum number of chars that we can display on the password
        /// feedback before we run out of UI space. this is used to determine
        /// if extra feedback (suggestions can be displayed or not)
        /// </summary>
        private const int c_passwordFeedBackMaxChars = 181;

        /// <summary>
        /// Enum used to identify which error message control to show or hide.
        /// </summary>
        private enum ErrorMessageControls
        {
            EmailErrorMessageCtrl = 0,
            PasswordErrorMessageCtrl,
            ConfirmPasswordErrorMessageCtrl,
            AllErrorMessagesCtrls
        }

        /// <summary>
        /// This object contains any displayable errors. It helps by identifying the highest
        /// priority error message to display (as we display only one message), as well as 
        /// indicating which items have errors (email, password etc).
        /// </summary>
        private HomeCreateAccountErrorManager m_homeCreateAccountErrorManager = new HomeCreateAccountErrorManager();
    }

}
