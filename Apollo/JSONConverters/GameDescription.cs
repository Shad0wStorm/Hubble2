//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! GameDescription, represents a game description.
//
//! Author:     Alan MacAree
//! Created:    28 Jan 2023
//----------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace JSONConverters
{
    /// <summary>
    /// Top level Game Description, this matches the Json structure.
    /// </summary>
    public class GameDescription
    {
        /// <summary>
        /// The deescription string
        /// </summary>
        [JsonPropertyName( "description" )]
        public string Description { get; set; }

    }
}
