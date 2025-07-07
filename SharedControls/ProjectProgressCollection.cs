using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

using ClientSupport;

namespace SharedControls
{
    public class ProjectProgressCollection : INotifyPropertyChanged
    {
        public class ActionCompletedEventArgs : EventArgs
        {
            public Project Project;

            public ActionCompletedEventArgs(Project p)
            {
                Project = p;
            }
        }

        public delegate void ActionCompletedEventHandler(object sender, ActionCompletedEventArgs ac);
        public event ActionCompletedEventHandler ActionCompleted;

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        private ObservableCollection<ProjectProgressMonitor> m_monitors = new ObservableCollection<ProjectProgressMonitor>();
        public ObservableCollection<ProjectProgressMonitor> Monitors
        {
            get { return m_monitors; }
            set { m_monitors = value; }
        }

        public ProgressMonitor Monitor(Project project)
        {
            ProjectProgressMonitor monitor = null;
            for (int mi = 0; mi < Monitors.Count; ++mi)
            {
                ProjectProgressMonitor m = Monitors[mi];
                if (m.Name == project.Name)
                {
                    monitor = m;
                    break;
                }
                if (m.Name.CompareTo(project.Name) > 0)
                {
                    monitor = new ProjectProgressMonitor(project);
                    monitor.PropertyChanged += ProgressChanged;
                    Monitors.Insert(mi, monitor);
                    break;
                }
            }
            if (monitor == null)
            {
                monitor = new ProjectProgressMonitor(project);
                monitor.PropertyChanged += ProgressChanged;
                Monitors.Add(monitor);
            }
            return monitor;
        }

        private void ProgressChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ActionCompleted == null)
            {
                return;
            }
            if (e.PropertyName == "HasCompleted")
            {
                foreach (ProjectProgressMonitor m in Monitors)
                {
                    if (m == sender)
                    {
                        ActionCompletedEventArgs ace = new ActionCompletedEventArgs(m.Project);
                        ActionCompleted(m, ace);
                    }
                }
            }
        }
    }
}
