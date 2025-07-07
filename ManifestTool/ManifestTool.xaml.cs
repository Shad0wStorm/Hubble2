using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
using Microsoft.Win32;

namespace ManifestTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ManifestToolWindow : Window, INotifyPropertyChanged
    {
        String m_workingDirectory = null;
        public String WorkingDirectory
        {
            get
            {
                return m_workingDirectory;
            }
            set
            {
                if (value != m_workingDirectory)
                {
                    m_workingDirectory = value;
                    RaisePropertyChanged("WorkingDirectory");
                }
            }
        }

        private String ManifestDirectory
        {
            get
            {
                return System.IO.Path.Combine(WorkingDirectory, "manifests");
            }
        }

        ObservableCollection<String> m_fileStoreItems = new ObservableCollection<String>();
        public ObservableCollection<String> FileStores
        {
            get
            {
                return m_fileStoreItems;
            }
            set
            {
                if (value != m_fileStoreItems)
                {
                    m_fileStoreItems = value;
                    RaisePropertyChanged("FileStores");
                }
            }
        }

        private bool m_addPermitted = false;
        public bool AddPermitted
        {
            get
            {
                return m_addPermitted;
            }
            set
            {
                if (value != m_addPermitted)
                {
                    m_addPermitted = value;
                    RaisePropertyChanged("AddPermitted");
                }
            }
        }

        private bool m_validatePermitted = false;
        public bool ValidatePermitted
        {
            get
            {
                return m_validatePermitted;
            }
            set
            {
                if (value != m_validatePermitted)
                {
                    m_validatePermitted = value;
                    RaisePropertyChanged("ValidatePermitted");
                }
            }
        }

        private bool m_exportPermitted = false;
        public bool ExportPermitted
        {
            get
            {
                return m_exportPermitted;
            }
            set
            {
                if (value != m_exportPermitted)
                {
                    m_exportPermitted = value;
                    RaisePropertyChanged("ExportPermitted");
                }
            }
        }

        private bool m_deletePermitted = false;
        public bool DeletePermitted
        {
            get
            {
                return m_deletePermitted;
            }
            set
            {
                if (value != m_deletePermitted)
                {
                    m_deletePermitted = value;
                    RaisePropertyChanged("DeletePermitted");
                }
            }
        }

        private FileStore m_activeFileStore;
        public FileStore ActiveFileStore
        {
            get
            {
                return m_activeFileStore;
            }
            set
            {
                if (m_activeFileStore != value)
                {
                    m_activeFileStore = value;
                    RaisePropertyChanged("ActiveFileStore");
                }
            }
        }


        public ManifestToolWindow()
        {
            InitializeComponent();

            if (Properties.Settings.Default.UpgradeSettings)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeSettings = false;
                Properties.Settings.Default.Save();
            }

            if (Properties.Settings.Default.KnownFileStores!=null)
            {
                foreach (String fsname in Properties.Settings.Default.KnownFileStores)
                {
                    if (!String.IsNullOrEmpty(fsname))
                    {
                        String fsc = System.IO.Path.Combine(fsname, FileStore.FileStoreName);
                        if (System.IO.File.Exists(fsc))
                        {
                            FileStores.Add(fsname);
                        }
                    }
                }
            }
            if (!String.IsNullOrEmpty(Properties.Settings.Default.CurrentFileStore))
            {
                SetCurrentFileStore(Properties.Settings.Default.CurrentFileStore);
            }

            ProcessArguments();
        }

        private void ProcessArguments()
        {
            String[] args = Environment.GetCommandLineArgs();

            String command = null;

            try
            {
                int current = 1;
                int num_copythreads = 1;
                String name = null;
                bool useSlash = false;
                bool exit = false;
                while (current < args.Length)
                {
                    command = args[current].ToLowerInvariant();

                    if (command == "/parallel")
                    {
                        ++current;
                        
                        if (!Int32.TryParse(args[current], out num_copythreads))
                        {
                            num_copythreads = 1;
                        }

                        if (num_copythreads < 1)
                        {
                            num_copythreads = 1;
                        }

                        if (num_copythreads > 64)
                        {
                            num_copythreads = 64;
                        }

                    }
                    else
                    {
                        if (command == "/filestore")
                        {
                            ++current;
                            SetCurrentFileStore(args[current]);
                        }
                        else
                        {
                            if (command == "/name")
                            {
                                ++current;
                                name = args[current];
                            }
                            else
                            {
                                if (command == "/useslash")
                                {
                                    useSlash = true;
                                }
                                else
                                {
                                    if (command == "/release")
                                    {
                                        ++current;
                                        String source = args[current];
                                        if (String.IsNullOrEmpty(name))
                                        {
                                            MessageBox.Show("When generating a manifest from the command line the name must be provided before the /Release directive.");
                                        }
                                        else
                                        {
                                            AddProductCommand(source, name, useSlash);
                                        }
                                        name = null;
                                        exit = true;
                                    }
                                    else
                                    {
                                        if (command == "/export")
                                        {
                                            ++current;
                                            String target = args[current];
                                            if (String.IsNullOrEmpty(name))
                                            {
                                                MessageBox.Show("When exporting a manifest from the command line the name must be provided before the /Export directive.");
                                            }
                                            else
                                            {
                                                ExportProductCommand(target, name, num_copythreads);
                                            }
                                            name = null;
                                            exit = true;
                                        }
                                        else
                                        {
                                            if (command == "/tidy")
                                            {
                                                TidyFileStore();
                                                exit = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    ++current;
                }
				if (exit)
				{
					Close();
				}
            }
            catch (System.IndexOutOfRangeException)
            {
                MessageBox.Show("Parameter for command "+command+" not found.");
            }
        }

        private void OnBrowseForFileStore(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
            String ext = System.IO.Path.GetExtension(FileStore.FileStoreName).Substring(1);
            sfd.FileName = System.IO.Path.GetFileNameWithoutExtension(FileStore.FileStoreName);
            sfd.DefaultExt = ext;
            sfd.Filter = ext.ToUpperInvariant()+"Files (."+ext.ToLowerInvariant()+")|*."+ext.ToLowerInvariant();
            sfd.CheckFileExists = false;
            sfd.CreatePrompt = false;
            sfd.OverwritePrompt = false;

            Nullable<bool> result = sfd.ShowDialog();

            if (result==true)
            {
                String path = sfd.FileName;
                String basename = System.IO.Path.GetFileName(path);
                if (basename != FileStore.FileStoreName)
                {
                    MessageBox.Show("File Store configuration must be named " + FileStore.FileStoreName+"\n\nOnly one File store is allowed per directory.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (!System.IO.File.Exists(path))
                {
                    CreateNewFileStore(path);
                }

                OpenFileStore(path);
            }
        }

        private void OpenFileStore(String path)
        {
            String storeDirectory = System.IO.Path.GetDirectoryName(path);
            SetCurrentFileStore(storeDirectory);
        }

        private void SetCurrentFileStore(String dir)
        {
            UpdateFileStoreCollection(dir);
            if (!String.IsNullOrEmpty(WorkingDirectory))
            {
                AddPermitted = System.IO.Directory.Exists(WorkingDirectory);
                UpdateActiveFileStore();
            }
        }

        private void UpdateActiveFileStore()
        {
            ActiveFileStore = new FileStore(WorkingDirectory);
            if (!String.IsNullOrEmpty(ActiveFileStore.ConfigurationErrors))
            {
                MessageBox.Show(ActiveFileStore.ConfigurationErrors, "FileStore configuration invalid", MessageBoxButton.OK, MessageBoxImage.Error);
                AddPermitted = false;
            }
        }

        private void UpdateFileStoreCollection(String dir)
        {
            if (System.IO.Directory.Exists(dir))
            {
                String fsc = System.IO.Path.Combine(dir, FileStore.FileStoreName);
                if (System.IO.File.Exists(fsc))
                {
                    if (!FileStores.Contains(dir))
                    {
                        FileStores.Add(dir);

                        if (Properties.Settings.Default.KnownFileStores == null)
                        {
                            Properties.Settings.Default.KnownFileStores = new System.Collections.Specialized.StringCollection();
                        }
                        else
                        {
                            Properties.Settings.Default.KnownFileStores.Clear();
                        }
                        foreach (String fsname in FileStores)
                        {
                            Properties.Settings.Default.KnownFileStores.Add(fsname);
                        }
                        Properties.Settings.Default.CurrentFileStore = WorkingDirectory;
                        Properties.Settings.Default.Save();
                    }

                    WorkingDirectory = dir;

                }
            }
        }
        private void CreateNewFileStore(String path)
        {
            // Currently no useful information is stored in the file we just
            // use it to determine the containing folder.
            //
            // This may change in the future so create an empty file for now.
            using (System.IO.File.Create(path))
            {

            }
        }

        private void FileStoreChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == m_fileStores)
            {
                String fsp = m_fileStores.SelectedItem as String;
                if (fsp!=null)
                {
                    if (FileStores.Contains(fsp))
                    {
                        SetCurrentFileStore(fsp);
                    }
                }
            }
        }

        private void AddProduct(object sender, RoutedEventArgs e)
        {
            // Dialog to manage import of new new product
            AddProductWindow apw = new AddProductWindow(ActiveFileStore);

            bool? result = apw.ShowDialog();

            if (result==true)
            {
                ManifestFileWorker worker = new ManifestFileWorker();
                worker.TaskCompleted += TaskCompleted;
                IsEnabled = false;
                worker.Run(ActiveFileStore, apw.VersionInfoFilePath, apw.ProjectVersionString,
                    apw.ProjectTitleString, apw.UseSlash);
            }
        }

        private void AddProductCommand(String location, String title, bool useSlash)
        {
            AddProductWindow apw = new AddProductWindow(ActiveFileStore);

            if (apw.CheckProduct(location, true))
            {
                ManifestFileWorker worker = new ManifestFileWorker();
                worker.TaskCompleted += AddProductCommandCompleted;
                IsEnabled = false;
                worker.Run(ActiveFileStore, apw.VersionInfoFilePath,
                    apw.ProjectVersionString, title, useSlash);
            }
            else
            {
                Application.Current.Shutdown(1);
            }
            apw.Close();
        }

        void TaskCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ManifestFileWorker worker = sender as ManifestFileWorker;
            if (sender!=null)
            {
                if (e.Error != null)
                {
                    String message = "Exception importing product : " + e.Error.Message + "\n";
                    message += e.Error.StackTrace;
                    MessageBox.Show(message, "Import Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                if (worker.Summary.Added > 0)
                {
                    String message = FilesAddedSummary(worker.Summary);
                    MessageBox.Show(message, "Files Added");
                }
                UpdateActiveFileStore();
                IsEnabled = true;
            }
        }

        void AddProductCommandCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ManifestFileWorker worker = sender as ManifestFileWorker;
            if (sender != null)
            {
                if (e.Error != null)
                {
                    String message = "Exception importing product : " + e.Error.Message + "\n";
                    message += e.Error.StackTrace;
                    MessageBox.Show(message, "Exception adding files");
                    Application.Current.Shutdown(2);
                }
                IsEnabled = true;
            }
        }

        void ExportProductCommand(String location, String title, int num_copythreads)
        {
            ManifestExportWorker worker = new ManifestExportWorker();
            worker.ExportMode = ManifestExportWorker.Mode.Tidy;
            worker.NumCopythreads = num_copythreads;
            IEnumerable<ManifestFileView> ml = ActiveFileStore.ManifestList;
            worker.Source = null;
            ManifestFileView latest = null;

            foreach (ManifestFileView mfv in ml)
            {
                if (mfv.ManifestName == title)
                {
                    worker.Source = mfv;
                }
                if (latest == null)
                {
                    latest = mfv;
                }
                else
                {
                    if (latest.Written < mfv.Written)
                    {
                        latest = mfv;
                    }
                }
            }
            if (worker.Source == null)
            {
                if (title.ToLowerInvariant() == "latest")
                {
                    worker.Source = latest;
                }
                else
                {
                    MessageBox.Show(title + " is not a valid manifest in the given file store.");
                }
            }
            worker.TargetDirectory = location;
            worker.ActiveFileStore = ActiveFileStore;
            worker.TaskCompleted += ExportProductCommandCompleted;
            IsEnabled = false;
            worker.Run();
        }

        void ExportProductCommandCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ManifestFileWorker worker = sender as ManifestFileWorker;
            if (sender != null)
            {
                if (e.Error != null)
                {
                    String message = "Exception importing product : " + e.Error.Message + "\n";
                    message += e.Error.StackTrace;
                    MessageBox.Show(message, "Exception adding files");
                    Application.Current.Shutdown(2);
                }
                IsEnabled = true;
            }
        }

        private String FilesAddedSummary(ManifestFileWorker.Statistics summary)
        {
            String message = "Added " + summary.Added.ToString() + " of " + summary.Total.ToString() + " files to store totaling ";
            message += FileStore.PrettyByteCount(summary.Size);
            return message;
        }

        private void OnManifestSelected(object sender, SelectionChangedEventArgs e)
        {
            if (sender == m_manifestList)
            {
                ExportPermitted = AddPermitted && (m_manifestList.SelectedItems.Count == 1);
                ValidatePermitted = AddPermitted && (m_manifestList.SelectedItems.Count == 1);
                DeletePermitted = AddPermitted && (m_manifestList.SelectedItems.Count == 1);
            }
        }

        private void ExportProduct(object sender, RoutedEventArgs e)
        {
            ManifestFileView mfv = m_manifestList.SelectedItem as ManifestFileView;
            if (mfv != null)
            {
                Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
                sfd.FileName = "VersionInfo.txt";
                if (!String.IsNullOrEmpty(Properties.Settings.Default.ExportDirectory))
                {
                    sfd.FileName = System.IO.Path.Combine(Properties.Settings.Default.ExportDirectory,sfd.FileName);
                }
                sfd.DefaultExt = ".txt";
                sfd.Filter = "TXT Files (.txt)|*.txt";
                sfd.CheckFileExists = false;
                sfd.CreatePrompt = false;
                sfd.OverwritePrompt = false;

                Nullable<bool> result = sfd.ShowDialog();

                if (result == true)
                {
                    String dirname = System.IO.Path.GetDirectoryName(sfd.FileName);

                    bool replacePrompt = false;
                    String[] items = System.IO.Directory.GetDirectories(dirname);
                    if (items.Length > 0)
                    {
                        replacePrompt = true;
                    }
                    else
                    {
                        items = System.IO.Directory.GetFiles(dirname);
                        if (items.Length > 0)
                        {
                            replacePrompt = true;
                        }
                    }
                    bool doexport = true;
                    ManifestExportWorker.Mode exmode = ManifestExportWorker.Mode.Tidy;
                    if (replacePrompt)
                    {
                        ExportSettingsDialog esd = new ExportSettingsDialog();
                        esd.SelectedMode = exmode;

                        bool? dr = esd.ShowDialog();

                        if (dr == true)
                        {
                            exmode = esd.SelectedMode;
                        }
                        else
                        {
                            doexport = false;
                        }
                    }
                    if (doexport)
                    {
                        Properties.Settings.Default.ExportDirectory = dirname;
                        Properties.Settings.Default.Save();
                        ManifestExportWorker worker = new ManifestExportWorker();
                        worker.ExportMode = exmode;
                        worker.Source = mfv;
                        worker.TargetDirectory = dirname;
                        worker.ActiveFileStore = ActiveFileStore;
                        worker.TaskCompleted += ExportCompleted;
                        IsEnabled = false;
                        worker.Run();
                    }
                }
            }
        }

        void ExportCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ManifestExportWorker worker = sender as ManifestExportWorker;
            if (sender != null)
            {
                if (e.Error != null)
                {
                    String message = "Exception exporting product : " + e.Error.Message + "\n";
                    message += e.Error.StackTrace;
                    MessageBox.Show(message, "Export Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    String message = "";
                    if (worker.FilesAdded > 0)
                    {
                        message += String.Format("Added {0} files.\n", worker.FilesAdded);
                    }
                    if (worker.FilesUpdated > 0)
                    {
                        message += String.Format("Updated {0} files.\n", worker.FilesUpdated);
                    }
                    if (worker.FilesRemoved>0)
                    {
                        message += String.Format("Removed {0} files.\n", worker.FilesRemoved);
                    }
                    if (worker.DirectoriesRemoved > 0)
                    {
                        message += String.Format("Removed {0} directories.\n", worker.DirectoriesRemoved);
                    }
                    if (message.Length == 0)
                    {
                        message = "No changes required.";
                    }
                    MessageBox.Show(message, "Export Completed");
                }
                IsEnabled = true;
            }
        }

        private void ValidateProduct(object sender, RoutedEventArgs e)
        {
            ManifestFileView mfv = m_manifestList.SelectedItem as ManifestFileView;
            if (mfv != null)
            {
                ManifestValidateWorker worker = new ManifestValidateWorker();
                worker.Source = mfv;
                worker.ActiveFileStore = ActiveFileStore;
                worker.TaskCompleted += ValidateCompleted;
                IsEnabled = false;
                worker.Run();
            }
        }

        void ValidateCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ManifestValidateWorker worker = sender as ManifestValidateWorker;
            if (sender != null)
            {
                if (e.Error != null)
                {
                    String message = "Exception validating product : " + e.Error.Message + "\n";
                    message += e.Error.StackTrace;
                    MessageBox.Show(message, "Validate Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show(worker.Report, "Validation Completed");
                }
                IsEnabled = true;
            }
        }

        private void DeleteProduct(object sender, RoutedEventArgs e)
        {
            ManifestFileView mfv = m_manifestList.SelectedItem as ManifestFileView;
            if (mfv != null)
            {
                String message = "Do you also want to remove all files no longer referenced by any manifest?";
                MessageBoxResult remove = MessageBox.Show(message, "Remove Manifest", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (remove == MessageBoxResult.Cancel)
                {
                    return;
                }

                String manifestPath = System.IO.Path.Combine(ActiveFileStore.EnsureManifestDirectory(), mfv.FileName);
                if (System.IO.File.Exists(manifestPath))
                {
                    System.IO.File.Delete(manifestPath);
                }

                if (remove==MessageBoxResult.Yes)
                {
                    // Update the active file store since the contents of the one
                    // we have are no longer valid.
                    UpdateActiveFileStore();

                    TidyFileStore();
                }
                else
                {
                    UpdateActiveFileStore();
                }
            }
        }

        void TidyFileStore()
        {
            ManifestTidyWorker worker = new ManifestTidyWorker();
            worker.ActiveFileStore = ActiveFileStore;
            worker.TaskCompleted += TidyCompleted;
            IsEnabled = false;
            worker.Run();
        }

        void TidyCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ManifestTidyWorker worker = sender as ManifestTidyWorker;
            if (worker != null)
            {
                if (e.Error != null)
                {
                    String message = "Exception tidying product : " + e.Error.Message + "\n";
                    message += e.Error.StackTrace;
                    MessageBox.Show(message, "Tidy Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show(worker.Report, "Tidy Completed");
                }
                UpdateActiveFileStore();
                IsEnabled = true;
            }
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
    }
}
