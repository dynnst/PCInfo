using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public class WindowsInfoService : IWindowsInfoService
    {
        private readonly IWmiService _wmi;

        public WindowsInfoService(IWmiService wmi) => _wmi = wmi;

        public async Task<WindowsInfo?> GetWindowsInfoAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var instances = await _wmi.QueryAsync(target, "Win32_OperatingSystem", credentials,
                    cancellationToken: cancellationToken);

                foreach (var os in instances)
                {
                    string arch = os.CimInstanceProperties["OSArchitecture"]?.Value?.ToString() ?? string.Empty;
                    if (arch.Contains("64")) arch = "64 Bit";
                    else if (arch.Contains("32")) arch = "32 Bit";

                    return new WindowsInfo
                    {
                        Caption = os.CimInstanceProperties["Caption"]?.Value?.ToString() ?? "Bilinmiyor",
                        Version = os.CimInstanceProperties["Version"]?.Value?.ToString() ?? string.Empty,
                        BuildNumber = os.CimInstanceProperties["BuildNumber"]?.Value?.ToString() ?? string.Empty,
                        Architecture = arch,
                        InstallDate = ParseWmiDate(os.CimInstanceProperties["InstallDate"]?.Value?.ToString()),
                        LastBootTime = ParseWmiDate(os.CimInstanceProperties["LastBootUpTime"]?.Value?.ToString()),
                        Edition = os.CimInstanceProperties["Caption"]?.Value?.ToString() ?? string.Empty
                    };
                }
            }
            catch { /* return null */ }

            return null;
        }

        private static string ParseWmiDate(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            // WMI date: 20231015130045.000000+180
            if (raw.Length >= 14 &&
                int.TryParse(raw[..4], out int year) &&
                int.TryParse(raw[4..6], out int month) &&
                int.TryParse(raw[6..8], out int day) &&
                int.TryParse(raw[8..10], out int hour) &&
                int.TryParse(raw[10..12], out int min) &&
                int.TryParse(raw[12..14], out int sec))
            {
                return $"{day:D2}.{month:D2}.{year} {hour:D2}:{min:D2}:{sec:D2}";
            }
            return raw;
        }
    }
}
