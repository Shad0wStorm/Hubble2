using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using ClientSupport;

namespace CobraBay
{
	/// <summary>
	/// Interaction logic for RedeemProductWindow.xaml
	/// </summary>
	public partial class RedeemProductWindow : Window
	{
		FORCManager m_manager;

		public bool ForceLogout = false;

		public RedeemProductWindow(FORCManager manager, bool forceSoftwareRendering)
		{
			if (forceSoftwareRendering)
			{
				this.Loaded += OnLoaded_ForceSoftwareRendering;
			}

			InitializeComponent();

			m_manager = manager;
		}

		private void OnLoaded_ForceSoftwareRendering(object sender, EventArgs e)
		{
			HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;
			HwndTarget hwndTarget = hwndSource.CompositionTarget;
			hwndTarget.RenderMode = RenderMode.SoftwareOnly;
		}

		private void OnClose(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void OnCopyClick(object sender, RoutedEventArgs e)
		{
			String gameKey = m_manager.GetSteamKey();

			try
			{
				Clipboard.SetText(gameKey);
			}
			catch (System.Exception)
			{
				
			}
		}

		private void OnRedeemClick(object sender, RoutedEventArgs e)
		{
            try
            {
				m_manager.StartRegistration(LocalResources.Properties.Resources.RegisterLink);
			}
            catch (System.Exception) { }
		}

		private void KeyPressed(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Enter:
					{
						e.Handled = true;
						OnRedeemClick(null, null);
						break;
					}
				case Key.Escape:
					{
						e.Handled = true;
						Close();
						break;
					}
			}
		}

		private void OnLogoutClick(object sender, RoutedEventArgs e)
		{
			ForceLogout = true;
			Close();
		}
	}
}
