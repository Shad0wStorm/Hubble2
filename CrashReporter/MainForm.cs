using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using ClientSupport;
using LocalResources;

namespace CrashReporter
{
    public partial class MainForm : Form
    {
        private String m_crashDescription = null;

        //--------------------------------------------------------------------------
        //! @brief Process command line arguments and set up resource strings.
        //--------------------------------------------------------------------------
        public MainForm()
        {

            var cmdArgs = Environment.GetCommandLineArgs();

            bool autoSend = false;

            for (int i = 0; i < cmdArgs.Length; ++i)
            {
                if (i + 1 < cmdArgs.Length)
                {
                    if (cmdArgs[i] == "/ApplicationPath")
                    {
						ApplicationPath = cmdArgs[i + 1];
                        try
                        {
                            Application = System.IO.Path.GetFileName(cmdArgs[i + 1]);
                        }
                        catch (System.Exception)
                        {
                            Application = cmdArgs[i + 1];
                        }

                    }
                    else if (cmdArgs[i] == "/DumpReport")
                    {
                        DumpReport = cmdArgs[i + 1];
                    }
                    else if (cmdArgs[i] == "/MachineToken")
                    {
                        MachineToken = cmdArgs[i + 1];
                    }
                    else if (cmdArgs[i] == "/Version")
                    {
                        Version = cmdArgs[i + 1];
                    }
                    else if (cmdArgs[i] == "/AuthToken")
                    {
                        AuthToken = cmdArgs[i + 1];
                    }
                    else if (cmdArgs[i] == "/MachineId")
                    {
                        MachineId = cmdArgs[i + 1];
                    }
                    else if (cmdArgs[i] == "/Time")
                    {
                        Time = cmdArgs[i + 1];
                    }
                    else if (cmdArgs[i] == "/TimeCorrection")
                    {
                        if (!String.IsNullOrEmpty(Time))
                        {
                            double timeCheck;
                            if (double.TryParse(Time, out timeCheck))
                            {
                                double correction;
                                if (double.TryParse(cmdArgs[i + 1], out correction))
                                {
                                    double correctedTime = timeCheck + correction;
                                    Time = String.Format("{0}", correctedTime);
                                }
                            }
                        }
                    }
                    else if (cmdArgs[i] == "/ServerRoot")
                    {
                        ServerRoot = cmdArgs[i + 1];
                    }
					else if (cmdArgs[i] == "/BuildType")
					{
						BuildType = cmdArgs[i + 1];
					}
                }
                if (cmdArgs[i] == "/AutoSend")
                {
                    autoSend = true;
                }
                else if (cmdArgs[i] == "/SkipCompress")
                {
                    SkipCompress = true;
                }
                else if (cmdArgs[i] == "/DebugBreak")
                {
                    Debugger.Break();
                }
            }

#if DEBUG
            MessageBox.Show(string.Format("DumpReport : {0}\r\nMachineToken : {1}\r\nVersion : {2}\r\nAuthToken : {3}\r\nMachineId : {4}\r\nTime : {5}\r\n",
                    DumpReport, MachineToken, Version, AuthToken, MachineId, Time));
#endif

            if (string.IsNullOrEmpty(MachineToken))
                throw new ArgumentException("MachineToken can't be null");

            if (string.IsNullOrEmpty(Version))
                throw new ArgumentException("Version can't be null");

            if (string.IsNullOrEmpty(AuthToken))
                throw new ArgumentException("AuthToken can't be null");

            if (string.IsNullOrEmpty(MachineId))
                throw new ArgumentException("MachineId can't be null");

            if (string.IsNullOrEmpty(Time))
                throw new ArgumentException("Time can't be null");

            m_reporter = new ReportUploader(ServerRoot);
            m_reporter.UploadStarted += OnUploadStarted;
            m_reporter.UploadProgress += OnUploadProgress;
            m_reporter.UploadCompleted += OnUploadCompleted;
            m_reporter.ErrorRecieved += OnErrorRecieved;

            InitializeComponent();

            if (autoSend)
            {
                OnSend(this, null);
            }

            //Set up the labels with their text from the resource files.
            Text = LocalResources.Properties.Resources.CrashReporterTitle;
            m_problemLabel.Text = LocalResources.Properties.Resources.PreApplication + Application +
                LocalResources.Properties.Resources.PostApplication;
            m_apologyLabel.Text = LocalResources.Properties.Resources.Apology;
            m_requestLabel.Text = LocalResources.Properties.Resources.Request;
            int crashDumpLinkStart = LocalResources.Properties.Resources.CrashDumpPrefix.Length + 1;
            int crashDumpLinkLength = LocalResources.Properties.Resources.CrashDumpTitle.Length;
            m_crashReportLabel.Text = LocalResources.Properties.Resources.CrashDumpPrefix + " " +
                LocalResources.Properties.Resources.CrashDumpTitle + " " + LocalResources.Properties.Resources.CrashDumpSuffix;
            m_crashReportLabel.LinkArea = new LinkArea(crashDumpLinkStart,
                crashDumpLinkLength);
            int hardwareLinkStart = LocalResources.Properties.Resources.HardwarePrefix.Length + 1;
            int hardwareLinkLength = LocalResources.Properties.Resources.HardwareTitle.Length;
            m_hardwareSurveyLabel.Text = LocalResources.Properties.Resources.HardwarePrefix + " " +
                LocalResources.Properties.Resources.HardwareTitle + " " + LocalResources.Properties.Resources.HardwareSuffix + " " +
                LocalResources.Properties.Resources.Confidentiality;
            m_hardwareSurveyLabel.LinkArea = new LinkArea(hardwareLinkStart,
                hardwareLinkLength);
            m_supportLabel.Text = LocalResources.Properties.Resources.SupportRequestDescription;
            m_supportDescriptionLabel.Text = LocalResources.Properties.Resources.SupportDescriptionName;
            int supportLinkStart = LocalResources.Properties.Resources.TicketLabel.Length + 1;
            int supportLinkLength = LocalResources.Properties.Resources.SupportLink.Length;
            m_supportLink.Text = LocalResources.Properties.Resources.TicketLabel + " " +
                LocalResources.Properties.Resources.SupportLink;
            m_supportLink.LinkArea = new LinkArea(supportLinkStart, supportLinkLength);
            m_sendButton.Text = LocalResources.Properties.Resources.BTNT_Send;
            m_dontSendButton.Text = LocalResources.Properties.Resources.BTNT_DoNotSend;
        }

