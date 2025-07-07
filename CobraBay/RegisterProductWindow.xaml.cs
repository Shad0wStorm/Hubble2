using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using ClientSupport;

namespace CobraBay
{
    /// <summary>
    /// Interaction logic for RegisterProductWindow.xaml
    /// </summary>
    public partial class RegisterProductWindow : Window
    {
        private readonly FORCManager _manager;
        private bool _login;
        private bool _register;

        public bool TriggerLogin => _login;

        public bool TriggerRegistration => _register;

        public RegisterProductWindow(FORCManager manager, bool forceSoftwareRendering)
        {
            if (forceSoftwareRendering)
            {
                Loaded += OnLoaded_ForceSoftwareRendering;
            }

            InitializeComponent();

            _manager = manager;
            SetProductDescription();
        }

        private void OnLoaded_ForceSoftwareRendering(object sender, EventArgs e)
        {
            if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
            {
                var hwndTarget = hwndSource.CompositionTarget;
                hwndTarget.RenderMode = RenderMode.SoftwareOnly;
            }
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnRegisterClick(object sender, RoutedEventArgs e)
        {
            _register = true;
            Close();
        }

        private void OnLoginClick(object sender, RoutedEventArgs e)
        {
            _login = true;
            Close();
        }

        private void KeyPressed(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    e.Handled = true;
                    OnRegisterClick(null, null);
                    break;
                case Key.Escape:
                    e.Handled = true;
                    Close();
                    break;
            }
        }

        private void OnSupportClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(LocalResources.Properties.Resources.RW_SupportLink);
            }
            catch (Exception ex)
            {
                // Handle exception, logging is recommended
                Console.WriteLine($"Error opening support link: {ex.Message}");
            }
        }

        private void SetProductDescription()
        {
            string productDescription = _manager.IsEpic
                ? LocalResources.Properties.Resources.PN_Epic
                : _manager.IsSteam
                    ? LocalResources.Properties.Resources.PN_Steam
                    : LocalResources.Properties.Resources.PN_Frontier;

            CreateNewAccountDesc.Text = CreateNewAccountDesc.Text.Replace("{0}", productDescription);
        }
    }
}
