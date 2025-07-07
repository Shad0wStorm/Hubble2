using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClientSupport.Utils
{
    public class OSIdent
    {
        /// <summary>
        /// Get an OS identifier
        /// </summary>
        /// <returns></returns>
        public static String GetOSIdent()
        {
            OperatingSystem system = Environment.OSVersion;
            PlatformID id = system.Platform;
            if (id == PlatformID.MacOSX)
            {
                return "Mac64";
            }
            String pa = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
			if ((pa == "AMD64") || (pa == "IA64"))
			{
				return "Win64";
			}
			else
			{
				pa = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432");
				if ((pa == "AMD64") || (pa == "IA64"))
				{
					// Somehow the user has set the launcher to a 32bit
					// application on a 64bit machine so declare ourselves to
					// be 64bit aware.
					return "Win64";
				}
			}
            return "Win32";
        }
    }
}
