using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public interface ISmartScannerService
    {
        bool IsScanning { get; }

        Task<List<ScanItemResult>> StartScanAsync(
            string rangeInput = "10.7.202.1 - 10.7.202.500",
            int maxConcurrency = 25,
            int timeoutMs = 1200,
            CancellationToken cancellationToken = default);

        void StopScan();

        List<string> GenerateIpRange(string input);
    }
}
