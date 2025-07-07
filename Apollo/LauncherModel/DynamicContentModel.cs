//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! DynamicContentModel 
//
//! Author:     Alan MacAree
//! Created:    01 Nov 2022
//----------------------------------------------------------------------

// #define LIVERY_COMES_FROM_FORC // define to use livery directly from FORCAPI, false to use EliteAPI's version of the list that's tailored specifically for the launcher

using CBViewModel;
using ClientSupport;
using JSONConverters;
using System.Collections.Generic;
using System.Diagnostics;

namespace LauncherModel
{
    /// <summary>
    /// Provides a model for the launchers dynamic content.
    /// </summary>
    public class DynamicContentModel
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_cobraBayView">The CobraBayView where data is retrieved from</param>
        public DynamicContentModel( CobraBayView _cobraBayView )
        {
            Debug.Assert( _cobraBayView != null );
            m_cobraBayView = _cobraBayView;
        }

        /// <summary>
        /// Gets the game description for the active project
        /// </summary>
        /// <param name="_project">The project to get the information for</param>
        /// <returns>The GameDescription, this can be null</returns>
        public GameDescription GetGameDescription()
        {
            GameDescription gameDescription = null;

            Debug.Assert( m_cobraBayView != null );

            if ( m_cobraBayView != null )
            {
                FORCManager fORCManager = m_cobraBayView.Manager();
                Debug.Assert( fORCManager != null );

                if ( fORCManager != null )
                {
                    Project project = m_cobraBayView.GetActiveProject();
                    if ( project != null )
                    {
                        ServerInterface serverInterface = fORCManager.ServerConnection;
                        Debug.Assert( serverInterface != null );

                        if ( serverInterface != null )
                        {
                            string jsonString = serverInterface.GetGameDescription( project );
                            if ( !string.IsNullOrWhiteSpace( jsonString ) )
                            {
                                gameDescription = JsonConverter.JsonToGameDescription( jsonString, m_cobraBayView );
                            }
                        }
                    }
                }
            }

            return gameDescription;
        }

        /// <summary>
        /// Gets the latest Galnet news via the Elite API and returns it.
        /// </summary>
        /// <returns>GalnetNews or null</returns>
        public GalnetNews GetLatestGalnetNews()
        {
            GalnetNews galnetNews = null;

            Debug.Assert( m_cobraBayView != null );

            if ( m_cobraBayView != null )
            {
                EliteServerInterface eliteServerInterface = m_cobraBayView.GetEliteServerInterface();
                Debug.Assert( eliteServerInterface != null );
                if ( eliteServerInterface != null )
                {
                    Project project = m_cobraBayView.GetActiveProject();
                    if ( project != null )
                    {
                        string jsonString = eliteServerInterface.GetGalnetNews( project );
                        if ( !string.IsNullOrWhiteSpace( jsonString ) )
                        {
                            galnetNews = JsonConverter.JsonToGalnetNews( jsonString, m_cobraBayView );
                        }
                    }
                }
            }

            return galnetNews;
        }

        /// <summary>
        /// Gets the latest Community Goals via the Elite API and returns it.
        /// </summary>
        /// <returns>CommunityGoals or null</returns>
        public CommunityGoals GetLatestCommunityGoals()
        {
            Debug.Assert( m_cobraBayView != null );

            CommunityGoals communityGoals = null;

            if ( m_cobraBayView != null )
            {
                EliteServerInterface eliteServerInterface = m_cobraBayView.GetEliteServerInterface();
                Debug.Assert( eliteServerInterface != null );
                if ( eliteServerInterface != null )
                {
                    Project project = m_cobraBayView.GetActiveProject();
                    if ( project != null )
                    {
                        string jsonString = eliteServerInterface.GetCommunityGoals( project );
                        if ( !string.IsNullOrWhiteSpace( jsonString ) )
                        {
                            communityGoals = JsonConverter.JsonToCommunityGoals( jsonString, m_cobraBayView );
                        }
                    }
                }
            }

            return communityGoals;
        }

