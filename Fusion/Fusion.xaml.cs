using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

using Utils;

namespace Fusion
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class FusionWindow : Window
    {
        private ClientInterface m_interface = new ClientInterface();

        public FusionWindow()
        {
            InitializeComponent();

            OptionsParser op = new OptionsParser();
            WindowSetup windowSetup = new WindowSetup();
            windowSetup.SetParser(op);
            
            op.AddCommand("--address", DetermineInitialPath());
            op.Parse();

            if (op.HasErrors)
            {
                MessageBox.Show(op.ErrorSummary());
            }

            windowSetup.SetupWindow(this);

            try
            {
                System.Uri uri = new System.Uri(op.Get("--address"));

                Browser.Navigate(uri);
                Browser.ObjectForScripting = m_interface;
                m_interface.SetBrowser(Browser);
            }
            catch (System.UriFormatException)
            {
                MessageBox.Show("Invalid address: " + op.Get("--address"));
            }
        }

        private void Browser_Navigated(object sender, NavigationEventArgs e)
        {
            m_interface.CollectSessionToken(e.Uri.ToString());

        }

        private void Browser_LoadCompleted(object sender, NavigationEventArgs e)
        {
        }

        private String DetermineInitialPath()
        {
            // This needs to do something sensible and probably only one simple
            // thing by the time it gets used properly but we also need to be
            // able to test it sensibly.
            //
            // Options:
            // 1) Use a fixed address. Fine if the address never changes and
            //    the real server can also be used for testing.
            // 2) Use a registry key. Possibly useful as a final solution if a
            //    fixed address is not acceptable. The key needs to be set
            //    initially which means it is not suitable for testing.
            // 3) Existing address option. Easier to set than a registry key
            //    but still needs to be set locally for testing.
            // 4) The old climb the path from the executable to find a partial
            //    path trick. Good for testing, not suitable for release since
            //    it may hit issues with people moving files around or running
            //    the executable from odd directories.
            // 5) Use an alternate URL for testing, but with an active server.
            //    Requires a running server and an option to switch to test
            //    mode but probably the best option once a test server is
            //    available.
            //
            // Initial solution to combine as many as possible:
            // Use 4 to find a "Start.html" file which works for local testing.
            // Initially this will load a local html file for local testing.
            // It can redirect to a testing server when one is available
            // without requiring an application change.
            // When the real server address is known it can be set as a fall
            // back when Start.html is not found.
            String startPage = "LocalPages/Start.html";

            String initialPage = m_interface.FindLocalDirectoryEntry(startPage);

            if (String.IsNullOrEmpty(initialPage))
            {
                initialPage = "http://intranet.corp.frontier.co.uk/";
            }

            return initialPage;
        }
    }
}
