using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public class CpuInfoService : ICpuInfoService
    {
        private readonly IWmiService _wmi;

        public CpuInfoService(IWmiService wmi) => _wmi = wmi;

        public async Task<CpuInfo?> GetCpuInfoAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var instances = await _wmi.QueryAsync(target, "Win32_Processor", credentials,
                    cancellationToken: cancellationToken);

                foreach (var cpu in instances)
                {
                    uint maxSpeed = 0;
                    uint curSpeed = 0;
                    int cores = 0;
                    int threads = 0;

                    if (cpu.CimInstanceProperties["MaxClockSpeed"]?.Value is uint ms) maxSpeed = ms;
                    if (cpu.CimInstanceProperties["CurrentClockSpeed"]?.Value is uint cs) curSpeed = cs;
                    if (cpu.CimInstanceProperties["NumberOfCores"]?.Value is uint nc) cores = (int)nc;
                    if (cpu.CimInstanceProperties["NumberOfLogicalProcessors"]?.Value is uint nl) threads = (int)nl;

                    return new CpuInfo
                    {
                        Name = cpu.CimInstanceProperties["Name"]?.Value?.ToString()?.Trim() ?? "Bilinmiyor",
                        Manufacturer = cpu.CimInstanceProperties["Manufacturer"]?.Value?.ToString() ?? string.Empty,
                        Cores = cores,
                        Threads = threads,
                        MaxSpeedGhz = maxSpeed > 0 ? $"{maxSpeed / 1000.0:F2} GHz" : "Bilinmiyor",
                        CurrentSpeedGhz = curSpeed > 0 ? $"{curSpeed / 1000.0:F2} GHz" : "Bilinmiyor",
                        Architecture = cpu.CimInstanceProperties["Architecture"]?.Value switch
                        {
                            (ushort)9 => "x64",
                            (ushort)5 => "ARM",
                            _ => cpu.CimInstanceProperties["Architecture"]?.Value?.ToString() ?? string.Empty
                        }
                    };
                }
            }
            catch { /* return null */ }

            return null;
        }
    }
}
