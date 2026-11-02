using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PCNetworkInspector.Data;
using PCNetworkInspector.Data.Entities;
using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly InventoryDbContext _db;
        private readonly IWebHostEnvironment _env;

        public InventoryService(InventoryDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<List<DeviceEntity>> GetAllDevicesAsync(string? search = null, AssetCategory? category = null, AssetStatus? status = null, string? location = null)
        {
            var query = _db.Devices.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(d =>
                    (d.AssetTag != null && d.AssetTag.ToLower().Contains(search)) ||
                    d.Brand.ToLower().Contains(search) ||
                    d.Model.ToLower().Contains(search) ||
                    d.SerialNumber.ToLower().Contains(search) ||
                    d.Location.ToLower().Contains(search) ||
                    (d.RoomNumber != null && d.RoomNumber.ToLower().Contains(search)) ||
                    (d.AssignedPerson != null && d.AssignedPerson.ToLower().Contains(search)) ||
                    (d.IpAddress != null && d.IpAddress.ToLower().Contains(search)) ||
                    (d.Notes != null && d.Notes.ToLower().Contains(search)));
            }

            if (category.HasValue)
            {
                query = query.Where(d => d.Category == category.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(d => d.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(d => d.Location == location);
            }

            return await query.OrderByDescending(d => d.UpdatedAt).ToListAsync();
        }

        public async Task<DeviceEntity?> GetDeviceByIdAsync(int id)
        {
            return await _db.Devices.FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<DeviceEntity?> GetDeviceByIpAsync(string ip)
        {
            return await _db.Devices.FirstOrDefaultAsync(d => d.IpAddress == ip);
        }

        public async Task<DeviceEntity> SaveDeviceAsync(DeviceEntity device, IFormFile? imageFile = null)
        {
            // Handle image upload
            if (imageFile != null && imageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "assets");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                string uniqueFileName = $"{Guid.NewGuid()}{ext}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                device.ImagePath = $"/uploads/assets/{uniqueFileName}";
            }

            device.UpdatedAt = DateTime.Now;

            if (device.Id == 0)
            {
                device.CreatedAt = DateTime.Now;
                _db.Devices.Add(device);
                await _db.SaveChangesAsync();
                return device;
            }
            else
            {
                var existing = await _db.Devices.FindAsync(device.Id);
                if (existing != null)
                {
                    existing.AssetTag = device.AssetTag;
                    existing.Category = device.Category;
                    existing.Brand = device.Brand;
                    existing.Model = device.Model;
                    existing.SerialNumber = device.SerialNumber;
                    existing.Location = device.Location;
                    existing.RoomNumber = device.RoomNumber;
                    existing.AssignedPerson = device.AssignedPerson;
                    existing.ProcurementDate = device.ProcurementDate;
                    existing.DeliveryDate = device.DeliveryDate;
                    existing.WarrantyExpiryDate = device.WarrantyExpiryDate;
                    existing.Status = device.Status;
                    existing.IpAddress = device.IpAddress;
                    existing.MacAddress = device.MacAddress;
                    existing.Notes = device.Notes;
                    existing.UpdatedAt = DateTime.Now;

                    if (!string.IsNullOrWhiteSpace(device.ImagePath))
                        existing.ImagePath = device.ImagePath;

                    if (!string.IsNullOrWhiteSpace(device.TechnicalDetailsJson))
                        existing.TechnicalDetailsJson = device.TechnicalDetailsJson;

                    await _db.SaveChangesAsync();
                    return existing;
                }

                return device;
            }
        }

        public async Task<bool> DeleteDeviceAsync(int id)
        {
            var dev = await _db.Devices.FindAsync(id);
            if (dev == null) return false;

            // Delete physical image file if exists
            if (!string.IsNullOrWhiteSpace(dev.ImagePath))
            {
                try
                {
                    string fullPath = Path.Combine(_env.WebRootPath ?? Directory.GetCurrentDirectory(), dev.ImagePath.TrimStart('/').Replace('/', '\\'));
                    if (File.Exists(fullPath)) File.Delete(fullPath);
                }
                catch { }
            }

            _db.Devices.Remove(dev);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task UpdateDeviceInfoFromWmiAsync(string ip, ComputerInfo info)
        {
            var device = await _db.Devices.FirstOrDefaultAsync(d => d.IpAddress == ip);
            if (device == null)
            {
                device = new DeviceEntity
                {
                    IpAddress = ip,
                    Category = AssetCategory.DesktopPc,
                    Status = AssetStatus.Active,
                    CreatedAt = DateTime.Now
                };
                _db.Devices.Add(device);
            }

            device.UpdatedAt = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(info.ComputerName) && string.IsNullOrWhiteSpace(device.Model))
                device.Model = info.ComputerName;

            if (!string.IsNullOrWhiteSpace(info.Manufacturer) && string.IsNullOrWhiteSpace(device.Brand))
                device.Brand = info.Manufacturer;

            if (!string.IsNullOrWhiteSpace(info.SerialNumber) && string.IsNullOrWhiteSpace(device.SerialNumber))
                device.SerialNumber = info.SerialNumber;

            if (!string.IsNullOrWhiteSpace(info.CurrentUser) && string.IsNullOrWhiteSpace(device.AssignedPerson))
                device.AssignedPerson = info.CurrentUser;

            var firstMac = info.NetworkAdapters.FirstOrDefault(a => !string.IsNullOrWhiteSpace(a.MacAddress))?.MacAddress;
            if (!string.IsNullOrWhiteSpace(firstMac)) device.MacAddress = firstMac;

            // Save technical JSON
            device.TechnicalDetailsJson = JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });

            await _db.SaveChangesAsync();
        }

        public async Task UpdateCameraInfoAsync(string ip, CameraDeviceInfo info)
        {
            var device = await _db.Devices.FirstOrDefaultAsync(d => d.IpAddress == ip);
            if (device == null)
            {
                device = new DeviceEntity
                {
                    IpAddress = ip,
                    Category = AssetCategory.Camera,
                    Brand = "Hikvision",
                    Status = AssetStatus.Active,
                    CreatedAt = DateTime.Now
                };
                _db.Devices.Add(device);
            }

            device.UpdatedAt = DateTime.Now;
            device.Brand = "Hikvision";
            if (!string.IsNullOrWhiteSpace(info.Model)) device.Model = info.Model;
            if (!string.IsNullOrWhiteSpace(info.SerialNumber)) device.SerialNumber = info.SerialNumber;
            if (!string.IsNullOrWhiteSpace(info.MacAddress)) device.MacAddress = info.MacAddress;

            device.TechnicalDetailsJson = JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });

            await _db.SaveChangesAsync();
        }

        public async Task<int> AddFromScanResultsAsync(IEnumerable<ScanItemResult> scanResults)
        {
            int added = 0;
            foreach (var r in scanResults.Where(s => s.IsOnline))
            {
                var existing = await _db.Devices.FirstOrDefaultAsync(d => d.IpAddress == r.IpAddress);
                if (existing != null)
                {
                    existing.UpdatedAt = DateTime.Now;
                    if (string.IsNullOrWhiteSpace(existing.Model) && !string.IsNullOrWhiteSpace(r.HostName))
                        existing.Model = r.HostName;
                }
                else
                {
                    var cat = r.DeviceType switch
                    {
                        DeviceType.WindowsPC => AssetCategory.DesktopPc,
                        DeviceType.HikvisionCamera => AssetCategory.Camera,
                        DeviceType.Printer => AssetCategory.Printer,
                        DeviceType.SwitchRouter => AssetCategory.NetworkDevice,
                        _ => AssetCategory.Other
                    };

                    _db.Devices.Add(new DeviceEntity
                    {
                        IpAddress = r.IpAddress,
                        Category = cat,
                        Brand = cat == AssetCategory.Camera ? "Hikvision" : "",
                        Model = r.HostName,
                        Status = AssetStatus.Active,
                        Notes = r.Details,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                    added++;
                }
            }

            await _db.SaveChangesAsync();
            return added;
        }

        public async Task<InventoryDashboardStats> GetDashboardStatsAsync()
        {
            var all = await _db.Devices.AsNoTracking().ToListAsync();

            var stats = new InventoryDashboardStats
            {
                TotalAssets = all.Count,
                TotalDesktopPc = all.Count(d => d.Category == AssetCategory.DesktopPc),
                TotalLaptop = all.Count(d => d.Category == AssetCategory.Laptop),
                TotalMonitors = all.Count(d => d.Category == AssetCategory.Monitor),
                TotalCameras = all.Count(d => d.Category == AssetCategory.Camera),
                TotalOpticalReaders = all.Count(d => d.Category == AssetCategory.OpticalReader),
                TotalFingerprintScanners = all.Count(d => d.Category == AssetCategory.FingerprintScanner),
                TotalPrinters = all.Count(d => d.Category == AssetCategory.Printer),
                TotalUps = all.Count(d => d.Category == AssetCategory.Ups),
                TotalNetworkDevices = all.Count(d => d.Category == AssetCategory.NetworkDevice),
                TotalOther = all.Count(d => d.Category == AssetCategory.Other || d.Category == AssetCategory.Server),

                TotalActive = all.Count(d => d.Status == AssetStatus.Active),
                TotalInStorage = all.Count(d => d.Status == AssetStatus.InStorage),
                TotalInRepair = all.Count(d => d.Status == AssetStatus.InRepair),
                TotalScrapped = all.Count(d => d.Status == AssetStatus.Scrapped),

                RecentAssets = all.OrderByDescending(d => d.UpdatedAt).Take(10).ToList()
            };

            // Location breakdown
            stats.LocationDistribution = all
                .Where(d => !string.IsNullOrWhiteSpace(d.Location))
                .GroupBy(d => d.Location)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count());

            // Brand breakdown
            stats.BrandDistribution = all
                .Where(d => !string.IsNullOrWhiteSpace(d.Brand))
                .GroupBy(d => d.Brand)
                .OrderByDescending(g => g.Count())
                .Take(8)
                .ToDictionary(g => g.Key, g => g.Count());

            return stats;
        }

        public async Task<List<string>> GetDistinctLocationsAsync()
        {
            return await _db.Devices.AsNoTracking()
                .Where(d => !string.IsNullOrWhiteSpace(d.Location))
                .Select(d => d.Location)
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();
        }
    }
}
