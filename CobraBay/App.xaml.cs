using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
//using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows;

using LocalResources;

namespace CobraBay
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        Mutex m_singleInstanceMutex;
        public bool Restart = false;
        public List<String> RestartExtraArgs = new List<string>();

        private void ApplicationStart(object sender, StartupEventArgs e)
        {
            try
            {
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.UnhandledException += new UnhandledExceptionEventHandler(HandleTheUnhandled);

				// Force an upgrade of the user settings so we pick up any
				// configured language before we try to use the language.
				CBViewModel.CobraBayView.UpgradeUserSettings();

                String activeLanguage = CBViewModel.Properties.Settings.Default.LanguageOverride;
                if (!String.IsNullOrEmpty(activeLanguage))
                {
                    try
                    {
                        Thread.CurrentThread.CurrentUICulture = new CultureInfo(activeLanguage);
                    }
                    catch (System.ArgumentException)
                    {
                        // Whatever the active language was it was not recognised
                        // as a valid language so revert to the system default.
                        CBViewModel.Properties.Settings.Default.LanguageOverride = "";
                        CBViewModel.Properties.Settings.Default.Save();
                    }
                    catch (Exception)
                    {
                        // Some other exception, e.g. the neutral culture exception
                        // seen in the wild.
                        CBViewModel.Properties.Settings.Default.LanguageOverride = "";
                        CBViewModel.Properties.Settings.Default.Save();
                    }
                }

                // This uses the GUID lifted directly from the project file, we
                // probably want a nicer way of finding it.
                String applicationGuidString = "696F8871-C91D-4CB1-825D-36BE18065575";
                String mutexId = string.Format("Global\\{{{0}}}", applicationGuidString);

                m_singleInstanceMutex = new Mutex(false, mutexId);
                MutexAccessRule aer = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
                MutexSecurity securitySettings = new MutexSecurity();
                securitySettings.AddAccessRule(aer);
                m_singleInstanceMutex.SetAccessControl(securitySettings);

                bool hasHandle = false;
                try
                {
                    try
                    {
                        hasHandle = m_singleInstanceMutex.WaitOne(0, false);
                    }
                    catch (AbandonedMutexException)
                    {
                        // Mutex still exists, but is not attached, so we take it
                        // over.
                        hasHandle = true;
                    }
                }
                catch (System.Exception)
                {

                }

                if (!hasHandle)
                {
                    MessageBox.Show(LocalResources.Properties.Resources.ApplicationAlreadyRunning);
                    Application.Current.Shutdown();
                    m_singleInstanceMutex = null;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(String.Format(LocalResources.Properties.Resources.ApplicationStartupFailure,ex.Message));
                Application.Current.Shutdown();
                m_singleInstanceMutex = null;
            }
        }

        private void ApplicationExit(object sender, ExitEventArgs e)
        {
            if (m_singleInstanceMutex != null)
            {
                m_singleInstanceMutex.ReleaseMutex();
            }
            if (Restart)
            {
                String[] argArray = Environment.GetCommandLineArgs();
                String argString = "";
                for (int argIndex = 1; argIndex < argArray.Length; argIndex++)
                {
                    argString += '"' + argArray[argIndex] + "\" ";
                }

                foreach (String extraArg in RestartExtraArgs)
                {
                    argString += '"' +  extraArg + "\" ";

                }

                ProcessStartInfo startInfo =
                    Process.GetCurrentProcess().StartInfo;
                String executable = Assembly.GetEntryAssembly().Location;
                startInfo.Arguments = argString;
                startInfo.FileName = executable;
                if (File.Exists(executable))
                {
                    Process.Start(startInfo);
                }
            }
        }

        static void HandleTheUnhandled(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = args.ExceptionObject as Exception;
            MessageBox.Show("Unhandled Exception: " + e.Message);
        }
    }
}
