//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! EliteServerConnection, provides access to the Elite API server.
//
//! Author:     Alan MacAree
//! Created:    28 Sept 2022
//----------------------------------------------------------------------

using ClientSupport;
using FORCServerSupport.Elite_Queries;
using System.Diagnostics;

namespace FORCServerSupport
{
    /// <summary>
    /// Provides access to the Elite API server.
    /// </summary>
    public class EliteServerConnection : EliteServerInterface
    {
        /// <summary>
        /// Default constructure
        /// </summary>
        public EliteServerConnection()
        {
        }

        /// <summary>
        /// Sets the language for this connection
        /// </summary>
        /// <param name="_languageCode">The language code to use.</param>
        public override void SetLanguage( string _languageCode )
        {
            m_languageCode = _languageCode;
        }

        /// <summary>
        /// Gets the Galnet news
        /// </summary>
        /// <param name="_project">The project to get the Galnet news for.</param>
        public override string GetGalnetNews( Project _project )
        {
            Debug.Assert( _project != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( m_languageCode ) );

            string result = null;

            if ( _project != null )
            {
                if ( !string.IsNullOrWhiteSpace( m_languageCode ) )
                {
                    GalnetNewsQuery query = new GalnetNewsQuery();
                    result = query.Run( _project, m_languageCode );
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the community goals as a Json string
        /// </summary>
        /// <param name="_project">The project to get the news for</param>
        /// <returns>The community goals as a json string, can be null</returns>
        public override string GetCommunityGoals( Project _project )
        {
            Debug.Assert( _project != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( m_languageCode ) );

            string result = null;

            if ( _project != null )
            {
                if ( !string.IsNullOrWhiteSpace( m_languageCode ) )
                {
                    CommunityGoalsQuery query = new CommunityGoalsQuery();
                    result = query.Run( _project, m_languageCode );
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the community news as a Json string
        /// </summary>
        /// <param name="_project">The project to get the news for</param>
        /// <returns>The community news as a json string, can be null</returns>
        public override string GetCommunityNews( Project _project )
        {
            Debug.Assert( _project != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( m_languageCode ) );

            string result = null;

            if ( _project != null )
            {
                if ( !string.IsNullOrWhiteSpace( m_languageCode ) )
                {
                    CommunityNewsQuery query = new CommunityNewsQuery();
                    result = query.Run( _project, m_languageCode );
                }
            }

            return result;
        }

        /// <summary>
        /// Gets EliteAPI's filtered list of livery items as a Json string
        /// </summary>
        /// <param name="_project">The project to get the in-game livery items for</param>
        /// <returns>The livery items as a json string.</returns>
        public override string GetFeaturedProducts(Project _project)
        {
            Debug.Assert(_project != null);
            Debug.Assert(!string.IsNullOrWhiteSpace(m_languageCode));

            string result = null;

            if (_project != null)
            {
                if (!string.IsNullOrWhiteSpace(m_languageCode))
                {
                    FeaturedProductsQuery query = new FeaturedProductsQuery(); 
                    result = query.Run(_project, m_languageCode);
                }
            }

            return result;
        }

        private string m_languageCode = null;
    }
}
