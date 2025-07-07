using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using ClientSupport;
using LocalResources;

namespace HardwareReporter
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            SetUpTargetDirectory();
            ForceShowWindow = false;
			HideWindow = false;
            var cmdArgs = Environment.GetCommandLineArgs();
            for (int i = 0; i < cmdArgs.Length; ++i)
            {
                if (i + 1 < cmdArgs.Length)
                {
                    if (cmdArgs[i] == "/MachineToken")
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
                    else if (cmdArgs[i] == "/Target")
                    {
                        OutputDirectory = cmdArgs[i + 1];
                    }
                }
				if (cmdArgs[i] == "/Silent")
				{
					HideWindow = true;
				}
            }

            m_reporter = new ReportUploader();
            m_reporter.UploadStarted += OnUploadStarted;
            m_reporter.UploadProgress += OnUploadProgress;
            m_reporter.UploadCompleted += OnUploadCompleted;
            m_reporter.ErrorRecieved += OnErrorRecieved;

			this.ShowInTaskbar = false;
			InitializeComponent();
        }

        public String OutputDirectory;

        public string OutputPath
        {
            get
            {
                var path = System.IO.Path.Combine(OutputDirectory, "specs.xml");
                if (System.IO.File.Exists(path))
                {
                    var fi = new FileInfo(path);
                    fi.IsReadOnly = false;
                    fi.Refresh();
                }
                return path;
            }
        }

        public bool ForceShowWindow;
		public bool HideWindow;

        public string MachineToken { get; private set; }
        public string Version { get; private set; }
        public string AuthToken { get; private set; }
        public string MachineId { get; private set; }
        public string Time
        {
            get
            {
                if (m_haveTime)
                {
                    TimeSpan ts = DateTime.UtcNow.Subtract(m_startTime);
                    int serverTimeNow = (int)(m_serverTimeSeconds + ts.TotalSeconds);
                    return serverTimeNow.ToString();
                }
                return null;
            }

            private set
            {
                if (double.TryParse(value, out m_serverTimeSeconds))
                {
                    m_startTime = DateTime.UtcNow;
                    m_haveTime = true;
                }
                else
                {
                    m_haveTime = false;
                }
            }
        }
        public byte[] Data { get; private set; }
        public bool CanUpload
        {
            get
            {
                return !string.IsNullOrEmpty(MachineToken) &&
                       !string.IsNullOrEmpty(Version) &&
                       !string.IsNullOrEmpty(AuthToken) &&
                       !string.IsNullOrEmpty(MachineId) &&
                       !string.IsNullOrEmpty(Time);
            }
        }

        public int MaxAttempts { get { return 100; } }
        public int SleepPeriod { get { return 1000; } }

        //--------------------------------------------------------------------------
        //! @brief Set the directory that the hardware information will be saved to.
        //--------------------------------------------------------------------------
        private void SetUpTargetDirectory()
        {
            String od = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            od = System.IO.Path.Combine(od, "Frontier_Developments");
            if (!System.IO.Directory.Exists(od))
            {
                System.IO.Directory.CreateDirectory(od);
            }
            OutputDirectory = od;
        }

