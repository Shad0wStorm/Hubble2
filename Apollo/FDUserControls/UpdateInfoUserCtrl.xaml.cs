//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! UpdateInfoUserCtrl, Displays the Product Update Information
//
//! Author:     Alan MacAree
//! Created:    02 Nov 2022
//----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace FDUserControls
{
    /// <summary>
    /// Interaction logic for UpdateInfoUserCtrl.xaml
    /// </summary>
    public partial class UpdateInfoUserCtrl : UserControl
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public UpdateInfoUserCtrl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Displays the data from the passed Information object
        /// </summary>
        /// <param name="_information">Where to get the data from</param>
        public void DisplayDataFrom( Information _information )
        {
            // We must have these things at this point
            Debug.Assert( _information != null );
            Debug.Assert( _information.ParentPage != null );
            Debug.Assert( _information.LogEventInterface != null );

            m_information = _information;

            PART_ProductName.Content = m_information.Title;
            PART_UpdateInfo.Content = m_information.SubTitle;
            PART_HyperLink.Text = m_information.HTTPLinkText;
        }

        /// <summary>
        /// Shows or hides (by Collapsing) the control.
        /// </summary>
        /// <param name="_showControl">When set to true, the control is Visible, else it is Collapsed</param>
        public void Show( bool _showControl = true )
        {
            if ( _showControl )
            {
                this.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                this.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Handles the link to full details information web page.
        /// Starts the default web browser and navigates to the update information page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnHyperLinkNavigate( object sender, RequestNavigateEventArgs e )
        {
            if ( m_information != null )
            {
                try
                {
                    if ( !string.IsNullOrWhiteSpace( m_information.HTTPLink ) )
                    {
                        Process.Start( m_information.HTTPLink );
                    }
                    else
                    {
                        Log( "Game Update Information", "Opening Link", "Link is empty" );
                    }
                }
                catch ( Exception ex )
                {
                    Log( "Game Update Information", "Opening Link", ex.ToString() );
                }
            }
            else
            {
                Log( "Game Update Information", "Opening Link", "m_information == null" );
            }

            e.Handled = true;
        }

        /// <summary>
        /// Log an issue
        /// </summary>
        /// <param name="_action">The action name</param>
        /// <param name="_key">The log key</param>
        /// <param name="_description">The log description</param>
        private void Log( string _action, string _key, string _description )
        {
            Debug.Assert( !string.IsNullOrWhiteSpace( _action ) );
            Debug.Assert( !string.IsNullOrWhiteSpace( _key ) );
            Debug.Assert( !string.IsNullOrWhiteSpace( _description ) );
            Debug.Assert( m_information.LogEventInterface != null );

            if ( m_information.LogEventInterface != null )
            {
                 m_information.LogEventInterface.LogEvent( _action, _key, _description );
            }
        }

        /// <summary>
        /// Where we get the data from to display and get 
        /// interfaces from.
        /// </summary>
        private Information m_information = null;
    }
}
