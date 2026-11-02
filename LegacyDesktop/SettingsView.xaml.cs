using PCNetworkInspector.ViewModels;
using System.Windows.Controls;

namespace PCNetworkInspector.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void BrowseFolderButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Use OpenFileDialog to select a folder in WPF without Windows.Forms dependency
            var dlg = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Dışa aktarma klasörünü seçin"
            };

            if (dlg.ShowDialog() == true)
            {
                if (DataContext is SettingsViewModel vm)
                    vm.DefaultExportPath = dlg.FolderName;
            }
        }
    }
}
