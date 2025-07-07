//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! JSONWebPutAndPost, Abstract base class for Json Put and Posts
//
//! Author:     Alan MacAree
//! Created:    22 Nov 2022
//----------------------------------------------------------------------

using ClientSupport;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace FORCServerSupport
{
    /// <summary>
    /// Abstract base class for JSON Web Posts
    /// </summary>
    public abstract class JSONWebPutAndPost
    {
        /// <summary>
        ///  The type of query
        /// </summary>
        public enum JSONWebType
        {
            Post = 0,
            Put,
            Delete
        }

        /// <summary>
        /// Store types. These are 3rd party store types that can be linked
        /// with a Frontier Account, allowing a seemless start of the game 
        /// when started via a linked store. 
        /// </summary>
        public enum LinkStoreAccountPost
        {
            Steam = 0,
            Epic
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_forcServerConnection">The FORCServerConnection, used to create logs</param>
        /// <param name="_userDetails">The users details, used to create logs</param>
        /// <param name="_jSONWebType">The type of query this JSONWebPutAndPost is for, defaults to JSONWebType.Post</param>
        public JSONWebPutAndPost( FORCServerConnection _forcServerConnection, 
                                  UserDetails _userDetails,
                                  JSONWebType _jSONWebType = JSONWebType.Post )
        {
            Debug.Assert( _forcServerConnection != null );
            Debug.Assert( _userDetails != null );

            m_forcServerConnection = _forcServerConnection;
            m_userDetails = _userDetails;

            switch ( _jSONWebType )
            {
                case JSONWebType.Post:
                    m_method = c_post;
                    break;
                case JSONWebType.Put:
                    m_method = c_put;
                    break;
                case JSONWebType.Delete:
                    m_method = c_delete;
                    break;
                default:
                    // Use the deault
                    m_method = c_post;
                    break;
            }
        }

        /// <summary>
        /// Executes a JSONWebPutAndPost to the past Url with the json string.
        /// 
        /// Note that the json decoding available via this method can only handle simple single
        /// layer json (not fields within fields, and cannot handle arrays). If complex json is
        /// expected, then pass _decodeJsonString = false and process the raw json from the returned
        /// result.
        /// 
        /// </summary>
        /// <param name="_serverUri">The server to sent the post to</param>
        /// <param name="_jsonString">The json string to include in the post</param>
        /// <param name="_decodeJsonString">Should the json string be decoded here, defaults to true (result stored in JSONWebPutsAndPostsResult)</param>
        /// <returns>The resulting JSONWebPostResult, may contain JSONWebPostResult.c_JsonErrorString</returns>
        public JSONWebPutsAndPostsResult ExecutePost( Uri _serverUri, string _jsonString, bool _decodeJsonString = true )
        {
            Uri serverUri = new Uri(_serverUri, Query);

            JSONWebPutsAndPostsResult jsonWebPostResult = new JSONWebPutsAndPostsResult();

            using ( HttpClient httpClient = new HttpClient() )
            {
                try
                {
                    using ( HttpRequestMessage request = new HttpRequestMessage( new HttpMethod( m_method ), serverUri ) )
                    {
                        // Add the json
                        request.Headers.Add( c_accept, c_applicationJson );
                        StringContent stringContent = new StringContent( _jsonString, Encoding.UTF8, c_applicationJson );
                        request.Content = stringContent;

                        // Add the headers
                        if ( m_headers != null )
                        {
                            foreach ( String header in m_headers.AllKeys )
                            {
                                request.Headers.Add( header, m_headers[header] );
                            }
                        }

                        using ( Task<HttpResponseMessage> responseTask = httpClient.SendAsync( request ) )
                        {
                            // Wait for the reply (but only for a period of time)
                            if ( responseTask.Wait( c_postTimeOutInMS ) )
                            {
                                if ( responseTask.Result != null )
                                {
                                    HttpResponseMessage response = responseTask.Result;
                                    jsonWebPostResult.HttpStatusResult = response.StatusCode;
                                    if ( response.StatusCode == HttpStatusCode.OK ||
                                         response.StatusCode == HttpStatusCode.Created )
                                    {
                                        Task<string> reply = response.Content.ReadAsStringAsync();
                                        jsonWebPostResult.RawJsonResult = reply.Result;
                                        // We don't always want to process the json string here, this is because the json
                                        // processing provided by JsonStringToDictionary is simple and only allows one level
                                        // of fields (not fields within fields). The client can decide to use the Raw Json
                                        // from the result.
                                        if ( _decodeJsonString )
                                        {
                                            jsonWebPostResult.HttpResponseDictionary = JsonStringToDictionary( reply.Result );
                                        }
                                    }
                                }
                                else
                                {
                                    // No result
                                    jsonWebPostResult.HttpStatusResult = HttpStatusCode.NoContent;
                                }
                            }
                            else
                            {
                                // We timed out
                                jsonWebPostResult.HttpStatusResult = HttpStatusCode.RequestTimeout;
                            }
                        }
                    }
                }
                catch( Exception ex )
                {
                    jsonWebPostResult.HttpStatusResult = HttpStatusCode.BadRequest;
                    Dictionary<string,string> errorDictionary = new Dictionary<string, string>();
                    errorDictionary.Add( JSONWebPutsAndPostsResult.c_JsonErrorString, ex.ToString() );
                    jsonWebPostResult.HttpResponseDictionary = errorDictionary;
                }

            }

            return jsonWebPostResult;
        }

        /// <summary>
        /// Adds a header to be used with the POST
        /// </summary>
        /// <param name="header"></param>
        /// <param name="value"></param>
        public void AddHeader( String header, String value )
        {
            if ( m_headers == null )
            {
                m_headers = new NameValueCollection();
            }
            m_headers[header] = value;
        }

        /// <summary>
        /// Add a parameter to the query.
        /// </summary>
        /// <param name="parameter">Name of the parameter.</param>
        /// <param name="value">Value of the parameter.</param>
        public void AddParameter( String parameter, String value )
        {
            try
            {
                if ( m_queryParameters == null )
                {
                    m_queryParameters = HttpUtility.ParseQueryString( String.Empty );
                }

                if ( m_queryParameters != null )
                {
                    m_queryParameters[parameter] = value;
                }
            }
            catch( Exception ex )
            {
                Debug.Assert( false );

                string action = GetType().ToString() + Consts.c_classMethodSeparator + MethodBase.GetCurrentMethod().Name;
                LogEvent( action, "Exception", ex.ToString() );
            }
        }

        /// <summary>
        /// The query string based on the base path and the query parameters
        /// set via AddParameter.
        /// </summary>
        public String Query
        {
            get
            {
                string result = "";
                if ( m_queryParameters != null )
                {
                    if ( m_queryParameters.Count > 0 )
                    {
                        result += "?";
                        result += m_queryParameters.ToString();
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Converts a Dictionary<string, string> into a json string for use with ExecutePost
        /// </summary>
        /// <param name="_dictionary">The Dictionary holding the data to convert to json</param>
        /// <returns>A json formatted string created from the passed Dictionary</returns>
        public string DictionaryToJsonString( Dictionary<string, string> _dictionary )
        {
            string jsonFromDictionary = "";

            if ( _dictionary != null )
            {
                foreach ( KeyValuePair<string, string> kvp in _dictionary )
                {
                    if ( !String.IsNullOrEmpty( kvp.Key ) )
                    {
                        if ( jsonFromDictionary.Length > 0 )
                        {
                            jsonFromDictionary += c_jsonFieldSeparator;
                        }

                        jsonFromDictionary += EncloseInJsonQuuote( kvp.Key );
                        jsonFromDictionary += c_jsonFieldValueSeparator;
                        if ( kvp.Value != null )
                        {
                            jsonFromDictionary += EncloseInJsonQuuote( kvp.Value );
                        }
                        else
                        {
                            jsonFromDictionary += c_jsonNull;
                        }
                    }
                }
            }

            string jsonString = c_jsonStartMarker;
            jsonString += jsonFromDictionary;
            jsonString += c_jsonEndMarker;

            return jsonString;
        }

        /// <summary>
        /// Converts a Dictionary<string, object> into a json string for use with ExecutePost
        /// </summary>
        /// <param name="_dictionary">The Dictionary holding the data to convert to json</param>
        /// <returns>A json formatted string created from the passed Dictionary</returns>
        public string DictionaryToJsonString( Dictionary<string, object> _dictionary )
        {
            string jsonFromDictionary = "";

            if ( _dictionary != null )
            {
                foreach ( KeyValuePair<string, object> kvp in _dictionary )
                {
                    if ( !String.IsNullOrEmpty( kvp.Key ) )
                    {
                        if ( jsonFromDictionary.Length > 0 )
                        {
                            jsonFromDictionary += c_jsonFieldSeparator;
                        }

                        jsonFromDictionary += EncloseInJsonQuuote( kvp.Key );
                        jsonFromDictionary += c_jsonFieldValueSeparator;
                        if ( kvp.Value != null )
                        {
                            if ( kvp.Value is string )
                            {
                                jsonFromDictionary += EncloseInJsonQuuote( kvp.Value.ToString() );
                            }
                            if ( kvp.Value is bool )
                            {
                                // json requires lower case bools
                                jsonFromDictionary += ( kvp.Value.ToString() ).ToLower();
                            }
                        }
                        else
                        {
                            jsonFromDictionary += c_jsonNull;
                        }
                    }
                }
            }

            string jsonString = c_jsonStartMarker;
            jsonString += jsonFromDictionary;
            jsonString += c_jsonEndMarker;

            return jsonString;
        }


        /// <summary>
        /// Converts a json string into a Dictionary<string, string>.
        /// </summary>
        /// <param name="_jsonString">The string containing the json formatted text</param>
        /// <returns>A Dictionary containing the field and value pairs from the passed json string</returns>
        private Dictionary<string, string> JsonStringToDictionary( string _jsonString )
        {
            Dictionary<string, string> resultingDictionary = new Dictionary<string, string>();

            int posOfNewline = _jsonString.IndexOf( "\n" );

            while ( posOfNewline != -1 )
            {
                _jsonString = _jsonString.Remove( posOfNewline, 1 );
                bool continueForLoop = true;
                for ( int pos = posOfNewline; pos < _jsonString.Length && continueForLoop; pos++ )
                {
                    if ( _jsonString[pos].CompareTo( ' ' ) == 0 )
                    {
                        _jsonString = _jsonString.Remove( pos--, 1 );
                    }
                    else
                    {
                        continueForLoop = false;
                    }
                }

                posOfNewline = _jsonString.IndexOf( "\n" );
            }



            if ( _jsonString != null )
            {
                // Strip the start and end json markers
                _jsonString = _jsonString.Replace( c_jsonStartMarker, "" );
                _jsonString = _jsonString.Replace( c_jsonEndMarker, "" );

                // For each pair of key and value:
                //  split the key and value
                //  remove the quotes
                List<string> listOfKeyValuePairs = (_jsonString.Split( c_jsonFieldSeparator )).ToList();
                foreach ( string keyValueStringWithQuotes in listOfKeyValuePairs )
                {
                    if ( !string.IsNullOrWhiteSpace( keyValueStringWithQuotes ) )
                    {
                        string keyValueString = keyValueStringWithQuotes.Replace( c_jsonQuote, "" );
                        string[] keyValuePairArray = keyValueString.Split( c_jsonFieldValueSeparator );
                        if ( keyValuePairArray.Count() == 2 )
                        {
                            string key = keyValuePairArray[0];
                            string value = keyValuePairArray[1];
                            if ( !string.IsNullOrWhiteSpace( key ) )
                            {
                                if ( !string.IsNullOrWhiteSpace( value ) )
                                {
                                    resultingDictionary.Add( key, value );
                                }
                                else
                                {
                                    resultingDictionary.Add( key, "" );
                                }
                            }
                        }
                    }
                }
            }

            return resultingDictionary;
        }

        /// <summary>
        /// Encloses the past string in Json Quotes, 
        /// </summary>
        /// <param name="stringToEnclose">The string to enclose in Json Quotes, should not be null.</param>
        /// <returns></returns>
        private string EncloseInJsonQuuote( string stringToEnclose )
        {
            Debug.Assert( stringToEnclose != null );

            string resultingString = c_jsonQuote;

            if ( stringToEnclose != null )
            {
                resultingString += stringToEnclose;
            }

            resultingString += c_jsonQuote;

            return resultingString;
        }


        /// <summary>
        /// Returns the store prefix for the passed LinkStoreAccountPost
        /// </summary>
        /// <param name="_linkStoreAccountPost">The store to get the prefix for</param>
        /// <returns>A string containing the prefix for the specified store</returns>
        protected string GetStorePrefix( LinkStoreAccountPost _linkStoreAccountPost )
        {
            string prefix = c_steamPrefix;

            switch ( _linkStoreAccountPost )
            {
                case LinkStoreAccountPost.Steam:
                    prefix = c_steamPrefix;
                    break;
                case LinkStoreAccountPost.Epic:
                    prefix = c_epicPrefix;
                    break;
            }

            // We should have a prefix.
            Debug.Assert( !string.IsNullOrWhiteSpace( c_steamPrefix ) );

            return prefix;
        }

        /// <summary>
        /// Logs an event (good or bad), along with a key and a description
        /// </summary>
        /// <param name="_action">The name of the action to log, must not be null or empty</param>
        /// <param name="_key">The key describing the description, must not be null or empty</param>
        /// <param name="_description">The description of the log, must not be null or empty</param>
        protected void LogEvent( string _action, string _key, string _description )
        {
            Debug.Assert( !string.IsNullOrWhiteSpace( _action ) );
            Debug.Assert( !string.IsNullOrWhiteSpace( _key ) );
            Debug.Assert( !string.IsNullOrWhiteSpace( _description ) );

            if ( m_forcServerConnection != null )
            {
                LogEntry logEntry = new LogEntry( _action );
                logEntry.AddValue( _key, _description );
                m_forcServerConnection.LogValues( m_userDetails, logEntry );
            }
        }

        /// <summary>
        /// The time we wait on Posts before we timeout
        /// </summary>
        private const int c_postTimeOutInMS = 5000;
        /// <summary>
        /// Json start marker
        /// </summary>
        private const string c_jsonStartMarker = "{";
        /// <summary>
        /// Json end marker
        /// </summary>
        private const string c_jsonEndMarker = "}";
        /// <summary>
        /// Json field value separator
        /// </summary>
        private const char c_jsonFieldValueSeparator = ':';
        /// <summary>
        /// Json field separator
        /// </summary>
        private const char c_jsonFieldSeparator = ',';
        /// <summary>
        /// Json quotes
        /// </summary>
        private const string c_jsonQuote = "\"";
        /// <summary>
        /// Json null
        /// </summary>
        private const string c_jsonNull = "null";
        /// <summary>
        /// Post string for posting a message
        /// </summary>
        private const string c_post = "POST";
        /// <summary>
        /// Put string for puts
        /// </summary>
        private const string c_put = "PUT";
        /// <summary>
        /// DELETE string for deletes
        /// </summary>
        private const string c_delete = "DELETE";
        /// <summary>
        /// 
        /// </summary>
        private const string c_accept = "Accept";
        /// <summary>
        /// 
        /// </summary>
        private const string c_applicationJson = "application/json";

        /// <summary>
        /// The method to use
        /// </summary>
        private string m_method = c_post;

        /// <summary>
        /// Headers to be included with the post
        /// </summary>
        private NameValueCollection m_headers = null;

        /// <summary>
        /// Parameters7 to be included with the post
        /// </summary>
        private NameValueCollection m_queryParameters = null;

        /// <summary>
        /// The steam auth prefix. Note this requires a space after the text,
        /// this is then combined with the steam auth token. Therefore, 
        /// do not remove the space at the end of the string.
        /// </summary>
        private const string c_steamPrefix = "steam ";

        /// <summary>
        /// The epic auth prefix. Note this requires a space after the text,
        /// this is then combined with the epic auth token. Therefore, 
        /// do not remove the space at the end of the string.
        /// </summary>
        private const string c_epicPrefix = "epic ";

        /// <summary>
        /// The oculus auth prefix. Note this requires a space after the text,
        /// this is then combined with the oculus auth token. Therefore, 
        /// do not remove the space at the end of the string.
        /// </summary>
        private const string c_oculusPrefix = "oculus ";

        /// <summary>
        /// Our FORCServerConnection, used to create logs
        /// </summary>
        private FORCServerConnection m_forcServerConnection;

        /// <summary>
        /// The user details, used to create logs
        /// </summary>
        private UserDetails m_userDetails;
    }
}
