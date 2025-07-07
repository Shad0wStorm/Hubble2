//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! CMSServerInterface, provides access to the CMS API server.
//
//! Author:     Alan MacAree
//! Created:    01 Nov 2022
//----------------------------------------------------------------------

namespace ClientSupport
{
    public abstract class CMSServerInterface
    {
        /// <summary>
        /// Default constructure
        /// </summary>
        public CMSServerInterface()
        {
        }

        /// <summary>
        /// Sets the language that information should be returned in.
        /// </summary>
        /// <param name="_languageCode"></param>
        public abstract void SetLanguage( string _languageCode );

        /// <summary>
        /// Gets the Product Update information as a Json string
        /// </summary>
        /// <param name="_project">The project to get the news for</param>
        /// <returns>The Product Update information as a json string.</returns>
        public abstract string GetProductUpdateInfo( Project _project );

    }
}
