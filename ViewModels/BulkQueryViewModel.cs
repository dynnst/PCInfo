using PCNetworkInspector.Helpers;
using PCNetworkInspector.Models;
using PCNetworkInspector.Services;
using System.Collections.ObjectModel;
using System.Security;

namespace PCNetworkInspector.ViewModels
{
    public class BulkQueryResultItem : ViewModelBase
    {
        private string _status = "Bekliyor";
        private string _statusIcon = "⏳";
        private bool _isOnline;
        private ComputerInfo? _info;

        public string Target { get; init; } = string.Empty;

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string StatusIcon
        {
            get => _statusIcon;
            set => SetProperty(ref _statusIcon, value);
        }

        public bool IsOnline
        {
            get => _isOnline;
            set => SetProperty(ref _isOnline, value);
        }

        public ComputerInfo? Info
        {
            get => _info;
            set
            {
                SetProperty(ref _info, value);
                if (value != null)
                {
                    IsOnline = value.IsOnline;
                    Status = value.IsOnline ? "Online" : $"Hata: {value.ErrorMessage}";
                    StatusIcon = value.IsOnline ? "🟢" : "🔴";
                    OnPropertyChanged(nameof(ComputerName));
                    OnPropertyChanged(nameof(Manufacturer));
                    OnPropertyChanged(nameof(Model));
                    OnPropertyChanged(nameof(SerialNumber));
                    OnPropertyChanged(nameof(CpuName));
                    OnPropertyChanged(nameof(RamText));
                    OnPropertyChanged(nameof(OsCaption));
                }
            }
        }

        public string ComputerName => Info?.ComputerName ?? string.Empty;
        public string Manufacturer => Info?.Manufacturer ?? string.Empty;
        public string Model => Info?.Model ?? string.Empty;
        public string SerialNumber => Info?.SerialNumber ?? string.Empty;
        public string CpuName => Info?.Cpu?.Name ?? string.Empty;
        public string RamText => Info?.Memory != null ? $"{Info.Memory.TotalGb:F1} GB" : string.Empty;
        public string OsCaption => Info?.Windows?.Caption ?? string.Empty;
    }

    public class BulkQueryViewModel : ViewModelBase
    {
        private readonly IComputerInfoService _computerInfoService;
        private readonly IExportService _exportService;
        private readonly SettingsViewModel _settings;

        private string _targetList = string.Empty;
        private bool _isBusy;
        private string _progressText = string.Empty;
        private int _totalCount;
        private int _doneCount;
        private int _onlineCount;
        private int _offlineCount;
        private bool _useCurrentUser = true;
        private string _username = string.Empty;
        private string _domain = string.Empty;
        private SecureString? _password;
        private CancellationTokenSource? _cts;
        private BulkQueryResultItem? _selectedItem;

        public BulkQueryViewModel(
            IComputerInfoService computerInfoService,
            IExportService exportService,
            SettingsViewModel settings)
        {
            _computerInfoService = computerInfoService;
            _exportService = exportService;
            _settings = settings;

            Results = new ObservableCollection<BulkQueryResultItem>();
            StartCommand = new AsyncRelayCommand(StartQueryAsync, () => !IsBusy && !string.IsNullOrWhiteSpace(TargetList));
            CancelCommand = new RelayCommand(Cancel, () => IsBusy);
            ClearCommand = new RelayCommand(Clear, () => !IsBusy);
            ExportExcelCommand = new AsyncRelayCommand(ExportExcelAsync, () => Results.Any(r => r.IsOnline));
            ExportCsvCommand = new AsyncRelayCommand(ExportCsvAsync, () => Results.Any(r => r.IsOnline));
            ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync, () => Results.Any(r => r.IsOnline));
        }

        public AsyncRelayCommand StartCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand ClearCommand { get; }
        public AsyncRelayCommand ExportExcelCommand { get; }
        public AsyncRelayCommand ExportCsvCommand { get; }
        public AsyncRelayCommand ExportPdfCommand { get; }

        public ObservableCollection<BulkQueryResultItem> Results { get; }

