//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! Article Class, holds article related information, intended to be 
//! used with the ArticleUserCtrl.
//
//! Author:     Alan MacAree
//! Created:    05 Oct 2022
//----------------------------------------------------------------------

using System.Collections.Generic;

namespace FDUserControls
{
    /// <summary>
    /// Article is a data holder class, it is intended
    /// to hold Article information and can be used with
    /// ArticleUserCtrl.
    /// </summary>
    public class Article
    {
        /// <summary>
        /// Unwanted text that can often be found within Ariticle
        /// text. This is removed via the RemoveUnwantedStrings method
        /// </summary>
        private const string c_RemoveThis1 = "{{top 5}}";
        private const string c_RemoveThis2 = "{{top5}}";

        /// <summary>
        /// The system this Article relates to.
        /// </summary>
        public string System { get; set; } = null;

        /// <summary>
        /// Holds the title for the Article
        /// </summary>
        public string Title { get; set; } = null;

        /// <summary>
        /// Holds the Article Date as a string
        /// </summary>
        public string DateAsString { get; set; } = null;

        /// <summary>
        /// Holds the Article's short text, a short
        /// version of the Article's text.
        /// </summary>
        public string ShortText { get; set; } = null;

        /// <summary>
        /// Hodls the Article's full text.
        /// </summary>
        public string FullText { get; set; } = null;

        /// <summary>
        /// The Url to get the image from (excludes image name)
        /// </summary>
        public string ImageURL { get; set; } = null;

        /// <summary>
        /// The image extension (e.g. .png)
        /// </summary>
        public string ImageExtension { get; set; } = null;

        /// <summary>
        /// Holds an overlayed image list (a single image that is
        /// made up of multiple overlayed images.
        /// </summary>
        public List<string> OverlayList { get; set; } = null;

        /// <summary>
        /// Removes unwanted strings from all of the text within the Article
        /// </summary>
        public void RemoveUnwantedStringsFromText()
        {
            if ( !string.IsNullOrWhiteSpace( ShortText ) )
            {
                ShortText = RemoveUnwantedStrings( ShortText );
            }
            if ( !string.IsNullOrWhiteSpace( FullText ) )
            {
                FullText = RemoveUnwantedStrings( FullText );
            }
        }

        /// <summary>
        /// Removes unwanted strings from the past string and returns the result.
        /// </summary>
        /// <param name="removeFromHere"></param>
        /// <returns></returns>
        private string RemoveUnwantedStrings( string removeFromHere )
        {
            string resultingString = removeFromHere;

            if ( !string.IsNullOrWhiteSpace( removeFromHere ) )
            {
                resultingString = resultingString.Replace( c_RemoveThis1, "" );
                resultingString = resultingString.Replace( c_RemoveThis2, "" );
            }

            return resultingString;
        }
    }
}
