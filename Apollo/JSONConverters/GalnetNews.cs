//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! GalnetNews, represents Galnet news. This set of classes is used
//              to by JsonSerializer to Deserialize JSON strings. Hence
//              these classes match the data returned from the Elite API.
//
//! Author:     Alan MacAree
//! Created:    29 Sept 2022
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;

namespace JSONConverters
{
    /// <summary>
    /// Top level GalnetNews, this matches the Json structure.
    /// </summary>
    public class GalnetNews
    {
        /// <summary>
        /// The System string
        /// </summary>
        [JsonPropertyName( "system" )]
        public string System { get; set; }

        /// <summary>
        /// The prefix location to get images from
        /// </summary>
        [JsonPropertyName( "image_url_prefix" )]
        public string ImageURL { get; set; }

        /// <summary>
        /// The image extension (e.g. .png, .bmp etc)
        /// </summary>
        [JsonPropertyName( "image_url_suffix" )]
        public string ImageExtension { get; set; }

        /// <summary>
        /// The list of data "Bulletin" objects
        /// </summary>
        [JsonPropertyName( "data" )]
        public List<Bulletin> Bulletins { get; set; }

        /// <summary>
        /// Determines if this object holds valid Galnet news
        /// information.
        /// </summary>
        /// <returns>true if this object holds valid galnet news information</returns>
        public bool IsValid()
        {
            return (System.CompareTo( c_SystemString ) == 0);
        }

        /// <summary>
        /// The System string that specifies that the JSON contains
        /// Galnet news information
        /// </summary>
        private const string c_SystemString = "Galnet";
    }

    /// <summary>
    /// Represents Galnet's JSON data
    /// </summary>
    public class Bulletin
    {
        /// <summary>
        /// The date of the Bulletin, for display to the user
        /// </summary>
        [JsonPropertyName( "date" )]
        public string Date { get; set; }

        /// <summary>
        /// The type of data we hold, this should be
        /// c_bulletin
        /// </summary>
        [JsonPropertyName( "type" )]
        public string Type { get; set; }

        /// <summary>
        /// The uid of this Bulletin
        /// </summary>
        [JsonPropertyName( "uid" )]
        public string Uid { get; set; }

        /// <summary>
        /// The params for the Bulletin, this may be null
        /// </summary>
        [JsonPropertyName("params")]
        public BulletinItem Params { get; set; }

        /// <summary>
        /// The date as a DateTime
        /// </summary>
        /// <returns>The date as a DateTime</returns>
        public DateTime DateAsDateTime()
        {
            DateTime result = DateTime.Now;
            if ( !string.IsNullOrWhiteSpace( Date ) )
            {
                try
                {
                    result = DateTime.Parse( Date );
                }
                catch( Exception )
                {
                    // We can't error out, so fail by not doing anything.
                    Debug.Assert( false );
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Represents a BulletinItem
    /// </summary>
    public class BulletinItem
    {
        /// <summary>
        /// Holds a BulletinItemContents object, this may be null
        /// </summary>
        [JsonPropertyName( "item" )]
        public BulletinItemContents Item { get; set; }
    }

    /// <summary>
    /// Represents the Bulletin Item Content
    /// </summary>
    public class BulletinItemContents
    {
        /// <summary>
        /// The type of content (text)
        /// </summary>
        [JsonPropertyName( "key" )]
        public string Key { get; set; }

        /// <summary>
        /// The title of the Bulletin.
        /// Note that this can contain formatted text.
        /// </summary>
        [JsonPropertyName( "title" )]
        public string Title { get; set; }

        /// <summary>
        /// The bulletin text (the main message) of the Bulletin
        /// </summary>
        [JsonPropertyName( "value" )]
        public string BulletinText { get; set; }

        /// <summary>
        /// The bulletin short text
        /// </summary>
        [JsonPropertyName( "value_short" )]
        public string BulletinShortText { get; set; }

        /// <summary>
        /// The type.
        /// </summary>
        [JsonPropertyName( "type" )]
        public string Type { get; set; }

        /// <summary>
        /// An image string that can contain multiple
        /// images separated using a separator. Each
        /// consecutive image is placed on top of the 
        /// previous image, creating a single image.
        /// 
        /// Use CompoundedImageList() method to get a
        /// list of images
        /// </summary>
        [JsonPropertyName( "images" )]
        public string ImagesString { get; set; }

        /// <summary>
        /// The Schedule, this contains the DateTime specifying
        /// when the Bulletin can be displayed to the user.
        /// </summary>
        [JsonPropertyName( "schedule" )]
        public BulletinItemSchedule Schedule { get; set; }

        /// <summary>
        /// Returns a list of all of the images in the order they should
        /// be placed on a UI, with the backmost image first.
        /// </summary>
        /// <returns>A List of images, this can be null</returns>
        public List<string> CompoundedImageList()
        {
            List<string> listResult = null;

            if ( ImagesString != null )
            {
                string[] arrayOfImages = ImagesString.Split( c_imageSeparator );
                listResult = arrayOfImages.ToList();
            }

            return listResult;
        }

        /// <summary>
        /// The separator used between images
        /// </summary>
        private const char c_imageSeparator = ',';
    }

    /// <summary>
    /// Represents a Server DateTime in UTC which is
    /// the dateTime specifying when the Bulletin can 
    /// be displayed to the user. The Bulletin must not
    /// be displayed before this DateTime.
    /// </summary>
    public class BulletinItemSchedule
    {
        /// <summary>
        /// The DateTime to display the Bulletin from.
        /// This is in Server UTC time.
        /// </summary>
        [JsonPropertyName( "start" )]
        public string Start { get; set; }

        /// <summary>
        /// The date as a real DateTime
        /// </summary>
        /// <returns>The date as a DateTime</returns>
        public DateTime DateAsDateTime()
        {
            DateTime result = DateTime.Now;
            if ( !string.IsNullOrWhiteSpace( Start ) )
            {
                try
                {
                    result = DateTime.Parse( Start );
                }
                catch ( Exception )
                {
                    // We can't error out, so fail by not doing anything.
                    Debug.Assert( false );
                }
            }
            return result;
        }
    }
}
