using PCNetworkInspector.ViewModels;
using System.Windows.Controls;

namespace PCNetworkInspector.Views
{
    public partial class QueryView : UserControl
    {
        public QueryView()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is QueryViewModel vm && sender is PasswordBox pb)
                vm.Password = pb.SecurePassword;
        }
    }
}
