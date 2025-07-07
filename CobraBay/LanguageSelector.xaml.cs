using ClientSupport;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace CobraBay
{
    /// <summary>
    /// Interaction logic for LanguageSelector.xaml
    /// </summary>
    public partial class LanguageSelector : Window
    {
        private readonly FORCManager _manager;
        private string _language;

        public string ActiveLanguage
        {
            get => GetActiveLanguage();
            set => SetActiveLanguage(value);
        }

        public LanguageSelector(FORCManager manager, bool forceSoftwareRendering)
        {
            _manager = manager;

            if (forceSoftwareRendering)
            {
                Loaded += OnLoaded_ForceSoftwareRendering;
            }

            InitializeComponent();
        }

        private void OnLoaded_ForceSoftwareRendering(object sender, EventArgs e)
        {
            if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
            {
                hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
            }
        }

        private string GetActiveLanguage()
        {
            return LanguageList.SelectedIndex < 0 ? _language : ((ComboBoxItem)LanguageList.SelectedItem)?.Tag as string;
        }

        private void SetActiveLanguage(string language)
        {
            LanguageList.Items.Clear();

            // Add system language option
            var systemLanguageItem = new ComboBoxItem
            {
                Content = LocalResources.Properties.Resources.SLW_System,
                Tag = string.Empty
            };
            LanguageList.Items.Add(systemLanguageItem);

            // Populate the available languages
            var availableLanguages = _manager.GetAvailableLanguages();
            var selectedIndex = 0;

            foreach (var supportedLanguage in availableLanguages)
            {
                var cultureInfo = CultureInfo.GetCultureInfo(supportedLanguage);
                var item = new ComboBoxItem
                {
                    Content = cultureInfo.NativeName,
                    Tag = supportedLanguage
                };

                var itemIndex = LanguageList.Items.Add(item);
                if (supportedLanguage == language)
                {
                    selectedIndex = itemIndex;
                }
            }

            _language = language;
            LanguageList.SelectedIndex = selectedIndex;
        }

        private void OnAccept(object sender, RoutedEventArgs e) => DialogResult = true;

        private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
