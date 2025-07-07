using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;


namespace SteamIF
{
	class LogHelper
	{
		private String m_path;
		public LogHelper(String path)
		{
			m_path = path;
			if (m_path!=null)
			{
				String dirname = Path.GetDirectoryName(m_path);
				if (!Directory.Exists(dirname))
				{
					Directory.CreateDirectory(dirname);
				}
				File.Delete(m_path);
				Log("Reset Log");
			}
		}

		public void Log(String message)
		{
			if (m_path!=null)
			{
				using (StreamWriter log = File.AppendText(m_path))
				{
					log.WriteLine(message);
					log.Flush();
				}
			}
		}
	}

	public class SteamInterface : SafeHandleZeroOrMinusOneIsInvalid
	{
		const String dllName = "steam_api64.dll";
		const String STEAMUSER_INTERFACE_VERSION = "SteamUser019";
		const bool c_saveSteamLog = false;

		bool m_haveSteam = false;
		UInt64 m_steamUserID = 0;
		const UInt32 INVALID_TOKEN_HANDLE = 0;
		UInt32 m_sessionTokenHandle = INVALID_TOKEN_HANDLE;
		const UInt32 MIN_BUFFER_SIZE = 1024;
		Byte[] m_rawSessionToken = null;
		String m_hexSessionToken = null;
		String m_steamLogPath = null;
		LogHelper m_log;

		public SteamInterface()
			: base(true)
		{
		}

		public bool Connected
		{
			get
			{
				return m_haveSteam
					&& (m_steamUserID != 0)
					&& (m_sessionTokenHandle != INVALID_TOKEN_HANDLE)
					&& (m_hexSessionToken != null);
			}
		}

		public UInt64 UserID
		{
			get
			{
				return m_steamUserID;
			}
		}

		public String SessionToken
		{
			get
			{
				return m_hexSessionToken;
			}
		}

		public override bool IsInvalid
		{
			get
			{
				if (m_haveSteam)
				{
					if (m_sessionTokenHandle!=INVALID_TOKEN_HANDLE)
					{
						return false;
					}
				}
				return base.IsInvalid;
			}
		}

		/// <summary>
		/// Get a new session token.
		/// 
		/// Although unlikely it is possible that the user could log out of
		/// Steam and log in as a different user. If that happens while the
		/// launcher is running unless we can refresh the current user/token
		/// strange things may happen.
		/// </summary>
		public void Refresh()
		{
			ReleaseSessionToken();
			GetSessionToken();
		}

		/// <summary>
		/// This only appears to get called if IsInvalid returns false so we
		/// could probably not repeat the tests here but since it only happens
		/// once per execution keep them in for now.
		/// </summary>
		/// <returns></returns>
		override protected bool ReleaseHandle()
		{
			if (m_haveSteam)
			{
				ReleaseSessionToken();
				SteamAPI_Shutdown();
				m_log.Log("SteamAPI_Shutdown Called");
			}
			return true;
		}

		public void Initialise()
		{
			String path = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			String dllPath = Path.Combine(path, dllName);
			String logPath = Path.Combine(path, "logs");
			m_steamLogPath = c_saveSteamLog ? Path.Combine(logPath, "steam.log") : null;
			m_log = new LogHelper(m_steamLogPath);

			if (File.Exists(dllPath))
			{
				try
				{
					m_haveSteam = SteamAPI_Init();
					if (m_haveSteam)
					{
						m_log.Log("SteamAPI_Init: SUCCESS : Steam API available.");
					}
					else
					{
						m_log.Log("SteamAPI_Init: FAILED : Steam API not available");
                        if (System.Diagnostics.Debugger.IsAttached)
                        {
                            System.Diagnostics.Trace.WriteLine($"Check for missing {Path.Combine(path, "steam_appid.txt")}");
                        }
					}
				}
				catch (System.Exception ex)
				{
					String result = ex.Message;
					m_haveSteam = false;
					m_log.Log("SteamAPI_Init: FAILED : "+result);
				}
			}
		}

		private void GetSessionToken()
		{
			if (m_haveSteam)
			{
				IntPtr iUser = GetUser();
				if (iUser!=IntPtr.Zero)
				{
					m_log.Log("GetSessionToken : Got User.");
					m_steamUserID = SteamAPI_ISteamUser_GetSteamID(iUser);
					if (m_steamUserID!=0)
					{
						m_log.Log(String.Format("GetSessionToken : Got User ID {0}", m_steamUserID));
						UInt32 bufferUsed = 0;
						m_rawSessionToken = new Byte[MIN_BUFFER_SIZE];

						m_sessionTokenHandle = SteamAPI_ISteamUser_GetAuthSessionTicket(iUser, m_rawSessionToken, MIN_BUFFER_SIZE, ref bufferUsed);
						if (m_sessionTokenHandle != INVALID_TOKEN_HANDLE)
						{
							m_log.Log(String.Format("GetSessionToken : Steam returned valid Token Handle {0}", m_sessionTokenHandle));
							m_hexSessionToken = ByteToHexString(m_rawSessionToken, bufferUsed);
						}
						else
						{
							m_log.Log("GetSessionToken : Steam returned INVALID_TOKEN_HANDLE");
						}
					}
					else
					{
						m_log.Log("GetSessionToken : Failed to get User ID");
					}
				}
				else
				{
					m_log.Log("GetSessionToken : No User Handle.");
				}
			}
		}

