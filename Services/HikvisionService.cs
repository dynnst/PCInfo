using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;
using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public class HikvisionService : IHikvisionService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HikvisionService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> IsCameraReachableAsync(string ip, int port = 80, int timeoutMs = 1500)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(ip, port);
                var timeoutTask = Task.Delay(timeoutMs);
                var completed = await Task.WhenAny(connectTask, timeoutTask);
                return completed == connectTask && client.Connected;
            }
            catch
            {
                return false;
            }
        }

        public async Task<CameraDeviceInfo> GetCameraInfoAsync(
            string ip,
            string username = "admin",
            string password = "",
            int port = 80,
            CancellationToken cancellationToken = default)
        {
            var result = new CameraDeviceInfo
            {
                IpAddress = ip,
                HttpPort = port,
                RtspPort = 554,
                QueryTime = DateTime.Now
            };

            // Setup credentials handler for Digest/Basic
            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(username, password),
                PreAuthenticate = true,
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };

            using var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(6)
            };

            string uri = $"http://{ip}:{port}/ISAPI/System/deviceInfo";

            try
            {
                var response = await httpClient.GetAsync(uri, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    string xmlContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    ParseDeviceInfoXml(xmlContent, result);

                    result.IsOnline = true;
                    result.SnapshotUrl = $"/api/camera/snapshot?ip={ip}";
                    result.RtspUrl = $"rtsp://{username}:{password}@{ip}:554/Streaming/Channels/102";
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    result.IsOnline = true;
                    result.DeviceName = "Hikvision Kamera (Yetki Hatası)";
                    result.Model = "Hikvision";
                    result.ErrorMessage = "Hatalı kullanıcı adı veya şifre (401 Unauthorized)";
                }
                else
                {
                    result.IsOnline = false;
                    result.ErrorMessage = $"HTTP Hata Kodu: {(int)response.StatusCode} {response.ReasonPhrase}";
                }
            }
            catch (Exception ex)
            {
                result.IsOnline = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<byte[]?> GetSnapshotAsync(
            string ip,
            string username = "admin",
            string password = "",
            int channel = 101,
            int port = 80,
            CancellationToken cancellationToken = default)
        {
            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(username, password),
                PreAuthenticate = true,
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };

            using var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(8)
            };

            // Hikvision snapshot URL
            string uri = $"http://{ip}:{port}/ISAPI/Streaming/channels/{channel}/picture";

            try
            {
                var response = await httpClient.GetAsync(uri, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync(cancellationToken);
                }
            }
            catch { }

            // Fallback to channel 1
            try
            {
                string fallbackUri = $"http://{ip}:{port}/ISAPI/Streaming/channels/1/picture";
                var response = await httpClient.GetAsync(fallbackUri, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync(cancellationToken);
                }
            }
            catch { }

            return null;
        }

        private static void ParseDeviceInfoXml(string xml, CameraDeviceInfo info)
        {
            try
            {
                var doc = XDocument.Parse(xml);
                var root = doc.Root;
                if (root == null) return;

                info.DeviceName = GetElementVal(root, "deviceName");
                info.Model = GetElementVal(root, "model");
                info.SerialNumber = GetElementVal(root, "serialNumber");
                info.MacAddress = GetElementVal(root, "macAddress");
                info.FirmwareVersion = GetElementVal(root, "firmwareVersion");

                if (string.IsNullOrWhiteSpace(info.DeviceName))
                    info.DeviceName = info.Model;
            }
            catch { }
        }

        private static string GetElementVal(XElement parent, string localName)
        {
            var el = parent.Descendants().FirstOrDefault(d => d.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));
            return el?.Value?.Trim() ?? string.Empty;
        }
    }
}
