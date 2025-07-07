using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

using CBViewModel;
using ClientSupport;
using FORCServerSupport;
using FrontierSupport;

namespace CobraBay
{
	/// <summary>
	/// Interaction logic for CobraBayWindow.xaml
	/// </summary>
	public partial class CobraBayWindow : Window, UserInterface, INotifyPropertyChanged
	{
		[DllImportAttribute("User32.dll")]
		private static extern IntPtr SetForegroundWindow(IntPtr hWnd);

		private CobraBayView m_view;
		public CobraBayView View { get { return m_view; } }
		BrowserInterface m_browser;
		private Suggestion m_suggestion;

		private WebBrowser OnDemandBrowser = null;

		private bool ForceSoftwareRendering = false;

		const int c_autoUpdate = 10 * 60 * 1000; // ten minutes in milliseconds.
		int m_nonbrowserHeight = 120;
		const int c_scrollBarAdjustment = 22;
		int m_titleBarHeight = 30;
		private Timer m_updateTimer;

        private DispatcherTimer m_forcExitStatusTimer;
        private TimeSpan m_forcExitStatusTimerInterval;

        private double m_heightLimit = -1;
		private double m_widthLimit = -1;
        
		private bool IsModal = false;
		private bool IsAwaitingRegistration = false;
        private bool IsClosing = false;

        public CobraBayWindow()
		{
			this.Loaded += OnLoaded;
			try
			{
				InitializeComponent();
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("Internal error creating window:\n\n" + ex.Message,
					"Application Failed To Start");
                CloseIfSafe();
			}
		}

		private void OnLoaded(object sender, EventArgs e)
		{
			if (m_view.m_manager.IsSteam)
			{
				ForceSoftwareRendering = true;
				HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;
				HwndTarget hwndTarget = hwndSource.CompositionTarget;
				hwndTarget.RenderMode = RenderMode.SoftwareOnly;
			}
		}

        /// <summary>
        /// Close the window if it is safe to do so.
        /// 
        /// All raw Close calls should be redirected here to ensure the
        /// launcher is a state where it can be safely closed down, e.g. not
        /// in the middle of a download.
        /// </summary>
        private void CloseIfSafe()
        {
            if (View.Monitor != null)
            {
                ErrorMessage(LocalResources.Properties.Resources.InstallationInProgress,
                    LocalResources.Properties.Resources.InstallationInProgressTitle);
            }
            else
            {
				if (IsAwaitingRegistration)
				{
					if (RegisterButton.IsVisible)
					{
						ErrorMessage(LocalResources.Properties.Resources.RegistrationInProgress,
							LocalResources.Properties.Resources.RegistrationInProgressTitle);
					}
					else
					{
						ErrorMessage(LocalResources.Properties.Resources.LinkInProgress,
							LocalResources.Properties.Resources.LinkInProgressTitle);
					}
					IsAwaitingRegistration = false;
				}
				else
				{
					if (m_suggestion != null)
					{
						m_suggestion.SuggestionEvent -= HandleSuggestion;
						m_suggestion.Stop();
					}
					Close();
				}
			}
		}

        private void OnInitialised(object sender, EventArgs e)
		{
			try
			{
                
#if DEVELOPMENT || DEBUG
            int changeInHeight = 100;
            this.Height += changeInHeight;
            ProductList.Height += changeInHeight;
            LauncherUIStackPanel.Height += changeInHeight;
            Canvas.SetTop(GameVersionGrid, Canvas.GetTop(GameVersionGrid) + changeInHeight);
#endif
				m_view = new CobraBayView(this);
				m_view.PropertyChanged += ViewPropertyChanged;
				m_browser = new BrowserInterface(this);

#if DEVELOPMENT
				ProcessCommandLine();
#endif
				ResizeIfRequired();

				DataContext = this;

				View.ResetManager();

				RequiresInteraction();

				UpdateBackground();

				WorkingButton.Visibility = Visibility.Collapsed;
				CancelProgressButton.Visibility = Visibility.Collapsed;

				WindowMoved(this, null);

				AddDeveloperMenu();

				CheckXInputDLL();


				Microsoft.Win32.SystemEvents.PowerModeChanged += this.PowerModeChanged;

				View.CheckLauncherVersion();

				Update();

				m_suggestion = new Suggestion("edlaunch_uss");
				m_suggestion.SuggestionEvent += HandleSuggestion;

				EnableHTMLView();


				View.CheckLauncherValid();

				AlternateDownloadItem.IsChecked = !View.DisableFastDownload;
				CacheCheckMenuItem.IsChecked = DownloadManagerLocalCache.EnableCache;
				VCacheCheckMenuItem.IsChecked = DownloadManagerLocalCache.EnableVirtualCache;
				DetailedUpdateLogItem.IsChecked = FORCManager.DetailedUpdateLog;
				DevIgnoreUpdates.IsChecked = false;

                UsePrivateTestServer.IsChecked = View.m_manager.UsePrivateServer;
                EnableGzipCompression.IsChecked = View.m_manager.EnableGzipCompression;

				m_updateTimer = new Timer(TimedUpdate, null, c_autoUpdate, c_autoUpdate);
                SetupForcExitStatusTimer();

			}
			catch (System.Exception ex)
			{
				ErrorMessage("Exception initialising application:\n\n" + ex.Message,
					"Error during application startup"
					);
                CloseIfSafe();
			}
		}

#if DEVELOPMENT
		void ProcessCommandLine()
		{
			String[] argv = Environment.GetCommandLineArgs();

			for(int arg = 1; arg<(argv.Length-1); ++arg)
			{
				String lc = argv[arg].ToLowerInvariant();
				if (lc == "/height")
				{
					if (!double.TryParse(argv[arg + 1], out m_heightLimit))
					{
						m_heightLimit = -1;
					}
				}
				if (lc == "/width")
				{
					if (!double.TryParse(argv[arg + 1], out m_widthLimit))
					{
						m_widthLimit = -1;
					}
				}
			}
		}
#endif

		void ResizeIfRequired()
		{
			m_titleBarHeight = (int)Canvas.GetTop(BodyCanvas);
			m_nonbrowserHeight = (int)(Height-Canvas.GetTop(LauncherUI));
			if (m_heightLimit < 0)
			{
				m_heightLimit = System.Windows.SystemParameters.WorkArea.Height;
			}
			if (m_widthLimit < 0)
			{
				m_widthLimit = System.Windows.SystemParameters.WorkArea.Width;
			}
			if (Height > m_heightLimit)
			{
				double browserHeight = Height - m_nonbrowserHeight;
				Height = m_heightLimit;

				// Decreasing the height requires a scroll bar on the browser
				// so we need to make some extra space for it.
				Width = Width + c_scrollBarAdjustment;
				Canvas.SetLeft(CloseButtonIcon, Canvas.GetLeft(CloseButtonIcon) + c_scrollBarAdjustment);
				Canvas.SetLeft(MinimiseButtonIcon, Canvas.GetLeft(MinimiseButtonIcon) + c_scrollBarAdjustment);
				browserHeight = Height - m_nonbrowserHeight;

				// How much has the LauncherUI moved?
				double change = Canvas.GetTop(LauncherUI);
				change = browserHeight - change;

				// Update Launcher UI position
				Canvas.SetTop(LauncherUI, browserHeight);

				// Relocate the window so the top does not get placed
				// off the screen.
				if (Top < System.Windows.SystemParameters.VirtualScreenTop)
				{
					Top = System.Windows.SystemParameters.VirtualScreenTop;
				}
			}
			if (Width > m_widthLimit)
			{
				double diff = Width - m_widthLimit;
				Width = m_widthLimit;
				Canvas.SetLeft(CloseButtonIcon, Canvas.GetLeft(CloseButtonIcon) - diff);
				Canvas.SetLeft(MinimiseButtonIcon, Canvas.GetLeft(MinimiseButtonIcon) - diff);
				if (Left < System.Windows.SystemParameters.VirtualScreenLeft)
				{
					Left = System.Windows.SystemParameters.VirtualScreenLeft;
				}
			}
        }

