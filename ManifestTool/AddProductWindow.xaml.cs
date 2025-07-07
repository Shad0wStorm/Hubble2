using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

using ClientSupport;

namespace ManifestTool
{
    /// <summary>
    /// Interaction logic for AddProductWindow.xaml
    /// </summary>
    public partial class AddProductWindow : Window, INotifyPropertyChanged
    {
        private bool m_importPermitted = false;
        public bool ImportPermitted
        {
            get
            {
                return m_importPermitted;
            }
            set
            {
                if (value != m_importPermitted)
                {
                    m_importPermitted = value;
                    RaisePropertyChanged("ImportPermitted");
                }
            }
        }

        private String m_versionInfoFile;
        public String VersionInfoFilePath
        {
            get
            {
                return m_versionInfoFile;
            }
            set
            {
                if (m_versionInfoFile != value)
                {
                    m_versionInfoFile = value;
                    RaisePropertyChanged("VersionInfoFilePath");
                }
            }
        }

        private String m_projectDirectory;
        public String ProjectDirectoryPath
        {
            get
            {
                return m_projectDirectory;
            }
            set
            {
                if (m_projectDirectory != value)
                {
                    m_projectDirectory = value;
                    RaisePropertyChanged("ProjectDirectoryPath");
                }
            }
        }

        private String m_versionString;
        public String ProjectVersionString
        {
            get
            {
                return m_versionString;
            }
            set
            {
                if (m_versionString != value)
                {
                    m_versionString = value;
                    RaisePropertyChanged("ProjectVersionString");
                }
            }
        }

        private String m_executableString;
        public String ProjectExecutableString
        {
            get
            {
                return m_executableString;
            }
            set
            {
                if (m_executableString != value)
                {
                    m_executableString = value;
                    RaisePropertyChanged("ProjectExecutableString");
                }
            }
        }

        private String m_titleString;
        public String ProjectTitleString
        {
            get { return m_titleString; }
            set
            {
                if (m_titleString != value)
                {
                    m_titleString = value;
                    RaisePropertyChanged("ProjectTitleString");
                }
            }
        }

        private bool m_useSlash = false;
        public bool UseSlash
        {
            get
            {
                return m_useSlash;
            }
            set
            {
                if (m_useSlash != value)
                {
                    m_useSlash = value;
                    RaisePropertyChanged("UseSlash");
                }
            }
        }

		private bool m_includePlatform = false;
		public bool IncludePlatform
		{
			get
			{
				return m_includePlatform;
			}
			set
			{
				if (m_includePlatform != value)
				{
					m_includePlatform = value;
					RaisePropertyChanged("IncludePlatform");
				}
			}
		}


        private FileStore m_fileStore;

        public AddProductWindow(FileStore fileStore)
        {
            InitializeComponent();
            m_fileStore = fileStore;
            DataContext = this;
        }

        private void OnImport(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(String property)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }

        private void SelectVersionInfo(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            ofd.FileName = "VersionInfo.txt";
            if (!String.IsNullOrEmpty(Properties.Settings.Default.ImportDirectory))
            {
                ofd.FileName = System.IO.Path.Combine(Properties.Settings.Default.ImportDirectory,ofd.FileName);
            }

            bool? result = ofd.ShowDialog();

            if (result == true)
            {
                String directoryPath = System.IO.Path.GetDirectoryName(ofd.FileName);
                Properties.Settings.Default.ImportDirectory = directoryPath;
                Properties.Settings.Default.Save();

                CheckProduct(directoryPath);
            }
        }

        public bool CheckProduct(String directoryPath, bool quiet = false)
        {
            bool valid = false;
            String failReason = "File 'VersionInfo.txt' missing";

            String projectName = System.IO.Path.GetFileName(directoryPath);
            Project p = new Project(projectName, System.IO.Path.GetDirectoryName(directoryPath));
			String projectVersionString = p.Version;
			if (IncludePlatform)
			{
				if (p.UseWatchDog64)
				{
					projectVersionString += ".x64";
				}
				else
				{
					projectVersionString += ".x86";
				}
			}
            if (p.Installed)
            {
                valid = true;
				if (m_fileStore.HasManifestForVersion(projectVersionString))
                {
					ProjectVersionString = "Already present : " + projectVersionString;
                    failReason = "This version already exists in the file store. Has the 'VersionInfo.txt' file been updated?";
                    valid = false;
                }
                else
                {
					ProjectVersionString = projectVersionString;
                }

                if (!System.IO.File.Exists(p.ExecutablePath))
                {
                    ProjectExecutableString = "Missing : " + p.ExecutablePath;
                    failReason = "Start up executable listed in 'VersionInfo.txt' is not present.";
                    valid = false;
                }
                else
                {
                    ProjectExecutableString = p.ExecutablePath;
                }
                VersionInfoFilePath = p.ProjectDirectory;
                ImportPermitted = valid;
            }

            if (!valid)
            {
                String message = "Directory '" + directoryPath + "' is not a valid product directory.\n\n";
                message += failReason;
                if (quiet)
                {
                    TemporaryMessageBox tmb = new TemporaryMessageBox(message,"Invalid Product", 300);
                    tmb.ShowDialog();
                }
                else
                {
                    MessageBox.Show(message, "Invalid Product", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return valid;
        }
    }
}
