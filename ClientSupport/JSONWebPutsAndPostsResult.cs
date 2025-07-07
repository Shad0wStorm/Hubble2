//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! JSONWebPutsAndPostsResult, used as a data results class for Json Posts
//
//! Author:     Alan MacAree
//! Created:    25 Nov 2022
//----------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;

namespace ClientSupport
{
    /// <summary>
    /// Class used to return JSONWebPost results 
    /// </summary>
    public class JSONWebPutsAndPostsResult
    {
        /// <summary>
        /// An error key used within the HttpResponseDictionary to return
        /// "special" errors.
        /// </summary>
        public const string c_JsonErrorString = "JSONWebPostResultError";

        /// <summary>
        /// The resulting HttpStatusCode of an API call
        /// </summary>
        public HttpStatusCode HttpStatusResult { get; set; } = HttpStatusCode.Unused;

        /// <summary>
        /// Contains any response from the API call, may also contain 
        /// a c_JsonErrorString as a key with an associated value.
        /// This can be empty.
        /// </summary>
        public Dictionary<string, string> HttpResponseDictionary { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Contains the raw json result, this may be empty
        /// </summary>
        public string RawJsonResult { get; set; } = string.Empty;
    }
}
