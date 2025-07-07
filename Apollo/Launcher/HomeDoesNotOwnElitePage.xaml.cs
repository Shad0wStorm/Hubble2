//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! HomeDoesNotOwnElitePage
//
//! Author:     Alan MacAree
//! Created:    19 Aug 2022
//----------------------------------------------------------------------

using CBViewModel;
using ClientSupport;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for HomeDoesNotOwnElite.xaml
    /// </summary>
    public partial class HomeDoesNotOwnElitePage : Page
    {
        /// <summary>
        /// Constrcutor
        /// </summary>
        /// <param name="_launcherWindow">Our LauncherWindow used get the objects to redeem codes and display the next page</param>
        public HomeDoesNotOwnElitePage( LauncherWindow _launcherWindow )
        {
            Debug.Assert( _launcherWindow != null );
            m_launcherWindow = _launcherWindow;
            InitializeComponent();
        }

        /// <summary>
        /// Called when the user clicks On redeem code
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PART_OnRedeemCode( object sender, RoutedEventArgs e )
        {
            Debug.Assert( m_launcherWindow != null );
            if ( m_launcherWindow != null )
            {
                if ( RedeemCode( PART_ProductCodeEditBox.TextBoxText ) )
                {
                    // We redeemed the code, display the main front page to the user
                    // We use an Async method because this can be
                    // a lengthy procss.
                    _ = m_launcherWindow.DisplayFrontPageAsync();
                }
                else
                {
                    // We failed to redeem the code
                    DisplayUserError( LocalResources.Properties.Resources.TXT_Check );
                }
            }
        }

        /// <summary>
        /// Displays the user error message
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
        /// Handles the Signup click on a hyperlink.
        /// This hyperlink does not have a URI, but instead is used like a button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPART_DontHaveACodeRequestNavigate( object sender, RequestNavigateEventArgs e )
        {
            Debug.Assert( m_launcherWindow != null );
            if ( m_launcherWindow != null )
            {
                m_launcherWindow.DisplayWaitPage();

                // This is a safety catch should the user manage to remember 
                // some user details which are not linked to a store and do
                // not own Elite. This can happen if the user manually starts
                // the launcher within the stores directory and then remembers
                // a user login details that does not own a game, and  
                // the account is not linked to the store in question. If
                // we don't clear the account details, the user can end up 
                // not being able to login via the store.
                CobraBayView cobraBayView = m_launcherWindow.GetCobraBayView();
                Debug.Assert( cobraBayView != null );
                if ( cobraBayView != null )
                {
                    if ( cobraBayView.IsEpic() ||
                         cobraBayView.IsSteam() )
                    {
                        m_launcherWindow.ForgetUsersDetails();
                    }
                }

                _ = m_launcherWindow.Logout();
            }
            e.Handled = true;
        }

        /// <summary>
        /// Called when the text changes within the editbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPART_ProductCodeEditBoxChanged( object sender, TextChangedEventArgs e )
        {
            PART_RedeemCodeBtn.IsEnabled = (PART_ProductCodeEditBox.TextBoxText.Length > 0);
            HideUserError();
        }

        /// <summary>
        /// Redeems a code
        /// </summary>
        /// <param name="_code">The code to redeem</param>
        /// <returns>trus if the code was redeemed okay</returns>
        private bool RedeemCode( string _code )
        {
            Debug.Assert( m_launcherWindow != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( _code ) );

            bool codeRedeemed = false;

            if ( m_launcherWindow != null &&
                 !string.IsNullOrWhiteSpace( _code ) )
            {

                CobraBayView cobraBayView = m_launcherWindow.GetCobraBayView();

                Debug.Assert( cobraBayView != null );
                if ( cobraBayView != null )
                {
                    FORCManager fORCManager = cobraBayView.Manager();

                    Debug.Assert( fORCManager != null );
                    if ( fORCManager != null )
                    {
                        JSONWebPutsAndPostsResult jsonWebPostResult = fORCManager.RedeemCode( _code );

                        switch( jsonWebPostResult.HttpStatusResult )
                        {
                            case HttpStatusCode.Created:
                                codeRedeemed = true;
                                break;
                            case HttpStatusCode.Unauthorized:
                                codeRedeemed = false;
                                break;
                            default:
                                codeRedeemed = false;
                                break;
                        }
                    }
                }
            }

            return codeRedeemed;
        }

        /// <summary>
        /// Our LauncherWindow
        /// </summary>
        private LauncherWindow m_launcherWindow;
    }
}
