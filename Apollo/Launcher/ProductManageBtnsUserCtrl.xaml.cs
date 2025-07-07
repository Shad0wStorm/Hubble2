//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ProductManageBtnsUserCtrl UserControl
//
//! Author:     Alan MacAree
//! Created:    20 Dec 2022
//----------------------------------------------------------------------

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for ProductManageBtnsUserCtrl.xaml
    /// </summary>
    public partial class ProductManageBtnsUserCtrl : UserControl
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public ProductManageBtnsUserCtrl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Sets the ProductUserCtrl to be used by this control
        /// </summary>
        /// <param name="_productUserCtrl"></param>
        public void SetProductUserCtrl( ProductUserCtrl _productUserCtrl )
        {
            Debug.Assert( _productUserCtrl != null );
            m_productUserCtrl = _productUserCtrl;
        }

        /// <summary>
        /// Validate files clicked by the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPART_ValidateBtnClick( object sender, RoutedEventArgs e )
        {
            if ( m_productUserCtrl != null )
            {
                m_productUserCtrl.ValidateGameFiles();
            }
        }

        /// <summary>
        /// Check for updates clicked by the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPART_CheckUpdatesBtnClick( object sender, RoutedEventArgs e )
        {
            if ( m_productUserCtrl != null )
            {
                m_productUserCtrl.CheckForUpdates();
            }
        }

        /// <summary>
        /// Uninstall clicked by the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPART_UninstallBtnClick( object sender, RoutedEventArgs e )
        {
            if ( m_productUserCtrl != null )
            {
                m_productUserCtrl.UninstallProduct();
            }
        }

        /// <summary>
        /// Back button clicked by the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPART_BackBtnClick( object sender, RoutedEventArgs e )
        {
            if ( m_productUserCtrl != null )
            {
                m_productUserCtrl.DisplayManagedControls( false );
            }
        }

        /// <summary>
        /// Our ProductUserCtrl object
        /// </summary>
        private ProductUserCtrl m_productUserCtrl;
    }
}
