//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! DynContentUserCtrl UserControl
//
//! Author:     Alan MacAree
//! Created:    26 Sep 2022
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CBViewModel;
using ClientSupport;
using FDUserControls;
using FORCServerSupport;
using JSONConverters;
using LauncherModel;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for DynContentUserCtrl.xaml
    /// </summary>
    public partial class DynContentUserCtrl : UserControl
    {
        /// <summary>
        /// FORC store id of hero featured product
        /// </summary>
        private string featuredProductSkuName;

        /// <summary>
        /// FORC store link of hero featured product
        /// </summary>
        private string featuredProductLink;

        /// <summary>
        /// Default consrucor
        /// </summary>
        public DynContentUserCtrl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_frontPage">The FrontPage that this user control is displayed on</param>
        /// <param name="_dynamicContentModel">The DynamicContentModel to get the data to display from</param>
        public DynContentUserCtrl( FrontPage _frontPage, DynamicContentModel _dynamicContentModel )
        {
            Debug.Assert( _frontPage != null );
            Debug.Assert( _dynamicContentModel != null );

            InitializeComponent();
            SetupObject( _frontPage, _dynamicContentModel );
        }

        /// <summary>
        /// Sets the FrontPage, this is needed as this control needs access to other
        /// objects that FrontPage provides.
        /// </summary>
        /// <param name="_frontPage">The FrontPage that this user control is displayed on<</param>
        /// <param name="_dynamicContentModel">The DynamicContentModel to get the data to display from</param>
        public void SetupObject( FrontPage _frontPage, DynamicContentModel _dynamicContentModel )
        {
            Debug.Assert( _frontPage != null );
            Debug.Assert( _dynamicContentModel != null );

            m_frontPage = _frontPage;
            m_dynamicContentModel = _dynamicContentModel;

            if ( m_frontPage != null )
            {
                LauncherWindow launcherWindow = m_frontPage.GetLauncherWindow();
                Debug.Assert( launcherWindow != null );
                if ( launcherWindow != null )
                {
                    CobraBayView cobraBayView = launcherWindow.GetCobraBayView();
                    Debug.Assert( cobraBayView != null );
                    if ( cobraBayView != null )
                    {
                        SetProduct( cobraBayView.GetActiveProject() );
                        // init scrolling timer
                        m_scrollEventTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    }
                }
            }
        }

        /// <summary>
        /// Waits for any UI Updates
        /// </summary>
        /// <returns></returns>
        public void WaitForAnyUIUpdates()
        {
            Task.WaitAll( m_waitUIUpdateTaskList.ToArray() );
        }

        /// <summary>
        /// Ses the project that this User Control deals with
        /// </summary>
        /// <param name="_project"></param>
        public void SetProduct( Project _project )
        {
            if ( m_project != _project )
            {
                m_project = _project;
                Update();
            }
        }

        /// <summary>
        /// Updates the Launcher window
        /// </summary>
        /// <param name="_serverStatusState">The server status</param>
        /// <param name="_serverStatusText">The server status text</param>
        /// <param name="_serverStatusMessage">The server status message</param>
        public void ServerStatusUpdate( InfoUserCtrl.ServerStatusState _serverStatusState, string _serverStatusText, string _serverStatusMessage )
        {
            switch ( _serverStatusState )
            {
                case InfoUserCtrl.ServerStatusState.OK:
                    if ( PART_HeroImageUserCtrl.IsServerWarningDisplayed() && !PART_HeroImageUserCtrl.PersistentWarning)
                    {
                        PART_HeroImageUserCtrl.HideServerWarning();
                    }
                    break;
                case InfoUserCtrl.ServerStatusState.NotOk:
                case InfoUserCtrl.ServerStatusState.Maintainance:
                    PART_HeroImageUserCtrl.DisplayServerWarning( _serverStatusText, _serverStatusMessage );
                    break;
                default:
                    // unexpected status, just hide the warning if it is displayed
                    if ( PART_HeroImageUserCtrl.IsServerWarningDisplayed() )
                    {
                        PART_HeroImageUserCtrl.HideServerWarning();
                    }
                    break;
            }
        }

        /// <summary>
        /// Updates the display
        /// </summary>
        private void Update()
        {
            m_waitUIUpdateTaskList.Clear();

            if ( m_project != null )
            {
                if (!m_project.NoDetails)
                {
                    m_waitUIUpdateTaskList.Add(UpdateHeroImageToFeaturedProductAsync());
                }
                else
                {
                    m_waitUIUpdateTaskList.Add(UpdateHeroImageAsync());
                }

                m_waitUIUpdateTaskList.Add( UpdateGalnetNewsAsync() );
                m_waitUIUpdateTaskList.Add( UpdateCommunityNewsAsync());
                m_waitUIUpdateTaskList.Add( UpdateCommunityGoalsAsync());
                m_waitUIUpdateTaskList.Add( UpdateFeaturedProductsAsync());
            }
        }

        /// <summary>
        /// Update the Galnet news
        /// </summary>
        private async Task UpdateGameDescriptionAsync()
        {
            Debug.Assert( m_dynamicContentModel != null );
            Debug.Assert( m_frontPage != null );

            LauncherWindow launcherWindow = null;

            if ( m_frontPage != null )
            {
                launcherWindow = m_frontPage.GetLauncherWindow();
                Debug.Assert( launcherWindow != null );
            }
            if ( m_dynamicContentModel != null && launcherWindow != null )
            {

                GameDescription gameDescription = null; 

                await Task.Run( () =>
                {
                    gameDescription = m_dynamicContentModel.GetGameDescription();
                } );

                if ( gameDescription != null )
                {

                    GridLayoutUserCtrl gridLayoutUserCtrl = new GridLayoutUserCtrl()
                    {
                        NumberOfColumns = c_numberOfGameDescriptionColumns,
                        Layout = c_gameDescriptionLayout
                    };

                    gridLayoutUserCtrl.CreateGridLayout( 1 );

                    Article article = new Article()
                    {
                        FullText = gameDescription.Description
                    };

                    bool displayShortText = false;
                    bool overlayDate = false;
                    bool shouldTheTextBeHidden = false;
                    ArticleUserCtrl articleUserCtrl = new ArticleUserCtrl( article,
                                                                            displayShortText,
                                                                            overlayDate,
                                                                            shouldTheTextBeHidden,
                                                                            launcherWindow );

                    _ = gridLayoutUserCtrl.AddUIElement( 1, articleUserCtrl );

                    // Was removed as we no longer display the game description
                    // PART_GameDescriptionPresentation.DisplayArticles( LocalResources.Properties.Resources.TITLE_GameDescription, gridLayoutUserCtrl );
                }
                        
            }
        }

        /// <summary>
        /// Update the Galnet news
        /// </summary>
        private async Task UpdateGalnetNewsAsync()
        {
            Debug.Assert( m_dynamicContentModel != null );
            Debug.Assert( m_frontPage != null );

            LauncherWindow launcherWindow = null;

            if ( m_frontPage != null )
            {
                launcherWindow = m_frontPage.GetLauncherWindow();
                Debug.Assert( launcherWindow != null );
            }

            if ( m_dynamicContentModel != null && launcherWindow != null )
            {
                GalnetNews galnetNews = null;

                await Task.Run( () =>
                {
                    galnetNews = m_dynamicContentModel.GetLatestGalnetNews();
                } );

                if ( galnetNews != null )
                {
                    /// Make sure we have valid news.
                    if ( galnetNews.IsValid() )
                    {
                        List<Bulletin> bulletinList = galnetNews.Bulletins;
                        if ( bulletinList != null )
                        {
                            if ( bulletinList.Count > 0 )
                            {
                                bulletinList = OrderBulletinListByDate( bulletinList );

                                GridLayoutUserCtrl gridLayoutUserCtrl = new GridLayoutUserCtrl()
                                {
                                    NumberOfColumns = c_numberOfGalnetColumns,
                                    Layout = c_galnetLayout
                                };
                                gridLayoutUserCtrl.CreateGridLayout( bulletinList.Count );

                                // For each bulletin, create an Article and use that to populate
                                // an ArticleUserCtrl. The ArticleUserCtrl is then added to the
                                // PART_GalnetDynGrid using the idx for a panel number.
                                bool continueAdding = true;
                                for ( int idx = 0; idx < bulletinList.Count && continueAdding; idx++ )
                                {
                                    Article article = new Article()
                                    {
                                        System = galnetNews.System,
                                        Title = bulletinList[idx].Params.Item.Title,
                                        FullText = bulletinList[idx].Params.Item.BulletinText,
                                        ShortText = bulletinList[idx].Params.Item.BulletinShortText,
                                        DateAsString = bulletinList[idx].Date,
                                        ImageURL = galnetNews.ImageURL,
                                        ImageExtension = galnetNews.ImageExtension,
                                        OverlayList = bulletinList[idx].Params.Item.CompoundedImageList()
                                    };

                                    bool displayShortText = true;
                                    bool overlayDate = true;

                                    Debug.Assert( m_frontPage != null );
                                    if ( m_frontPage != null )
                                    {
                                        bool shouldTheTextBeHidden = false;
                                        ArticleUserCtrl articleUserCtrl = new ArticleUserCtrl( article,
                                                                                               displayShortText,
                                                                                               overlayDate,
                                                                                               shouldTheTextBeHidden,
                                                                                               launcherWindow );
                                        if ( articleUserCtrl.PopulatedOkay )
                                        {
                                            articleUserCtrl.Cursor = Cursors.Hand;
                                            articleUserCtrl.MouseDown += OnGalnetClick;
                                            continueAdding = gridLayoutUserCtrl.AddUIElement( idx + 1, articleUserCtrl );
                                        }
                                    }
                                }

                                PART_GalnetNewsPresentation.DisplayArticles( LocalResources.Properties.Resources.TITLE_Galnet, gridLayoutUserCtrl );
                            }
                        }
                    }
                }
            }

        }

        /// <summary>
        /// User clicks on an Article, displays the Articles full details in a new Page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnGalnetClick( object sender, MouseButtonEventArgs e )
        {
            ArticleUserCtrl articleUserCtrl = sender as ArticleUserCtrl;
            Debug.Assert( articleUserCtrl != null );

            if ( articleUserCtrl != null )
            {
                Article article = articleUserCtrl.TheArticle;
                Debug.Assert( article != null );

                Dictionary<string, string> dataToLog = new Dictionary<string, string>();
                dataToLog.Add("galnet_news_article", article.Title);
                LogEvent("OnGalnetClick", dataToLog);

                if ( article != null )
                {
                    // If we don't have a m_frontPage, we don't want to navigate to the
                    // full article because we will never return to the correct place.
                    Debug.Assert( m_frontPage != null );
                    if ( m_frontPage != null )
                    {
                        ArticleFullTextPage articleFullTextPage = new ArticleFullTextPage( article, m_frontPage );
                        m_frontPage.NavigationService.Navigate( articleFullTextPage );
                    }
                }
            }
        }

        /// <summary>
        /// Update the community news
        /// </summary>
        private async Task UpdateCommunityNewsAsync()
        {
            Debug.Assert( m_dynamicContentModel != null );
            Debug.Assert( m_frontPage != null );

            LauncherWindow launcherWindow = null;

            if ( m_frontPage != null )
            {
                launcherWindow = m_frontPage.GetLauncherWindow();
                Debug.Assert( launcherWindow != null );
            }

            if ( m_dynamicContentModel != null && launcherWindow != null )
            {
                CommunityNews communityNews = null;
                await Task.Run( () =>
                {
                    communityNews = m_dynamicContentModel.GetLatestCommunityNews();
                } );

                if ( communityNews != null )
                {
                    /// Make sure we have valid news.
                    if ( communityNews.IsValid() )
                    {
                        List<Bulletin> bulletinList = communityNews.Bulletins;
                        if ( bulletinList != null )
                        {
                            if ( bulletinList.Count > 0 )
                            {
                                bulletinList = OrderBulletinListByDate( bulletinList );

                                GridLayoutUserCtrl gridLayoutUserCtrl = new GridLayoutUserCtrl()
                                {
                                    NumberOfColumns = c_numberOfCommunityNewsColumns,
                                    Layout = c_communityNewsLayout
                                };

                                gridLayoutUserCtrl.CreateGridLayout( bulletinList.Count );

                                // For each bulletin, create an Article and use that to populate
                                // an ArticleUserCtrl. 
                                bool continueAdding = true;
                                for ( int idx = 0; idx < bulletinList.Count && continueAdding; idx++ )
                                {
                                    Article article = new Article()
                                    {
                                        System = communityNews.System,
                                        Title = bulletinList[idx].Params.Item.Title,
                                        FullText = bulletinList[idx].Params.Item.BulletinText,
                                        ShortText = bulletinList[idx].Params.Item.BulletinShortText,
                                        DateAsString = bulletinList[idx].Date,
                                        ImageURL = communityNews.ImageURL,
                                        ImageExtension = communityNews.ImageExtension,
                                        OverlayList = bulletinList[idx].Params.Item.CompoundedImageList()
                                    };

                                    bool displayShortText = true;
                                    bool overlayDate = true;

                                    Debug.Assert( m_frontPage != null );
                                    if ( m_frontPage != null )
                                    {
                                        bool shouldTheTextBeHidden = false;
                                        ArticleUserCtrl articleUserCtrl = new ArticleUserCtrl( article,
                                                                                                displayShortText,
                                                                                                overlayDate,
                                                                                                shouldTheTextBeHidden,
                                                                                                launcherWindow );
                                        if ( articleUserCtrl.PopulatedOkay )
                                        {
                                            continueAdding = gridLayoutUserCtrl.AddUIElement( idx + 1, articleUserCtrl );
                                        }
                                    }
                                }

                                PART_CommunityNewsPresentation.DisplayArticles( LocalResources.Properties.Resources.TITLE_CommunityNews, gridLayoutUserCtrl );
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update the community negoalsws
        /// </summary>
        private async Task UpdateCommunityGoalsAsync()
        {
            Debug.Assert( m_frontPage != null );

            if ( m_frontPage != null )
            {
                LauncherWindow launcherWindow = m_frontPage.GetLauncherWindow();
                Debug.Assert( launcherWindow != null );
                if ( m_dynamicContentModel != null && launcherWindow != null )
                {
                    CommunityGoals communityGoals = null;

                    await Task.Run( () =>
                    {
                        communityGoals = m_dynamicContentModel.GetLatestCommunityGoals();
                    } );

                    if ( communityGoals != null )
                    {
                        /// Make sure we have valid goals.
                        if ( communityGoals.IsValid() )
                        {
                            List<Goal> goalsList = communityGoals.Goals;
                            if ( goalsList != null )
                            {
                                if ( goalsList.Count > 0 )
                                {
                                    GridLayoutUserCtrl gridLayoutUserCtrl = new GridLayoutUserCtrl()
                                    {
                                        NumberOfColumns = c_numberOfCommunityGoalsColumns,
                                        Layout = c_communityGoalsLayout
                                    };
                                    gridLayoutUserCtrl.CreateGridLayout( goalsList.Count );

                                    // For each bulletin, create an Article and use that to populate
                                    // an ArticleUserCtrl. The ArticleUserCtrl is then added to the
                                    // PART_GalnetDynGrid using the idx for a panel number.
                                    bool continueAdding = true;
                                    for ( int idx = 0; idx < goalsList.Count && continueAdding; idx++ )
                                    {
                                        // ToDo, replace Ends text with string from resource, what about UTC??????
                                        string fulltext = string.Format( "Ends: {0} UTC\n", goalsList[idx].ExpiryAsString );
                                        Article article = new Article()
                                        {
                                            System = communityGoals.System,
                                            Title = goalsList[idx].Title,
                                            FullText = fulltext + goalsList[idx].News,
                                            DateAsString = goalsList[idx].ExpiryAsString,
                                            ImageURL = communityGoals.ImageURL,
                                            ImageExtension = communityGoals.ImageExtension,
                                            OverlayList = goalsList[idx].CompoundedImageList()
                                        };
                                        // Remove unwanted strings within the article text.
                                        article.RemoveUnwantedStringsFromText();

                                        bool displayShortText = false;
                                        bool overlayDate = false;

                                        Debug.Assert( m_frontPage != null );
                                        if ( m_frontPage != null )
                                        {
                                            bool shouldTheTextBeHidden = false;
                                            ArticleUserCtrl articleUserCtrl = new ArticleUserCtrl( article,
                                                                                                    displayShortText,
                                                                                                    overlayDate,
                                                                                                    shouldTheTextBeHidden,
                                                                                                    launcherWindow );
                                            if ( articleUserCtrl.PopulatedOkay )
                                            {
                                                continueAdding = gridLayoutUserCtrl.AddUIElement( idx + 1, articleUserCtrl );
                                            }
                                        }
                                    }

                                    PART_CommunityGoalsPresentation.DisplayArticles( LocalResources.Properties.Resources.TITLE_CommunityGoals, gridLayoutUserCtrl );
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the FeaturedProducts part of the display
        /// </summary>
        private async Task UpdateFeaturedProductsAsync()
        {
            Debug.Assert( m_frontPage != null );

            if ( m_frontPage != null )
            {
                LauncherWindow launcherWindow = m_frontPage.GetLauncherWindow();
                Debug.Assert( launcherWindow != null );
                if ( m_dynamicContentModel != null && launcherWindow != null )
                {
                    Featured featuredProducts = null;
                    await Task.Run( () =>
                    {
                        featuredProducts = m_dynamicContentModel.GetFeaturedProducts();

                    } );

                    if ( featuredProducts != null )
                    {
                        FeaturedContainer featuredContainer = featuredProducts.TheFeaturedContainer;
                        if ( featuredContainer != null )
                        {
                            List<FeaturedProducts> featuredProductsList = featuredContainer.FeaturedProductsList;
                            if ( featuredProductsList != null )
                            {
                                if ( featuredProductsList.Count > 0 )
                                {
                                    GridLayoutUserCtrl gridLayoutUserCtrl = new GridLayoutUserCtrl()
                                    {
                                        NumberOfColumns = c_numberOfFeaturedProductColumns,
                                        Layout = c_featuredProductsLayout
                                    };

                                    // we use first for topmost
                                    int maxNumberOfPanels = featuredProductsList.Count - 1;
                                    if ( maxNumberOfPanels > c_maxFeaturedProducts )
                                    {
                                        maxNumberOfPanels = c_maxFeaturedProducts;
                                    }

                                    gridLayoutUserCtrl.CreateGridLayout( c_maxFeaturedProducts );

                                    // For each FeaturedProducts, create an Article and use that to populate
                                    // an ArticleUserCtrl. The ArticleUserCtrl is then added to the
                                    // PART_FeaturedProductsGrid using the idx for a panel number.
                                    bool continueAdding = true;

                                    CobraBayView  cobraBayView = launcherWindow.GetCobraBayView();
                                    Debug.Assert( cobraBayView != null );
                                    string productServer = null;
                                    if ( cobraBayView != null )
                                    {
                                        productServer = cobraBayView.GetProductServer();
                                    }

                                    for ( int idx = 1; 
                                          idx < featuredProductsList.Count  && 
                                          continueAdding                    &&
                                          idx < c_maxFeaturedProducts; 
                                          idx++ )
                                    {
                                        // not every product from the store has correct URLs for all three images, so there's some fallback 
                                        // the base image should be manually set by the monetisation team each week to something that's got the 16x9 aspect ratio
                                        // if it's not set, attempt to cycle through the gallery images, if present
                                        // if not present, show something instead of nothing using the thumbnail then small images. although those are square so are a last resort as it makes them huge in the launcher's UI
                                        string fullImageUri = c_imageUri;
                                        
                                        string productLinkUri = productServer;
                                        productLinkUri += featuredProductsList[idx].UrlKey;
                                        // decorating link if needed
                                        string additionalParams = "";
                                        if (m_utmUriParams != "") {
                                            additionalParams = "?" + m_utmUriParams;
                                        }
                                        productLinkUri += additionalParams;

                                        fullImageUri += featuredProductsList[idx].ImageUri;

                                        FeaturedProduct fp = new FeaturedProduct( fullImageUri,
                                                                                  featuredProductsList[idx].CurrentPrice.ToString(),
                                                                                  featuredProductsList[idx].Title,
                                                                                  null,
                                                                                  productLinkUri);

                                        if (featuredProductsList[idx].Gallery != null)
                                        {
                                            for (int j = 0; j < featuredProductsList[idx].Gallery.Count; ++j)
                                            {
                                                string thisFallBackUrl = c_imageUri + featuredProductsList[idx].Gallery[j].ImageUri;
                                                fp.AddFallbackImage(thisFallBackUrl);
                                            }
                                        } 
                                        
                                        string thumbnailImageUri = c_imageUri + featuredProductsList[idx].ThumbnailUri;
                                        fp.AddFallbackImage(thumbnailImageUri);
                                        string smallImageUri = c_imageUri + featuredProductsList[idx].SmallImageUri;
                                        fp.AddFallbackImage(smallImageUri);

                                        Debug.Assert( m_frontPage != null );
                                        if ( m_frontPage != null )
                                        {
                                            FeaturedProductUserCtrl fpUserCtrl = new FeaturedProductUserCtrl( fp, launcherWindow );
                                            fpUserCtrl.Cursor = Cursors.Hand;
                                            fpUserCtrl.MouseDown += OnFeaturedProductClick;
                                            continueAdding = gridLayoutUserCtrl.AddUIElement( idx + 1, fpUserCtrl );
                                        }
                                    }
                                    PART_FeaturedProductsPresentation.DisplayArticles( LocalResources.Properties.Resources.TITLE_FeaturedProducts, gridLayoutUserCtrl );
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// User clicks on a Featured Product 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFeaturedProductClick( object sender, MouseButtonEventArgs e )
        {
            FeaturedProductUserCtrl featuredProductUserCtrl = sender as FeaturedProductUserCtrl;
            Debug.Assert( featuredProductUserCtrl != null );

            if ( featuredProductUserCtrl != null )
            {
                FeaturedProduct featuredProduct = featuredProductUserCtrl.TheFeaturedProduct;
                Debug.Assert( featuredProduct != null );

                if ( featuredProduct != null )
                {
                    if ( !string.IsNullOrWhiteSpace( featuredProduct.Link ) )
                    {
                        try
                        {
                            Dictionary<string, string> dataToLog = new Dictionary<string, string>();
                            dataToLog.Add("featured_product_grid_item_title", featuredProduct.Title);
                            dataToLog.Add("featured_product_grid_item_link", featuredProduct.Link);
                            LogEvent("OnFeaturedProductClick", dataToLog);

                            Process.Start( featuredProduct.Link );
                        }
                        catch( Exception ex )
                        {
                            Log( "Featured Product Link", featuredProduct.Link, ex.ToString() );
                        }
                    }
                    else
                    {
                        Log( "Featured Product Link", featuredProduct.Title, "No Web Link" );
                    }
                }
            }

            e.Handled = true;
        }

        /// <summary>
        /// Updates the Product Update Information
        /// </summary>
        private async Task UpdateProductUpdateInformationAsync()
        {
            Debug.Assert( m_frontPage != null );

            if ( m_frontPage != null )
            {
                LauncherWindow launcherWindow = m_frontPage.GetLauncherWindow();
                Debug.Assert( launcherWindow != null );
                CobraBayView cobraBayView = launcherWindow.GetCobraBayView();
                Debug.Assert( cobraBayView != null );

                if ( m_dynamicContentModel != null &&
                     launcherWindow != null &
                     cobraBayView != null )
                {
                    List<ProductUpdateInformation> productUpdateInformationList = null;

                    await Task.Run( () =>
                    {
                        productUpdateInformationList = m_dynamicContentModel.GetProductUpdateInformation();
                    } );

                    if ( productUpdateInformationList != null )
                    {
                        // We should only have one item, more means we have a problem
                        if ( productUpdateInformationList.Count == 1 )
                        {
                            ProductUpdateInformation productUpdateDetails = productUpdateInformationList[0];

                            Information information = new Information( m_frontPage, launcherWindow )
                            {
                                Title = HTMLStringUtils.StripHTMLFromString( productUpdateDetails.GameName ),
                                SubTitle = HTMLStringUtils.StripHTMLFromString( productUpdateDetails.Title ),
                                Description = productUpdateDetails.PatchNotes,
                                HTTPLink = productUpdateDetails.GetHttpLink(),
                                HTTPLinkText = LocalResources.Properties.Resources.MSG_ClickForPathNotes
                            };
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Orders the Bulletin by the date which should be displayed to the user.
        /// Latest date first
        /// </summary>
        /// <param name="_bulletinList">The Bulletin list to order</param>
        /// <returns>An ordered (latest date first) Bulletin list</returns>
        private List<Bulletin> OrderBulletinListByDate( List<Bulletin> _bulletinList )
        {
            List<Bulletin> resultList = null;

            if ( _bulletinList != null )
            {
                resultList = _bulletinList.OrderBy( e => e.DateAsDateTime() ).ToList<Bulletin>();
                resultList.Reverse();
            }

            return resultList;
        }

        private async Task UpdateHeroImageToFeaturedProductAsync()
        {
            PART_HeroImageUserCtrl.HideDefaultViewAgeRating();
            PART_HeroImageUserCtrl.HideFeaturedProductBlocks(OnFeaturedProductLinkClick);

            if (m_frontPage != null)
            {
                LauncherWindow launcherWindow = m_frontPage.GetLauncherWindow();
                Debug.Assert(launcherWindow != null);
                
                CobraBayView cobraBayView = launcherWindow.GetCobraBayView();
                Debug.Assert(cobraBayView != null);
                
                string productServer = null;

                if (cobraBayView != null)
                {
                    productServer = cobraBayView.GetProductServer();
                }

                PART_HeroImageUserCtrl.Height = 350;

                string productLinkUri = productServer;

                PART_HeroImageUserCtrl.SetImageUri(m_project.HeroImageURI, new List<string>());

                Featured featuredProducts = null;

                try
                {
                    await Task.Run(() => {
                        featuredProducts = m_dynamicContentModel.GetFeaturedProducts();
                    });
                }
                catch (Exception ex)
                {
                    Log("UpdateHeroImageToFeaturedProduct", "Loading Product on main view", ex.ToString());
                }

                // TODO pick first product
                if (featuredProducts != null)
                {
                    FeaturedContainer featuredContainer = featuredProducts.TheFeaturedContainer;
                    if (featuredContainer != null)
                    {
                        List<FeaturedProducts> featuredProductsList = featuredContainer.FeaturedProductsList;
                        if (featuredProductsList != null)
                        {
                            productLinkUri += featuredProductsList[0].UrlKey;

                            // decorating link if needed
                            string additionalParams = "";
                            if (m_utmUriParams != "")
                            {
                                additionalParams = "?" + m_utmUriParams;
                            }
                            productLinkUri += additionalParams;

                            string thisFallBackUrl = c_imageUri + featuredProductsList[0].Gallery[0].ImageUri;
                            

                            // Create a listing of fall back images
                            List<string> fallbackImages = new List<string>();

                            if (featuredProductsList[0].Gallery != null)
                            {
                                for (int j = 1; j < featuredProductsList[0].Gallery.Count; ++j)
                                {
                                    string uri = c_imageUri + featuredProductsList[0].Gallery[j].ImageUri;
                                    fallbackImages.Add(uri);
                                }
                            }

                            string thumbnailImageUri = c_imageUri + featuredProductsList[0].ThumbnailUri;
                            fallbackImages.Add(thumbnailImageUri);
                            string smallImageUri = c_imageUri + featuredProductsList[0].SmallImageUri;
                            fallbackImages.Add(smallImageUri);

                            PART_HeroImageUserCtrl.SetImageUri(thisFallBackUrl, fallbackImages);

                            this.featuredProductSkuName = featuredProductsList[0].ItemsList[0].Sku;
                            this.featuredProductLink = productLinkUri;
                            string buynow = LocalResources.Properties.Resources.MSG_BuyNow.ToUpper();
                            if(featuredProductsList[0].Sku.Contains("ELITE_V_FEATURE"))
                            {
                                buynow = "SEE IN GAME";
                            }

                            PART_HeroImageUserCtrl.SetFeaturedProductDetails(
                                featuredProductsList[0].Title,
                                featuredProductsList[0].CurrentPrice,
                                featuredProductsList[0].Category,
                                buynow
                           );

                            PART_HeroImageUserCtrl.SetFeatureProductLinkClickEventHandler(OnFeaturedProductLinkClick);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// User clicks on the featured product link, navigates to a browser page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnFeaturedProductLinkClick(object sender, MouseButtonEventArgs e)
        {
            Debug.Assert(sender != null);

            if (this.featuredProductLink != null && sender != null)
            {

                Dictionary<string, string> dataToLog = new Dictionary<string, string>();
                dataToLog.Add("hero_featured_product_id", this.featuredProductSkuName);
                dataToLog.Add("hero_featured_product_link", this.featuredProductLink);
                LogEvent("HeroFeaturedProductLinkClick", dataToLog);

                Process.Start(HTMLStringUtils.StripHTMLFromString(this.featuredProductLink));
            }
        }


        /// <summary>
        /// Updates the hero image
        /// </summary>
        private async Task UpdateHeroImageAsync()
        {
            PART_HeroImageUserCtrl.HideFeaturedProductBlocks(OnFeaturedProductLinkClick);

            string heroUriString = c_defaultHeroImage;

            // Get the image from the project, failing that then use a default image
            if ( m_project != null )
            {
                string projectHeroImageURI = m_project.HeroImageURI;
                if ( !string.IsNullOrWhiteSpace( projectHeroImageURI ) )
                {
                    heroUriString = projectHeroImageURI;
                }
            }

            PART_HeroImageUserCtrl.SetImageUri( heroUriString, new List<string>() );
            PART_HeroImageUserCtrl.Height = 680;

            if ( m_frontPage != null )
            {
                LauncherWindow launcherWindow = m_frontPage.GetLauncherWindow();
                Debug.Assert( launcherWindow != null );
                if ( launcherWindow != null )
                {
                    CobraBayView cobraBayView = launcherWindow.GetCobraBayView();
                    Debug.Assert( cobraBayView != null );
                    if ( cobraBayView != null )
                    { 
                        PART_HeroImageUserCtrl.SetAgeRating( cobraBayView.LanguageOverride, 
                                                             m_project.ESRBRating, 
                                                             m_project.PEGIRating );
                    }
                }
            }

            PART_HeroImageUserCtrl.ShowDefaultViewAgeRating();
        }

        /// <summary>
        /// When scroll viewer triggers, we check if we want to check visibility and then send data to FORC
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            double visibilityThreshold = 0.1;
            // we need timeout to avoid spamming FORC and give useless information
            if (m_scrollEventTimestamp + m_scrollEventTimeoutSecs < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                // reset timeout to shorter time after initial run
                if(m_scrollEventTimeoutSecs > m_scrollEventTimeoutSecsNormal)
                {
                    m_scrollEventTimeoutSecs = m_scrollEventTimeoutSecsNormal;
                }
                
                Dictionary<string, string> sectionsVisible = new Dictionary<string, string>();

                // hero image
                var howMuchHeroImageVisible = HowMuchVisible(PART_HeroImageUserCtrl);
                
                if (howMuchHeroImageVisible > visibilityThreshold)
                {
                    sectionsVisible.Add("hero_image", howMuchHeroImageVisible.ToString());
                }

                // galnet
                var howMuchGalnetVisible = HowMuchVisible(PART_GalnetNewsPresentation);
                
                if (howMuchGalnetVisible > visibilityThreshold)
                {
                    sectionsVisible.Add("galnet", howMuchGalnetVisible.ToString());
                }

                // community goals
                var howMuchCommunityGoalsVisible = HowMuchVisible(PART_CommunityGoalsPresentation);
                
                if (howMuchCommunityGoalsVisible > visibilityThreshold)
                {
                    sectionsVisible.Add("community_goals", howMuchCommunityGoalsVisible.ToString());
                }

                // featured products
                var howMuchFeaturedProductsVisible = HowMuchVisible(PART_FeaturedProductsPresentation);
                
                if (howMuchFeaturedProductsVisible > 10)
                {
                    sectionsVisible.Add("featured_products", howMuchFeaturedProductsVisible.ToString());
                }

                // send event
                LogEvent("ProductScrollEventVisible", sectionsVisible);

                // update timestamp
                m_scrollEventTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }

        /// <summary>
        /// Returns of how much of dynamic content control subsection are visible
        /// </summary>
        /// <param name="section"></param>
        /// <returns>double</returns>
        private double HowMuchVisible(UserControl section)
        {
            double sectionHeight = section.ActualHeight;
            double sectionVisibleHeight = section.ActualHeight;
            var positionTransform = section.TransformToAncestor(this);
            var position = positionTransform.Transform(new Point(0, 0));
            // 
            double whereWeAre = position.Y;
            // if higher than zero, subtract from height
            // this gives you have much of section is visible
            if (whereWeAre > 0)
            {
                // if positive, we are missing whole
                sectionVisibleHeight -= whereWeAre;
            }
            else {
                // if negative, we actually subtract
                sectionVisibleHeight += whereWeAre;
            }

            return sectionVisibleHeight / sectionHeight;
        }

        /// <summary>
        /// LogEvent to FORC telemetry
        /// </summary>
        /// <param name="eventToLog"></param>
        /// <param name="dataToLog"></param>
        /// <returns>bool</returns>
        private bool LogEvent(string eventToLog, Dictionary<string, string> dataToLog)
        {
            if (m_frontPage != null)
            {
                LauncherWindow launcherWindow = m_frontPage.GetLauncherWindow();
                Debug.Assert(launcherWindow != null);
                if (launcherWindow != null)
                {
                    CobraBayView cobraBayView = launcherWindow.GetCobraBayView();
                    Debug.Assert(cobraBayView != null);
                    if (cobraBayView != null)
                    {
                        FORCManager fORCManager = cobraBayView.Manager();
                        if (fORCManager != null)
                        {
                            FORCServerConnection fORCServerConnection = (FORCServerConnection)fORCManager.ServerConnection;
                            LogEntry sg = new LogEntry(eventToLog);
                            // add project name
                            sg.AddValue("product", cobraBayView.GetActiveProject().Name);
                            // pass data from event triggered
                            foreach (KeyValuePair<string, string> entry in dataToLog)
                            {
                                sg.AddValue(entry.Key, entry.Value);
                            }

                            fORCServerConnection.LogValues(fORCManager.UserDetails, sg);
                        }
                    }
                }
            }
            return true;
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

            if ( m_frontPage != null )
            {
                LauncherWindow launcherWindow = m_frontPage.GetLauncherWindow();
                Debug.Assert( launcherWindow != null );
                if ( launcherWindow != null )
                {
                    launcherWindow.LogEvent( _action, _key, _description );
                }
            }
        }

        /// <summary>
        /// Our FrontPage
        /// </summary>
        private FrontPage m_frontPage = null;

        /// <summary>
        /// Access to the Dynamic Content Model
        /// </summary>
        private DynamicContentModel m_dynamicContentModel = null;

        /// <summary>
        /// The current active project
        /// </summary>
        Project m_project = null;

        /// <summary>
        /// The number of columns in the GameDescription area
        private const int c_numberOfGameDescriptionColumns = 1;

        /// <summary>
        /// The gridlayout for the GameDescription area
        /// </summary>
        private const GridLayoutUserCtrl.LayoutOptions c_gameDescriptionLayout = GridLayoutUserCtrl.LayoutOptions.UseWholeTopRow;

        /// <summary>
        /// The number of columns in the Galnet News area
        /// </summary>
        private const int c_numberOfGalnetColumns = 2;

        /// <summary>
        /// The gridlayout for the Galnet News area
        /// </summary>
        private const GridLayoutUserCtrl.LayoutOptions c_galnetLayout = GridLayoutUserCtrl.LayoutOptions.UseWholeTopRow;

        /// <summary>
        /// The number of columns in the Community Goals area
        /// </summary>
        private const int c_numberOfCommunityGoalsColumns = 2;

        /// <summary>
        /// The gridlayout for the Community Goals area
        /// </summary>
        private const GridLayoutUserCtrl.LayoutOptions c_communityGoalsLayout = GridLayoutUserCtrl.LayoutOptions.UseAllOfGrid;

        /// <summary>
        /// The number of columns in the Community News area
        /// </summary>
        private const int c_numberOfCommunityNewsColumns = 2;

        /// <summary>
        /// The gridlayout for the Community News area
        /// </summary>
        private const GridLayoutUserCtrl.LayoutOptions c_communityNewsLayout = GridLayoutUserCtrl.LayoutOptions.UseAllOfGrid;

        /// <summary>
        /// The number of columns in the Featured Products area
        /// </summary>
        private const int c_numberOfFeaturedProductColumns = 2;

        /// <summary>
        /// Define a max number of featured products to display
        /// </summary>
        private const int c_maxFeaturedProducts = 6;

        /// <summary>
        /// The gridlayout for the Featured Products area
        /// </summary>
        private const GridLayoutUserCtrl.LayoutOptions c_featuredProductsLayout = GridLayoutUserCtrl.LayoutOptions.UseWholeTopAndBottomRow;

        /// <summary>
        /// The default hero image, in case we can't get one from the server.
        /// </summary>
        private const string c_defaultHeroImage = "pack://application:,,,/Images/DefaultBG.png";

        /// <summary>
        /// URL to the product images
        /// </summary>
        private const string c_imageUri = "https://store-staging.frontierstore.net/";

        /// <summary>
        /// A Task list that is waited on to complete  UI updates
        /// </summary>
        private List<Task> m_waitUIUpdateTaskList = new List<Task>();

        /// <summary>
        /// stores last time we registered event for scroll event
        /// </summary>
        private long m_scrollEventTimestamp;

        /// <summary>
        /// timeout between scroll events we will send to FORC
        /// </summary>
        private int m_scrollEventTimeoutSecsNormal = 2;

        /// <summary>
        /// Actual timeout stored for scroll events, initially set on longer timeout for startup
        /// </summary>
        private int m_scrollEventTimeoutSecs = 6;

        /// <summary>
        /// UTM campaign params for store link decoration
        /// </summary>
        private string m_utmUriParams = "utm_source=launcher_dlc_link&utm_medium=application&utm_campaign=elite_gamestore";
    }
}