		private void ReleaseSessionToken()
		{
			if (m_haveSteam)
			{
				if (m_sessionTokenHandle != INVALID_TOKEN_HANDLE)
				{
					IntPtr iUser = GetUser();
					if (iUser!=IntPtr.Zero)
					{
						m_log.Log(String.Format("ReleaseSessionToken : Releasing Token {0} for user.", m_sessionTokenHandle));
						SteamAPI_ISteamUser_CancelAuthTicket(iUser, m_sessionTokenHandle);
					}
					else
					{
						m_log.Log("ReleaseSessionToken : No user handle.");
					}
					m_sessionTokenHandle = INVALID_TOKEN_HANDLE;
					m_hexSessionToken = "";
				}
			}
		}

		private String ByteToHexString(Byte[] source, UInt32 count)
		{
			char[] hex = new char[count * 2];
			string lookup = "0123456789ABCDEF";
			for (int i=0; i<count; ++i)
			{
				hex[(2 * i)] = lookup[source[i] >> 4];
				hex[(2 * i) + 1] = lookup[source[i] & 15];
			}
			return new String(hex);
		}

		/// <summary>
		/// Steam API notes that pointers into the API should not be cached so
		/// extract the steps necessary to get the ISteamUser interface and call
		/// it on demand.
		/// </summary>
		/// <returns>Instance pointer for the current Steam user.</returns>
		private IntPtr GetUser()
		{
			IntPtr client = SteamClient();
			if (client!=IntPtr.Zero)
			{
				m_log.Log("GetUser: Have valid SteamClient");
				Int32 hPipe = SteamAPI_GetHSteamPipe();
				if (hPipe!=0)
				{
					m_log.Log(String.Format("GetHSteamPipe : Got valid pipe handle {0}", hPipe));
					Int32 hUser = SteamAPI_ISteamClient_ConnectToGlobalUser(client, hPipe);
					if (hUser!=0)
					{
						m_log.Log(String.Format("ConnectToGlobalUser : Got valid user handle {0}", hUser));
						IntPtr iUser = SteamAPI_ISteamClient_GetISteamUser(client, hUser, hPipe, STEAMUSER_INTERFACE_VERSION);
						if (iUser!=IntPtr.Zero)
						{
							m_log.Log("GetISteamUser : Have valid user pointer.");
							return iUser;
						}
						else
						{
							m_log.Log("GetISteamuser : Null user pointer.");
						}
					}
					else
					{
						m_log.Log("ConnectToGlobalUser : Got invalid user handle");
					}
				}
				else
				{
					m_log.Log("GetHSteamPipe : Invalid PIPE handle.");
				}
			}
			else
			{
				m_log.Log("GetUser: Have null SteamClient");
			}
			return IntPtr.Zero;
		}

		[DllImport(dllName, EntryPoint = "SteamAPI_Init")]
		static extern bool SteamAPI_Init();

		[DllImport(dllName, EntryPoint = "SteamAPI_Shutdown")]
		static extern void SteamAPI_Shutdown();

		[DllImport(dllName, EntryPoint = "SteamClient")]
		static extern IntPtr SteamClient();

		[DllImport(dllName, EntryPoint = "SteamAPI_GetHSteamPipe")]
		static extern Int32 SteamAPI_GetHSteamPipe();

		[DllImport(dllName, EntryPoint = "SteamAPI_ISteamClient_ConnectToGlobalUser")]
		static extern Int32 SteamAPI_ISteamClient_ConnectToGlobalUser(IntPtr instance, Int32 pipe);

		[DllImport(dllName, EntryPoint = "SteamAPI_ISteamClient_GetISteamUser")]
		static extern IntPtr SteamAPI_ISteamClient_GetISteamUser(IntPtr instance, Int32 user, Int32 pipe, String version);

		[DllImport(dllName, EntryPoint = "SteamAPI_ISteamUser_GetSteamID")]
		static extern UInt64 SteamAPI_ISteamUser_GetSteamID(IntPtr instance);

		[DllImport(dllName, EntryPoint = "SteamAPI_ISteamUser_GetAuthSessionTicket")]
		static extern UInt32 SteamAPI_ISteamUser_GetAuthSessionTicket(IntPtr instance, Byte[] buffer, UInt32 size, ref UInt32 count );

		[DllImport(dllName, EntryPoint = "SteamAPI_ISteamUser_CancelAuthTicket")]
		static extern void SteamAPI_ISteamUser_CancelAuthTicket(IntPtr instance, UInt32 hTicket);
	}
}
