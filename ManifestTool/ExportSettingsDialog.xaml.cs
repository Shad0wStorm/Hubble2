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
    /// Interaction logic for ExportSettingsDialog.xaml
    /// </summary>
    public partial class ExportSettingsDialog : Window
    {
        public ExportSettingsDialog()
        {
            InitializeComponent();
            SizeToContent = SizeToContent.WidthAndHeight;
        }


        public ManifestExportWorker.Mode SelectedMode
        {
            get
            {
                if (Clean.IsChecked == true)
                {
                    return ManifestExportWorker.Mode.Wipe;
                }
                if (Merge.IsChecked == true)
                {
                    return ManifestExportWorker.Mode.Merge;
                }
                return ManifestExportWorker.Mode.Tidy;
            }
            set
            {
                switch (value)
                {
                    case ManifestExportWorker.Mode.Wipe:
                        {
                            Clean.IsChecked = true;
                            break;
                        }
                    case ManifestExportWorker.Mode.Tidy:
                        {
                            Tidy.IsChecked = true;
                            break;
                        }
                    case ManifestExportWorker.Mode.Merge:
                        {
                            Merge.IsChecked = true;
                            break;
                        }
                }
            }
        }

        private void SelectMode(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelMode(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