		/// <summary>
		/// Test whether interaction is required from the user before starting
		/// the game.
		/// 
		/// This will trigger if the game was not started automatically.
		/// </summary>
		private void RequiresInteraction()
		{
			if (m_view.m_manager.OculusEnabled)
			{
				String message = null;
				if (!View.m_manager.Authorised)
				{
					message = "login";
				}
				else
				{
					Project p = View.GetActiveProject();
					if (p != null)
					{
						if ((p.Action == Project.ActionType.Update) ||
							(p.Action == Project.ActionType.Install))
						{
							message = "upgrade";
						}
					}
				}
				if (message!=null)
				{
					Assembly an = Assembly.GetEntryAssembly();
					String prompt = System.IO.Path.GetDirectoryName(an.Location);
					prompt = System.IO.Path.Combine(prompt, "ORPrompt.exe");
					if (System.IO.File.Exists(prompt))
					{
						Process.Start(prompt, message);
					}
				}
			}
		}

		private void UpdateBackground()
		{
			// Relocate the background image to ensure the logo is
			// still visible.
			double imageHeight = BackgroundImage.Height;
			String sourceUri = "pack://application:,,,/EDLaunch;component/EliteImages/background-full-FRONTIER.jpg";
			if (View.m_manager!=null)
			{
				if (View.m_manager.IsSteam)
				{
					if (View.m_manager.IsReleaseBuild)
					{
						sourceUri = "pack://application:,,,/EDLaunch;component/EliteImages/launcher-background-FRONTIER-STEAM.jpg";
					}
					else
					{
						sourceUri = "pack://application:,,,/EDLaunch;component/EliteImages/launcher-background-FRONTIERDEV-STEAM.jpg";
					}
				} else if (View.m_manager.IsEpic)
                {
                    if (View.m_manager.IsReleaseBuild)
                    {
                        sourceUri = "pack://application:,,,/EDLaunch;component/EliteImages/launcher-background-FRONTIER-EPIC.jpg";
                    }
                    else
                    {
                        sourceUri = "pack://application:,,,/EDLaunch;component/EliteImages/launcher-background-FRONTIERDEV-EPIC.jpg";
                    }

                }
				else
				{
					if (!View.m_manager.IsReleaseBuild)
					{
						sourceUri = "pack://application:,,,/EDLaunch;component/EliteImages/launcher-background-FRONTIERDEV.jpg";
					}
				}
			}
			double availableHeight = Height - m_titleBarHeight;
			BitmapImage backgroundData = new BitmapImage(new Uri(sourceUri));
			CroppedBitmap cropped = new CroppedBitmap();
			cropped.BeginInit();
			cropped.Source = backgroundData;
			int imageOffset = 0;
			int imageTop = (int)(backgroundData.PixelHeight - availableHeight);
			int usableHeight = (int)availableHeight;
			if (imageTop < 0)
			{
				imageOffset = 0;
				imageTop = 0;
				usableHeight = backgroundData.PixelHeight;
			}
			cropped.SourceRect = new Int32Rect(0, imageTop, (int)backgroundData.PixelWidth, usableHeight);
			cropped.EndInit();
			BackgroundImage.Source = cropped;
			BackgroundImage.Height = availableHeight;
			double shift = (Width - backgroundData.PixelWidth)/2.0;
			if (shift < 0)
			{
				// Width is smaller than the width of the image so instead of
				// centering it, align it to the right edge since that has the
				// logo which should remain visible.
				shift = Width - backgroundData.PixelWidth;
			}
			Canvas.SetLeft(BackgroundImage, shift);
			Canvas.SetTop(BackgroundImage, imageOffset);
		}
		
		/// <summary>
		/// Update a URL if it takes the form of an internal link.
		/// 
		/// The intent is that edlaunch://local/ uris are kept intact as they
		/// correspond to a command to be executed by the launcher.
		/// edlaunch://host/ uris are modified such that the scheme is modified
		/// to https with the target page being opened inside the launcher
		/// browser window.
		/// Non edlaunch uris are also unchanged.
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		private Uri RedirectInternalLink(Uri uri)
		{
			if (uri.Scheme!="edlaunch")
			{
				return uri;
			}
			if (uri.Host=="local")
			{
				return uri;
			}
			UriBuilder result = new UriBuilder(uri.ToString());
			bool usesDefaultPort = result.Uri.IsDefaultPort;
			result.Scheme = "https";
			result.Port = usesDefaultPort ? -1 : result.Port;
			return new Uri(result.ToString());
		}

		/// <summary>
		/// Determine if the uri refers to an 'internal' link, i.e. a web page
		/// that should be opened in the internal browser rather than externally.
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		private bool IsInternalLink(Uri uri)
		{
			if (uri.Scheme=="edlaunch")
			{
				if (uri.Host!="local")
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Handle a Uri as a command.
		/// 
		/// Executes and returns true if the uri represents an internal command
		/// (i.e. edlaunch://local/) false otherwise.
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		private bool HandleSuggestionFromUri(Uri uri)
		{
			if (uri.Scheme!="edlaunch")
			{
				return false;
			}
			if (uri.Host!="local")
			{
				return false;
			}
			HandleSuggestion(uri.PathAndQuery);
			return true;
		}


		/// <summary>
		/// Execute a command based on the passed suggestion.
		/// </summary>
		/// <param name="suggestion"></param>
		private void HandleSuggestion(String suggestion)
		{
			Dispatcher.BeginInvoke(new Action(() =>
			{
				if (suggestion=="/login")
				{
					USSLogin();
				}
				if (suggestion.Substring(0,5)=="/auth")
				{
                    LogEntry log = new LogEntry("External authentication on link");
                    log.AddValue("Suggestion", suggestion);
                    View.m_manager.Log(log);
                    USSAuthenticate(suggestion);
				}
				Update();
				QueueActivation();
			}));
		}

		private DispatcherTimer m_activationTimer;
		private void ActivationTick(object sender, EventArgs e)
		{
			DispatcherTimer timer = m_activationTimer;
			m_activationTimer = null;
			timer.Tick -= ActivationTick;
			timer.Stop();

			Dispatcher.BeginInvoke(new Action(() =>
			{
				Update();
				Activate();
			}));
		}

		private void QueueActivation()
		{
			if (m_activationTimer!=null)
			{
				m_activationTimer.Stop();
			}
			else
			{
				m_activationTimer = new DispatcherTimer();
				m_activationTimer.Interval = TimeSpan.FromSeconds(2);
				m_activationTimer.Tick += ActivationTick;
			}
			m_activationTimer.Start();
		}

		void ViewPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "ExternalPage")
			{
				NavigateToPage(View.ExternalPage);
			}
		}

		public void PopupMessage(String description)
		{
			Dispatcher.Invoke(new Action(() =>
			{
				MessageBox.Show(Window.GetWindow(this), description);
			}));
		}

		public void WarningMessage(String description, String title)
		{
			Dispatcher.Invoke(new Action(() =>
			{
				MessageBox.Show(Window.GetWindow(this), description, title,
					MessageBoxButton.OK, MessageBoxImage.Warning);
			}));
		}

		public void ErrorMessage(String description, String title)
		{
			Dispatcher.Invoke(new Action(() =>
			{
				MessageBox.Show(Window.GetWindow(this), description, title,
								MessageBoxButton.OK, MessageBoxImage.Error);
			}));
		}

