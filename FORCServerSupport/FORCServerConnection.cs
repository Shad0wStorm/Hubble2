using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

using ClientSupport;


namespace FORCServerSupport
{
    public class FORCServerConnection : ServerInterface
    {
        // Keys used to look up values in the responses from the server.
        private const String c_tokenName = "token";

        /// <summary>
        /// Log object messages are sent to.
        /// </summary>
        private MultiLogger m_logger;

        private SKUDetails[] m_skuDetails = null;
        private String[] SKUList
        {
            get
            {
                if (m_skuDetails == null)
                {
                    return null;
                }
                if (m_skuDetails.Length == 0)
                {
                    return null;
                }
                String[] result = new String[m_skuDetails.Length];
                for (int i = 0; i < m_skuDetails.Length; ++i)
                {
                    result[i] = m_skuDetails[i].m_sku;
                }
                return result;
            }
        }

        private FORCServerState m_state;
        private FORCAuthorisationManager m_auth;
        private DownloadManagerBase m_downloader;

        public FORCServerConnection(FORCManager manager)
        {
            ServicePointManager.DefaultConnectionLimit = Environment.ProcessorCount * 12;

            if (Properties.Settings.Default.UpgradeSettings)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeSettings = false;
                Properties.Settings.Default.Save();
            }
            m_state = new FORCServerState(manager);
            m_auth = new FORCAuthorisationManager(m_state);

            String settingsPath = m_state.m_manager.GetLocalDirectory("Settings", false);
            if (settingsPath != null)
            {
                if (!m_state.m_manager.IsReleaseBuild)
                {
                    m_state.ConfigureServer(settingsPath);
                }
            }

            if (m_downloader == null)
            {
                FORCDownloadManager dlm = new FORCDownloadManager(m_state, this);
                m_downloader = new DownloadManagerLocalCache(dlm, manager);
            }

            m_logger = new MultiLogger();

            FileLogger fileLog = new FileLogger();
            fileLog.SetLogFile(m_state.m_manager, "Client.log");
            m_logger.AddLogger(fileLog);

            m_state.m_remoteLogger = new FORCTelemetry(m_state.m_manager);
            m_state.m_remoteLogger.SetServer(m_state.GetServerAPI(m_state.SupportedAPI));
            m_logger.AddLogger(m_state.m_remoteLogger);
        }

		/// <summary>
		/// Set the language to request from the server.
		/// </summary>
		/// <param name="language"></param>
		public void SetLanguage(String language)
		{
			String actual = language;
			if (String.IsNullOrEmpty(actual))
			{
				actual = LocalResources.Properties.Resources.LanguageCode;
				if (String.IsNullOrEmpty(actual))
				{
					// If there is no specified language we fall back to the
					// system default. Annoyingly this seems to be en-US even on
					// an en-GB system for some reason. Since we only provide
					// en resources that should be fine.
					actual = CultureInfo.CurrentUICulture.Name;
					List<String> available = m_state.m_manager.GetAvailableLanguages();
					String bestmatch = actual;
					foreach (String consider in available)
					{
						if (consider == actual)
						{
							// Found exact match use that.
							bestmatch = actual;
							break;
						}
						else
						{
							if ((actual.StartsWith(consider, true, CultureInfo.InvariantCulture)) &&
								(consider.Length < bestmatch.Length))
							{
								bestmatch = consider;
							}
						}
					}
					actual = bestmatch;
				}
			}

			m_state.Language = actual;
		}

        public override DownloadManagerBase GetDownloadManager() { return m_downloader; }

        /// <summary>
        /// The real server is already logged in if we have an existing token.
        /// </summary>
        /// <returns></returns>
        public override bool IsLoggedIn(UserDetails user)
        {
            return m_auth.IsLoggedIn(user);
        }

        /// <summary>
        /// Returns the product server URL to use to allow users to
        /// purchase items
        /// </summary>
        /// <returns>The product server URL or an empty string if m_state is null</returns>
        public override string GetProductServer()
        {
            string productServer = "";
            if ( m_state != null )
            {
                productServer = m_state.ProductServer;
            }

            return productServer;
        }

