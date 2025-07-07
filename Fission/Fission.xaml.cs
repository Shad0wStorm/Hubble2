using System;
using System.Linq;
using System.Windows;
using ClientSupport;
using FORCServerSupport;
using FrontierSupport;
using SharedControls;

namespace Fission
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class FissionWindow : Window
    {
        public string ExeDirectory { get { return System.IO.Path.GetDirectoryName(Application.ResourceAssembly.Location); } }

        FORCManager m_manager;
        LoginDialog m_login;
        ProjectList m_projects;

        public FissionWindow()
        {
            InitializeComponent();
            PositionWindow();
            InitializeManager();
            Update();
        }

        private void InitializeManager()
        {
            m_manager = new FORCManager();

            if (m_manager.ServerURL != null)
            {
                Uri connection = new Uri(m_manager.ServerURL);
                // Uncomment the following block if mock server logic is required
                /* if (connection.Scheme == "mock")
                {
                    MockServerConnection mockServer = new MockServerConnection(m_manager);
                    m_manager.ServerConnection = mockServer;
                    if (mockServer.RequiresMockMachineIdentifier)
                    {
                        m_manager.MachineIdentifier = new MockMachineIdentifier();
                    }
                } */
            }

            if (m_manager.ServerConnection == null)
            {
                // Use a real server connection
                FORCServerConnection realServer = new FORCServerConnection(m_manager);
                m_manager.ServerConnection = realServer;
            }

            if (m_manager.MachineIdentifier == null)
            {
                m_manager.MachineIdentifier = new FrontierMachineIdentifier();
            }
        }

        public void PositionWindow()
        {
            double left = Properties.Settings.Default.Left;
            double top = Properties.Settings.Default.Top;
            double width = Properties.Settings.Default.Width;
            double height = Properties.Settings.Default.Height;

            // If window dimensions are invalid, center it on the screen
            if (width < 0)
            {
                width = Width;
                left = (System.Windows.SystemParameters.PrimaryScreenWidth - width) / 2.0;
            }
            if (height < 0)
            {
                height = Height;
                top = (System.Windows.SystemParameters.PrimaryScreenHeight - height) / 2.0;
            }

            Top = top;
            Left = left;
            Height = height;
            Width = width;

            if (Properties.Settings.Default.Maximised)
            {
                WindowState = WindowState.Maximized;
            }

            Closing += SaveWindowPosition;
        }

        private void SaveWindowPosition(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Left = Left;
            Properties.Settings.Default.Top = Top;
            Properties.Settings.Default.Width = Width;
            Properties.Settings.Default.Height = Height;
            Properties.Settings.Default.Maximised = WindowState == WindowState.Maximized;
            Properties.Settings.Default.Save();
        }

        public void Update()
        {
            if (!m_manager.Authorised)
            {
                ShowLoginDialog();
            }
            else
            {
                ShowProjectsList();
            }
        }

        private void ShowLoginDialog()
        {
            if (m_login == null)
            {
                m_login = new LoginDialog();
                m_login.SubmitUserDetails += new EventHandler(LoginSubmitted);
            }

            m_login.SetUser(m_manager.UserDetails);
            m_login.SetRequiresTwoFactor(m_manager.RequiresTwoFactor);
            m_login.SetStatus(m_manager.Status);
            ContentArea.Content = m_login;
            ContentArea.DataContext = m_login;
            m_login.SetFocus();
        }

        private void ShowProjectsList()
        {
            if (m_projects == null)
            {
                m_projects = new ProjectList(m_manager);
            }
            ContentArea.Content = m_projects;
            ContentArea.DataContext = m_projects;
            m_projects.Update();
        }

        private void LoginSubmitted(object sender, EventArgs e)
        {
            if (m_login == sender)
            {
                m_login.GetUser(m_manager.UserDetails);
                if (m_login.RememberMe)
                {
                    m_manager.SaveUserDetails(true);
                }
                m_manager.Authenticate(false);
                Update();
            }
        }
    }
}
