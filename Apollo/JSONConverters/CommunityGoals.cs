//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! CommunityGoals, represents Community Goals. This set of classes is used
//              by JsonSerializer to Deserialize JSON strings. Hence
//              these classes match the data returned from the Elite API.
//
//! Author:     Alan MacAree
//! Created:    13 Oct 2022
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace JSONConverters
{
    /// <summary>
    /// Top level CommunityGoals, this matches the Json structure.
    /// </summary>
    public class CommunityGoals
    {
        /// <summary>
        /// The System string
        /// </summary>
        [JsonPropertyName( "system" )]
        public string System { get; set; }

        /// <summary>
        /// The location to get images from
        /// </summary>
        [JsonPropertyName( "image_url_prefix" )]
        public string ImageURL { get; set; }

        /// <summary>
        /// The image extension (e.g. .png, .bmp etc)
        /// </summary>
        [JsonPropertyName( "image_url_suffix" )]
        public string ImageExtension { get; set; }

        /// <summary>
        /// The list of data "Goals" objects
        /// </summary>
        [JsonPropertyName( "data" )]
        public List<Goal> Goals { get; set; }

        /// <summary>
        /// Determines if this object holds valid Community Goals
        /// information.
        /// </summary>
        /// <returns>true if this object holds valid Community Goals information</returns>
        public bool IsValid()
        {
            return (System.CompareTo( c_SystemString ) == 0);
        }

        /// <summary>
        /// The System string that specifies that the JSON contains
        /// CommunityGoals news information
        /// </summary>
        private const string c_SystemString = "Community Goals";
    }

    /// <summary>
    /// Represents CommunityGoal's JSON data
    /// </summary>
    public class Goal
    {
        /// <summary>
        /// The Id of the Community Goal
        /// </summary>
        [JsonPropertyName( "id" )]
        public UInt64 Id { get; set; }

        /// <summary>
        /// The title of the Community Goal
        /// </summary>
        [JsonPropertyName( "title" )]
        public string Title { get; set; }

        /// <summary>
        /// The expiry of the Community Goal as a string
        /// </summary>
        [JsonPropertyName( "expiry" )]
        public string ExpiryAsString { get; set; }

        /// <summary>
        /// The expiry of the Community Goal as a DateTime
        /// </summary>
        [JsonIgnore]
        public DateTime ExpiryAsDateTime
        {
            get
            {
                return DateTime.Parse( ExpiryAsString );
            }
        }

        /// <summary>
        /// The market name of the Community Goal
        /// </summary>
        [JsonPropertyName( "market_name" )]
        public string MarketName { get; set; }

        /// <summary>
        /// The star system name of the Community Goal
        /// </summary>
        [JsonPropertyName( "starsystem_name" )]
        public string StarSystemName { get; set; }

        /// <summary>
        /// The activity type of the Community Goal
        /// e.g. illegaltrade
        /// </summary>
        [JsonPropertyName( "activityType" )]
        public string ActivityType { get; set; }

        /// <summary>
        /// The target qty of the Community Goal
        /// e.g. illegaltrade
        /// </summary>
        [JsonPropertyName( "target_qty" )]
        public Int64 TargetQty { get; set; }

        /// <summary>
        /// The qty of the Community Goal
        /// e.g. illegaltrade
        /// </summary>
        [JsonPropertyName( "qty" )]
        public UInt64 Qty { get; set; }

        /// <summary>
        /// The objective of the Community Goal
        /// e.g. illegaltrade
        /// </summary>
        [JsonPropertyName( "objective" )]
        public string Objective { get; set; }

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
        /// The news of the Community Goal
        /// e.g. illegaltrade
        /// </summary>
        [JsonPropertyName( "news" )]
        public string News { get; set; }

        /// <summary>
        /// The bulletin of the Community Goal
        /// </summary>
        [JsonPropertyName( "bulletin" )]
        public string Bulletin { get; set; }

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
}
