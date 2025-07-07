using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using ClientSupport;

namespace CBViewModel
{
    public class LogOutManager
    {
        private enum PendingLogOut { None, User, Machine }
        private PendingLogOut m_pendingLogout = PendingLogOut.None;

        private String m_userTitle;
        public String UserTitle
        {
            get
            {
                return AddSuffix(m_userTitle);
            }
        }
        private String m_machineTitle;
        public String MachineTitle
        {
            get
            {
                return AddSuffix(m_machineTitle);
            }
        }

        public bool Enabled
        {
            get
            {
                return m_pendingLogout == PendingLogOut.None;
            }
        }

        private UserInterface m_host;
        private FORCManager m_manager;

        public LogOutManager(String userTitle, String machineTitle)
        {
            m_userTitle = userTitle;
            m_machineTitle = machineTitle;
        }

        private String AddSuffix(String title)
        {
            if (m_pendingLogout == PendingLogOut.None)
            {
                return title;
            }
            return title + LocalResources.Properties.Resources.MenuLogOutPendingSuffix;
        }

        public void LogOutUser(FORCManager manager, ProjectProgressMonitor monitor, UserInterface host)
        {
            if (monitor != null)
            {
                monitor.RequestCancellation = true;
                m_host = host;
                m_manager = manager;
                m_pendingLogout = PendingLogOut.User;
                monitor.PropertyChanged += MonitorUpdated;
            }
            else
            {
                PerformLogout(manager, host);
            }
        }

        private void PerformLogout(FORCManager manager, UserInterface host)
        {
            manager.LogoutUser();
            host.MarkForUpdate();
        }

        private void MonitorUpdated(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "HasCompleted")
            {
                if ((m_manager != null) && (m_host != null))
                {
                    switch (m_pendingLogout)
                    {
                        case PendingLogOut.User:
                            {
                                PerformLogout(m_manager, m_host);
                                m_pendingLogout = PendingLogOut.None;
                                m_host = null;
                                m_manager = null;
                                break;
                            }
                        case PendingLogOut.Machine:
                            {
                                PerformLogOutMachine(m_manager, m_host);
                                m_pendingLogout = PendingLogOut.None;
                                m_host = null;
                                m_manager = null;
                                break;
                            }
                    }
                }
            }
        }

        public void LogOutMachine(FORCManager manager, ProjectProgressMonitor monitor, UserInterface host)
        {
            if (monitor != null)
            {
                monitor.RequestCancellation = true;
                m_host = host;
                m_manager = manager;
                m_pendingLogout = PendingLogOut.Machine;
                monitor.PropertyChanged += MonitorUpdated;
            }
            else
            {
                PerformLogOutMachine(manager, host);
            }
        }

        public void PerformLogOutMachine(FORCManager manager, UserInterface host)
        {
            manager.LogoutMachine();
            host.MarkForUpdate();
        }
    }
}
