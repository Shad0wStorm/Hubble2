using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DriveCheck
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int m_phase = 0;
        String m_content = "";
        public MainWindow()
        {
            InitializeComponent();

            Report("This program will examine drives reported by the system.");
            Report("The test will be executed in several stages to help identify");
            Report("the point at which drive contents are requested.");
            NL();
            Report("If any error dialogs appear at any stage of the test please");
            Report("make a note of the affected drive and error and continue the");
            Report("dialog to get back to this window. Then make a note of the");
            Report("error next to the most recent report for the drive in this window.");
            NL();
            Report("Press start to begin each test.");
            NL();
        }

        private void NL()
        {
            Report("");
        }

        public void Report(String message)
        {
            ReportBox.Text += message + "\r\n";
        }

        private void OnStart(object sender, RoutedEventArgs e)
        {
            try
            {
                if (m_phase > 0)
                {
                    if (m_content != ReportBox.Text)
                    {
                        Report("Window contents have changed, aborting further tests.");
                        m_phase = -1;
                    }
                }
                switch (m_phase)
                {
                    case 0:
                        {
                            ListDrives();
                            break;
                        }
                    case 1:
                        {
                            ReadyCheck();
                            break;
                        }
                    case 2:
                        {
                            SizeCheck();
                            break;
                        }
                    case 3:
                        {
                            FolderCheck();
                            break;
                        }
                    case 4:
                        {
                            PhysicalDiskCheck();
                            break;
                        }
                    case 5:
                        {
                            PhysicalAssociationCheck();
                            break;
                        }
                    default:
                        {
                            Report("All tests completed.");
                            m_phase = -1;
                            break;
                        }
                }
            }
            catch (System.Exception ex)
            {
                Report("An exception occurred carrying out the check for phase " + m_phase.ToString());
                Report("The exception message is " + ex.Message);
                m_phase = -1;
            }
            if (m_phase < 0)
            {
                Report("Please copy the text in this window Ctrl-A Ctrl-C and attach it to the ticket.");
            }
            else
            {
                ++m_phase;
            }
            m_content = ReportBox.Text;
            ReportScroll.ScrollToBottom();
        }

        private void ListDrives()
        {
            Report("Checking for drives as reported by system");
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            foreach (DriveInfo d in allDrives)
            {
                Report("Found Drive : " + d.Name );
            }
            NL();
        }

        private void ReadyCheck()
        {
            Report("Checking for drive readiness");
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            foreach (DriveInfo d in allDrives)
            {
                try
                {
                    if (d.IsReady)
                    {
                        Report(d.Name + " READY");
                    }
                    else
                    {
                        Report(d.Name + " NOT READY");
                    }
                }
                catch (System.Exception ex)
                {
                    Report(d.Name + " Exception testing for readiness " + ex.Message);                	
                }
            }
            NL();
        }

        private void SizeCheck()
        {
            Report("Checking for drive capacities");
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            foreach (DriveInfo d in allDrives)
            {
                try
                {
                    if (d.IsReady)
                    {
                        Report(d.Name + " Reported Size : " + d.TotalSize.ToString());
                    }
                    else
                    {
                        Report(d.Name + " NOT READY");
                    }
                }
                catch (System.Exception ex)
                {
                    Report(d.Name + " Exception testing for readiness " + ex.Message);
                }
            }
            NL();
        }

        private void FolderCheck()
        {
            Report("Checking for folder on drive");
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            foreach (DriveInfo d in allDrives)
            {
                try
                {
                    if (d.IsReady)
                    {
                        String dir = System.IO.Path.Combine(d.Name, "Data");
                        if (Directory.Exists(dir))
                        {
                            Report("Directory " + dir + " exists.");
                        }
                        else
                        {
                            if (File.Exists(dir))
                            {
                                Report("File " + dir + " exists.");
                            }
                            else
                            {
                                Report("Directory " + dir + " does not exist.");
                            }
                        }
                    }
                    else
                    {
                        Report(d.Name + " NOT READY");
                    }
                }
                catch (System.Exception ex)
                {
                    Report(d.Name + " Exception testing for readiness " + ex.Message);
                }
            }
            NL();
        }

        private void PhysicalDiskCheck()
        {
            Report("Physical Disks");
            ManagementObjectSearcher pd = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            DumpManagementObjects(pd);

            Report("Disk Partitions");
            ManagementObjectSearcher partd = new ManagementObjectSearcher("SELECT * FROM Win32_DiskPartition");
            DumpManagementObjects(partd);

            Report("Logical Disks");
            ManagementObjectSearcher ld = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk");
            DumpManagementObjects(ld);
        }

        private void PhysicalAssociationCheck()
        {
            ManagementObjectSearcher pd = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            foreach (ManagementObject wmiPD in pd.Get())
            {
                wmiPD.Get();
                String searchPath = "ASSOCIATORS OF {Win32_DiskDrive.DeviceID='" +
                    wmiPD["DeviceID"] + "'} WHERE AssocClass = Win32_DiskDriveToDiskPartition";
                ManagementObjectSearcher dpa = new ManagementObjectSearcher(searchPath);

                foreach (ManagementObject ldmPD in dpa.Get())
                {
                    String ldmSearch = "ASSOCIATORS OF {Win32_DiskPartition.DeviceID='" +
                        ldmPD["DeviceID"] + "'} WHERE AssocClass = Win32_LogicalDiskToPartition";
                    ManagementObjectSearcher ldma = new ManagementObjectSearcher(ldmSearch);

                    foreach (ManagementObject lp in ldma.Get())
                    {
                        Report(wmiPD["DeviceID"] + " -> " + ldmPD["DeviceID"] + " -> " + lp["DeviceID"]);
                    }
                }
                NL();
            }
        }

        private void DumpManagementObjects(ManagementObjectSearcher pd)
        {
            foreach (ManagementObject wmiPD in pd.Get())
            {
                wmiPD.Get();
                foreach (PropertyData p in wmiPD.Properties)
                {
                    String value = null;
                    if (p.Value != null)
                    {
                        value = p.Value.ToString();
                    }
                    else
                    {
                        value = "<null>";
                    }
                    if (String.IsNullOrEmpty(value))
                    {
                        value = "Not A String";
                    }
                    Report(p.Name + " : " + value);
                }
                NL();
            }
        }
    }
}
