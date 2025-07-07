using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace MessageBox
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			string title;
			string message;
			for (int d = 0; d < e.Args.Length;  ++d)
			{
				string display = e.Args[d].ToUpper();
				title = "Unknown";
				message = "Please remove headset and follow on screen instructions.";
				if (display == "REG")
				{
					title = "Registration Required";
					message = "Please remove headset and register the game in your web browser.";
				}
				if (display == "LOGIN")
				{
					title = "Login Required";
					message = "Please remove headset and login to your Frontier Account.";
				}
				System.Windows.MessageBox.Show(message, title);
			}

			Shutdown(1);
		}
	}
}
