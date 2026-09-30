using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public class MotherboardInfoService : IMotherboardInfoService
    {
        private readonly IWmiService _wmi;

        public MotherboardInfoService(IWmiService wmi) => _wmi = wmi;

        public async Task<MotherboardInfo?> GetMotherboardInfoAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var instances = await _wmi.QueryAsync(target, "Win32_BaseBoard", credentials,
                    cancellationToken: cancellationToken);

                foreach (var mb in instances)
                {
                    return new MotherboardInfo
                    {
                        Manufacturer = mb.CimInstanceProperties["Manufacturer"]?.Value?.ToString() ?? "Bilinmiyor",
                        Product = mb.CimInstanceProperties["Product"]?.Value?.ToString() ?? "Bilinmiyor",
                        SerialNumber = mb.CimInstanceProperties["SerialNumber"]?.Value?.ToString() ?? string.Empty,
                        Version = mb.CimInstanceProperties["Version"]?.Value?.ToString() ?? string.Empty
                    };
                }
            }
            catch { /* return null */ }

            return null;
        }
    }
}
