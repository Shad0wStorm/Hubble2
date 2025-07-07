using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using ClientSupport;
using System.Diagnostics;

namespace FORCServerSupport
{
    /// <summary>
    /// Abstract out some of the server interactions to better handle the
    /// insistence of the HttpWebRequest to use exceptions instead of 
    /// response codes.
    /// </summary>
    public class JSONWebQuery
    {
        private NameValueCollection m_queryParameters = null;
        private NameValueCollection m_headers = null;
        private String m_base;
        private String m_method;
        private String m_body;
        private static String s_userAgent;

        public static String UserAgent { get { return s_userAgent; } }

        private String m_queryText;
        public String QueryText { get { return m_queryText; } }

        private String m_responseText;
        public String ResponseText { get { return m_responseText; } }

        /// <summary>
        /// The query string based on the base path and the query parameters
        /// set via AddParameter.
        /// </summary>
        public String Query
        {
            get
            {
                string result = m_base;
                if (m_queryParameters!=null)
                {
                    if (m_queryParameters.Count > 0)
                    {
                        result += "?";
                        result += m_queryParameters.ToString();
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="queryBase">
        /// The base part of the query (before the '?' if any).
        /// </param>
        public JSONWebQuery(String queryBase, bool get=true)
        {
            m_base = queryBase;
            if (get)
            {
                m_method = "GET";
            }
            else
            {
                m_method = "POST";
            }
        }

        /// <summary>
        /// Add a parameter to the query.
        /// </summary>
        /// <param name="parameter">Name of the parameter.</param>
        /// <param name="value">Value of the parameter.</param>
        public void AddParameter(String parameter, String value)
        {
            if (m_queryParameters == null)
            {
                m_queryParameters = HttpUtility.ParseQueryString(String.Empty);
            }
            m_queryParameters[parameter] = value;
        }

        public void AddHeader(String header, String value)
        {
            if (m_headers == null)
            {
                m_headers = new NameValueCollection();
            }
            m_headers[header] = value;
        }

        public void AddTimeStamp(String parameter, DateTime start, long seconds)
        {
            TimeSpan hourglass = DateTime.UtcNow.Subtract(start);
            AddParameter(parameter, (seconds + hourglass.TotalSeconds).ToString());
        }

        public void SetBody(String body)
        {
            m_body = body;
        }

        public static void SetUserAgent(String name, String version, String osident, String comment)
        {
            s_userAgent = name;
            if (!String.IsNullOrEmpty(version))
            {
                s_userAgent += "/" + version;
            }
            if (!String.IsNullOrEmpty(osident))
            {
                s_userAgent += "/" + osident;
            }
            if (!String.IsNullOrEmpty(comment))
            {
                s_userAgent += " (" + comment + ")";
            }
        }

        /// <summary>
        /// Execute the query against the passed server.
        /// Note, HttpWebResponse (returned) implements IDisposable
        /// </summary>
        /// <param name="_server">
        /// Server to receive the query. The query string consisting of the
        /// original base passed and any query parameters is treated as
        /// relative to the server Uri passed in.
        /// </param>
        /// <param name="_allowRedirect">
        /// if the request should automatically follow redirection responses from the Internet resource
        /// </param>
        /// <returns>HttpWebResponse, note that this needs to be disposed</returns>
        /// <exception cref="WebException"></exception>
        /// <exception cref="Exception"></exception>
        public HttpWebResponse ExecuteRequest( Uri _server, bool _allowRedirect = true )
        {
            Uri loginRequest = new Uri(_server, Query);
            m_queryText = m_method + " " + Query;

            FORCManager.EnsureIsUsingSecureTlsProtocol();

            HttpWebRequest request = HttpWebRequest.Create(loginRequest) as HttpWebRequest;
            request.Method = m_method;

            // MCH - 20150505 - OSV-721
            // Search for above string to find related changes.
            // Disable keepalive based on suggestion 1 here:
            // http://stackoverflow.com/questions/21481682/httpwebrequest-the-underlying-connection-was-closed-the-connection-was-closed
            // in the hope it improves reliability.
            // It may affect performance, I do not expect that as the number of
            // requests is fairly low but if reliability is better but
            // performance measurably worse the alternative suggestsions can
            // be considered.
            request.KeepAlive = false;
            if ( !String.IsNullOrEmpty( s_userAgent ) )
            {
                request.UserAgent = s_userAgent;
            }
            // Disable proxy lookup which adds a significant overhead on each
            // request for some reason.
            // On one hand we probably do not want to use a proxy anyway, since
            // we do not want to retrieved cached values for example. On the
            // other hand if we do not have a direct route to the outside world
            // then this could stop everything working. Ideally we want to do
            // the proxy look up once, and then stick with that.
            request.Proxy = WebRequest.DefaultWebProxy;
            request.AllowAutoRedirect = _allowRedirect;

            if ( !String.IsNullOrEmpty( m_body ) )
            {
                ASCIIEncoding encoding = new ASCIIEncoding();
                byte[] bodyBytes = encoding.GetBytes(m_body);
                request.ContentType = "text/plain";
                request.ContentLength = bodyBytes.Length;
                using ( Stream stream = request.GetRequestStream() )
                {
                    stream.Write( bodyBytes, 0, bodyBytes.Length );
                    stream.Close();
                }
            }
            if ( m_headers != null )
            {
                foreach ( String header in m_headers.AllKeys )
                {
                    request.Headers[header] = m_headers[header];
                }
            }

            return (HttpWebResponse)request.GetResponse();
        }

        /// <summary>
        /// Execute the query against the passed server and returns a Dictionary
        /// </summary>
        /// <param name="_server">
        /// Server to receive the query. The query string consisting of the
        /// original base passed and any query parameters is treated as
        /// relative to the server Uri passed in.
        /// </param>
        /// <param name="_decoded">
        /// Dictionary reference will be set to the dictionary of objects
        /// extracted from the JSON response.
        /// </param>
        /// <param name="_message">
        /// Error message regarding unusual results.
        /// </param>
        /// <param name="_allowRedirect">
        /// if the request should automatically follow redirection responses from the Internet resource
        /// </param>
        /// <returns>Status code from the web request.</returns>
        public HttpStatusCode Execute(  Uri _server, 
                                        out Dictionary<String, object> _decoded, 
                                        out String _message,
                                        bool _allowRedirect = true, 
                                        bool _expectArray = false)
        {
            _decoded = null;
            _message = null;

            try
            {
                using ( HttpWebResponse response = ExecuteRequest( _server, _allowRedirect ) )
                {
                    switch ( response.StatusCode )
                    {
                        case HttpStatusCode.OK:
                            {
                                _decoded = ResponseToDictionary( response, _expectArray );
                                return response.StatusCode;
                            }
                        case HttpStatusCode.Found:
                        case HttpStatusCode.TemporaryRedirect:
                            {
                                _decoded = ResponseToDictionary( response );
                                if ( _decoded == null )
                                {
                                    _decoded = new Dictionary<string, object>();
                                    foreach ( String key in response.Headers.AllKeys )
                                    {
                                        _decoded[key] = response.Headers[key];
                                    }
                                    _decoded["DecodedHeaders"] = "True";
                                }
                                return response.StatusCode;
                            }
                    }
                }
            }
            catch (WebException we)
            {
                using (WebResponse response = we.Response)
                {
                    using( HttpWebResponse httpResponse = (HttpWebResponse)response)
                    {
                        if (httpResponse != null)
                        {
                            try
                            {
                                _decoded = ResponseToDictionary(httpResponse);
                            }
                            catch (System.Exception ex)
                            {
                                _message = String.Format(LocalResources.Properties.Resources.FSSJWQ_SRParseFail,
                                    ex.Message);
                                return HttpStatusCode.ServiceUnavailable;
                            }
                            return httpResponse.StatusCode;
                        }
                        else
                        {
                            // Do not try to use an invalid response.
                            _message = String.Format(LocalResources.Properties.Resources.FSSJWQ_SRNoResponse,
                                we.Message);
                            return HttpStatusCode.ServiceUnavailable;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _message = String.Format(LocalResources.Properties.Resources.FSSJWQ_SRQueryX,
                    ex.Message);
            }
            return HttpStatusCode.InternalServerError;
        }

        /// <summary>
        /// Execute the query against the passed server and gets a raw JSON string
        /// </summary>
        /// <param name="server">
        /// Server to receive the query. The query string consisting of the
        /// original base passed and any query parameters is treated as
        /// relative to the server Uri passed in.
        /// </param>
        /// <param name="_jsonString">
        /// The raw json string
        /// </param>
        /// <param name="message">
        /// Error message regarding unusual results.
        /// </param>
        /// <param name="_allowRedirect">
        /// if the request should automatically follow redirection responses from the Internet resource
        /// </param>
        /// <returns>Status code from the web request.</returns>
        public HttpStatusCode Execute( Uri _server,
                                        out string _jsonString,
                                        out String _message,
                                        bool _allowRedirect = true )
        {
            Debug.Assert( _server != null );

            HttpStatusCode resultingHttpStatusCode = HttpStatusCode.InternalServerError;
            _jsonString = null;
            _message = null;

            if ( _server != null )
            { 
                try
                {
                    using ( HttpWebResponse response = ExecuteRequest( _server, _allowRedirect ) )
                    {
                        if ( response != null )
                        {
                            resultingHttpStatusCode = response.StatusCode;

                            switch ( response.StatusCode )
                            {
                                case HttpStatusCode.OK:
                                    {
                                        JavaScriptSerializer jss = new JavaScriptSerializer();
                                        using ( StreamReader reader = new StreamReader( response.GetResponseStream() ) )
                                        {
                                            _jsonString = reader.ReadToEnd();
                                        }
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            _message = String.Format( LocalResources.Properties.Resources.FSSJWQ_SRNoResponse, _server.ToString() );
                            resultingHttpStatusCode = HttpStatusCode.ServiceUnavailable;
                        }
                    }
                }
                catch ( WebException we )
                {
                    using ( WebResponse response = we.Response )
                    {
                        using ( HttpWebResponse httpResponse = (HttpWebResponse)response )
                        {
                            if ( httpResponse != null )
                            {
                                resultingHttpStatusCode = httpResponse.StatusCode;
                            }
                            else
                            {
                                // Do not try to use an invalid response.
                                _message = String.Format( LocalResources.Properties.Resources.FSSJWQ_SRNoResponse, we.Message );
                                resultingHttpStatusCode = HttpStatusCode.ServiceUnavailable;
                            }
                        }
                    }
                }
                catch ( Exception ex )
                {
                    _message = String.Format( LocalResources.Properties.Resources.FSSJWQ_SRQueryX, ex.Message );
                    resultingHttpStatusCode = HttpStatusCode.InternalServerError;
                }
            }

            return resultingHttpStatusCode;
        }

        /// <summary>
        /// Parse the content text from a web response as JSON formated and
        /// create a dictionary result.
        /// </summary>
        /// <param name="response">The response to parse.</param>
        /// <returns>Dictionary object</returns>
        private Dictionary<String, object> ResponseToDictionary(HttpWebResponse response, bool expectArray=false)
        {

            JavaScriptSerializer jss = new JavaScriptSerializer();
            Stream responseStream = response.GetResponseStream();
            StreamReader r = new StreamReader(responseStream);
            String content = r.ReadToEnd();
            m_responseText = content;

            try
            {
                Dictionary<String, object> result;
                if (expectArray)
                {
                    result = new Dictionary<String, object>();
                    result["array"] = jss.Deserialize<List<object>>(content);
                }
                else
                {
                    result = jss.Deserialize<Dictionary<String, object>>(content);
                    if ((result != null) && (false))
                    {
                        result["#ContentString"] = content;
                    }
                }
                return result;
            }
            catch (System.ArgumentException)
            {
                return null;
            }
            catch (System.InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the response as an int.
        /// </summary>
        /// <param name="response">The Dictionary holding the expected response</param>
        /// <param name="name">The key of the value within the Dictionary to get</param>
        /// <param name="original">A default value to use as the return should the key or type be incorrect</param>
        /// <returns>original or the value of the int with the key=name</returns>
        protected int GetResponseInt( Dictionary<String, object> response,
                                      String name, int original )
        {
            int result = original;
            if ( response != null )
            {
                Object valueObject = null;
                if ( response.TryGetValue( name, out valueObject ) )
                {
                    if ( valueObject is int )
                    {
                        result = (int)valueObject;
                    }
                }
            }
            return result;
        }

        protected String GetResponseText(Dictionary<String,object> response,
            String name, String original)
        {
            String result = original;
            if (response!=null)
            {
                if (response.ContainsKey(name))
                {
                    result = response[name] as String;
                }
            }
            return result;
        }
    }
}
