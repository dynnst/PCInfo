using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public class BiosInfoService : IBiosInfoService
    {
        private readonly IWmiService _wmi;

        public BiosInfoService(IWmiService wmi) => _wmi = wmi;

        public async Task<BiosInfo?> GetBiosInfoAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var instances = await _wmi.QueryAsync(target, "Win32_BIOS", credentials,
                    cancellationToken: cancellationToken);

                foreach (var bios in instances)
                {
                    return new BiosInfo
                    {
                        Manufacturer = bios.CimInstanceProperties["Manufacturer"]?.Value?.ToString() ?? "Bilinmiyor",
                        Version = bios.CimInstanceProperties["SMBIOSBIOSVersion"]?.Value?.ToString() ?? string.Empty,
                        ReleaseDate = ParseWmiDate(bios.CimInstanceProperties["ReleaseDate"]?.Value?.ToString()),
                        SMBIOSVersion = $"{bios.CimInstanceProperties["SMBIOSMajorVersion"]?.Value}.{bios.CimInstanceProperties["SMBIOSMinorVersion"]?.Value}"
                    };
                }
            }
            catch { /* return null */ }

            return null;
        }

        private static string ParseWmiDate(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            if (raw.Length >= 8 &&
                int.TryParse(raw[..4], out int year) &&
                int.TryParse(raw[4..6], out int month) &&
                int.TryParse(raw[6..8], out int day))
            {
                return $"{day:D2}.{month:D2}.{year}";
            }
            return raw;
        }
    }
}
