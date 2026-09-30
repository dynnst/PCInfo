using Microsoft.Management.Infrastructure;
using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public interface IWmiService
    {
        Task<IEnumerable<CimInstance>> QueryAsync(
            string target,
            string wmiClass,
            ConnectionCredentials credentials,
            string? condition = null,
            CancellationToken cancellationToken = default);

        Task<ConnectionTestResult> TestConnectionAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default);
    }
}
