using PCNetworkInspector.Helpers;
using PCNetworkInspector.Models;

namespace PCNetworkInspector.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly AppSettings _settings;

        public SettingsViewModel()
        {
            _settings = new AppSettings();
        }

        public int MaxConcurrentConnections
        {
            get => _settings.MaxConcurrentConnections;
            set
            {
                if (_settings.MaxConcurrentConnections != value)
                {
                    _settings.MaxConcurrentConnections = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ConnectionTimeoutSeconds
        {
            get => _settings.ConnectionTimeoutSeconds;
            set
            {
                if (_settings.ConnectionTimeoutSeconds != value)
                {
                    _settings.ConnectionTimeoutSeconds = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DefaultExportPath
        {
            get => _settings.DefaultExportPath;
            set
            {
                if (_settings.DefaultExportPath != value)
                {
                    _settings.DefaultExportPath = value;
                    OnPropertyChanged();
                }
            }
        }

        public AppSettings GetSettings() => _settings;
    }
}
