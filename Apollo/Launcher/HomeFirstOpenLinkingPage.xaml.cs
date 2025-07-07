//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! HomeFirstOpenLinkingPage
//
//! Author:     Alan MacAree
//! Created:    19 Aug 2022
//----------------------------------------------------------------------

using CBViewModel;
using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for HomeFirstOpenLinkingPage.xaml
    /// </summary>
    public partial class HomeFirstOpenLinkingPage : Page
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_launcherWindow">The LauncherWindow used to get UI information to display </param>
        public HomeFirstOpenLinkingPage( LauncherWindow _launcherWindow )
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
                        PART_TitleLinking.Text = string.Format( LocalResources.Properties.Resources.TITLE_LinkingStoreAccount, LocalResources.Properties.Resources.TITLE_StoreSteam );
                        PART_StoreImage.Source = new BitmapImage( new Uri( Consts.c_steamLogoImage, UriKind.Absolute ) );
                    }
                    else if ( cobraBayView.IsEpic() )
                    {
                        // This has been started via Epic
                        PART_TitleLinking.Text = string.Format( LocalResources.Properties.Resources.TITLE_LinkingStoreAccount, LocalResources.Properties.Resources.TITLE_StoreEpic );
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
        /// Our Launcher window
        /// </summary>
        private LauncherWindow m_launcherWindow = null;
    }
}
