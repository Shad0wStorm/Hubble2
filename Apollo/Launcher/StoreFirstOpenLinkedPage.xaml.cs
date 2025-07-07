//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! StoreFirstOpenLinkedPage UserControl
//
//! Author:     Alan MacAree
//! Created:    01 Dec 2022
//----------------------------------------------------------------------

using CBViewModel;
using ClientSupport;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for StoreFirstOpenLinkedPage.xaml
    /// </summary>
    public partial class StoreFirstOpenLinkedPage : Page
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_launcherWindow">The LauncherWindow</param>
        public StoreFirstOpenLinkedPage( LauncherWindow _launcherWindow )
        {
            Debug.Assert( _launcherWindow != null );
            m_launcherWindow = _launcherWindow;

            InitializeComponent();

            // Display the current users registered name
            string registeredUsersName = GetCurrentUsersRegisteredUsersName();

            if ( !string.IsNullOrWhiteSpace( registeredUsersName ) )
            {
                PART_PlayersEmail.Content = registeredUsersName;
            }

            // Setup the text based on which store the user is using
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
                        PART_LinkedToAStoreTitle.Text = LocalResources.Properties.Resources.TITLE_SteamLinkedToFDAccount;
                        PART_LogInWithStore.Content = LocalResources.Properties.Resources.BTNT_LoginWithSteam;
                    }
                    else if ( cobraBayView.IsEpic() )
                    {
                        // This has been started via Epic
                        PART_LinkedToAStoreTitle.Text = LocalResources.Properties.Resources.TITLE_EPICLinkedToFDAccount;
                        PART_LogInWithStore.Content = LocalResources.Properties.Resources.BTNT_LoginWithEPIC;
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
        /// Returns the current users registered name
        /// </summary>
        /// <returns>Current users registered name</returns>
        private string GetCurrentUsersRegisteredUsersName()
        {
            string registeredUsersName = null;

            Debug.Assert( m_launcherWindow != null );
            if ( m_launcherWindow != null )
            {
                CobraBayView cobraBayView = m_launcherWindow.GetCobraBayView();
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
                                registeredUsersName = userDetails.RegisteredName;
                            }
                        }
                    }
                }
            }

            return registeredUsersName;
        }

        /// <summary>
        /// Handles the Log In With the store
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPART_LogInWithStoreClick( object sender, RoutedEventArgs e )
        {
            Debug.Assert( m_launcherWindow != null );
            if ( m_launcherWindow != null )
            {
                m_launcherWindow.DisplayWaitPage();
                _ = m_launcherWindow.DisplayFrontPageAsync();
            }
        }

        /// <summary>
        /// Handles the Login with different account
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPART_LoginDiffAccRequestNavigate( object sender, RequestNavigateEventArgs e )
        {
            Debug.Assert( m_launcherWindow != null );
            if ( m_launcherWindow != null )
            {
                bool displayAccountLinkChoice = false;
                bool requestNewLogon = true;
                m_launcherWindow.DisplayHomeLoginPage( displayAccountLinkChoice, requestNewLogon );
            }
            e.Handled = true;
        }

        /// <summary>
        /// Our LauncherWindow
        /// </summary>
        private LauncherWindow m_launcherWindow = null;
    }
}
