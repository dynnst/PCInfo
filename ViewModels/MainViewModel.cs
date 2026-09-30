using PCNetworkInspector.Helpers;
using System.Collections.Specialized;
using System.ComponentModel;

namespace PCNetworkInspector.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private int _selectedPageIndex;

        public MainViewModel(
            DashboardViewModel dashboard,
            QueryViewModel query,
            BulkQueryViewModel bulkQuery,
            SettingsViewModel settings)
        {
            Dashboard = dashboard;
            Query = query;
            BulkQuery = bulkQuery;
            Settings = settings;

            NavigateCommand = new RelayCommand<int>(idx => SelectedPageIndex = idx);

            // When bulk query results change, update dashboard
            BulkQuery.Results.CollectionChanged += OnBulkResultsChanged;
        }

        public RelayCommand<int> NavigateCommand { get; }

        public DashboardViewModel Dashboard { get; }
        public QueryViewModel Query { get; }
        public BulkQueryViewModel BulkQuery { get; }
        public SettingsViewModel Settings { get; }

        public int SelectedPageIndex
        {
            get => _selectedPageIndex;
            set => SetProperty(ref _selectedPageIndex, value);
        }

        private void OnBulkResultsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (BulkQuery.Results.Count > 0)
                Dashboard.UpdateFromResults(BulkQuery.Results
                    .Where(r => r.Info != null)
                    .Select(r => r.Info!));
        }
    }
}
