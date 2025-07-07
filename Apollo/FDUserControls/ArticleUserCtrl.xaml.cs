//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ArticleUserCtrl, Contains the controls to display information
//! from an Article class. 
//! This includes: Title, Text, images (overlayed) and a date (overlayed)
//
//! Author:     Alan MacAree
//! Created:    29 Sept 2022
//----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FDUserControls
{
    /// <summary>
    /// Interaction logic for ArticleUserCtrl.xaml
    /// </summary>
    public partial class ArticleUserCtrl : UserControl
    {
        /// <summary>
        /// The Article we are displaying
        /// </summary>
        public Article TheArticle { get; private set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ArticleUserCtrl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor that populates the internal controls
        /// </summary>
        /// <param name="_article">Where to get the data from</param>
        /// <param name="_bUseShortText">true will cause the short text from the Article to be displayed</param>
        /// <param name="_overlayDate">true will cause the date to overlay the images</param>
        /// <param name="_hideText">if true, the text is not displayed, but the title is</param>
        /// <param name="_logEventInterface">Interface to log problems</param>
        public ArticleUserCtrl( Article _article, 
                                bool _bUseShortText, 
                                bool _overlayDate, 
                                bool _hideText,
                                ILogEvent _logEventInterface )
        {
            InitializeComponent();

            // We cannot do much about an Article that was not populated correctly,
            // so let it fail rather than causing an error at this point.
            // Note that PopulatedOkay is set to true or false
            PopulatedOkay = PopulateFromArticle( _article, _bUseShortText, _overlayDate, _hideText, _logEventInterface );
        }

        /// <summary>
        /// Populates this control from the passed Article
        /// </summary>
        /// <param name="_article">The Article to use as a source to display info</param>
        /// <param name="_displayShortText">true will cause the short text from the Article to be displayed</param>
        /// <param name="_overlayDate">true will cause the date to overlay the images</param>
        /// <param name="_hideText">Hides all text if set to true</param>
        /// <param name="_logEventInterface">Interface to log problems</param>
        /// <returns>true if the control was populated okay</returns>
        public bool PopulateFromArticle( Article _article, 
                                         bool _displayShortText, 
                                         bool _overlayDate,
                                         bool _hideText,
                                         ILogEvent _logEventInterface )
        {
            Debug.Assert( _article != null );
            Debug.Assert( _logEventInterface != null );

            // Keep the Article
            TheArticle = _article;

            bool wasPopulatedOkay = true;

            // Clear any current data
            PART_ImageGrid.Children.Clear();
            PART_TitleLabel.Text = "";
            PART_TextBlock.Text = "";

            if ( _article != null )
            {
                if ( !_hideText )
                {
                    // Display the article text
                    if ( !DisplayArticleText( _article, _displayShortText, _logEventInterface ) )
                    {
                        // We failed display the text
                        if ( _logEventInterface != null )
                        {
                            string action = GetType().ToString() + Consts.c_classMethodSeparator + MethodBase.GetCurrentMethod().Name;
                            string description = _article.Title;
                            _logEventInterface.LogEvent( action, "Failed to display article text", description );
                        }
                        wasPopulatedOkay = false;
                    }
                }

                // Overlay the article images
                if ( OverlayImages( _article, _logEventInterface ) )
                {
                    if ( _overlayDate )
                    {
                        // Overlay the date on the images
                        if ( !OverlayDateOnImages( _article.DateAsString, _logEventInterface ) )
                        {
                            // We failed to overlay the date
                            if ( _logEventInterface != null )
                            {
                                string action = GetType().ToString() + Consts.c_classMethodSeparator + MethodBase.GetCurrentMethod().Name;
                                string description = _article.Title + " " + _article.DateAsString;
                                _logEventInterface.LogEvent( action, "Failed to overlay date", description );
                            }
                            wasPopulatedOkay = false;
                        }
                    }
                }
                else
                {
                    // We failed to overlay the images.
                    if ( _logEventInterface != null )
                    {
                        string action = GetType().ToString() + Consts.c_classMethodSeparator + MethodBase.GetCurrentMethod().Name;
                        _logEventInterface.LogEvent( action, "Failed to overlay images", _article.Title );
                    }
                    // wasPopulatedOkay = false; // carry on anyway with blank images
                }
            }

            // Remember if we populated the UserControl okay or not
            PopulatedOkay = wasPopulatedOkay;

            return wasPopulatedOkay;
        }

        /// <summary>
        /// Displays the articles text
        /// </summary>
        /// <param name="_article">The Article to display the text from</param>
        /// <param name="_displayShortText">If we should display the short text (true)  or long text (false)</param>
        /// <param name="_logEventInterface">Used to log errors</param>
        /// <returns>Returns true if the text was displayed okay</returns>
        private bool DisplayArticleText( Article _article, bool _displayShortText, ILogEvent _logEventInterface )
        {
            Debug.Assert( _article != null );
            Debug.Assert( _logEventInterface != null );

            bool displayedTextOkay = true;

            if ( _article != null )
            {
                // Display the title
                if ( !string.IsNullOrWhiteSpace( _article.Title ) )
                {
                    PART_TitleLabel.Text = _article.Title;
                }
                else
                {
                    if ( _logEventInterface != null )
                    {
                        string action = GetType().ToString() + Consts.c_classMethodSeparator + MethodBase.GetCurrentMethod().Name;
                        string description = _article.Title;
                        _logEventInterface.LogEvent( action, "No Title", description );
                    }
                    displayedTextOkay = false;
                }

                // Assume we are displaying short text, and change to full text if we need to.
                // Also keep track of any possible error info we need to report.
                string textToDisplay = _article.ShortText;
                string errorInfo = "No Short Text";
                if ( !_displayShortText )
                {
                    textToDisplay = _article.FullText;
                    errorInfo = "No Full Text";
                }

                // Display the text
                if ( !string.IsNullOrWhiteSpace( textToDisplay ) )
                {
                    PART_TextBlock.Text = textToDisplay;
                }
                else
                {
                    // We have a problem, log it
                    if ( _logEventInterface != null )
                    {
                        string action = GetType().ToString() + Consts.c_classMethodSeparator + MethodBase.GetCurrentMethod().Name;
                        string description = _article.Title;
                        _logEventInterface.LogEvent( action, errorInfo, description );
                    }
                    displayedTextOkay = false;
                }
            }
            else
            {
                // No Article
                if ( _logEventInterface != null )
                {
                    string action = GetType().ToString() + Consts.c_classMethodSeparator + MethodBase.GetCurrentMethod().Name;;
                    _logEventInterface.LogEvent( action, "No Article", "" );
                }
                displayedTextOkay = false;
            }

            return displayedTextOkay;
        }

        /// <summary>
        /// Sets the list of the image overlays and creates the
        /// final image to display.
        /// </summary>
        /// <param name="_imageList"></param>
        /// <param name="_logEventInterface">Interface to log events</param>
        /// <returns>true if all of the images were added okay, or if we had noen to add</returns>
        private bool OverlayImages( Article _article, 
                                    ILogEvent _logEventInterface )
        {
            Debug.Assert( _article != null );
            Debug.Assert( _logEventInterface != null );

            bool allImagesAdded = true;

            if ( _article.OverlayList != null )
            {
                // For each image, get it, and display it on top of each over
                // in the order dictated by _article.OverlayList.
                foreach ( string imageName in _article.OverlayList )
                {
                    // Make sure we have an image name
                    if ( !string.IsNullOrWhiteSpace( imageName ) )
                    {
                        string fullImageName = imageName;
                        // Do we need to add an image xtension
                        if ( !string.IsNullOrWhiteSpace( _article.ImageExtension ) )
                        {
                            fullImageName += _article.ImageExtension;
                        }

                        // Make sure we have a URL to download the image from
                        if ( !string.IsNullOrWhiteSpace( _article.ImageURL ) )
                        {
                            string fullUri = _article.ImageURL;
                            fullUri += fullImageName;

                            try
                            {
                                // Create the image, set it to the downloaded image and 
                                // set the other values (row, stretch etc).
                                Image image = new Image();
                                image.Source = new BitmapImage( new Uri( fullUri, UriKind.Absolute ) );
                                image.Stretch = System.Windows.Media.Stretch.UniformToFill;
                                image.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                                _ = PART_ImageGrid.Children.Add( image );
                            }
                            catch ( Exception ex )
                            {
                                // Silently fail
                                Debug.Assert( false );
                                string action = GetType().ToString() + Consts.c_classMethodSeparator + MethodBase.GetCurrentMethod().Name;
                                LogArticleIssue( _logEventInterface, _article, action, ex.Message, _article.Title );
                                allImagesAdded = false;
                            }
                        }
                        else
                        {
                            string action = GetType().ToString() + Consts.c_classMethodSeparator + MethodBase.GetCurrentMethod().Name;
                            string description = imageName + " " + _article.Title;
                            LogArticleIssue( _logEventInterface, _article, action, description, _article.Title );
                            allImagesAdded = false;
                        }
                    }
                    else
                    {
                        // We do not have an image name
                        string action = GetType().ToString() + Consts.c_classMethodSeparator + MethodBase.GetCurrentMethod().Name;
                        LogArticleIssue( _logEventInterface, _article, action, "No Image Name", _article.Title );
                        allImagesAdded = false;
                    }
                }
            }

            return allImagesAdded;
        }

        /// <summary>
        /// Log an Article issue
        /// </summary>
        /// <param name="_logEventInterface">The interface used to log the issue</param>
        /// <param name="_article">Which article this issue is for</param>
        /// <param name="_action">The action that was taking place, or class + method name</param>
        /// <param name="_key">The key for the log, i.e. what happened</param>
        /// <param name="_description">The description for the log</param>
        private void LogArticleIssue( ILogEvent _logEventInterface, Article _article, string _action, string _key, string _description )
        {
            if ( _logEventInterface != null )
            {
                string issueDescription = "";
                if ( !string.IsNullOrWhiteSpace( _article.System ) )
                {
                    issueDescription = _article.System;
                    issueDescription += Consts.c_errorStringSeparator;
                }

                issueDescription += _description;
                _logEventInterface.LogEvent( _action, _key, issueDescription );
            }
        }

        /// <summary>
        /// Overlays a date on the images, the date is left/bottom aligned.
        /// </summary>
        /// <param name="_text">The text to overlay</param>
        /// <param name="_logEventInterface">Interface to log events</param>
        /// <returns>true if the text was overlayed okay</returns>
        private bool OverlayDateOnImages( string _dateText, ILogEvent _logEventInterface )
        {
            bool wasTextOverlayed = false;

            try
            {
                if ( !string.IsNullOrWhiteSpace( _dateText ) )
                {
                    Label label = new Label();
                    _= label.Content = _dateText;
                    _= label.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    _= label.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                    _= label.Foreground = new SolidColorBrush( m_textOverlayColour );
                    _ = PART_Grid.Children.Add( label );
                    wasTextOverlayed = true;
                }
                else
                {
                    if ( _logEventInterface != null )
                    {
                        // Log the problem
                        string action = GetType().ToString() + Consts.c_classMethodSeparator + MethodBase.GetCurrentMethod().Name;
                        _logEventInterface.LogEvent( action, "No Text to overlay on images", "" );
                    }
                }
            }
            catch( Exception ex )
            {
                if ( _logEventInterface != null )
                {
                    // Log the problem
                    string action = GetType().ToString() + Consts.c_classMethodSeparator + MethodBase.GetCurrentMethod().Name;
                    _logEventInterface.LogEvent( action, _dateText, ex.ToString() );
                }
                wasTextOverlayed = false;
            }

            return wasTextOverlayed;
        }

        /// <summary>
        /// Indicates if this user control has been populated okay.
        /// </summary>
        public bool PopulatedOkay { get; private set; } = false;

        /// <summary>
        /// The colour of the text overlayed on images.
        private Color m_textOverlayColour = Colors.LightGray;
    }
}
