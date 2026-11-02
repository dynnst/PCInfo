using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public class PeripheralInfoService : IPeripheralInfoService
    {
        private readonly IWmiService _wmi;

        public PeripheralInfoService(IWmiService wmi)
        {
            _wmi = wmi;
        }

        public async Task<List<MonitorInfo>> GetMonitorsAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default)
        {
            var monitors = new List<MonitorInfo>();

            // Method 1: root\wmi -> WmiMonitorID (Contains actual manufacturer, model, and serial number!)
            try
            {
                var instances = await _wmi.QueryAsync(
                    target,
                    "WmiMonitorID",
                    credentials,
                    wmiNamespace: @"root\wmi",
                    cancellationToken: cancellationToken);

                foreach (var inst in instances)
                {
                    string mfg = DecodeUshortArray(inst.CimInstanceProperties["ManufacturerName"]?.Value as ushort[]);
                    string model = DecodeUshortArray(inst.CimInstanceProperties["UserFriendlyName"]?.Value as ushort[]);
                    string serial = DecodeUshortArray(inst.CimInstanceProperties["SerialNumberID"]?.Value as ushort[]);

                    int year = 0;
                    if (inst.CimInstanceProperties["YearOfManufacture"]?.Value is ushort yr) year = yr;

                    int week = 0;
                    if (inst.CimInstanceProperties["WeekOfManufacture"]?.Value is byte wk) week = wk;

                    if (!string.IsNullOrWhiteSpace(model) || !string.IsNullOrWhiteSpace(mfg) || !string.IsNullOrWhiteSpace(serial))
                    {
                        monitors.Add(new MonitorInfo
                        {
                            Manufacturer = string.IsNullOrWhiteSpace(mfg) ? "Bilinmiyor" : mfg,
                            Model = string.IsNullOrWhiteSpace(model) ? (string.IsNullOrWhiteSpace(mfg) ? "Monitör" : $"{mfg} Monitör") : model,
                            SerialNumber = string.IsNullOrWhiteSpace(serial) ? "Mevcut Değil" : serial,
                            YearOfManufacture = year,
                            WeekOfManufacture = week,
                            IsActive = true
                        });
                    }
                }
            }
            catch
            {
                // root\wmi might not be supported on all drivers, proceed to fallback
            }

            // Fallback: root\cimv2 -> Win32_DesktopMonitor
            if (monitors.Count == 0)
            {
                try
                {
                    var instances = await _wmi.QueryAsync(
                        target,
                        "Win32_DesktopMonitor",
                        credentials,
                        cancellationToken: cancellationToken);

                    foreach (var inst in instances)
                    {
                        string name = inst.CimInstanceProperties["Name"]?.Value?.ToString() ?? string.Empty;
                        string mfg = inst.CimInstanceProperties["MonitorManufacturer"]?.Value?.ToString() ?? string.Empty;
                        string type = inst.CimInstanceProperties["MonitorType"]?.Value?.ToString() ?? string.Empty;

                        if (!string.IsNullOrWhiteSpace(name) || !string.IsNullOrWhiteSpace(type))
                        {
                            monitors.Add(new MonitorInfo
                            {
                                Manufacturer = string.IsNullOrWhiteSpace(mfg) ? "Standart" : mfg,
                                Model = string.IsNullOrWhiteSpace(type) ? name : type,
                                SerialNumber = "—",
                                IsActive = true
                            });
                        }
                    }
                }
                catch { }
            }

            return monitors;
        }

        public async Task<List<UsbDeviceInfo>> GetUsbDevicesAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default)
        {
            var usbDevices = new List<UsbDeviceInfo>();

            try
            {
                var instances = await _wmi.QueryAsync(
                    target,
                    "Win32_PnPEntity",
                    credentials,
                    condition: "PNPClass = 'USB' OR Service = 'USBSTOR'",
                    cancellationToken: cancellationToken);

                foreach (var inst in instances)
                {
                    string name = inst.CimInstanceProperties["Name"]?.Value?.ToString() ?? string.Empty;
                    string desc = inst.CimInstanceProperties["Description"]?.Value?.ToString() ?? string.Empty;
                    string devId = inst.CimInstanceProperties["DeviceID"]?.Value?.ToString() ?? string.Empty;
                    string mfg = inst.CimInstanceProperties["Manufacturer"]?.Value?.ToString() ?? string.Empty;
                    string status = inst.CimInstanceProperties["Status"]?.Value?.ToString() ?? "OK";

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        usbDevices.Add(new UsbDeviceInfo
                        {
                            Name = name,
                            Description = desc,
                            DeviceId = devId,
                            Manufacturer = mfg,
                            Status = status
                        });
                    }
                }
            }
            catch { }

            return usbDevices;
        }

        public async Task<List<PrinterInfo>> GetPrintersAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default)
        {
            var printers = new List<PrinterInfo>();

            try
            {
                var instances = await _wmi.QueryAsync(
                    target,
                    "Win32_Printer",
                    credentials,
                    cancellationToken: cancellationToken);

                foreach (var inst in instances)
                {
                    string name = inst.CimInstanceProperties["Name"]?.Value?.ToString() ?? string.Empty;
                    string driver = inst.CimInstanceProperties["DriverName"]?.Value?.ToString() ?? string.Empty;
                    string port = inst.CimInstanceProperties["PortName"]?.Value?.ToString() ?? string.Empty;
                    bool isDefault = inst.CimInstanceProperties["Default"]?.Value as bool? ?? false;
                    bool isNetwork = inst.CimInstanceProperties["Network"]?.Value as bool? ?? false;
                    string status = inst.CimInstanceProperties["Status"]?.Value?.ToString() ?? "OK";

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        printers.Add(new PrinterInfo
                        {
                            Name = name,
                            DriverName = driver,
                            PortName = port,
                            IsDefault = isDefault,
                            IsNetwork = isNetwork,
                            Status = status
                        });
                    }
                }
            }
            catch { }

            return printers;
        }

        private static string DecodeUshortArray(ushort[]? array)
        {
            if (array == null || array.Length == 0) return string.Empty;
            return new string(array.Select(c => (char)c).Where(c => c != 0 && c != '\r' && c != '\n').ToArray()).Trim();
        }
    }
}