        public string TargetList
        {
            get => _targetList;
            set => SetProperty(ref _targetList, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string ProgressText
        {
            get => _progressText;
            set => SetProperty(ref _progressText, value);
        }

        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        public int DoneCount
        {
            get => _doneCount;
            set => SetProperty(ref _doneCount, value);
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

        public int MaxConcurrentConnections
        {
            get => _settings.MaxConcurrentConnections;
            set
            {
                _settings.MaxConcurrentConnections = value;
                OnPropertyChanged();
            }
        }

        public bool UseCurrentUser
        {
            get => _useCurrentUser;
            set => SetProperty(ref _useCurrentUser, value);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Domain
        {
            get => _domain;
            set => SetProperty(ref _domain, value);
        }

        public SecureString? Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public BulkQueryResultItem? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        private ConnectionCredentials BuildCredentials() => new()
        {
            UseCurrentUser = UseCurrentUser,
            Username = UseCurrentUser ? null : Username,
            Domain = UseCurrentUser ? null : Domain,
            Password = UseCurrentUser ? null : Password
        };

        private async Task StartQueryAsync()
        {
            var targets = TargetList
                .Split(new[] { '\n', '\r', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToList();

            if (targets.Count == 0) return;

            _cts = new CancellationTokenSource();
            IsBusy = true;
            Results.Clear();
            DoneCount = 0;
            OnlineCount = 0;
            OfflineCount = 0;
            TotalCount = targets.Count;

            // Pre-populate result items
            foreach (var t in targets)
                Results.Add(new BulkQueryResultItem { Target = t });

            int maxConcurrent = _settings.MaxConcurrentConnections;
            var semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            var credentials = BuildCredentials();

            var tasks = Results.Select(async item =>
            {
                await semaphore.WaitAsync(_cts.Token);
                try
                {
                    item.Status = "Sorgulanıyor...";
                    item.StatusIcon = "⏳";

                    var info = await _computerInfoService.GetComputerInfoAsync(
                        item.Target,
                        credentials,
                        cancellationToken: _cts.Token);

                    item.Info = info;
                }
                catch (OperationCanceledException)
                {
                    item.Status = "İptal edildi";
                    item.StatusIcon = "⚫";
                }
                catch (Exception ex)
                {
                    item.Status = $"Hata: {ex.Message}";
                    item.StatusIcon = "🔴";
                }
                finally
                {
                    semaphore.Release();
                    var done = System.Threading.Interlocked.Increment(ref _doneCount);
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        DoneCount = done;
                        OnlineCount = Results.Count(r => r.IsOnline);
                        OfflineCount = Results.Count(r => !r.IsOnline && r.Info != null);
                        ProgressText = $"{done}/{TotalCount} tamamlandı  |  Online: {OnlineCount}  Offline: {OfflineCount}";
                    });
                }
            });

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) { }
            finally
            {
                IsBusy = false;
                ProgressText = $"Tamamlandı  |  Toplam: {TotalCount}  Online: {OnlineCount}  Offline: {OfflineCount}";
            }
        }

        private void Cancel() => _cts?.Cancel();

        private void Clear()
        {
            Results.Clear();
            TotalCount = 0;
            DoneCount = 0;
            OnlineCount = 0;
            OfflineCount = 0;
            ProgressText = string.Empty;
        }

        private async Task ExportExcelAsync()
        {
            var path = GetSavePath("Excel Dosyası|*.xlsx", "toplu_rapor.xlsx");
            if (path == null) return;
            await _exportService.ExportToExcelAsync(Results.Where(r => r.Info != null).Select(r => r.Info!), path);
        }

        private async Task ExportCsvAsync()
        {
            var path = GetSavePath("CSV Dosyası|*.csv", "toplu_rapor.csv");
            if (path == null) return;
            await _exportService.ExportToCsvAsync(Results.Where(r => r.Info != null).Select(r => r.Info!), path);
        }

        private async Task ExportPdfAsync()
        {
            var path = GetSavePath("PDF Dosyası|*.pdf", "toplu_rapor.pdf");
            if (path == null) return;
            await _exportService.ExportToPdfAsync(Results.Where(r => r.Info != null).Select(r => r.Info!), path);
        }

        private static string? GetSavePath(string filter, string defaultName)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog { Filter = filter, FileName = defaultName };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }
    }
}
