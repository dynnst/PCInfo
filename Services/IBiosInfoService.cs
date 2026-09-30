using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public interface IBiosInfoService
    {
        Task<BiosInfo?> GetBiosInfoAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default);
    }
}
