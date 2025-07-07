using System;
using System.Collections.Generic;
using System.IO;
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

namespace LocalisationTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<SourceProject> m_projects = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            String existing = Properties.Settings.Default.Projects;
            if (!String.IsNullOrEmpty(existing))
            {
                LoadProjects(existing);
            }
            UpdateActiveProject();
        }

        private void LoadProjects(String description)
        {
            m_projects = new List<SourceProject>();
            String[] projects = description.Split('<');
            foreach (String project in projects)
            {
                String[] details = project.Split('>');
                SourceProject p = new SourceProject();
                p.SpreadSheet = details[0];
                p.ResourceFile = details[1];
                m_projects.Add(p);
            }
        }

        private void ProjectListUpdated()
        {
            String result = "";
            foreach (SourceProject p in m_projects)
            {
                if (!String.IsNullOrEmpty(result))
                {
                    result = result + "<";
                }
                result = result + p.SpreadSheet + ">" + p.ResourceFile;
            }
            Properties.Settings.Default.Projects = result;
            Properties.Settings.Default.Save();

            UpdateActiveProject();
        }

        private void UpdateActiveProject()
        {
            SourceSelector.Items.Clear();
            if (m_projects!=null)
            {
                foreach (SourceProject p in m_projects)
                {
                    ComboBoxItem item = new ComboBoxItem();
                    item.Content = p.SpreadSheet;
                    item.Tag = p;
                    SourceSelector.Items.Add(item);
                }
            }
            if (SourceSelector.Items.Count>0)
            {
                SourceSelector.SelectedIndex = 0;
            }
        }

        private void SelectedSourceChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (ComboBoxItem item in e.AddedItems)
            {
                SourceProject p = item.Tag as SourceProject;
                if (p != null)
                {
                    p.Refresh();
                    if (0 != m_projects.IndexOf(p))
                    {
                        m_projects.Remove(p);
                        m_projects.Insert(0, p);
                        ProjectListUpdated();
                    }
                    else
                    {
                        ResourceFile.Text = p.ResourceFile;
                        Languages.Text = p.Languages;
                    }
                }
            }
        }

        private void SelectSpreadSheet(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = ".xlsx";
            ofd.Filter = "Excel files (*.xlsx)|*.xlsx|All Files (*.*)|*.*";

            Nullable<bool> result = ofd.ShowDialog();
            if (result == true)
            {
                String ss = ofd.FileName;
                SourceProject found = null;
                if (m_projects!=null)
                {
                    foreach (SourceProject p in m_projects)
                    {
                        if (p.SpreadSheet == ss)
                        {
                            found = p;
                        }
                    }
                }
                if (found != null)
                {
                    m_projects.Remove(found);
                }
                else
                {
                    found = new SourceProject();
                    found.SpreadSheet = ss;
                }
                if (m_projects == null)
                {
                    m_projects = new List<SourceProject>();
                }
                m_projects.Insert(0, found);
                ProjectListUpdated();
            }
        }

        private void SelectResourceFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = ".resx";
            ofd.Filter = "Resource files (*.resx)|*.resx|All Files (*.*)|*.*";

            Nullable<bool> result = ofd.ShowDialog();
            if (result == true)
            {
                if (m_projects.Count>0)
                {
                    m_projects[0].ResourceFile = ofd.FileName;
                    ProjectListUpdated();
                }
            }
        }

        private void ExportChanges(object sender, RoutedEventArgs e)
        {
            if (m_projects.Count > 0)
            {
                SaveFileDialog ofd = new SaveFileDialog();
                ofd.DefaultExt = ".xlsx";
                ofd.Filter = "Excel files (*.xlsx)|*.xlsx|All Files (*.*)|*.*";
                ofd.FileName = "Export.xlsx";

                Nullable<bool> result = ofd.ShowDialog();
                if (result == true)
                {
                    if (ofd.FileName == m_projects[0].SpreadSheet)
                    {
                        MessageBox.Show("Cannot export changes over source spreadsheet");
                    }
                    else
                    {
                        m_projects[0].ExportChanges(ofd.FileName);
                    }
                }
            }
            else
            {
                MessageBox.Show("A project must be defined before it can be exported.");
            }
            UpdateActiveProject();
        }

        private void ImportChanges(object sender, RoutedEventArgs e)
        {
            if (m_projects.Count > 0)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.DefaultExt = ".xlsx";
                ofd.Filter = "Excel files (*.xlsx)|*.xlsx|All Files (*.*)|*.*";
                ofd.FileName = "Export.xlsx";

                Nullable<bool> result = ofd.ShowDialog();
                if (result == true)
                {
                    String source = m_projects[0].SpreadSheet;
                    bool writable = true;
                    {
                        FileInfo info = new FileInfo(source);
                        if (info.IsReadOnly)
                        {
                            MessageBoxResult r = MessageBox.Show("Source spread sheet '" + source + "' is read only.\n\nDo you want to overwrite?",
                                "Read Only File", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (r == MessageBoxResult.Yes)
                            {
                                info.IsReadOnly = false;
                            }
                            else
                            {
                                writable = false;
                            }
                        }
                    }
                    if (writable)
                    {
                        m_projects[0].ImportChanges(ofd.FileName);
                        ReportMessages(m_projects[0].Messages, "Import");
                    }
                }
            }
            else
            {
                MessageBox.Show("A project must be defined before it can be exported.");
            }
            UpdateActiveProject();
        }

        private void ReportMessages(String[] messages, String process)
        {
            if (messages != null)
            {
                String message = "";
                foreach (String line in messages)
                {
                    message = message + line + "\n";
                }
                MessageBox.Show(message, "Warnings from "+process+" Process");
            }
        }

        private void UpdateResX(object sender, RoutedEventArgs e)
        {
            m_projects[0].UpdateResX();
            ReportMessages(m_projects[0].Messages, "Update");
            UpdateActiveProject();
        }

        private void ListDifferences(object sender, RoutedEventArgs e)
        {
            if (m_projects.Count > 0)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.DefaultExt = ".xlsx";
                ofd.Filter = "Excel files (*.xlsx)|*.xlsx|All Files (*.*)|*.*";
                ofd.FileName = "Diff.xlsx";

                Nullable<bool> result = ofd.ShowDialog();
                if (result == true)
                {
                    String source = m_projects[0].SpreadSheet;
                    m_projects[0].DiffChanges(ofd.FileName);
                    ReportMessages(m_projects[0].Messages, "Differences");
                }
            }
            else
            {
                MessageBox.Show("A project must be defined before it can be exported.");
            }
            UpdateActiveProject();
        }
    }
}
