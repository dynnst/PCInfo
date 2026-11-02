using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public interface IPeripheralInfoService
    {
        Task<List<MonitorInfo>> GetMonitorsAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default);

        Task<List<UsbDeviceInfo>> GetUsbDevicesAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default);

        Task<List<PrinterInfo>> GetPrintersAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default);
    }
}
