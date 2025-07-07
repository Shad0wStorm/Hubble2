using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using ClientSupport;

namespace CBViewModel
{
    public class LoginView
    {
        public enum DisplayMode { UserPass, Verification };

        private FORCManager m_manager = null;
        private ILoginDisplay m_display = null;
        private bool m_rememberUsername = true;
        public bool RememberUsername
        {
            get { return m_rememberUsername; }
            set
            {
                if (value != m_rememberUsername)
                {
                    m_rememberUsername = value;
                    StoreSettings();
                }
            }
        }
        private bool m_rememberPassword = false;
        public bool RememberPassword
        {
            get { return m_rememberPassword; }
            set
            {
                if (m_rememberPassword != value)
                {
                    m_rememberPassword = value;
                    StoreSettings();
                }
            }
        }

        public String EmailAddress
        {
            get
            {
                if (m_manager != null)
                {
                    return m_manager.UserDetails.EmailAddress;
                }
                return null;
            }
        }
        public String Password
        {
            get
            {
                if (m_manager != null)
                {
                    return m_manager.UserDetails.Password;
                }
                return null;
            }
        }

        private String m_registrationPrefix;
        public String RegistrationPrefix
        {
            get
            {
                return m_registrationPrefix;
            }
        }

        private String m_registrationSuffix;
        public String RegistrationSuffix
        {
            get
            {
                return m_registrationSuffix;
            }
        }

        private String m_registrationLink;
        public String RegistrationLink
        {
            get
            {
                return m_registrationLink;
            }
        }

        public LoginView(FORCManager manager, ILoginDisplay display)
        {
            m_manager = manager;
            m_display = display;

            m_rememberUsername = Properties.Settings.Default.Option_RememberMe;
            m_rememberPassword = Properties.Settings.Default.Option_RememberPassword;

            SetupRegistrationText();

            m_manager.ResetLogin(m_rememberUsername, m_rememberUsername);

            LogEntry entry = new LogEntry("ShowLogin");
            m_manager.Log(entry);
        }

        private void SetupRegistrationText()
        {
            String raw = null;
            if (m_manager.IsSteam)
            {
                raw = LocalResources.Properties.Resources.LW_RegistrationSteam;
            }
            else if (m_manager.IsEpic)
            {
                raw = LocalResources.Properties.Resources.LW_RegistrationEpic;
            }
            else
            {
				if (m_manager.OculusEnabled)
				{
					raw = LocalResources.Properties.Resources.LW_NoRegistration;
				}
				else
				{
					raw = LocalResources.Properties.Resources.LW_RegistrationRequired;
				}
            }
            Regex here = new Regex("(.*)\\{HERE:([^}]*)\\}(.*)");
            Match m = here.Match(raw);
            if (m.Success)
            {
                m_registrationPrefix = m.Groups[1].Value;
                m_registrationSuffix = m.Groups[3].Value;
                m_registrationLink = m.Groups[2].Value;
            }
            else
            {
                m_registrationPrefix = raw;
                m_registrationSuffix = "";
                m_registrationLink = "";
            }
        }

        /// <summary>
        /// Update the current dialog state.
        /// Note that since the password box does not support binding we do
        /// everything through the update/button event handlers since there
        /// is little point in binding some things if we still need to do some
        /// stuff manually.
        /// </summary>
        public void Update()
        {
            m_display.SetDisplayMode(m_manager.RequiresTwoFactor ? DisplayMode.Verification : DisplayMode.UserPass);

            m_display.SetStatus(m_manager.Status);

            LogEntry entry = new LogEntry("LoginUpdated");
            entry.AddValue("Status", m_manager.Status);
            m_manager.Log(entry);
        }

        public bool CancelLogin()
        {
            bool willCancel = true;
            if (m_manager.RequiresTwoFactor)
            {
                willCancel = m_display.CheckCancel();
            }
            if (willCancel)
            {
                // User specifically cancelled the in progress operation.
                LogEntry entry = new LogEntry("LoginCancelled");
                m_manager.Log(entry);
            }
            else
            {
                LogEntry entry = new LogEntry("LoginContinuing");
                m_manager.Log(entry);
            }
            return willCancel;
        }

        public bool SubmitLogin(String username, String password, String verify,
            bool remember, bool rememberp)
        {
            if (!m_manager.Authorised )
            {
                // User is submitting login information.
                m_manager.UserDetails.EmailAddress = username;
                m_manager.UserDetails.Password = password;
                m_manager.UserDetails.TwoFactor = verify;
                m_manager.Authenticate(false);
                if (remember)
                {
                    m_manager.SaveUserDetails(rememberp);
                }
                else
                {
                    m_manager.ClearUserDetails();
                }
                LogEntry entry = new LogEntry("LoginSubmitted");
                entry.AddValue("Authorised", m_manager.Authorised ? "True" : "False");
                m_manager.Log(entry);
                if (m_manager.Authorised)
                {
                    return true;
                }
                else
                {
                    Update();
                }
            }
            return false;
        }

        public void UpdateDetails(String username, String password)
        {
            if (m_manager != null)
            {
                m_manager.UserDetails.EmailAddress = username;
                m_manager.UserDetails.Password = password;
                Update();
            }
        }

        private void StoreSettings()
        {
            Properties.Settings.Default.Option_RememberMe = m_rememberUsername;
            Properties.Settings.Default.Option_RememberPassword = m_rememberPassword;
            Properties.Settings.Default.Save();
        }

        public void PasswordForgotten()
        {
            try
            {
                Process.Start(LocalResources.Properties.Resources.LW_ForgotPasswordLink);
            }
            catch (System.Exception) { }
        }

        public bool OpenRegisterLink()
        {
            try
            {
                bool close = ((!String.IsNullOrEmpty(m_manager.UserDetails.SteamLinkLink)) ||
                    (!String.IsNullOrEmpty(m_manager.UserDetails.SteamRegistrationLink)));
                m_manager.StartRegistration(LocalResources.Properties.Resources.RegisterLink);
                return close;
            }
            catch (System.Exception) { }
            return false;
        }
    }
}
