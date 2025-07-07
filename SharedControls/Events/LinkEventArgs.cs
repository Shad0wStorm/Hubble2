using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using ClientSupport;

namespace SharedControls
{
    public delegate void LinkEventHandler(object sender, LinkEventArgs le);

    public class LinkEventArgs : RoutedEventArgs
    {
        public LinkEventArgs(RoutedEvent routedEvent, ExternalLink target)
            : base(routedEvent)
        {
            Link = target;
        }

        public ExternalLink Link;
    }
}
