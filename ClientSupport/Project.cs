using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

using LocalResources;

namespace ClientSupport
{
	/// <summary>
	/// Represent an individual project properties.
	/// </summary>
	[System.Runtime.InteropServices.Guid("A064F811-BF3D-430A-A1EC-132EB771FAE7")]
	public class Project : IComparable<Project>
    {
        /// <summary>
        /// Installer types.
        /// </summary>
        public enum InstallerType { Executable, Manifest, None };

        /// <summary>
        /// Version information for the installed version of a project.
        /// </summary>
        private class VersionInfo
        {
            /// <summary>
            /// Version string
            /// </summary>
            public String version;
            /// <summary>
            /// Link to the project store page.
            /// </summary>
            public String store;
            /// <summary>
            /// Link to the project support page.
            /// </summary>
            public String support;
            /// <summary>
            /// Link to the project news feed.
            /// </summary>
            public String newsfeed;
            /// <summary>
            /// Executable file to run.
            /// </summary>
            public String executable;
            /// <summary>
            /// Additional arguments to pass to the executable.
            /// </summary>
            public String arguments;
            /// <summary>
            /// User friendly version of the name.
            /// </summary>
            public String name;
			/// <summary>
			/// Representation of supported video card driver versions.
			/// </summary>
			public String videoversion;
            /// <summary>
            /// Can the project be run offline, i.e. without logging in.
            /// </summary>
            public bool offline;

            public bool steamaware;

            public bool ignoreupdates;

			public bool useWatchDog64;

            public VersionInfo()
            {
                version = null;
                store = null;
                support = null;
                newsfeed = null;
                executable = null;
                arguments = null;
                name = null;
				videoversion = null;
                offline = false;
                steamaware = true;
                ignoreupdates = false;
				useWatchDog64 = false;
            }
        }

        /// <summary>
        /// Details of the installer to use to perform an uninstall if
        /// requested.
        /// </summary>
        public class InstallerDetails
        {
            /// <summary>
            /// Path to the most recent installer used.
            /// </summary>
            public String InstallerPath;
            /// <summary>
            /// Arguments to pass to the installer to remove the project.
            /// </summary>
            public String UninstallArgs;
            public InstallerDetails()
            {
                InstallerPath = null;
                UninstallArgs = null;
            }
        }

        /// <summary>
        /// The actions that can be performed on the project.
        /// 
        /// Disabled is used to indicate that an action is already in progress
        /// and no new action should be started until the existing action has
        /// completed.
        /// </summary>
        public enum ActionType
        {
            Install,
            Update,
            Play,
            Disabled,
			Invalid
        }



        /// <summary>
        /// The name of the project (internal 'safe' name used as a key/path)
        /// </summary>
        private String m_name;
        public String Name { get { return m_name; } }

        public Boolean isArenaVersion()
        {
            return string.Equals(this.Name, "FORC-FDEV-D-1012", StringComparison.OrdinalIgnoreCase);
        }

        public Boolean isCombatVersion()
        {
            return string.Equals(this.Name, "COMBAT_TUTORIAL_DEMO", StringComparison.OrdinalIgnoreCase);
        }

        private bool m_isRedirected = false;
        public bool IsRedirected { get { return m_isRedirected; } }
        private String m_redirection = null;
        public String Redirection { get { return m_redirection; } }

        /// <summary>
        /// Determines whether the default view will be loaded(true) or the one with featured products(false)
        /// </summary>
        public bool m_noDetails;
        public bool NoDetails 
        { 
            get { return m_noDetails; }

            set
            {
                if (m_noDetails != value)
                {
                    m_noDetails = value;
                }
            }
        }

        private bool m_canMove = false;
        public bool CanMove { get { return m_canMove; } }

		/// <summary>
		/// This is an application wide setting as it is dependent on whether
		/// the launcher handles the update or an external application.
		/// </summary>
        public static bool ExternalUpdate = false;

