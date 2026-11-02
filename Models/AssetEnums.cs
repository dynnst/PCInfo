namespace PCNetworkInspector.Models
{
    public enum AssetCategory
    {
        DesktopPc = 1,          // Masaüstü Bilgisayar
        Laptop = 2,             // Dizüstü Bilgisayar
        Monitor = 3,            // Monitör
        Camera = 4,             // IP Kamera (Hikvision vb.)
        OpticalReader = 5,      // Optik Okuyucu (Sınav / OMR)
        FingerprintScanner = 6, // Parmak İzi / PDKS
        Printer = 7,            // Yazıcı / Tarayıcı
        Ups = 8,                // Kesintisiz Güç Kaynağı (UPS)
        NetworkDevice = 9,      // Switch / Router / Ağ
        Server = 10,            // Sunucu
        Other = 99              // Diğer Donanım
    }

    public enum AssetStatus
    {
        Active = 1,     // Kullanımda / Faal
        InStorage = 2,  // Depoda / Yedek
        InRepair = 3,   // Arızalı / Tamirde
        Scrapped = 4    // Hurda / HEK
    }

    public static class AssetExtensions
    {
        public static string GetCategoryName(this AssetCategory category) => category switch
        {
            AssetCategory.DesktopPc => "Masaüstü Bilgisayar",
            AssetCategory.Laptop => "Dizüstü Bilgisayar (Laptop)",
            AssetCategory.Monitor => "Monitör",
            AssetCategory.Camera => "IP Kamera (Hikvision vb.)",
            AssetCategory.OpticalReader => "Optik Okuyucu",
            AssetCategory.FingerprintScanner => "Parmak İzi / PDKS",
            AssetCategory.Printer => "Yazıcı / Tarayıcı",
            AssetCategory.Ups => "Güç Kaynağı (UPS)",
            AssetCategory.NetworkDevice => "Switch / Ağ Cihazı",
            AssetCategory.Server => "Sunucu",
            _ => "Diğer Donanım"
        };

        public static string GetCategoryIcon(this AssetCategory category) => category switch
        {
            AssetCategory.DesktopPc => "bi-pc-display",
            AssetCategory.Laptop => "bi-laptop",
            AssetCategory.Monitor => "bi-display",
            AssetCategory.Camera => "bi-camera-video-fill",
            AssetCategory.OpticalReader => "bi-file-earmark-check-fill",
            AssetCategory.FingerprintScanner => "bi-fingerprint",
            AssetCategory.Printer => "bi-printer-fill",
            AssetCategory.Ups => "bi-battery-charging",
            AssetCategory.NetworkDevice => "bi-hdd-network-fill",
            AssetCategory.Server => "bi-server",
            _ => "bi-cpu"
        };

        public static string GetStatusName(this AssetStatus status) => status switch
        {
            AssetStatus.Active => "Kullanımda / Faal",
            AssetStatus.InStorage => "Depoda / Yedek",
            AssetStatus.InRepair => "Arızalı / Tamirde",
            AssetStatus.Scrapped => "Hurda / HEK",
            _ => "Bilinmiyor"
        };

        public static string GetStatusBadgeClass(this AssetStatus status) => status switch
        {
            AssetStatus.Active => "badge bg-success-subtle text-success border border-success-subtle",
            AssetStatus.InStorage => "badge bg-primary-subtle text-primary border border-primary-subtle",
            AssetStatus.InRepair => "badge bg-warning-subtle text-warning-emphasis border border-warning-subtle",
            AssetStatus.Scrapped => "badge bg-danger-subtle text-danger border border-danger-subtle",
            _ => "badge bg-light text-dark border"
        };
    }
}