        #region Public Properties

        public string Application { get; private set; }
		public string ApplicationPath { get; private set; }
		public string DumpReport { get; private set; }

        public string MachineToken { get; private set; }
        public string Version { get; private set; }
        public string AuthToken { get; private set; }
        public string MachineId { get; private set; }
        public string Time { get; private set; }
		public string BuildType { get; private set; }
        public String ServerRoot { get; private set; }
        private bool SkipCompress { get; set; }

        #endregion
        //##########################################################################
        #region Private Methods

        //--------------------------------------------------------------------------
        //! @brief Open a link in a web browser.
        //--------------------------------------------------------------------------
        void OpenLink
        (
            string _url
        )
        {
            try
            {
                Process.Start(_url);
            }
            catch (System.Exception)
            {
            }
        }

        //--------------------------------------------------------------------------
        //! @brief Open a file.
        //--------------------------------------------------------------------------
        void OpenFile
        (
            String _path
        )
        {
            if (!string.IsNullOrEmpty(_path) && System.IO.File.Exists(_path))
            {
                try
                {
#if MONO
                    Process p = new Process();
                    p.StartInfo.FileName = _path;
                    p.Start();
#else
                    var args = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");
                    args += ",OpenAs_RunDLL " + _path;
                    Process.Start("rundll32.exe", args);
#endif
                }
                catch (System.Exception)
                {

                }
            }
        }

        //--------------------------------------------------------------------------
        //! @brief Upload the crash report to our remote server.
        //--------------------------------------------------------------------------
        void Upload(object _state)
        {
            try
            {
				String tempPath = System.IO.Path.GetTempPath();
				String zipFile = System.IO.Path.Combine(tempPath, "CrashReport.zip");

                if (!File.Exists(zipFile))
                {
                    SkipCompress = false;
                }
                if (!SkipCompress)
                {
					String report = "Frontier Crash Reporter " + Version + "\r\n\r\n";
                    BeginInvoke(new Action(() =>
                    {
                        m_progressBar.Visible = true;
                        m_statusLabel.Text = LocalResources.Properties.Resources.StatusCompressing;
                    }));

                    String hardwareSpecFile = FindFile("specs.xml");

					report += "Application: " + Application;
					if (ApplicationPath != Application)
					{
						report += " (" + ApplicationPath + ")";
					}
					report += "\r\n\r\n";
					report += "CrashDump: " + DumpReport +"\r\n\r\n";

                    FilePackage.FilePackage package = new FilePackage.FilePackage();
                    package.AddFile(DumpReport, "Crash.dmp");

					report += "Hardware Survey: ";
					if (hardwareSpecFile != null)
					{
						report += hardwareSpecFile + " ";
						if (File.Exists(hardwareSpecFile))
						{
							FileInfo hsfi = new FileInfo(hardwareSpecFile);
							report += hsfi.LastWriteTimeUtc;
							package.AddFile(hardwareSpecFile, System.IO.Path.GetFileName(hardwareSpecFile));
						}
						else
						{
							report += "missing.";
						}
					}
					else
					{
						report += "not in any searched location.";
					}
					report += "\r\n\r\n";

					WriteStringAsFile(tempPath, m_crashDescription, "UserReport.txt", package);

					WriteStringAsFile(tempPath, report, "CrashReporter.txt", package);

                    package.WriteFile(zipFile);
                }

                byte[] compressedData = File.ReadAllBytes(zipFile);

                m_reporter.Upload(compressedData, MachineToken, "crashDump", Version, AuthToken, MachineId, Time, BuildType);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(String.Format(LocalResources.Properties.Resources.UploadException,
                    ex.Message),
                    LocalResources.Properties.Resources.ErrorTitle);
            }
        }

