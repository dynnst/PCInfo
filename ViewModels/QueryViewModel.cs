using PCNetworkInspector.Helpers;
using PCNetworkInspector.Models;
using PCNetworkInspector.Services;
using System.Security;
using System.Windows;

namespace PCNetworkInspector.ViewModels
{
    public class QueryViewModel : ViewModelBase
    {
        private readonly IComputerInfoService _computerInfoService;
        private readonly IWmiService _wmiService;
        private readonly ISoftwareInfoService _softwareInfoService;
        private readonly IExportService _exportService;

        private string _targetInput = string.Empty;
        private bool _useCurrentUser = true;
        private string _username = string.Empty;
        private string _domain = string.Empty;
        private SecureString? _password;
        private bool _isBusy;
        private string _statusMessage = string.Empty;
        private bool _isConnected;
        private string _progressText = string.Empty;
        private ComputerInfo? _computerInfo;
        private ConnectionTestResult? _testResult;
        private bool _showCredentials;
        private CancellationTokenSource? _cts;
        private int _selectedTabIndex;

        public QueryViewModel(
            IComputerInfoService computerInfoService,
            IWmiService wmiService,
            ISoftwareInfoService softwareInfoService,
            IExportService exportService)
        {
            _computerInfoService = computerInfoService;
            _wmiService = wmiService;
            _softwareInfoService = softwareInfoService;
            _exportService = exportService;

            TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync, () => !IsBusy && !string.IsNullOrWhiteSpace(TargetInput));
            GetInfoCommand = new AsyncRelayCommand(GetInfoAsync, () => !IsBusy && !string.IsNullOrWhiteSpace(TargetInput));
            CancelCommand = new RelayCommand(Cancel, () => IsBusy);
            LoadSoftwareCommand = new AsyncRelayCommand(LoadSoftwareAsync, () => IsConnected && !IsBusy);
            ExportExcelCommand = new AsyncRelayCommand(ExportToExcelAsync, () => ComputerInfo != null);
            ExportCsvCommand = new AsyncRelayCommand(ExportToCsvAsync, () => ComputerInfo != null);
            ExportPdfCommand = new AsyncRelayCommand(ExportToPdfAsync, () => ComputerInfo != null);
        }

        public AsyncRelayCommand TestConnectionCommand { get; }
        public AsyncRelayCommand GetInfoCommand { get; }
        public RelayCommand CancelCommand { get; }
        public AsyncRelayCommand LoadSoftwareCommand { get; }
        public AsyncRelayCommand ExportExcelCommand { get; }
        public AsyncRelayCommand ExportCsvCommand { get; }
        public AsyncRelayCommand ExportPdfCommand { get; }

        public string TargetInput
        {
            get => _targetInput;
            set => SetProperty(ref _targetInput, value);
        }

        public bool UseCurrentUser
        {
            get => _useCurrentUser;
            set
            {
                SetProperty(ref _useCurrentUser, value);
                ShowCredentials = !value;
            }
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

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public string ProgressText
        {
            get => _progressText;
            set => SetProperty(ref _progressText, value);
        }

        public ComputerInfo? ComputerInfo
        {
            get => _computerInfo;
            set => SetProperty(ref _computerInfo, value);
        }

        public ConnectionTestResult? TestResult
        {
            get => _testResult;
            set => SetProperty(ref _testResult, value);
        }

        public bool ShowCredentials
        {
            get => _showCredentials;
            set => SetProperty(ref _showCredentials, value);
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        private ConnectionCredentials BuildCredentials() => new()
        {
            UseCurrentUser = UseCurrentUser,
            Username = UseCurrentUser ? null : Username,
            Domain = UseCurrentUser ? null : Domain,
            Password = UseCurrentUser ? null : Password
        };

        private async Task TestConnectionAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            IsBusy = true;
            StatusMessage = "Bağlantı test ediliyor...";
            TestResult = null;

            try
            {
                var result = await _wmiService.TestConnectionAsync(
                    TargetInput.Trim(),
                    BuildCredentials(),
                    _cts.Token);

                TestResult = result;

                if (result.IsFullySuccessful)
                    StatusMessage = "Bağlantı başarılı";
                else
                    StatusMessage = result.ErrorMessage ?? "Bağlantı başarısız";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Test zaman aşımına uğradı.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Hata: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task GetInfoAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            IsBusy = true;
            IsConnected = false;
            ComputerInfo = null;
            StatusMessage = "Bilgiler alınıyor...";

            var progress = new Progress<string>(msg => ProgressText = msg);

            try
            {
                var info = await _computerInfoService.GetComputerInfoAsync(
                    TargetInput.Trim(),
                    BuildCredentials(),
                    progress,
                    _cts.Token);

                ComputerInfo = info;

                if (info.IsOnline)
                {
                    IsConnected = true;
                    StatusMessage = $"✓  {info.ComputerName}  —  Bilgiler başarıyla alındı";
                }
                else
                {
                    StatusMessage = $"✕  Bağlantı başarısız: {info.ErrorMessage}";
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Sorgu iptal edildi.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Hata: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                ProgressText = string.Empty;
            }
        }

        private void Cancel()
        {
            _cts?.Cancel();
        }

        private async Task LoadSoftwareAsync()
        {
            if (ComputerInfo == null) return;

            _cts?.Cancel();
            _cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            IsBusy = true;
            ProgressText = "Kurulu programlar yükleniyor... (Bu işlem birkaç dakika sürebilir)";

            try
            {
                var software = await _softwareInfoService.GetInstalledSoftwareAsync(
                    TargetInput.Trim(),
                    BuildCredentials(),
                    _cts.Token);

                ComputerInfo.InstalledSoftware.Clear();
                foreach (var s in software)
                    ComputerInfo.InstalledSoftware.Add(s);

                OnPropertyChanged(nameof(ComputerInfo));
                ProgressText = $"{software.Count} program listelendi.";
            }
            catch (OperationCanceledException)
            {
                ProgressText = "İptal edildi.";
            }
            catch (Exception ex)
            {
                ProgressText = $"Hata: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExportToExcelAsync()
        {
            if (ComputerInfo == null) return;
            var path = GetSavePath("Excel Dosyası|*.xlsx", $"{ComputerInfo.ComputerName}_rapor.xlsx");
            if (path == null) return;
            await _exportService.ExportSingleToExcelAsync(ComputerInfo, path);
            StatusMessage = $"Excel dosyası kaydedildi: {path}";
        }

        private async Task ExportToCsvAsync()
        {
            if (ComputerInfo == null) return;
            var path = GetSavePath("CSV Dosyası|*.csv", $"{ComputerInfo.ComputerName}_rapor.csv");
            if (path == null) return;
            await _exportService.ExportToCsvAsync(new[] { ComputerInfo }, path);
            StatusMessage = $"CSV dosyası kaydedildi: {path}";
        }

        private async Task ExportToPdfAsync()
        {
            if (ComputerInfo == null) return;
            var path = GetSavePath("PDF Dosyası|*.pdf", $"{ComputerInfo.ComputerName}_rapor.pdf");
            if (path == null) return;
            await _exportService.ExportSingleToPdfAsync(ComputerInfo, path);
            StatusMessage = $"PDF dosyası kaydedildi: {path}";
        }

        private static string? GetSavePath(string filter, string defaultName)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = filter,
                FileName = defaultName
            };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }
    }
}
