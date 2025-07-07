//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! FeaturedProducts, represents Featured Products. This set of classes 
//              is used by JsonSerializer to Deserialize JSON strings. Hence
//              these classes match the data returned from the FORC API.
//
//! Author:     Alan MacAree
//! Created:    28 Oct 2022
//----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace JSONConverters
{
    /// <summary>
    /// Featured class, represents the top level
    /// featured products information from the
    /// server FORC Api
    /// </summary>
    public class Featured
    {
        /// <summary>
        /// The featured string, featured products are under this
        /// </summary>
        [JsonPropertyName("featured")]
        public FeaturedContainer TheFeaturedContainer { get; set; }
    }

    /// <summary>
    /// FeaturedContainer, holds a list of
    /// FeaturedProducts
    /// </summary>
    public class FeaturedContainer
    {
        /// <summary>
        /// The title
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; }

        /// <summary>
        /// The list of "FeaturedProducts" objects
        /// </summary>
        [JsonPropertyName("modules")]
        public List<FeaturedProducts> FeaturedProductsList { get; set; }
    }

    /// <summary>
    /// FeaturedProducts, details of a featured product
    /// </summary>
    public class FeaturedProducts
    {
        /// <summary>
        /// The Sku
        /// </summary>
        [JsonPropertyName("sku")]
        public string Sku { get; set; }

        /// <summary>
        /// The Entity ID
        /// </summary>
        [JsonPropertyName("entity_id")]
        public string EntityID { get; set; }

        /// <summary>
        /// The Parent Sku
        /// </summary>
        [JsonPropertyName("parent_sku")]
        public List<string> ParentSku { get; set; }

        /// <summary>
        /// The Child Sku
        /// </summary>
        [JsonPropertyName("child_sku")]
        public List<string> ChildSku { get; set; }

        /// <summary>
        /// The title
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; }

        /// <summary>
        /// The position
        /// </summary>
        [JsonPropertyName("position")]
        public int Position { get; set; }

        /// <summary>
        /// Current price
        /// </summary>
        [JsonPropertyName("current_price")]
        public double CurrentPrice { get; set; }

        /// <summary>
        /// Original price
        /// </summary>
        [JsonPropertyName("original_price")]
        public double OriginalPrice { get; set; }

        /// <summary>
        /// Attribute List
        /// </summary>
        [JsonPropertyName("attribute_list")]
        public List<int> AttributeList { get; set; }

        /// <summary>
        /// Thumbnail Uri
        /// </summary>
        [JsonPropertyName("thumbnail")]
        public string ThumbnailUri { get; set; }

        /// <summary>
        /// Image Uri
        /// </summary>
        [JsonPropertyName("image")]
        public string ImageUri { get; set; }

        /// <summary>
        /// Small Image Uri
        /// </summary>
        [JsonPropertyName("small_image")]
        public string SmallImageUri { get; set; }

        /// <summary>
        /// The elite name
        /// </summary>
        [JsonPropertyName("elite_name")]
        public string EliteName { get; set; }

        /// <summary>
        /// Is this available
        /// </summary>
        [JsonPropertyName("available")]
        public bool Available { get; set; }

        /// <summary>
        /// Is this owned
        /// </summary>
        [JsonPropertyName("owned")]
        public bool Owned { get; set; }

        /// <summary>
        /// The description
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>
        /// The short description
        /// </summary>
        [JsonPropertyName("short_description")]
        public string ShortDescription { get; set; }

        /// <summary>
        /// The items list 
        /// </summary>
        [JsonPropertyName("itemslist")]
        public List<FeaturedProductItem> ItemsList { get; set; }

        /// <summary>
        /// Extras Subtype
        /// </summary>
        [JsonPropertyName("extras_subtype")]
        public string ExtrasSubtype { get; set; }

        /// <summary>
        /// Extra Type 
        /// </summary>
        [JsonPropertyName("extra_type")]
        public int ExtraType { get; set; }

        /// <summary>
        /// Ed Livery Bg Alpha
        /// </summary>
        [JsonPropertyName("ed_livery_bg_alpha")]
        public string EdLiveryBgAlpha { get; set; }

        /// <summary>
        /// Ed Livery Bg Type
        /// </summary>
        [JsonPropertyName("ed_livery_bg_type")]
        public string EdLiveryBgType { get; set; }

        /// <summary>
        /// Ed Livery Bg Colour
        /// </summary>
        [JsonPropertyName("ed_livery_bg_colour")]
        public string EdLiveryBgColour { get; set; }

        /// <summary>
        /// Ed Livery Bg Icon Uri
        /// </summary>
        [JsonPropertyName("ed_livery_bg_icon")]
        public string EdLiveryBgIconUri { get; set; }

        /// <summary>
        /// Ed Livery Bg Icon Catalog 
        /// </summary>
        [JsonPropertyName("ed_livery_bg_icon_catalog")]
        public string EdLiveryBgIconCatalog { get; set; }

        /// <summary>
        /// Ed Livery Carousel Pos
        /// </summary>
        [JsonPropertyName("ed_livery_carousel_pos")]
        public string EdLiveryCarouselPos { get; set; }

        /// <summary>
        /// Ed Livery Item
        /// </summary>
        [JsonPropertyName("ed_livery_item")]
        public string EdLiveryItem { get; set; }

        /// <summary>
        /// Category Name
        /// </summary>
        [JsonPropertyName("category_name")]
        public string CategoryName { get; set; }

        /// <summary>
        /// Category
        /// </summary>
        [JsonPropertyName("category")]
        public string Category { get; set; }

        /// <summary>
        /// Url Key
        /// </summary>
        [JsonPropertyName("url_key")]
        public string UrlKey { get; set; }

        /// <summary>
        /// Media 
        /// </summary>
        [JsonPropertyName("media")]
        public List<MediaEntry> Media { get; set; }

        /// <summary>
        /// Gallery 
        /// </summary>
        [JsonPropertyName("gallery")]
        public List<FeaturedProductGallery> Gallery { get; set; }

        /// <summary>
        /// Fdl Product Info
        /// </summary>
        [JsonPropertyName("fdl_productinfo")]
        public string FdlProductInfo { get; set; }

        /// <summary>
        /// Fdl Compatibility
        /// </summary>
        [JsonPropertyName("fdl_compatibility")]
        public string FdlCompatibility { get; set; }

        /// <summary>
        /// Minimum Client Version
        /// </summary>
        [JsonPropertyName("minimum_client_version")]
        public int MinimumClientVersion { get; set; }

        /// <summary>
        /// Minimum Client Season
        /// </summary>
        [JsonPropertyName("minimum_season")]
        public int MinimumSeason { get; set; }

        /// <summary>
        /// Returns a list of all of the images in the order they should
        /// be placed on a UI, with the backmost image first.
        /// </summary>
        /// <returns>A List of images, this can be null</returns>
        public List<string> CompoundedImageList()
        {
            List<string> listResult = null;

            if (ImageUri != null)
            {
                string[] arrayOfImages = ImageUri.Split(c_imageSeparator);
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
    /// FeaturedProductItem
    /// </summary>
    public class FeaturedProductItem
    {
        /// <summary>
        /// Sku
        /// </summary>
        [JsonPropertyName("sku")]
        public string Sku { get; set; }

        /// <summary>
        /// Attribute List
        /// </summary>
        [JsonPropertyName("attribute_list")]
        public List<int> AttributeList { get; set; }

        /// <summary>
        /// Title
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    /// <summary>
    /// FeaturedProductGallery, the image Uris for a
    /// featured product.
    /// </summary>
    public class FeaturedProductGallery
    {
        /// <summary>
        /// Image Uri
        /// </summary>
        [JsonPropertyName("image")]
        public string ImageUri { get; set; }

        /// <summary>
        /// Image Uri
        /// </summary>
        [JsonPropertyName("position")]
        public int Position { get; set; }
    }

    public class MediaEntry
    {
        /// <summary>
        /// full URL to the media, typically a video, includes protocol
        /// e.g. https://player.vimeo.com/video/261473851?autoplay=1&loop=1&muted=1
        /// </summary>
        [JsonPropertyName("url")]
        public string url { get; set; }
        /// <summary>
        /// partial URL to a thumbnail image for the media, using same naming scheme as other images from the gallery
        /// e.g. media/catalog/product/frontier/thumbnails/VultureRaider.jpg
        /// </summary>
        [JsonPropertyName("thumbnail")]
        public string thumbnail { get; set; }
    }
}

