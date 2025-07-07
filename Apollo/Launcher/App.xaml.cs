//----------------------------------------------------------------------
//! Copyright(c) 2022 Frontier Development Plc
//----------------------------------------------------------------------

//----------------------------------------------------------------------
//! App, our start point for our application
//
//! Author:     Alan MacAree
//! Created:    12 Aug 2022
//----------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;

using CBViewModel;
using JSONConverters;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Used to determine if we are restarting
        /// </summary>
        public bool Restart { get; set; } = false;

        /// <summary>
        /// Restart arguments
        /// </summary>
        public List<String> RestartExtraArgs { get; set; } = new List<string>();


        private void UnitTest_JsonFeaturedProduct()
        {
            // LauncherWindow is the concretion of the UI interface needed for a CobraBayView object.
            LauncherWindow newUI = new LauncherWindow();
            CobraBayView cobraBayView = new CobraBayView(newUI);
            // C# doesn't recognist JSONs \/ as an escape character, so I think \\/ is the right representation.
            string jsonString = "{\"media\":[{\"url\":\"https:\\/\\/player.vimeo.com\\/video\\/261473851?autoplay=1&loop=1&muted=1\",\"thumbnail\":\"media\\/catalog\\/product\\/frontier\\/thumbnails\\/VultureRaider.jpg\"}]}";
            Featured feature = JsonConverter.JsonToFeaturedProducts(jsonString, cobraBayView);
//            Debug.Assert(false, feature.ToString() );
        }

        /// <summary>
        /// Called on application start
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ApplicationStart( object sender, StartupEventArgs e )
        {
            // UnitTest_JsonFeaturedProduct();

            try
            {
                // Setup unhandled Exceptions
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.UnhandledException += new UnhandledExceptionEventHandler( HandleTheUnhandled );

                // Force an upgrade of the user settings so we pick up any
                // configured language before we try to use the language.
                CBViewModel.CobraBayView.UpgradeUserSettings();

                String activeLanguage = CBViewModel.Properties.Settings.Default.LanguageOverride;

                // Try and get a default language from the PC
                if ( String.IsNullOrEmpty( activeLanguage ) )
                {
                    activeLanguage = LanguageHelper.GetDefaultLanguageString();
                }

                if ( !String.IsNullOrEmpty( activeLanguage ) )
                {
                    try
                    {
                        Thread.CurrentThread.CurrentUICulture = new CultureInfo( activeLanguage );
                    }
                    catch ( System.ArgumentException )
                    {
                        // Whatever the active language was it was not recognised
                        // as a valid language so revert to the system default.
                        CBViewModel.Properties.Settings.Default.LanguageOverride = "";
                        CBViewModel.Properties.Settings.Default.Save();
                    }
                    catch ( Exception )
                    {
                        // Some other exception, e.g. the neutral culture exception
                        // seen in the wild.
                        CBViewModel.Properties.Settings.Default.LanguageOverride = "";
                        CBViewModel.Properties.Settings.Default.Save();
                    }
                }

                // This uses the GUID lifted directly from the old launcher.
                String mutexId = string.Format("Global\\{{{0}}}", c_applicationGuidString);

                m_singleInstanceMutex = new Mutex( false, mutexId );
                MutexAccessRule aer = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
                MutexSecurity securitySettings = new MutexSecurity();
                securitySettings.AddAccessRule( aer );
                m_singleInstanceMutex.SetAccessControl( securitySettings );

                bool hasHandle = false;
                try
                {
                    try
                    {
                        hasHandle = m_singleInstanceMutex.WaitOne( 0, false );
                    }
                    catch ( AbandonedMutexException )
                    {
                        // Mutex still exists, but is not attached, so we take it
                        // over.
                        hasHandle = true;
                    }
                }
                catch ( System.Exception )
                {
                    // Do nothing
                }

                if ( !hasHandle )
                {
                    MessageBox.Show( LocalResources.Properties.Resources.ApplicationAlreadyRunning );
                    Application.Current.Shutdown();
                    m_singleInstanceMutex = null;
                }
            }
            catch ( System.Exception ex )
            {
                MessageBox.Show( String.Format( LocalResources.Properties.Resources.ApplicationStartupFailure, ex.Message ) );
                Application.Current.Shutdown();
                m_singleInstanceMutex = null;
            }
        }

        /// <summary>
        /// Called when the application exists.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ApplicationExit( object sender, ExitEventArgs e )
        {
            if ( m_singleInstanceMutex != null )
            {
                m_singleInstanceMutex.ReleaseMutex();
            }
            if ( Restart )
            {
                String[] argArray = Environment.GetCommandLineArgs();
                String argString = "";
                for ( int argIndex = 1; argIndex < argArray.Length; argIndex++ )
                {
                    argString += '"' + argArray[argIndex] + "\" ";
                }

                foreach ( String extraArg in RestartExtraArgs )
                {
                    argString += '"' + extraArg + "\" ";

                }

                ProcessStartInfo startInfo =
                    Process.GetCurrentProcess().StartInfo;
                String executable = Assembly.GetEntryAssembly().Location;
                startInfo.Arguments = argString;
                startInfo.FileName = executable;
                if ( File.Exists( executable ) )
                {
                    Process.Start( startInfo );
                }
            }
        }

        /// <summary>
        /// Handles unhandled exceptions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        static void HandleTheUnhandled( object sender, UnhandledExceptionEventArgs args )
        {
            Exception e = args.ExceptionObject as Exception;
            MessageBox.Show( "Unhandled Exception: " + e.Message );
        }

        /// <summary>
        /// This uses the GUID lifted directly from the old launcher, used to make sure
        /// we only have one copy of the launcher (old or new) running
        /// </summary>
        private const string c_applicationGuidString = "696F8871-C91D-4CB1-825D-36BE18065575";

        /// <summary>
        /// Mutex used to make sure we only have one copy of the launcher running 
        /// at any one time.
        /// </summary>
        private Mutex m_singleInstanceMutex = null;
    }
}
