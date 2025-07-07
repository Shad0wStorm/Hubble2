using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;

namespace USS
{
	class Program
	{
		/// <summary>
		/// Connect to the Launcher named pipe and send the passed transmission
		/// text.
		/// </summary>
		/// <param name="transmission"></param>
		static void SendToLauncher(String transmission)
		{
			using (NamedPipeClientStream client = new NamedPipeClientStream(".", "edlaunch_uss", PipeDirection.Out))
			{
				Console.WriteLine("Connecting....");
				try
				{
					client.Connect(2000);
					using (StreamWriter sw = new StreamWriter(client))
					{
						sw.Write(transmission);
					}
				}
				catch (System.TimeoutException)
				{
					MessageBox.Show("Failed to connect to an active Elite launcher.\n\nPlease run manually when ready.");
				}
			}
		}

		/// <summary>
		/// Generate a string of random hex digits of a given length.
		/// 
		/// The launcher will not do anything useful with it but it checks the
		/// complete transmission is received.
		/// 
		/// Note that the string is generated using Random, but the length is
		/// used as the seed as well as the length so the string will always be
		/// the same for any given length.
		/// </summary>
		/// <param name="count"></param>
		/// <returns></returns>
		static String RandomHexString(Int32 count)
		{
			Random source = new Random(count);
			StringBuilder result = new StringBuilder(count, count);
			String hexChars = "0123456789ABCDEF";
			for (int h = 0; h<count; ++h)
			{
				result.Append(hexChars[source.Next(16)]);
			}
			return result.ToString();
		}

		static void Main(string[] args)
		{
			if (args.Length>0)
			{
				try
				{
					Uri uri = new Uri(args[0]);
					if (uri.Scheme=="edlaunch")
					{
						String transmit = null;
						if (uri.Host=="local")
						{
							// Handle the 'random' local path by generating a
							// string of hex characters and sending that instead
							// e.g. /random/16 will get expanded to 
							// /random/18FF6817F21FAC20
							const String c_random = "/random/";
							transmit = uri.LocalPath;
							if (transmit.StartsWith(c_random,StringComparison.InvariantCultureIgnoreCase))
							{
								String request = uri.LocalPath.Substring(c_random.Length);
								Int32 count = 0;
								if (Int32.TryParse(request, out count))
								{
									transmit = c_random + RandomHexString(count);
								}
								else
								{
									MessageBox.Show("Invalid random data count " + uri.LocalPath);
								}
							}
						}
						else
						{
							MessageBox.Show("Unsupported host : " + uri.Host);
						}
						if (!String.IsNullOrEmpty(transmit))
						{
							SendToLauncher(transmit);
						}
					}
					else
					{
						MessageBox.Show("Unrecognised scheme : "+uri.Scheme);
						
					}
				}
				catch (UriFormatException ex)
				{
					MessageBox.Show("Invalid Uri");
				}
			}
		}
	}
}
