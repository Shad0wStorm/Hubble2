using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using ClientSupport;

namespace FORCServerSupport
{
    class FORCServerState
    {
        public const String c_authToken = "authToken";
		public const String c_headerAuthToken = "Authorization";
		public const string c_machineIdQuery = "machineId";
        public const String c_machineToken = "machineToken";
        public const string c_errorenum = "error_enum";
        public const string c_errorCode = "errorCode";
        public const String c_message = "message";
        public const string c_timeQuery = "fTime";
        public const string c_registeredName = "registeredName";
		public const string c_language = "lang";
		public const string c_steam = "steam";
		public const string c_oculus = "oculus";

        public static class APIVersion
        {
            public const int V1_1 = 0;
			public const int V3_0 = 1;
        }

        /// <summary>
        /// Manager we are providing services to (not required, or only
        /// required if we allow the root url to be supplied externally?)
        /// </summary>
        public FORCManager m_manager;

        public FORCTelemetry m_remoteLogger;

#if DEVELOPMENT
		public String DevKey = null;
#endif

        /// <summary>
        /// Root of the requests we want to make to the server.
        /// </summary>
        private Uri m_server = new Uri("https://api.zaonce.net/");

        private String[] m_apiversions = { "1.1/", "3.0/" };

        public String m_newsServerPath = "https://api.orerve.net/1.5/elite/news/latest";
        public bool m_usingInternalServer = false;

        public int SupportedAPI = APIVersion.V1_1;

        /// <summary>
        /// The product server to use, this can be changed within this code to use
        /// a user defined (settings file) product server
        /// </summary>
        public string ProductServer { get; private set; } = "https://dlc.elitedangerous.com/product/";

        public Uri GetServerAPI(int index)
        {
            int ri = index;
            if (ri >= m_apiversions.Length)
            {
                ri = m_apiversions.Length - 1;
            }
            return new Uri(m_server, m_apiversions[ri]);
        }

        public String m_message;


        /// <summary>
        /// Defines a no error code for this class and
        /// LastErrorCode
        /// </summary>
        public const int c_ServerStateNoErrorCode = -1;

        /// <summary>
        /// Holds a last errorcode (if any), defaults to c_ServerStateNoErrorCode
        /// </summary>
        public int LastErrorCode { get; set; } = c_ServerStateNoErrorCode;

        public String m_language = null;
		public String Language
		{
			get
			{
				return m_language;
			}
			set
			{
                // Forc requires lower case languages Windows seems to prefer
                // upper case, or at least historically the launcher has done
                // i.e. pt-BR not pt-br.
                // This has 'infected' eg. via the file names so rather than
                // try and unpick everything to use lower case, make sure we
                // store the lower case language when we pass it to forc.
                String llanguage = value.ToLowerInvariant();
                if (m_language != llanguage)
				{
                    m_language = llanguage;
				}
			}
		}

        public FORCServerState(FORCManager manager)
        {
            m_manager = manager;
        }

        public bool m_alive = false;

        // Time tracking.
        /// <summary>
        /// Textual representation of the time returned from the server.
        /// Possibly no longer required as this class manages the time which
        /// needs to track real time.
        /// </summary>
        public String m_remoteTime;
        /// <summary>
        /// The local time that we got the response from the server so we can
        /// tell how much time has passed later.
        /// </summary>
        public DateTime m_systemStartTime;
        /// <summary>
        /// The server base time.
        /// 
        /// m_startTime + (DateTime.UtcNow - m_systemStartTime).TotalSeconds
        /// gives us the current time on the server (ish)
        /// </summary>
        public long m_startTime = 0;

        public void SetTime(long seconds)
        {
            m_startTime = seconds;
            m_remoteTime = m_startTime.ToString();
            m_systemStartTime = DateTime.UtcNow;
            if (String.IsNullOrEmpty(m_remoteTime))
            {
                m_remoteTime = m_systemStartTime.ToShortDateString() + m_systemStartTime.ToLongTimeString();
                DateTime origin = new DateTime(1970, 1, 1);
                TimeSpan span = m_systemStartTime.Subtract(origin);
                m_startTime = (long)span.TotalSeconds;
            }
        }

        /// <summary>
        /// Return the current time adjusting for any offset between the
        /// local machine and the server.
        /// </summary>
        /// <returns>Time string</returns>
        public String GetCurrentTimeStamp()
        {
            TimeSpan hourglass = DateTime.UtcNow.Subtract(m_systemStartTime);
            return (m_startTime + hourglass.TotalSeconds).ToString();
        }

        /// <summary>
        /// Helper method, add a parameter representing a time stamp to the
        /// passed query.
        /// </summary>
        /// <param name="query"></param>
        public void AddTimeStamp(JSONWebQuery query)
        {
            query.AddTimeStamp(c_timeQuery, m_systemStartTime, m_startTime);
        }

        public void ConfigureServer(String settings)
        {
#if DEVELOPMENT
			String devKeyFile = Path.Combine(settings, "DevKey.txt");
			if (File.Exists(devKeyFile))
			{
				try
				{
					String devKey = File.ReadAllText(devKeyFile);
					if (!String.IsNullOrEmpty(devKey))
					{
						DevKey = devKey;
					}
				}
				catch (System.Exception)
				{

				}
			}
			String[] argv = Environment.GetCommandLineArgs();
			bool wasDevKey = false;
			foreach (String arg in argv)
			{
				if (wasDevKey)
				{
					DevKey = arg;
					wasDevKey = false;
				}
				else
				{
					String lc = arg.ToLowerInvariant();
					if (lc == "/devkey")
					{
						wasDevKey = true;
					}
				}
			}

#endif
            String serverRedirect = Path.Combine(settings, "UseInternalServer.txt");
            if (File.Exists(serverRedirect))
            {
                try
                {
                    String redirect = File.ReadAllText(serverRedirect);
                    redirect = redirect.Trim();
                    m_server = new Uri(redirect);
                    m_usingInternalServer = true;
                }
                catch (System.Exception)
                {

                }
            }

            // Do we need to override the live product server?
            String productsRedirect = Path.Combine(settings, "UseProductServer.txt");
            if ( File.Exists( productsRedirect ) )
            {
                try
                {
                    String redirect = File.ReadAllText(productsRedirect);
                    redirect = redirect.Trim();
                    ProductServer = redirect;
                }
                catch ( System.Exception )
                {
                }
            }

            String newsRedirect = Path.Combine(settings, "NewsFeed.txt");
            if (File.Exists(newsRedirect))
            {
                try
                {
                    String redirect = File.ReadAllText(newsRedirect);
                    redirect = redirect.Trim();
                    if (redirect.Contains(':'))
                    {
                        m_newsServerPath = redirect;
                    }
                    else
                    {
                        if (redirect.StartsWith(".\\"))
                        {
                            String filePath = Path.GetFullPath(Path.Combine(settings, redirect));
                            if (File.Exists(filePath))
                            {
                                m_newsServerPath = "file://" + filePath;
                            }
                        }
                        else
                        {
                            m_newsServerPath = new Uri(m_server, redirect).ToString();
                        }
                    }
                }
                catch (System.Exception)
                {

                }
            }
        }
    }
}
