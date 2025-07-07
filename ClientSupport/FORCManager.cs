using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

//using Epic.OnlineServices.Logging;
//using Epic.OnlineServices.Platform;
//using Epic.OnlineServices;
using LocalResources;
using SteamIF;
//using EosIF;

using System.Windows.Threading;
using System.Net;

namespace ClientSupport
{
    /// <summary>
    /// Data model for the Client which manages the various components.
    /// </summary>
    public class FORCManager
    {
        /// <summary>
        /// Constant values.
        /// </summary>
        private const String emailKey = "email";
        private const String passwordKey = "password";
        private const String urlKey = "URL";
        private const String binDir = "bin";
        private const String watchDog = "WatchDog.exe";
        private const String watchDog64 = "WatchDog64.exe";
        private const String hardwarereporter = "HardwareReporter.exe";
        private const String registrationTool = "Register.exe";
        private const String escapeTool = "Escape.exe";
        private const String c_applicationLanguage = "en";

        /// <summary>
        /// Root directory of the client. Everything relevant should be
        /// under here.
        /// </summary>
        private String m_directory;
        /// <summary>
        /// Directory containing the project specific folders.
        /// </summary>
        private String m_projectDirectory;
        /// <summary>
        /// Directory used for settings files.
        /// </summary>
        private String m_settingsDirectory;
        /// <summary>
        /// Path to the user settings file.
        /// </summary>
        private String m_userSettings;
        /// <summary>
        /// The full path to the start up executable.
        /// </summary>
        private String m_executable;
        /// <summary>
        /// Path to the watch dog executable that is used to monitor started
        /// projects for crashes.
        /// </summary>
        private String m_watchDogPath;
        private String m_watchDog64Path;
        private String m_hardwareReporterPath;
        private String m_registrationToolPath;
        //EpicCommandLineArgInfo m_epicCommandLineArgs;

        /// <summary>
        /// Used to force the FORCManager to ignore Epic switches when
        /// logging in. This forces the user to use a FD login only
        /// </summary>
        public bool IgnoreEpic { get; set; } = false;

        /// <summary>
        /// Language setting for the application that is passed to the started
        /// application.
        /// </summary>
        private String m_runLanguage;

        /// <summary>
        /// Details of the user whether logged in or attempting to log in.
        /// </summary>
        public UserDetails UserDetails = new UserDetails();
        private SteamInterface m_steamInterface = null;
        //private EosInterface m_eosInterface = null;

        /// <summary>
        /// The collection of projects available to the logged in user.
        /// </summary>
        public ProjectCollection AvailableProjects;

        /// <summary>
        /// Reference to an object that implements the server interface.
        /// 
        /// We expect the server to be set externally so an appropriate server
        /// implementation can be used depending on context. During testing
        /// for example we may want to use a server that does not actually use
        /// the network.
        /// </summary>
        private ServerInterface m_server;
        public ServerInterface ServerConnection
        {
            get { return m_server; }
            set
            {
                if (m_server != value)
                {
                    m_server = value;
                    ServerTime = m_server.GetTimeStamp();
                }
            }
        }

        /// <summary>
        /// Reference to an object that implements the
        /// MachineIdentifierInterface.
        /// 
        /// 
        /// </summary>
        private MachineIdentifierInterface m_machineIdentifier = null;
        public MachineIdentifierInterface MachineIdentifier
        {
            get { return m_machineIdentifier; }
            set { m_machineIdentifier = value; }
        }

        /// <summary>
        /// The URL used to identify the server in the configuration.
        /// </summary>
        public String ServerURL;

        /// <summary>
        /// The current status of the manager. This can be presented to the
        /// user to provide feedback on any issues that have occurred.
        /// </summary>
        public String Status;

        /// <summary>
        /// The time returned by the server. The server is expected to use this
        /// as a check when authenticating (and possibly at other times) to
        /// ensure that the attempt is recent. The client does not interpret
        /// the time it simply requests it from the server at start up and
        /// passes it back unchanged.
        /// </summary>
        private String ServerTime;
        public bool HasServer
        {
            get { return ServerTime != null; }
        }

        /// <summary>
        /// Indicates that the server has requested a Two Factor authentication
        /// process.
        /// 
        /// The server will have sent an additional code to the user via a
        /// separate channel such as an email or SMS to confirm the user is
        /// not using  someone elses credentials.
        /// </summary>
        public bool RequiresTwoFactor;

        /// <summary>
        /// A link to the users account page. This will be opened in an
        /// external browser window, no account options are available within
        /// the client application.
        /// </summary>
        public ExternalLink AccountLink = new ExternalLink();

        /// <summary>
        /// Indication that the client has received an authentication token
        /// from the server.
        /// </summary>
        public bool Authorised { get { return !String.IsNullOrEmpty(UserDetails.SessionToken); } }

        /// <summary>
        /// Is the application entry point a release build?
        /// </summary>
        private bool m_isReleaseBuild;
        public bool IsReleaseBuild
        {
            get
            {
                return m_isReleaseBuild;
            }
        }

#if DEVELOPMENT
        public bool UsePrivateServer = true;
#else
        public bool UsePrivateServer = false;
#endif // DEVELOPMENT



#if DEVELOPMENT
        public bool EnableGzipCompression = true;
#else
        public bool EnableGzipCompression = false;
#endif // DEVELOPMENT


        /// <summary>
        /// If true the AppData folder will never be searched for the Products
        /// directory.
        /// </summary>
        private bool m_isForceLocal;
        public bool IsForceLocal
        {
            get
            {
                return m_isForceLocal;
            }
        }

        private bool m_isEpic;

        public bool IsEpic
        {
            get
            {
                bool startedViaEpic = false;
                if ( !IgnoreEpic )
                {
                    startedViaEpic = m_isEpic;
                }
                return startedViaEpic;
            }
            private set
            {
                m_isEpic = value;
                Project.ExternalUpdate = m_isEpic;
            }
        }

        private bool m_isSteam;
        public bool IsSteam
		{
			get
			{
				return m_isSteam;
			}
			private set
			{
				m_isSteam = value;
				Project.ExternalUpdate = m_isSteam;
			}
		}

        public enum VRMode
        {
            Unspecified,
            Enabled,
            Disabled
        }
        private VRMode m_VRMode;
        public VRMode ActiveVRMode { get { return m_VRMode; } }

        public enum OculusMode
        {
            Disabled,
            Enabled,
            Implicit
        }
        private String m_oculusNonce = null;
        private OculusMode m_oculusActivation = OculusMode.Disabled;
        public OculusMode OculusActivation
        {
            get
            {
                return m_oculusActivation;
            }
        }
        public bool OculusEnabled
        {
            get
            {
                return m_oculusActivation != OculusMode.Disabled;
            }
        }
        public String OculusNonce
        {
            get
            {
                return m_oculusNonce;
            }
        }
        private String m_externalUser = null;
        public String ExternalUserName
        {
            get
            {
                return m_externalUser;
            }
        }

        private bool m_isAutoRun;
        public bool IsAutoRun { get { return m_isAutoRun; } }
        private bool m_isAutoQuit;
        public bool IsAutoQuit { get { return m_isAutoQuit; } }

        private String m_steamAppID = null;

        public bool FilteringEnabled
        {
            get
            {
                if (m_activeFilters == null)
                {
                    return false;
                }
                if (m_activeFilters.Length == 0)
                {
                    return false;
                }
                return true;
            }
        }
        private String[] m_activeFilters = null;

        /// <summary>
        /// File version string for the application entry point.
        /// 
        /// This may be modified to reflect an overridden version number for
        /// testing (developer builds only) or to mark the build as a developer
        /// build.
        /// </summary>
        private String m_fileVersion;
        public String ApplicationVersion
        {
            get
            {
                return m_fileVersion;
            }
        }