        /// <summary>
        /// Return a timestamp from the server. This is passed back to the
        /// server during authorisation to ensure that the request has been
        /// made recently. Clients do not interpret the value they simply pass
        /// it back unchanged so the server can use any representation it
        /// chooses.
        /// </summary>
        /// <returns></returns>
        public override String GetTimeStamp()
        {
            Queries.TimeStampQuery query = new Queries.TimeStampQuery();

            String time = query.Run(m_state);
            m_state.m_alive = !String.IsNullOrEmpty(time);

            // This call may have updated our concept of the current api version
            // so update the logger.
            m_state.m_remoteLogger.SetServer(m_state.GetServerAPI(m_state.SupportedAPI));
            return time;
        }

        /// <summary>
        /// Return the current time adjusting for any offset between the
        /// local machine and the server.
        /// </summary>
        /// <returns>Time string</returns>
        public override String GetCurrentTimeStamp()
        {
            return m_state.GetCurrentTimeStamp();
        }

        /// <summary>
        /// Return the status of the given version.
        /// </summary>
        public override VersionStatus CheckClientVersion(String version, out String current)
        {
            if (m_state.m_alive)
            {
                Queries.ClientVersionQuery cvq = new Queries.ClientVersionQuery();

                return cvq.Run(m_state, version, out current);
            }
            else
            {
                current = version;
                return VersionStatus.Current;
            }
        }

        /// <summary>
        /// Return the root path for the server. This may be useful for
        /// requests that are not made through the server interface for
        /// secondary features that could be provided by other servers.
        /// </summary>
        /// <returns></returns>
        public override String GetServerPath(String key)
        {
            String relative = null;

            // Special case any keys here, otherwise assume key is a relative
            // path.
            if (key == "News")
            {
                return m_state.m_newsServerPath;
            }
            relative = key;

            if (relative != null)
            {
                return m_state.GetServerAPI(m_state.SupportedAPI).ToString() + relative;
            }
            return null;
        }

        /// <summary>
        /// Request authorisation from the server based on the provided user
        /// details, machine identifier and a time previously returned from
        /// GetTimeStamp.
        /// </summary>
        /// <param name="user">
        /// User details for the user attempting to log in. The email and
        /// password fields are used to authenticate, along with the two factor
        /// code if provided. If successfully authorised (method returns
        /// AuthorisationResult.Authorised) then the user details
        /// AuthenticationToken will be updated with a value provided by the
        /// ServerInterface.
        /// </param>
        /// <param name="machine">
        /// Source to use to obtain an identifier for the current machine. If
        /// the server does not recognise the identifier it may request a two
        /// factor authentication token before authorising the user.
        /// </param>
        /// <param name="serverTime">
        /// The current server time as returned previously by GetTimeStamp().
        /// </param>
        /// <returns></returns>
        public override AuthorisationResult GetAuthorisation(UserDetails user, MachineIdentifierInterface machine, String serverTime)
        {
            if (m_state.m_alive)
            {
                return m_auth.GetAuthorisation(user, machine, serverTime);
            }
            else
            {
                return AuthorisationResult.Failed;
            }
        }

		public override AuthorisationResult UseAuthorisation(UserDetails user, MachineIdentifierInterface machine, String serverTime, NameValueCollection external)
		{
			if (m_state.m_alive)
			{
				return m_auth.UseAuthorisation(user, machine, serverTime, external);
			}
			else
			{
				return AuthorisationResult.Failed;
			}
		}

		/// <summary>
		/// Return the list of projects the user is authorised to access.
		/// </summary>
		/// <param name="user">
		/// The AuthenticationToken for the user is used to validate with the
		/// server.
		/// </param>
		/// <param name="serverTime">
		/// Time previously retrieved via GetTimeStamp() (unused).
		/// </param>
		/// <returns>
		/// A list of project (safe) names that the user is allowed to access.
		/// </returns>
		public override String[] GetProjectList(UserDetails user, String serverTime)
        {
            if (m_state.m_alive)
            {
                Queries.ProjectListQuery plq = new Queries.ProjectListQuery();

                m_skuDetails = plq.Run(m_state, user);
            }

            //LogEntry pll = new LogEntry("ProjectListQuery");
            //pll.AddValue("user", user.EmailAddress);
            //pll.AddValue("query", plq.QueryText);
            //pll.AddValue("response", plq.ResponseText);
            //LogValues(user, pll);

            if (m_skuDetails!=null)
            {
                return SKUList;
            }
            return null;
        }

