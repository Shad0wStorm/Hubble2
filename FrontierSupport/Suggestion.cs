using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

using Microsoft.Win32;

namespace FrontierSupport
{
	public class Suggestion
	{
		// https://code.msdn.microsoft.com/Interprocess-Communication-858cc9c7

		Thread m_serverThread;
		bool m_running = true;
		bool m_registered = false;
		String Name;

		public delegate void SuggestionHandler(String suggestion);
		public event SuggestionHandler SuggestionEvent;

		// If this is set to false we only record the scheme handler while the
		// application is running. This is more correct when more than one
		// launcher is in use (e.g. Steam and non Steam based accounts) but
		// causes an error when no launcher is running and a page attempts to
		// access an address using the scheme. Unfortunately we cannot know for
		// certain which is the correct instance to run or what the correct
		// arguments would be so we just report an error and let the user figure
		// it out.
		const bool PermanentSchemeRegistration = true;

		public Suggestion(String name)
		{
			Name = name;
			m_serverThread = new Thread(ServerThread);
			m_serverThread.IsBackground = true;
			m_serverThread.Start(this);
			Register();
		}

		/// <summary>
		/// Stop the server.
		/// 
		/// This only registers the intent to stop as WaitForConnection is
		/// blocking so the thread will only exit naturally if a new message is
		/// received between this function being called and the thread being
		/// terminated on application close.
		/// </summary>
		public void Stop()
		{
			m_running = false;
			Deregister();
		}

		/// <summary>
		/// A message has been received on the named pipe so pass it on for
		/// processing to any registered event handlers.
		/// </summary>
		/// <param name="message"></param>
		private void Process(string message)
		{
			if (SuggestionEvent != null)
			{
				String finalMessage = message;
				if (message.Length>2)
				{
					if ((message[0] == '/') && (message[1]=='b'))
					{
						String b64 = message.Substring(2);
						while ((b64.Length%4)!=0)
						{
							b64 += "=";
						}
						try
						{
							byte[] decoded = Convert.FromBase64String(b64);
							finalMessage = System.Text.Encoding.UTF8.GetString(decoded);
						}
						catch (System.ArgumentNullException) { /* Assume not Base64 */ }
						catch (System.FormatException) { /* Assume not Base64 */ }
					}
				}
				SuggestionEvent(finalMessage);
			}
		}

		/// <summary>
		/// Register a URI scheme so we can be triggered externally.
		/// </summary>
		private void Register()
		{
			Assembly launcher = Assembly.GetEntryAssembly();
			String path = System.IO.Path.GetDirectoryName(launcher.Location);
			String[] handlers = { "USS.exe" };
			foreach (String handler in handlers)
			{
				String schemeHandler = Path.Combine(path, handler);
				if (File.Exists(schemeHandler))
				{
					RegistryKey schemeRoot = Registry.CurrentUser.OpenSubKey("Software\\Classes", true);
					if (schemeRoot != null)
					{
						RegistryKey edl = schemeRoot.CreateSubKey("edlaunch");
						edl.SetValue("", "URL:Elite Dangerous Launch Protocol");
						edl.SetValue("URL Protocol", "");
						RegistryKey icon = edl.CreateSubKey("DefaultIcon");
						icon.SetValue("", "\"" + launcher.Location + ",1\"");
						RegistryKey command = edl.CreateSubKey("shell\\open\\command");
						command.SetValue("", "\"" + schemeHandler + "\" \"%1\"");
						m_registered = true;
					}
					return;
				}
			}
		}

		/// <summary>
		/// Deregister the URI scheme so we do not receive events.
		/// </summary>
		private void Deregister()
		{
			try
			{
				if ((m_registered) && (!PermanentSchemeRegistration))
				{
					RegistryKey schemeRoot = Registry.CurrentUser.OpenSubKey("Software\\Classes", true);
					schemeRoot.DeleteSubKeyTree("edlaunch");
				}
			}
			catch (System.Exception ex)
			{
				string Message = ex.Message;
			}
		}

		/// <summary>
		/// Function run on a separate thread to implement the server without
		/// blocking the main application.
		/// </summary>
		/// <param name="data">Creating Suggestion object.</param>
		private static void ServerThread(object data)
		{
			Suggestion suggestion = data as Suggestion;

			if (suggestion==null)
			{
				return;
			}

			try
			{
				// Internal buffer size is 1024 at the time of writing so there
				// is little point in allocating a larger buffer ourselves.
				const int BufferSize = 1024; 
				Byte[] rawmessage = new Byte[BufferSize];
				char[] chars = new char[BufferSize];
				Decoder decoder = Encoding.UTF8.GetDecoder();

				NamedPipeServerStream pipeServer = new NamedPipeServerStream(suggestion.Name);

				while (suggestion.m_running)
				{
					try
					{
						pipeServer.WaitForConnection();

						int numbytes = pipeServer.Read(rawmessage, 0, BufferSize);
						StringBuilder fullMessage = new StringBuilder(BufferSize);
						while (numbytes > 0)
						{
							int numChars = decoder.GetCharCount(rawmessage, 0, numbytes);
							int decoded = decoder.GetChars(rawmessage, 0, numbytes, chars, 0, false);
							String message = new String(chars, 0, numChars);
							fullMessage.Append(message);
							numbytes = pipeServer.Read(rawmessage, 0, BufferSize);
						}
						pipeServer.Disconnect();
						if (fullMessage.Length!=0)
						{
							suggestion.Process(fullMessage.ToString());
						}
					}
					catch (ThreadInterruptedException)
					{
						// Main application interrupted the wait.
					}
				}
			}
			catch (System.Exception ex)
			{
				String message = ex.Message;
				suggestion.Process("!"+message);
			}
		}
	}
}