//Hardware capture for OS X.
#if MONO

        //--------------------------------------------------------------------------
        //! @brief Launch the tool that will get hardware info. OS X version.
        //--------------------------------------------------------------------------
        bool LaunchProcess()
        {
            try
            {
                //Delete any existing data.
                if (System.IO.File.Exists(OutputPath))
                {
                    System.IO.File.Delete(OutputPath);
                }
                m_dxDiagProcess = new Process();
                m_dxDiagProcess.StartInfo.FileName = @"system_profiler";
                m_dxDiagProcess.StartInfo.CreateNoWindow = true;
                m_dxDiagProcess.StartInfo.Arguments = "-xml -detailLevel basic -timeout 15";
                m_dxDiagProcess.StartInfo.RedirectStandardOutput = true;
                m_dxDiagProcess.StartInfo.UseShellExecute = false;
                m_dxDiagProcess.Start();
                String hardwareInfo = m_dxDiagProcess.StandardOutput.ReadToEnd();
                m_dxDiagProcess.WaitForExit();
                //Other tools may expect this file to exist, so we're going to save it
                //out manually to make sure that it does.
                StreamWriter outputFile = File.CreateText(OutputPath);
                outputFile.WriteLine(hardwareInfo);
                outputFile.Close();
                //We waited for the process to complete, we should only need one attempt.
                QueueProcessResults(1);
                return true;
            }
            catch
            {
                return false;
            }
        }

        //--------------------------------------------------------------------------
        //! @brief Open the hardware report.
        //--------------------------------------------------------------------------
        void LinkClicked
        (
            Object i_sender,
            LinkLabelLinkClickedEventArgs i_args
        )
        {
            if (!string.IsNullOrEmpty(OutputPath) && System.IO.File.Exists(OutputPath))
            {
                Process p = new Process();
                p.StartInfo.FileName = OutputPath;
                p.Start();
            }
        }

#else

        //--------------------------------------------------------------------------
        //! @brief Launch the tool that will get hardware info. Windows version.
        //--------------------------------------------------------------------------
        bool LaunchProcess()
        {
            // delete any existing data
            if (System.IO.File.Exists(OutputPath))
            {
                System.IO.File.Delete(OutputPath);
            }

            m_dxDiagProcess = new Process();
            m_dxDiagProcess.StartInfo.FileName = @"dxdiag.exe";
            m_dxDiagProcess.StartInfo.Arguments = string.Format(@"/whql:off /x {0}", OutputPath);
            m_dxDiagProcess.EnableRaisingEvents = true;
            m_dxDiagProcess.Exited += OnDxDiagProcessExited;

            m_progressBarLabel.Text = LocalResources.Properties.Resources.Status_Aquiring;
            m_progressBar.Style = ProgressBarStyle.Marquee;

            try
            {
                m_dxDiagProcess.Start();
            }
            catch
            {
                try
                {
                    m_dxDiagProcess.StartInfo.FileName = @"C:\Windows\System32\dxdiag.exe";
                    m_dxDiagProcess.Start();
                }
                catch
                {
                    //Give up.
                    return false;
                }
            }

            return true;
        }

        //--------------------------------------------------------------------------
        //! @brief Open the hardware report.
        //--------------------------------------------------------------------------
        void LinkClicked
        (
            Object i_sender,
            LinkLabelLinkClickedEventArgs i_args
        )
        {
            if (!string.IsNullOrEmpty(OutputPath) && System.IO.File.Exists(OutputPath))
            {
                try
                {
                    var args = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");
                    args += ",OpenAs_RunDLL " + OutputPath;
                    Process.Start("rundll32.exe", args);
                }
                catch (System.Exception)
                {

                }
            }
        }

        //--------------------------------------------------------------------------
        //! @brief Try to read the results of the process.
        //--------------------------------------------------------------------------
        void OnDxDiagProcessExited
        (
            Object i_sender,
            EventArgs i_e
        )
        {
            // dxdiag seems to spawn a child process and kills the one we launched
            // immediately, so unfortunately we get here immediately too. So wait 
            // a bit for the first attempt
            Thread.Sleep(3000);

            // now queue an attempt to read the result
            QueueProcessResults(MaxAttempts);
        }

