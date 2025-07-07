using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Controls;
using System.Windows.Threading;

using ClientSupport;

namespace Fusion
{
    [PermissionSet(SecurityAction.Demand, Name="FullTrust")]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public class ClientInterface
    {
        private WebBrowser m_browser = null;
        private String m_myDirectory;
        private String m_sessionToken;
        private String m_projectDirectory;
        private Dictionary<String, Project> m_projects = new Dictionary<String, Project>();
        private DispatcherTimer m_timer;
        private ProgressMonitor m_monitor;

        public ClientInterface()
        {
            String myPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            m_myDirectory = Path.GetDirectoryName(myPath);
            m_projectDirectory = FindLocalDirectoryEntry("Projects");
            if (!String.IsNullOrEmpty(m_projectDirectory))
            {
                m_myDirectory = Path.GetDirectoryName(m_projectDirectory);
            }

            m_timer = new DispatcherTimer();
            m_timer.Tick += new EventHandler(Timer_Tick);
            m_timer.Interval = new TimeSpan(0, 0, 2);
            m_timer.Start();
        }

        public String FindLocalDirectoryEntry(String target)
        {
            return PathExtensions.FindLocalDirectoryEntry(target, m_myDirectory);
        }

        public void SetBrowser(WebBrowser wb) { m_browser = wb; }

        // Class to provide interface for accessing client side services
        // from scripting in the contained browser instance.
        public String PopulateDataForProject(String project)
        {
            if (m_browser!=null)
            {
                // To avoid recompiling every time we want to try a different
                // state while the javascript side is being modified retrieve
                // the test data from the script.
                try
                {
                    object testDataObject = m_browser.InvokeScript("GetTestData", new object[] { project });
                    if (testDataObject!=null)
                    {
                        String testData = testDataObject.ToString();
                        if (!String.IsNullOrEmpty(testData))
                        {
                            return testData;
                        }
                    }
                }
                catch (Exception)
                {
                    // Assume this script does not provide test data so do not
                    // do anything.
                }
                Project active = GetProject(project);
                Dictionary<String, object> properties = new Dictionary<String, object>();
                AddSystemProperties(properties);
                active.Description(ref properties);
                JavaScriptSerializer s = new JavaScriptSerializer();
                return s.Serialize(properties);
            }
            return null;
        }

        private Project GetProject(String project)
        {
            Project result;
            if (!m_projects.ContainsKey(project))
            {
                result = new Project(project, m_projectDirectory);
                m_projects[project] = result;
            }
            else
            {
                result = m_projects[project];
            }
            return result;
        }

        public void DownloadFile(String project, String url, System.Int64 size)
        {
            if (m_monitor==null)
            {
                m_monitor = new BrowserProgressReporter(m_browser);
            }

            GetProject(project).DownloadFile(url, size, m_monitor);
        }

        public void RunInstaller(String project)
        {
            if (m_monitor == null)
            {
                m_monitor = new BrowserProgressReporter(m_browser);
            }

            GetProject(project).RunInstaller(m_monitor);
        }

        public void CollectSessionToken(String source)
        {
            // Extract a session token from the passed string. The token is
            // prefixed by the string "token=" and ends at the end of the
            // string or the character '&' whichever comes first.
            String tokenMarker = "token=";
            int tokenStart = source.IndexOf(tokenMarker);

            if (tokenStart < 0)
            {
                // Token not found
                return;
            }

            tokenStart += tokenMarker.Length;
            int tokenEnd = source.IndexOf("&", tokenStart);
            if (tokenEnd < 0)
            {
                tokenEnd = source.Length;
            }
            m_sessionToken = source.Substring(tokenStart, tokenEnd - tokenStart);
        }

        public void AddSystemProperties(Dictionary<String,object> properties)
        {
            String drive = Path.GetPathRoot(m_myDirectory);

            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                if (d.IsReady && (d.Name == drive))
                {
                    properties["diskfree"] = d.TotalFreeSpace;
                }
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (m_monitor == null)
            {
                m_monitor = new BrowserProgressReporter(m_browser);
            }

            foreach (Project p in m_projects.Values)
            {
                p.UpdateStatus(m_monitor);
            }
        }
    }
}
