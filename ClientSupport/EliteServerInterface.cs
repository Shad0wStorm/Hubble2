//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! EliteServerInterface, provides Elite API interface.
//
//! Author:     Alan MacAree
//! Created:    10 Oct 2022
//----------------------------------------------------------------------

namespace ClientSupport
{
    /// <summary>
    /// provides an interface for the Elite API
    /// </summary>
    public abstract class EliteServerInterface
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public EliteServerInterface()
        {
        }

        /// <summary>
        /// Sets the language that information should be returned in.
        /// </summary>
        /// <param name="_languageCode"></param>
        public abstract void SetLanguage( string _languageCode );

        /// <summary>
        /// Gets the Galnet news as a Json string
        /// </summary>
        /// <param name="_project">The project to get the news for</param>
        /// <returns>The galnet news as a json string.</returns>
        public abstract string GetGalnetNews( Project _project );

        /// <summary>
        /// Gets the community goals as a Json string
        /// </summary>
        /// <param name="_project">The project to get the news for</param>
        /// <returns>The community goals as a json string.</returns>
        public abstract string GetCommunityGoals( Project _project );

        /// <summary>
        /// Gets the community news as a Json string
        /// </summary>
        /// <param name="_project">The project to get the news for</param>
        /// <returns>The community news as a json string.</returns>
        public abstract string GetCommunityNews( Project _project );

        /// <summary>
        /// Gets EliteAPI's filtered list of livery items as a Json string
        /// </summary>
        /// <param name="_project">The project to get the in-game livery items for</param>
        /// <returns>The livery items as a json string.</returns>
        public abstract string GetFeaturedProducts(Project _project);
    }

}
