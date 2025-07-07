//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! StoreFirstOpenUnlinkedPage UserControl
//
//! Author:     Alan MacAree
//! Created:    15 Aug 2022
//----------------------------------------------------------------------

using CBViewModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for StoreFirstOpenUnlinkedPage.xaml
    /// </summary>
    public partial class StoreFirstOpenUnlinkedPage : Page
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_launcherWindow">The LauncherWindow</param>
        public StoreFirstOpenUnlinkedPage( LauncherWindow _launcherWindow )
        {
            Debug.Assert( _launcherWindow != null );
            m_launcherWindow = _launcherWindow;

            InitializeComponent();

            // Setup the UI
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
                        PART_StoreNotLinkedTitle.Text = string.Format( LocalResources.Properties.Resources.TITLE_StoreNotLinkedToFDAccount, LocalResources.Properties.Resources.TITLE_StoreSteam );
                    }
                    else if ( cobraBayView.IsEpic() )
                    {
                        // This has been started via Epic
                        PART_StoreNotLinkedTitle.Text = string.Format( LocalResources.Properties.Resources.TITLE_StoreNotLinkedToFDAccount, LocalResources.Properties.Resources.TITLE_StoreEpic );
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
        /// Handles the SignIn
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPART_LogInClick( object sender, RoutedEventArgs e )
        {
            Debug.Assert( m_launcherWindow != null );
            if ( m_launcherWindow != null )
            {
                bool displayAccountLinkChoice = true;
                m_launcherWindow.DisplayHomeLoginPage( displayAccountLinkChoice );
            }
        }

        /// <summary>
        /// Handles the SignUp request
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPART_SignUpRequestNavigate( object sender, RequestNavigateEventArgs e )
        {
            Debug.Assert( m_launcherWindow != null );
            if ( m_launcherWindow != null )
            {
                bool displayAccountLinkChoice = true;
                m_launcherWindow.DisplayHomeCreateAccountPage( displayAccountLinkChoice );
            }
            e.Handled = true;
        }

        /// <summary>
        /// Our LauncherWindow
        /// </summary>
        private LauncherWindow m_launcherWindow = null;
    }
}
