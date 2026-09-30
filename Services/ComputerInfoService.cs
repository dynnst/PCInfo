using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public class ComputerInfoService : IComputerInfoService
    {
        private readonly IWmiService _wmi;
        private readonly ICpuInfoService _cpu;
        private readonly IMemoryInfoService _memory;
        private readonly IDiskInfoService _disk;
        private readonly IGpuInfoService _gpu;
        private readonly INetworkInfoService _network;
        private readonly IWindowsInfoService _windows;
        private readonly IBiosInfoService _bios;
        private readonly IMotherboardInfoService _motherboard;

        public ComputerInfoService(
            IWmiService wmi,
            ICpuInfoService cpu,
            IMemoryInfoService memory,
            IDiskInfoService disk,
            IGpuInfoService gpu,
            INetworkInfoService network,
            IWindowsInfoService windows,
            IBiosInfoService bios,
            IMotherboardInfoService motherboard)
        {
            _wmi = wmi;
            _cpu = cpu;
            _memory = memory;
            _disk = disk;
            _gpu = gpu;
            _network = network;
            _windows = windows;
            _bios = bios;
            _motherboard = motherboard;
        }

        public async Task<ComputerInfo> GetComputerInfoAsync(
            string target,
            ConnectionCredentials credentials,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var info = new ComputerInfo
            {
                Target = target,
                QueryTime = DateTime.Now
            };

            try
            {
                // System info
                progress?.Report("Sistem bilgileri alınıyor...");
                await GetSystemInfoAsync(info, target, credentials, cancellationToken);
                info.IsOnline = true;

                // CPU
                progress?.Report("İşlemci bilgileri alınıyor...");
                info.Cpu = await _cpu.GetCpuInfoAsync(target, credentials, cancellationToken);

                // RAM
                progress?.Report("Bellek bilgileri alınıyor...");
                info.Memory = await _memory.GetMemoryInfoAsync(target, credentials, cancellationToken);

                // Disks
                progress?.Report("Disk bilgileri alınıyor...");
                info.Disks = await _disk.GetDiskInfoAsync(target, credentials, cancellationToken);

                // GPU
                progress?.Report("Ekran kartı bilgileri alınıyor...");
                info.Gpus = await _gpu.GetGpuInfoAsync(target, credentials, cancellationToken);

                // Motherboard
                progress?.Report("Anakart bilgileri alınıyor...");
                info.Motherboard = await _motherboard.GetMotherboardInfoAsync(target, credentials, cancellationToken);

                // BIOS
                progress?.Report("BIOS bilgileri alınıyor...");
                info.Bios = await _bios.GetBiosInfoAsync(target, credentials, cancellationToken);

                // Windows
                progress?.Report("İşletim sistemi bilgileri alınıyor...");
                info.Windows = await _windows.GetWindowsInfoAsync(target, credentials, cancellationToken);

                // Network
                progress?.Report("Ağ bilgileri alınıyor...");
                info.NetworkAdapters = await _network.GetNetworkInfoAsync(target, credentials, cancellationToken);

                progress?.Report("Tamamlandı.");
            }
            catch (OperationCanceledException)
            {
                info.IsOnline = false;
                info.ErrorMessage = "Sorgu iptal edildi.";
            }
            catch (Exception ex)
            {
                info.IsOnline = false;
                info.ErrorMessage = ex.Message;
            }

            return info;
        }

        private async Task GetSystemInfoAsync(
            ComputerInfo info,
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken)
        {
            // Win32_ComputerSystem
            var systems = await _wmi.QueryAsync(target, "Win32_ComputerSystem", credentials,
                cancellationToken: cancellationToken);

            foreach (var sys in systems)
            {
                info.ComputerName = sys.CimInstanceProperties["Name"]?.Value?.ToString() ?? string.Empty;
                info.Manufacturer = sys.CimInstanceProperties["Manufacturer"]?.Value?.ToString() ?? string.Empty;
                info.Model = sys.CimInstanceProperties["Model"]?.Value?.ToString() ?? string.Empty;
                info.SystemType = sys.CimInstanceProperties["SystemType"]?.Value?.ToString() ?? string.Empty;
                info.Domain = sys.CimInstanceProperties["Domain"]?.Value?.ToString() ?? string.Empty;
                info.Workgroup = sys.CimInstanceProperties["Workgroup"]?.Value?.ToString() ?? string.Empty;
                info.CurrentUser = sys.CimInstanceProperties["UserName"]?.Value?.ToString() ?? string.Empty;
                break;
            }

            // Win32_ComputerSystemProduct
            try
            {
                var products = await _wmi.QueryAsync(target, "Win32_ComputerSystemProduct", credentials,
                    cancellationToken: cancellationToken);

                foreach (var prod in products)
                {
                    info.SerialNumber = prod.CimInstanceProperties["IdentifyingNumber"]?.Value?.ToString() ?? string.Empty;
                    info.ProductName = prod.CimInstanceProperties["Name"]?.Value?.ToString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(info.Manufacturer))
                        info.Manufacturer = prod.CimInstanceProperties["Vendor"]?.Value?.ToString() ?? string.Empty;
                    break;
                }
            }
            catch { /* ignore */ }

            // Last user via Win32_UserProfile
            try
            {
                var profiles = await _wmi.QueryAsync(target, "Win32_UserProfile", credentials,
                    cancellationToken: cancellationToken);

                DateTime? lastUse = null;
                foreach (var profile in profiles)
                {
                    var special = profile.CimInstanceProperties["Special"]?.Value;
                    if (special is bool isSpecial && isSpecial) continue;

                    var lastUsedVal = profile.CimInstanceProperties["LastUseTime"]?.Value;
                    if (lastUsedVal is DateTime dt)
                    {
                        if (lastUse == null || dt > lastUse)
                        {
                            lastUse = dt;
                            info.LastUser = profile.CimInstanceProperties["LocalPath"]?.Value?.ToString()
                                ?.Split('\\').LastOrDefault() ?? string.Empty;
                        }
                    }
                }
            }
            catch { /* ignore */ }
        }
    }
}
