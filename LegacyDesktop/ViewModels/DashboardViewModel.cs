using PCNetworkInspector.Helpers;
using PCNetworkInspector.Models;
using System.Collections.ObjectModel;

namespace PCNetworkInspector.ViewModels
{
    public class StatItem : ViewModelBase
    {
        public string Label { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
        public string Icon { get; init; } = string.Empty;
        public string Color { get; init; } = "#2196F3";
    }

    public class DashboardViewModel : ViewModelBase
    {
        private int _totalQueried;
        private int _onlineCount;
        private int _offlineCount;
        private string _lastQueryTime = "—";

        public DashboardViewModel()
        {
            OsStats = new ObservableCollection<StatItem>();
            ManufacturerStats = new ObservableCollection<StatItem>();
        }

        public int TotalQueried
        {
            get => _totalQueried;
            set => SetProperty(ref _totalQueried, value);
        }

        public int OnlineCount
        {
            get => _onlineCount;
            set => SetProperty(ref _onlineCount, value);
        }

        public int OfflineCount
        {
            get => _offlineCount;
            set => SetProperty(ref _offlineCount, value);
        }

        public string LastQueryTime
        {
            get => _lastQueryTime;
            set => SetProperty(ref _lastQueryTime, value);
        }
        public ObservableCollection<StatItem> OsStats { get; }
        public ObservableCollection<StatItem> ManufacturerStats { get; }

        public void UpdateFromResults(IEnumerable<ComputerInfo> results)
        {
            var list = results.ToList();
            TotalQueried = list.Count;
            OnlineCount = list.Count(r => r.IsOnline);
            OfflineCount = list.Count(r => !r.IsOnline);
            LastQueryTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

            OsStats.Clear();
            var osGroups = list
                .Where(r => r.IsOnline && r.Windows != null)
                .GroupBy(r => SimplifyOsName(r.Windows!.Caption))
                .OrderByDescending(g => g.Count());

            string[] osColors = { "#4CAF50", "#2196F3", "#FF9800", "#9C27B0", "#607D8B" };
            int ci = 0;
            foreach (var g in osGroups)
            {
                OsStats.Add(new StatItem
                {
                    Label = g.Key,
                    Value = g.Count().ToString(),
                    Icon = "🪟",
                    Color = osColors[ci++ % osColors.Length]
                });
            }

            ManufacturerStats.Clear();
            var mfGroups = list
                .Where(r => r.IsOnline && !string.IsNullOrWhiteSpace(r.Manufacturer))
                .GroupBy(r => r.Manufacturer)
                .OrderByDescending(g => g.Count());

            string[] mfColors = { "#E91E63", "#00BCD4", "#FF5722", "#8BC34A", "#FFC107" };
            ci = 0;
            foreach (var g in mfGroups)
            {
                ManufacturerStats.Add(new StatItem
                {
                    Label = g.Key,
                    Value = g.Count().ToString(),
                    Icon = "🖥",
                    Color = mfColors[ci++ % mfColors.Length]
                });
            }
        }

        private static string SimplifyOsName(string caption)
        {
            if (caption.Contains("Windows 11")) return "Windows 11";
            if (caption.Contains("Windows 10")) return "Windows 10";
            if (caption.Contains("Windows Server 2022")) return "Server 2022";
            if (caption.Contains("Windows Server 2019")) return "Server 2019";
            if (caption.Contains("Windows Server 2016")) return "Server 2016";
            if (caption.Contains("Windows 8")) return "Windows 8";
            if (caption.Contains("Windows 7")) return "Windows 7";
            return caption.Length > 30 ? caption[..30] : caption;
        }
    }
}
