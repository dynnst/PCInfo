using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public interface IDiskInfoService
    {
        Task<List<DiskInfo>> GetDiskInfoAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default);
    }
}
