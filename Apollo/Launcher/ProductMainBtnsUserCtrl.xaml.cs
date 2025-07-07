//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ProductMainBtnsUserCtrl UserControl
//
//! Author:     Alan MacAree
//! Created:    20 Dec 2022
//----------------------------------------------------------------------

using CBViewModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for ProductMainBtnsUserCtrl.xaml
    /// </summary>
    public partial class ProductMainBtnsUserCtrl : UserControl
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ProductMainBtnsUserCtrl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Sets the CobraBayView for the internal DownloadInfoUserCtrl
        /// </summary>
        /// <param name="_cobraBayView"></param>
        public void SetCobraBayView( CobraBayView _cobraBayView )
        {
            PART_DownloadInfoUserCtrl.TheCobraBayView = _cobraBayView;
        }

        /// <summary>
        /// Enables or disables all buttons
        /// </summary>
        /// <param name="_enableButtons">Sets to enabled if true</param>
        public void Enable( bool _enableButtons )
        {
            EnableBigBtn( _enableButtons );
            EnableManageBtn( _enableButtons );
            EnableSmallBtn( _enableButtons );
        }

        /// <summary>
        /// Enables or disables the big button
        /// </summary>
        /// <param name="_enableButton">Sets to enabled if true</param>
        public void EnableBigBtn( bool _enableButton )
        {
            PART_BigBtn.IsEnabled = _enableButton;
        }

        /// <summary>
        /// Enables or disables the manage button
        /// </summary>
        /// <param name="_enableButton">Sets to enabled if true</param>
        public void EnableManageBtn( bool _enableButton )
        {
            PART_ManageBtn.IsEnabled = _enableButton;
        }

        /// <summary>
        /// Enables or disables the small button
        /// </summary>
        /// <param name="_enableButton">Sets to enabled if true</param>
        public void EnableSmallBtn( bool _enableButton )
        {
            PART_SmallBtn.IsEnabled = _enableButton;
        }

        /// <summary>
        /// Updates the button text 
        /// </summary>
        public void UpdateButtonText()
        {
            if ( BigButton != null )
            {
                PART_BigBtn.Content = BigButton.ButtonText();
                BigButton.SetupButton( PART_BigBtn );
            }

            if ( ManageButton != null )
            {
                PART_ManageBtn.Content = ManageButton.ButtonText();
                ManageButton.SetupButton( PART_ManageBtn );
            }

            if ( SmallButton != null )
            {
                PART_SmallBtn.Content = SmallButton.ButtonText();
                SmallButton.SetupButton( PART_SmallBtn );
            }
        }

        /// <summary>
        /// Called when the user clicks on the big button (normally a Play or Login button)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PART_BigBtn_Click( object sender, RoutedEventArgs e )
        {
            Debug.Assert( BigButton != null );
            if ( BigButton != null )
            {
                PART_BigBtn.IsEnabled = false;
                // The action depends on the ButtionAction object that is current held, so this
                // can change from Install to Update to Play or even be disabled.
                if ( !BigButton.PerformAction( this ) )
                {
                    PART_BigBtn.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Called when the user clicks on the Manage button 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PART_ManageBtn_Click( object sender, RoutedEventArgs e )
        {
            Debug.Assert( ManageButton != null );
            if ( ManageButton != null )
            {
                _ = ManageButton.PerformAction( this );
            }
        }

        /// <summary>
        /// Called when the user clicks on the small button (versions etc)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PART_SmallBtn_Click( object sender, RoutedEventArgs e )
        {
            Debug.Assert( SmallButton != null );
            if ( SmallButton != null )
            {
                // The action depends on the ButtionAction object that is current held, so this
                // can change.
                SmallButton.PerformAction( PART_SmallBtn );
            }
        }

        /// <summary>
        /// Our Buttons, these are dynamic buttons
        /// </summary>
        public ButtonAction BigButton { get; set; } = null;
        public ButtonAction ManageButton { get; set; } = null;
        public ButtonAction SmallButton { get; set; } = null;
    }
}
