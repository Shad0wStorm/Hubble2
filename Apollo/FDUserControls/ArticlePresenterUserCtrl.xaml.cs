//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ArticlePresenterUserCtrl, wraps up articles into aUserControl
//
//! Author:     Alan MacAree
//! Created:    11 Oct 2022
//----------------------------------------------------------------------

using System.Diagnostics;
using System.Windows.Controls;


namespace FDUserControls
{
    /// <summary>
    /// Interaction logic for ArticlePresenterUserCtrl.xaml
    /// </summary>
    public partial class ArticlePresenterUserCtrl : UserControl
    {
        /// <summary>
        /// Default contructor
        /// </summary>
        public ArticlePresenterUserCtrl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Display the title along with the articles
        /// </summary>
        /// <param name="_title">The title to use</param>
        /// <param name="_articleUserCtrl">The control that contains the Articles</param>
        public void DisplayArticles( string _title, GridLayoutUserCtrl _gridLayoutUserCtrl   )
        {
            Debug.Assert( _title != null );
            Debug.Assert( _gridLayoutUserCtrl != null );

            bool somethingWasDisplayed = false;

            // Display the title
            if ( !string.IsNullOrWhiteSpace( _title ) )
            {
                PART_TitleLabel.Content = _title;
                somethingWasDisplayed = true;
            }

            // Display the articles
            if ( _gridLayoutUserCtrl != null )
            {
                PART_ArticleFrame.Content = _gridLayoutUserCtrl;
                somethingWasDisplayed = true;
            }

            /// Only allow the PART_Separator to be displayed if we have 
            /// displayed something in the control.
            if ( somethingWasDisplayed )
            {
                PART_Separator.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void UserControl_Loaded( object sender, System.Windows.RoutedEventArgs e )
        {
            GridLayoutUserCtrl test = PART_ArticleFrame.Content as GridLayoutUserCtrl;
        }
    }
}
