using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public interface IMemoryInfoService
    {
        Task<MemoryInfo?> GetMemoryInfoAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default);
    }
}
