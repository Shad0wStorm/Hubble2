//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! PopupPage, displays messages and a back button to the user
//
//! Author:     Alan MacAree
//! Created:    20 Oct 2022
//----------------------------------------------------------------------

using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for PopupPage.xaml
    /// This displays a popup message to the user, it has:
    /// A title
    /// A main message
    /// A sub message (under the main message)
    /// An OK button, the Page is closed and navigated back to 
    /// the previous page
    /// </summary>
    public partial class PopupPage : Page
    {
        /// <summary>
        /// Constructor that takes all of the parameters
        /// </summary>
        /// <param name="_previousPage">The previous page to return to when OK is clicked</param>
        /// <param name="_title">The title to display</param>
        /// <param name="_mainMsg">The main message to display</param>
        /// <param name="_subMsg">The sub message to display</param>
        public PopupPage( Page _previousPage, string _title, string _mainMsg, string _subMsg )
        {
            m_previousPage = _previousPage;
            InitializeComponent();
            PART_Title.Content = _title;
            PART_MainMessage.Text = _mainMsg;
            PART_SubMessage.Text = _subMsg;
        }

        /// <summary>
        /// Called when the user clicks on the OK button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnOKClick( object sender, RoutedEventArgs e )
        {
            // We should go back to the previous page, but we must also allow 
            // for issues in case we don't have one.
            if ( m_previousPage != null )
            {
                NavigationService.Navigate( m_previousPage );
            }
            else
            {
                NavigationService ns = NavigationService.GetNavigationService( this );
                ns.GoBack();
            }
        }

        /// <summary>
        /// The Page to return to once this Page 
        /// goes away.
        /// </summary>
        private Page m_previousPage = null;
    }
}
