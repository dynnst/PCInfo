using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public interface INetworkInfoService
    {
        Task<List<NetworkAdapterInfo>> GetNetworkInfoAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default);
    }
}