#endif

        //--------------------------------------------------------------------------
        //! @brief Repeatedly try to read the DXDiag output.
        //--------------------------------------------------------------------------
        void QueueProcessResults
        (
            int i_attempts
        )
        {
            if (i_attempts < 1)
            {
                BeginInvoke(new Action(() =>
                {
                    Close();
                }));
            }
            else
            {
                ThreadPool.QueueUserWorkItem(OnProcessResults, i_attempts - 1);
            }
        }

        //--------------------------------------------------------------------------
        //! @brief Try to read the DXDiag output.
        //--------------------------------------------------------------------------
        void OnProcessResults
        (
            Object i_state
        )
        {
            int attempts = (int)i_state;

            if (System.IO.File.Exists(OutputPath))
            {
                try
                {
                    // this may throw if dxdiag hasn't released its lock on the file
                    var text = System.IO.File.ReadAllText(OutputPath);

                    // store something in case the formatting code fails
                    Data = System.Text.Encoding.ASCII.GetBytes(text);

                    try
                    {
                        // this could all throw if the document is invalid
                        var settings = new XmlWriterSettings()
                        {
                            Encoding = Encoding.UTF8,
                            Indent = true,
                            IndentChars = "\t"
                        };

                        var sb = new StringBuilder();
                        using (var wr = XmlTextWriter.Create(sb, settings))
                        {
                            // there seems to be some random newlines, so remove them
                            text = text.Replace("\r\n", "");

                            // parse the doc
                            var doc = XDocument.Parse(text);

                            // save formatted
                            doc.Save(wr);
                        }

                        // replace the data with formatted data
                        Data = System.Text.Encoding.ASCII.GetBytes(sb.ToString());

                        // write back out for other systems to use
                        System.IO.File.WriteAllText(OutputPath, sb.ToString());
                    }
                    catch (System.Exception) { }

                    BeginInvoke(new Action(() =>
                    {
                        if (CanUpload)
                        {
                            m_progressBarLabel.Visible = false;
                            m_progressBar.Visible = false;
                            m_cancelButton.Visible = false;
                            m_sendButton.Visible = true;
                            m_dontSendButton.Visible = true;
                            int linkStart = LocalResources.Properties.Resources.CompletedPrefix.Length + 1;
                            int linkLength = LocalResources.Properties.Resources.CompletedTitle.Length;
                            m_informationLabel1.Text = LocalResources.Properties.Resources.CompletedPrefix + " " +
                                LocalResources.Properties.Resources.CompletedTitle + " " + LocalResources.Properties.Resources.CompletedSuffix;
                            m_informationLabel1.LinkArea = new LinkArea(linkStart, linkLength);
                            m_informationLabel1.Font = new System.Drawing.Font(
                                m_informationLabel1.Font, FontStyle.Regular);
                            m_informationLabel2.Text = LocalResources.Properties.Resources.CompletedConfidential;
                            m_informationLabel2.Font = new System.Drawing.Font(
                                m_informationLabel2.Font, FontStyle.Bold);
                        }
                        else
                        {
                            Console.WriteLine("Exited without upload.");
                            Close();
                        }
                    }));
                }
                catch
                {
                    // dxdiag probably still has the lock... try again in a bit
                    Thread.Sleep(SleepPeriod);
                    QueueProcessResults(attempts);
                }
            }
            else
            {
                // result doesn't exist yet
                Thread.Sleep(SleepPeriod);
                QueueProcessResults(attempts);
            }
        }

        //--------------------------------------------------------------------------
        //! @brief Fill out the labels with the language-appropriate resource
        //! strings.
        //--------------------------------------------------------------------------
        private void MainWindowLoad
        (
            Object i_sender,
            EventArgs i_e
        )
        {
			if (HideWindow)
			{
				this.ShowInTaskbar = true;
				this.WindowState = FormWindowState.Minimized;
				this.Hide();
			}
            m_informationLabel1.Text = LocalResources.Properties.Resources.RetrieveHardwareStats;
            m_informationLabel2.Text = LocalResources.Properties.Resources.PleaseWait;
            m_cancelButton.Text = LocalResources.Properties.Resources.BTNT_Cancel;
            m_sendButton.Text = LocalResources.Properties.Resources.BTNT_Send;
            m_dontSendButton.Text = LocalResources.Properties.Resources.BTNT_DoNotSend;
            m_progressBarLabel.Text = LocalResources.Properties.Resources.Status_Aquiring;

            ThreadPool.QueueUserWorkItem((o) =>
            {
                if (!LaunchProcess())
                {
                    BeginInvoke(new Action(() =>
                        this.Close()));
                }
            });
        }

        //--------------------------------------------------------------------------
        //! @brief Close the application.
        //--------------------------------------------------------------------------
        private void CancelClick
        (
            Object i_sender,
            EventArgs i_e
        )
        {
            this.Close();
        }

        //--------------------------------------------------------------------------
        //! @brief Close the application.
        //--------------------------------------------------------------------------
        private void DontSendClick
        (
            Object i_sender,
            EventArgs i_e
        )
        {
            this.Close();
        }

        //--------------------------------------------------------------------------
        //! @brief Send the hardware information.
        //--------------------------------------------------------------------------
        private void SendClick
        (
            Object i_sender,
            EventArgs i_e
        )
        {
            if (Data != null)
            {
                m_sendButton.Enabled = false;
                m_dontSendButton.Enabled = false;
                m_progressBar.Visible = true;
                int linkStart = LocalResources.Properties.Resources.UploadingPrefix.Length;
                int linkLength = LocalResources.Properties.Resources.CompletedTitle.Length;
                m_informationLabel1.Text = LocalResources.Properties.Resources.UploadingPrefix +
                    LocalResources.Properties.Resources.CompletedTitle + LocalResources.Properties.Resources.UploadingSuffix;
                m_informationLabel1.LinkArea = new LinkArea(linkStart, linkLength);
                m_informationLabel2.Visible = false;

                try
                {
                    m_reporter.Upload(Data, MachineToken, "hwDiag", Version, AuthToken, MachineId, Time, null);
                }
                catch (System.Exception)
                {
                    // close the dialog
                    Close();
                }
            }
        }

        //--------------------------------------------------------------------------
        //! @brief Handle the report upload starting.
        //--------------------------------------------------------------------------
        void OnUploadStarted()
        {
            BeginInvoke(new Action(() =>
            {
                m_progressBar.Style = ProgressBarStyle.Continuous;
                m_progressBar.Minimum = 0;
                m_progressBar.Maximum = 100;
                m_progressBar.Value = 0;
                m_progressBarLabel.Visible = true;
                m_progressBarLabel.Text = LocalResources.Properties.Resources.Status_Connecting;
            }));
        }

        //--------------------------------------------------------------------------
        //! @brief Handle progress being made on the report upload.
        //--------------------------------------------------------------------------
        void OnUploadProgress
        (
            int i_percentage
        )
        {
            BeginInvoke(new Action(() =>
            {
                m_progressBar.Value = (int)i_percentage;
                m_progressBarLabel.Text = string.Format(
                    LocalResources.Properties.Resources.Status_UploadProgress, i_percentage);
            }));
        }

        //--------------------------------------------------------------------------
        //! @brief Handle the report upload completing.
        //--------------------------------------------------------------------------
        void OnUploadCompleted()
        {
            BeginInvoke(new Action(() =>
            {
                m_progressBarLabel.Text = LocalResources.Properties.Resources.Status_UploadComplete;
            }));
            if (m_errorReceived != null)
            {
                MessageBox.Show(m_errorReceived);
            }
            Thread.Sleep(2000);
            BeginInvoke(new Action(() =>
            {
                Close();
            }));
        }

        //--------------------------------------------------------------------------
        //! @brief Handle an error being received.
        //--------------------------------------------------------------------------
        void OnErrorRecieved
        (
            string i_message
        )
        {
            BeginInvoke(new Action(() =>
            {
                if (m_errorReceived == null)
                {
                    m_errorReceived = i_message;
                }
                else
                {
                    m_errorReceived = m_errorReceived + LocalResources.Properties.Resources.LineBreak + i_message;
                }
            }));
        }

        private bool m_haveTime;
        private double m_serverTimeSeconds;
        private DateTime m_startTime;
        Process m_dxDiagProcess;
        ReportUploader m_reporter;
        String m_errorReceived = null;
    }
}