        private String m_projectRootDirectory;
        /// <summary>
        /// The directory containing the project specific files.
        /// </summary>
        private String m_projectDirectory;
        public String ProjectDirectory
        {
            get { return m_projectDirectory; }
            set
            {
				// Evilness ensues.
				//
				// We do not want to force people with an existing directory
				// to reinstall, since this would create a second copy of the
				// data (avoiding the download of most of the files) and
				// provide no way to remove the old data. Therefore we check
				// to see if the original location already exists, if so use
				// that as before.
				// This can however be a problem on Steam as the game is
				// installed into the shared location so if there happens to be
				// a old folder it will be found, but there will be no way to
				// update it as the 'update via Steam' message will be shown.
				// See https://jira.corp.frontier.co.uk/browse/ED-110242
				// Therefore we provide a property (SKUFirst) to control the
				// order in which we consider directories.
				String primaryName = ExternalUpdate ? value : m_name;
				String secondaryName = ExternalUpdate ? m_name : value;
				String potential = Path.Combine(m_projectRootDirectory, ValidFileName(primaryName));
                potential = Redirect(potential);
                if (!Directory.Exists(potential))
                {
                    // No existing directory so fall back to using the one we
                    // are given the name of
                    potential = Path.Combine(m_projectRootDirectory, ValidFileName(secondaryName));
                    potential = Redirect(potential);
                }
                m_canMove = !Directory.Exists(potential);
				if (m_projectDirectory!=potential)
				{
					m_projectDirectory = potential;
					// Project location has changed to rescan new location.
					Update();
				}
			}
		}

		private String ValidFileName(String name)
		{
			String valid = name;
			foreach (char c in Path.GetInvalidFileNameChars())
			{
				valid = valid.Replace(c.ToString(), "_");
			}
			return valid;
		}

        /// <summary>
        /// Handle redirection of the expected path to a different location.
        /// 
        /// This is an attempt to
        /// a) allow custom install locations for different products.
        /// b) work around the long path name issue for Oculus/Steam users.
        /// 
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private String Redirect(String path)
        {
            m_redirection = null;
            m_isRedirected = false;

            // If the path exists and is a directory then use it normally.
            if (Directory.Exists(path))
            {
                return path;
            }

            // If a redirection file exists then use the redirected path.
            String redirectionPath = path + ".rdr";
            m_redirection = redirectionPath;
            if (File.Exists(redirectionPath))
            {
                String alternateRoot = LoadRedirection(redirectionPath);
                if (alternateRoot != null)
                {
                    String node = Path.GetFileName(path);
                    m_isRedirected = true;
                    return Path.Combine(alternateRoot, node);
                }
            }

#if DEVELOPMENT
            // For development builds we allow a root redirect which allows the
            // products to be stored outside of the application directory so
            // they can be shared between different builds, and potentially
            // branches.

            // Project level redirection is handled first, and global
            // redirection does not count as being redirected as far as the
            // UI is concerned.
            String drive = Path.GetPathRoot(path);
            String developmentRedirect = Path.Combine(drive, "CobraBay.rdr");
            if (File.Exists(developmentRedirect))
            {
                String alternateRoot = LoadRedirection(developmentRedirect);
                if (alternateRoot!=null)
                {
                    String node = Path.GetFileName(path);
                    return Path.Combine(alternateRoot, node);
                }
            }
#endif
            return path;
        }

        private String LoadRedirection(String path)
        {
            try
            {
                String[] content = File.ReadAllLines(path);
                foreach (String l in content)
                {
                    String newpath = l.Trim();
                    if (!String.IsNullOrEmpty(newpath))
                    {
                        return newpath;
                    }
                }
                return null;
            }
            catch
            {
                // some sort of error so treat as empty.
                return null;
            }
        }

		public int MaxDownloadThreads;

        /// <summary>
        /// Directory used to hold local copies of installers downloaded from
        /// the server.
        /// </summary>
        public String InstallationDirectory
        {
            get
            {
                return Path.Combine(m_projectDirectory, "Installers");
            }
        }