        public override String GetTitleForProject(String project)
        {
            if (m_skuDetails!=null)
            {
                foreach (SKUDetails details in m_skuDetails)
                {
                    if (details.m_sku == project)
                    {
                        return details.m_name;
                    }
                }
            }
            return project;
        }

        public override String GetSortKeyForProject(String project)
        {
            if (m_skuDetails!=null)
            {
                foreach (SKUDetails details in m_skuDetails)
                {
                    if (details.m_sku == project)
                    {
                        return details.m_sortKey;
                    }
                }
            }
            return project;
        }

        public override String GetDirectoryForProject(String project)
        {
            if (m_skuDetails != null)
            {
                foreach (SKUDetails details in m_skuDetails)
                {
                    if (details.m_sku == project)
                    {
                        return details.m_directory;
                    }
                }
            }
            return project;
        }

		public override String GetHighlightColourForProject(String project)
		{
			if (m_skuDetails != null)
			{
				foreach (SKUDetails details in m_skuDetails)
				{
					if (details.m_sku == project)
					{
						if (!String.IsNullOrEmpty(details.m_highlight))
						{
							return details.m_highlight;
						}
#if DEVELOPMENT
						else
						{
							if (!String.IsNullOrEmpty(m_state.DevKey))
							{
								if (details.m_sortKey == m_state.DevKey)
								{
									return "#cb7bf0";
								}
							}
						}
#endif
					}
				}
			}
			return "#f07b05";
		}

		public override String GetTitlePageForProject(String project)
		{
			bool forceSteam = false;
			if (m_skuDetails != null)
			{
				foreach (SKUDetails details in m_skuDetails)
				{
					if (details.m_sku == project)
					{
						if (!String.IsNullOrEmpty(details.m_page))
						{
							return details.m_page;
						}
#if DEVELOPMENT
						else
						{
							if (!String.IsNullOrEmpty(m_state.DevKey))
							{
								if (details.m_sortKey != m_state.DevKey)
								{
									forceSteam = true;
								}
							}
						}
#endif
					}
				}
			}
			if ((m_state.m_manager.IsSteam) || forceSteam)
			{
				return LocalResources.Properties.Resources.RF_PageSteam;
			}
			return LocalResources.Properties.Resources.RF_Page;
		}

		public override String[] GetFiltersForProject(String project)
		{
			if (m_skuDetails != null)
			{
				foreach (SKUDetails details in m_skuDetails)
				{
					if (details.m_sku == project)
					{
#if DEVELOPMENT
						if (details.m_filters == null)
						{
							if (details.m_name.ToLower().Contains("horizons"))
							{
								//return new String[1] { "edh" };
								return null;
							}
							else
							{
								if (details.m_name.ToLower().Contains("dangerous"))
								{
									return new String[1] { "ed" };
								}
							}
						}
#endif
						return details.m_filters;
					}
				}
			}
			return null;
		}

