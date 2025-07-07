using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using ClientSupport;
using FORCServerSupport;
using FrontierSupport;

namespace CBViewModel
{
    /// <summary>
    /// Backing view for the CobraBay application used to detach the logic
    /// from the user interface with the intention of replacing it with a Forms
    /// based version that will work under Mono.
    /// </summary>
    public class CobraBayView : INotifyPropertyChanged
    {
        /* Privatise */
        /// <summary>
        /// The FORCManager object, all future changes should use
        /// the method Manager() to get this object and not access
        /// this variable directly from outside this class.
        /// </summary>
        public FORCManager m_manager = null;

        /// <summary>
        /// The Elite API Server interface
        /// </summary>
        private EliteServerInterface m_eliteServerInterface = null;

        /// <summary>
        /// The CMS API Server interface
        /// </summary>
        private CMSServerInterface m_cmsServerInterface = null;

        private UserInterface m_ui = null;

        /* Privatise */
        public LogOutManager m_logOutManager = null;

        private String m_autoSelection = null;

        private ProjectProgressMonitor m_monitor = null;

        /// <summary>
        /// Define the LinkAccount message field
        /// </summary>
        private const string c_linkAccountMessage = "message";

        /// <summary>
        /// The steam client id
        /// </summary>
        private const string c_SteamClientId = "359320";

        /// <summary>
        /// Used to monitor progress on installation, or null if no
        /// installation is in process.
        /// </summary>
        public ProjectProgressMonitor Monitor
        {
            get
            {
                return m_monitor;
            }
            set
            {
                if (m_monitor != value)
                {
                    m_monitor = value;
                    RaisePropertyChanged("Monitor");
                }
            }
        }

        /* Privatise */
        public bool m_wasAuthorised = false;

        /* Privatise */
        public ServerInterface.VersionStatus m_versionStatus = ServerInterface.VersionStatus.Current;
        /* Privatise */
        public String m_serverClientVersion;

        private String m_appversion = LocalResources.Properties.Resources.DefaultApplicationVersionString;
        /// <summary>
        /// Version string for the CobraBay application.
        /// </summary>
        public String CobraBayVersion
        {
            get { return m_appversion; }
            set
            {
                if (m_appversion != value)
                {
                    m_appversion = value;
                    RaisePropertyChanged("CobraBayVersion");
                }
            }
        }

        /// <summary>
        /// Determines if we are logged on via Steam
        /// </summary>
        /// <returns>true if we are logged on via Steam</returns>
        public bool IsSteam()
        {
            bool isSteam = false;
            Debug.Assert( m_manager != null );

            if ( m_manager != null )
            {
                isSteam = m_manager.IsSteam;
            }

            return isSteam;
        }

        /// <summary>
        /// Determines if we are logged on via Epic
        /// </summary>
        /// <returns>true if we are logged on via Epic</returns>
        public bool IsEpic()
        {
            bool isEpic = false;
            Debug.Assert( m_manager != null );

            if ( m_manager != null )
            {
                isEpic = m_manager.IsEpic;
            }

            return isEpic;
        }

        /* Privatise */
        public String m_actualVersion;
        private String m_versionInfo;
        /// <summary>
        /// Extended version string which also includes status about the
        /// version to be presented to the user.
        /// </summary>
        public String CobraBayVersionInfo
        {
            get
            {
                return m_versionInfo;
            }
            set
            {
                String adjusted = value;
                if (DisableFastDownload)
                {
                    adjusted += " ND";
                }
                else
                {
                    adjusted += " FD";
                }
                if (m_versionInfo != adjusted)
                {
                    m_versionInfo = adjusted;
                    RaisePropertyChanged("CobraBayVersionInfo");
                }
            }
        }

        public bool DeveloperIgnoreUpdates = false;

        public CBTheme Theme = CBTheme.Theme;

        public bool DisableFastDownload
        {
            get
            {
                return Properties.Settings.Default.Option_DisableFastDownload;
            }
            set
            {
                if (value != Properties.Settings.Default.Option_DisableFastDownload)
                {
                    Properties.Settings.Default.Option_DisableFastDownload = value;
                    Properties.Settings.Default.Save();
                    UpdateCobraBayVersionInfo();
                    RaisePropertyChanged("DisableFastDownload");
                }
            }
        }

        /// <summary>
        /// The build version of the application. In the usual case where the
        /// CobraBayVersion is the same as the ActualVersion it will be used
        /// directly. Where they differ they will be combined to show that
        /// the application is reporting one version but was built as a
        /// different version. The distinction should only be present in
        /// developer/internal builds.
        /// </summary>
        public void UpdateCobraBayVersionInfo()
        {
            String effectiveVersion = null;
            if (CobraBayVersion != m_actualVersion)
            {
                effectiveVersion = String.Format(LocalResources.Properties.Resources.AlteredLauncherVersion,
                    m_manager.ApplicationVersion, m_actualVersion);
            }
            else
            {
                effectiveVersion = CobraBayVersion;
            }
            switch (m_versionStatus)
            {
                case (ServerInterface.VersionStatus.Current):
                    {
                        CobraBayVersionInfo = effectiveVersion;
                        break;
                    }
                case (ServerInterface.VersionStatus.Supported):
                    {
                        CobraBayVersionInfo = String.Format(LocalResources.Properties.Resources.VersionStatusSupported,
                            effectiveVersion, m_serverClientVersion);
                        break;
                    }
                case (ServerInterface.VersionStatus.Expired):
                    {
                        CobraBayVersionInfo = String.Format(LocalResources.Properties.Resources.VersionStatusRequired,
                                                    effectiveVersion, m_serverClientVersion);
                        break;
                    }
                case (ServerInterface.VersionStatus.Future):
                    {
                        CobraBayVersionInfo = String.Format(LocalResources.Properties.Resources.VersionStatusFuture,
                                                    effectiveVersion, m_serverClientVersion);
                        break;
                    }
            }
        }

        /// <summary>
        /// Version string used to indicate the currently installed game
        /// version.
        /// </summary>
        private String m_gameVersionInfo;
        public String GameVersionInfo
        {
            get { return m_gameVersionInfo; }
            set
            {
                if (m_gameVersionInfo != value)
                {
                    m_gameVersionInfo = value;
                    RaisePropertyChanged("GameVersionInfo");
                    GameVersionInfoVisible = !String.IsNullOrEmpty(m_gameVersionInfo);
                }
            }
        }

        /// <summary>
        /// Determine whether the version info should be visible.
        /// </summary>
        private bool m_gameVersionInfoVisible = false;
        public bool GameVersionInfoVisible
        {
            get { return m_gameVersionInfoVisible; }
            set
            {
                if (m_gameVersionInfoVisible != value)
                {
                    m_gameVersionInfoVisible = value;
                    RaisePropertyChanged("GameVersionInfoVisible");
                }
            }
        }

        /// <summary>
        /// Location of the product directory.
        /// </summary>
        private String m_productDirectoryRoot;
        public String ProductDirectoryRoot
        {
            get { return m_productDirectoryRoot; }
            set
            {
                if (m_productDirectoryRoot != value)
                {
                    m_productDirectoryRoot = value;
                    RaisePropertyChanged("ProductDirectoryRoot");
                }
            }
        }

