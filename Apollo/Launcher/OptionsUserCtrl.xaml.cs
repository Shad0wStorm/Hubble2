//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! OptionsUserCtrl UserControl
//
//! Author:     Alan MacAree
//! Created:    15 Nov 2022
//----------------------------------------------------------------------

using CBViewModel;
using ClientSupport;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for OptionsUserCtrl.xaml
    /// </summary>
    public partial class OptionsUserCtrl : UserControl
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_cobraView">The CobraBayView object</param>
        public OptionsUserCtrl( CobraBayView _cobraView )
        {
            Debug.Assert( _cobraView != null );

            InitializeComponent();
            m_cobraBayView = _cobraView;

            SetupCheckBoxes();
        }

        /// <summary>
        /// Sets up the checkboxes within the UI
        /// </summary>
        private void SetupCheckBoxes()
        {
            if ( m_cobraBayView != null )
            {
                PART_MultiThreadDownloadCB.IsChecked = !m_cobraBayView.DisableFastDownload;
                PART_CacheCheckCB.IsChecked = DownloadManagerLocalCache.EnableCache;
                PART_VirtualCacheCB.IsChecked = DownloadManagerLocalCache.EnableVirtualCache;
                PART_CheckForXInputCB.IsChecked = Properties.Settings.Default.DXCheck;
            }
        }

        /// <summary>
        /// Called when the ctrl is unloaded, this saves the user
        /// options.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUnloaded( object sender, RoutedEventArgs e )
        {
            if ( m_cobraBayView != null )
            {
                m_cobraBayView.DisableFastDownload = !PART_MultiThreadDownloadCB.IsChecked ?? false;
                DownloadManagerLocalCache.EnableCache = PART_CacheCheckCB.IsChecked ?? false;
                DownloadManagerLocalCache.EnableVirtualCache = PART_VirtualCacheCB.IsChecked ?? false;
                Properties.Settings.Default.DXCheck = PART_CheckForXInputCB.IsChecked ?? false;
                Properties.Settings.Default.Save();
            }
        }

        /// <summary>
        /// Called when the user clicks on Hardware Survey
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHardwareSurveyClick( object sender, RoutedEventArgs e )
        {
            if ( m_cobraBayView != null )
            {
                bool allowUpload = true;
                bool waitToFinish = false;

                m_cobraBayView.RunHardwareSurvey( allowUpload, waitToFinish );
            }
        }

        /// <summary>
        /// The LauncherWindow object
        /// </summary>
        private CobraBayView m_cobraBayView;

    }
}