        /// <summary>
        /// Version Information.
        /// 
        /// Only used internally. This class provides accessors for the UI to
        /// bind to to present the information to the user.
        /// </summary>
        private VersionInfo m_version;

        /// <summary>
        /// Is the project actually installed.
        /// </summary>
        public bool Installed { get { return m_version.version!=null; } }

        /// <summary>
        /// Version string, or null if project is not installed.
        /// </summary>
        public String Version
        {
            get
            {
                return m_version.version;
            }
        }

        private String m_serverVersion;
        /// <summary>
        /// Set the latest version known on the server.
        /// </summary>
        public String ServerVersion
        {
            get
            {
                return m_serverVersion;
            }
            set
            {
                if (m_serverVersion != value)
                {
                    m_serverVersion = value;
                }
            }
        }

        /// <summary>
        /// Version string, or an alternative message where the project is not
        /// installed.
        /// 
        /// Ideally this would be done in XAML using the main Version value
        /// rather than code behind, but XAML does not support does not equal
        /// for data triggers which requires using a value converter and
        /// code behind anyway. We probably need a better strategy by the time
        /// we come to localise.
        /// </summary>
        public String PrettyVersion
        {
            get
            {
                String pv;
                if (m_version.version == null)
                {
                    pv = LocalResources.Properties.Resources.NotInstalled;
                }
                else
                {
                    pv = m_version.version;
                }
                if (!String.IsNullOrEmpty(m_serverVersion))
                {
                    if (m_serverVersion!=pv)
                    {
                        pv += String.Format(LocalResources.Properties.Resources.Project_ServerVersionSuffix,
                            m_serverVersion );
                    }
                }
                if (m_version.version!=null)
                {
                    pv = String.Format(LocalResources.Properties.Resources.Project_VersionFormat, pv);
                }
                return pv;
            }
        }

        /// <summary>
        /// Path of the project executable to run to start the project, or
        /// null i fthe project is not installed/has no executable.
        /// </summary>
        public String ExecutablePath
        {
            get
            {
                if (m_version.version == null)
                {
                    return null;
                }
                if (m_version.executable == null)
                {
                    return null;
                }
                return Path.Combine(m_projectDirectory, m_version.executable);
            }
        }

        private String m_executableHash = null;
        public String ExecutableHash
        {
            get
            {
                if (m_version.version == null)
                {
                    return null;
                }
                if (m_version.executable == null)
                {
                    return null;
                }
                return m_executableHash;
            }
            set
            {
                if (m_executableHash != value)
                {
                    m_executableHash = value;
                }
            }
        }

        /// <summary>
        /// Arguments to use when running the executable, or null if there
        /// are no arguments configured.
        /// </summary>
        public String Arguments
        {
            get
            {
                if (m_version.version == null)
                {
                    return null;
                }
                if (m_version.arguments == null)
                {
                    return null;
                }
                return m_version.arguments;
            }
        }

		public String VideoVersion
		{
			get
			{
				if (m_version.version == null)
				{
					return null;
				}
				return m_version.videoversion;
			}
		}

        private InstallerType m_installerType = InstallerType.None;
        public InstallerType RemoteInstallerType { get { return m_installerType; } }

        /// <summary>
        /// Links to project specific pages on the website.
        /// </summary>
        public ExternalLink StorePage = new ExternalLink();
        public ExternalLink SupportPage = new ExternalLink();
        public ExternalLink NewsFeed = new ExternalLink();

        /// <summary>
        /// The next action to perform on the project.
        /// </summary>
        public ActionType Action { get; set; }

        /// <summary>
        /// Return the name of the project for displaying to the user,
        /// for example when selecting projects.
        /// </summary>
        private String m_prettyName = null;
        public String PrettyName
        {
            get
            {
                if (m_prettyName != null)
                {
                    return m_prettyName;
                }
                if (m_version.name != null)
                {
                    return m_version.name;
                }
                return Name;
            }
            set
            {
                if (value!=Name)
                {
                    m_prettyName = value;
                }
            }
        }

