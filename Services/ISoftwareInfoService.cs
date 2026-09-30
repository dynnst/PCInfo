using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public interface ISoftwareInfoService
    {
        Task<List<InstalledSoftware>> GetInstalledSoftwareAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default);
    }
}