        /// <summary>
        /// Property indicating whether the release notes link for the current
        /// game version should be visible/enabled.
        /// </summary>
        private bool m_gameReleaseNotesButtonPermitted = true;
        public bool GameReleaseNotesButtonPermitted
        {
            get
            {
                return m_gameReleaseNotesButtonPermitted;
            }
            set
            {
                if (m_gameReleaseNotesButtonPermitted != value)
                {
                    m_gameReleaseNotesButtonPermitted = value;
                    RaisePropertyChanged("GameReleaseNotesButtonPermitted");
                }
            }
        }

        private bool m_gameReleaseNotesButtonVisible = true;
        public bool GameReleaseNotesButtonVisible
        {
            get
            {
                return m_gameReleaseNotesButtonVisible;
            }
            set
            {
                if (m_gameReleaseNotesButtonVisible != value)
                {
                    m_gameReleaseNotesButtonVisible = value;
                    RaisePropertyChanged("GameReleaseNotesButtonVisible");
                }
            }
        }

        /// <summary>
        /// The identifier (SKU) of the selected product.
        /// </summary>
        /* Privatise */
        public String SelectedProject = null;
        
        /// <summary>
        /// The default value for the identifier (SKU) of the selected product.
        /// </summary>
        /* Privatise */
        public String SelectedProjectDefault = null;

        /// <summary>
        /// The user friendly name of the selected project.
        /// </summary>
        private String m_selectedProjectName;
        public String SelectedProjectName
        {
            get { return m_selectedProjectName; }
            set
            {
                if (m_selectedProjectName != value)
                {
                    m_selectedProjectName = value;
                    RaisePropertyChanged("SelectedProjectName");
                }
            }
        }

