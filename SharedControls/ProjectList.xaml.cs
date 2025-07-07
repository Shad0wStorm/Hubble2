using System;
using System.Collections.Generic;
using System.Diagnostics;
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

using ClientSupport;

namespace SharedControls
{
    /// <summary>
    /// Interaction logic for ProjectList.xaml
    /// </summary>
    public partial class ProjectList : UserControl
    {
        private FORCManager m_manager;
        private ProgressStatusWindow m_statusWindow;
        private ProgressStatus m_progressControl;
        private ProjectProgressCollection m_progressCollection = new ProjectProgressCollection();

        public ProjectList(FORCManager manager)
        {
            InitializeComponent();

            m_manager = manager;

            FullUpdate();
        }

        public void FullUpdate()
        {
            m_manager.UpdateProjectList();
            Update();
        }

        public void Update()
        {
            Project[] projects = m_manager.AvailableProjects.GetProjectArray();

            Project selected = ProjectListBox.SelectedItem as Project;

            ProjectListBox.Items.Clear();
            foreach (Project p in projects)
            {
                ProjectListBox.Items.Add(p);
            }

            if (selected != null)
            {
                ProjectListBox.SelectedItem = selected;
            }
            else
            {
                if (ProjectListBox.Items.Count > 0)
                {
                    ProjectListBox.SelectedIndex = 0;
                }
            }
            StatusPage.SetAccountLink(m_manager.AccountLink);
        }

        private void OpenExternalLink(object sender, LinkEventArgs e)
        {
            if (e.Link != null)
            {
                Process.Start(e.Link.URL);
            }
        }

        private void PerformProjectAction(object sender, ProjectActionEventArgs e)
        {
            if (e.Target != null)
            {
                Project project = e.Target;
                ProjectStatus status = sender as ProjectStatus;
                ProjectProgressCollection progColl = m_progressCollection;
                if (status != null)
                {
                    if (status.ProgressCollection != null)
                    {
                        progColl = status.ProgressCollection;
                    }
                }
                bool needWindow = progColl == m_progressCollection;
                if (needWindow && project.Action!=Project.ActionType.Play)
                {
                    EnsureStatusWindow();
                }
                switch (project.Action)
                {
                    case Project.ActionType.Install:
                        {
                            ProgressMonitor monitor = progColl.Monitor(project);
                            m_manager.DownloadAndInstallForProject(project, monitor);
                            break;
                        }
                    case Project.ActionType.Update:
                        {
                            ProgressMonitor monitor = progColl.Monitor(project);
                            m_manager.DownloadAndInstallForProject(project, monitor);
                            break;
                        }
                    case Project.ActionType.Play:
                        {
                            String message = m_manager.Run(project, RunnerCompleted);
                            if (message != null)
                            {
                                MessageBox.Show(Window.GetWindow(this),message, project.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            break;
                        }
                    case Project.ActionType.Disabled:
					case Project.ActionType.Invalid:
                        {
                            break;
                        }
                }
                UpdateProjectStatus(project);
            }
        }

        private void RunnerCompleted(object sender, ProjectRunner.ProjectCompletedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                FullUpdate();
            }
                    ));
        }

        private void UpdateProjectStatus(Project project)
        {
            // A bit hacky but it avoids extending the project with
            // property changed events at least for now. Clear the current
            // selected item and select the project on which the action has
            // been triggered (which should be the same item).
            // This way everything rebinds and any changes are reflected
            // in the UI.
            ProjectListBox.SelectedItem = null;
            ProjectListBox.SelectedItem = project;
        }

        private void EnsureStatusWindow()
        {
            if (m_statusWindow == null)
            {
                m_statusWindow = new ProgressStatusWindow();
                m_statusWindow.Owner = Window.GetWindow(this);
                m_statusWindow.Closing += StatusWindowClosing;
                m_progressControl = m_statusWindow.ProgressView;
                m_progressControl.DataContext = m_progressCollection;
                m_progressCollection.ActionCompleted += CommandCompleted;
            }
            m_statusWindow.Show();
            m_statusWindow.Activate();
        }

        private void StatusWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            m_progressCollection.ActionCompleted -= CommandCompleted;
            m_statusWindow = null;
        }

        private void CommandCompleted(object sender, ProjectProgressCollection.ActionCompletedEventArgs ace)
        {
            //foreach (ProjectProgressMonitor m in m_progressCollection.Monitors)
            {
                //if (m == sender)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        FullUpdate();
                    }
                    ));
                }
            }
        }

        private void UninstallProject(object sender, RoutedEventArgs e)
        {
            Project selected = ProjectListBox.SelectedItem as Project;

            if (selected == null)
            {
                return;
            }
            String result = selected.Uninstall(false);
            if (!String.IsNullOrEmpty(result))
            {
                MessageBox.Show(result);
            }
            FullUpdate();
        }
    }
}
