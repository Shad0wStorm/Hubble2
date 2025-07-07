using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace ManifestTool
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window, INotifyPropertyChanged
    {
        private String m_information;
        public String Information
        {
            get { return m_information; }
            set
            {
                if (m_information != value)
                {
                    m_information = value;
                    RaisePropertyChanged("Information");
                }
            }
        }

        private String m_action;
        public String Action
        {
            get { return m_action; }
            set
            {
                if (m_action != value)
                {
                    m_action = value;
                    RaisePropertyChanged("Action");
                }
            }
        }

        private int m_progress;
        public int Progress
        {
            get
            {
                return m_progress;
            }
            set
            {
                if (m_progress != value)
                {
                    m_progress = value;
                    RaisePropertyChanged("Progress");
                }
            }
        }

        public bool CancelRequested = false;
        public BackgroundWorker Worker = null;

        public ProgressWindow()
        {
            InitializeComponent();

            Information = "Not telling";
            Title = "Something slow";
            Action = "Stuff";
            Progress = 0;

            DataContext = this;
        }

        private void CancelAction(object sender, RoutedEventArgs e)
        {
            CancelRequested = true;
            if (Worker != null)
            {
                Worker.CancelAsync();
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
