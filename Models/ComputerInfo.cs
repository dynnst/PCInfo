namespace PCNetworkInspector.Models
{
    public class ComputerInfo
    {
        public string Target { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime QueryTime { get; set; } = DateTime.Now;

        // System
        public string ComputerName { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string SystemType { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Workgroup { get; set; } = string.Empty;
        public string CurrentUser { get; set; } = string.Empty;
        public string LastUser { get; set; } = string.Empty;

        // Sub-info
        public CpuInfo? Cpu { get; set; }
        public MemoryInfo? Memory { get; set; }
        public List<DiskInfo> Disks { get; set; } = new();
        public List<GpuInfo> Gpus { get; set; } = new();
        public MotherboardInfo? Motherboard { get; set; }
        public BiosInfo? Bios { get; set; }
        public WindowsInfo? Windows { get; set; }
        public List<NetworkAdapterInfo> NetworkAdapters { get; set; } = new();
        public List<InstalledSoftware> InstalledSoftware { get; set; } = new();
    }

    public class CpuInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public int Cores { get; set; }
        public int Threads { get; set; }
        public string MaxSpeedGhz { get; set; } = string.Empty;
        public string CurrentSpeedGhz { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
    }

    public class MemoryInfo
    {
        public double TotalGb { get; set; }
        public List<MemorySlot> Slots { get; set; } = new();
    }

    public class MemorySlot
    {
        public string Tag { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public double CapacityGb { get; set; }
        public uint SpeedMhz { get; set; }
        public string PartNumber { get; set; } = string.Empty;
    }

    public class DiskInfo
    {
        public string Model { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public double TotalSizeGb { get; set; }
        public string InterfaceType { get; set; } = string.Empty;
        public List<LogicalDiskInfo> LogicalDisks { get; set; } = new();
    }

    public class LogicalDiskInfo
    {
        public string DriveLetter { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string FileSystem { get; set; } = string.Empty;
        public double TotalSizeGb { get; set; }
        public double FreeSpaceGb { get; set; }
        public double UsedSpaceGb => TotalSizeGb - FreeSpaceGb;
        public double UsedPercent => TotalSizeGb > 0 ? (UsedSpaceGb / TotalSizeGb) * 100.0 : 0;
    }

    public class GpuInfo
    {
        public string Name { get; set; } = string.Empty;
        public string AdapterType { get; set; } = string.Empty;
        public string VramText { get; set; } = string.Empty;
        public string DriverVersion { get; set; } = string.Empty;
        public string DriverDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class MotherboardInfo
    {
        public string Manufacturer { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
    }

    public class BiosInfo
    {
        public string Manufacturer { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string ReleaseDate { get; set; } = string.Empty;
        public string SMBIOSVersion { get; set; } = string.Empty;
    }

    public class WindowsInfo
    {
        public string Caption { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string BuildNumber { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
        public string InstallDate { get; set; } = string.Empty;
        public string LastBootTime { get; set; } = string.Empty;
        public string Edition { get; set; } = string.Empty;
        public string ReleaseId { get; set; } = string.Empty;
        public string DisplayVersion { get; set; } = string.Empty;
    }

    public class NetworkAdapterInfo
    {
        public string Name { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
        public List<string> IpAddresses { get; set; } = new();
        public List<string> IPv6Addresses { get; set; } = new();
        public string DefaultGateway { get; set; } = string.Empty;
        public string DhcpServer { get; set; } = string.Empty;
        public List<string> DnsServers { get; set; } = new();
        public bool DhcpEnabled { get; set; }
        public string AdapterType { get; set; } = string.Empty;
        public string Speed { get; set; } = string.Empty;
    }

    public class InstalledSoftware
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string InstallDate { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
    }
}
