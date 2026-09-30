using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public interface IWindowsInfoService
    {
        Task<WindowsInfo?> GetWindowsInfoAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default);
    }
}
