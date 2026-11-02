namespace PCNetworkInspector.Models
{
    public class MonitorInfo
    {
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public int YearOfManufacture { get; set; }
        public int WeekOfManufacture { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UsbDeviceInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class PrinterInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string PortName { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool IsNetwork { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
