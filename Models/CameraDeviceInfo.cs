namespace PCNetworkInspector.Models
{
    public class CameraDeviceInfo
    {
        public string IpAddress { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;
        public int HttpPort { get; set; } = 80;
        public int RtspPort { get; set; } = 554;
        public string SnapshotUrl { get; set; } = string.Empty;
        public string RtspUrl { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime QueryTime { get; set; } = DateTime.Now;
    }
}
