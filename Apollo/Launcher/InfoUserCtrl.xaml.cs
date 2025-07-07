//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! InfoUserCtrl UserControl
//
//! Author:     Alan MacAree
//! Created:    23 Dec 2022
//----------------------------------------------------------------------

using System;
using System.Threading;
using System.Windows.Controls;
using CBViewModel;
using System.Diagnostics;
using System.Windows.Media;
using ClientSupport;
using System.Windows;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for InfoUserCtrl.xaml
    /// </summary>
    public partial class InfoUserCtrl : UserControl
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public InfoUserCtrl()
        {
            InitializeComponent();

            ClearServiceStatusInformation();
        }

        /// <summary>
        /// Sets the LauncherWindow && CobraBayView held by this object
        /// </summary>
        /// <param name="_launcherWindow">The launcher window to send updates to</param>
        /// <param name="_cobraBayView">CobraBayView to get the data from</param>
        public void SetUpObject( LauncherWindow _launcherWindow,  CobraBayView _cobraBayView )
        {
            m_cobraBayView = _cobraBayView;
            Debug.Assert( _launcherWindow != null );
            m_launcherWindow = _launcherWindow;

            ClearServiceStatusInformation();
            UpdateServerStatus();
            ServiceStatusInformationVisibility();
        }

        /// <summary>
        /// Called when this user control is initialised
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnInitialized( object sender, EventArgs e )
        {
            // Create a timer for check the server status
            CreateOrChangeVariableTimer();
        }

        /// <summary>
        /// Clears the server status information
        /// </summary>
        private void ClearServiceStatusInformation()
        {
            // Clear the data
            PART_ProjectName.Content = string.Empty;
            PART_ServerStatus.Content = string.Empty;
        }

        /// <summary>
        /// Sets the two changable Labels to:
        /// Visibility.Visible or Visibility.Collapsed.
        /// If the label contains text then it is set to Visibility.Visible,
        /// else it is set to System.Windows.Visibility.Collapsed
        /// </summary>
        private void ServiceStatusInformationVisibility()
        {
            if ( !string.IsNullOrWhiteSpace( PART_ProjectName.Content.ToString() ) )
            {
                SetControlVisibility( PART_ProjectName, System.Windows.Visibility.Visible );
            }
            else
            {
                SetControlVisibility( PART_ProjectName, System.Windows.Visibility.Collapsed );
            }

            if ( !string.IsNullOrWhiteSpace( PART_ServerStatus.Content.ToString() ) )
            {
                SetControlVisibility( PART_ServerStatus, System.Windows.Visibility.Visible );
            }
            else
            {
                SetControlVisibility( PART_ServerStatus, System.Windows.Visibility.Collapsed );
            }
        }

        /// <summary>
        /// Sets a UIElement Visibility to a value, but only if it is not
        /// already set to that value.
        /// </summary>
        /// <param name="_uIElement">The UIElement to set</param>
        /// <param name="_visibility">The Visibility to set the _uIElement to</param>
        private void SetControlVisibility( UIElement _uIElement, System.Windows.Visibility _visibility )
        {
            Debug.Assert( _uIElement != null );
            if ( _uIElement != null )
            {
                if ( _uIElement.Visibility != _visibility )
                {
                    _uIElement.Visibility = _visibility;
                }
            }
        }

        /// <summary>
        /// Called via a timer, this uses Invoke to call the UI to update the
        /// server status
        /// </summary>
        /// <param name="data">Not used</param>
        public void TimedServerStatus( object data )
        {
            CreateOrChangeVariableTimer();
            Dispatcher.Invoke( new Action( () => { UpdateServerStatus(); } ) );
        }

        /// <summary>
        /// Updates the server status.
        /// </summary>
        public void UpdateServerStatus()
        {
            // Set the application verison
            SetApplicationVersion();

            try
            {
                string currentServerStatus = LocalResources.Properties.Resources.TXT_ServerStatusUnknown;

                bool updatedTheUIServerStatusOK = false;
                lock ( m_cobraBayViewLock )
                {
                    if ( m_cobraBayView != null )
                    {
                        FORCManager fORCManager = m_cobraBayView.Manager();

                        Debug.Assert( fORCManager != null );

                        // Display product in if the user is logged on
                        if ( fORCManager != null )
                        {
                            if ( fORCManager.Authorised )
                            {
                                // Update the project status
                                string serverStatusText = null;
                                string serverStatusMessage = null;
                                ServerStatusState serverStatusState = (ServerStatusState)m_cobraBayView.GetServerStatus( out serverStatusText,
                                                                                                                     out serverStatusMessage );
                                UpdateTheUI( serverStatusState, serverStatusText );
                                UpdateLauncherWindow( serverStatusState, serverStatusText, serverStatusMessage );
                                updatedTheUIServerStatusOK = true;
                            }
                        }
                    }
                }

                // The server status may not have been updated, as we don't know what it is, clear it for
                // both this UI and let the launcher window know
                if ( !updatedTheUIServerStatusOK )
                {
                    Dispatcher.Invoke( new Action( () =>
                    {
                        UpdateLauncherWindow( ServerStatusState.OK, string.Empty, string.Empty );
                        ClearServiceStatusInformation();
                    } ) );
                }
            }
            catch( Exception )
            {
                // Keep quite, don't crash
                ClearServiceStatusInformation();
            }

            ServiceStatusInformationVisibility();
        }

        /// <summary>
        /// Sets the application version, this gets set only once. Once the value
        /// has been set, the method does nothing
        /// </summary>
        private void SetApplicationVersion()
        {
            lock ( m_cobraBayViewLock )
            {
                if ( m_cobraBayView != null )
                {
                    if ( string.IsNullOrWhiteSpace( (string)PART_LauncherVersion.Content ) )
                    {
                        m_cobraBayView.UpdateCobraBayVersionInfo();
                        string resourceSrting = LocalResources.Properties.Resources.TXT_LauncherVersion;
                        PART_LauncherVersion.Content = String.Format( resourceSrting, m_cobraBayView.CobraBayVersionInfo );
                    }
                }
            }
        }

        /// <summary>
        /// Updates the UI with the passed server status
        /// </summary>
        /// <param name="_serverStatusState">The server status</param>
        /// <param name="_serverStatusText">The server status text</param>
        private void UpdateTheUI( ServerStatusState _serverStatusState , string _serverStatusText )
        {
            // Set the status to empty if we don't get one
            string displayableServerStatusString = _serverStatusText;
            if ( string.IsNullOrWhiteSpace( displayableServerStatusString ) )
            {
                displayableServerStatusString = string.Empty;
            }
            else
            {
                displayableServerStatusString = string.Format( LocalResources.Properties.Resources.TXT_ServerStatus, _serverStatusText );
            }

            PART_ServerStatus.Content = displayableServerStatusString;

            // Set the Server Status colour
            switch ( _serverStatusState )
            {
                case ServerStatusState.NotOk:
                    PART_ServerStatus.Foreground = new SolidColorBrush( m_notOkColour );
                    break;
                case ServerStatusState.OK:
                    PART_ServerStatus.Foreground = new SolidColorBrush( m_OkColour );
                    break;
                case ServerStatusState.Maintainance:
                    PART_ServerStatus.Foreground = new SolidColorBrush( m_maintainanceColour );
                    break;
                default:
                    PART_ServerStatus.Foreground = new SolidColorBrush( m_notOkColour );
                    break;
            }


            // Update the project name
            string displayableProjectInfo = string.Empty;
            if ( m_cobraBayView != null )
            {
                Project activeProduct = m_cobraBayView.GetActiveProject();
                if ( activeProduct != null )
                {
                    displayableProjectInfo = activeProduct.PrettyName;
                    if ( !string.IsNullOrWhiteSpace( activeProduct.Name ) )
                    {
                        displayableProjectInfo = string.Format( c_productNameFormatter, displayableProjectInfo, activeProduct.Name );
                    }
                }
            }

            PART_ProjectName.Content = displayableProjectInfo;

            ServiceStatusInformationVisibility();
        }

        /// <summary>
        /// Updates the Launcher window
        /// </summary>
        /// <param name="_serverStatusState">The server status</param>
        /// <param name="_serverStatusText">The server status text</param>
        /// <param name="_serverStatusMessage">The server status message</param>
        private void UpdateLauncherWindow( ServerStatusState _serverStatusState, string _serverStatusText, string _serverStatusMessage )
        {
            if ( m_launcherWindow != null )
            {
                m_launcherWindow.ServerStatusUpdate( _serverStatusState, _serverStatusText, _serverStatusMessage );
            }
        }

        /// <summary>
        /// Creates or changes a variable timer between the possible timers we allow
        /// </summary>
        private void CreateOrChangeVariableTimer()
        {
            // Create it if it does not exist
            if ( m_serverStatusTimer == null )
            {
                int timer = c_oneMinuteInMS * c_minTimer;
                m_serverStatusTimer = new Timer( TimedServerStatus, null, timer, timer );
            }

            // Use a random number generator to select a number, this is
            // then used to calculate a time to use on the timer
            Random rnd = new Random();
            int timerSelection = rnd.Next( c_minTimer, c_maxTimer + 1 );
            int timeUsed = c_oneMinuteInMS * timerSelection;
            m_serverStatusTimer.Change( timeUsed, timeUsed );
        }

        /// <summary>
        /// Our CobraBayView, used to get the server status
        /// and our lock object
        /// </summary>
        private CobraBayView m_cobraBayView;
        static readonly object m_cobraBayViewLock = new object();

        /// <summary>
        /// The Launcher window to send updates to
        /// </summary>
        private LauncherWindow m_launcherWindow;

        /// <summary>
        /// Time used to check the status of the 
        /// Server
        /// </summary>
        private const int c_oneMinuteInMS = (60 * 1000); // 1 minute in milliseconds.
        private const int c_minTimer = 3;
        private const int c_maxTimer = 5;  
        private Timer m_serverStatusTimer;

        /// <summary>
        /// Possible server status state
        /// </summary>
        public enum ServerStatusState
        {
            NotOk = -1,
            Maintainance = 0,
            OK = 1
        };

        /// <summary>
        /// Our server status NOT OK colour
        /// </summary>
        private Color m_notOkColour = Colors.Red;

        /// <summary>
        /// Our server status maintainance colour
        /// </summary>
        private Color m_maintainanceColour = Colors.Red;

        /// <summary>
        /// Our server status OK colour
        /// </summary>
        private Color m_OkColour = Colors.LightGray;

        /// <summary>
        /// String used to format the product name, this is made up of
        /// {0} = Pretty Name
        /// {1} = SKU 
        /// </summary>
        private const string c_productNameFormatter = "{0} ({1})";
    }
}
