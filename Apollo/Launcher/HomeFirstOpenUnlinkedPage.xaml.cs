//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! HomeFirstOpenUnlinkedPage, allows the user to chose to link the
//! Frontier Account with the Steam Account
//
//! Author:     Alan MacAree
//! Created:    30 Nov 2022
//----------------------------------------------------------------------

using CBViewModel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for HomeFirstOpenUnlinkedPage.xaml
    /// </summary>
    public partial class HomeFirstOpenUnlinkedPage : Page
    {
        /// <summary>
        /// Constrcutor
        /// </summary>
        /// <param name="_launcherWindow">Our LauncherWindow used to get UI information to display</param>
        public HomeFirstOpenUnlinkedPage( LauncherWindow _launcherWindow )
        {
            Debug.Assert( _launcherWindow != null );
            m_launcherWindow = _launcherWindow;
            InitializeComponent();

            SetupUIBasedOnStore();
        }

        /// <summary>
        /// Sets up the UI based on which store we are using
        /// </summary>
        private void SetupUIBasedOnStore()
        {
            Debug.Assert( m_launcherWindow != null );
            if ( m_launcherWindow != null )
            {
                CobraBayView cobraBayView = m_launcherWindow.GetCobraBayView();

                Debug.Assert( cobraBayView != null );
                if ( cobraBayView != null )
                {
                    if ( cobraBayView.IsSteam() )
                    {
                        // This has been started via Steam
                        PART_TitleWantToLinkStoreAccount.Text = string.Format( LocalResources.Properties.Resources.TITLE_SuccessLinkStoreAndFD2, LocalResources.Properties.Resources.TITLE_StoreSteam );
                        PART_WhenYouLinkYourAccount.Content = string.Format( LocalResources.Properties.Resources.TITLE_SubSuccessLinkSteamAndFD, LocalResources.Properties.Resources.TITLE_StoreSteam );

                        PART_StoreImage.Source = new BitmapImage( new Uri( Consts.c_steamLogoImage, UriKind.Absolute ) );
                    }
                    else if ( cobraBayView.IsEpic() )
                    {
                        // This has been started via Epic
                        PART_TitleWantToLinkStoreAccount.Text = string.Format( LocalResources.Properties.Resources.TITLE_SuccessLinkStoreAndFD2, LocalResources.Properties.Resources.TITLE_StoreEpic );
                        PART_WhenYouLinkYourAccount.Content = string.Format( LocalResources.Properties.Resources.TITLE_SubSuccessLinkEpicAndFD, LocalResources.Properties.Resources.TITLE_StoreEpic );

                        PART_StoreImage.Source = new BitmapImage( new Uri( Consts.c_epicLogoImage, UriKind.Absolute ) );
                    }
                    else
                    {
                        // We don't know what has started this, and maybe we
                        // should not be here.
                        Debug.Assert( false );
                    }
                }
            }
        }

        /// <summary>
        /// Called when the user clicks On Link Accounts
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPART_LinkAccountsClick( object sender, RoutedEventArgs e )
        {
            _ = UpdateDisplayAndLinkAccountsAsync();
        }

        /// <summary>
        /// Displays the DisplayHomeFirstOpenLinkingPage whilst the
        /// linking takes place, then displays the DisplayFrontPageAsync
        /// if the link was okay
        /// </summary>
        private async Task UpdateDisplayAndLinkAccountsAsync()
        {
            bool linkedAccountsOkay = false;
            bool serverLinkStateChange = false;

            // The accountLinkedToAnotherStoreAccount is set if the linking
            // fails because the Frontier account is already linked to another store 
            // account.
            bool accountLinkedToAnotherStoreAccount = false;

            Debug.Assert( m_launcherWindow != null );
            if ( m_launcherWindow != null )
            {
                m_launcherWindow.DisplayHomeFirstOpenLinkingPage();

                CobraBayView cobraBayView = m_launcherWindow.GetCobraBayView();

                Debug.Assert( cobraBayView != null );
                if ( cobraBayView != null )
                {
                    await Task.Run( () =>
                    {
                        // We forceLinkChange this to fail if other links already exist
                        bool forceLinkChange = false;
                        m_launcherWindow.LogEvent( "AccountLinking", "First Attempt", "Not forcing the link" );
                        linkedAccountsOkay = cobraBayView.LinkAccounts( forceLinkChange, out accountLinkedToAnotherStoreAccount, out serverLinkStateChange );
                    } );
                }
            }

            // Were the accounts linked okay?
            if ( linkedAccountsOkay )
            {
                m_launcherWindow.LogEvent( "LinkAccounts", "Success", "First Attempt" );
                m_launcherWindow.DisplayStoreFirstOpenLinkedPage();
            }
            else
            {
                // We failed to link the accounts
                if ( accountLinkedToAnotherStoreAccount )
                {
                    // We failed because the Frontier account is already linked to
                    // another store account. Give the user a chance to override this link
                    m_launcherWindow.LogEvent( "LinkAccounts", "Failed", "Store is linked to another FD account" );
                    m_launcherWindow.DisplayHomeLinkedToAnotherStorePage();
                }
                else
                {
                    if ( serverLinkStateChange )
                    {
                        // Error condition
                        m_launcherWindow.LogEvent( "LinkAccounts", "Failed", "Server state has changed" );
                    }
                    else
                    {
                        // This is an error, the accounts failed to link, let the
                        // user try again
                        m_launcherWindow.LogEvent( "LinkAccounts", "Failed", "Server refused link" );
                    }
                    _ = m_launcherWindow.Logout();
                }
            }
        }

        /// <summary>
        /// Handles the don't link, but login
        /// This hyperlink does not have a URI, but instead is used like a button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPART_DontLinkLogInRequestNavigate( object sender, RequestNavigateEventArgs e )
        {
            Debug.Assert( m_launcherWindow != null );
            if ( m_launcherWindow != null )
            {
                m_launcherWindow.DisplayWaitPage();
                _ = m_launcherWindow.DisplayFrontPageAsync();
            }
            e.Handled = true;
        }

        /// <summary>
        /// Our LauncherWindow
        /// </summary>
        private LauncherWindow m_launcherWindow;
    }
}
