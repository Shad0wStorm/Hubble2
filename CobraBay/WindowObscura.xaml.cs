using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CobraBay
{
    /// <summary>
    /// Interaction logic for WindowObscura.xaml
    /// </summary>
    public partial class WindowObscura : Window
    {
        public WindowObscura(bool forceSoftwareRendering)
        {
            if (forceSoftwareRendering)
            {
                this.Loaded += OnLoaded_ForceSoftwareRendering;
            }
            InitializeComponent();

        }

        private void OnLoaded_ForceSoftwareRendering(object sender, EventArgs e)
        {
            HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            HwndTarget hwndTarget = hwndSource.CompositionTarget;
            hwndTarget.RenderMode = RenderMode.SoftwareOnly;
        }

        override protected void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            if (Owner!=null)
            {
                Owner.Activate();
                Owner = null;
            }
        }
    }
}
