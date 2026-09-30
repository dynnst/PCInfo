using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public class DiskInfoService : IDiskInfoService
    {
        private readonly IWmiService _wmi;

        public DiskInfoService(IWmiService wmi) => _wmi = wmi;

        public async Task<List<DiskInfo>> GetDiskInfoAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default)
        {
            var result = new List<DiskInfo>();

            try
            {
                // Physical disks
                var physicalDisks = await _wmi.QueryAsync(target, "Win32_DiskDrive", credentials,
                    cancellationToken: cancellationToken);

                var logicalDiskMap = await BuildLogicalDiskMapAsync(target, credentials, cancellationToken);

                foreach (var pd in physicalDisks)
                {
                    ulong size = 0;
                    if (pd.CimInstanceProperties["Size"]?.Value is ulong s) size = s;

                    string mediaTypeRaw = pd.CimInstanceProperties["MediaType"]?.Value?.ToString() ?? string.Empty;
                    string model = pd.CimInstanceProperties["Model"]?.Value?.ToString()?.Trim() ?? "Bilinmiyor";

                    string mediaType = DetermineMediaType(mediaTypeRaw, model);

                    var diskInfo = new DiskInfo
                    {
                        Model = model,
                        SerialNumber = pd.CimInstanceProperties["SerialNumber"]?.Value?.ToString()?.Trim() ?? string.Empty,
                        MediaType = mediaType,
                        TotalSizeGb = Math.Round(size / (1024.0 * 1024 * 1024), 1),
                        InterfaceType = pd.CimInstanceProperties["InterfaceType"]?.Value?.ToString() ?? string.Empty,
                    };

                    // Link logical disks via Win32_DiskDriveToDiskPartition and Win32_LogicalDiskToPartition
                    string deviceId = pd.CimInstanceProperties["DeviceID"]?.Value?.ToString() ?? string.Empty;
                    if (logicalDiskMap.TryGetValue(deviceId, out var logicals))
                        diskInfo.LogicalDisks.AddRange(logicals);

                    result.Add(diskInfo);
                }

                // If no physical disks, fall back to logical only
                if (result.Count == 0)
                {
                    var logicals = await GetLogicalDisksDirectAsync(target, credentials, cancellationToken);
                    if (logicals.Count > 0)
                    {
                        result.Add(new DiskInfo
                        {
                            Model = "Bilinmiyor",
                            LogicalDisks = logicals
                        });
                    }
                }
            }
            catch { /* return empty */ }

            return result;
        }

        private async Task<Dictionary<string, List<LogicalDiskInfo>>> BuildLogicalDiskMapAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken)
        {
            var map = new Dictionary<string, List<LogicalDiskInfo>>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // DiskDrive -> Partition
                var driveToPart = await _wmi.QueryAsync(target, "Win32_DiskDriveToDiskPartition", credentials,
                    cancellationToken: cancellationToken);

                var drivePartMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var rel in driveToPart)
                {
                    string? antecedent = rel.CimInstanceProperties["Antecedent"]?.Value?.ToString();
                    string? dependent = rel.CimInstanceProperties["Dependent"]?.Value?.ToString();
                    if (antecedent != null && dependent != null)
                        drivePartMap[ExtractId(dependent)] = ExtractId(antecedent);
                }

                // Partition -> LogicalDisk
                var partToLogical = await _wmi.QueryAsync(target, "Win32_LogicalDiskToPartition", credentials,
                    cancellationToken: cancellationToken);

                var partLogicalMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var rel in partToLogical)
                {
                    string? antecedent = rel.CimInstanceProperties["Antecedent"]?.Value?.ToString();
                    string? dependent = rel.CimInstanceProperties["Dependent"]?.Value?.ToString();
                    if (antecedent != null && dependent != null)
                        partLogicalMap[ExtractId(antecedent)] = ExtractId(dependent);
                }

                // LogicalDisk details
                var logicalDisks = await _wmi.QueryAsync(target, "Win32_LogicalDisk", credentials,
                    "DriveType = 3", cancellationToken);

                var logicalMap = new Dictionary<string, LogicalDiskInfo>(StringComparer.OrdinalIgnoreCase);
                foreach (var ld in logicalDisks)
                {
                    ulong total = 0, free = 0;
                    if (ld.CimInstanceProperties["Size"]?.Value is ulong t) total = t;
                    if (ld.CimInstanceProperties["FreeSpace"]?.Value is ulong f) free = f;

                    string deviceId = ld.CimInstanceProperties["DeviceID"]?.Value?.ToString() ?? string.Empty;
                    logicalMap[deviceId] = new LogicalDiskInfo
                    {
                        DriveLetter = deviceId,
                        Label = ld.CimInstanceProperties["VolumeName"]?.Value?.ToString() ?? string.Empty,
                        FileSystem = ld.CimInstanceProperties["FileSystem"]?.Value?.ToString() ?? string.Empty,
                        TotalSizeGb = Math.Round(total / (1024.0 * 1024 * 1024), 1),
                        FreeSpaceGb = Math.Round(free / (1024.0 * 1024 * 1024), 1)
                    };
                }

                // Build final map: DriveID -> [LogicalDiskInfo]
                foreach (var (partId, driveId) in drivePartMap)
                {
                    if (partLogicalMap.TryGetValue(partId, out string? logicalId) &&
                        logicalMap.TryGetValue(logicalId, out var ldInfo))
                    {
                        if (!map.ContainsKey(driveId))
                            map[driveId] = new List<LogicalDiskInfo>();
                        map[driveId].Add(ldInfo);
                    }
                }
            }
            catch { /* ignore mapping errors */ }

            return map;
        }

        private async Task<List<LogicalDiskInfo>> GetLogicalDisksDirectAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken)
        {
            var result = new List<LogicalDiskInfo>();
            try
            {
                var disks = await _wmi.QueryAsync(target, "Win32_LogicalDisk", credentials,
                    "DriveType = 3", cancellationToken);

                foreach (var ld in disks)
                {
                    ulong total = 0, free = 0;
                    if (ld.CimInstanceProperties["Size"]?.Value is ulong t) total = t;
                    if (ld.CimInstanceProperties["FreeSpace"]?.Value is ulong f) free = f;

                    result.Add(new LogicalDiskInfo
                    {
                        DriveLetter = ld.CimInstanceProperties["DeviceID"]?.Value?.ToString() ?? string.Empty,
                        Label = ld.CimInstanceProperties["VolumeName"]?.Value?.ToString() ?? string.Empty,
                        FileSystem = ld.CimInstanceProperties["FileSystem"]?.Value?.ToString() ?? string.Empty,
                        TotalSizeGb = Math.Round(total / (1024.0 * 1024 * 1024), 1),
                        FreeSpaceGb = Math.Round(free / (1024.0 * 1024 * 1024), 1)
                    });
                }
            }
            catch { }
            return result;
        }

        private static string ExtractId(string wmiPath)
        {
            // Extract key value from WMI path like \\.\root\cimv2:Win32_DiskPartition.DeviceID="Disk #0, Partition #0"
            int start = wmiPath.LastIndexOf('"');
            if (start < 0) return wmiPath;
            int end = wmiPath.IndexOf('"');
            if (end == start) return wmiPath;

            // Return the value between the last pair of quotes
            var trimmed = wmiPath.Trim();
            var eqIdx = trimmed.LastIndexOf('=');
            if (eqIdx >= 0)
                return trimmed.Substring(eqIdx + 1).Trim('"');

            return wmiPath;
        }

        private static string DetermineMediaType(string mediaType, string model)
        {
            string m = model.ToUpperInvariant();
            if (m.Contains("SSD") || m.Contains("SOLID") || m.Contains("NVME") || m.Contains("NVM"))
                return "SSD";
            if (m.Contains("HDD") || m.Contains("HARD DISK"))
                return "HDD";

            string mt = mediaType.ToUpperInvariant();
            if (mt.Contains("SSD") || mt.Contains("SOLID"))
                return "SSD";
            if (mt.Contains("HDD") || mt.Contains("FIXED"))
                return "HDD";

            return mediaType.Length > 0 ? mediaType : "Bilinmiyor";
        }
    }
}