        public String LanguageOverride
        {
            get
            {
                return Properties.Settings.Default.LanguageOverride;
            }
            set
            {
                if (value!=Properties.Settings.Default.LanguageOverride)
                {
                    Properties.Settings.Default.LanguageOverride = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public bool ProductListVisible
        {
            get
            {
                int minimumProductsForVisibility = 2;
                if (m_manager.IsSteam)
                {
                    // Steam users do not need to manually upgrade the products
                    // as Steam should take care of it for them. As a result
                    // they may not realise that if they 'play' before logging
                    // in they will be playing the demo not the full game.
                    // In this case showing the product list even with one item
                    // makes it clearer which product will be played.
                    //minimumProductsForVisibility = 1;
                }
                return (m_manager.AvailableProjects.GetProjectArray().Length >= minimumProductsForVisibility) && (Monitor == null);
            }
        }

        private String m_externalPage = null;
        public String ExternalPage
        {
            get
            {
                return m_externalPage;
            }
            set
            {
                if (m_externalPage != value)
                {
                    m_externalPage = value;
                    RaisePropertyChanged("ExternalPage");
                }
            }
        }

        private String m_highlightColour = null;
        public String HighlightColour
        {
            get
            {
                return m_highlightColour;
            }
            set
            {
                if (m_highlightColour != value)
                {
                    m_highlightColour = value;
                    RaisePropertyChanged("HighlightColour");
                }
            }
        }

        public bool UnfilteredOnly
        {
            get
            {
                if (!m_manager.FilteringEnabled)
                {
                    return false;
                }
                Project[] projects = m_manager.AvailableProjects.GetProjectArray();
                foreach (Project p in projects)
                {
                    if ((p.Filters != null))
                    {
                        if (p.Filters.Length > 0)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        private bool m_shouldExit;
        public bool ShouldExit
        {
            get { return m_shouldExit; }
        }

        public CobraBayView(UserInterface ui)
        {
            m_ui = ui;
            m_shouldExit = false;

            // Upgrading here as it is the correct place for it.
            // In practice the settings should have been upgraded (at least on
            // windows) at the application level as we need to make sure that
            // any configured language is available before we try to use it.
            UpgradeUserSettings();

            m_logOutManager = new LogOutManager(LocalResources.Properties.Resources.MenuLogOutUser,
                LocalResources.Properties.Resources.MenuLogOutMachine);

            ProcessCommandLine();
        }

        /// <summary>
        /// Returns the FORCManager, the variable is public, but all
        /// future external class access must go via this method.
        /// </summary>
        /// <returns>FORCManager, this can be null</returns>
        public FORCManager Manager()
        {
            return m_manager;
        }

        /// <summary>
        /// Returns the Elite API Server interface, giving access to the Elite API methods.
        /// </summary>
        /// <returns>The Elite API Server interface</returns>
        public EliteServerInterface GetEliteServerInterface()
        {
            return m_eliteServerInterface;
        }

        /// <summary>
        /// Returns the CMSServerInterface, giving access to the CMS API methods.
        /// </summary>
        /// <returns>The CMSServerInterface</returns>
        public CMSServerInterface GetCMSServerInterface()
        {
            return m_cmsServerInterface;
        }

        public static void UpgradeUserSettings()
        {
            if (Properties.Settings.Default.UpgradeSettings)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeSettings = false;
                Properties.Settings.Default.Option_DisableFastDownload = false;
                Properties.Settings.Default.Save();
            }
        }


        private void ProcessCommandLine()
        {
            String[] argv = Environment.GetCommandLineArgs();

            for (int arg = 1; arg < (argv.Length - 1); ++arg)
            {
                String lc = argv[arg].ToLowerInvariant();
                if (lc == "/select")
                {
                    m_autoSelection = argv[arg + 1].ToLowerInvariant();
                }
                if (lc == "/selectedproject" && ((arg + 1) < argv.Length))
                {
                    SelectedProjectDefault = argv[arg + 1];
                    SelectedProject = SelectedProjectDefault;
                }
            }
        }

        /// <summary>
        /// Create the required manager object to handle the (potentially)
        /// installed products.
        /// 
        /// Setup the connection to the server and various other information.
        /// </summary>
        public void ResetManager()
        {
            m_manager = new FORCManager();
            m_manager.Initialise(null);
            //m_manager.InitialiseEpic();
            if (m_manager.ServerConnection == null)
            {
                // Assume we will be using a real server in this case.
                FORCServerConnection realServer = new FORCServerConnection(m_manager);
                realServer.SetLanguage(Properties.Settings.Default.LanguageOverride);
                m_manager.ServerConnection = realServer;
            }

            // Gain access to Galnet (Elite API)
            if ( m_eliteServerInterface == null )
            {
                EliteServerConnection eliteServerConnection = new EliteServerConnection();
                eliteServerConnection.SetLanguage( LocalResources.Properties.Resources.LanguageCode );
                m_eliteServerInterface = eliteServerConnection;
            }

            // Gain access to the CMS API 
            if ( m_cmsServerInterface == null )
            {
                CMSServerConnection cmsServerConnection = new CMSServerConnection();
                cmsServerConnection.SetLanguageAndCountryCode( LocalResources.Properties.Resources.LanguageAndCountryCode );
                m_cmsServerInterface = cmsServerConnection;
            }

            if (m_manager.MachineIdentifier == null)
            {
                m_manager.MachineIdentifier = new FrontierMachineIdentifier();
            }

            AssemblyName an = Assembly.GetEntryAssembly().GetName();

            m_actualVersion = m_manager.ActualVersion;

            CobraBayVersion = m_manager.ApplicationVersion;

            LogEntry cv;
            cv = new LogEntry("ClientVersion");
            cv.AddValue("application", CobraBayVersion);
            if (CobraBayVersion != m_actualVersion)
            {
                cv.AddValue("actual", m_actualVersion);
            }
            FileInfo info = new FileInfo(Assembly.GetEntryAssembly().Location);
            cv.AddValue("path", info.FullName);
            cv.AddValue("modification", info.LastWriteTimeUtc.ToString());
            m_manager.ServerConnection.LogValues(m_manager.UserDetails, cv);


            JSONWebQuery.SetUserAgent(an.Name, CobraBayVersion, m_ui.GetOSIdent(), null);

            m_versionStatus = m_manager.ServerConnection.CheckClientVersion(CobraBayVersion, out m_serverClientVersion);

            TidyOldConfigurations();

            // See if we can log in the user automatically.
            //m_manager.WaitForInit();

            m_manager.AutoLogin();

            m_manager.UpdateProjectList();

            AutoRun();

            OculusWarning();
        }

        public void UpdateExitStatus()
        {
            m_shouldExit = m_shouldExit;//|| m_manager.ShouldExit();
        }

        /// <summary>
        /// Remove configurations generated by older versions of the
        /// configuration that may have been left over by previous
        /// installations, ideally in a way that does not trash configurations
        /// we are still interested in.
        /// 
        /// If the application does not have a current version, or if we have
        /// failed to retrieve the current live version from the server then
        /// stop checking as we have no hope of doing anything sensible.
        /// 
        /// Otherwise pick the oldest version of:
        /// a) The version we are currently using.
        /// b) The version the server is currently providing.
        /// c) The configuration we are actually using.
        /// 
        /// And only remove versions older than that.
        /// 
        /// This should allow us to safely use future versions without
        /// removing saved configurations from the current version. By the
        /// time we have the current/server version information the options
        /// should have been upgraded already if required so it should be safe
        /// to remove the version we upgraded from.
        /// </summary>
        private void TidyOldConfigurations()
        {
            if (String.IsNullOrEmpty(CobraBayVersion))
            {
                return;
            }
            if (String.IsNullOrEmpty(m_serverClientVersion))
            {
                return;
            }
            SimpleFileVersionInfo limit = new SimpleFileVersionInfo(CobraBayVersion);
            SimpleFileVersionInfo info = new SimpleFileVersionInfo(m_serverClientVersion);
            if (info < limit)
            {
                limit = info;
            }
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            String configFile = config.FilePath;
            String configDir = System.IO.Path.GetDirectoryName(configFile);
            String configName = System.IO.Path.GetFileName(configDir);
            String configRoot = System.IO.Path.GetDirectoryName(configDir);
            String[] dirs = Directory.GetDirectories(configRoot);
            info = new SimpleFileVersionInfo(configName);
            if (info < limit)
            {
                limit = info;
            }

            foreach (String dir in dirs)
            {
                String configVersion = System.IO.Path.GetFileName(dir);
                if (configVersion == configName)
                {
                    // Never delete the current configuration file.
                    continue;
                }

                SimpleFileVersionInfo test = new SimpleFileVersionInfo(configVersion);
                if (test.FileMajorPart == -1)
                {
                    // Never remove a directory which does not correspond to a
                    // valid version number.
                    continue;
                }

                if (test < limit)
                {
                    // Only remove directories older than our chosen limit.
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch (System.Exception)
                    {

                    }
                }
            }
        }

        private void AutoRun()
        {
            if (m_manager.IsAutoRun)
            {
                if (m_manager.Authorised)
                {
                    
                    Project p = GetActiveProject();

                    if (p.Action == Project.ActionType.Play)
                    {
                        StartSelectedProject();
                    }
                }

            }
        }

        private void OculusWarning()
        {
            if (m_manager.OculusActivation == FORCManager.OculusMode.Implicit)
            {
                if (m_ui != null)
                {
                    m_ui.ErrorMessage(LocalResources.Properties.Resources.OculusDirectRunWarningDetails,
                        LocalResources.Properties.Resources.OculusDirectRunWarningTitle);
                }
            }
        }

        public void CheckLauncherVersion()
        {
            if (m_versionStatus == ServerInterface.VersionStatus.Expired)
            {
                if ((!m_manager.IsSteam) && (!m_manager.OculusEnabled))
                {
                    DownloadLauncherUpdate();
                }
            }
        }

        public void CheckLauncherValid()
        {
            if (!m_manager.HasServer)
            {
                if (SelectedProject == null)
                {
                    m_ui.ErrorMessage(LocalResources.Properties.Resources.TimeSyncFailedDetails,
                        LocalResources.Properties.Resources.TimeSyncFailedTitle);
                    m_ui.CloseWindow();
                }
            }
        }

        public bool DownloadLauncherUpdate()
        {
            String title = LocalResources.Properties.Resources.ExpiredLauncherVersionTitle;
            String details = String.Format(LocalResources.Properties.Resources.ExpiredLauncherVersionDetails,
                m_serverClientVersion);
            if (m_ui.YesNoQuery(details, title))
            {
                DownloadLauncherInstaller();
            }
            return false;
        }

        public bool DownloadOptionalLauncherInstaller()
        {
            String title = LocalResources.Properties.Resources.OptionalLauncherVersionTitle;
            String details = String.Format(LocalResources.Properties.Resources.OptionalLauncherVersionDetails,
                m_serverClientVersion);
            if (m_ui.YesNoQuery(details, title))
            {
                DownloadLauncherInstaller();
            }
            return false;
        }

        public bool DownloadLauncherInstaller()
        {
            Start(LocalResources.Properties.Resources.ExpiredLauncherLink);
            m_ui.CloseWindow();
            return true;
        }

        /// <summary>
        /// In the single item client we need a specific project of interest.
        /// </summary>
        /// <returns></returns>
        public Project GetSelectedProject()
        {
            Project p = DetermineSelectedProject();
            SetThemeForProject(p);
            m_ui.UpdateSelectedProject();
            return p;
        }

        private void SetThemeForProject(Project theme)
        {
            Project p = theme;

            if (p != null)
            {
                ExternalPage = p.Page;
                Theme.SetThemeColour(p.Highlight);
                HighlightColour = p.Highlight;
            }
            else
            {
                // If we do not have a project use the default configured through the resources.
                if (m_manager.IsSteam)
                {
                    ExternalPage = LocalResources.Properties.Resources.RF_PageSteam;
                }
                else
                {
                    ExternalPage = LocalResources.Properties.Resources.RF_Page;
                }
                //String baseGameColour = "#f07b05
                String horizonsColour = "#0a8bd6";

                String highlight = horizonsColour;
                Theme.SetThemeColour(highlight);
                HighlightColour = highlight;
            }
        }

		private Project GetHighPriorityProject(Project def)
		{
			Project p = def;
			// Undo all the hard work figuring out which is the selected
			// project and just pick the most important one and always use that.
			Project[] projects = m_manager.AvailableProjects.GetProjectArray();
			if (projects.Length > 0)
			{
				int best = 0;
				for (int t = 1; t < projects.Length; ++t)
				{
					int skc = projects[t].SortKey.CompareTo(projects[best].SortKey);
					if (skc < 0)
					{
						best = t;
					}
					else
					{
						if (skc == 0)
						{
							if (projects[t].Name.CompareTo(projects[best].Name) < 0)
							{
								best = t;
							}
						}
					}
				}
				p = projects[best];
			}
			return p;
		}

        /// <summary>
        /// Attempts to link the store account to the current Frontier account
        /// </summary>
        /// <param name="_overRideAnyExistingLinks">Should existing links be ignored? if not then
        /// the call can produce _accountLinkedToAnotherStoreAccount == true, else it forces
        /// the link to be created</param>
        /// <param name="_accountLinkedToAnotherStoreAccount">out parameter. Set to true if the linking 
        /// failed because the Frontier Account is already linked to another store account,
        /// this can be any store (Epic, Steam etc).</param>
        /// <param name="_serverLinkStateChange">out parameter. The account linking state on the server has changed,
        /// meaning this link cannot be done with the current settings because the server side link state has already 
        /// been updated via another client.</returns>
        public bool LinkAccounts( bool _overRideAnyExistingLinks, out bool _accountLinkedToAnotherStoreAccount, out bool _serverLinkStateChange )
        {
            bool didAccountLinkOkay = false;
            _accountLinkedToAnotherStoreAccount = false;
            _serverLinkStateChange = false;

            LogEntry log = new LogEntry("LinkAccounts");

            FORCManager forcManager = Manager();
            Debug.Assert( forcManager != null );

            if ( forcManager != null )
            {
                if ( forcManager.UserDetails != null )
                {
                    string sessionToken = forcManager.UserDetails.SteamSessionToken;
                    string clientId = c_SteamClientId;
                    if ( IsEpic() )
                    {
                        sessionToken = forcManager.UserDetails.EpicAccessToken;
                        clientId = null;
                    }

                    JSONWebPutsAndPostsResult result = forcManager.LinkAccounts( sessionToken,
                                                                                 clientId,
                                                                                 forcManager.UserDetails.EmailAddress,
                                                                                 forcManager.UserDetails.Password,
                                                                                 _overRideAnyExistingLinks );
                    if ( result != null )
                    {
                        switch ( result.HttpStatusResult )
                        {
                            case HttpStatusCode.OK:
                            case HttpStatusCode.Created:
                                {
                                    // We linked the accounts
                                    didAccountLinkOkay = true;
                                    SetStoreAuthenticationType();
                                    log.AddValue( "LinkAccount", "Success" );
                                    break;
                                }
                            case HttpStatusCode.Unauthorized:
                                {
                                    // Failed to link accounts
                                    didAccountLinkOkay = false;
                                    log.AddValue( "LinkAccount", "failed" );
                                    log.AddValue( "LinkAccountStatus", "HttpStatusCode.Unauthorized" );
                                    break;
                                }
                            case (HttpStatusCode)422:
                                {
                                    // Failed, and we have a message attached
                                    didAccountLinkOkay = false;
                                    string message = null;
                                    result.HttpResponseDictionary.TryGetValue( c_linkAccountMessage, out message );
                                    log.AddValue( "LinkAccount", "failed" );
                                    log.AddValue( "LinkAccountStatus", "422" );
                                    if ( !string.IsNullOrWhiteSpace( message ) )
                                    {
                                        log.AddValue( "LinkAccount", message );
                                    }
                                    else
                                    {
                                        log.AddValue( "LinkAccount", "No message return from server" );
                                    }
                                    break;
                                }
                             case HttpStatusCode.PreconditionFailed:
                                 {
                                     // The Frontier account is linked to another store account
                                     didAccountLinkOkay = false;
                                     _accountLinkedToAnotherStoreAccount = true;
                                     log.AddValue( "LinkAccount", "failed" );
                                     log.AddValue( "LinkAccountStatus", "HttpStatusCode.PreconditionFailed" );
                                     log.AddValue( "LinkAccountStatus", "Linked to another store" );
                                     break;
                                 }
                            case HttpStatusCode.NotAcceptable:
                                {
                                    // The store account link has changed, it has now been linked to another
                                    // FD account or unlinked somehow, causing the link to fail (even if it is forced).
                                    didAccountLinkOkay = false;
                                    // We do not know if the account is linked to another store, the state has changed
                                    // and we don't know what it has changed to, set this to false and the caller must
                                    // key off _serverLinkStateChange.
                                    _accountLinkedToAnotherStoreAccount = false;
                                    _serverLinkStateChange = true;
                                    log.AddValue( "LinkAccount", "failed" );
                                    log.AddValue( "LinkAccountStatus", "HttpStatusCode.NotAcceptable" );
                                    log.AddValue( "LinkAccountStatus", "The server link state has changed, unable to link account" );
                                    break;
                                }
                            default:
                                {
                                    string errorMsg = string.Format( "failed, unexpected HttpStatusResult {0}", result.HttpStatusResult );
                                    log.AddValue( "LinkAccount", errorMsg );
                                    didAccountLinkOkay = false;
                                    break;
                                }
                        }
                    }
                    else
                    {
                        log.AddValue( "LinkAccount", "Server, No reply" );
                    }
                }
                else
                {
                    log.AddValue( "LinkAccount", "UserDetails is null" );
                }
            }

            if ( forcManager != null )
            {
                forcManager.ServerConnection.LogValues( forcManager.UserDetails, log );
            }

            return didAccountLinkOkay;
        }

        /// <summary>
        /// Sets the authentication type for stores
        /// </summary>
        private void SetStoreAuthenticationType()
        {
            FORCManager forcManager = Manager();
            Debug.Assert( forcManager != null );

            if ( forcManager != null )
            {
                if ( IsSteam() )
                {
                    forcManager.UserDetails.AuthenticationType = ServerInterface.AuthenticationType.Steam;
                }
                else if ( IsEpic() )
                {
                    forcManager.UserDetails.AuthenticationType = ServerInterface.AuthenticationType.Epic;
                }
            }
        }

        /// <summary>
        /// Attempts to delete a linked store account to the curremt Frontier account
        /// </summary>
        /// <returns>true if the link was deleted</returns>
        public bool DeleteLinkAccounts()
        {
            bool didDelteLinkOkay = false;

            LogEntry log = new LogEntry("DeleteLinkAccounts");

            FORCManager forcManager = Manager();
            Debug.Assert( forcManager != null );

            if ( forcManager != null )
            {
                if ( forcManager.UserDetails != null )
                {
                    string sessionToken = forcManager.UserDetails.SteamSessionToken;
                    string clientId = c_SteamClientId;
                    if ( forcManager.IsEpic )
                    {
                        sessionToken = forcManager.UserDetails.EpicAccessToken;
                        clientId = forcManager.UserDetails.EpicClientId;
                    }

                    JSONWebPutsAndPostsResult result = forcManager.RemoveLinkAccounts( sessionToken,
                                                                                       clientId );

                    if ( result != null )
                    {
                        switch ( result.HttpStatusResult )
                        {
                            case HttpStatusCode.OK:
                            case HttpStatusCode.Created:
                                {
                                    // We deleted the link
                                    didDelteLinkOkay = true;
                                    log.AddValue( "DeleteLinkAccounts", "Success" );
                                    break;
                                }
                            case HttpStatusCode.Unauthorized:
                                {
                                    // Failed to delete link
                                    didDelteLinkOkay = false;
                                    log.AddValue( "DeleteLinkAccounts", "failed" );
                                    log.AddValue( "LinkAccountStatus", "HttpStatusCode.Unauthorized" );
                                    break;
                                }
                            case (HttpStatusCode)422:
                                {
                                    // Failed, and we have a message attached
                                    didDelteLinkOkay = false;
                                    string message = null;
                                    result.HttpResponseDictionary.TryGetValue( c_linkAccountMessage, out message );
                                    log.AddValue( "DeleteLinkAccounts", "failed" );
                                    log.AddValue( "LinkAccountStatus", "422" );
                                    if ( !string.IsNullOrWhiteSpace( message ) )
                                    {
                                        log.AddValue( "DeleteLinkAccounts", message );
                                    }
                                    else
                                    {
                                        log.AddValue( "DeleteLinkAccounts", "No message returned from server" );
                                    }
                                    break;
                                }
                            default:
                                {
                                    log.AddValue( "DeleteLinkAccounts", string.Format( "Unexepected  HttpStatusResult from server {0}", result.HttpStatusResult ) );
                                    didDelteLinkOkay = false;
                                    break;
                                }
                        }
                    }
                    else
                    {
                        log.AddValue( "DeleteLinkAccounts", "No reply from server" );
                    }
                }
                else
                {
                    log.AddValue( "DeleteLinkAccounts", "UserDetails is null" );
                }
            }

            if ( forcManager != null )
            {
                forcManager.ServerConnection.LogValues( forcManager.UserDetails, log );
            }

            return didDelteLinkOkay;
        }

        public Project GetActiveProject()
        {
            Project p = GetSelectedProject();
            if ((p != null) && (p.Offline || m_manager.Authorised))
            {
                return p;
            }
            return null;
        }

        public Project DetermineSelectedProject()
        {
            Project[] projects = m_manager.AvailableProjects.GetProjectArray();
            String[] SKUs = {
                                "FORC_FDEV_D_VIP_1001",
                                "FORC_FDEV_D_PRESS_1001",
                                "FORC-FDEV-D-1000",
                                "FORC-FDEV-D-1001",
                                "FORC-FDEV-D-1002",
                                "FORC-FDEV-D-1003"
                            };

            // Look for the known alpha product by name, this seems fixed and
            // ensures it works if there are multiple products defined.
            // It will need updating if the SKU changes at any time, and does
            // not handle the update from alpha to beta or similar gracefully.
            // We are likely to need a new client before that point anyway.
            //foreach (String SKU in SKUs)
            if (SelectedProject != null)
            {
                foreach (Project p in projects)
                {
                    if (p.Name == SelectedProject)
                    {
                        SelectedProjectName = p.PrettyName;
                        GameReleaseNotesButtonVisible = p.Installed ? GameReleaseNotesButtonPermitted : false;
                        return p;
                    }
                }
            }

            bool steamRedeemHack = UnfilteredOnly &&
                (m_manager.IsSteam /*|| m_manager.IsOculus*/);

            // If we have projects, prioritise them according to the SKU list
            // above. If none of the SKUs match, give up and use the first one.
            if ((projects.Length > 0) && (!steamRedeemHack))
            {
                // The available projects list is sorted based on the server
                // sort order so use that if available.
                Project available = SelectFromAvailableProjects();
                if (available != null)
                {
                    SelectedProject = available.Name;
                    SelectedProjectName = available.PrettyName;
                    return available;
                }

                foreach (String sku in SKUs)
                {
                    foreach (Project p in projects)
                    {
                        if (p.Name == sku)
                        {
                            SelectedProject = p.Name;
                            SelectedProjectName = p.PrettyName;
                            GameReleaseNotesButtonVisible = p.Installed ? GameReleaseNotesButtonPermitted : false;
                            return p;
                        }
                    }
                }
                // Nothing in the known SKUs so use the first one which is
                // not offline.
                Project offline = null;
                foreach (Project p in projects)
                {
                    if (!p.Offline)
                    {
                        SelectedProject = p.Name;
                        SelectedProjectName = p.PrettyName;
                        GameReleaseNotesButtonVisible = p.Installed ? GameReleaseNotesButtonPermitted : false;
                        return p;
                    }
                    else
                    {
                        if (offline == null)
                        {
                            offline = p;
                        }
                    }
                }
                SelectedProject = offline.Name;
                SelectedProjectName = offline.PrettyName;
                GameReleaseNotesButtonVisible = offline.Installed ? GameReleaseNotesButtonPermitted : false;
                return offline;
            }

            // Not sure what to do, assume we have no products and wait for
            // someone to come up with a better solution again.
            GameReleaseNotesButtonVisible = false;
            return null;
        }

        public class ProjectInfo : IComparable<ProjectInfo>
        {
            public String Name;
            public String Reference;
            public String SortKey;
            public bool Installed = false;

            public int CompareTo(ProjectInfo info)
            {
                if (info.SortKey != SortKey)
                {
                    return SortKey.CompareTo(info.SortKey);
                }
                if (info.Name != Name)
                {
                    return Name.CompareTo(info.Name);
                }
                return 0;
            }
        }

        public IEnumerable<ProjectInfo> AvailableProjects()
        {
            List<ProjectInfo> available = new List<ProjectInfo>();
            m_manager.UpdateProjectList(true);
            Project[] projects = m_manager.AvailableProjects.GetProjectArray();

            foreach (Project p in projects)
            {
                ProjectInfo pi = new ProjectInfo();
                pi.Name = p.PrettyName;
                pi.Reference = p.Name;
                pi.SortKey = p.SortKey;
                pi.Installed = p.Installed;
                available.Add(pi);
            }

            available.Sort();

            return available;
        }

        /// <summary>
        /// Choose a project as the current project based on the server sort
        /// order.
        /// 
        /// First installed project if any.
        /// First uninstalled project the user has available.
        /// </summary>
        /// <returns></returns>
        public Project SelectFromAvailableProjects()
        {
            IEnumerable<ProjectInfo> available = AvailableProjects();
            Project[] projects = m_manager.AvailableProjects.GetProjectArray();

            Project selectedProject = null;

            if (!String.IsNullOrEmpty(m_autoSelection))
            {
                foreach (ProjectInfo pi in available)
                {
                    if (pi.Name.ToLowerInvariant().Contains(m_autoSelection))
                    {
                        foreach (Project p in projects)
                        {
                            if (p.Name == pi.Reference)
                            {
                                return p;
                            }
                        }
                    }
                }
            }

            foreach (ProjectInfo pi in available)
            {
                foreach (Project p in projects)
                {
                    if (p.Name == pi.Reference)
                    {
                        if (p.Installed)
                        {
                            return p;
                        }
                        else
                        {
                            if (selectedProject==null)
                            {
                                selectedProject = p;
                            }
                        }
                    }
                }
            }

            return selectedProject;
        }

        public void UpdateAuthorisation()
        {
            if (m_manager.Authorised != m_wasAuthorised)
            {
                LogEntry auth;
                if (m_manager.Authorised)
                {
                    auth = new LogEntry("ClientAuthorised");
                    auth.AddValue("user", m_manager.UserDetails.EmailAddress);
                    auth.AddValue("clientversion", CobraBayVersion);
                }
                else
                {
                    auth = new LogEntry("ClientAuthorisationRevoked");
                }
                m_manager.ServerConnection.LogValues(m_manager.UserDetails, auth);

                if (m_manager.Authorised)
                {
                    AuthorisationGranted();
                }
                m_wasAuthorised = m_manager.Authorised;
                // Reset the selected product so if an offline product was
                // selected previously we now prefer a real product.
                SelectedProject = SelectedProjectDefault;
            }
        }

        /// <summary>
        /// Tasks to carry out when authorisation is granted.
        /// 
        /// This is currently used to collection telemetry on the available
        /// IP addresses, specifically whether IPv4 and IPv6 addresses are
        /// available. This is collected for interest only as we want to 
        /// support what people are using, but the game is responsible for
        /// setting up the networking it uses and the launcher relies on the
        /// magic of DNS so should not care what protocol is used. If the
        /// process fails for any reason just give up and return. No telemetry
        /// will be collected from the user but it should work for the majority
        /// of users.
        /// 
        /// This extra error handling was added in response to reports that the
        /// launcher was crashing when the host name contained non-ascii
        /// characters (ED-91450). This should allow affected machines to
        /// play the game. The crash is a result of an issue with .NET, later
        /// versions do not crash so updating .NET seems to resolve the
        /// problem so this change just ensures that the players with
        /// unexpected host names and the affected .NET version can play.
        /// 
        /// Should a new issue arise that makes logging potentially useful the
        /// relevant code can be retrieved from the SVN history.
        /// </summary>
        private void AuthorisationGranted()
        {
            String hostName = null;

            try
            {
                hostName = Dns.GetHostName();
            }
            catch (Exception)
            {
                return;
            }

            bool hasIPv4 = false;
            bool hasIPv6 = false;

            if (hostName != null)
            {
                IPHostEntry hostDetails;
                try
                {
                    // Attempt to get the details for the host.
                    // This can fail when the host name contains unexpected
                    // characters, though later versions of .NET seem to work
                    // correctly.
                    hostDetails = Dns.GetHostEntry(hostName);
                }
                catch (Exception)
                {
                    try
                    {
                        // Assume we are in the case where we failed to get the
                        // entry because of unexpected characters, so get the
                        // entry for the local host.
                        hostDetails = Dns.GetHostEntry("localhost");
                        if (hostDetails.HostName != hostName)
                        {
                            // If the name of the found entry matches the
                            // host name we tried to look up we assume it is
                            // what we should have got back from GetHostEntry,
                            // if not give up.
                            return;
                        }
                    }
                    catch (Exception)
                    {
                        // Failed to get the local host entry either so give up.
                        return;
                    }

                }

                // If we got this far we should have the details for the host
                // so see what address families are supported.
                foreach (IPAddress curAdd in hostDetails.AddressList)
                {
                    if (curAdd.AddressFamily == AddressFamily.InterNetwork)
                    {
                        hasIPv4 = true;
                    }
                    else
                    {
                        if (curAdd.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            if (!curAdd.IsIPv6LinkLocal)
                            {
                                hasIPv6 = true;
                            }
                        }
                    }
                }
            }

            if (hasIPv4 || hasIPv6)
            {
                LogEntry addresses = new LogEntry("SupportedAddress");
                if (hasIPv4)
                {
                    addresses.AddValue("IPv4", "true");
                }
                if (hasIPv6)
                {
                    addresses.AddValue("IPv6", "true");
                }
                m_manager.Log(addresses);
            }
        }

        public void StartSelectedProject()
        {
            Project project = GetSelectedProject();
            if (!IsAlreadyRunning(project))
            {
                bool start = true;
#if DEVELOPMENT
                if ((m_manager.IsReleaseBuild) || ((!DeveloperIgnoreUpdates) && (!project.IgnoreUpdates) && (project.SortKey!="1EX")))
#endif
                {
                    if ((!project.Offline) && (!m_manager.OculusEnabled))
                    {
                        // This would allow the check to pass automatically
                        // by the user turning on online mode, however the
                        // necessary options will not be passed to the game
                        // so it will fail to start anyway.
                        // Also do not run in Oculus mode since the executable
                        // is different so we would replace the game with the
                        // non-Oculus executable.
                        start = m_manager.PreFlightCheck(project);
                    }
                }

                if ((m_manager.OculusEnabled) || (!m_manager.IsReleaseBuild && (DeveloperIgnoreUpdates || project.IgnoreUpdates || (project.SortKey=="1EX"))))
                {
                    // If we are not a release build, and the ignore updates
                    // option is enabled use whatever the current executable
                    // file hash as the expected hash.
                    DecoderRing ring = new DecoderRing();
                    long length;
                    project.ExecutableHash = ring.SHA1EncodeFile(project.ExecutablePath, out length);
                }


                if (start)
                {
                    LogEntry sg = new LogEntry("ProjectStarted");
                    sg.AddValue("project", project.Name);
                    m_manager.ServerConnection.LogValues(m_manager.UserDetails, sg);
                    String gameLanguage = LocalResources.Properties.Resources.GameLanguage;
                    if (gameLanguage == "#")
                    {
                        gameLanguage = null;
                    }
                    m_manager.SetRunLanguage(gameLanguage);

                    String message = m_manager.Run(project, RunnerCompleted);
                    if (message != null)
                    {
                        m_ui.WarningMessage(message, project.Name);
                    }
                }
                else
                {
                    LogEntry pfc = new LogEntry("PFCFail");
                    pfc.AddValue("project", project.Name);
                    m_manager.ServerConnection.LogValues(m_manager.UserDetails, pfc);
                }
                m_ui.MarkForUpdate();
            }
        }

                /// <summary>
        /// Determine if the project is already running.
        /// </summary>
        /// <param name="project">
        /// Project to test if the named startup executable is already running.
        /// </param>
        /// <returns>True if running false otherwise.</returns>
        public bool IsAlreadyRunning(Project project)
        {
            Process[] processList = Process.GetProcesses();

            foreach (Process process in processList)
            {
                try
                {
                    if (process.MainModule.FileName == project.ExecutablePath)
                    {
                        m_ui.MoveToFront(process);
                        return true;
                    }
                }
                catch (System.Exception)
                {
                }
            }
            return false;
        }

        /// <summary>
        /// Event handler for event fired when the started project exists.
        /// </summary>
        /// <param name="sender">Unused</param>
        /// <param name="e">Unused</param>
        public void RunnerCompleted(object sender, ProjectRunner.ProjectCompletedEventArgs e)
        {
            LogEntry sg = new LogEntry("ProjectFinished");
            Project project = DetermineSelectedProject();
            if (project != null)
            {
                sg.AddValue("project", project.Name);
            }
            m_manager.ServerConnection.LogValues(m_manager.UserDetails, sg);
            ResetDMCache();
            if (m_manager.IsEpic || (m_manager.IsAutoRun && m_manager.IsAutoQuit))
            {
                m_shouldExit = true;
            }
            m_ui.MarkForUpdate();
        }

        /// <summary>
        /// Reset the cache used to reduce the network impact of the launcher.
        /// 
        /// This needs to be public so the UI can force a reset if required,
        /// for example when checking for updates.
        /// </summary>
        public void ResetDMCache()
        {
            DownloadManagerBase dm = m_manager.ServerConnection.GetDownloadManager();
            if (dm != null)
            {
                dm.ResetCache();
            }
        }

        public void InstallFromPath(String installerPath)
        {
            if (!File.Exists(installerPath))
            {
                m_ui.ErrorMessage(String.Format(LocalResources.Properties.Resources.SelectInstallerFailedDetails, installerPath),
                    LocalResources.Properties.Resources.SelectInstallerFailedTitle);
            }
            else
            {
                // TODO: Create an alternate server that can be used to download
                //       the installer as if it was the real thing
                DownloadManagerBase downloader = new DownloadManagerSingleFile(installerPath);

                CommonDownload(downloader);
            }
        }

        public String UpdateCheckMessage(bool ignore)
        {
            String message = null;
            Project p = GetSelectedProject();
            if (p != null)
            {
                switch (p.Action)
                {
                    case Project.ActionType.Disabled:
                        {
                            message = LocalResources.Properties.Resources.UpdateCheckDisabled;
                            break;
                        }
                    case Project.ActionType.Invalid:
                    case Project.ActionType.Install:
                        {
                            message = LocalResources.Properties.Resources.UpdateCheckInstall;
                            break;
                        }
                    case Project.ActionType.Play:
                        {
                            message = LocalResources.Properties.Resources.UpdateCheckPlay;
                            break;
                        }
                    case Project.ActionType.Update:
                        {
                            message = LocalResources.Properties.Resources.UpdateCheckUpdate;
                            if (ignore)
                            {
                                message += LocalResources.Properties.Resources.UpdateCheckUpdateDev;
                            }
                            break;
                        }
                }
            }
            return message;
        }

        public void OpenPurchaseLink()
        {
            Start(LocalResources.Properties.Resources.PurchaseLink);
        }

        public String GetRegistrationLink()
        {
            return m_manager.GetRegistrationLink(LocalResources.Properties.Resources.RegisterLink);
        }
        public void OpenRegisterLink()
        {
            m_manager.StartRegistration(LocalResources.Properties.Resources.RegisterLink);
        }

        public void OpenDirectXLink()
        {
            Start(LocalResources.Properties.Resources.DX_Link);
        }

        private void Start(String target)
        {
            try
            {
                Process.Start(target);
            }
            catch (System.Exception)
            {
            }
        }

        class ProcessCallBack
        {
            public Project m_project;
            public DownloadManagerBase m_downloader;
            public ProcessCallBack(Project project, DownloadManagerBase downloader)
            {
                m_project = project;
                m_downloader = downloader;
            }
        }

        /// <summary>
        /// The common download behaviour for installing a fresh version or
        /// upgrading an existing version.
        /// </summary>
        public void CommonDownload(DownloadManagerBase downloader = null)
        {
            Project project = GetSelectedProject();
            Monitor = new ProjectProgressMonitor(project);
            Monitor.PropertyChanged += Monitor_PropertyChanged;
            project.Action = Project.ActionType.Disabled;
            m_ui.MarkForUpdate();
            ProcessCallBack delayStart = new ProcessCallBack(project, downloader);
            // Remove the timer as it is causing the download start to be
            // unreliable.
            //Timer delay = new Timer(TimerTriggered, delayStart,1000,Timeout.Infinite);
            TimerTriggered(delayStart);
            m_ui.MarkForUpdate();
        }

        public void TimerTriggered(Object data)
        {
            ProcessCallBack pcb = data as ProcessCallBack;
            if (pcb != null)
            {
                ClientSupport.ProjectUpdater.UpdateOptions options = new ClientSupport.ProjectUpdater.UpdateOptions();

                options.DisableFastDownload = DisableFastDownload;

                m_manager.DownloadAndInstallForProject(pcb.m_project, Monitor, pcb.m_downloader, options);
                m_ui.MarkForUpdate();
            }
#if DEVELOPMENT
            else
            {
                m_ui.ErrorMessage("Update Timer Triggered with no callback", "InternalError");
            }
#endif
        }

        public void Upgrade()
        {
            CommonDownload();
        }

        public void Install()
        {
            CommonDownload();
        }

        public void UninstallSelectedProduct()
        {
            Project project = GetSelectedProject();
            if (project == null)
            {
                return;
            }

            String title = LocalResources.Properties.Resources.UninstallWarningTitle;
            String warning = LocalResources.Properties.Resources.UninstallWarning;

            bool douninstall = m_ui.YesNoQuery(warning, title);

            if (douninstall)
            {
                Monitor = new ProjectProgressMonitor(project);
                Monitor.PropertyChanged += UninstallCompleted;
                Monitor.PropertyChanged += Monitor_PropertyChanged;
                //ProgressBarGrid.Visibility = Visibility.Visible;
                //WorkingButton.Visibility = Visibility.Visible;
                m_manager.UninstallForProject(project, Monitor, true);
                ResetDMCache();
            }
            m_ui.MarkForUpdate();
        }

        public void CancelProgress()
        {
            if ((Monitor != null) && (Monitor.RequestCancellation == false))
            {
                bool cancel = m_ui.YesNoQuery(LocalResources.Properties.Resources.CancelDownloadDetails, LocalResources.Properties.Resources.CancelDownloadTitle);
                if (cancel)
                {
                    // Recheck since the the action may have completed while
                    // the user was pondering the question.
                    if ((Monitor != null) && (Monitor.RequestCancellation == false))
                    {
                        Monitor.RequestCancellation = true;
                    }
                }
            }
        }

        /// <summary>
        /// Track the property changed event for properties we are interested
        /// in but do not have their own events.
        /// 
        /// Since the property change events may occur on threads other than
        /// the UI thread any work required is invoked rather than called
        /// directly.
        /// </summary>
        /// <param name="sender">Source of the event.</param>
        /// <param name="e">Contains the name of the changed property.</param>
        public void Monitor_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "HasCompleted")
            {
                MonitorCompleted();
            }
            if (Monitor != null)
            {
                if (sender == Monitor)
                {
                    if (e.PropertyName == "CanCancel")
                    {
                        MonitorUpdateCancel();
                    }
                }
            }
        }

        /// <summary>
        /// Log completion of an uninstall operation.
        /// </summary>
        /// <param name="sender">source of the change.</param>
        /// <param name="e">Check for name of changed property.</param>
        public void UninstallCompleted(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "HasCompleted")
            {
                LogEntry entry = new LogEntry("UninstallProjectCompleted");
                entry.AddValue("project", Monitor.Project.Name);
                m_manager.Log(entry);
            }
        }

        /// <summary>
        /// The sequence of actions being monitored has completed so hide the
        /// associated UI, discard the monitor, and report any failure
        /// messages.
        /// </summary>
        public void MonitorCompleted()
        {
            if (Monitor != null)
            {
                ResetDMCache();
                String failureResponse = Monitor.Action;
                m_ui.MonitorCompleted();

                if (!String.IsNullOrEmpty(failureResponse))
                {
                    m_ui.PopupMessage(failureResponse);
                }
            }
        }

        /// <summary>
        /// The CanCancel state of the monitor has changed so update the UI
        /// accordingly.
        /// </summary>
        public void MonitorUpdateCancel()
        {
            if (Monitor != null)
            {
                m_ui.ShowMonitorCancelButton(Monitor.CanCancel);
            }
        }

        /// <summary>
        /// The user has requested a log out.
        /// </summary>
        public void LogoutUser()
        {
            m_logOutManager.LogOutUser(m_manager, Monitor, m_ui);
            m_ui.MarkForUpdate();
        }

        /// <summary>
        /// The user has specified a log out for the machine.
        /// </summary>
        public void LogoutMachine()
        {
            m_logOutManager.LogOutMachine(m_manager, Monitor, m_ui);
            m_ui.MarkForUpdate();
        }

        /// <summary>
        /// The main window has been closed and the application is exiting.
        /// </summary>
        public void Closed()
        {
            Properties.Settings.Default.Save();
            m_manager.ServerConnection.LogValues(m_manager.UserDetails, new LogEntry("Closing")); ;
        }

        private String m_crashUploadFile = null;
        public String CrashUploadFileName
        {
            get
            {
                return m_crashUploadFile;
            }
            set
            {
                if (value != m_crashUploadFile)
                {
                    m_crashUploadFile = value;
                    RaisePropertyChanged("CrashUploadFileName");
                    CrashUploadEnabled = m_crashUploadFile != null;
                }
            }
        }

        public void SetCrashUploadFileName(String path)
        {
            if (File.Exists(path))
            {
                CrashUploadFileName = path;
            }
            else
            {
                CrashUploadFileName = null;
            }
        }

        private bool m_crashUploadFileEnabled = false;
        public bool CrashUploadEnabled
        {
            get
            {
                return m_crashUploadFileEnabled;
            }
            set
            {
                if (m_crashUploadFileEnabled != value)
                {
                    m_crashUploadFileEnabled = value;
                    RaisePropertyChanged("CrashUploadEnabled");
                }
            }
        }

        public class ManifestInfo
        {
            public String Description;
            public String Name;
        }

        public IEnumerable<ManifestInfo> GetManifestContents(String path)
        {
            if (File.Exists(path))
            {
                List<ManifestInfo> availableManifests = new List<ManifestInfo>();

                String fileStore = System.IO.Path.GetDirectoryName(path);
                String manifestDir = System.IO.Path.Combine(fileStore, "manifest");

                if (Directory.Exists(manifestDir))
                {
                    String[] manifests = Directory.GetFiles(manifestDir, "manifest_*.xml*");

                    foreach (String manifest in manifests)
                    {
                        String manifestFile = System.IO.Path.GetFileName(manifest);
                        String manifestLower = manifestFile.ToLowerInvariant();
                        String name;
                        if (manifestLower.EndsWith(".xml"))
                        {
                            name = manifestFile.Substring(0, manifestFile.Length - 4);
                        }
                        else
                        {
                            if (manifestLower.EndsWith(".xml.gz"))
                            {
                                name = manifestFile.Substring(0, manifestFile.Length - 7) + " (gz)";
                            }
                            else
                            {
                                name = manifestFile;
                            }
                        }
                        ManifestFile mf = new ManifestFile();
                        try
                        {
                            if (mf.LoadFile(manifest))
                            {
                                if (!String.IsNullOrEmpty(mf.ProductTitle))
                                {
                                    name = mf.ProductTitle + " [" + name + "]";
                                }
                                ManifestInfo mi = new ManifestInfo();
                                mi.Description = name;
                                mi.Name = manifest;
                                availableManifests.Add(mi);
                            }
                        }
                        catch (System.Exception)
                        {
                            // Failed to load the manifest so just pretend
                            // it is not there.
                        }
                    }
                }
                if (availableManifests.Count > 0)
                {
                    return availableManifests;
                }
            }
            return null;
        }

        private bool IsSlow(String path)
        {
            try
            {
                String root = Path.GetPathRoot(path);

                if (root.StartsWith(@"\\\\"))
                {
                    return true;
                }

                DriveInfo drive = new DriveInfo(root);
                if ((drive.DriveType == DriveType.Network) ||
                    (drive.DriveType == DriveType.CDRom))
                {
                    return true;
                }
            }
            catch (System.Exception)
            {
                
            }
            return false;
        }

        public void InstallFromManifest(String manifestPath)
        {
            String manifestDir = System.IO.Path.GetDirectoryName(manifestPath);
            String fileStore = System.IO.Path.GetDirectoryName(manifestDir);

            DownloadManagerFileStore dmfs = new DownloadManagerFileStore();
            if (IsSlow(fileStore))
            {
                dmfs.SetSerialiseAccess();
            }
            dmfs.SetFileStore(fileStore);
            dmfs.SetActiveManifest(System.IO.Path.GetFileName(manifestPath));

            CommonDownload(dmfs);
        }

        public void PerformVersionCheck()
        {
            Project p = GetSelectedProject();
            if (p == null)
            {
                return;
            }

            DriverVersionCheck dvc = new DriverVersionCheck(p);

            DriverVersionCheckResult dvcr;

            while ((dvcr=dvc.Next())!=null)
            {
                m_ui.PopupMessage(dvcr.Message);			
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
        public void RunHardwareSurvey( bool _allowUpload, bool _waitForFinish )
        {
            if ( m_manager != null )
            {
                m_manager.RunHardwareSurvey( _allowUpload, _waitForFinish );
            }
        }

        /// <summary>
        /// Returns the current server status for the given project
        /// </summary>
        /// <param name="_serverStatusText">An out parameter, the server status text in the current language, can be null</param>
        /// <param name="_serverMessage">An out parameter, server state message in the current language, can be null</param>
        /// <returns>An ID representing the server state, -1 = NOT OKAY, 0 = Maintainance, 1 = OK</returns>
        public int GetServerStatus( out string _serverStatusText, out string _serverMessage )
        {
            Debug.Assert( m_manager != null );

            int currentServerState = -1;
            _serverMessage = null;
            _serverStatusText = null;

            if ( m_manager != null )
            {
                Project project = GetActiveProject();

                if ( project != null )
                {
                    ServerInterface serverInterface = m_manager.ServerConnection;
                    Debug.Assert( serverInterface != null );

                    if ( serverInterface != null )
                    {
                        currentServerState = serverInterface.GetServerStatus( project, out _serverStatusText, out _serverMessage );
                    }
                }
            }

            return currentServerState;
        }

        /// <summary>
        /// Returns the product server URL to use to allow users to
        /// purchase items
        /// </summary>
        /// <returns>The product server URL, this can be null</returns>
        public string GetProductServer()
        {
            string productServer = null;

            if ( m_manager != null )
            {
                productServer = m_manager.GetProductServer();
            }

            return productServer;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(String property)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }

    }
}
