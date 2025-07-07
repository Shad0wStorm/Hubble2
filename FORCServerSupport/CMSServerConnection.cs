//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! CMSServerConnection, provides access to the CMS API server.
//
//! Author:     Alan MacAree
//! Created:    01 Nov 2022
//----------------------------------------------------------------------

using ClientSupport;
using FORCServerSupport.CMS_Queries;
using System.Diagnostics;

namespace FORCServerSupport
{
    /// <summary>
    /// Provides an implementation for CMSServerInterface
    /// </summary>
    public class CMSServerConnection : CMSServerInterface
    { 
        /// <summary>
        /// Sets the language that information should be returned in.
        /// </summary>
        /// <param name="_languageCode"></param>
        public override string GetProductUpdateInfo( Project _project )
        {
            Debug.Assert( _project != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( m_languageCountryCode ) );

            string result = null;

            if ( _project != null )
            {
                if ( !string.IsNullOrWhiteSpace( m_languageCountryCode ) )
                {
                    ProductUpdateInfoQuery query = new ProductUpdateInfoQuery();
                    result = query.Run( _project, m_languageCountryCode );
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the Product Update information as a Json string
        /// </summary>
        /// <param name="_languageCode">The language to get the product information info for</param>
        /// <returns>The Product Update information as a json string.</returns>
        public void SetLanguageAndCountryCode( string _languageCountryCode )
        {
            m_languageCountryCode = _languageCountryCode;
        }

        /// <summary>
        /// Not used.
        /// </summary>
        /// <param name="_languageCode"></param>
        public override void SetLanguage( string _languageCode )
        {
            // This is an unused method and should not be called.
            Debug.Assert( false );
        }

        /// <summary>
        /// The current lamguage code
        /// </summary>
        private string m_languageCountryCode = null;
    }
}
