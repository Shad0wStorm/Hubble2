using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;

using ClientSupport;
using LocalResources;

namespace FrontierSupport
{
	public class DriverVersionCheckResult
	{
		public String Message;
	}

	public class DriverVersionCheck
	{
		public DriverVersionCheck(Project p)
		{
			m_project = p;
#if !MONO
			if (String.IsNullOrEmpty(p.VideoVersion))
			{
				return;
			}

			String precheck = p.PrettyName + "#" + p.VideoVersion;
			if (s_reported.Contains(precheck))
			{
				// Already checked this combination, user has been warned.
				return;
			}
			s_reported.Add(precheck);

			String[] checks = p.VideoVersion.Split(';');

			ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

			foreach (ManagementObject mo in searcher.Get())
			{
				PropertyData description = mo.Properties["Description"];
				String descriptionValue = description.Value as String;
				if (!String.IsNullOrEmpty(descriptionValue))
				{
					String dl = descriptionValue.ToLowerInvariant();
					PropertyData driver = mo.Properties["DriverVersion"];
					String dv = driver.Value as String;
					if (!String.IsNullOrEmpty(dv))
					{
						FileVersionHelper driverVersion = new FileVersionHelper(dv);
						foreach (String check in checks)
						{
							String[] checking = check.Split('=');
							if (checking.Length < 2)
							{
								// Invalid check, no point in reporting since there is
								// nothing the user can do about it.
								continue;
							}
							String driverIdent = checking[0].ToLowerInvariant();
							if (dl.StartsWith(driverIdent))
							{
								FileVersionHelper requiredVersion = new FileVersionHelper(checking[1]);
								if (requiredVersion > driverVersion)
								{
									if (m_results == null)
									{
										m_results = new List<DriverVersionCheckResult>();
									}
									DriverVersionCheckResult r = new DriverVersionCheckResult();
									r.Message = String.Format(LocalResources.Properties.Resources.VideoDriverVersionCheckFailed,
										descriptionValue, dv, checking[1]);
									m_results.Add(r);
								}
							}
						}
					}
				}
			}
#endif
		}

		public DriverVersionCheckResult Next()
		{
			if (m_results != null)
			{
				DriverVersionCheckResult result = m_results[0];
				m_results.Remove(result);
				if (m_results.Count == 0)
				{
					m_results = null;
				}
				return result;
			}
			return null;
		}

		Project m_project;
		List<DriverVersionCheckResult> m_results = null;
		static HashSet<String> s_reported = new HashSet<String>();
	}
}