		public bool YesNoQuery(String description, String title)
		{
			if (MessageBox.Show(description, title, MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
			{
				return true;
			}
			return false;
		}

		public void CloseWindow()
		{
            CloseIfSafe();
		}

		private void PowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs e)
		{
			switch (e.Mode)
			{
				case Microsoft.Win32.PowerModes.Resume:
					{
						m_view.m_versionStatus = m_view.m_manager.ServerConnection.CheckClientVersion(m_view.CobraBayVersion, out m_view.m_serverClientVersion);
						m_view.CheckLauncherVersion();
						Update();
						break;
					}
			}
		}

		public void TimedUpdate(object data)
		{
			Dispatcher.Invoke(new Action(() => { TimedUpdateInternal(); }));
		}

		private void TimedUpdateInternal()
		{
			Project p = View.GetActiveProject();
			if (p!=null)
			{
				if ((View.m_manager.Authorised) && (p.Action != Project.ActionType.Disabled))
				{
					Update();
				}
			}
		}

		private void CheckXInputDLL()
		{
			DXCheckItem.IsChecked = Properties.Settings.Default.DXCheck;
			if (!Properties.Settings.Default.DXCheck)
			{
				// Check Disabled
				return;
			}
			try
			{
				String paths = Environment.GetEnvironmentVariable("PATH");
				String[] pathArray = paths.Split(';');
				bool found = false;
				foreach (String path in pathArray)
				{
					String xinputPath = System.IO.Path.Combine(path, "XINPUT1_3.DLL");
					if (System.IO.File.Exists(xinputPath))
					{
						found = true;
						break;
					}
				}
				if (!found)
				{
					View.OpenDirectXLink();
				}
			}
			catch (System.Exception)
			{
				
			}
		}

		/// <summary>
		/// Update the UI to match current state.
		/// 
		/// We have two main states, if we are not authorised then the only
		/// thing the user can do is attempt to log in. Once logged in more
		/// options become available.
		/// </summary>
		private void Update()
		{
            View.UpdateExitStatus();

			if (View.ShouldExit)
			{
                CloseIfSafe();
                if (IsClosing)
                {
                    return;
                }
			}

			UninstallProductItem.Visibility = Visibility.Collapsed;
			InstallButton.Visibility = Visibility.Collapsed;
			UpgradeButton.Visibility = Visibility.Collapsed;
			PlayButton.Visibility = Visibility.Collapsed;
			PlayOfflineButton.Visibility = Visibility.Collapsed;
			PurchaseButton.Visibility = Visibility.Collapsed;
			ValidateProductItem.Visibility = Visibility.Collapsed;
			LoginButton.Visibility = Visibility.Collapsed;
			SteamLoginButton.Visibility = Visibility.Collapsed;
			RegisterButton.Visibility = Visibility.Collapsed;
			LinkButton.Visibility = Visibility.Collapsed;
			RedeemButton.Visibility = Visibility.Collapsed;
			ProductList.Visibility = Visibility.Collapsed;
			ProgressBarGrid.Visibility = Visibility.Collapsed;
            MoveProductItem.Visibility = Visibility.Collapsed;
            RestoreProductItem.Visibility = Visibility.Collapsed;

			UpdateProjectList();

			if (View.ProductListVisible)
			{
				ProductList.Visibility = Visibility.Visible;
			}

			//UpdateCobraBayVersionInfo();

			if (m_view.m_versionStatus != ServerInterface.VersionStatus.Expired)
			{
				View.UpdateAuthorisation();

				if (m_view.m_manager.Authorised)
				{
					// User is now authorised so any registration/link must have
					// been completed or the user logged in manually.
					IsAwaitingRegistration = false;

					LogoutButton.Content = m_view.m_manager.UserDetails.EmailAddress;

					if (m_view.Monitor != null)
					{
						ProgressBarGrid.Visibility = Visibility.Visible;
						GameVersionIdentifier.Visibility = Visibility.Hidden;
						WorkingButton.Visibility = Visibility.Visible;
					}
					else
					{
						GameVersionIdentifier.Visibility = Visibility.Visible;
						WorkingButton.Visibility = Visibility.Collapsed;
						CancelProgressButton.Visibility = Visibility.Collapsed;
					}
					LogOutUserItem.Visibility = Visibility.Visible;
					LogOutMachineItem.Visibility = Visibility.Visible;
					UpdateMenuItem.Visibility = Visibility.Visible;
					ShowHardwareSurveryItem.Visibility = Visibility.Visible;
					UploadHardwareSurveyItem.Visibility = Visibility.Visible;
				}
				else
				{
					LoginButton.Visibility = Visibility.Visible;
					SteamLoginButton.Visibility = View.m_manager.AllowSteamLogin() ? Visibility.Visible : Visibility.Collapsed;
					RegisterButton.Visibility = m_view.m_manager.UserDetails.SteamRegistrationLink==null ? Visibility.Collapsed : Visibility.Visible;
					LinkButton.Visibility = m_view.m_manager.UserDetails.SteamLinkLink == null ? Visibility.Collapsed : Visibility.Visible;
					LogoutButton.Content = "Options";
					LogOutUserItem.Visibility = Visibility.Collapsed;
					LogOutMachineItem.Visibility = Visibility.Collapsed;
					UpdateMenuItem.Visibility = Visibility.Collapsed;
					ShowHardwareSurveryItem.Visibility = Visibility.Collapsed;
					UploadHardwareSurveyItem.Visibility = Visibility.Collapsed;
				}

				Project p = View.GetActiveProject();
				if (p != null)
				{
					Project.ActionType action = p.Action;
#if DEVELOPMENT
					if ((action == Project.ActionType.Update) && (DevIgnoreUpdates.IsChecked || p.IgnoreUpdates))
					{
						action = Project.ActionType.Play;
					}
#endif
					switch (action)
					{
						case Project.ActionType.Disabled:
							{
								LoginButton.Visibility = Visibility.Collapsed;
								SteamLoginButton.Visibility = Visibility.Collapsed;
								RegisterButton.Visibility = Visibility.Collapsed;
								LinkButton.Visibility = Visibility.Collapsed;
								ProductList.Visibility = Visibility.Collapsed;
								UpdateMenuItem.Visibility = Visibility.Collapsed;
								break;
							}
						case Project.ActionType.Invalid:
							{
								InstallButton.Visibility = Visibility.Visible;
								InstallButton.IsEnabled = false;
								break;
							}
						case Project.ActionType.Install:
							{
								InstallButton.Visibility = Visibility.Visible;
								if (EnabledUpdate(p))
								{
									InstallButton.IsEnabled = true;
                                    if (p.CanMove)
                                    {
                                        if (p.IsRedirected)
                                        {
                                            RestoreProductItem.Visibility = Visibility.Visible;
                                        }
                                        else
                                        {
                                            MoveProductItem.Visibility = Visibility.Visible;
                                        }
                                    }
								}
								else
								{
									InstallButton.IsEnabled = false;
								}
								break;
							}
						case Project.ActionType.Update:
							{
								UpgradeButton.Visibility = Visibility.Visible;
								if (EnabledUpdate(p))
								{
									UninstallProductItem.Visibility = Visibility.Visible;
									UpgradeButton.IsEnabled = true;
								}
								else
								{
									UpgradeButton.IsEnabled = false;
								}
								break;
							}
						case Project.ActionType.Play:
							{
								if ((p.Offline) && true)
								{
									PlayOfflineButton.Visibility = Visibility.Visible;
								}
								else
								{
									PlayButton.Visibility = Visibility.Visible;
								}
								if (EnabledUpdate(p))
								{
									UninstallProductItem.Visibility = Visibility.Visible;
								}
								if (p.RemoteInstallerType == Project.InstallerType.Manifest)
								{
									ValidateProductItem.Visibility = Visibility.Visible;
								}
								break;
							}
					}
					if (!p.Installed)
					{
						View.GameVersionInfo = LocalResources.Properties.Resources.NotInstalled;
					}
					else
					{
						View.GameVersionInfo = p.PrettyVersion;
					}
				}
				else
				{
					if (m_view.m_manager.Authorised)
					{
						if (View.UnfilteredOnly &&
							(View.m_manager.IsSteam||View.m_manager.OculusEnabled||View.m_manager.IsEpic)) 
						{
							LoginButton.Visibility = Visibility.Collapsed;
							SteamLoginButton.Visibility = Visibility.Collapsed;
							RegisterButton.Visibility = Visibility.Collapsed;
							LinkButton.Visibility = Visibility.Collapsed;
							ProductList.Visibility = Visibility.Collapsed;
							UpdateMenuItem.Visibility = Visibility.Collapsed;
							RedeemButton.Visibility = Visibility.Visible;
						}
						else
						{
							PurchaseButton.Visibility = Visibility.Visible;
						}
					}
				}
			}

			UpdateLogoutMenuItems();

			SetCommanderName();
			HideUnusedSeparators();
			View.ProductDirectoryRoot = m_view.m_manager.RootDirectory;

			PerformStartUpChecks();
		}

		/// <summary>
		/// Test whether the project is enabled for update.
		/// 
		/// Initial quick implementation to make management easier to use.
		/// If it looks like more control is required it may be preferable to
		/// split up the test so sub-parts can be tested independently.
		/// 
		/// Move the "public_test_server" portion to the Project class rather
		/// than hardwiring the decision. This could then be set from the
		/// VersionInfo.txt file or Available projects query (i.e. the server)
		/// so deciding which products are affected can be a decision made
		/// after release.
		/// 
		/// Move the Steam/Oculus tests to the view or manager so they can be
		/// shared between platforms and possibly different rules applied if
		/// new stores are added for example.
		/// 
		/// Testing independently may be useful for example to support
		/// uninstallation of Beta builds for Steam/Oculus installations where
		/// all removal is currently disabled. Note that this is non trivial
		/// even if the test is split. We rely on the previously installed 
		/// beta product (via the virtual cache) to provide the release files
		/// without downloading again when the final beta becomes the live
		/// version. Therefore uninstalling the beta before updating the
		/// release version is not recommended. At the same time the release
		/// occurs the beta is removed from the product list which means it is
		/// not accessible for selection/removal via the menu anyway.
		/// 
		/// Split into separate methods to ease independent testing, notes
		/// regarding relocation to more sensible places remain relevant.
		/// </summary>
		/// <param name="p">Thr project to test</param>
		/// <returns>True if the project can be updated, false otherwise.</returns>
		public bool EnabledUpdate(Project p)
		{
            // Override so that the launcher can always install any products in the list
            // Fixes problems with Odyssey being inaccessible
            return true;
            //return (!RequiresExternalUpdate()) || SecondaryProduct(p);							
		}

		public bool RequiresExternalUpdate()
		{
			return m_view.m_manager.IsEpic  || (m_view.m_manager.IsSteam) || (m_view.m_manager.OculusEnabled);
		}

		public bool SecondaryProduct(Project p)
		{
			return p.Name.ToLowerInvariant().Contains("public_test_server");
		}

		/// <summary>
		/// Update the project list element with the current set of available
		/// projects.
		/// </summary>
		public void UpdateProjectList()
		{
			ProductList.Items.Clear();
			IEnumerable<CobraBayView.ProjectInfo> ap = View.AvailableProjects();
			foreach (CobraBayView.ProjectInfo p in ap)
			{
				ListBoxItem item = new ListBoxItem();
				item.Content = p.Name;
				item.Tag = p.Reference;
				item.Foreground = p.Installed ? Brushes.White : Brushes.LightGray;
				//item.HorizontalContentAlignment = p.Installed ? HorizontalAlignment.Left : HorizontalAlignment.Right;
				ProductList.Items.Add(item);
			}
			UpdateSelectedProject();
		}

		/// <summary>
		/// Ensure the currently selected project is selected in the product
		/// list (for example if the selection changed internally) and is in
		/// the visible area.
		/// </summary>
		public void UpdateSelectedProject()
		{
			foreach (ListBoxItem item in ProductList.Items)
			{
				String name = item.Tag as String;
				if (name == View.SelectedProject)
				{
					ProductList.SelectedValue = item;
					int index = ProductList.SelectedIndex;
					ProductList.ScrollIntoView(item);
                    
                    // Fix for bug where options menu closes as soon as it's clicked
                    // Because this method, called indirectly, is changing the focus
                    if (!LogoutMenu.IsOpen)
                    {
                        item.Focus();
                    }
					break;
				}
			}
		}

		/// <summary>
		/// Selected product in the product list has been changed. Ensure the
		/// internal SelectedProject variable is updated to match.
		/// </summary>
		/// <param name="sender">ListBox for which the selection is changing.
		/// Currently only the ProductList element is supported.</param>
		/// <param name="e">Arguments for the selection changed event. We are
		/// interested in the products being added to the selection and that
		/// is different from the existing selection. At the point this
		/// event is triggered the actual selection on the list box has not
		/// been changed.</param>
		private void OnSelectedProjectChanged(object sender, SelectionChangedEventArgs e)
		{
			if (sender == ProductList)
			{
				s_selectionChangeEvents++;
				foreach (ListBoxItem item in e.AddedItems)
				{
					String name = item.Tag as String;
					if (name != View.SelectedProject)
					{
						View.SelectedProject = name;
						Update();
					}
				}
			}
		}

		private void OnSelectedProjectKey(object sender, KeyEventArgs e)
		{
			if (sender == ProductList)
			{
				int selectedIndex = ProductList.SelectedIndex;
				switch (e.Key)
				{
					case Key.Up:
						{
							--selectedIndex;
							e.Handled = true;
							break;
						}
					case Key.Down:
						{
							++selectedIndex;
							e.Handled = true;
							break;
						}
				}
				if (selectedIndex >= ProductList.Items.Count)
				{
					selectedIndex = ProductList.Items.Count - 1;
				}
				if (selectedIndex < 0)
				{
					selectedIndex = 0;
				}
				ProductList.SelectedIndex = selectedIndex;
			}
		}


		static int s_selectionChangeEvents = 0 ;

		/*private void UpdateCobraBayVersionInfo()
		{
			View.UpdateCobraBayVersionInfo();

			switch (m_view.m_versionStatus)
			{
				case (ServerInterface.VersionStatus.Current):
					{
						VersionIdentifier.Foreground = Brushes.White;
						VersionIdentifier.Opacity = 0.25;
						if (DownloadButtonIcon.Visibility!=Visibility.Collapsed)
						{
							DownloadButtonIcon.Visibility = Visibility.Hidden;
						}
						break;
					}
				case (ServerInterface.VersionStatus.Supported):
					{
						VersionIdentifier.Foreground = Brushes.Yellow;
						VersionIdentifier.Opacity = 0.5;
						if (DownloadButtonIcon.Visibility != Visibility.Collapsed)
						{
							DownloadButtonIcon.Visibility = Visibility.Visible;
						}
						break;
					}
				case (ServerInterface.VersionStatus.Expired):
					{
						VersionIdentifier.Foreground = Brushes.Red;
						VersionIdentifier.Opacity = 1.0;
						if (DownloadButtonIcon.Visibility != Visibility.Collapsed)
						{
							DownloadButtonIcon.Visibility = Visibility.Visible;
						}
						break;
					}
				case (ServerInterface.VersionStatus.Future):
					{
						VersionIdentifier.Foreground = Brushes.LightBlue;
						VersionIdentifier.Opacity = 0.5;
						if (DownloadButtonIcon.Visibility != Visibility.Collapsed)
						{
							DownloadButtonIcon.Visibility = Visibility.Visible;
						}
						break;
					}
			}
		}*/

		private void UpdateLogoutMenuItems()
		{
			LogOutUserItem.Header = View.m_logOutManager.UserTitle;
			LogOutMachineItem.Header = View.m_logOutManager.MachineTitle;
			LogOutUserItem.IsEnabled = View.m_logOutManager.Enabled;
			LogOutMachineItem.IsEnabled = View.m_logOutManager.Enabled;
		}

		/// <summary>
		/// We want to use separators in the menu, but we also want to hide
		/// items in the menu that are not relevant (it is a context menu after
		/// all).
		/// 
		/// Run through the list of menu items looking for separators and hide
		/// it if there are no following visible items, or the next item is
		/// another separator.
		/// </summary>
		private void HideUnusedSeparators()
		{
			int visibleItems = 0;
			for(int itemIndex=0; itemIndex<LogoutMenu.Items.Count; ++itemIndex)
			{
				Separator separatorItem = LogoutMenu.Items[itemIndex] as Separator;
				if (separatorItem == null)
				{
					MenuItem item = LogoutMenu.Items[itemIndex] as MenuItem;
					if (item.Visibility == Visibility.Visible)
					{
						visibleItems++;
					}
					continue;
				}
				// Found a separator, assume we do not want it to start with.
				Visibility showSep = Visibility.Collapsed;
				if (visibleItems>0)
				{
					// If we have seen at least one visible, non separator
					// then this separator is a candidate for being visible
					// if it is also followed by at least one visible non
					// separator.
					for (int followingIndex = itemIndex + 1; followingIndex < LogoutMenu.Items.Count; ++followingIndex)
					{
						Separator nextSeparator = LogoutMenu.Items[followingIndex] as Separator;
						if (nextSeparator != null)
						{
							// Found another separator, so the current show state
							// is correct, we do not want multiple separators shown
							// so if the first thing is another separator we want
							// to hide the current separator, and let the next one
							// appear if it is followed by anything interesting.
							break;
						}
						else
						{
							MenuItem nextMenu = LogoutMenu.Items[followingIndex] as MenuItem;
							if (nextMenu != null)
							{
								if (nextMenu.Visibility == Visibility.Visible)
								{
									// Found a visible item after the separator so
									// we want to show the separator.
									showSep = Visibility.Visible;
								}
							}
						}
					}
				}
				separatorItem.Visibility = showSep;
			}
		}

		/// <summary>
		/// If required add in the developer menu.
		/// </summary>
		private void AddDeveloperMenu()
		{
			if (m_view.m_manager.IsReleaseBuild)
			{
				return;
			}
			DeveloperMenu.Visibility = Visibility.Visible;
		}

		/// <summary>
		/// Event handler generated by the replacement close icon.
		/// 
		/// There is only one close icon so just close the window.
		/// </summary>
		/// <param name="sender">Close icon (unused)</param>
		/// <param name="e">Event arguments (unused)</param>
		private void OnClose(object sender, RoutedEventArgs e)
		{
            CloseIfSafe();
		}

		/// <summary>
		/// The user clicked the purchase button.
		/// </summary>
		/// <param name="sender">Button (unused)</param>
		/// <param name="e">Event arguments (unused)</param>
		private void OnPurchase(object sender, RoutedEventArgs e)
		{
			View.OpenPurchaseLink();
		}

		/// <summary>
		/// Perform the actions required to trigger the registration process.
		/// </summary>
		private void TriggerRegistration()
		{
			String page = null;
			String reglink = View.GetRegistrationLink();
			//reglink = reglink.Replace("https:", "edlaunch:");
			Uri reguri = new Uri(reglink);
			if (IsInternalLink(reguri))
			{
				page = RedirectInternalLink(reguri).ToString();
			}
			if ((page == null) || (m_browser == null))
			{
				IsAwaitingRegistration = true;
				View.OpenRegisterLink();
			}
			else
			{
				NavigateToPage(page);
			}
		}

		/// <summary>
		/// The user clicked the register button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnRegister(object sender, RoutedEventArgs e)
		{
			TriggerRegistration();
		}

		/// <summary>
		/// The user clicked the play button.
		/// 
		/// If the project is already running (started by a previous instance
		/// of the client) then bring it to the front, otherwise run the
		/// project.
		/// 
		/// This does lead to a difference in behaviour if the user quits the
		/// client while the game is running and starts it again, against
		/// keeping it running.
		/// </summary>
		/// <param name="sender">Button (unused)</param>
		/// <param name="e">Event arguments (unused)</param>
		private void OnPlay(object sender, RoutedEventArgs e)
		{
			View.StartSelectedProject();
		}

		public void MoveToFront(Process process)
		{
			SetForegroundWindow(process.MainWindowHandle);
		}

		/// <summary>
		/// The sequence of actions being monitored has completed so hide the
		/// associated UI, discard the monitor, and report any failure
		/// messages.
		/// </summary>
		public void MonitorCompleted()
		{
			Dispatcher.Invoke(new Action(() =>
			{
				m_view.Monitor = null;
				Update();
			}));
		}

		public void ShowMonitorCancelButton(bool show)
		{
			Dispatcher.Invoke(new Action(() =>
			{
				CancelProgressButton.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
			}));
		}

		/// <summary>
		/// The user has pressed the Upgrade button.
		/// </summary>
		/// <param name="sender">Unused</param>
		/// <param name="e">Unused</param>
		private void OnUpgrade(object sender, RoutedEventArgs e)
		{
			View.Upgrade();
		}

		/// <summary>
		/// The user has pressed the Install button.
		/// </summary>
		/// <param name="sender">Unused</param>
		/// <param name="e">Unused</param>
		private void OnInstall(object sender, RoutedEventArgs e)
		{
			View.Install();
		}

		private void OnLogin(object sender, RoutedEventArgs e)
		{
			LoginWindow lw = new LoginWindow(m_view.m_manager, ForceSoftwareRendering);

			ObscureDialog(lw);
		}

		private void OnSteamLogin(object sender, RoutedEventArgs e)
		{
			View.LogoutMachine();
			View.m_manager.AutoLogin();
			Update();
			IsRegistrationRequired();
		}

		private void IsRegistrationRequired()
		{
			// Registration no longer required for Oculus as it is handled
			// before the launcher starts.
			if (!View.m_manager.Authorised && ((View.m_manager.IsSteam || View.m_manager.IsEpic))/* || View.m_manager.IsOculus*/)
			{
				bool login = true;
				bool register = false;
				if ((View.m_manager.IsSteam || View.m_manager.IsEpic) && (
						(View.m_manager.UserDetails.SteamRegistrationLink!=null) ||
						(View.m_manager.UserDetails.SteamLinkLink!=null)
					))
				{
					RegisterProductWindow rw = new RegisterProductWindow(m_view.m_manager, ForceSoftwareRendering);

					ObscureDialog(rw);

					login = rw.TriggerLogin;
					register = rw.TriggerRegistration;
				}

				if (register)
				{
					TriggerRegistration();
				}
				else
				{
					if (View.m_manager.UserDetails.SteamUnavailable)
					{
						WOMessageBox message = new WOMessageBox(ForceSoftwareRendering);

						message.TitleText = LocalResources.Properties.Resources.WTHIS_Title;
						message.SetMessageWithLinks(LocalResources.Properties.Resources.WTHIS_Message);
						message.LeftButtonText = LocalResources.Properties.Resources.WTHIS_Close;
						message.RightButtonText = null;

						ObscureDialog(message);
					}
					else
					{
						if (login)
						{
							OnLogin(LoginButton, null);
						}
					}
				}
			}
		}

		private void ObscureDialog(Window w)
		{
			IsModal = true;
			WindowObscura wo = new WindowObscura(ForceSoftwareRendering);
			wo.Width = Width;
			wo.Height = Height;
			wo.Top = Top;
			wo.Left = Left;
			wo.Owner = this;
			wo.Show();

			w.Left = Left + (ActualWidth - w.Width) / 2;
			w.Top = Top + (ActualHeight - w.Height) / 2;
			w.Owner = wo;
			w.ShowDialog();
			w.Owner = null;
            wo.Close();
			IsModal = false;

			// Hack Alert
			// For some reason using a two level set of windows, (wo + lw)
			// means that when both are closed focus is not restored correctly
			// to the parent window so when the options menu is shown it is
			// shown relative to the screen top left, not the mouse as
			// expected. This only happens the first time and can be avoided by
			// clicking in the non browser area of the window before clicking
			// the options button.
			// Hiding and reshowing the browser for some reason fixes this
			// hiding presumably forces focus elsewhere which programatically
			// setting focus (e.g. with Focus()) does not acheive.
			// Unfortunately it does lead to a flicker (it does not work unless
			// we bookend the update call).
			if (OnDemandBrowser != null)
			{
				OnDemandBrowser.Visibility = Visibility.Hidden;
			}

			Update();

			if (OnDemandBrowser != null)
			{
				OnDemandBrowser.Visibility = Visibility.Visible;
			}
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

		private void OnClosed(object sender, EventArgs e)
		{
			View.Closed();
            IsClosing = true;
        }

		private void DragWindow(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				this.DragMove();
			}
		}

		public String GetOSIdent()
		{
			return ClientSupport.Utils.OSIdent.GetOSIdent();
		}

		public void ExternalUpdate()
		{
			Dispatcher.Invoke(new Action(() => { Update(); }));
		}

		public void MarkForUpdate()
		{
			ExternalUpdate();
		}

		private void LogoutUser(object sender, RoutedEventArgs e)
		{
			View.LogoutUser();
		}

		private void LogoutMachine(object sender, RoutedEventArgs e)
		{
			View.LogoutMachine();
		}

		private void ShowLogoutMenu(object sender, RoutedEventArgs e)
		{
            ShowOptions();
		}

		private void OnOpenLogoutMenu(object sender, ContextMenuEventArgs e)
		{
			SetUpContextMenu();
		}

		private void OnOpenLogoutMenu(object sender, RoutedEventArgs e)
		{
			SetUpContextMenu();
		}
        

        public void ShowOptions()
        {
            LogoutMenu.Placement = PlacementMode.MousePoint;
            LogoutMenu.IsOpen = true;
        }

        // Using XAML to reference resource strings seems to be more complex
        // than expected. This brute force approach should work.
        private void SetUpContextMenu()
		{
			if (DownloadManagerLocalCache.EnableCache)
			{
				VCacheCheckMenuItem.Visibility = Visibility.Visible;
			}
			else
			{
				VCacheCheckMenuItem.Visibility = Visibility.Collapsed;
			}
			if (View.GetSelectedProject() == null)
			{
				DevSelectManifest.ToolTip = "Log in and select a product to install a manifest";
			}
			else
			{
				DevSelectManifest.ToolTip = "Chose a manifest to update the currently selected product";
			}
			foreach (MenuItem item in DevSelectManifest.Items)
			{
				item.IsEnabled = (View.GetSelectedProject() != null);
			}
		}

		private void DXCheckItemToggle(object sender, RoutedEventArgs e)
		{
			DXCheckItem.IsChecked = !DXCheckItem.IsChecked;

			Properties.Settings.Default.DXCheck = DXCheckItem.IsChecked;
			Properties.Settings.Default.Save();
		}


		private void ValidateProduct(object sender, RoutedEventArgs e)
		{
			// This option will only be enabled if the game is up to date,
			// and the remote installer is a manifest file which means the
			// current installed version must be manifest based. Unless some
			// mad fool made an installer and a manifest of the same version,
			// even then it should work by uninstalling the installer based
			// version first.
			View.CommonDownload();
		}

		private void UninstallProduct(object sender, RoutedEventArgs e)
		{
			View.UninstallSelectedProduct();
		}

		private void OnCancelProgress(object sender, RoutedEventArgs e)
		{
			View.CancelProgress();
		}

		private void WindowMoved(object sender, EventArgs e)
		{
			double effectiveHeight = ActualHeight == 0 ? Height : ActualHeight;
			double effectiveWidth = ActualWidth == 0 ? Width : ActualWidth;
			if (e==null)
			{
				Rect workArea = System.Windows.SystemParameters.WorkArea;
				Left = ((workArea.Width - effectiveWidth)/2.0) + workArea.Left;
				Top = ((workArea.Height - effectiveHeight) / 2.0) + workArea.Top;
			}
			double leftLimit = System.Windows.SystemParameters.VirtualScreenLeft;
			if (Left < leftLimit)
			{
				Left = leftLimit;
			}
			double topLimit = System.Windows.SystemParameters.VirtualScreenTop;
			if (Top < topLimit)
			{
				Top = topLimit;
			}
			double bottomLimit = topLimit + System.Windows.SystemParameters.VirtualScreenHeight;
			if (Top + effectiveHeight > bottomLimit)
			{
				Top = bottomLimit - effectiveHeight;
			}
			double rightLimit = leftLimit + System.Windows.SystemParameters.VirtualScreenWidth;
			if (Left + effectiveWidth > rightLimit)
			{
				Left = rightLimit - effectiveWidth;
			}
		}

		private void KeyPressed(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Enter:
					{
						if (LoginButton.Visibility == Visibility.Visible)
						{
							e.Handled = true;
							OnLogin(null, null);
						}
						else
						{
							if (InstallButton.Visibility == Visibility.Visible)
							{
								e.Handled = true;
								OnInstall(null, null);
							}
							else
							{
								if (UpgradeButton.Visibility == Visibility.Visible)
								{
									e.Handled = true;
									OnUpgrade(null, null);
								}
								else
								{
									if ((PlayButton.Visibility == Visibility.Visible) ||
										(PlayOfflineButton.Visibility == Visibility.Visible))
									{
										e.Handled = true;
										OnPlay(null, null);
									}
									else
									{
										if (PurchaseButton.Visibility == Visibility.Visible)
										{
											e.Handled = true;
											OnPurchase(null, null);
										}
									}
								}
							}
						}
						break;
					}
				case Key.Escape:
					{
						if (CancelProgressButton.Visibility == Visibility.Visible)
						{
							e.Handled = true;
							OnCancelProgress(null, null);
						}
						break;
					}
				case Key.V:
					{
						if (Keyboard.Modifiers == ModifierKeys.Control)
						{
							e.Handled = true;
							if (ValidateProductItem.Visibility == Visibility.Visible)
							{
								ValidateProduct(this, null);
							}
						}
						break;
					}
				case Key.U:
					{
						if (Keyboard.Modifiers == ModifierKeys.Control)
						{
							e.Handled = true;
							if (UninstallProductItem.Visibility == Visibility.Visible)
							{
								UninstallProduct(this, null);
							}
						}
						break;
					}
			}
		}

