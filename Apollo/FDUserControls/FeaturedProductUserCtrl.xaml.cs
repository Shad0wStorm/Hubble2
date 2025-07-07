//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! FeatureUserCtrl, Contains the controls to display information
//! from a featureproduct class. 
//
//! Author:     Alan MacAree
//! Created:    29 Dec 2022
//----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FDUserControls
{
    /// <summary>
    /// Interaction logic for FeaturedProductUserCtrl.xaml
    /// </summary>
    public partial class FeaturedProductUserCtrl : UserControl
    {
        public FeaturedProduct TheFeaturedProduct { get; private set; } = null;

        private int m_fallbackImagesAttemptedIndex = 0;

        /// <summary>
        /// Default constructor
        /// </summary>
        public FeaturedProductUserCtrl()
        {
            InitializeComponent();
        }

        public FeaturedProductUserCtrl(FeaturedProduct _featuredProduct,
                                        ILogEvent _logEventInterface)
        {
            InitializeComponent();
            DisplayFeaturedProduct(_featuredProduct, _logEventInterface);
        }

        /// <summary>
        /// Displays the featuredproduct information
        /// </summary>
        /// <param name="_featuredProduct">The object containing the info to display</param>
        /// <param name="_logEventInterface">An interface used to log events</param>
        public void DisplayFeaturedProduct(FeaturedProduct _featuredProduct,
                                            ILogEvent _logEventInterface)
        {
            TheFeaturedProduct = _featuredProduct;

            Debug.Assert(TheFeaturedProduct != null);
            Debug.Assert(_logEventInterface != null);

            if (TheFeaturedProduct != null)
            {
                // Display the product image. Try the 'baseimage' first as that's been manually set by the monetisation team in the store
                // if that fails for whatever reason, go down the Gallery, then try Small or Thumbnail as a final fallback.
                // Small and Thumbnail images are often square instead of widescreen, so look huge in the launcher
                if (!string.IsNullOrWhiteSpace(TheFeaturedProduct.ImageUri))
                {
                    BitmapImage thisImage = new BitmapImage();
                    thisImage.BeginInit();
                    thisImage.UriSource = new Uri(TheFeaturedProduct.ImageUri, UriKind.Absolute);
                    thisImage.DownloadFailed += OnImageDownloadFailed;
                    thisImage.EndInit();
                    PART_Image.Source = thisImage;
                }
                else
                {
                    if (_logEventInterface != null)
                    {
                        string action = GetType().ToString() + "::" + MethodBase.GetCurrentMethod().Name;
                        string message = TheFeaturedProduct.ToString();
                        _logEventInterface.LogEvent(action, " m_featuredProduct.ImageUri is empty", message);
                    }
                }

                // Display the product price
                if (!string.IsNullOrWhiteSpace(TheFeaturedProduct.Price))
                {
                    PART_Price.Content = TheFeaturedProduct.Price;
                }
                else
                {
                    if (_logEventInterface != null)
                    {
                        string action = GetType().ToString() + "::" + MethodBase.GetCurrentMethod().Name;
                        string message = TheFeaturedProduct.ToString();
                        _logEventInterface.LogEvent(action, " m_featuredProduct.Price is empty", message);
                    }
                }

                // Display the product title
                if (!string.IsNullOrWhiteSpace(TheFeaturedProduct.Title))
                {
                    PART_Title.Content = TheFeaturedProduct.Title;
                }
                else
                {
                    if (_logEventInterface != null)
                    {
                        string action = GetType().ToString() + "::" + MethodBase.GetCurrentMethod().Name;
                        string message = TheFeaturedProduct.ToString();
                        _logEventInterface.LogEvent(action, " m_featuredProduct.Title is empty", message);
                    }
                }
            }
            else
            {
                // We do not have a _featuredProduct, log it
                if (_logEventInterface != null)
                {
                    string action = GetType().ToString() + "::" + MethodBase.GetCurrentMethod().Name;
                    _logEventInterface.LogEvent(action, "_featuredProduct", "is null");
                }
            }
        }

        public void OnImageDownloadFailed(object sender, System.Windows.Media.ExceptionEventArgs eventArgs)
        {
            // todo: check eventArgs to see if this is a retryable thing? for now, just assume it's filenotfoundexception or fileformatexception and rotate on to the next image
            if (m_fallbackImagesAttemptedIndex < TheFeaturedProduct.FallbackImages.Count)
            {
                BitmapImage thisImage = new BitmapImage();
                thisImage.BeginInit();
                thisImage.UriSource = new Uri(TheFeaturedProduct.FallbackImages[m_fallbackImagesAttemptedIndex], UriKind.Absolute);
                thisImage.DownloadFailed += OnImageDownloadFailed;
                thisImage.EndInit();
                PART_Image.Source = thisImage;
                ++m_fallbackImagesAttemptedIndex;
            }

        }

    }
}
