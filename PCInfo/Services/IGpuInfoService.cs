using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public interface IGpuInfoService
    {
        Task<List<GpuInfo>> GetGpuInfoAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default);
    }
}