        public override int GetMaxDownloadThreadsForProject(String project)
        {
            if (m_skuDetails != null)
            {
                foreach (SKUDetails details in m_skuDetails)
                {
                    if (details.m_sku == project)
                    {
                        return details.m_maxDownloadThreads;
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// Returns the projects Box Image URI
        /// </summary>
        /// <param name="project"></param>
        /// <returns>The projects box image URI, this can be null</returns>
        public override String GetBoxImageURIForProject( String _project )
        {
            string theBoxImageURI = null;

            if ( m_skuDetails != null )
            {
                bool foundTheImage = false;
                for( int idx = 0; idx < m_skuDetails.Length && !foundTheImage; idx++ )
                {
                    if ( m_skuDetails[idx].m_sku == _project )
                    {
                        theBoxImageURI = m_skuDetails[idx].m_box;
                        foundTheImage = true;
                    }
                }
            }
            return theBoxImageURI;
        }

        /// <summary>
        /// Returns the projects Hero Image URI
        /// </summary>
        /// <param name="project"></param>
        /// <returns>The projects hero image URI, this can be null</returns>
        public override String GetHeroImageURIForProject( String _project )
        {
            string theHeroImage = null;

            if ( m_skuDetails != null )
            {
                bool foundTheImage = false;
                for ( int idx = 0; idx < m_skuDetails.Length && !foundTheImage; idx++ )
                {
                    if ( m_skuDetails[idx].m_sku == _project )
                    {
                        theHeroImage = m_skuDetails[idx].m_hero;
                        foundTheImage = true;
                    }
                }
            }
            return theHeroImage;
        }

        /// <summary>
        /// Returns the project's no_details setting
        /// </summary>
        /// <param name="_project">The project to get the no_details setting for</param>
        /// <returns>The project's no_details setting, it is true by default</returns>
        public override bool GetNoDetailsForProject(String _project)
        {
            bool noDetails = true;

            if (m_skuDetails != null)
            {
                
                for (int idx = 0; idx < m_skuDetails.Length; idx++)
                {
                    if (m_skuDetails[idx].m_sku == _project)
                    {
                        noDetails = m_skuDetails[idx].m_noDetails;
                    }
                }
            }

            return noDetails;
        }

        /// <summary>
        /// Returns the projects logo Image URI
        /// </summary>
        /// <param name="project"></param>
        /// <returns>The projects logo image URI, this can be null</returns>
        public override String GetLogoImageURIForProject( String _project )
        {
            string theLogoImage = null;

            if ( m_skuDetails != null )
            {
                bool foundTheImage = false;
                for ( int idx = 0; idx < m_skuDetails.Length && !foundTheImage; idx++ )
                {
                    if ( m_skuDetails[idx].m_sku == _project )
                    {
                        theLogoImage = m_skuDetails[idx].m_logo;
                        foundTheImage = true;
                    }
                }
            }
            return theLogoImage;
        }

        /// <summary>
        /// Returns the projects ESRB Rating
        /// </summary>
        /// <param name="project"></param>
        /// <returns>The projects ESRB Rating, this can be null</returns>
        public override String GetESRBRatingForProject( String _project )
        {
            string theESRBRating = null;

            if ( m_skuDetails != null )
            {
                bool foundTheRating = false;
                for ( int idx = 0; idx < m_skuDetails.Length && !foundTheRating; idx++ )
                {
                    if ( m_skuDetails[idx].m_sku == _project )
                    {
                        theESRBRating = m_skuDetails[idx].m_esrbRating;
                        foundTheRating = true;
                    }
                }
            }
            return theESRBRating;
        }

        /// <summary>
        /// Returns the projects PEGI Rating
        /// </summary>
        /// <param name="project"></param>
        /// <returns>The projects PEGI Rating, this can be null</returns>
        public override String GetPEGIRatingForProject( String _project )
        {
            string thePEGIRathing = null;

            if ( m_skuDetails != null )
            {
                bool foundTheRating = false;
                for ( int idx = 0; idx < m_skuDetails.Length && !foundTheRating; idx++ )
                {
                    if ( m_skuDetails[idx].m_sku == _project )
                    {
                        thePEGIRathing = m_skuDetails[idx].m_pegiRating;
                        foundTheRating = true;
                    }
                }
            }
            return thePEGIRathing;
        }

        /// <summary>
        /// Gets the featured products as a json string
        /// </summary>
        /// <param name="_project">The project to get the information for</param>
        /// <returns>The featured products as a json string, this can be null</returns>
        public override string GetFeaturedProducts( Project _project )
        {
            Debug.Assert( _project != null );
            Debug.Assert( m_state != null );

            string jsonString = null;
            if ( _project != null && m_state != null )
            {
                if ( m_state.m_alive )
                {
                    Queries.FeaturedProductsQuery featuredProductsQuery = new Queries.FeaturedProductsQuery();
                    jsonString = featuredProductsQuery.Run( m_state, _project );
                }
            }

            return jsonString;
        }

        /// <summary>
        /// Gets the game description as a json string
        /// </summary>
        /// <param name="_project">The project to get the information for</param>
        /// <returns>The game description as a json string, this can be null</returns>
        public override string GetGameDescription( Project _project )
        {
            Debug.Assert( _project != null );
            Debug.Assert( m_state != null );

            string jsonString = null;
            if ( _project != null && m_state != null )
            {
                if ( m_state.m_alive )
                {
                    Queries.RequestGameDescriptionQuery requestGameDescriptionQuery = new Queries.RequestGameDescriptionQuery();
                    jsonString = requestGameDescriptionQuery.Run( m_state, _project );
                }
            }

            return jsonString;
        }

        /// <summary>
        /// Returns the current server status for the given project
        /// </summary>
        /// <param name="_project">The project to get the server status for</param>
        /// <param name="_serverStatusText">An out parameter, the server status text in the current language, can be null</param>
        /// <param name="_serverMessage">An out parameter, server state message in the current language, can be null</param>
        /// <returns>An ID representing the server state, -1 = NOT OKAY, 0 = Maintenance, 1 = OK</returns>
        public override int GetServerStatus( Project _project, out string _serverStatusText, out string _serverMessage )
        {
            int serverStatusResult = -1;

            Queries.ServerStatusQuery query = new Queries.ServerStatusQuery();
            serverStatusResult = query.Run( m_state, _project, out _serverStatusText, out _serverMessage );

            return serverStatusResult;
        }

        /// <summary>
        /// Returns the Project's Game API.
        /// </summary>
        /// <param name="project">The project to get the Game API for</param>
        /// <returns>The Project's Game API, this can be null</returns>
        public override String GetGameApiForProject( String _project )
        {
            string theGameApi = null;

            if ( m_skuDetails != null )
            {
                bool foundTheGameApi = false;
                for ( int idx = 0; idx < m_skuDetails.Length && !foundTheGameApi; idx++ )
                {
                    if ( m_skuDetails[idx].m_sku == _project )
                    {
                        theGameApi = m_skuDetails[idx].m_gameApi;
                        foundTheGameApi = true;
                    }
                }
            }
            return theGameApi;
        }

        /// <summary>
        /// Returns the projects game code (used in cms).
        /// </summary>
        /// <param name="project">The project to get the game code for</param>
        /// <returns>The projects game code, 0 means a game code was not retrieved</returns>
        public override int GetGameCodeForProject( String _project )
        {
            int theGameCode = 0;

            if (m_skuDetails != null )
            {
                bool foundTheGameCode = false;
                for (int idx = 0; idx<m_skuDetails.Length && !foundTheGameCode; idx++ )
                {
                    if (m_skuDetails[idx].m_sku == _project )
                    {
                        theGameCode = m_skuDetails[idx].m_gameCode;
                        foundTheGameCode = true;
                    }
                }
            }
            return theGameCode;
        }

        /// <summary>
        /// If we changed which server is being used due to the presence of a
        /// "UseInternalServer" file, then we want to let the started product
        /// know that it should be using the internal server too.
        /// 
        /// We do not actually/currently pass the address of the server, we
        /// assume that the product knows what to do in this case.
        /// </summary>
        /// <returns></returns>
        public override String ServerExtraRunArguments(Project project)
        {
            String serverArgs = "";
            String gameArgs = "";
            if (m_skuDetails!=null)
            {
                foreach (SKUDetails sku in m_skuDetails)
                {
                    if (project.Name == sku.m_sku)
                    {
                        if (sku.m_testAPI)
                        {
                            serverArgs = AddOption(serverArgs, "/Test");
                        }
                        if (!String.IsNullOrEmpty(sku.m_serverArgs))
                        {
                            serverArgs = AddOption(serverArgs, sku.m_serverArgs);
#if DEVELOPMENT
                            if (!m_state.m_manager.IsReleaseBuild && m_state.m_manager.UsePrivateServer && serverArgs.Contains("/Test"))
                            {
                                serverArgs = serverArgs.Replace("/Test", "/PrivateTest");
                            }
#endif // DEVELOPMENT
                        }
                        if (!String.IsNullOrEmpty(sku.m_gameArgs))
                        {
                            gameArgs = AddOption(gameArgs, sku.m_gameArgs);
                        }
                    }
                }
            }
            return serverArgs + " | " + gameArgs;
        }

        private String AddOption(String original, String option)
        {
            if (!String.IsNullOrEmpty(original))
            {
                return original + " " + option;
            }
            return option;
        }

        /// <summary>
        /// Return the content of the last response from the server.
        /// 
        /// This is typically used to present context relevant information to
        /// the user on failure or completion. For example a call to 
        /// GetAuthorisation may fail, after which GetLastContent may provide
        /// information as to why the attempt failed and what the user could
        /// try to fix it, such as where to look for a Two Factor
        /// authentication code set to the user via a different method.
        /// </summary>
        /// <returns>Text of the message.</returns>
        public override String GetLastContent()
        {
            return m_state.m_message;
        }

        /// <summary>
        /// Returns the last error code (-1 means no last error was recorded).
        /// Can be used by the UI to determine what has gone wrong (if anything)
        /// </summary>
        /// <returns>The last error code that was recorded</returns>
        public override int GetLastErrorCode()
        {
            return m_state.LastErrorCode;
        }

        public override void ResetLogin()
        {
            m_auth.ResetLogin();
        }

        public override void LogoutUser(UserDetails details)
        {
            m_auth.LogoutUser(details);
        }

        /// <summary>
        /// Logout the user and the machine from the server.
        /// </summary>
        /// <param name="details">The details of the user being logged out.</param>
        public override void LogoutMachine(UserDetails details)
        {
            m_auth.LogoutMachine(details);
        }

        /// <summary>
        /// Log the requested values.
        /// 
        /// Ultimately this will log to the server once a suitable endpoint is
        /// provided.
        /// 
        /// We use a local log file for testing, and I intend to leave it
        /// enabled so the user can see what information is being sent.
        /// Transparency and all that.
        /// </summary>
        /// <param name="details"></param>
        /// <param name="entry"></param>
        public override void LogValues(UserDetails details, LogEntry entry)
        {
            if (m_logger != null)
            {
                m_logger.Log(details, entry);
            }
        }

        /// <summary>
        /// Creates a new FD account
        /// </summary>
        /// <param name="_userDetails">The users details, used to create logs</param>
        /// <param name="_firstName">The users first name</param>
        /// <param name="_lastName">The users last name</param>
        /// <param name="_email">The users email</param>
        /// <param name="_password">The users password</param>
        /// <param name="_passwordConfirm">the users conformation password</param>
        /// <param name="_newsAndPromoSignUp">does the user signup for news and promotions</param>
        /// <returns>A JSONWebPostResult containing the result of the call</returns>
        public override JSONWebPutsAndPostsResult CreateFrontierAccount( UserDetails _userDetails,
                                                                         string _firstName,
                                                                         string _lastName,
                                                                         string _email,
                                                                         string _password,
                                                                         string _passwordConfirm,
                                                                         bool _newsAndPromoSignUp )
        {

            PutsAndPosts.CreateAccountPost post = new PutsAndPosts.CreateAccountPost( this, _userDetails );
            return post.Run( null, m_state, _firstName, _lastName, _email, _password, _passwordConfirm, _newsAndPromoSignUp );
        }

        /// <summary>
        /// Confirms a new FD account
        /// </summary>
        /// <param name="_userDetails">The users details, used to create logs</param>
        /// <param name="_email">The users email</param>
        /// <param name="_password">The users password</param>
        /// <param name="_otp">The otp code sent to the users email address</param>
        /// <param name="_machineId">The id of this machine</param>
        /// <returns>A JSONWebPostResult containing the result of the call</returns>
        public override JSONWebPutsAndPostsResult ConfirmFrontierAccount( UserDetails _userDetails, 
                                                                          string _email,
                                                                          string _password,
                                                                          string _otp,
                                                                          string _machineId )

        {

            PutsAndPosts.ConfirmFrontierAccountPut put = new PutsAndPosts.ConfirmFrontierAccountPut( this, _userDetails );
            return put.Run( null, m_state, _email, _password, _otp, _machineId );
        }

        /// <summary>
        /// Checks the password to see if it meets the password rules.
        /// </summary>
        /// <param name="_userDetails">The users details, used to create logs</param>
        /// <param name="_password">The users password</param>
        /// <returns>A JSONWebPostResult containing the result of the call</returns>
        public override JSONWebPutsAndPostsResult CheckPasswordComplexity( UserDetails _userDetails, 
                                                                           string _password )
        {

            PutsAndPosts.PasswordCheckPost post = new PutsAndPosts.PasswordCheckPost( this, _userDetails );
            return post.Run( m_state, _password);
        }

        /// <summary>
        /// Attempts to link the store account to the Frontier account. The store account may
        /// be one of any of the stores (Steam, Epic etc).
        /// </summary>
        /// <param name="_userDetails">The users details, used to create logs</param>
        /// <param name="_storeAuthorisation">The stores authorisations code</param>
        /// <param name="_storeClientId">The stores Client ID</param>
        /// <param name="_email">The users email</param>
        /// <param name="_password">The users password</param>
        /// <param name="_overRideAnyExistingLinks">Should existing links be ignored? if not then
        /// the call can produce a HttpStatusCode.PreconditionFailed, meaning linked to
        /// another account..</param>
        /// <returns>JSONWebPutsAndPostsResult containing the result of the call</returns>
        public override JSONWebPutsAndPostsResult LinkAccounts( UserDetails _userDetails,
                                                                string _storeAuthorisation,
                                                                string _storeClientId,
                                                                string _email,
                                                                string _password,
                                                                bool _overRideAnyExistingLinks )
        {
            JSONWebPutAndPost.LinkStoreAccountPost linkStoreAccountPost = JSONWebPutAndPost.LinkStoreAccountPost.Steam;
            if ( m_state.m_manager.IsSteam )
            {
                linkStoreAccountPost = JSONWebPutAndPost.LinkStoreAccountPost.Steam;
            }
            else if ( m_state.m_manager.IsEpic )
            {
                linkStoreAccountPost = JSONWebPutAndPost.LinkStoreAccountPost.Epic;
            }

            PutsAndPosts.LinkAccountPost post = new PutsAndPosts.LinkAccountPost( this, _userDetails );
            return post.Run( m_state, 
                            _storeAuthorisation, 
                            _storeClientId, 
                            _email, 
                            _password, 
                            linkStoreAccountPost, 
                            _overRideAnyExistingLinks );
        }

        /// <summary>
        /// Attempts to remove a link between a store and a Frontier account. The store account may
        /// be one of any of the stores (Steam, Epic etc).
        /// </summary>
        /// <param name="_userDetails">The users details, used to create logs</param>
        /// <param name="_storeAuthorisation">The stores authorisations code</param>
        /// <param name="_storeClientId">The stores client ID</param>
        /// <returns>JSONWebPutsAndPostsResult containing the result of the call</returns>
        public override JSONWebPutsAndPostsResult RemoveLinkAccounts( UserDetails _userDetails, 
                                                                      string _storeAuthorisation,
                                                                      string _storeClientId )
        {
            JSONWebPutAndPost.LinkStoreAccountPost linkStoreAccountPost = JSONWebPutAndPost.LinkStoreAccountPost.Steam;
            if ( m_state.m_manager.IsSteam )
            {
                linkStoreAccountPost = JSONWebPutAndPost.LinkStoreAccountPost.Steam;
            }
            else if ( m_state.m_manager.IsEpic )
            {
                linkStoreAccountPost = JSONWebPutAndPost.LinkStoreAccountPost.Epic;
            }

            DeleteQueries.DeleteLinkAccounts post = new DeleteQueries.DeleteLinkAccounts( this, _userDetails );
            return post.Run( m_state, _storeAuthorisation, _storeClientId, linkStoreAccountPost );
        }

        /// <summary>
        /// Redeems a game code for the current user and returns the result.
        /// </summary>
        /// <param name="_userDetails">The users details, used to create logs</param>
        /// <param name="_machine">The MachineIdentifierInterface</param>
        /// <param name="_code">The game code to redeem</param>
        /// <returns>JSONWebPutsAndPostsResult containing the result of the call, can be null</returns>
        public override JSONWebPutsAndPostsResult RedeemCode( UserDetails _userDetails, 
                                                              MachineIdentifierInterface _machine,
                                                              string _code )
        {
            Debug.Assert( _userDetails != null );
            Debug.Assert( _machine != null );
            Debug.Assert( !string.IsNullOrWhiteSpace( _code ) );

            JSONWebPutsAndPostsResult jSONWebPutsAndPostsResult = null;
            if ( _userDetails != null &&
                 _machine != null &&
                 !string.IsNullOrWhiteSpace( _code ) )
            {
                PutsAndPosts.RedeemCodePost post = new PutsAndPosts.RedeemCodePost( this, _userDetails );
                jSONWebPutsAndPostsResult = post.Run( m_state, _machine, _userDetails, _code );
            }

            return jSONWebPutsAndPostsResult;
        }
    }
}
