//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! FeaturedProduct Class, holds featured product information
//
//! Author:     Alan MacAree
//! Created:    29 Dec 2022
//----------------------------------------------------------------------

using System.Collections.Generic;

namespace FDUserControls
{
    /// <summary>
    /// Represents a FeaturedProduct that a user can purchase
    /// </summary>
    public class FeaturedProduct
    {
        /// <summary>
        /// The products image
        /// </summary>
        public string ImageUri { get; } = null;

        public List<string> FallbackImages { get; set; } = new List<string>();

        /// <summary>
        /// the products price
        /// </summary>
        public string Price { get; } = null;

        /// <summary>
        /// The products title
        /// </summary>
        public string Title { get; } = null;

        /// <summary>
        /// The products type (e.g. Paint Jobs)
        /// </summary>
        public string Type { get;} = null;

        /// <summary>
        /// The Frontier link to purchase the item
        /// </summary>
        public string Link { get; } = null;

        /// <summary>
        /// Default contructor
        /// </summary>
        public FeaturedProduct()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_imageUri">The products images uri</param>
        /// <param name="_price">The products price</param>
        /// <param name="_title">The products title</param>
        /// <param name="_type">The products type</param>
        /// <param name="_link">The products link to purchase the item</param>
        public FeaturedProduct( string _imageUri,
                                string _price,
                                string _title,
                                string _type,
                                string _link)
        {
            ImageUri = _imageUri;
            Price = _price;
            Title = _title;
            Type = _type;
            Link = _link;
        }

        public void AddFallbackImage(string _imageUri)
        {
            FallbackImages.Add(_imageUri);
        }

        /// <summary>
        /// Overrides the ToString to provide all related data
        /// </summary>
        /// <returns>A String representing this FeaturedProduct</returns>
        public override string ToString()
        {
            string result = "";
            result += Title;
            result += " : ";
            result += Type;
            result += " : ";
            result += Price;
            result += " : ";
            result += ImageUri;
            result += " : ";
            result += Link;
            return result;
        }

    }
}
