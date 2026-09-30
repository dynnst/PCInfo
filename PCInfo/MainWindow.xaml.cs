using PCNetworkInspector.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace PCNetworkInspector
{
    public partial class MainWindow : Window
    {
        private readonly Button[] _navButtons;
        private readonly UIElement[] _pages;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            _navButtons = new[] { BtnDashboard, BtnQuery, BtnBulkQuery, BtnSettings };
            _pages = new UIElement[] { PageDashboard, PageQuery, PageBulkQuery, PageSettings };
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string tagStr) return;
            if (!int.TryParse(tagStr, out int index)) return;

            NavigateTo(index);
        }

        private void NavigateTo(int index)
        {
            var activeStyle = (System.Windows.Style)Resources["NavButtonActiveStyle"];
            var normalStyle = (System.Windows.Style)Resources["NavButtonStyle"];

            for (int i = 0; i < _navButtons.Length; i++)
            {
                _navButtons[i].Style = i == index ? activeStyle : normalStyle;
                _pages[i].Visibility = i == index ? Visibility.Visible : Visibility.Collapsed;
            }

            if (DataContext is MainViewModel vm)
                vm.SelectedPageIndex = index;
        }
    }
}
