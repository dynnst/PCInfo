using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public interface IComputerInfoService
    {
        Task<ComputerInfo> GetComputerInfoAsync(
            string target,
            ConnectionCredentials credentials,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
