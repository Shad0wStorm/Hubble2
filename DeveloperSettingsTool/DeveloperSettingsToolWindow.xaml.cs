using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DeveloperSettingsTool
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class DeveloperSettingsToolWindow : Window
	{
		public DeveloperSettingsToolWindow()
		{
			InitializeComponent();

			Update();
		}

		private void Update()
		{
			UpdateLauncherLocations();
			UpdateServerLocations();
		}

		private void UpdateLauncherLocations()
		{
			String[] baselocations = GetLauncherLocationSettings();

			String[] locations = ExpandLocations(baselocations);

			ActivateLauncherLocations(locations);
		}

		private String[] ExpandLocations(String[] baselocations)
		{
			String[] current = ExpandDrives(baselocations);
			int changes = 1;
			Char[] wildcards = { '*' };
			while (changes>0)
			{
				changes = 0;
				List<String> expanded = new List<String>();
				foreach (String location in current)
				{
					String[] segments = location.Split(wildcards, 2);
					if (segments.Length > 1)
					{
						++changes;
						if (Directory.Exists(segments[0]))
						{
							foreach (String item in Directory.GetDirectories(segments[0]))
							{
								String add = item;
								add += segments[1];
								expanded.Add(add);
							}
						}
					}
					else
					{
						if (Directory.Exists(location))
						{
							expanded.Add(location);
						}
					}
				}
				current = expanded.ToArray();
			}

			return current;
		}

		private String[] ExpandDrives(String[] baselocations)
		{
			List<String> expanded = new List<String>();

			String[] drives = GetAvailableDrives();

			foreach (String location in baselocations)
			{
				if (location.Length > 3)
				{
					String drive = location.Substring(0, 3);
					if (drive == "*:\\")
					{
						String remainder = location.Substring(3);
						foreach (String d in drives)
						{
							expanded.Add(d + remainder);
						}
					}
					else
					{
						expanded.Add(location);
					}
				}
			}

			return expanded.ToArray();
		}

		private String[] GetAvailableDrives()
		{
			DriveInfo[] allDrives = DriveInfo.GetDrives();
			List<String> available = new List<String>();

			foreach (DriveInfo d in allDrives)
			{
				try
				{
					if (d.IsReady)
					{
						available.Add(d.Name);
					}
				}
				catch (System.Exception)
				{
				}
			}

			return available.ToArray();
		}

		private void ActivateLauncherLocations(String[] locations)
		{
			String selected = LauncherLocations.SelectedItem as String;
			String previous = Properties.Settings.Default.CurrentLauncher;
			LauncherLocations.Items.Clear();
			foreach (String location in locations)
			{
				LauncherLocations.Items.Add(location);
			}
			if (selected!=null)
			{
				if (LauncherLocations.Items.Contains(selected))
				{
					LauncherLocations.SelectedItem = selected;
				}
				else
				{
					selected = null;
				}
			}
			if (selected == null)
			{
				selected = previous;
				if (!String.IsNullOrEmpty(selected))
				{
					if (LauncherLocations.Items.Contains(selected))
					{
						LauncherLocations.SelectedItem = selected;
					}
					else
					{
						selected = null;
					}
				}
				else
				{
					selected = null;
				}
			}
			if (selected == null)
			{
				LauncherLocations.SelectedIndex = 0;
			}
		}

		private String[] GetLauncherLocationSettings()
		{
			String setting = Properties.Settings.Default.SearchPaths;

			try
			{
				String[] paths = setting.Split(';');
				return paths;
			}
			catch (System.Exception)
			{
				return null;
			}
		}

		private void UpdateServerLocations()
		{
			List<String> servers = new List<String>();

			servers.Add(c_production);

			String[] config = GetConfiguredHosts();

			foreach (String server in config)
			{
				servers.Add(server);
			}

			String settings = GetSettingsFileForLauncher();
			String current = null;
			if (File.Exists(settings))
			{
				EditServerButton.IsEnabled = true;
				try
				{
					current = File.ReadAllText(settings);
					current = current.Trim();
					if (!servers.Contains(current))
					{
						servers.Add("*" + current);
					}
				}
				catch (System.Exception)
				{

				}
			}
			else
			{
				EditServerButton.IsEnabled = false;
			}

			String selected = ServerLocations.SelectedItem as String;
			if (!servers.Contains(selected))
			{
				selected = current;
			}
			if (!servers.Contains(selected))
			{
				selected = null;
				foreach (String server in servers)
				{
					if (server[0] == '*')
					{
						selected = server;
					}
				}
				if (selected == null)
				{
					selected = c_production;
				}
			}
			ServerLocations.Items.Clear();
			foreach (String server in servers)
			{
				ServerLocations.Items.Add(server);
			}
			ServerLocations.SelectedItem = selected;
		}

		private String[] GetConfiguredHosts()
		{
			String configured = Properties.Settings.Default.Hosts;
			String[] config = configured.Split(';');
			return config;
		}

		private String GetSettingsFileForLauncher()
		{
			String launcherDir = LauncherLocations.SelectedItem as String;
			String settingsDir = System.IO.Path.Combine(launcherDir, "Settings");
			String settingsFile = System.IO.Path.Combine(settingsDir, "UseInternalServer.txt");
			return settingsFile;
		}

		private void RefreshLaunchers(object sender, RoutedEventArgs e)
		{
			UpdateLauncherLocations();
		}

		private void LauncherSelected(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				Properties.Settings.Default.CurrentLauncher = LauncherLocations.SelectedItem as String;
				Properties.Settings.Default.Save();
			}
			catch (System.Exception)
			{
				
			}
		}

		private void ActivateServer(object sender, RoutedEventArgs e)
		{
			String selected = ServerLocations.SelectedItem as String;
			String settings = GetSettingsFileForLauncher();

			if (selected == c_production)
			{
				if (File.Exists(settings))
				{
					File.Delete(settings);
				}
			}
			else
			{
				if (selected[0] == '*')
				{
					selected = selected.Substring(1);
					List<String> hosts = GetConfiguredHosts().ToList();
					if (!hosts.Contains(selected))
					{
						hosts.Add(selected);
						hosts.Sort();
					}
					String config = String.Join(";",hosts.ToArray());
					Properties.Settings.Default.Hosts = config;
					Properties.Settings.Default.Save();
				}
				try
				{
					String dir = System.IO.Path.GetDirectoryName(settings);
					if (!Directory.Exists(dir))
					{
						Directory.CreateDirectory(dir);
					}
					File.WriteAllText(settings, selected);
				}
				catch (System.Exception ex)
				{
					MessageBox.Show(ex.Message);
				}
			}
			UpdateServerLocations();
		}

		private void RefreshServer(object sender, RoutedEventArgs e)
		{
			UpdateServerLocations();
		}

		private void EditServer(object sender, RoutedEventArgs e)
		{
			String settings = GetSettingsFileForLauncher();

			if (File.Exists(settings))
			{
				Process.Start(settings);
			}
		}

		private void DiscardServer(object sender, RoutedEventArgs e)
		{
			String selection = ServerLocations.SelectedItem as String;
			if (selection[0] == '*')
			{
				selection = selection.Substring(1);
			}
			if (selection == c_production)
			{
				return;
			}

			List<String> hosts = GetConfiguredHosts().ToList();

			if (hosts.Contains(selection))
			{
				hosts.Remove(selection);
			}

			String config = String.Join(";", hosts.ToArray());
			Properties.Settings.Default.Hosts = config;
			Properties.Settings.Default.Save();

			UpdateServerLocations();
		}

		private void KeyActivated(object sender, KeyEventArgs e)
		{
			String selection = ServerLocations.SelectedItem as String;
			if (((e.Key == Key.LeftCtrl) || (e.Key == Key.RightCtrl)) && (selection!=c_production))
			{
				DiscardServerButton.Visibility = Visibility.Visible;
				ActivateServerButton.Visibility = Visibility.Hidden;
				RefreshServerButton.Visibility = Visibility.Hidden;
				EditServerButton.Visibility = Visibility.Hidden;
			}
		}

		private void KeyDeactivated(object sender, KeyEventArgs e)
		{
			if ((e.Key == Key.LeftCtrl) || (e.Key == Key.RightCtrl))
			{
				DiscardServerButton.Visibility = Visibility.Hidden;
				ActivateServerButton.Visibility = Visibility.Visible;
				RefreshServerButton.Visibility = Visibility.Visible;
				EditServerButton.Visibility = Visibility.Visible;
			}
		}

		const String c_production = "Production";
	}
}
