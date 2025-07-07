using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

using ClientSupport;

namespace Fusion
{
    public class BrowserProgressReporter : ProgressMonitor
    {
        private WebBrowser m_browser = null;

        public BrowserProgressReporter(WebBrowser browser)
        {
            m_browser = browser;
        }

        public override void StartAction(String key, String action)
        {
            m_browser.InvokeScript("BeginAction", new object[] { key, action });
        }

        public override void StartProgressAction(String key, String action, Int64 target)
        {
            m_browser.InvokeScript("BeginProgressAction", new object[] { key, action, target });
        }

        public override void ReportActionProgress(String key, Int64 progress)
        {
            m_browser.InvokeScript("ReportProgress", new object[] { key, progress });
        }

        public override void CompleteAction(String key)
        {
            m_browser.InvokeScript("CompleteAction", new object[] { key });
        }

        public override void Complete(String key)
        {
            m_browser.InvokeScript("Completion", new object[] { key });
        }

        public override void Fail(String key, String message)
        {
            m_browser.InvokeScript("FailAction", new object[] { key, message });
        }
    }
}
