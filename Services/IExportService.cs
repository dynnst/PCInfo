using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public interface IExportService
    {
        Task ExportToExcelAsync(IEnumerable<ComputerInfo> computers, string filePath);
        Task ExportToCsvAsync(IEnumerable<ComputerInfo> computers, string filePath);
        Task ExportToPdfAsync(IEnumerable<ComputerInfo> computers, string filePath);
        Task ExportSingleToExcelAsync(ComputerInfo computer, string filePath);
        Task ExportSingleToPdfAsync(ComputerInfo computer, string filePath);
    }
}
