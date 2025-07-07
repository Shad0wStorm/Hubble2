//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ArticleFullTextPage, displays an Article information. If full text is
//! not available, then short text will be displayed.
//
//! Author:     Alan MacAree
//! Created:    19 Oct 2022
//----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace FDUserControls
{
    /// <summary>
    /// Interaction logic for ArticleFullTextPage.xaml
    /// </summary>
    public partial class ArticleFullTextPage : Page
    {
        /// <summary>
        /// The Article that this page displays
        /// </summary>
        public Article TheArticle { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_article">The Article to display</param>
        /// <param name="_previousPage">The page to navigate back to</param>
        public ArticleFullTextPage( Article _article, Page _previousPage )
        {
            Debug.Assert( _article != null );
            Debug.Assert( _previousPage != null );

            TheArticle = _article;
            m_previousPage = _previousPage;
            InitializeComponent();
            DisplayArticle();
        }

        /// <summary>
        /// Displays the Article.
        /// Note that using data binding would cause a more complex
        /// class, therefore using this approach to make it simple.
        /// </summary>
        private void DisplayArticle()
        {
            if ( TheArticle != null )
            {
                // Display the "Date - System" string
                string dateSystemString = GetDateSystem();
                if ( !string.IsNullOrWhiteSpace( dateSystemString ) )
                {
                    PART_DateSystemLabel.Content = GetDateSystem();
                }

                // Display the Article title
                if ( !string.IsNullOrWhiteSpace( TheArticle.Title ) )
                {
                    PART_TitleTextLabel.Content = TheArticle.Title;
                }

                // Display the Article text
                //
                // If we don't have any full text, try at least to display the
                // short text.
                if ( !string.IsNullOrWhiteSpace( TheArticle.FullText ) )
                {
                    // The control cannnot handle newlines very well, so we
                    // add in NewLines
                    PART_FullTextBlock.Text = InsertNewLines( TheArticle.FullText );
                }
                else
                {
                    if ( !string.IsNullOrWhiteSpace( TheArticle.ShortText ) )
                    {
                        // The control cannnot handle newlines very well, so we
                        // add in NewLines
                        PART_FullTextBlock.Text = InsertNewLines( TheArticle.ShortText );
                    }
                }
            }
        }

        /// <summary>
        /// Inserts Environment.NewLine into places where \n are found
        /// </summary>
        /// <param name="_theString"></param>
        /// <returns></returns>
        private string InsertNewLines( string _theString )
        {
            return _theString.Replace( c_NewLine, c_NewLine + Environment.NewLine );
        }
        /// <summary>
        /// Returns the "Date - System" name as a string.
        /// </summary>
        /// <returns>The Date System string, this may be null.</returns>
        private string GetDateSystem()
        {
            string dateSystemString = null;

            if ( TheArticle != null )
            {
                if ( !string.IsNullOrWhiteSpace( TheArticle.DateAsString ) )
                {
                    dateSystemString = TheArticle.DateAsString;
                }
                if ( !string.IsNullOrWhiteSpace( TheArticle.System ) )
                {
                    if ( !string.IsNullOrWhiteSpace( dateSystemString ) )
                    {
                        dateSystemString += c_dateSystemDivider;
                    }
                    dateSystemString += TheArticle.System;
                }
            }

            return dateSystemString;
        }

        /// <summary>
        /// BackButton click, uses NavigationService to return
        /// to previous page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBackClick( object sender, RoutedEventArgs e )
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
        /// The page to navigate back to
        /// </summary>
        private Page m_previousPage;

        /// <summary>
        /// The divider between the date and system (displayed
        /// on the page.
        /// </summary>
        private const string c_dateSystemDivider = " - ";

        /// <summary>
        /// Define the new line
        /// </summary>
        private const string c_NewLine = "\n";
    }
}
