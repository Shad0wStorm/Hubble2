using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using ClientSupport;

namespace SharedControls
{
    public delegate void ProjectActionEventHandler(object sender, ProjectActionEventArgs pae);

    public class ProjectActionEventArgs : RoutedEventArgs
    {
        public ProjectActionEventArgs(RoutedEvent routedEvent, Project target)
            : base(routedEvent)
        {
            Target = target;
        }

        public Project Target;
    }
}
