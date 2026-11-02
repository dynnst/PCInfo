using PCNetworkInspector.Data.Entities;
using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public interface IExportService
    {
        Task<byte[]> ExportInventoryToExcelAsync(IEnumerable<DeviceEntity> devices);
        Task<byte[]> ExportInventoryToPdfAsync(IEnumerable<DeviceEntity> devices);
        Task<byte[]> ExportSinglePcToExcelAsync(ComputerInfo computer);
        Task<byte[]> ExportSinglePcToPdfAsync(ComputerInfo computer);
    }
}
