//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! SettingsCtrl UserControl
//
//! Author:     Alan MacAree
//! Created:    22 Aug 2022
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FDUserControls
{
    /// <summary>
    /// Interaction logic for SettingsCtrl.xaml
    /// </summary>
    public partial class SettingsCtrl : UserControl
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SettingsCtrl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_iSettingsCtrlUI">Interface to send ISettingsCtrlUI events to</param>
        public SettingsCtrl( ISettingsCtrlUI _iSettingsCtrlUI)
        {
            InitializeComponent();
            RegisterInterface( _iSettingsCtrlUI );
        }

        /// <summary>
        /// Register a ISettingsCtrlUI to be sent events
        /// </summary>
        /// <param name="_iSettingsCtrlUI"></param>
        public void RegisterInterface( ISettingsCtrlUI _iSettingsCtrlUI )
        {
            m_ISettingsCtrlUlList.Add( _iSettingsCtrlUI );
            PART_UsersName.Text = _iSettingsCtrlUI.GetUsersName();
            PART_UsersEmail.Text = FDUtils.ReduceStringToMaxLength( _iSettingsCtrlUI.GetUsersEmail(), c_maxEmailLengthOnUI, c_emailAdressReducedExt );
        }

        /// <summary>
        /// Support Btn Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSupportBtnClick( object sender, RoutedEventArgs e )
        {
            // Update everyone who has registered about a Support button click
            foreach ( ISettingsCtrlUI iSettingsCtrlUI in m_ISettingsCtrlUlList )
            {
                try
                {
                    iSettingsCtrlUI.OnSupportBtnClicked();
                }
                catch ( Exception )
                {
                    // Do nothing, just don't crash
                }
            }
        }

        /// <summary>
        /// Frontier Links Btn Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFDLinksBtnClick( object sender, RoutedEventArgs e )
        {
            // Update everyone who has registered about a FDLinks button click
            foreach ( ISettingsCtrlUI iSettingsCtrlUI in m_ISettingsCtrlUlList )
            {
                try
                {
                    iSettingsCtrlUI.OnFrontierLinksBtnClicked();
                }
                catch ( Exception )
                {
                    // Do nothing, just don't crash
                }
            }
        }

        /// <summary>
        /// Options Btn Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnOptionsBtnClick( object sender, RoutedEventArgs e )
        {
            // Update everyone who has registered about a Options button click
            foreach ( ISettingsCtrlUI iSettingsCtrlUI in m_ISettingsCtrlUlList )
            {
                try
                {
                    iSettingsCtrlUI.OnOptionsBtnClicked();
                }
                catch ( Exception )
                {
                    // Do nothing, just don't crash
                }
            }
        }
        
        /// <summary>
        /// Language Btn Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLanguageBtnClick( object sender, RoutedEventArgs e )
        {
            // Update everyone who has registered about a Language button click
            foreach ( ISettingsCtrlUI iSettingsCtrlUI in m_ISettingsCtrlUlList )
            {
                try
                {
                    iSettingsCtrlUI.OnLanguageBtnClicked();
                }
                catch( Exception )
                {
                    // Do nothing, just don't crash
                }
            }
        }

        /// <summary>
        /// Back Btn Clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBackBtnClick( object sender, RoutedEventArgs e )
        {
            // Update everyone who has registered about a Back button click
            foreach ( ISettingsCtrlUI iSettingsCtrlUI in m_ISettingsCtrlUlList )
            {
                try
                {
                    iSettingsCtrlUI.OnBackBtnClicked();
                }
                catch ( Exception )
                {
                    // Do nothing, just don't crash
                }
            }
        }

        /// <summary>
        /// List of IProductUICtrl to inform of any ProductUICtrl related events
        /// </summary>
        private List<ISettingsCtrlUI> m_ISettingsCtrlUlList = new List<ISettingsCtrlUI>();

        /// <summary>
        /// The maximum numbers of chars we can display for an email address
        /// </summary>
        private const int c_maxEmailLengthOnUI = 165;

        /// <summary>
        /// The chars added to the end of the email address if we end up reducing it.
        /// </summary>
        public const string c_emailAdressReducedExt = "...";
    }
}
