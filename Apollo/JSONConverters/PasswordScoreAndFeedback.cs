//----------------------------------------------------------------------
//! Copyright(c) 2023 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! PasswordScoreAndFeedback, represents a password score and feedback
//! for a given password (not included).
//!
//! Can contains what is wrong with the password along with suggestions 
//! as to how it can be improved.
//
//! Author:     Alan MacAree
//! Created:    19 April 2023
//----------------------------------------------------------------------

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JSONConverters
{
    public class PasswordScoreAndFeedback
    {
        /// <summary>
        /// A password score
        /// </summary>
        [JsonPropertyName( "score" )]
        public int Score { get; set; }

        /// <summary>
        /// Indicates if the password was acceptable or not.
        /// </summary>
        [JsonPropertyName( "pass" )]
        public bool Pass { get; set; }

        /// <summary>
        /// The password score feed back, this can be null.
        /// </summary>
        [JsonPropertyName( "feedback" )]
        public PasswordFeedback FeedBack { get; set; }
    }

    /// <summary>
    /// Represents PasswordFeedback
    /// </summary>
    public class PasswordFeedback
    {
        /// <summary>
        /// Holds a password feed back warning string, this is a 
        /// string which indicates what is wrong with the password.
        /// </summary>
        [JsonPropertyName( "warning" )]
        public string Warning { get; set; }

        /// <summary>
        /// Holds the list of suggestion strings on how to improve the password.
        /// </summary>
        [JsonPropertyName( "suggestions" )]
        public List<string> Suggestions { get; set; }

        /// <summary>
        /// Holds a hashed warning code that identifies the warning string
        /// </summary>
        [JsonPropertyName( "warning_code" )]
        public string WarningCode { get; set; }

        /// <summary>
        /// holds a list of hashed suggestion codes that identifies the suggestions.
        /// </summary>
        [JsonPropertyName( "suggestion_codes" )]
        public List<string> SuggestionCodes { get; set; }
    }
}