        public bool Offline { get { return m_version.offline; } }

        public bool SteamAware { get { return m_version.steamaware; } }

		public bool UseWatchDog64 { get { return m_version.useWatchDog64; } }

        public bool IgnoreUpdates { get { return m_version.ignoreupdates; } }

        private String m_sortKey;
        public String SortKey
        {
            get
            {
                return m_sortKey;
            }
            set
            {
                if (m_sortKey != value)
                {
                    m_sortKey = value;
                }
            }
        }

		private String m_highlight = null; 
		public String Highlight
		{
			get
			{
				return m_highlight;
			}
			set
			{
				if (m_highlight != value)
				{
					m_highlight = value;
				}
			}
		}

		private String m_page = null;
		public String Page
		{
			get
			{
				return m_page;
			}
			set
			{
				if (m_page != value)
				{
					m_page = value;
				}
			}
		}

        /// <summary>
        /// The BoxURI
        /// </summary>
        private String m_boxImageURI = null;
        /// <summary>
        /// Set/get the box URI
        /// </summary>
        public String BoxImageURI
        {
            get
            {
                return m_boxImageURI;
            }
            set
            {
                if ( m_boxImageURI != value )
                {
                    m_boxImageURI = value;
                }
            }
        }

        /// <summary>
        /// The Hero URI
        /// </summary>
        private String m_heroImageURI = null;
        /// <summary>
        /// Set/get the Hero URI
        /// </summary>
        public String HeroImageURI
        {
            get
            {
                return m_heroImageURI;
            }
            set
            {
                if ( m_heroImageURI != value )
                {
                    m_heroImageURI = value;
                }
            }
        }

        /// <summary>
        /// The Logo (product) URI
        /// </summary>
        private String m_logoImageURI = null;
        /// <summary>
        /// Set/get the Logo (product) URI
        /// </summary>
        public String LogoImageURI
        {
            get
            {
                return m_logoImageURI;
            }
            set
            {
                if ( m_logoImageURI != value )
                {
                    m_logoImageURI = value;
                }
            }
        }

        /// <summary>
        /// The ESRB rating
        /// </summary>
        private String m_esrbRating = null;
        /// <summary>
        /// Set/get the ESRB rating (e.g. teen)
        /// </summary>
        public String ESRBRating
        {
            get
            {
                return m_esrbRating;
            }
            set
            {
                if ( m_esrbRating != value )
                {
                    m_esrbRating = value;
                }
            }
        }

        /// <summary>
        /// The PEGI rating
        /// </summary>
        private String m_pegiRating = null;
        /// <summary>
        /// Set/get the PEGI rating (e.g. 7)
        /// </summary>
        public String PEGIRating
        {
            get
            {
                return m_pegiRating;
            }
            set
            {
                if ( m_pegiRating != value )
                {
                    m_pegiRating = value;
                }
            }
        }

        /// <summary>
        /// The Game API for this project
        /// </summary>
        private String m_gameApi = null;
        /// <summary>
        /// Set/get the Game API
        /// </summary>
        public String GameApi
        {
            get
            {
                return m_gameApi;
            }
            set
            {
                if ( m_gameApi != value )
                {
                    m_gameApi = value;
                }
            }
        }

        /// <summary>
        /// The Game Code for this project
        /// </summary>
        private int m_gameCode = 0;
        /// <summary>
        /// Set/get the Game Code
        /// </summary>
        public int GameCode
        {
            get
            {
                return m_gameCode;
            }
            set
            {
                if ( m_gameCode != value )
                {
                    m_gameCode = value;
                }
            }
        }

        private String[] m_filters = null;
		public String[] Filters
		{
			get
			{
				return m_filters;
			}
			set
			{
				if (m_filters != value)
				{
					m_filters = value;
				}
			}
		}

        public Project(String name, String projectDirectory)
        {
            m_projectRootDirectory = projectDirectory;
            m_name = name;
            ProjectDirectory = name;
            MaxDownloadThreads = 0;
        }