		private void OnMinimize(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}

		private void ShowHardwareSurvey(object sender, RoutedEventArgs e)
		{
			LogoutMenu.Visibility = Visibility.Hidden;
			LogoutMenu.IsOpen = false;

			Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action( () =>
			{
				HardwareSurveyDisplayWindow hsdw = new HardwareSurveyDisplayWindow(m_view.m_manager, ForceSoftwareRendering);

				hsdw.ShowDialog();
				LogoutMenu.Visibility = Visibility.Visible;
			}));
		}

		private void UploadHardwareSurvey(object sender, RoutedEventArgs e)
		{
			LogoutMenu.Visibility = Visibility.Hidden;
			LogoutMenu.IsOpen = false;

			Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
			{
				m_view.m_manager.RunHardwareSurvey(true, false);
				LogoutMenu.Visibility = Visibility.Visible;
			}));
		}

		private void CheckForUpdates(object sender, RoutedEventArgs e)
		{
			View.ResetDMCache();
			Update();

			String message = View.UpdateCheckMessage(DevIgnoreUpdates.IsChecked);
			if (!String.IsNullOrEmpty(message))
			{
				MessageBox.Show(message, LocalResources.Properties.Resources.UpdateCheckTitle);
			}
		}

        private void AlternateDownload(object sender, RoutedEventArgs e)
        {
			if (View.Monitor==null)
			{
				// Only allow mode to change if we have not already started
				// an operation.
				View.DisableFastDownload = !View.DisableFastDownload;
			}
            AlternateDownloadItem.IsChecked = !View.DisableFastDownload;
        }

		private void CheckForCache(object sender, RoutedEventArgs e)
		{
			DownloadManagerLocalCache.EnableCache = !DownloadManagerLocalCache.EnableCache;
			CacheCheckMenuItem.IsChecked = DownloadManagerLocalCache.EnableCache;
		}

		private void CheckForVCache(object sender, RoutedEventArgs e)
		{
			DownloadManagerLocalCache.EnableVirtualCache = !DownloadManagerLocalCache.EnableVirtualCache;
			VCacheCheckMenuItem.IsChecked = DownloadManagerLocalCache.EnableVirtualCache;
		}

		private void CreateDetailedUpdateLog(object sender, RoutedEventArgs e)
		{
			FORCManager.DetailedUpdateLog = !FORCManager.DetailedUpdateLog;
			DetailedUpdateLogItem.IsChecked = FORCManager.DetailedUpdateLog;
		}

		private void Refresh(object sender, RoutedEventArgs e)
		{
			Update();
		}

		private void RunUploadCrashFile(String extras)
		{
			if (!String.IsNullOrEmpty(View.CrashUploadFileName))
			{
				String arguments = "";
				if (extras!=null)
				{
					arguments += extras;
				}
				arguments += " /MachineToken ";
				arguments += m_view.m_manager.UserDetails.AuthenticationToken;
				arguments += " /Version ";
				arguments += m_view.m_manager.ApplicationVersion;
				arguments += " /AuthToken ";
				arguments += m_view.m_manager.UserDetails.SessionToken;
				arguments += " /MachineId ";
				arguments += m_view.m_manager.MachineIdentifier.GetMachineIdentifier();
				arguments += " /DumpReport ";
				arguments += View.CrashUploadFileName;
				arguments += " /Time ";
				arguments += m_view.m_manager.ServerConnection.GetCurrentTimeStamp();
				arguments += " /ApplicationPath CrashReporterDebugUpload";

				ProcessStartInfo pstart = new ProcessStartInfo();
				Assembly an = Assembly.GetEntryAssembly();
				String cr = System.IO.Path.GetDirectoryName(an.Location);
				cr = System.IO.Path.Combine(cr, "CrashReporter.exe");

				pstart.FileName = cr;
				pstart.WorkingDirectory = System.IO.Path.GetDirectoryName(View.CrashUploadFileName);
				pstart.Arguments = arguments;

				try
				{
					Process pid = Process.Start(pstart);
				}
				catch (System.Exception ex)
				{
					String message = String.Format(LocalResources.Properties.Resources.UploadCrashReportDetails, View.CrashUploadFileName, ex.Message);
					MessageBox.Show(message,LocalResources.Properties.Resources.UploadCrashReportTitle);
				}
			}
		}

		private void UploadCrashFileHandler(object sender, RoutedEventArgs e)
		{
			RunUploadCrashFile(null);
		}

		private void DebugUploadCrashFileHandler(object sender, RoutedEventArgs e)
		{
			RunUploadCrashFile("/DebugBreak");
		}

		private void SelectCrashFileHandler(object sender, RoutedEventArgs e)
		{
			// TODO: Allow the user to select an installer to run
			Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
			ofd.DefaultExt = LocalResources.Properties.Resources.SelectCrashFileDefaultExtension;
			ofd.Filter = LocalResources.Properties.Resources.SelectCrashFileFilter;

			Nullable<bool> result = ofd.ShowDialog();

			if (result == true)
			{
				View.SetCrashUploadFileName(ofd.FileName);
			}
		}

		private void SelectInstaller(object sender, RoutedEventArgs e)
		{
			// TODO: Allow the user to select an installer to run
			Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
			ofd.DefaultExt = LocalResources.Properties.Resources.SelectInstallerDefaultExtension;
			ofd.Filter = LocalResources.Properties.Resources.SelectInstallerFilter;

			Nullable<bool> result = ofd.ShowDialog();

			if (result == true)
			{
				View.InstallFromPath(ofd.FileName);
			}
		}

		private void SelectFileStore(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
			ofd.FileName = LocalResources.Properties.Resources.SelectFileStoreFileName;
			ofd.Filter = LocalResources.Properties.Resources.SelectFileStoreFilter;

			Nullable<bool> result = ofd.ShowDialog();

			if (result == true)
			{
				IEnumerable<CobraBayView.ManifestInfo> availableManifests;
				availableManifests = View.GetManifestContents(ofd.FileName);

				if (availableManifests != null)
				{
					DevSelectManifest.Items.Clear();

					foreach (CobraBayView.ManifestInfo mi in availableManifests)
					{
						// Hack, the menu label steals the first
						// underscore in the text so stick one at
						// the front so we lose that one, not one
						// that may be significant in the name.
						MenuItem item = new MenuItem();
						item.Header = "_" + mi.Description;
						item.Tag = mi.Name;
						item.Click += InstallFromManifest;
						DevSelectManifest.Items.Add(item);
					}

					if (DevSelectManifest.Items.Count == 0)
					{
						DevSelectManifest.Visibility = Visibility.Collapsed;
					}
					else
					{
						DevSelectManifest.Visibility = Visibility.Visible;
					}
				}
			}
		}

		private void InstallFromManifest(object sender, RoutedEventArgs e)
		{
			MenuItem item = sender as MenuItem;
			if (item != null)
			{
				String manifestPath = item.Tag as String;
				if (!String.IsNullOrEmpty(manifestPath))
				{
					if (View.GetSelectedProject() != null)
					{
						View.InstallFromManifest(manifestPath);
					}
					else
					{
						MessageBox.Show("You must be logged in and have a product selected to install from a manifest",
							"Install Error", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
			}
		}

		private void SelectLanguage(object sender, RoutedEventArgs e)
		{
			LanguageSelector ls = new LanguageSelector(m_view.m_manager, ForceSoftwareRendering);

			ls.ActiveLanguage = m_view.LanguageOverride;

			bool? dr = ls.ShowDialog();
			if (dr != null)
			{
				if (dr == true)
				{
					String activeLanguage = ls.ActiveLanguage;
					if (activeLanguage == null)
					{
						activeLanguage = "";
					}
					if (m_view.LanguageOverride!=activeLanguage)
					{
						m_view.LanguageOverride = activeLanguage;
						App outsider = Application.Current as App;
						if (outsider!=null)
						{
							outsider.Restart = true;

                            /*if (m_view.m_manager.IsEpic)
                            {
                                outsider.RestartExtraArgs.Add("/EpicRefreshToken");
                                outsider.RestartExtraArgs.Add(m_view.m_manager.GetRefreshToken());
                            }*/
						}
						CloseIfSafe();
					}
				}
			}
		}

		private void EnableHTMLView()
		{
			if (OnDemandBrowser == null)
			{
				String[] arguments = Environment.GetCommandLineArgs();
				foreach (String argument in arguments)
				{
					String la = argument.ToLowerInvariant();
					if (la == "/nobrowser")
					{
						return;
					}
				}
				try
				{
					double browserHeight = Height - m_nonbrowserHeight;
					OnDemandBrowser = new WebBrowser();
					OnDemandBrowser.Width = Width;
					OnDemandBrowser.Height = browserHeight;
					OnDemandBrowser.Visibility = Visibility.Visible;
					BodyCanvas.Children.Add(OnDemandBrowser);
					Canvas.SetLeft(OnDemandBrowser, 0);
					Canvas.SetTop(OnDemandBrowser, 0);
				}
				catch (System.Exception ex)
				{
					String error = String.Format(LocalResources.Properties.Resources.RF_BrowserInitialisationFailure, ex.Message);
					MessageBox.Show(error);
					OnDemandBrowser = null;
				}
			}
			if (OnDemandBrowser == null)
			{
				return;
			}
			if (m_view.m_manager.HasServer)
			{
				String page = LocalResources.Properties.Resources.RF_Page;
				if (m_view.m_manager.IsSteam)
				{
					page = LocalResources.Properties.Resources.RF_PageSteam;
				}
				if (!String.IsNullOrEmpty(m_view.ExternalPage))
				{
					page = m_view.ExternalPage;
				}
				if (!String.IsNullOrEmpty(page))
				{
					OnDemandBrowser.ObjectForScripting = m_browser;
					OnDemandBrowser.Navigating += Navigating;
					NavigateToPage(page);
				}
			}
		}

		private void NavigateToPage(String page)
		{
			Uri pageSource = RedirectInternalLink(new Uri(page));
			m_browser.InternalNavigation = true;
			if ((OnDemandBrowser!=null) && (pageSource!=null))
			{

				OnDemandBrowser.Navigate(pageSource);
			}
		}

		private void Navigating(object sender, NavigatingCancelEventArgs e)
		{
			if (HandleSuggestionFromUri(e.Uri))
			{
				e.Cancel = true;
				return;
			}
			if (IsInternalLink(e.Uri))
			{
				return;
			}
			if (!m_browser.InternalNavigation)
			{
				e.Cancel = true;
				try
				{
					Process.Start(e.Uri.ToString());
				}
				catch (System.Exception)
				{
					
				}
			}
		}

		private void EnableHTMLUI(object sender, RoutedEventArgs e)
		{
			EnableBrowserUI(false);
		}

		public void EnableBrowserUI(bool full)
		{
			m_browser.InternalNavigation = false;
			if (OnDemandBrowser == null)
			{
				return;
			}
			OnDemandBrowser.Width = Width;
			OnDemandBrowser.Visibility = Visibility.Visible;
			if (full)
			{
				Canvas.SetTop(OnDemandBrowser, 0);
				OnDemandBrowser.Height = Height - m_nonbrowserHeight;
				SetCommanderName();
				LogoutButton.Visibility = Visibility.Hidden;
			}
			else
			{
				OnDemandBrowser.Height = 200;
				Canvas.SetTop(OnDemandBrowser, 30);
				LogoutButton.Visibility = Visibility.Visible;
			}
		}

		private void SetCommanderName()
		{
			if (OnDemandBrowser == null)
			{
				return;
			}
			object[] args = new object[1];
			args[0] = m_view.m_manager.UserDetails.RegisteredName;
			if (OnDemandBrowser.Document != null)
			{
				try
				{
					OnDemandBrowser.InvokeScript("SetCommanderName", args);
				}
				catch (System.Exception)
				{
					// Failed to call the script, possibly does not include
					// the named function?
				}
			}
		}

		private void SelectHTMLUI(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
			ofd.DefaultExt = "*.html";

			Nullable<bool> result = ofd.ShowDialog();

			if (result == true)
			{
				if (File.Exists(ofd.FileName))
				{
					NavigateToPage("file://" + ofd.FileName);
				}
			}
		}

		private void LauncherOutOfDate(object sender, RoutedEventArgs e)
		{
			m_view.DownloadLauncherUpdate();
		}

		private void OnShowReleaseNotes(object sender, RoutedEventArgs e)
		{
			Project p = View.GetSelectedProject();
			if (p!=null)
			{
				if (p.Installed)
				{
					String target = LocalResources.Properties.Resources.EL_ReleaseNotes + p.Version;
					try
					{
						Process.Start(target);
					}
					catch (System.Exception)
					{
					}
				}
			}
		}

		public void HideReleaseNotesLink()
		{
			View.GameReleaseNotesButtonPermitted = false;
			Project p = View.GetSelectedProject();
		}

		private void UpdateDevIgnoreUpdates(object sender, RoutedEventArgs e)
		{
			View.DeveloperIgnoreUpdates = DevIgnoreUpdates.IsChecked;
			Update();
		}

        private void UpdateUsePrivateTestServer(object sender, RoutedEventArgs e)
        {
            View.m_manager.UsePrivateServer = !View.m_manager.UsePrivateServer;
            UsePrivateTestServer.IsChecked = View.m_manager.UsePrivateServer;
        }

        private void UpdateEnableGzipCompression(object sender, RoutedEventArgs e)
        {
            View.m_manager.EnableGzipCompression = !View.m_manager.EnableGzipCompression;
            UsePrivateTestServer.IsChecked = View.m_manager.EnableGzipCompression;
        }

		private void OnDownloadLauncher(object sender, RoutedEventArgs e)
		{
			View.DownloadOptionalLauncherInstaller();
		}

		private bool m_previouslyShown;
		private void OnShown(object sender, EventArgs e)
		{
			if (!m_previouslyShown)
			{
				m_previouslyShown = true;

				IsRegistrationRequired();

				PerformStartUpChecks();
			}
		}

		/// <summary>
		/// Perform status checks that we want done as soon as possible.
		/// 
		/// These can occur at two points, the first time the main window is
		/// shown, or during an update cycle.
		/// 
		/// If the user is set to auto log in the first update can occur before
		/// the window is shown so there is no UI to interact with. In that
		/// case we want to delay until the window is shown. Alternatively
		/// it may not be possible to perform a check until the user has logged
		/// in, which if the user was not automatically logged in will be some
		/// time after the window is shown.
		/// </summary>
		private void PerformStartUpChecks()
		{
			CheckMissingProducts();
			PerformVersionCheck();
			CheckRequiresHostUpdate();
		}

		private bool m_checkMissingProducts = true;
		private void CheckMissingProducts()
		{
			if ((m_checkMissingProducts) && (m_previouslyShown))
			{
				if (View.m_manager != null)
				{
					if ((View.m_manager.Authorised)
						&& (View.m_manager.UserDetails.AuthenticationType!=ServerInterface.AuthenticationType.Steam)
						)
					{
						if (View.m_manager.AvailableProjects != null)
						{
							m_checkMissingProducts = false;
							if (View.m_manager.AvailableProjects.MissingProducts)
							{
								Dispatcher.Invoke(new Action(() => { ReportMissingProduct(); }));
							}
							return;
						}
					}
				}
			}
		}

		private void ReportMissingProduct()
		{
			RedeemProductWindow rw = new RedeemProductWindow(m_view.m_manager, ForceSoftwareRendering);

			// If the dialog is being shown, disable the host update message as
			// one potential cause is that the currently logged in user does not
			// have any valid products on the account in which case the update
			// message is not helpful, and prevents the user logging out and
			// changing to an account with the products available (or logging in
			// via Steam).
			m_checkRequiresHostUpdate = false;

			ObscureDialog(rw);

			if (rw.ForceLogout)
			{
				View.LogoutMachine();
				Update();
			}
		}

		/// <summary>
		/// Provide a graphics driver version check.
		/// 
		/// Because the launcher for a number of different games is the obvious
		/// place to check driver versions for a specific game.
		/// </summary>
		private void PerformVersionCheck()
		{
			if (m_previouslyShown)
			{
				View.PerformVersionCheck();
			}
		}

		private bool m_checkRequiresHostUpdate = true;
		private void CheckRequiresHostUpdate()
		{
			if (m_checkRequiresHostUpdate && m_previouslyShown)
			{
				if (View.m_manager != null)
				{
					if (View.m_manager.Authorised)
					{
						m_checkRequiresHostUpdate = false;
						if (RequiresExternalUpdate())
						{
							bool disabledUpdate = false;
							bool noneInstalled = true;
							foreach (Project p in View.m_manager.AvailableProjects.GetProjectArray())
							{
								if (!SecondaryProduct(p))
								{
									if (p.Action == Project.ActionType.Update)
									{
										//disabledUpdate = true;
										//break;
									}
									if (p.Action == Project.ActionType.Play)
									{
										noneInstalled = false;
									}
								}
							}
							if ((noneInstalled))
							{
                                if (noneInstalled)
                                {
                                    // Tell log file what's going on
                                    LogEntry entry = new LogEntry("UpdateRequired CloseLauncher");
                                    entry.AddValue("Reason", "No product installed");
                                    View.m_manager.Log(entry);
                                }

								WOMessageBox message = new WOMessageBox(ForceSoftwareRendering);

								message.TitleText = LocalResources.Properties.Resources.HU_Title;
								message.MessageText = LocalResources.Properties.Resources.HU_Message;
								if (View.m_manager.IsSteam)
								{
									message.MessageText = LocalResources.Properties.Resources.HU_MessageST;
								} else if (View.m_manager.IsEpic)
                                {
                                    message.MessageText = LocalResources.Properties.Resources.HU_MessageEpic;
                                }

								message.LeftButtonText = LocalResources.Properties.Resources.HU_Continue;
								message.RightButtonText = null;

								ObscureDialog(message);

								IsAwaitingRegistration = false;
								CloseIfSafe();
							}
						}
					}
				}
			}
		}

        private void MoveProduct(object sender, RoutedEventArgs e)
        {
            Project p = View.GetActiveProject();
            if (p!=null)
            {
                System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
                fbd.Description = LocalResources.Properties.Resources.SelectInstallLocationInstructions;
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        String path = fbd.SelectedPath;
                        if (Directory.Exists(path))
                        {
                            File.WriteAllText(p.Redirection, fbd.SelectedPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            Update();
        }

        private void RestoreProduct(object sender, RoutedEventArgs e)
        {
            Project p = View.GetActiveProject();
            if (p != null)
            {
                try
                {
                    File.Delete(p.Redirection);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            Update();
        }

		private void USSLogin()
		{
			if ((!View.m_manager.Authorised) && (!IsModal))
			{
				if (View.m_manager.IsSteam || View.m_manager.IsEpic)
				{
					View.m_manager.AutoLogin();
				}
				if (!View.m_manager.Authorised)
				{
					OnLogin(this, null);
				}
			}
		}

		private void USSAuthenticate(String suggestion)
		{
			try
			{
				String[] segments = suggestion.Split("?".ToCharArray(), 2);
				if (segments.Length>0)
				{
					if (segments[0]=="/auth")
					{
						if (segments.Length>1)
						{
							try
							{
								NameValueCollection queryParams = System.Web.HttpUtility.ParseQueryString(segments[1]);
								View.m_manager.ExternalAuthorisation(queryParams);
							}
							catch (ArgumentNullException)
							{
							}
						}
						else
						{
							// Missing auth details, Windows swallowed them, so
							// bounce back to do an automated login and hope we
							// can authenticate ourselves now.
							USSLogin();
						}
					}
				}
			}
			catch (Exception ex)
			{
				PopupMessage(ex.Message);
			}
		}

        // Setup the FORC exit status timer, just in case we have an auth issue that requires a shutdown 
        private void SetupForcExitStatusTimer()
        {
            m_forcExitStatusTimerInterval = new TimeSpan(0, 0, 0, 0, 100);
            m_forcExitStatusTimer = new DispatcherTimer();
            //m_forcExitStatusTimer.Tick += ForcExitStatusTimerTick;
            m_forcExitStatusTimer.Interval = m_forcExitStatusTimerInterval;

            m_forcExitStatusTimer.Start();
        }

        // Periodically check with the FORC manager, if it thinks we need to shut down due to an auth issue
        // Attempt a shutdown.
        /*private void ForcExitStatusTimerTick(object sender, EventArgs e)
        {
            if (View.m_manager.ShouldExit())
            {
                CloseIfSafe();
            }
        }*/
    }
}
