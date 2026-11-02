using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.SignalR;
using PCNetworkInspector.Hubs;
using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public class SmartScannerService : ISmartScannerService
    {
        private readonly IHubContext<ScannerHub> _hubContext;
        private readonly IHikvisionService _hikvision;
        private CancellationTokenSource? _scanCts;
        private bool _isScanning;

        // Key ports to probe for device identification
        private static readonly int[] DiscoveryPorts = { 135, 445, 80, 554, 8000, 9100, 22 };

        public SmartScannerService(
            IHubContext<ScannerHub> hubContext,
            IHikvisionService hikvision)
        {
            _hubContext = hubContext;
            _hikvision = hikvision;
        }

        public bool IsScanning => _isScanning;

        public void StopScan()
        {
            _scanCts?.Cancel();
            _isScanning = false;
        }

        public async Task<List<ScanItemResult>> StartScanAsync(
            string rangeInput = "10.7.202.1 - 10.7.202.500",
            int maxConcurrency = 25,
            int timeoutMs = 1200,
            CancellationToken cancellationToken = default)
        {
            _scanCts?.Cancel();
            _scanCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _scanCts.Token;

            _isScanning = true;
            var ipList = GenerateIpRange(rangeInput);
            var results = new List<ScanItemResult>();

            int total = ipList.Count;
            int done = 0;
            int onlineCount = 0;

            await _hubContext.Clients.All.SendAsync("ScanStarted", total, token);

            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

            var tasks = ipList.Select(async ip =>
            {
                await semaphore.WaitAsync(token);
                try
                {
                    token.ThrowIfCancellationRequested();

                    var item = await ScanSingleIpAsync(ip, timeoutMs, token);
                    lock (results)
                    {
                        results.Add(item);
                        if (item.IsOnline) onlineCount++;
                    }

                    int currentDone = Interlocked.Increment(ref done);

                    // Push live discovery if device is online (or every progress tick)
                    if (item.IsOnline)
                    {
                        await _hubContext.Clients.All.SendAsync("DeviceDiscovered", item, token);
                    }

                    await _hubContext.Clients.All.SendAsync("ScanProgress", currentDone, total, onlineCount, token);
                }
                catch (OperationCanceledException) { }
                catch { }
                finally
                {
                    semaphore.Release();
                }
            });

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _isScanning = false;
                await _hubContext.Clients.All.SendAsync("ScanCompleted", results.Count(r => r.IsOnline), total, token);
            }

            return results.OrderBy(r => r.IpAddress).ToList();
        }

        private async Task<ScanItemResult> ScanSingleIpAsync(string ip, int timeoutMs, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            var item = new ScanItemResult
            {
                IpAddress = ip,
                IsOnline = false
            };

            // Probe discovery ports in parallel for fast identification
            var portTasks = DiscoveryPorts.Select(async port =>
            {
                bool isOpen = await CheckPortAsync(ip, port, timeoutMs, ct);
                return (port, isOpen);
            });

            var portResults = await Task.WhenAll(portTasks);
            var openPorts = portResults.Where(p => p.isOpen).Select(p => p.port).ToList();
            item.OpenPorts = openPorts;

            sw.Stop();
            item.ResponseTimeMs = sw.ElapsedMilliseconds;

            if (openPorts.Count > 0)
            {
                item.IsOnline = true;
                item.HostName = await TryGetHostNameAsync(ip);

                // Port-based device fingerprinting
                if (openPorts.Contains(554) || openPorts.Contains(8000) || (openPorts.Contains(80) && !openPorts.Contains(135)))
                {
                    // Check if it's a Hikvision camera
                    bool isHik = await _hikvision.IsCameraReachableAsync(ip, 80, 800) ||
                                 await _hikvision.IsCameraReachableAsync(ip, 8000, 800);

                    if (isHik || openPorts.Contains(554))
                    {
                        item.DeviceType = DeviceType.HikvisionCamera;
                        item.Details = "Hikvision IP Kamera / Video Akışı (RTSP 554 / HTTP 80)";
                    }
                    else
                    {
                        item.DeviceType = DeviceType.Unknown;
                        item.Details = $"Açık Portlar: {string.Join(", ", openPorts)}";
                    }
                }
                else if (openPorts.Contains(135) || openPorts.Contains(445))
                {
                    item.DeviceType = DeviceType.WindowsPC;
                    item.Details = "Windows İş İstasyonu / Sunucu (WMI & SMB Aktif)";
                }
                else if (openPorts.Contains(9100))
                {
                    item.DeviceType = DeviceType.Printer;
                    item.Details = "Ağ Yazıcısı (RAW JetDirect 9100)";
                }
                else if (openPorts.Contains(22))
                {
                    item.DeviceType = DeviceType.SwitchRouter;
                    item.Details = "Ağ Anahtarı / Router / Linux Cihaz (SSH 22)";
                }
                else
                {
                    item.DeviceType = DeviceType.Unknown;
                    item.Details = $"Açık Portlar: {string.Join(", ", openPorts)}";
                }
            }

            return item;
        }

        private static async Task<bool> CheckPortAsync(string ip, int port, int timeoutMs, CancellationToken ct)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(ip, port);
                var timeoutTask = Task.Delay(timeoutMs, ct);
                var completed = await Task.WhenAny(connectTask, timeoutTask);
                return completed == connectTask && client.Connected;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<string> TryGetHostNameAsync(string ip)
        {
            try
            {
                var entry = await Dns.GetHostEntryAsync(ip);
                return entry.HostName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public List<string> GenerateIpRange(string input)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(input)) return result;

            input = input.Trim();

            // Case 1: "10.7.202.1 - 10.7.202.500" or similar range notation
            if (input.Contains('-'))
            {
                var parts = input.Split('-', StringSplitOptions.TrimEntries);
                if (parts.Length == 2)
                {
                    string startStr = parts[0];
                    string endStr = parts[1];

                    // Check if end is a special format like "10.7.202.500" or simple count
                    if (IPAddress.TryParse(startStr, out var startIp))
                    {
                        var startBytes = startIp.GetAddressBytes();

                        // If user entered something like "10.7.202.500" where the 4th octet is > 255:
                        var endParts = endStr.Split('.');
                        if (endParts.Length == 4 && int.TryParse(endParts[3], out int count) && count > 255)
                        {
                            // Expand starting from startIp for 'count' addresses, skipping .0 and .255 broadcast
                            int added = 0;
                            byte b2 = startBytes[2];
                            byte b3 = startBytes[3];

                            while (added < count)
                            {
                                if (b3 == 0 || b3 == 255)
                                {
                                    // skip broadcast/network
                                }
                                else
                                {
                                    result.Add($"{startBytes[0]}.{startBytes[1]}.{b2}.{b3}");
                                    added++;
                                }

                                if (b3 == 255)
                                {
                                    b3 = 0;
                                    b2++;
                                }
                                else
                                {
                                    b3++;
                                }
                            }
                            return result;
                        }

                        // Standard IP to IP range
                        if (IPAddress.TryParse(endStr, out var endIp))
                        {
                            var endBytes = endIp.GetAddressBytes();
                            uint startUint = ToUint(startBytes);
                            uint endUint = ToUint(endBytes);

                            if (startUint <= endUint)
                            {
                                for (uint u = startUint; u <= endUint; u++)
                                {
                                    var bytes = FromUint(u);
                                    if (bytes[3] != 0 && bytes[3] != 255)
                                    {
                                        result.Add($"{bytes[0]}.{bytes[1]}.{bytes[2]}.{bytes[3]}");
                                    }
                                }
                                return result;
                            }
                        }
                    }
                }
            }

            // Case 2: Comma or newline separated IPs
            var rawList = input.Split(new[] { '\n', '\r', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var s in rawList)
            {
                if (IPAddress.TryParse(s, out _))
                {
                    result.Add(s);
                }
            }

            return result.Distinct().ToList();
        }

        private static uint ToUint(byte[] bytes)
        {
            return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
        }

        private static byte[] FromUint(uint val)
        {
            return new byte[]
            {
                (byte)((val >> 24) & 0xFF),
                (byte)((val >> 16) & 0xFF),
                (byte)((val >> 8) & 0xFF),
                (byte)(val & 0xFF)
            };
        }
    }
}
