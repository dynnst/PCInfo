using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public interface IMotherboardInfoService
    {
        Task<MotherboardInfo?> GetMotherboardInfoAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default);
    }
}
