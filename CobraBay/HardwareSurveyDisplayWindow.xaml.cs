using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using ClientSupport;

namespace CobraBay
{
    /// <summary>
    /// Interaction logic for HardwareSurveyDisplayWindow.xaml
    /// </summary>
    public partial class HardwareSurveyDisplayWindow : Window, INotifyPropertyChanged
    {
        private string _hardwareDescription = "Unknown";

        public event PropertyChangedEventHandler PropertyChanged;

        public string HardwareDescription
        {
            get => _hardwareDescription;
            set
            {
                if (_hardwareDescription != value)
                {
                    _hardwareDescription = value;
                    OnPropertyChanged(nameof(HardwareDescription));
                }
            }
        }

        public HardwareSurveyDisplayWindow(FORCManager manager, bool forceSoftwareRendering)
        {
            if (forceSoftwareRendering)
            {
                Loaded += OnLoaded_ForceSoftwareRendering;
            }

            InitializeComponent();
            DataContext = this;

            // Show loading cursor
            Mouse.OverrideCursor = Cursors.Wait;

            // Get hardware description asynchronously
            _ = SetHardwareDescriptionAsync(manager);
        }

        private async Task SetHardwareDescriptionAsync(FORCManager manager)
        {
            try
            {
                HardwareDescription = await Task.Run(() => manager.GetHardwareDescription());
            }
            catch (Exception ex)
            {
                HardwareDescription = $"Exception retrieving hardware survey results:\n\n{ex.Message}";
            }
            finally
            {
                // Reset cursor to default
                Mouse.OverrideCursor = null;
            }
        }

        private void OnLoaded_ForceSoftwareRendering(object sender, EventArgs e)
        {
            if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
            {
                hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
            }
        }

        private void OnClose(object sender, RoutedEventArgs e) => Close();

        private async void OnCopy(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    Clipboard.SetText(HardwareDescription);
                    return;
                }
                catch (Exception)
                {
                    await Task.Delay(100); // Use async delay instead of Thread.Sleep
                }
            }
        }

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
