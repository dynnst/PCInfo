using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public class MemoryInfoService : IMemoryInfoService
    {
        private readonly IWmiService _wmi;

        public MemoryInfoService(IWmiService wmi) => _wmi = wmi;

        public async Task<MemoryInfo?> GetMemoryInfoAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var info = new MemoryInfo();

                // Total RAM from Win32_ComputerSystem
                var systems = await _wmi.QueryAsync(target, "Win32_ComputerSystem", credentials,
                    cancellationToken: cancellationToken);

                foreach (var sys in systems)
                {
                    if (sys.CimInstanceProperties["TotalPhysicalMemory"]?.Value is ulong total)
                        info.TotalGb = Math.Round(total / (1024.0 * 1024 * 1024), 1);
                    break;
                }

                // RAM slots
                var slots = await _wmi.QueryAsync(target, "Win32_PhysicalMemory", credentials,
                    cancellationToken: cancellationToken);

                foreach (var slot in slots)
                {
                    ulong cap = 0;
                    uint speed = 0;

                    if (slot.CimInstanceProperties["Capacity"]?.Value is ulong c) cap = c;
                    if (slot.CimInstanceProperties["Speed"]?.Value is uint s) speed = s;

                    info.Slots.Add(new MemorySlot
                    {
                        Tag = slot.CimInstanceProperties["Tag"]?.Value?.ToString() ?? string.Empty,
                        Manufacturer = slot.CimInstanceProperties["Manufacturer"]?.Value?.ToString()?.Trim() ?? "Bilinmiyor",
                        CapacityGb = Math.Round(cap / (1024.0 * 1024 * 1024), 1),
                        SpeedMhz = speed,
                        PartNumber = slot.CimInstanceProperties["PartNumber"]?.Value?.ToString()?.Trim() ?? string.Empty
                    });
                }

                return info;
            }
            catch
            {
                return null;
            }
        }
    }
}