        /// <summary>
        /// The actual version is the version reported by the system, at least
        /// as the user is concerned.
        /// 
        /// This may be different from the version information stored in the
        /// file as it includes an indication that the build is a developer
        /// build vs a release build. It may be different from the
        /// ApplicationVersion value if the Application version has been
        /// modified for testing.
        /// </summary>
        private String m_actualVersion;
        public String ActualVersion
        {
            get
            {
                return m_actualVersion;
            }
        }

        /// <summary>
        /// The root directory in use if different from the default.
        /// </summary>
        private String m_rootDirectory;
        public String RootDirectory
        {
            get
            {
                return m_rootDirectory;
            }
        }

        public String ProjectDirectory
        {
            get
            {
                return m_projectDirectory;
            }
        }

        public static bool DetailedUpdateLog
        {
            get
            {
                return Properties.Settings.Default.DetailedUpdateLog;
            }
            set
            {
                if (value != Properties.Settings.Default.DetailedUpdateLog)
                {
                    Properties.Settings.Default.DetailedUpdateLog = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private bool m_hasRunHardwareSurvey = false;

        private String m_hardwareSurveyLocation;
        public String HardwareSurveyLocation
        {
            get
            {
                return m_hardwareSurveyLocation;
            }
            private set
            {
                m_hardwareSurveyLocation = value;
            }
        }

        public String AppDataFolder
        {
            get
            {
                String lad = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                lad = Path.Combine(lad, "Frontier_Developments");
                return lad;
            }
        }

        /// <summary>
        /// Constructor. Set up the initial client state.
        /// </summary>
        public FORCManager()
        {
            if (Properties.Settings.Default.UpgradeSettings)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeSettings = false;
                // Comment out the following line to force the hardware survey
                // to be performed each time a new launcher is installed.
                // Generally only required for launcher releases corresponding
                // to major game updates.
                //Properties.Settings.Default.HardwareSurveyPerformed = false;
                Properties.Settings.Default.Save();
            }
        }

        public void Initialise(DirectorySelector selector)
        {
            //m_eosInterface = new EosInterface();
            ProcessCommandLine();
            SetUpPaths(selector);
            UpdateVersionInfo();
            LoadUserProperties();
            LoadServerProperties();
            LoadSteamProperties();
            SetTlsProtocol(SecurityProtocolType.Tls12);
            //SetupTimer();
            AvailableProjects = new ProjectCollection(m_projectDirectory);
        }

        private DispatcherTimer m_DispatcherTimer;
        private TimeSpan m_DispatcherTimerInterval;

 /*       private void SetupTimer()
        {
            if (IsEpic)
            {
                m_DispatcherTimerInterval = new TimeSpan(0, 0, 0, 0, 100);

                m_DispatcherTimer = new DispatcherTimer();
                m_DispatcherTimer.Tick += EpicTick;
                m_DispatcherTimer.Interval = m_DispatcherTimerInterval;
            }
        }

        public bool IsEpicReady()
        {
            if (IsEpic)
            {
                bool hasAuthToken = m_eosInterface.HasAuthToken();

                if (!hasAuthToken)
                {
                    Console.WriteLine("Need to wait for the Epic auth token before logging in");
                }
                return hasAuthToken;
            } 
              return true;
            
        }
        public void InitialiseEpic()
        {
            if (!IsEpic)
            {
                Console.WriteLine("Not Epic, not initialising epic!");
                LogEntry entry = new LogEntry("Not initialising Epic");
                Log(entry);
            }
            else
            {

                if ((m_epicCommandLineArgs.m_epicAuthTokenName != null && m_epicCommandLineArgs.m_epicAuthPort != null)
                    || (m_epicCommandLineArgs.m_epicAuthType != null && m_epicCommandLineArgs.m_epicAuthPassword != null)
                    || (m_epicCommandLineArgs.m_epicRefreshToken != null))
                {
                    LogEntry entry = new LogEntry("Initialising Epic");
                    m_eosInterface.Initialise(m_epicCommandLineArgs);

                    m_DispatcherTimer.Start();
                } else
                {
                    Log(new LogEntry("No Epic auth token selected, or alternative auth method"));

                }
            }
            
        }

        private bool m_loggedEpicAccessTokenLoss = false;
        private int m_tokenChecksAtLastLog = 0;
        private void EpicTick(object sender, EventArgs e)
        {
            m_eosInterface.Tick();
            if (m_eosInterface.HasAuthToken())
            {
                Dictionary<String,String> epicTokenChecks = m_eosInterface.CheckAuthToken();
                if ((epicTokenChecks.Count > 0) && (epicTokenChecks.Count != m_tokenChecksAtLastLog))
                {
                    LogEntry le = new LogEntry("EpicTokenChecks");
                    foreach (String key in epicTokenChecks.Keys)
                    {
                        le.AddValue(key, epicTokenChecks[key]);
                    }
                    Log(le);
                }
                m_tokenChecksAtLastLog = epicTokenChecks.Count;
                m_loggedEpicAccessTokenLoss = false;
                UserDetails.EpicClientId = m_eosInterface.GetClientId();
                UserDetails.EpicAccessToken = m_eosInterface.GetAccessToken();
            } else if (m_eosInterface.IsLoggedIn() && !m_loggedEpicAccessTokenLoss)
            {
                UserDetails.EpicAccessToken = m_eosInterface.GetAccessToken();
                Log(new LogEntry("No Epic Access Token, but we are logged in. We must have lost it."));
                m_loggedEpicAccessTokenLoss = true;
            }

        }*/

        /// <summary>
        /// Returns the product server URL to use to allow users to
        /// purchase items.
        /// </summary>
        /// <returns>The product server URL, this can be null</returns>
        public string GetProductServer()
        {
            string productServer = null;
            if ( m_server != null )
            {
                productServer = m_server.GetProductServer();
            }

            return productServer;
        }

        private String ExtractArgValue(String commandLineArg)
        {
            String result = "";
            String[] argAndValue = commandLineArg.Split('=');
            if (argAndValue.Length > 1)
            {
                result = argAndValue[1];
            }
            return result;

        }
        /// <summary>
        /// Process command line options of interest.
        /// </summary>
        private void ProcessCommandLine()
        {
            m_isForceLocal = false;
            IsSteam = false;
            m_VRMode = VRMode.Unspecified;
            m_isAutoRun = false;
            m_isAutoQuit = false;
            HashSet<String> activeFilters = new HashSet<String>();
            if (m_activeFilters != null)
            {
                foreach (String existingFilter in m_activeFilters)
                {
                    activeFilters.Add(existingFilter);
                }

            }

            String[] argv = Environment.GetCommandLineArgs();
            for (int an = 0; an < argv.Length; ++an )
            {
                String arg = argv[an];
                String lc = arg.ToLowerInvariant();
                if ((lc == "/steam") || (lc == "/epic") || (lc == "/forcelocal") ||
                    (lc=="/steamid") || (lc == "/oculus"))
                {
                    m_isForceLocal = true;
                }
                if ((lc == "/steam") || (lc=="/steamid"))
                {
                    IsSteam = true;
                }
 /*               if ((lc == "/epic") || (lc.StartsWith("-epicenv")) && !IgnoreEpic )
                {
                    IsEpic = true;
                }
                if (lc == "/epicrefreshtoken")
                {
                    m_epicCommandLineArgs.m_epicRefreshToken = argv[an + 1];
                }
                if (lc == "/logepicinfo")
                {
                    m_epicCommandLineArgs.m_writeToLog = true;
                }
                if (lc.StartsWith("-epicenv") )
                {
                    m_epicCommandLineArgs.m_epicEnvironment = ExtractArgValue(arg);
                }
                if (lc.StartsWith("-epiclocale"))
                {
                    m_epicCommandLineArgs.m_epicLocale = ExtractArgValue(arg);
                }
                if (lc.StartsWith("-epicuserid"))
                {
                    m_epicCommandLineArgs.m_epicUserId = ExtractArgValue(arg);
                }
                if (lc.StartsWith("-auth_type"))
                {
                    m_epicCommandLineArgs.m_epicAuthType = ExtractArgValue(arg);
                }
                if (lc.StartsWith("-auth_password"))
                {
                    m_epicCommandLineArgs.m_epicAuthPassword = ExtractArgValue(arg);
                }
                if (lc == "/epicauthport")
                {
                    m_epicCommandLineArgs.m_epicAuthPort = argv[an + 1];
                }
                if (lc == "/epictokenname")
                {
                    m_epicCommandLineArgs.m_epicAuthTokenName = argv[an + 1];
                }*/
                if (lc == "/oculus")
                {
                    if ((an + 1) < argv.Length)
                    {
                        m_oculusNonce = argv[an + 1];
                    }
                    m_oculusActivation = OculusMode.Enabled;
                }
                if (lc == "/user")
                {
                    if ((an + 1) < argv.Length)
                    {
                        m_externalUser = argv[an + 1];
                    }
                }
                if (lc == "/vr")
                {
                    m_VRMode = VRMode.Enabled;
                    m_isAutoRun = true;
                }
                if (lc == "/novr")
                {
                    m_VRMode = VRMode.Disabled;
                }
                if (lc == "/autorun")
                {
                    m_isAutoRun = true;
                }
                if (lc == "/autoquit")
                {
                    m_isAutoQuit = true;
                }
                if (lc == "/ed")
                {
                    activeFilters.Add("ed");
                }
                if (lc == "/edh")
                {
                    activeFilters.Add("edh");
                }
                if (lc == "/eda")
                {
                    activeFilters.Add("eda");
                }
                if ((lc == "/filters") || (lc=="/filter"))
                {
                    if ((an + 1) < argv.Length)
                    {
                        activeFilters.Add(argv[an + 1]);
                    }
                }
                if (lc == "/steamid")
                {
                    if ((an + 1) < argv.Length)
                    {
                        m_steamAppID = argv[an + 1];
                    }
                }
            }
            m_activeFilters = activeFilters.ToArray();
        }

        /// <summary>
        /// Set up the standard paths required.
        /// </summary>
        private void SetUpPaths(DirectorySelector selector)
        {
            const String projectNode = "Products";
            // Allow a local project directory in case a user wants multiple
            // instances, e.g. for using local settings as well as a standard
            // install.
            String cwd = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            // Cannot actually select a folder since doing so seems to break
            // the news feed display in strange ways. Probably need to decouple
            // the product location selection from construction. Longer term
            // this would be beneficial anyway, but is a fairly major change.
            //String selection = selector.SelectDirectory(cwd);

            m_directory = null;
            if (IsForceLocal)
            {
                m_directory = cwd;
            }
            else
            {
                String local = Path.Combine(cwd, projectNode);
                if (Directory.Exists(local))
                {
                    try
                    {
                        // Only use the local folder if it is writable to catch
                        // cases where we had a previous installation using a
                        // local folder.
                        String writable = "writable.txt";
                        File.WriteAllText(writable, "");
                        File.Delete(writable);
                        m_directory = cwd;
                    }
                    catch (UnauthorizedAccessException)
                    {

                    }
                }
                if (m_directory == null)
                {
                    String ladp = Path.Combine(AppDataFolder, projectNode);

                    bool existingladp = false;
                    if (Directory.Exists(ladp))
                    {
                        if (Directory.GetDirectories(ladp).Length > 0)
                        {
                            // Only use the local app data folder if it already
                            // has contents. That way if you uninstall and restart
                            // the launcher it will try using the local folder
                            // first
                            existingladp = true;
                        }
                    }
                    if (!existingladp)
                    {
                        try
                        {
                            Directory.CreateDirectory(local);
                            m_directory = cwd;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            Directory.CreateDirectory(AppDataFolder);
                            m_directory = AppDataFolder;
                        }
                    }
                    else
                    {
                        m_directory = AppDataFolder;
                    }
                }
            }
            m_projectDirectory = Path.Combine(m_directory, projectNode);
            if (!Directory.Exists(m_projectDirectory))
            {
                Directory.CreateDirectory(m_projectDirectory);
            }
            m_settingsDirectory = Path.Combine(m_directory,  "Settings");
            m_userSettings = Path.Combine(m_settingsDirectory, "User.cfg");

            Assembly asm = Assembly.GetEntryAssembly();
            m_executable = asm.Location;
            String executablePath = Path.GetDirectoryName(m_executable);
            m_watchDogPath = FindExecutable(executablePath, watchDog);
            m_watchDog64Path = FindExecutable(executablePath, watchDog64);
            m_hardwareReporterPath = FindExecutable(executablePath, hardwarereporter);
            m_registrationToolPath = FindExecutable(executablePath, registrationTool);
            // m_escapeToolPath = FindExecutable(executablePath, escapeTool);
            m_rootDirectory = null; // Not currently used

            HardwareSurveyLocation = Path.Combine(AppDataFolder, "specs.xml");

            if (OculusActivation == OculusMode.Disabled)
            {
                String oridpath = FindExecutable(executablePath, "ORID.EXE");
                if (oridpath != null)
                {
                    m_oculusActivation = OculusMode.Implicit;
                }
            }
        }

        private String FindExecutable(String executablePath, String executable)
        {
            String watchDogPath = Path.Combine(executablePath, executable);
            if (File.Exists(watchDogPath))
            {
                return watchDogPath;
            }
            else
            {
                String tempDirectory = Path.Combine(executablePath, binDir);
                watchDogPath = Path.Combine(tempDirectory, executable);
                if (File.Exists(watchDogPath))
                {
                    return watchDogPath;
                }
            }
            return null;
        }

        private void UpdateVersionInfo()
        {
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(m_executable);
#if DEVELOPMENT
            m_isReleaseBuild = false;
            m_fileVersion = String.Format("{0}.{1}.{2}.{3}", fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, fvi.FilePrivatePart == 0 ? 200 : fvi.FilePrivatePart);
#else
            m_isReleaseBuild = (fvi.FilePrivatePart == 0);
            m_fileVersion = fvi.FileVersion;
#endif
            // Set the actual version to the current file version. If the
            // version has been modified because we are in a development build
            // users will see the modified version not the real version.
            m_actualVersion = m_fileVersion;

            if (!IsReleaseBuild)
            {
                String alternateVersionFile = Path.Combine(m_settingsDirectory,"ReportVersion.txt");
                if (System.IO.File.Exists(alternateVersionFile))
                {
                    try
                    {
                        String content = System.IO.File.ReadAllText(alternateVersionFile);
                        content = content.Trim();
                        String[] elements = content.Split('.');
                        if (elements.Length == 4)
                        {
                            bool succeeded = true; ;
                            int final = 0;
                            for (int e = 0; e < elements.Length; ++e)
                            {

                                if (!int.TryParse(elements[e], out final))
                                {
                                    succeeded = false;
                                }
                            }
                            if (succeeded)
                            {
                                m_fileVersion = content;

                                // Update the version, but do not reset the
                                // IsReleaseBuild value since other things
                                // may also be depending on it for development.
                            }
                        }
                    }
                    catch (System.Exception)
                    {

                    }
                }
            }

        }

        private void LoadGameOptions( out String launchOptions, out String targetOptions)
        {
            launchOptions = "";
            targetOptions = "";

            String gameOptionFile = Path.Combine(m_settingsDirectory,"GameOptions.txt");
            if (System.IO.File.Exists(gameOptionFile))
            {
                try
                {
                    String[] content = System.IO.File.ReadAllLines(gameOptionFile);
                    char[] delim = { ':' };

                    foreach (String line in content)
                    {
                        String text = line.Trim();
                        if (!String.IsNullOrEmpty(text))
                        {
                            String[] elements = text.Split(delim, 2);
                            bool used = false;
                            if (elements.Length == 2)
                            {
                                String name = elements[0].Trim().ToLowerInvariant();
                                if (name == "launcher")
                                {
                                    launchOptions = launchOptions + " " + elements[1];
                                    used = true;
                                }
                                else
                                {
                                    if (name == "game")
                                    {
                                        targetOptions = targetOptions + " " + elements[1];
                                        used = true;
                                    }
                                }
                            }
                            if (!used)
                            {
                                targetOptions = targetOptions + " " + text;
                            }
                        }
                    }
                }
                catch (System.Exception)
                {

                }
            }
        }

        /// <summary>
        /// Load the user settings. Currently this is the username and
        /// password. In plain text.
        /// </summary>
        private void LoadUserProperties()
        {
            DecoderRing ring = new DecoderRing();
            String un = Properties.Settings.Default.UserName;
            if (!String.IsNullOrEmpty(un))
            {
                UserDetails.EmailAddress = un;
            }
            else
            {
                UserDetails.EmailAddress = null;
            }
            String pwd = Properties.Settings.Default.Password;
            if (!String.IsNullOrEmpty(pwd))
            {
                pwd = ring.Decode(pwd);
            }
            if (!String.IsNullOrEmpty(pwd))
            {
                UserDetails.Password = pwd;
            }
            else
            {
                UserDetails.Password = null;
            }
        }

  /*      public void WaitForInit()
        {
            if(IsEpic && m_eosInterface != null)
            {
                const int c_waitInterval = 100; // times in milliseconds
                const int c_maxWaitTime = 10000;
                int timeWaited = 0;

                while (!m_eosInterface.HasAuthToken() && timeWaited < c_maxWaitTime)
                {
                    m_eosInterface.Tick();
                    System.Threading.Thread.Sleep(c_waitInterval);
                    timeWaited += c_waitInterval;
                }
                if (m_eosInterface.HasAuthToken())
                {
                    LogEntry le = new LogEntry("EpicAuthReceived");
                    le.AddValue("TimeElapsed", timeWaited);
                    Log(le);
                }
            }
        }

        private void WaitForEpicLogin()
        {
            const int c_waitInterval = 100; // times in milliseconds
            const int c_maxWaitTime = 15 * 60 * 1000; // 15 minutes then fall back and just keep going
            int timeWaited = 0;

            while (m_eosInterface.m_loginInProgress && timeWaited < c_maxWaitTime)
            {
                m_eosInterface.Tick();
                System.Threading.Thread.Sleep(c_waitInterval);
                timeWaited += c_waitInterval;
            }
            if (m_eosInterface.HasAuthToken())
            {
                LogEntry le = new LogEntry("EpicAuthReceived");
                le.AddValue("TimeElapsed", timeWaited);
                Log(le);
            }
        }*/

        /// <summary>
        /// Attempt to log the user in if the necessary details have already
        /// been saved.
        /// 
        /// If the attempt fails reset the login status so when the dialog
        /// is opened we are in a known state.
        /// </summary>
        public void AutoLogin()
        {
            if (m_steamInterface != null)
            {
                m_steamInterface.Refresh();
                UserDetails.SteamID = m_steamInterface.UserID;
                UserDetails.SteamSessionToken = m_steamInterface.SessionToken;
            }
            /*if (m_isEpic && m_eosInterface != null)
            {
                if (m_eosInterface.m_loginInProgress)
                {
                    WaitForEpicLogin();
                }
                UserDetails.EpicAccessToken = m_eosInterface.GetAccessToken();
                UserDetails.EpicClientId = m_eosInterface.GetClientId();
            }*/

            if ( ((m_steamInterface==null) || (!m_steamInterface.Connected)) && ! IsEpic)
            {
                if (UserDetails.EmailAddress == null)
                {
                    return;
                }
                if (UserDetails.Password == null)
                {
                    return;
                }
            }

            Authenticate(true);
            if (!Authorised)
            {
                ResetLogin(true, true);
            }
        }

        /// <summary>
        /// Test whether the user can log in through Steam directly.
        /// </summary>
        /// <returns></returns>
        public bool AllowSteamLogin()
        {
            if (!IsSteam)
            {
                return false;
            }
            if (UserDetails.SteamID==0)
            {
                return false;
            }
            if ((UserDetails.SteamRegistrationLink!=null) ||
                (UserDetails.SteamLinkLink!=null))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Use (and possibly validate) externally given authentication tokens.
        /// </summary>
        /// <param name="parameters"></param>
        public void ExternalAuthorisation(NameValueCollection parameters)
        {
            ServerConnection.UseAuthorisation(UserDetails, MachineIdentifier, ServerTime, parameters);
            if (!Authorised)
            {
                ResetLogin(true, true);
            }
        }
        /// <summary>
        /// Get a path to an existing directory relative to the client root.
        /// </summary>
        /// <param name="directory"></param>
        /// <returns>
        /// The path to an existing directory, or null if the directory does
        /// not exist.
        /// </returns>
        public String GetLocalDirectory(String directory, bool mustexist = true)
        {

            String result = Path.Combine(m_directory, directory);
            if ((!mustexist) || (Directory.Exists(result)) )
            {
                return result;
            }
            return null;
        }

        /// <summary>
        /// Internal save of the properties
        /// </summary>
        private void SaveUserProperties(bool withPassword)
        {
            Properties.Settings.Default.UserName = UserDetails.EmailAddress;
            if (withPassword)
            {
                DecoderRing ring = new DecoderRing();
                Properties.Settings.Default.Password = ring.Encode(UserDetails.Password);
            }
            else
            {
                Properties.Settings.Default.Password = null;
            }
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Save the current user properties to the file ready for loading on
        /// the next client start.
        /// </summary>
        public void SaveUserDetails(bool withPassword)
        {
            SaveUserProperties(withPassword);
        }

        /// <summary>
        /// Remove any existing user properties so the user must enter them
        /// on the next load. Note that this will not remove any authentication
        /// tokens, or the current in memory versions
        /// </summary>
        public void ClearUserDetails()
        {
            try
            {
                Properties.Settings.Default.UserName = null;
                Properties.Settings.Default.Password = null;
                Properties.Settings.Default.Save();
            }
            catch (System.Exception)
            {
            }
        }

        /// <summary>
        /// Reset the login condition to a known state.
        ///
        /// If we are not remembering the user, scrub the email address
        /// password and any left over two factor code. We retain any tokens
        /// since unless the user has explicitly requested a log out from the
        /// machine we expect those to remain valid.
        /// 
        /// If the client has been restarted the values would have been
        /// forgotten anyway, but this ensures they are cleared within the
        /// client if they explicitly log out, and then log back in again,
        /// as a different user say.
        /// 
        /// Similarly even if we are remembering the user, if we are not also
        /// remembering the password we clear the password here.
        /// </summary>
        public void ResetLogin(bool rememberUser, bool rememberPassword)
        {
            RequiresTwoFactor = false;
            Status = null;
            if (!rememberUser)
            {
                UserDetails.EmailAddress = null;
                UserDetails.Password = null;
                UserDetails.TwoFactor = null;
            }
            else
            {
                if (!rememberPassword)
                {
                    UserDetails.Password = null;
                    UserDetails.TwoFactor = null;
                }
            }
            if (ServerConnection != null)
            {
                ServerConnection.ResetLogin();
            }
        }

        /// <summary>
        /// Load the server configuration.
        /// 
        /// Currently this is only the URL to use to communicate with the
        /// server and is used for triggering a different servers for testing.
        /// 
        /// Only allow the server to be changed on development builds.
        /// </summary>
        private void LoadServerProperties()
        {
            RequiresTwoFactor = false; // Initially assume no two factor
            // required the server will tell us later if it is.

            if (!IsReleaseBuild)
            {
                String serverPropertiesFile = Path.Combine(m_settingsDirectory, "Server.cfg");
                Dictionary<String, object> dict = LoadJSON(serverPropertiesFile);
                if (dict == null)
                {
                    // Use the default server.
                }
                else
                {
                    if (dict.ContainsKey(urlKey))
                    {
                        ServerURL = dict[urlKey] as String;
                    }
                }
            }
        }

        /// <summary>
        /// Connect to the Steam Client if available.
        /// </summary>
        private void LoadSteamProperties()
        {
            if (IsSteam)
            {
                m_steamInterface = new SteamInterface();
                m_steamInterface.Initialise();
            }

        }

        /// <summary>
        /// Load a file containing JSON formatted data in the form of a
        /// dictionary of strings to objects allowing nested structures which
        /// the caller is responsible for extracting/checking.
        /// </summary>
        /// <param name="path">
        /// Path of an existing file containing a JSON format file.
        /// </param>
        /// <returns>Dictionary of named objects.</returns>
        public Dictionary<String, object> LoadJSON(String path)
        {
            Dictionary<String, object> dict = null;
            if (File.Exists(path))
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    // TODO: Consider security implications of storing login
                    // details in plain text, even if an attacker gets them
                    // they would need access to the victims email, quite
                    // possible if they have access to the physical machine.
                    String content = sr.ReadToEnd();
                    JavaScriptSerializer json = new JavaScriptSerializer();
                    dict = json.Deserialize<Dictionary<String, object>>(content);
                }
            }
            return dict;
        }

        /// <summary>
        /// Authenticate with the configured server using the current user
        /// details.
        /// </summary>
        public void Authenticate(bool automatic)
        {
            if (MachineIdentifier == null)
            {
                Status = LocalResources.Properties.Resources.FORCManager_UnableToDetermineMachineID;
            }
            if (ServerConnection == null)
            {
                Status = LocalResources.Properties.Resources.FORCManager_NoServerStatus;
            }

            UserDetails.Automatic = automatic;
            Console.WriteLine("Automatic value: " + automatic);
            ServerInterface.AuthorisationResult result = ServerConnection.GetAuthorisation(UserDetails, MachineIdentifier, ServerTime);

            RequiresTwoFactor = false;
            switch (result)
            {
                case ServerInterface.AuthorisationResult.Authorised:
                    {
                        LogEntry logAuth = new LogEntry("Authenticated");
                        logAuth.AddValue("user", UserDetails.DisplayName);
                        Log(logAuth);
                        if (UserDetails.AuthenticationType==ServerInterface.AuthenticationType.FORC)
                        {
                            AccountLink.URL = "http://www.frontier.co.uk/account?token=" + UserDetails.AuthenticationToken;
                        }
                        ConsiderHardwareSurvey();
                        break;
                    }
                case ServerInterface.AuthorisationResult.Failed:
                case ServerInterface.AuthorisationResult.Denied:
                    {
                        LogEntry logAuth = new LogEntry("AuthenticationRejected");
                        logAuth.AddValue("user", UserDetails.EmailAddress);
                        Log(logAuth);
                        Status = ServerConnection.GetLastContent();
                        break;
                    }
                case ServerInterface.AuthorisationResult.RequiresSecondFactor:
                    {
                        LogEntry logAuth = new LogEntry("TwoFactorAuthenticationRequested");
                        logAuth.AddValue("user", UserDetails.EmailAddress);
                        Log(logAuth);
                        RequiresTwoFactor = true;
                        Status = ServerConnection.GetLastContent();
                        break;
                    }
            }
        }

        /// <summary>
        /// Consider whether it is necessary to perform a hardware survey.
        /// 
        /// For now the logic is hardwired to trigger whenever no survey
        /// appears to have been done, but the request should probably go via
        /// the server for a more flexible solution.
        /// </summary>
        public void ConsiderHardwareSurvey()
        {
            if (IsEpic)
            {
                return;
            }
            if (Properties.Settings.Default.HardwareSurveyPerformed)
            {
                if ((IsSteam) || (OculusEnabled))
                {
                    // On steam so we never update the game ourselves so
                    // cannot rely on that to update our specs file.
                    if (!m_hasRunHardwareSurvey)
                    {
                        RunHardwareSurvey(false, false);
                    }
                }
                return;
            }

            try
            {
                // assume its local
                RunHardwareSurvey(true, false);

                Properties.Settings.Default.HardwareSurveyPerformed = true;
                Properties.Settings.Default.Save();
            }
            catch (Exception)
            {
            }

        }

        /// <summary>
        /// Low level method to actually run the hardware survey tool which is
        /// in a different executable.
        /// </summary>
        /// <param name="allowUpload">
        /// If true the external application will be provided with server
        /// details so it can upload to the server if wanted. Otherwise it is
        /// expected to create the report on disk.
        /// </param>
        /// <param name="waitForFinish">
        /// If true wait for the external tool to exit before returning, if
        /// false return immediately and let the tool manage itself.
        /// </param>
        public void RunHardwareSurvey(bool allowUpload, bool waitForFinish)
        {
            m_hasRunHardwareSurvey = true;

            Log(new LogEntry("CollectHardwareSurvey"));
            String arguments = "";
            if (allowUpload)
            {
                arguments = String.Format("/MachineToken {0} /Version {1} /AuthToken {2} /MachineId {3} /Time {4}",
                    UserDetails.AuthenticationToken, "1.0.0.0", UserDetails.SessionToken, MachineIdentifier.GetMachineIdentifier(),
                    ServerConnection.GetCurrentTimeStamp());
            }
            else
            {
                if (!waitForFinish)
                {
                    arguments = "/Silent";
                }
            }
            arguments += " /Target \"" + Path.GetDirectoryName(HardwareSurveyLocation) +"\"";
            try
            {
                String HR = m_hardwareReporterPath;
                if (File.Exists(HR))
                {
                    Process pid = Process.Start(HR, arguments);
                    if (waitForFinish)
                    {
                        pid.WaitForExit();
                    }
                }
            }
            catch (System.Exception)
            {
                
            }
        }

        /// <summary>
        /// Return the hardware description as a text string.
        /// 
        /// The external tool is run to update the description then the
        /// generated file loaded and returned as a string. If the process
        /// fails at any point the string contains the error report. It is
        /// assumed that the user it not in a position to fix any errors
        /// reported here so it is more useful to have the failure reported
        /// back to base for analysis.
        /// </summary>
        /// <returns>Textual hardware description, or error string.</returns>
        public String GetHardwareDescription()
        {
            try
            {
                RunHardwareSurvey(false, true);
                String descName = HardwareSurveyLocation;
                if (!File.Exists(descName))
                {
                    return String.Format(LocalResources.Properties.Resources.FORCManager_HardwareSurveyMissing, descName);
                }
                try
                {
                    return File.ReadAllText(descName);
                }
                catch (System.Exception ex)
                {
                    return String.Format(LocalResources.Properties.Resources.FORCManager_HardwareSurveyReadFail,
                        descName, ex.Message);
                }
            }
            catch (System.Exception ex)
            {
                return String.Format(LocalResources.Properties.Resources.FORCManager_HardwareSurveyCollectionFail,
                    ex.Message);
            }
        }

        private String[] m_projectList = null;
        /// <summary>
        /// Update the list of projects that the user has access to.
        /// 
        /// This retrieves the full list from the server and uses it to compare
        /// with the installed projects to identify which ones require updating
        /// or installation before they can be played.
        /// </summary>
        public void UpdateProjectList(bool cached = false)
        {
            if ((HasServer && Authorised) && ((!cached) || (m_projectList==null)))
            {
                m_projectList = ServerConnection.GetProjectList(UserDetails, ServerTime);
                LogEntry logProj = new LogEntry("AvailableProjects");
                logProj.AddValue("user", UserDetails.EmailAddress);
                String projectArrayText = "";
                if (m_projectList != null)
                {
                    foreach (String p in m_projectList)
                    {
                        if (projectArrayText.Length > 0)
                        {
                            projectArrayText += ", ";
                        }
                        projectArrayText += p;
                    }
                }
                logProj.AddValue("projects", "[" + projectArrayText + "]");
                Log(logProj);
            }

            AvailableProjects.UpdateProjects(m_projectList);

            Project[] allProjects = AvailableProjects.GetProjectArray();
            if (allProjects != null)
            {
                DownloadManagerBase.RemoteFileDetails details = new DownloadManagerBase.RemoteFileDetails();
                foreach (Project p in allProjects)
                {
                    // If we need to add more of these SKU->Project properties
                    // consider extracting the SKU details from the server
                    // implementation and moving them to the client support
                    // library so project can be given a single SKU details
                    // item rather than copying them all individually.
                    //
                    // SKU Details extracted, but not as convenient to use
                    // since the server methods actually do more work than
                    // just returning the corresponding value, e.g. handling
                    // nulls and special cases. If these can be determined to
                    // be a feature of the SKU rather than specific to the
                    // source the extraction can continue.
                    p.PrettyName = ServerConnection.GetTitleForProject(p.Name);
                    p.SortKey = ServerConnection.GetSortKeyForProject(p.Name);
                    p.ProjectDirectory = ServerConnection.GetDirectoryForProject(p.Name);
                    p.Highlight = ServerConnection.GetHighlightColourForProject(p.Name);
                    p.Page = ServerConnection.GetTitlePageForProject(p.Name);
                    p.Filters = ServerConnection.GetFiltersForProject(p.Name);
                    p.BoxImageURI = ServerConnection.GetBoxImageURIForProject( p.Name );
                    p.HeroImageURI = ServerConnection.GetHeroImageURIForProject( p.Name );
                    p.LogoImageURI = ServerConnection.GetLogoImageURIForProject( p.Name );
                    p.ESRBRating= ServerConnection.GetESRBRatingForProject( p.Name );
                    p.PEGIRating = ServerConnection.GetPEGIRatingForProject( p.Name );
                    p.GameApi = ServerConnection.GetGameApiForProject( p.Name );
                    p.GameCode = ServerConnection.GetGameCodeForProject( p.Name );
                    p.MaxDownloadThreads = ServerConnection.GetMaxDownloadThreadsForProject(p.Name);
                    p.NoDetails = ServerConnection.GetNoDetailsForProject(p.Name);
                    if (p.Action==Project.ActionType.Play)
                    {
                        if (Authorised)
                        {
                            // We only care whether there is an update if there
                            // is an existing version we could play, otherwise
                            // we already know an install is needed.
                            DownloadManagerBase dm = ServerConnection.GetDownloadManager();
                            if (dm != null)
                            {
                                DownloadManagerBase.InstallerVersionResult result = dm.GetInstallerPath(UserDetails, p.Name, p.Version, ref details);
                                if (result != DownloadManagerBase.InstallerVersionResult.Failed)
                                {
                                    if (result == DownloadManagerBase.InstallerVersionResult.Update)
                                    {
                                        LogEntry entry = new LogEntry("UpdateRequired");
										entry.AddValue("Project", p.Name);
										entry.AddValue("Directory", Path.GetFileName(p.ProjectDirectory));
                                        entry.AddValue("Current", p.Version);
                                        entry.AddValue("Remote", details.RemoteVersion);
                                        Log(entry);
                                        p.RequiresUpdate();
                                    }
                                    p.SetRemoteInstallerType(details.LocalFileName);
                                }
                                else
                                {
                                    p.InvalidInstaller();
                                    details.RemoteVersion = null;
                                }
                            }
                            p.ServerVersion = details.RemoteVersion;
                        }
                    }
                }
            }

            AvailableProjects.FilterProjects(m_activeFilters);
        }

        /// <summary>
        /// Download the installer for a project and then run the installer.
        /// </summary>
        /// <param name="project">The project to be installed/updated.</param>
        /// <param name="monitor">
        /// The monitor object to use to display progress.
        /// </param>
        /// <param name="alternate">
        /// The ServerInterface from which to retrieve the file. If null then
        /// the existing server connection will be used. If required a
        /// FileInstallServer may be passed to handle installing from an
        /// existing local file.
        /// </param>
        public void DownloadAndInstallForProject(Project project,
            ProgressMonitor monitor,
            DownloadManagerBase alternate = null,
            ProjectUpdater.UpdateOptions options = null)
        {
            DownloadManagerBase downloader = alternate == null ? m_server.GetDownloadManager() : alternate;
            LogEntry entry = new LogEntry("DownloadAndInstallProject");
            entry.AddValue("project", project.Name);
            Log(entry);
            project.Action = Project.ActionType.Disabled;
            String logPath = null;
            if (DetailedUpdateLog)
            {
                String logDir = GetLocalDirectory("logs");
                logPath = Path.Combine(logDir, Path.GetFileName(project.ProjectDirectory) + "_update.log");
            }
            ProjectUpdater.ProjectUpdater pu = new ProjectUpdater.ProjectUpdater(project, UserDetails, downloader, monitor, logPath, options);
            pu.Go();
            if (!m_hasRunHardwareSurvey)
            {
                // We do this test here as we only want to perform the auto
                // generation once, if we have not already run the survey at
                // all.
                // We still want to run the survey if manually told to do so,
                // but if it has been run manually, it is then not necessary
                // to auto run it.
                RunHardwareSurvey(false, false);
            }
        }

        public void UninstallForProject(Project project, ProgressMonitor monitor, bool silent )
        {
            LogEntry entry = new LogEntry("UninstallProject");
            entry.AddValue("project", project.Name);
            Log(entry);
            project.Action = Project.ActionType.Disabled;
            ProjectUpdater.ProjectUpdater pu = new ProjectUpdater.ProjectUpdater(project, UserDetails, null, monitor, null, null);
            pu.SetMode(silent ? ProjectUpdater.UpdateStatus.UpdateMode.UninstallSilent : ProjectUpdater.UpdateStatus.UpdateMode.Uninstall);
            pu.Go();
        }

        /// <summary>
        /// Check that the game is in a state ready to run.
        /// </summary>
        /// <param name="project"></param>
        public bool PreFlightCheck(Project project)
        {
            LogEntry control = new LogEntry("@FileOff");
            Log(control);
            try
            {
                DownloadManagerBase downloader = m_server.GetDownloadManager();
                SilentProgressMonitor spm = new SilentProgressMonitor();
                ProjectUpdater.ProjectUpdater pu = new ProjectUpdater.ProjectUpdater(project, UserDetails, downloader, spm, null, null);
                pu.PreFlightCheck();
                if (!pu.Success)
                {
                    LogEntry entry = new LogEntry("UpdateFailure");
                    entry.AddValue("Error", pu.Error);
                    Log(entry);
                    return false;
                }
                if (pu.Changes > 0)
                {
                    LogEntry entry = new LogEntry("PreflightCheck");
                    entry.AddValue("changes", pu.Changes.ToString());
                    Log(entry);
                }
                return true;
            }
            catch (System.Exception ex)
            {
                LogEntry entry = new LogEntry("PFCException");
                entry.AddValue("Error", ex.Message);
                entry.AddValue("HRESULT", ex.HResult);
                return false;
            }
            finally
            {
                control = new LogEntry("@FileOn");
                Log(control);
            }
        }

        public void SetRunLanguage(String language)
        {
            m_runLanguage = language;
        }

        public List<String> GetAvailableLanguages()
        {
            List<String> available = new List<String>();

            Assembly an = Assembly.GetEntryAssembly();
            String directory = System.IO.Path.GetDirectoryName(an.Location);
            if (System.IO.Directory.Exists(directory))
            {
                foreach (String potential in System.IO.Directory.GetDirectories(directory))
                {
                    String basename = System.IO.Path.GetFileName(potential);
                    try
                    {
                        CultureInfo CI = CultureInfo.GetCultureInfo(basename);
                        if (CI != null)
                        {
                            if (!available.Contains(basename))
                            {
                                available.Add(basename);
                            }
                        }
                    }
                    catch (System.ArgumentException)
                    {
                        // Culture does not exist. This is a somewhat python
                        // like approach to the problem, but since the
                        // alternative is to retrieve a list of all possible
                        // cultures and then run through that to determine if
                        // the possible culture is present this seems like
                        // the neatest approach.
                    }
                }
            }

            if (!available.Contains(c_applicationLanguage))
            {
                available.Add(c_applicationLanguage);
            }
            available.Sort();
            return available;
        }


        /// <summary>
        /// Run the given project.
        /// </summary>
        /// <param name="project">The project to be started.</param>
        /// <param name="handler">
        /// The handler to call when the project finishes executing.
        /// </param>
        /// <returns>
        /// Null on success or a text string indicating errors encountered when
        /// trying to start the application.
        /// </returns>
        public String Run(Project project, ProjectRunner.ProjectCompletedEventHandler handler)
        {
            String launchOptions = null;
            String targetOptions = null;

            if (!IsReleaseBuild)
            {
                LoadGameOptions(out launchOptions, out targetOptions);
            }
            String projectExecutableDir = Path.GetDirectoryName(project.ExecutablePath);
            String projectWatchDogPath = Path.Combine(projectExecutableDir, watchDog);
            if (!File.Exists(projectWatchDogPath))
            {
                projectWatchDogPath = project.UseWatchDog64 ? m_watchDog64Path : m_watchDogPath;
            }
            if (!String.IsNullOrEmpty(m_runLanguage))
            {
                String languageOption = "/language " + m_runLanguage;
                if (String.IsNullOrEmpty(targetOptions))
                {
                    targetOptions = languageOption;
                }
                else
                {
                    targetOptions = targetOptions + " " + languageOption;
                }
            }
            if (IsSteam && project.SteamAware)
            {
                const String steamOption = "/steam";
                if (String.IsNullOrEmpty(targetOptions))
                {
                    targetOptions = steamOption;
                }
                else
                {
                    targetOptions += " " + steamOption;
                }
            }

            /*if (IsEpic)
            {
                if (!m_eosInterface.IsLoggedIn())
                {
                    Log(new LogEntry("User is not authed with EOS, cannot get token for user."));
                } else if (!m_eosInterface.HasValidRefreshToken())
                {
                    LogEntry le = new LogEntry("User refresh token is not valid");
                    le.AddValue("HasAuthToken", m_eosInterface.HasAuthToken());
                    if (m_eosInterface.HasAuthToken())
                    {
                        le.AddValue("RefreshToken", m_eosInterface.GetRefreshToken());
                        le.AddValue("TokenExpiry", m_eosInterface.GetRefreshTokenExpiryInfo());
                    }
                    Log(le);

                } else
                {
                    // Then we're good 
                    String epicOption = "\"EpicToken " + m_eosInterface.GetRefreshToken() + "\"";
                    if (String.IsNullOrEmpty(targetOptions))
                    {
                        targetOptions = epicOption;
                    }
                    else
                    {
                        targetOptions += " " + epicOption;
                    }
                    Log(new LogEntry("Passing Token to client: " + epicOption));
                }
            }*/

            if (ActiveVRMode!=VRMode.Unspecified)
            {
                String vrOption;
                switch (ActiveVRMode)
                {
                    case VRMode.Enabled:
                        {
                            vrOption = "/vr";
                            break;
                        }
                    case VRMode.Disabled:
                        {
                            vrOption = "/novr";
                            break;
                        }
                    default:
                        {
                            vrOption=null;
                            break;
                        }
                }
                if (!String.IsNullOrEmpty(vrOption))
                {
                    if (String.IsNullOrEmpty(targetOptions))
                    {
                        targetOptions = vrOption;
                    }
                    else
                    {
                        targetOptions += " " + vrOption;
                    }
                }
            }
            ProjectRunner runner = new ProjectRunner(project, projectWatchDogPath, launchOptions, targetOptions);
            runner.ProjectCompleted += handler;

            String message = runner.Run(UserDetails, MachineIdentifier.GetMachineIdentifier(), ServerConnection.GetCurrentTimeStamp(), ServerConnection.ServerExtraRunArguments(project));
            return message;
        }

        /// <summary>
        /// Get the link to use for registration.
        /// 
        /// If the link requires arguments it is not really a link but an
        /// external application. In that instance the caller is expected to
        /// call StartRegistration as normal.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public String GetRegistrationLink(String target)
        {
            String arguments;
            String link = MakeRegistrationLink(target, out arguments);
            if (!String.IsNullOrEmpty(arguments))
            {
                return null;
            }
            return link;
        }

        /// <summary>
        /// Move the start into the manager itself now that the requirements
        /// are made more complicated by the possibility of using an external
        /// tool to actually generate the link.
        /// </summary>
        /// <param name="target">Base link to visit</param>
        public void StartRegistration(String target)
        {
            String arguments;
            String application = MakeRegistrationLink(target, out arguments);

            if (!String.IsNullOrEmpty(arguments))
            {
                Process.Start(application, arguments);
            }
            else
            {
                Process.Start(application);
            }
        }

        /// <summary>
        /// Make the link to register a product.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public String MakeRegistrationLink(String target, out String arguments)
        {
            if (!String.IsNullOrEmpty(UserDetails.SteamRegistrationLink))
            {
                arguments = null;
                return UserDetails.SteamRegistrationLink;
            }
            if (!String.IsNullOrEmpty(UserDetails.SteamLinkLink))
            {
                arguments = null;
                return UserDetails.SteamLinkLink;
            }
            String result = HaveExternalGenerator(target, out arguments);
            if (result != target)
            {
                return result;
            }

            arguments = null;

            int added = 0;
           /* if (IsEpic)
            {
                result = LocalResources.Properties.Resources.RegisterLinkEpic;
                if (m_eosInterface.HasAuthToken())
                {
                    result = result + "/" + m_eosInterface.GetAccessToken();
                }
				return result;
            }*/
            if (!OculusEnabled)
            {
                result = result + AddOption("steam=" + (IsSteam ? "true" : "false"), ref added);
                ++added;
            }
            if (!String.IsNullOrEmpty(ExternalUserName))
            {
                result = result + AddOption("userex=" + ExternalUserName, ref added);
            }
            if ((IsSteam) && (m_machineIdentifier != null))
            {
                String key = GetSteamKey();
                
                if (!String.IsNullOrEmpty(key))
                {
                    result = result + AddOption("key=" + key, ref added);
                }
            }
            if ((OculusEnabled))
            {
                result = result + AddOption("oculus=true",ref added);
                result = result + AddOption("nonce=" + OculusNonce, ref added);
            }
            return result;
        }

        private String AddOption(String optionText, ref int added)
        {
            String result;
            if (added == 0)
            {
                result = "?";
            }
            else
            {
                result = "&";
            }
            ++added;
            return result + optionText;
        }

        public String HaveExternalGenerator(String target, out String arguments)
        {
            if (String.IsNullOrEmpty(m_registrationToolPath))
            {
                arguments = null;
                return target;
            }

            arguments = "";

            String appID = null;
            if (OculusEnabled)
            {
                appID = m_oculusNonce;
            }
            if (IsSteam)
            {
                appID = m_steamAppID;
            }
            if (!String.IsNullOrEmpty(appID))
            {
                arguments += " /app " + appID;
            }

            arguments += " /target " + target;

            return m_registrationToolPath;
        }

        public String GetSteamKey()
        {
            if (!String.IsNullOrEmpty(m_steamAppID))
            {
                return m_machineIdentifier.GetSteamKey(m_steamAppID);
            }

            if (m_activeFilters.Contains("edh"))
            {
                // Make this use the correct 
                return m_machineIdentifier.GetSteamKey("419270");
            }
            else
            {
                return m_machineIdentifier.GetSteamKey("359320");
            }
        }

        /// <summary>
        /// Log out the user discarding any session tokens.
        /// </summary>
        public void LogoutUser()
        {
            Log(new LogEntry("LoggedOutUser"));
            if (m_server != null)
            {
                m_server.LogoutUser(UserDetails);
            }
            ClearUserDetails();
            m_projectList = null;
        }

        /// <summary>
        /// Log out the user and the machine, discarding any session tokens
        /// along with machine tokens previously obtained.
        /// </summary>
        public void LogoutMachine()
        {
            Log(new LogEntry("LoggedOutMachine"));
            if (m_server != null)
            {
                m_server.LogoutMachine(UserDetails);
            }
            ClearUserDetails();
            m_projectList = null;
        }

        public void Log(LogEntry entry)
        {
            if (m_server != null)
            {
                m_server.LogValues(UserDetails, entry);
            }
        }

        /*public String GetRefreshToken()
        {
            String refreshToken = "";

            if (m_eosInterface != null)
            {
                refreshToken = m_eosInterface.GetRefreshToken();
            }
            return refreshToken;
        }

        public bool ShouldExit()
        {
            if (IsEpic)
            {
                if (m_eosInterface.ShouldShutDown())
                {
                    Log(new LogEntry("Shutting down, at request of EOS"));
                }
                return m_eosInterface.ShouldShutDown();
            }

            return false;
        }*/

        /// <summary>
        /// Creates a new FD account
        /// </summary>
        /// <param name="_firstName">The users first name</param>
        /// <param name="_lastName">The users last name</param>
        /// <param name="_email">The users email</param>
        /// <param name="_password">The users password</param>
        /// <param name="_passwordConfirm">The users conformation password</param>
        /// <param name="_newsAndPromoSignUp">Is the user signing-up for news and promotions</param>
        /// <returns>A JSONWebPostResult containing the result of the call, this can be null</returns>
        public JSONWebPutsAndPostsResult CreateFrontierAccount( string _firstName,
                                                                string _lastName,
                                                                string _email,
                                                                string _password,
                                                                string _passwordConfirm,
                                                                bool _newsAndPromoSignUp )
        {
            JSONWebPutsAndPostsResult jsonWebPostResult = null;

            Debug.Assert( ServerConnection != null );

            if ( ServerConnection != null )
            {
                jsonWebPostResult = ServerConnection.CreateFrontierAccount( UserDetails,
                                                                            _firstName,
                                                                            _lastName,
                                                                            _email,
                                                                            _password,
                                                                            _passwordConfirm,
                                                                            _newsAndPromoSignUp );
            }

            return jsonWebPostResult;
        }

        /// <summary>
        /// Checks the password to see if it meets the password complexity rules.
        /// </summary>
        /// <param name="_password">The users password</param>
        /// <returns>A JSONWebPostResult containing the result of the call, this can be null</returns>
        public JSONWebPutsAndPostsResult CheckPasswordComplexity( string _password )
        {
            JSONWebPutsAndPostsResult jsonWebPostResult = null;

            Debug.Assert( ServerConnection != null );

            if ( ServerConnection != null )
            {
                jsonWebPostResult = ServerConnection.CheckPasswordComplexity( UserDetails, _password );
            }

            return jsonWebPostResult;
        }

        /// <summary>
        /// Confirms a new FD account by passing a verification code (otp) to the server,
        /// along with email, password and a machineId.
        /// </summary>
        /// <param name="_email">The users email</param>
        /// <param name="_password">The users password</param>
        /// <param name="_otp">The otp code (verification code)</param>
        /// <param name="_machineId">The id of this machine</param>
        /// <returns>A JSONWebPostResult containing the result of the call, this can be null</returns>
        public JSONWebPutsAndPostsResult ConfirmFrontierAccount( string _email,
                                                                 string _password,
                                                                 string _otp,
                                                                 string _machineId )
        {
            JSONWebPutsAndPostsResult jsonWebPostResult = null;

            Debug.Assert( ServerConnection != null );

            if ( ServerConnection != null )
            {
                jsonWebPostResult = ServerConnection.ConfirmFrontierAccount( UserDetails,
                                                                            _email,
                                                                            _password,
                                                                            _otp,
                                                                            _machineId );
            }

            return jsonWebPostResult;
        }

        /// <summary>
        /// Attempts to link the store (e.g. Steam, Epic) account to the Frontier account.
        /// </summary>
        /// <param name="_storeAuthorisation">The store's authorisations code</param>
        /// <param name="_storeClientId">The store's client id</param>
        /// <param name="_email">The users email address</param>
        /// <param name="_password">The users password</param>
        /// <param name="_overRideAnyExistingLinks">Should existing links be ignored? if not then
        /// the call can produce a HttpStatusCode.PreconditionFailed, meaning linked to
        /// another account..</param>
        /// <returns>The result in the form of a JSONWebPutsAndPostsResult, this can be null</returns>
        public JSONWebPutsAndPostsResult LinkAccounts( string _storeAuthorisation,
                                                       string _storeClientId,
                                                       string _email,
                                                       string _password,
                                                       bool _overRideAnyExistingLinks )
        {
            JSONWebPutsAndPostsResult jsonWebPostResult = null;

            Debug.Assert( ServerConnection != null );

            if ( ServerConnection != null )
            {
                jsonWebPostResult = ServerConnection.LinkAccounts( UserDetails,
                                                                   _storeAuthorisation,
                                                                   _storeClientId,
                                                                   _email,
                                                                   _password,
                                                                   _overRideAnyExistingLinks );
            }

            return jsonWebPostResult;
        }

        /// <summary>
        /// Attempts to remove a store (e.g. Steam, Epic) link from a Frontier account
        /// </summary>
        /// <param name="_storeAuthorisation">The stores authorisations code</param>
        /// <param name="_storeClientId">The store's client id</param>
        /// <returns>The result in the form of a JSONWebPutsAndPostsResult, this can be null</returns>
        public JSONWebPutsAndPostsResult RemoveLinkAccounts( string _storeAuthorisation,
                                                             string _storeClientId )
        {
            JSONWebPutsAndPostsResult jsonWebPostResult = null;

            Debug.Assert( ServerConnection != null );

            if ( ServerConnection != null )
            {
                jsonWebPostResult = ServerConnection.RemoveLinkAccounts( UserDetails,
                                                                         _storeAuthorisation,
                                                                         _storeClientId );
            }
            return jsonWebPostResult;
        }

        /// <summary>
        /// Redeems a game code for the current user
        /// </summary>
        /// <param name="_code">The game code to redeem</param>
        /// <returns>The result in the form of a JSONWebPutsAndPostsResult, this can be null</returns>
        public JSONWebPutsAndPostsResult RedeemCode( string _code )
        {
            JSONWebPutsAndPostsResult jsonWebPostResult = null;

            Debug.Assert( ServerConnection != null );

            if ( ServerConnection != null )
            {
                jsonWebPostResult = ServerConnection.RedeemCode( UserDetails, MachineIdentifier, _code );
            }
            return jsonWebPostResult;
        }

        /* 
            This isn't pretty but setting the TLS config has to live somewhere, and it has to be central enough that all parts of the launcher 
            and related exes will have access to it. As this class does handle downloads and most of the web connectivity 
            It's one of the least bad places for these functions to live. Should consider separating them into a separate static util class though
        */
        public static void SetTlsProtocol(SecurityProtocolType newProtocol)
        {
            System.Net.ServicePointManager.SecurityProtocol = newProtocol;
        }

        public static void EnsureIsUsingSecureTlsProtocol()
        {
            System.Net.SecurityProtocolType outdatedProtocols = SecurityProtocolType.Tls | SecurityProtocolType.Tls11;
            bool isAllowingOutdatedProtocol = (System.Net.ServicePointManager.SecurityProtocol & outdatedProtocols) > 0;
#if DEVELOPMENT
            if (isAllowingOutdatedProtocol)
            {
                Console.WriteLine("System using outdated security protocols: " + System.Net.ServicePointManager.SecurityProtocol);
            }
#endif
            bool isOnlyUsingBestTls = System.Net.ServicePointManager.SecurityProtocol == SecurityProtocolType.Tls12;

            if (!isOnlyUsingBestTls)
            {
                // Report error and fix
#if DEVELOPMENT
                Console.WriteLine("Updating to use TLS 1.2");
#endif
                SetTlsProtocol(System.Net.SecurityProtocolType.Tls12);
            }
        }
    }
}
