using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace ClientSupport
{
    /// <summary>
    /// The MockServerConnection implements the ServerInterface allowing client
    /// flow to be tested and demonstrated before a real server is available.
    /// </summary>
    public class MockServerConnection : ServerInterface
    {
        FORCManager m_manager;

        /// <summary>
        /// Representation of a collection of MockUser instances.
        /// 
        /// This class is loaded via the JSON deserialize method from a
        /// configuration file on disk so normal member naming conventions are
        /// not used.
        /// </summary>
        class MockUsers
        {
            /// <summary>
            /// Array of known users.
            /// </summary>
            public MockUser[] users;

            /// <summary>
            /// Requests that will fail. The request index is checked against
            /// the failing request link in order to simulate network failures
            /// during use.
            /// </summary>
            public int[] failingRequests;

            /// <summary>
            /// The number of entries in a failure cycle. With the default
            /// value of 0 a request will only fail if the request counter
            /// is found in the failingRequests array. If the value is non-zero
            /// then the current request counter modulo failureCycle is tested
            /// against the failingRequests array causing the failures to
            /// repeat.
            /// </summary>
            public int failureCycle;

            // Constructor.
            public MockUsers()
            {
                users = null;
                failingRequests = null;
                failureCycle = 0;
            }
        }

        /// <summary>
        /// A single mock user.
        /// 
        /// This class is loaded via the JSON deserialize method from a
        /// configuration file on disk so normal member naming conventions are
        /// not used.
        /// </summary>
        class MockUser
        {
            /// <summary>
            /// The email address associated with the user.
            /// </summary>
            public String email;

            /// <summary>
            /// The password associated with the email address.
            /// </summary>
            public String password;

            /// <summary>
            /// The response returned by the server if the properties of the
            /// user do not match.
            /// </summary>
            public String response;

            /// <summary>
            /// The required two factor code for authorisation, or null to 
            /// indicate no two factor code required.
            /// </summary>
            public String twoFactor;

            /// <summary>
            /// List of projects that the user has access to.
            /// </summary>
            public String[] projects;

            /// <summary>
            /// Constructor.
            /// </summary>
            public MockUser()
            {
                email = null;
                password = null;
                response = null;
                twoFactor = null;
                projects = null;
            }
        }

        /// <summary>
        /// The set of configured users for matching against for
        /// authentication.
        /// </summary>
        private MockUsers m_users;

        /// <summary>
        /// The authenticated user.
        /// </summary>
        private MockUser m_active;

        /// <summary>
        /// The last content returned from the server, or in this case whatever
        /// it was set to.
        /// </summary>
        private String m_lastContent;

        /// <summary>
        /// Directory used to store information about the MockServerConnection,
        /// such as the user configurations.
        /// </summary>
        private String m_connectionDir;

        /// <summary>
        /// Indicates that a file error has occurred.
        /// </summary>
        private bool m_fileError = false;

        /// <summary>
        /// The number of server requests that have been made.
        /// </summary>
        private int m_requests = 0;

        /// <summary>
        /// Indicates that a MockMachineIdentifier is required.
        /// </summary>
        public bool RequiresMockMachineIdentifier;

        private DownloadManagerMock m_fileStore;

        /// <summary>
        /// Constructor.
        /// 
        /// Set up the user and file information required by the mock server.
        /// 
        /// The connecting manager is required since it provides client
        /// relative path information that the mock server uses to find its
        /// files.
        /// </summary>
        /// <param name="manager">
        /// The client manager that will be connecting to the server.
        /// </param>
        public MockServerConnection(FORCManager manager)
        {
            m_manager = manager;
            RequiresMockMachineIdentifier = false;
            m_active = null;

            LoadUserData();
            PopulateFileStore();
        }

        /// <summary>
        /// Load the user database from the database.
        /// 
        /// The server URL from the manager is used to find the correct user
        /// data base to load.
        /// </summary>
        private void LoadUserData()
        {
            m_connectionDir = m_manager.GetLocalDirectory("MockConnections");

            if ((!String.IsNullOrEmpty(m_connectionDir)) && (!String.IsNullOrEmpty(m_manager.ServerURL)))
            {
                Uri url = new Uri(m_manager.ServerURL);
                String connectionInfo = Path.Combine(m_connectionDir, url.LocalPath);
                if (File.Exists(connectionInfo))
                {
                    String content = File.ReadAllText(connectionInfo);
                    if (!String.IsNullOrEmpty(m_connectionDir))
                    {
                        m_fileError = false;
                        try
                        {
                            JavaScriptSerializer js = new JavaScriptSerializer();
                            m_users = js.Deserialize<MockUsers>(content);
                            if (m_users.users == null)
                            {
                                m_lastContent = "User data contained no users.";
                                m_fileError = true;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            m_lastContent = "Invalid user data in file: " + ex.Message;
                            m_fileError = true;
                        }
                    }
                }
            }
            m_fileStore = new DownloadManagerMock(m_connectionDir);
        }

        public override DownloadManagerBase GetDownloadManager()
        {
            return m_fileStore;
        }

        /// <summary>
        /// Set up the installer files the mock server provides access to.
        /// </summary>
        public void PopulateFileStore()
        {
            m_fileStore.AddFiles();
            m_fileStore.ValidateFiles();
        }

        /// <summary>
        /// The mock server does not store previous state so it requires a user
        /// to log in every time it starts.
        /// </summary>
        /// <returns></returns>
        public override bool IsLoggedIn(UserDetails user)
        {
            return m_active != null;
        }

        /// <summary>
        /// The mock server does not do any validation on the time stamp so
        /// the returned string is never checked.
        /// </summary>
        /// <returns></returns>
        public override String GetTimeStamp()
        {
            return "Blob";
        }

        /// <summary>
        /// Return the current time adjusting for any offset between the
        /// local machine and the server.
        /// We do not care about the time stamp in the mock server so
        /// just return the standard time.
        /// </summary>
        /// <returns>Time string</returns>
        public override String GetCurrentTimeStamp()
        {
            return GetTimeStamp();
        }

        /// <summary>
        /// Return version status. Extend when required to test, for now
        /// always assume up to date.
        /// </summary>
        /// <param name="version"></param>
        /// <param name="current"></param>
        /// <returns></returns>
        public override VersionStatus CheckClientVersion(String version, out String current)
        {
            current = version;
            return ServerInterface.VersionStatus.Current;
        }

        /// <summary>
        /// Return the root path for the server. This may be useful for
        /// requests that are not made through the server interface for
        /// secondary features that could be provided by other servers.
        /// 
        /// The mock server does not support server paths since there is
        /// no real server. If we need files for testing we could use a file
        /// url, for now just bounce everything of the main server.
        /// </summary>
        /// <returns></returns>
        public override String GetServerPath(String key)
        {
            return "http://www.frontier.co.uk/" + key;
        }

        /// <summary>
        /// Test the passed user details to see if we have a corresponding
        /// user in our database, if so allow authentication.
        /// </summary>
        /// <param name="user">Submitted user details.</param>
        /// <param name="machine">Client machine identifier.</param>
        /// <param name="serverTime">Current client server time string.</param>
        /// <returns></returns>
        public override AuthorisationResult GetAuthorisation(UserDetails user, MachineIdentifierInterface machine, String serverTime)
        {
            String machineID = machine.GetMachineIdentifier();
            if (String.IsNullOrEmpty(machineID))
            {
                // If we do not have a machine identifier of some description
                // then we never authenticate.
                m_lastContent = "Unable to determine machine identifier. Launcher may not be correctly installed.";
                return AuthorisationResult.Failed;
            }
            m_requests++; // Processed another request.

            // Reload the user data so any local edits are picked up.
            LoadUserData();

            user.AuthenticationToken = null; // Not authorised unless given the all clear.
            user.SessionToken = null;
            if (!m_fileError)
            {
                // Only look up users if we loaded the user definitions without
                // any errors.
                if (m_users!=null)
                {
                    // Loaded user definitions, and have defined users so test
                    // to see if the current request should fail
                    if (m_users.failingRequests!=null)
                    {
                        int requests = m_requests;
                        if (m_users.failureCycle != 0)
                        {
                            requests = requests % m_users.failureCycle;
                        }
                        if (m_users.failingRequests.Contains(requests))
                        {
                            m_lastContent = "Server connection failed, please try again later.";
                            return AuthorisationResult.Failed;
                        }
                    }
                    if (m_users.users!=null)
                    {
                        // Do have users, so test them against the submitted
                        // user details to find a match.
                        foreach (MockUser m in m_users.users)
                        {
                            if ((m.email == user.EmailAddress) && (m.password == user.Password))
                            {
                                // Username and password match.
                                if ((m.twoFactor == null) || (m.twoFactor == user.TwoFactor))
                                {
                                    // Either this user does not require a two
                                    // factor authentication code, or a code is
                                    // required and the client supplied it
                                    // correctly.
                                    user.AuthenticationToken = m.response;
                                    user.SessionToken = m.response;
                                    // Store the current user to avoid
                                    // searching for it on subsequent requests.
                                    m_active = m;
                                    return AuthorisationResult.Authorised;
                                }
                                else
                                {
                                    // A two factor response is required but
                                    // was not submitted. Put the expected
                                    // value in the response since the mock
                                    // server cannot send a real email or SMS
                                    // and reject the request notifying the
                                    // client that they will need to provide
                                    // the additional details.
                                    m_lastContent = "A second factor authentication is required (" + m.twoFactor + ")";
                                    return AuthorisationResult.RequiresSecondFactor;
                                }
                            }
                        }
                        // No username/password match found, authentication
                        // failed.
                        m_lastContent = "Unrecognised username or password.";
                        return AuthorisationResult.Denied;
                    }
                }

                // No users to consider so give up.
                m_lastContent = "No users defined.";
                return AuthorisationResult.Denied;
            }
            return AuthorisationResult.Denied;
        }

		public override AuthorisationResult UseAuthorisation(UserDetails user, MachineIdentifierInterface machine, String serverTime, NameValueCollection external)
		{
			return AuthorisationResult.Denied;
		}

		/// <summary>
		/// Return the list of projects available to a given user.
		/// 
		/// Since the mock server only supports a single user if we have an
		/// active user return their projects, otherwise make one up.
		/// </summary>
		/// <param name="user">
		/// User for which the project list is being requested.
		/// </param>
		/// <param name="serverTime">
		/// The last requested time from the server.
		/// </param>
		/// <returns>Array of project name strings.</returns>
		public override String[] GetProjectList(UserDetails user, String serverTime)
        {
            if (m_active != null)
            {
                return m_active.projects;
            }
            String[] projects = new String[1];
            projects[0] = "Dummy";
            return projects;
        }

        public override String GetTitleForProject(String project)
        {
            return project;
        }

        public override String GetSortKeyForProject(String project)
        {
            return project;
        }

        public override String GetDirectoryForProject(String project)
        {
            return project;
        }

		public override String GetHighlightColourForProject(String project)
		{
			return null;
		}

		public override String GetTitlePageForProject(String project)
		{
			return null;
		}

		public override String[] GetFiltersForProject(String project)
		{
			return null;
		}

        public override String GetBoxImageURIForProject(String project)
        {
            return null;
        }

        public override String GetHeroImageURIForProject(String project)
        {
            return null;
        }

        public override int GetMaxDownloadThreadsForProject(string project)
        {
            return 0;
        }

        /// <summary>
        /// The mock server never needs extra run arguments, though we may
        /// want to add one for testing.
        /// </summary>
        /// <returns>null</returns>
        public override String ServerExtraRunArguments(Project project)
        {
            return null;
        }

        /// <summary>
        /// Return the last content text.
        /// 
        /// This will have been set by another function as required.
        /// </summary>
        /// <returns>The previously set string.</returns>
        public override String GetLastContent()
        {
            return m_lastContent;
        }

        public override void ResetLogin()
        {

        }

        public override void LogoutUser(UserDetails details)
        {
            details.SessionToken = null;
            details.TwoFactor = null;
        }

        /// <summary>
        /// Logout the user and the machine from the server.
        /// </summary>
        /// <param name="details">The details of the user being logged out.</param>
        public override void LogoutMachine(UserDetails details)
        {
            LogoutUser(details);
            details.AuthenticationToken = null;
        }

        public override void LogValues(UserDetails details, LogEntry entry)
        {

        }
    }
}