		private void WriteStringAsFile(String tempPath, String content, String reportName, FilePackage.FilePackage package)
		{
			if (!String.IsNullOrEmpty(content))
			{
				String reportPath = Path.Combine(tempPath, reportName);
				using (StreamWriter writer = new StreamWriter(reportPath))
				{
					writer.Write(content);
				}
				package.AddFile(reportPath, reportName);
			}
		}

        //--------------------------------------------------------------------------
        //! @brief Try to find a file by looking in places we might have put it.
        //--------------------------------------------------------------------------
        String FindFile
        (
            String _name
        )
        {
            if (File.Exists(_name))
            {
                return _name;
            }

            String od = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            od = System.IO.Path.Combine(od, "Frontier_Developments");

            if (!String.IsNullOrEmpty(od))
            {
                String foundFile = System.IO.Path.Combine(od, _name);
                if (File.Exists(foundFile))
                {
                    return foundFile;
                }
            }

            return null;
        }

        //--------------------------------------------------------------------------
        //! @brief Start uploading the crash report.
        //--------------------------------------------------------------------------
        void OnSend
        (
            Object _sender,
            EventArgs _args
        )
        {
            m_crashDescription = m_supportText.Text;
            ThreadPool.QueueUserWorkItem(Upload);
            m_sendButton.Enabled = false;
            m_dontSendButton.Enabled = false;
        }

        //--------------------------------------------------------------------------
        //! @brief If the problem isn't being reported, close.
        //--------------------------------------------------------------------------
        void OnDontSend
        (
            Object _sender,
            EventArgs _args
        )
        {
            Close();
        }

        //--------------------------------------------------------------------------
        //! @brief Show the crash dump file.
        //--------------------------------------------------------------------------
        void OnCrashDumpLinkClicked
        (
            Object _sender,
            LinkLabelLinkClickedEventArgs _args
        )
        {
            OpenFile(DumpReport);
        }

        //--------------------------------------------------------------------------
        //! @brief Show the hardware survey file.
        //--------------------------------------------------------------------------
        void OnHardwareSurveyLinkClicked
        (
            Object _sender,
            LinkLabelLinkClickedEventArgs _args
        )
        {
            String specFile = FindFile("specs.xml");
            OpenFile(specFile);
        }

        //--------------------------------------------------------------------------
        //! @brief Open a browser with the link to create a support ticket.
        //--------------------------------------------------------------------------
        void OnSupportTicketLinkClicked
        (
            Object _sender,
            LinkLabelLinkClickedEventArgs _args
        )
        {
            OpenLink(LocalResources.Properties.Resources.SupportLink);
        }

        //--------------------------------------------------------------------------
        //! @brief Handle upload starting.
        //--------------------------------------------------------------------------
        void OnUploadStarted()
        {
            BeginInvoke(new Action(() =>
            {
                m_statusLabel.Text = LocalResources.Properties.Resources.Status_Connecting;
            }));
        }

        //--------------------------------------------------------------------------
        //! @brief Handle upload progress.
        //--------------------------------------------------------------------------
        void OnUploadProgress
        (
            int _percentage
        )
        {
            BeginInvoke(new Action(() =>
            {
                m_progressBar.Value = (int)_percentage;
                m_statusLabel.Text = string.Format(LocalResources.Properties.Resources.Status_UploadProgress,
                    _percentage);
            }));
        }

        //--------------------------------------------------------------------------
        //! @brief Handle upload completing.
        //--------------------------------------------------------------------------
        void OnUploadCompleted()
        {
            BeginInvoke(new Action(() =>
            {
                m_statusLabel.Text = LocalResources.Properties.Resources.Status_UploadComplete;
            }));
            Thread.Sleep(2000);
            BeginInvoke(new Action(() =>
            {
                Close();
            }));
        }

        //--------------------------------------------------------------------------
        //! @brief Handle upload completing.
        //--------------------------------------------------------------------------
        void OnErrorRecieved
        (
            string _message
        )
        {
            BeginInvoke(new Action(() =>
            {
                MessageBox.Show(this, _message);
            }));
        }

        #endregion
        //##########################################################################
        #region Private Data

        ReportUploader m_reporter;

        #endregion

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
    }
}
