//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! CMSApiUri, builds the URI for the CMS API
//
//! Author:     Alan MacAree
//! Created:    01 Nov 2022
//----------------------------------------------------------------------

using ClientSupport;
using System;
using System.Diagnostics;

namespace FORCServerSupport
{
    /// <summary>
    /// Builds the full URI to accss the CMSApi:
    /// example https://cms.zaonce.net/en-GB/api/launcher/update-notes?game=943
    /// 
    /// This is split into 4 parts
    /// 1st Part of CMSApi           - https://cms.zaonce.net/
    /// Language                     - en-GB
    /// 2nd Part of CMSApi           - /api/launcher/update-notes
    /// Game specific reference      - ?game=943
    /// </summary>
    public class CMSApiUri
    {
        /// <summary>
        /// Returns the 1st part of the CMS Api
        /// </summary>
        /// <returns>The 1st part of the CMS Api</returns>
        static protected string Get1stPartOfCMSApi()
        {
            return c_CMSApi1stPart;
        }

        /// <summary>
        /// Returns the language URI string, this is appened to a URI
        /// </summary>
        /// <returns></returns>
        static protected string GetLanguageUriString( string _languageCode )
        {
            Debug.Assert( !string.IsNullOrWhiteSpace( _languageCode ) );
            string result = c_defaultLanguageCode;

            if ( !string.IsNullOrWhiteSpace( _languageCode ) )
            {
                result = _languageCode;
            }

            return result;
        }

        /// <summary>
        /// Returns the 2nd part of the CMS Api
        /// </summary>
        /// <returns>The 2nd part of the CMS Api</returns>
        static protected string Get2ndPartOfCMSApi()
        {
            return c_CMSApi2ndPart;
        }

        /// <summary>
        /// Returns the Product Part of the CMS API
        /// </summary>
        /// <param name="_project"></param>
        /// <returns></returns>
        static protected string GetProductPartOfCMSApi( Project _project )
        {
            Debug.Assert( _project != null );

            string productPartOfCmsApi = c_CSMApiProductCode;
            if ( _project != null )
            {
                productPartOfCmsApi += _project.GameCode;
            }

            return productPartOfCmsApi;
        }

        /// <summary>
        /// Builds the Elite API Uri from a number of different sources
        /// Including:
        ///     Project
        ///     Elite API Version
        ///     System Uri
        ///     Language
        /// </summary>
        /// <param name="_project">The project to get the Elite Api Url for</param>
        /// <param name="_subSystemUri">The system to get the Elite Api Url for</param>
        /// <param name="_languageCode">The language code to get the Elite Api Url for</param>
        /// <returns>A Uri to the Elite API or null</returns>
        static private Uri GetUri( Project _project, string _languageCode )
        {
            Debug.Assert( _project != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( _languageCode ) );

            string uriString = null;

            Uri uri = null;

            if ( _project != null )   
            {
                // Build the URI up from:

                // The 1st part of the CMSApi
                uriString = Get1stPartOfCMSApi();
                uriString += GetLanguageUriString( _languageCode );
                uriString += Get2ndPartOfCMSApi();
                uriString += GetProductPartOfCMSApi( _project );
                try
                {
                    uri = new Uri( uriString );
                }
                catch ( Exception )
                {
                    uri = null;
                }
            }

            return uri;
        }

        /// <summary>
        /// Returns the full Galnet API URI
        /// </summary>
        /// <param name="_project">The project to return the URI for, must not be null</param>
        /// <param name="_languageCode">The language code o us, must not be null or empty</param>
        /// <returns>The full Galnet API URI, this can be null</returns>
        static public Uri GetProductUpdateUri( Project _project, string _languageCode )
        {
            Debug.Assert( _project != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( _languageCode ) );

            return GetUri( _project, _languageCode );
        }

        /// <summary>
        /// The 1st part of the CMS Api
        /// </summary>
        private const string c_CMSApi1stPart= "https://cms.zaonce.net/";

        /// <summary>
        /// Default language code, this is used in case noe is 
        /// passed to this methodes in this class.
        /// </summary>
        private const string c_defaultLanguageCode = "en-GB";

        /// <summary>
        /// The 2nd part of the CMS Api
        /// </summary>
        private const string c_CMSApi2ndPart = "/api/launcher/update-notes";

        /// <summary>
        /// CMS Game Code part, the code is appended to the end
        /// of this.
        /// </summary>
        private const string c_CSMApiProductCode = "?game=";

    }

}