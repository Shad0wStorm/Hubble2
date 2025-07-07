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
using System.Windows.Shapes;

namespace ManifestTool
{
    /// <summary>
    /// Interaction logic for TemporaryMessageBox.xaml
    /// </summary>
    public partial class TemporaryMessageBox : Window
    {
        private int m_closeafter;
        System.Threading.Timer m_timer;
        
        public TemporaryMessageBox(String message, String caption, int seconds)
        {
            InitializeComponent();
            Message.Text = message;
            Title = caption;
            Timeout.Text = "";
            m_closeafter = seconds;
            m_timer = new System.Threading.Timer(Tick, null, 0, 1000);
        }

        void Tick(object state)
        {
            --m_closeafter;
            if (m_closeafter <= 0)
            {
                m_timer.Dispose();
                Dispatcher.Invoke(new Action(() =>
                {
                    Close();
                }
                ));
            }
            else
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    if (m_closeafter > 60)
                    {
                        int minutes = (m_closeafter + 30) / 60;
                        Timeout.Text = String.Format("Continuing in {0} minutes.", minutes);
                    }
                    else
                    {
                        Timeout.Text = String.Format("Continuing in {0} seconds", m_closeafter);
                    }
                }
                        ));
            }
        }

        private void OnOKClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
