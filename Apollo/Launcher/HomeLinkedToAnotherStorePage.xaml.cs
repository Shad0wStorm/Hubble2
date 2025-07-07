//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! HomeLinkedToAnotherStorePage, When the current account is 
//! linked to another steam account
//
//! Author:     Alan MacAree
//! Created:    27 Nov 2022
//----------------------------------------------------------------------

using CBViewModel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for HomeLinkedToAnotherStorePage.xaml
    /// </summary>
    public partial class HomeLinkedToAnotherStorePage : Page
    {
        public HomeLinkedToAnotherStorePage( LauncherWindow _launcherWindow )
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
                        PART_StoreImage.Source = new BitmapImage( new Uri( Consts.c_steamLogoImage, UriKind.Absolute ) );
                        PART_AccLinkedToDiffAcc1.Text = LocalResources.Properties.Resources.TITLE_AccLinkedToDiffSteam1;
                        PART_AccLinkedToDiffAcc2.Text = LocalResources.Properties.Resources.TITLE_AccLinkedToDiffSteam2;
                    }
                    else if ( cobraBayView.IsEpic() )
                    {
                        // This has been started via Epic
                        PART_StoreImage.Source = new BitmapImage( new Uri( Consts.c_epicLogoImage, UriKind.Absolute ) );
                        PART_AccLinkedToDiffAcc1.Text = LocalResources.Properties.Resources.TITLE_AccLinkedToDiffSEpic1;
                        PART_AccLinkedToDiffAcc2.Text = LocalResources.Properties.Resources.TITLE_AccLinkedToDiffEpic2;
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
        /// Handles the Just log in anyway option
        /// This hyperlink does not have a URI, but instead is used like a button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPART_DontLinkAccRequestNavigate( object sender, RequestNavigateEventArgs e )
        {
            Debug.Assert( m_launcherWindow != null );
            if ( m_launcherWindow != null )
            {
                _ = m_launcherWindow.DisplayFrontPageAsync();
            }
            e.Handled = true;
        }

        /// <summary>
        /// Called when user opts to link this account to the steam account
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPART_LinkToThisAccountClick( object sender, System.Windows.RoutedEventArgs e )
        {
            _ = UpdateDisplayAndLinkAccountsAsync();
        }

        /// <summary>
        /// Displays the DisplayHomeFirstOpenLinkingPage whilst the
        /// linking takes place, then displays the DisplayFrontPageAsync
        /// if the link was okay.
        /// </summary>
        private async Task UpdateDisplayAndLinkAccountsAsync()
        {
            bool linkedAccountsOkay = false;
            bool serverLinkStateChange = false;

            Debug.Assert( m_launcherWindow != null );
            if ( m_launcherWindow != null )
            {
                m_launcherWindow.DisplayHomeFirstOpenLinkingPage();

                CobraBayView cobraBayView = m_launcherWindow.GetCobraBayView();
                Debug.Assert( cobraBayView != null );

                if ( cobraBayView != null )
                {
                    // We do not use accountLinkedToAnotherStoreAccountUnused, because we
                    // are forcing the the link change
                    bool accountLinkedToAnotherStoreAccountUnused = false;
                    await Task.Run( () =>
                    {
                        // Make sure we override the existing account link
                        bool forceLinkChange = true;
                        m_launcherWindow.LogEvent( "LinkAccounts", "Relinking Account", "Forcing the link" );
                        linkedAccountsOkay = cobraBayView.LinkAccounts( forceLinkChange, out accountLinkedToAnotherStoreAccountUnused, out serverLinkStateChange );
                    } );
                }
            }

            // Were the accounts linked okay?
            if ( linkedAccountsOkay )
            {
                m_launcherWindow.LogEvent( "LinkAccounts", "Success", "Relinking Account" );
                m_launcherWindow.DisplayStoreFirstOpenLinkedPage();
            }
            else
            {
                if ( serverLinkStateChange )
                {
                    // The server link account has changed, this means either the account is now unlinked
                    // or it is linked to another account. Either way, the current client is not in the 
                    // state to link or carry on. Force the user back to the start.
                    m_launcherWindow.LogEvent( "LinkAccounts", "Failed", "Relinking Account, Server State Changed" );
                }
                else
                {
                    // We failed to link the accounts
                    // This is an error, the accounts failed to link, let the
                    // user try again
                    m_launcherWindow.LogEvent( "LinkAccounts", "Failed", "Relinking Account, Server refused" );
                }

                _ = m_launcherWindow.Logout();
            }
        }

        /// <summary>
        /// Our LauncherWindow
        /// </summary>
        private LauncherWindow m_launcherWindow;
    }
}