        /// <summary>
        /// Update the project status to represent the current condition.
        /// </summary>
        public void Update()
        {
            Action = ActionType.Install;
            bool installed = Directory.Exists(m_projectDirectory);
            m_version = new VersionInfo();
            StorePage.URL = LocalResources.Properties.Resources.Project_URLDefaultStore; // For now, everything links to the main page.
            SupportPage.URL = LocalResources.Properties.Resources.Project_URLDefaultSupport;
            NewsFeed.URL = LocalResources.Properties.Resources.Project_URLDefaultNews;
            if (installed)
            {
                ReadVersionInfo();
            }
        }

        /// <summary>
        /// Read the details of the project from the stored version info file.
        /// </summary>
        private void ReadVersionInfo()
        {
            String projectInfoFile = Path.Combine(m_projectDirectory, "VersionInfo.txt");
            if (File.Exists(projectInfoFile))
            {
                String content = File.ReadAllText(projectInfoFile);
                try
                {
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    m_version = js.Deserialize<VersionInfo>(content);
                    if (m_version.store != null)
                    {
                        StorePage.URL = m_version.store;
                    }
                    if (m_version.support != null)
                    {
                        SupportPage.URL = m_version.support;
                    }
                    if (m_version.newsfeed != null)
                    {
                        NewsFeed.URL = m_version.newsfeed;
                    }
                    Action = ActionType.Play;

                }
                catch (System.Exception)
                {
                }
            }
        }

        /// <summary>
        /// Mark the project as requiring an update as the server has a newer
        /// Version than the one currently installed.
        /// </summary>
        public void RequiresUpdate()
        {
            if (Action == ActionType.Play)
            {
                Action = ActionType.Update;
            }
        }

		/// <summary>
		/// Mark the project as not having a valid installer.
		/// </summary>
		public void InvalidInstaller()
		{
			Action = ActionType.Invalid;
		}

        public void SetRemoteInstallerType(String path)
        {
            String extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".exe")
            {
                m_installerType = InstallerType.Executable;
            }
            else
            {
                m_installerType = InstallerType.Manifest;
            }
        }

        /// <summary>
        /// A new version has been installed so store the information necessary
        /// to perform an uninstallation.
        /// </summary>
        /// <param name="path"></param>
        public void SetInstaller(String path)
        {
            Dictionary<String, object> installerInfo = new Dictionary<String, object>();

            String installerInfoPath = InstallerInfoPath();

            installerInfo["InstallerPath"] = path;
            installerInfo["UninstallArgs"] = "/x"; // Assume install shield uninstall for now.

            try
            {
                JavaScriptSerializer js = new JavaScriptSerializer();
                String result = js.Serialize(installerInfo);
                if (!String.IsNullOrEmpty(result))
                {
                    using (StreamWriter fp = new StreamWriter(InstallerInfoPath()))
                    {
                        fp.WriteLine(result);
                    }
                }
            }
            catch (System.Exception)
            {
            }

        }

        private InstallerType GetUninstallerDetails(out InstallerDetails details)
        {
            InstallerType executable = InstallerType.None;
            InstallerDetails uninstaller = new InstallerDetails();

            String installerPath = InstallerInfoPath();

            if (File.Exists(installerPath))
            {
                try
                {
                    String content = File.ReadAllText(installerPath);
                    JavaScriptSerializer js = new JavaScriptSerializer();
                    uninstaller = js.Deserialize<InstallerDetails>(content);
                }
                catch (System.Exception)
                {
                    installerPath = Path.Combine(m_projectDirectory, "unins000.exe");
                    if (File.Exists(installerPath))
                    {
                        uninstaller.InstallerPath = installerPath;
                    }
                }
            }

            if (!String.IsNullOrEmpty(uninstaller.InstallerPath))
            {
                String uninstallExtension = Path.GetExtension(uninstaller.InstallerPath);

                if (uninstallExtension == ".exe")
                {
                    // We installed using an executable, this was probably an
                    // INNOSetup installer in which case the uninstaller is actually
                    // a different file.
                    String innouninstaller = Path.Combine(m_projectDirectory, "unins000.exe");
                    if (File.Exists(innouninstaller))
                    {
                        // Uninstaller exists so use that, otherwise assume the
                        // installer also does the uninstall.
                        uninstaller.InstallerPath = innouninstaller;
                    }
                    executable = InstallerType.Executable;
                }
                else
                {
                    executable = InstallerType.Manifest;
                }
            }

            details = uninstaller;
            return executable;
        }

