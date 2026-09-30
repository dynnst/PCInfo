using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public interface ICpuInfoService
    {
        Task<CpuInfo?> GetCpuInfoAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default);
    }
}