        /// <summary>
        /// Gets the latest Community News via the Elite API and returns it.
        /// </summary>
        /// <returns>CommunityNews or null</returns>
        public CommunityNews GetLatestCommunityNews()
        {
            CommunityNews communityNews = null;
            Debug.Assert( m_cobraBayView != null );

            if ( m_cobraBayView != null )
            {
                EliteServerInterface eliteServerInterface = m_cobraBayView.GetEliteServerInterface();
                Debug.Assert( eliteServerInterface != null );
                if ( eliteServerInterface != null )
                {
                    Project project = m_cobraBayView.GetActiveProject();
                    if ( project != null )
                    {
                        string jsonString = eliteServerInterface.GetCommunityNews( project );
                        if ( !string.IsNullOrWhiteSpace( jsonString ) )
                        {
                            communityNews = JsonConverter.JsonToCommunityNews( jsonString, m_cobraBayView );
                        }
                    }
                }

            }
            return communityNews;
        }

        /// <summary>
        /// Gets the current featured products via the Elite API
        /// </summary>
        /// <returns>The Featured and sub classes that represent the featured products</returns>
        public Featured GetFeaturedProducts()
        {
            Featured featured = null;

            Debug.Assert( m_cobraBayView != null );

            if ( m_cobraBayView != null )
            {
#if LIVERY_COMES_FROM_FORC
                FORCManager forcManager = m_cobraBayView.Manager();
                Debug.Assert(forcManager != null);
                if (forcManager != null)
                {
                    Debug.Assert(forcManager.ServerConnection != null);
                    if (forcManager.ServerConnection != null)
                    {
                        Project project = m_cobraBayView.GetActiveProject();
                        if (project != null)
                        {
                            string jsonString = forcManager.ServerConnection.GetFeaturedProducts(project);
                            if (!string.IsNullOrWhiteSpace(jsonString))
                            {
                                featured = JsonConverter.JsonToFeaturedProducts(jsonString, m_cobraBayView);
                            }
                        }
                    }
                }
#else
                EliteServerInterface eliteServerInterface = m_cobraBayView.GetEliteServerInterface();
                Debug.Assert(eliteServerInterface != null);
                if (eliteServerInterface != null)
                {
                    Project project = m_cobraBayView.GetActiveProject();
                    if (project != null)
                    {
                        string jsonString = eliteServerInterface.GetFeaturedProducts(project);
                        if (!string.IsNullOrWhiteSpace(jsonString))
                        {
                            featured = JsonConverter.JsonToFeaturedProducts(jsonString, m_cobraBayView);
                        }
                    }
                }
#endif
            }

            return featured;
        }

        /// <summary>
        /// Gets the Production Update Information via the CMS API
        /// </summary>
        /// <returns>The Production Update Information for the current selected Product</returns>
        public List<ProductUpdateInformation> GetProductUpdateInformation()
        {
            List<ProductUpdateInformation> productUpdateInformationList = null;

            Debug.Assert( m_cobraBayView != null );

            if ( m_cobraBayView != null )
            {
                CMSServerInterface cmsServerInterface  = m_cobraBayView.GetCMSServerInterface();
                Debug.Assert( cmsServerInterface != null );
                if ( cmsServerInterface != null )
                {
                    Project project = m_cobraBayView.GetActiveProject();
                    if ( project != null )
                    {
                        string jsonString = cmsServerInterface.GetProductUpdateInfo( project );
                        if ( !string.IsNullOrWhiteSpace( jsonString ) )
                        {
                            productUpdateInformationList = JsonConverter.JsonToProductUpdateInformation( jsonString, m_cobraBayView );
                        }
                    }
                }
            }

            return productUpdateInformationList;
        }

        /// <summary>
        /// CobraBayView, used to gain access to the underlying server communication classes
        /// </summary>
        private CobraBayView m_cobraBayView;
    }
}
