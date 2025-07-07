//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! ProductUpdateInformation, represents Product Update Information. 
//              This set of classes is used to by JsonSerializer to 
//              Deserialize JSON strings. Hence these classes match 
//              the data returned from the CMS API.
//
//! Author:     Alan MacAree
//! Created:    01 Nov 2022
//----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace JSONConverters
{
    /// summary>
    /// Product Update Information Details
    /// </summary>
    public class ProductUpdateInformation
    {
        /// <summary>
        /// The title
        /// </summary>
        [JsonPropertyName( "title" )]
        public string Title { get; set; }

        /// <summary>
        /// The body
        /// </summary>
        [JsonPropertyName( "body" )]
        public string Body { get; set; }

        /// <summary>
        /// Patch Notes
        /// </summary>
        [JsonPropertyName( "patch_notes" )]
        public string PatchNotes { get; set; }

        /// <summary>
        /// Version
        /// </summary>
        [JsonPropertyName( "version" )]
        public string Version { get; set; }

        /// <summary>
        /// Image Uri (full)
        /// </summary>
        [JsonPropertyName( "image" )]
        public string Image { get; set; }

        /// <summary>
        /// Game ID
        /// </summary>
        [JsonPropertyName( "cms_game_id" )]
        public int GameId { get; set; }

        /// <summary>
        /// Game Name
        /// </summary>
        [JsonPropertyName( "game_name" )]
        public string GameName { get; set; }

        /// <summary>
        /// Returns a list of all of the images in the order they should
        /// be placed on a UI, with the backmost image first.
        /// </summary>
        /// <returns>A List of images, this can be null</returns>
        public List<string> CompoundedImageList()
        {
            List<string> listResult = null;

            if ( Image != null )
            {
                string[] arrayOfImages = Image.Split( c_imageSeparator );
                listResult = arrayOfImages.ToList();
            }

            return listResult;
        }

        /// <summary>
        /// Returns the HHTP link for the product update information
        /// </summary>
        /// <returns></returns>
        public string GetHttpLink()
        {
            string httpLink = c_eliteDangerous;
            httpLink += c_forwardSlash;
            httpLink += LocalResources.Properties.Resources.UpdateNotesLinkLanguage;
            httpLink += c_forwardSlash;
            httpLink += LocalResources.Properties.Resources.UpdateNotesLink;
            httpLink += c_forwardSlash;
            httpLink += GetVersionInLinkFormat();

            return httpLink;
        }

        /// <summary>
        /// Returns the version in a link format, this is because the
        /// version is normally in nn.nn.nn.nn format, and we need it in
        /// nn-nn-nn-nn format (to use within a link)
        /// </summary>
        /// <returns>The version with dashes in place of dots</returns>
        private string GetVersionInLinkFormat()
        {
            return Version.Replace( c_versionDot, c_versionDash );
        }

        /// <summary>
        /// The separator used between images
        /// </summary>
        private const char c_imageSeparator = ',';

        /// <summary>
        /// The main EliteDangerous web address
        /// </summary>
        private const string c_eliteDangerous = "https://www.elitedangerous.com";

        /// <summary>
        /// A forward slash used in web address
        /// </summary>
        private const char c_forwardSlash = '/';

        /// <summary>
        /// Defines a dot that is used within the standard version format
        /// </summary>
        private const char c_versionDot = '.';

        /// <summary>
        /// Defines a dash used within version text used within URLs
        /// </summary>
        private const char c_versionDash = '-';
    }

}
