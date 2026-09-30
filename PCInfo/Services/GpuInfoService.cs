using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public class GpuInfoService : IGpuInfoService
    {
        private readonly IWmiService _wmi;

        public GpuInfoService(IWmiService wmi) => _wmi = wmi;

        public async Task<List<GpuInfo>> GetGpuInfoAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default)
        {
            var result = new List<GpuInfo>();

            try
            {
                var gpus = await _wmi.QueryAsync(target, "Win32_VideoController", credentials,
                    cancellationToken: cancellationToken);

                foreach (var gpu in gpus)
                {
                    ulong vram = 0;
                    if (gpu.CimInstanceProperties["AdapterRAM"]?.Value is uint vr32) vram = vr32;
                    else if (gpu.CimInstanceProperties["AdapterRAM"]?.Value is ulong vr64) vram = vr64;

                    string vramText = vram > 0
                        ? $"{Math.Round(vram / (1024.0 * 1024 * 1024), 1):F1} GB"
                        : "Bilinmiyor";

                    // Fix for overflow: some WMI returns wrong uint for large VRAM
                    if (vram == uint.MaxValue) vramText = "Bilinmiyor";

                    result.Add(new GpuInfo
                    {
                        Name = gpu.CimInstanceProperties["Caption"]?.Value?.ToString()?.Trim() ?? "Bilinmiyor",
                        AdapterType = gpu.CimInstanceProperties["AdapterCompatibility"]?.Value?.ToString() ?? string.Empty,
                        VramText = vramText,
                        DriverVersion = gpu.CimInstanceProperties["DriverVersion"]?.Value?.ToString() ?? string.Empty,
                        DriverDate = FormatDriverDate(gpu.CimInstanceProperties["DriverDate"]?.Value?.ToString()),
                        Status = gpu.CimInstanceProperties["Status"]?.Value?.ToString() ?? string.Empty
                    });
                }
            }
            catch { /* return empty */ }

            return result;
        }

        private static string FormatDriverDate(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            // WMI date format: 20231015000000.000000+000
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