        /// <summary>
        /// Uninstall the project using the most recent installer.
        /// </summary>
        /// <returns>
        /// Error report if there was a problem with uninstall.
        /// </returns>
        public String Uninstall(bool silent)
        {
            InstallerDetails uninstaller;

            if (GetUninstallerDetails(out uninstaller)==InstallerType.Executable)
            {
                if (File.Exists(uninstaller.InstallerPath))
                {
                    try
                    {
                        ProcessStartInfo pstart = new ProcessStartInfo();
                        pstart.WorkingDirectory = ProjectDirectory;
                        pstart.FileName = uninstaller.InstallerPath;
                        pstart.UseShellExecute = false;
                        pstart.EnvironmentVariables.Add("TARGETDIRECTORY", ProjectDirectory);
                        pstart.Arguments = uninstaller.UninstallArgs;
                        if (silent && (uninstaller.UninstallArgs == "/x") && (Path.GetFileName(pstart.FileName).ToLowerInvariant() == "unins000.exe"))
                        {
                            pstart.Arguments += " /SILENT";
                        }

                        Process pid = Process.Start(pstart);
                        pid.WaitForExit();
                        if (pid.ExitCode > 1)
                        {
                            return String.Format(LocalResources.Properties.Resources.Project_UninstallFailureExitCode,
                                pid.ExitCode.ToString());
                        }
                        else
                        {
                            if (pid.ExitCode == 1)
                            {
                                return LocalResources.Properties.Resources.Project_UninstallCancelled;
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        return String.Format(LocalResources.Properties.Resources.Project_UninstallException,
                            ex.Message);
                    }
                }
            }

            // Remove everything in the project directory.
            // If this was a installer based install the [un]installer should
            // have unhooked any registry entries and deleted most of the files.
            // This call will just clean up any left overs.
            // If it was a manifest based install there were no registry entries
            // to remove, so just discard the files.
            String vi = Path.Combine(m_projectDirectory, "VersionInfo.txt");
            if (File.Exists(vi))
            {
                // Make sure we delete VersionInfo.txt file first if it exists,
                // that way we will always force a new install next time rather
                // than risking a cancellation part way through looking like a
                // complete game.
                File.Delete(vi);
            }

            return null;
        }

        public String UninstallIfRequired()
        {
            InstallerDetails uninstaller;

            InstallerType currentType = GetUninstallerDetails(out uninstaller);

            if ((currentType != RemoteInstallerType) && (currentType!=InstallerType.None))
            {
                return Uninstall(false);
            }
            return null;
        }

        /// <summary>
        /// Path to use to store details of the last installer run.
        /// </summary>
        /// <returns>Installer path.</returns>
        public String InstallerInfoPath()
        {
            return Path.Combine(m_projectDirectory, "InstallerInfo.txt");
        }

        /// <summary>
        /// Implements the IComparable intrface.
        /// Based on SortKey and Name. Allows projects to
        /// be listed in order based on name and sortkey.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>
        /// < 0  this instance precedes "other"
        /// 0    this instance is in the same position as "other"
        /// > 0  this instance follows the position of "other"
        /// </returns>
        public int CompareTo( Project other )
        {
            int resultingValue = 0;

            // First, sort om SortKey, then on Name
            if ( other.SortKey != SortKey )
            {
                resultingValue = SortKey.CompareTo( other.SortKey );
            }
            else if ( other.Name != Name )
            {
                resultingValue = Name.CompareTo( other.Name );
            }

            return resultingValue;
        }
    }
}
