using Microsoft.AspNetCore.Http;
using PCNetworkInspector.Data.Entities;
using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public interface IInventoryService
    {
        Task<List<DeviceEntity>> GetAllDevicesAsync(string? search = null, AssetCategory? category = null, AssetStatus? status = null, string? location = null);
        Task<DeviceEntity?> GetDeviceByIdAsync(int id);
        Task<DeviceEntity?> GetDeviceByIpAsync(string ip);
        Task<DeviceEntity> SaveDeviceAsync(DeviceEntity device, IFormFile? imageFile = null);
        Task<bool> DeleteDeviceAsync(int id);
        Task UpdateDeviceInfoFromWmiAsync(string ip, ComputerInfo info);
        Task UpdateCameraInfoAsync(string ip, CameraDeviceInfo info);
        Task<int> AddFromScanResultsAsync(IEnumerable<ScanItemResult> scanResults);
        Task<InventoryDashboardStats> GetDashboardStatsAsync();
        Task<List<string>> GetDistinctLocationsAsync();
    }

    public class InventoryDashboardStats
    {
        public int TotalAssets { get; set; }

        // Category breakdown
        public int TotalDesktopPc { get; set; }
        public int TotalLaptop { get; set; }
        public int TotalMonitors { get; set; }
        public int TotalCameras { get; set; }
        public int TotalOpticalReaders { get; set; }
        public int TotalFingerprintScanners { get; set; }
        public int TotalPrinters { get; set; }
        public int TotalUps { get; set; }
        public int TotalNetworkDevices { get; set; }
        public int TotalOther { get; set; }

        // Status breakdown
        public int TotalActive { get; set; }
        public int TotalInStorage { get; set; }
        public int TotalInRepair { get; set; }
        public int TotalScrapped { get; set; }

        // Distributions
        public Dictionary<string, int> LocationDistribution { get; set; } = new();
        public Dictionary<string, int> BrandDistribution { get; set; } = new();
        public List<DeviceEntity> RecentAssets { get; set; } = new();
    }
}
