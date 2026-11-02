namespace PCNetworkInspector.Models
{
    public enum DeviceType
    {
        Unknown = 0,
        WindowsPC = 1,
        HikvisionCamera = 2,
        Printer = 3,
        SwitchRouter = 4
    }

    public class ScanItemResult
    {
        public string IpAddress { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public DeviceType DeviceType { get; set; } = DeviceType.Unknown;
        public string DeviceTypeDisplay => DeviceType switch
        {
            DeviceType.WindowsPC => "Windows PC",
            DeviceType.HikvisionCamera => "Hikvision Kamera",
            DeviceType.Printer => "Yazıcı",
            DeviceType.SwitchRouter => "Switch / Router",
            _ => "Bilinmeyen Cihaz"
        };
        public string DeviceIcon => DeviceType switch
        {
            DeviceType.WindowsPC => "bi-pc-display",
            DeviceType.HikvisionCamera => "bi-camera-video-fill",
            DeviceType.Printer => "bi-printer-fill",
            DeviceType.SwitchRouter => "bi-hdd-network-fill",
            _ => "bi-question-circle"
        };
        public bool IsOnline { get; set; }
        public List<int> OpenPorts { get; set; } = new();
        public string Details { get; set; } = string.Empty;
        public long ResponseTimeMs { get; set; }
    }
}
