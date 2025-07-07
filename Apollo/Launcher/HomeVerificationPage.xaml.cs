//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! HomeVerificationPage, this verifies the user login
//
//! Author:     Alan MacAree
//! Created:    08 Sept 2022
//----------------------------------------------------------------------

using CBViewModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for HomeVerificationPage.xaml
    /// </summary>
    public partial class HomeVerificationPage : Page
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_launcherWindow">The main launcher window</param>
        /// <param name="_emailAddress">Users email address</param>
        /// <param name="_password">Users password</param>
        /// <param name="_previousIVerificiationPage">The previous page, must provide a IVerification interface</param>
        /// <param name="_developerVerificationCode">Developer verification code, can be null</param>
        public HomeVerificationPage( LauncherWindow _launcherWindow,
                                     string _emailAddress,
                                     string _password,
                                     Page _previousIVerificiationPage,
                                     string _developerVerificationCode = null )
        {
            Debug.Assert( _launcherWindow != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( _emailAddress ) );
            Debug.Assert( !string.IsNullOrWhiteSpace( _password ) );
            Debug.Assert( _previousIVerificiationPage != null );
            Debug.Assert( _previousIVerificiationPage is IVerification );

            InitializeComponent();
            m_email = _emailAddress;
            m_password = _password;
            m_launcherWindow = _launcherWindow;
            m_previousIVerificationPage = _previousIVerificiationPage;

            PART_PlayersEmail.Content = Utils.FormatLabelString( _emailAddress );

            string lineWithEmail = string.Format( LocalResources.Properties.Resources.LW_VerificationInstLine3,
                                                                _emailAddress );
            PART_Line3WithEmailAddress.Content = Utils.FormatLabelString( lineWithEmail );

            if ( m_previousIVerificationPage != null )
            {
                IVerification verificationPageInterface = m_previousIVerificationPage as IVerification;
                if ( verificationPageInterface != null )
                {
                    string formattedtext = string.Format( LocalResources.Properties.Resources.LW_ReturnToPage, verificationPageInterface.PageTitle() );
                    PART_ReturnToPreviousPageCtrl.Text = formattedtext;
                }
            }

            EnableOrDisableSubmit();

            // See if we need to display a developer code
            if ( _developerVerificationCode != null )
            {
                #if DEVELOPMENT || DEBUG
                    PART_VerificationEditBox.TextBoxText = _developerVerificationCode;
                #endif
            }
        }

        /// <summary>
        /// Called when the text changes within the Verification Code EditBoxUserCtrl
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTextChangedEventHandler( object sender, TextChangedEventArgs e )
        {
            EnableOrDisableSubmit();
        }

        /// <summary>
        /// Enables or disabled the submit button
        /// </summary>
        private void EnableOrDisableSubmit()
        {
            bool enableSubmitBtn = false;

            string verificationCode = (string)PART_VerificationEditBox.TextBoxText;

            if ( !string.IsNullOrWhiteSpace( verificationCode ) )
            {
                // We must have at least c_MinNumberOfCharsInCode number of chars
                if ( verificationCode.Length >= c_MinNumberOfCharsInCode )
                {
                    enableSubmitBtn = true;
                }
            }

            PART_SubmitBtn.IsEnabled = enableSubmitBtn;
        }

        /// <summary>
        /// Verifies the user entered code
        /// </summary>
        /// <param name="_verificationCode">The verification code</param>
        /// <returns>true if the verification code is valid</returns>
        private bool VerifyUserProvidedCode( string _verificationCode )
        {
            bool wasItVerified = false;

            if ( m_previousIVerificationPage != null )
            {
                IVerification verificationPageInterface = m_previousIVerificationPage as IVerification;
                if ( verificationPageInterface != null )
                {
                    wasItVerified = verificationPageInterface.ValidateCode( m_email, m_password, _verificationCode );
                }
            }

            return wasItVerified;
        }

        /// <summary>
        /// Called when the page is loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Page_Loaded( object sender, RoutedEventArgs e )
        {
            PART_VerificationEditBox.TextChangedEventHandler += OnTextChangedEventHandler;
        }

        /// <summary>
        /// Called when page is unloaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Page_Unloaded( object sender, RoutedEventArgs e )
        {
            // Free up our text change handler.
            PART_VerificationEditBox.TextChangedEventHandler -= OnTextChangedEventHandler;
        }

        /// <summary>
        /// Handles the "Return to previous page" hyperlink.
        /// This hyperlink does not have a real URI, but instead is used like a button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnReturnToPreviousPage( object sender, RequestNavigateEventArgs e )
        {
            // Make sure we reset the login, otherwise the login process will be
            // left in a bad state
            ResetLogin();

            // We should go back to the previous page, but we must also allow 
            // for issues in case we don't have one.
            if ( m_previousIVerificationPage != null )
            {
                NavigationService.Navigate( m_previousIVerificationPage );
            }
            else
            {
                NavigationService ns = NavigationService.GetNavigationService( this );
                ns.GoBack();
            }
            
            e.Handled = true;
        }

        /// <summary>
        /// Resets the login process, this ensures we know the server login process
        /// is in a clean state.
        /// </summary>
        private void ResetLogin()
        {
            Debug.Assert( m_launcherWindow != null );

            if ( m_launcherWindow != null )
            {
                CobraBayView cobraBayView = m_launcherWindow.GetCobraBayView();
                Debug.Assert( cobraBayView != null );

                if ( cobraBayView != null )
                {
                    bool rememberUser = false;
                    bool rememberPassword = false;
                    cobraBayView.Manager().ResetLogin( rememberUser, rememberPassword );
                }
            }
        }

        /// <summary>
        /// Handles the "Resend Code" hyperlink.
        /// This hyperlink does not have a real URI, but instead is used like a button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnResendCode( object sender, RequestNavigateEventArgs e )
        {
            // Reset the login process so that it is in a clean state.
            ResetLogin();

            if ( m_previousIVerificationPage != null )
            {
                IVerification verificationPageInterface = m_previousIVerificationPage as IVerification;
                if ( verificationPageInterface != null )
                {
                    string devCode = verificationPageInterface.ResendCode();
                    if ( devCode != null )
                    {
#if DEVELOPMENT || DEBUG
                        PART_VerificationEditBox.TextBoxText = devCode;
#endif
                    }
                }
            }

            string title = LocalResources.Properties.Resources.DialogTitle;
            string message = LocalResources.Properties.Resources.LW_CheckEmail;

            Debug.Assert( m_launcherWindow != null );
            if ( m_launcherWindow != null )
            {
                m_launcherWindow.DisplayPopupPage( this, title, "", message );
            }

            e.Handled = true;
        }

        /// <summary>
        /// Called when the user clicks the submit button to submit the code
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSubmit( object sender, RoutedEventArgs e )
        {
            SubmitValidationCode();
        }

        /// <summary>
        /// Submits the validation code to the server
        /// </summary>
        private void SubmitValidationCode()
        {
            PART_ErrorMessage.Visibility = Visibility.Hidden;

            string verificationCode = (string)PART_VerificationEditBox.TextBoxText;
            bool verifiedOkay = false;

            if ( !string.IsNullOrWhiteSpace( verificationCode ) )
            {

                verifiedOkay = VerifyUserProvidedCode( verificationCode );

                if ( !verifiedOkay )
                {
                    PART_ErrorMessage.ErrorMessage = LocalResources.Properties.Resources.TXT_Check;
                    PART_ErrorMessage.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// The previous page to return to
        /// </summary>
        private Page m_previousIVerificationPage;

        /// <summary>
        /// Our launcher window
        /// </summary>
        private LauncherWindow m_launcherWindow;

        /// <summary>
        /// Users email address
        /// </summary>
        private string m_email;

        /// <summary>
        /// Users password
        /// </summary>
        private string m_password;

        /// <summary>
        /// Min Number of chars in the validation code
        /// </summary>
        private const int c_MinNumberOfCharsInCode = 5;
    }

}
