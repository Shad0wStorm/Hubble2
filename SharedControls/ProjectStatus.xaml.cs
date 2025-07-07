using System;
using System.Collections.Generic;
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
    /// Interaction logic for ProjectStatus.xaml
    /// </summary>
    public partial class ProjectStatus : UserControl
    {
        public static readonly RoutedEvent OpenLinkEvent = EventManager.RegisterRoutedEvent("OpenLink", RoutingStrategy.Bubble, typeof(LinkEventHandler), typeof(ProjectStatus));

        public event RoutedEventHandler OpenLink
        {
            add { AddHandler(OpenLinkEvent, value); }
            remove { RemoveHandler(OpenLinkEvent, value); }
        }

        public static readonly RoutedEvent StartProjectActionEvent = EventManager.RegisterRoutedEvent("StartProjectAction", RoutingStrategy.Bubble, typeof(ProjectActionEventHandler), typeof(ProjectStatus));

        public event RoutedEventHandler StartProjectAction
        {
            add { AddHandler(StartProjectActionEvent, value); }
            remove { RemoveHandler(StartProjectActionEvent, value); }
        }

        private ProjectProgressCollection m_progress = new ProjectProgressCollection();
        public ProjectProgressCollection ProgressCollection
        {
            get
            {
                return m_progress;
            }
        }

        public ProjectStatus()
        {
            InitializeComponent();

            if (m_progress!=null)
            {
                StatusMonitor.DataContext = m_progress;
                m_progress.ActionCompleted += ProjectActionCompleted;
            }
        }
        
        public event ProjectProgressCollection.ActionCompletedEventHandler ActionCompleted;

        private void ProjectActionCompleted(object sender, ProjectProgressCollection.ActionCompletedEventArgs ac)
        {
            if (ActionCompleted != null)
            {
                ActionCompleted(sender, ac);
            }

            // Possibly remove items that are not in progress?
        }

        private void DataContextChangedHandler(object sender, DependencyPropertyChangedEventArgs e)
        {
            Update();
        }

        public void Update()
        {
            if (DataContext == null)
            {
                Visibility = Visibility.Collapsed;
            }
            else
            {
                Visibility = Visibility.Visible;
            }
            Project project = DataContext as Project;
            if (project!=null)
            {
                // Don't bother with binding these, we would need to extend
                // the classes with dependency properties or property changed
                // events. Since we know they all change at once we can set
                // them up in one go.
                StoreLink.IsEnabled = project.StorePage.IsEnabled;
                SupportLink.IsEnabled = project.SupportPage.IsEnabled;
                StoreLink.Tag = project.StorePage;
                SupportLink.Tag = project.SupportPage;
                if (project.NewsFeed.IsEnabled)
                {
                    NewsFeed.Navigate(new Uri(project.NewsFeed.URL));
                }
            }
        }

        public void SetAccountLink(ExternalLink link)
        {
            AccountLink.Tag = link;
        }

        private void OpenLinkClicked(object sender, RoutedEventArgs e)
        {
            Project project = DataContext as Project;
            if (project !=null)
            {
                FrameworkElement fe = sender as FrameworkElement;
                if (fe != null)
                {
                    ExternalLink target = fe.Tag as ExternalLink;
                    if (target!=null)
                    {
                        LinkEventArgs ea = new LinkEventArgs(OpenLinkEvent, target);
                        RaiseEvent(ea);
                    }
                }
            }
        }

        private void StartAction(object sender, RoutedEventArgs e)
        {
            Project project = DataContext as Project;
            if (project != null)
            {
                FrameworkElement fe = sender as FrameworkElement;
                if (fe == PerformActionButton)
                {
                    ProjectActionEventArgs pae = new ProjectActionEventArgs(StartProjectActionEvent, project);
                    RaiseEvent(pae);
                }
            }
        }
    }
}
