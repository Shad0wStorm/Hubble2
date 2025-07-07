//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! HeroImageUserCtrl UserControl
//
// !Displays a hero image, along with a section at the bottom where
// !text may be displayed.
//
//! Author:     Alan MacAree
//! Created:    27 Oct 2022
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace FDUserControls
{
    /// <summary>
    /// Interaction logic for HeroImageUserCtrl.xaml
    /// </summary>
    public partial class HeroImageUserCtrl : UserControl
    {
        /// <summary>
        /// Shared with XAML
        /// </summary>
        public double SlopeHeight => 50d;

        private int m_fallbackImagesAttemptedIndex = 0;
        private List<string> m_fallbackHeroImages;

        public bool PersistentWarning
        {get; set; }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public HeroImageUserCtrl()
        {
            PersistentWarning = false;
            InitializeComponent();
        }

        /// <summary>
        /// Sets the hero image Uri to use
        /// </summary>
        /// <param name="_heroImageURI">The Uri to the hero image</param>
        public void SetImageUri( string _heroImageURI, List<string> _fallbackHeroImages)
        {
            m_fallbackHeroImages = _fallbackHeroImages;

            // Set image source
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(_heroImageURI, UriKind.Absolute);
            bitmapImage.DownloadFailed += OnHeroImageDownloadFailed;
            bitmapImage.EndInit();

            PART_HeroImage.ImageSource = bitmapImage;
        }

        public void OnHeroImageDownloadFailed(object sender, System.Windows.Media.ExceptionEventArgs eventArgs)
        {
            if (m_fallbackImagesAttemptedIndex < m_fallbackHeroImages.Count)
            {
                BitmapImage thisImage = new BitmapImage();
                thisImage.BeginInit();
                thisImage.UriSource = new Uri(m_fallbackHeroImages[m_fallbackImagesAttemptedIndex], UriKind.Absolute);
                thisImage.DownloadFailed += OnHeroImageDownloadFailed;
                thisImage.EndInit();

                PART_HeroImage.ImageSource = thisImage;

                ++m_fallbackImagesAttemptedIndex;
            }

        }

        /// <summary>
        /// Sets the featured product title, link and ARX
        /// </summary>
        public void SetFeaturedProductDetails(string productTitle, double productPrice, string productCategory, string productLinkLabel)
        {
            if (!string.IsNullOrEmpty(productLinkLabel))
            {
                PART_FeaturedProductLink.Text = productLinkLabel;
            } else
            {
                PART_FeaturedProductLink.Text = "BUY NOW";
            }

            if (!string.IsNullOrEmpty(productTitle))
            {
                PART_FeaturedProductTitle.Text = productTitle.ToUpper();
                PART_FeaturedProductTitle.Visibility = Visibility.Visible;
                PART_FeaturedProductLink.Visibility = Visibility.Visible;
            }

            if (productPrice != 0.0)
            {
                PART_FeaturedProductPrice.Content = productPrice;
                PART_FeaturedProductPrice_Tag.Visibility = Visibility.Visible;
            }

            if (!string.IsNullOrEmpty(productCategory))
            {
                PART_FeaturedProductCategory.Text = productCategory;
                PART_FeaturedProductCategory.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Hides the Default view's age rating
        /// </summary>
        public void HideDefaultViewAgeRating()
        {
            PART_DefaultViewAgeRating.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Reveals the Default view's age rating
        /// </summary>
        public void ShowDefaultViewAgeRating()
        {
            PART_DefaultViewAgeRating.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Hides the featured product blocks from ui
        /// </summary>
        public void HideFeaturedProductBlocks(MouseButtonEventHandler handler)
        {
            PART_FeaturedProductPrice_Tag.Visibility = Visibility.Collapsed;
            PART_FeaturedProductTitle.Visibility = Visibility.Collapsed;
            PART_FeaturedProductLink.Visibility = Visibility.Collapsed;
            PART_FeaturedProductCategory.Visibility = Visibility.Collapsed;

            PART_FeaturedProductLink.RemoveHandler(MouseDownEvent, handler);
            PART_FeaturedProductTitle.RemoveHandler(MouseDownEvent, handler);
            PART_FeaturedProductCategory.RemoveHandler(MouseDownEvent, handler);
        }

        public void SetFeatureProductLinkClickEventHandler(MouseButtonEventHandler handler)
        {
            PART_FeaturedProductLink.MouseDown += handler;
            PART_FeaturedProductTitle.MouseDown += handler;
            PART_FeaturedProductCategory.MouseDown += handler;
        }

        /// <summary>
        /// Called when the user control changes size, this allows for the redrawing of the control,
        /// mainly used to recaluclate the shape that the image is drawn within.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSizeChanged( object sender, SizeChangedEventArgs e )
        {
            double halfActualWidth = ActualWidth/2;
            double startX = halfActualWidth - c_halfSlopeWidth;
            double endX = halfActualWidth + c_halfSlopeWidth;
            double slopeYPos = ActualHeight - SlopeHeight;

            // We already have a start point (Top Left)
            PART_BottomLeftSegment.Point = new Point( 0d, ActualHeight );
            PART_LeftBtmSlopeSegment.Point = new Point( startX, ActualHeight );
            PART_LeftTopSlopeSegment.Point = new Point( startX + c_slopDifferenceX, slopeYPos );
            PART_RightTopSlopeSegment.Point = new Point( endX - c_slopDifferenceX, slopeYPos );
            PART_RightBtmSlopeSegment.Point = new Point( endX, ActualHeight );
            PART_BottomRightSegment.Point = new Point( ActualWidth, ActualHeight );
            PART_TopRightSegment.Point = new Point( ActualWidth, 0d );
        }

        /// <summary>
        /// Displays an age rating image based on the language passed,
        /// the PC language, and the ratingString passed.
        /// 
        /// Special condition, if English language, check if this PC is English US or English UK
        /// If ESRB Language, then use ESRB Icon
        /// If English UK (or any other European language), use PEGI
        /// Anything else, don't display an age rating
        /// </summary>
        /// <param name="_languageString">The game language to set the rating for</param>
        /// <param name="_ratingString">The rating string to determine an image to use</param>
       public void SetAgeRating( string _languageString, 
                                  string _esrbRratingString,
                                  string _pegiRatingString )
        {
            PART_AgeRatingImage.Visibility = Visibility.Collapsed;

            if ( IsLanguageEuropean( _languageString ) )
            {
                if ( UseESRBRating( _languageString ) )
                {
                    if ( !String.IsNullOrEmpty( _esrbRratingString ) )
                    {
                        // If this is English(US) use a ESRB image
                        string esrbImageSource = GetESRBRatingImage( _esrbRratingString  );
                        Debug.Assert( !String.IsNullOrEmpty( esrbImageSource ) );
                        if ( !String.IsNullOrEmpty( esrbImageSource ) )
                        {
                            PART_AgeRatingImage.Source = new BitmapImage( new Uri( esrbImageSource, UriKind.Absolute ) );
                            PART_AgeRatingImage.Visibility = Visibility.Visible;
                            PART_AgeRatingImageESRBOverlay.Visibility = Visibility.Visible;
                            PART_AgeRatingImageESRBOverlay.Source = new BitmapImage( new Uri( esrbImageSource, UriKind.Absolute ) ); ;
                        }
                    }
                }
                else if ( !String.IsNullOrEmpty( _pegiRatingString ) )
                {
                    string pegiImageSource = GetPEGIRatingImage( _pegiRatingString );
                    Debug.Assert( !String.IsNullOrEmpty( pegiImageSource ) );
                    if ( !String.IsNullOrEmpty( pegiImageSource ) )
                    {
                        // We don't use the PART_AgeRatingImageESRBOverlay for PEGI Ratings
                        PART_AgeRatingImageESRBOverlay.Visibility = Visibility.Hidden;
                        PART_AgeRatingImage.Source = new BitmapImage( new Uri( pegiImageSource, UriKind.Absolute ) );
                        PART_AgeRatingImage.Visibility = Visibility.Visible;
                    }
                }
            }

            PART_DefaultViewAgeRating.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Displays the Server Warning, any previous Server Warning
        /// is overwritten. This may cause the Server Warning UI to change
        /// from Hidden to Visible, or just replace any existing text within
        /// the Server Warning UI.
        /// </summary>
        /// <param name="_serverWarningStatusString">The Server Warning status to display,
        /// should contain text and not be null or empty.</param>
        /// <param name="_serverWarningText">The Server Warning Message to display,
        /// should contain text and not be null or empty.</param>
        public void DisplayServerWarning( string _serverWarningStatusString, string _serverWarningStatusMessage )
        {
            bool displayedSomething = false;
            // Warning Status Text (a kind of title)
            if ( !String.IsNullOrEmpty( _serverWarningStatusString ) )
            {
                PART_ServerWarningStatusText.Text = _serverWarningStatusString;
                displayedSomething = true;
            }
            else
            {
                PART_ServerWarningStatusText.Text = string.Empty;
            }

            // Warning Status Message
            if ( !String.IsNullOrEmpty( _serverWarningStatusMessage ) )
            {
                PART_ServerWarningStatusMessage.Text = _serverWarningStatusMessage;
                displayedSomething = true;
            }
            else
            {
                PART_ServerWarningStatusText.Text = string.Empty;
            }

            // Only change the visibility if we have something to display
            if ( displayedSomething )
            {
                PART_ServerWarningGrid.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Hides the Server Warning, it does not matter if the 
        /// Server Warning is already not displayed
        /// </summary>
        public void HideServerWarning()
        {
            PART_ServerWarningStatusText.Text = string.Empty;
            PART_ServerWarningStatusMessage.Text = string.Empty;
            PART_ServerWarningGrid.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Checks to see if the Server Warning is current displayed
        /// </summary>
        /// <returns>true if the server warning is displayed, else returns false</returns>
        public bool IsServerWarningDisplayed()
        {
            return (PART_ServerWarningGrid.Visibility != Visibility.Hidden);
        }

        /// <summary>
        /// Determines if we should use the ERSB rating or not.
        /// URSB is used United States, Canada, and Mexico, this
        /// check only returns true if:
        /// The launcher language is set to English to default language and
        ///    if the PC language is set to United States, Canada, or Mexico.
        /// </summary>
        /// <returns>true if ESRB should be used, else false</returns>
        private bool UseESRBRating( string _languageString )
        {
            bool useERSB = false;

            bool checkPCLanguage = false;
            switch( _languageString )
            {
                case "en":
                case "es":
                case "":
                    checkPCLanguage = true;
                    break;
                default:
                    useERSB = false;
                    break;
            }

            if ( checkPCLanguage )
            {
                CultureInfo culture = CultureInfo.CurrentCulture;
                string languageName = culture.Name;

                switch ( languageName )
                {
                    case "en-US":
                    case "en-CA":
                    case "es-MX":
                        useERSB = true;
                        break;
                    default:
                        useERSB = false;
                        break;
                }
            }

            return useERSB;
        }

        /// <summary>
        /// Returns true if the passed language string is
        /// European
        /// </summary>
        /// <param name="_languageString">The language string to check</param>
        /// <returns>true if European language, else false</returns>
        private bool IsLanguageEuropean( string _languageString )
        {
            bool isEuropean = false;
            switch ( _languageString )
            {
                case "de":
                case "en":
                case "es":
                case "fr":
                case "pt-BR":
                case "":
                    isEuropean = true;
                    break;
                case "ru":
                    isEuropean = false;
                    break;
                default:
                    isEuropean = false;
                    break;
            }

            return isEuropean;
        }

        /// <summary>
        /// Returns the image for the passed ESRB Rating text
        /// </summary>
        /// <param name="_esrbRating">The ESRB text to get the rating for</param>
        /// <returns>The location & name of the image for the ESRB rating</returns>
        private string GetESRBRatingImage( string _esrbRating )
        {
            string esrbImageSource = c_esrbImageLoc;

            Debug.Assert( !String.IsNullOrEmpty( _esrbRating ) );

            if ( !String.IsNullOrEmpty( _esrbRating ) )
            {
                switch ( _esrbRating )
                {
                    case c_esrbTeenText:
                        esrbImageSource += GetESRBTeenImage();
                        break;
                    default:
                        esrbImageSource += GetESRBTeenImage();
                        break;
                }
            }


            return esrbImageSource;
        }

        /// <summary>
        /// Returns the language specific teen ESRB image
        /// </summary>
        /// <returns></returns>
        private string GetESRBTeenImage()
        {
            string esrbImageSource = c_esrbEnTeenImg;
            CultureInfo culture = CultureInfo.CurrentCulture;
            switch ( culture.Name )
            {
                case "en-US":
                case "en-CA":
                    esrbImageSource = c_esrbEnTeenImg;
                    break;
                case "es-MX":
                    esrbImageSource = c_esrbEsTeenImg;
                    break;
                default:
                    esrbImageSource = c_esrbEnTeenImg;
                    break;
            }

            return esrbImageSource;
        }

        /// <summary>
        /// Returns the image for the passed PEGI Rating text
        /// </summary>
        /// <param name="_pegiRating">The PEGI text to get the rating for</param>
        /// <returns>The location & name of the image for the PEGI rating</returns>
        private string GetPEGIRatingImage( string _pegiRating )
        {
            string pegiImageSource = c_pegiImageLoc;

            Debug.Assert( !String.IsNullOrEmpty( _pegiRating ) );

            if ( !String.IsNullOrEmpty( _pegiRating ) )
            {
                switch ( _pegiRating )
                {
                    case c_pegiEighteenImg:
                        pegiImageSource += c_pegiEighteenImg;
                        break;
                    case c_pegiSixteenText:
                        pegiImageSource += c_pegiSixteenImg;
                        break;
                    case c_pegiTwelveText:
                        pegiImageSource += c_pegiTwelveImg;
                        break;
                    case c_pegiSevenText:
                        pegiImageSource += c_pegiSevenImg;
                        break;
                    case c_pegiSixText:
                        pegiImageSource += c_pegiSixImg;
                        break;
                    case c_pegiFourText:
                        pegiImageSource += c_pegiFourImg;
                        break;
                    case c_pegiThreeText:
                        pegiImageSource += c_pegiThreeImg;
                        break;
                    default:
                        pegiImageSource += c_pegiSixteenImg;
                        break;
                }
            }

            return pegiImageSource;
        }

        /// <summary>
        /// The slope width (the bar slope at the bottom of the heroimage)
        /// </summary>
        private const double c_slopeWidth = 500d;

        /// <summary>
        /// Half the slope width (the bar slope at the bottom of the heroimage, 
        /// used in calculations, therefore calculated here rather than dynamically.
        /// </summary>
        private const double c_halfSlopeWidth = c_slopeWidth/2;

        /// <summary>
        /// The bar slope X axis distance (width)
        /// </summary>
        private const double c_slopDifferenceX = 50d;

        /// <summary>
        /// The English(US) language name, this used as a special case for age rating
        /// </summary>
        private const string c_usCultureString = "en-US";

        /// <summary>
        /// Location of the ESRB images
        /// </summary>
        private const string c_esrbImageLoc = "pack://application:,,,/FDUserControls;component/Images/ESRB/";

        /// <summary>
        /// ESRB text and associated image
        /// </summary>
        private const string c_esrbTeenText = "teen";
        private const string c_esrbEnTeenImg = "ESRB_teenEn.png";
        private const string c_esrbEsTeenImg = "ESRB_teenEs.png";

        /// <summary>
        /// Location of the PEGI image
        /// </summary>
        private const string c_pegiImageLoc = "pack://application:,,,/FDUserControls;component/Images/PEGI/";

        /// <summary>
        /// PEGI text and associated image
        /// </summary>
        private const string c_pegiEighteenText = "18";
        private const string c_pegiEighteenImg = "age-18-white.png";
        private const string c_pegiSixteenText = "16";
        private const string c_pegiSixteenImg = "age-16-white.png";
        private const string c_pegiTwelveText = "12";
        private const string c_pegiTwelveImg = "age-12-white.png";
        private const string c_pegiSevenText = "7";
        private const string c_pegiSevenImg = "age-7-white.png";
        private const string c_pegiSixText = "6";
        private const string c_pegiSixImg = "age-6-white.png";
        private const string c_pegiFourText = "4";
        private const string c_pegiFourImg = "age-4-white.png";
        private const string c_pegiThreeText = "3";
        private const string c_pegiThreeImg = "age-3-white.png";
    }
}
