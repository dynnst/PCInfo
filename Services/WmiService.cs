using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using PCNetworkInspector.Models;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace PCNetworkInspector.Services
{
    public class WmiService : IWmiService
    {
        public async Task<ConnectionTestResult> TestConnectionAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default)
        {
            var result = new ConnectionTestResult();

            // Step 1: Reachability (Ping + Port 135 fallback for firewall-blocked ICMP)
            bool isLocalhost = IsLocalhost(target);
            if (!isLocalhost)
            {
                result.NetworkReachable = await CheckReachabilityAsync(target, cancellationToken);

                if (!result.NetworkReachable)
                {
                    result.ErrorMessage = "Bilgisayara ağ üzerinden ulaşılamıyor.";
                    result.Suggestions.Add("Bilgisayar kapalı olabilir.");
                    result.Suggestions.Add("IP adresi veya bilgisayar adı yanlış olabilir.");
                    result.Suggestions.Add("Ağ bağlantısı yok veya güvenlik duvarı tüm portları engelliyor.");
                    return result;
                }
            }
            else
            {
                result.NetworkReachable = true;
            }

            // Step 2: WMI / CIM Test
            try
            {
                var instances = await QueryAsync(target, "Win32_ComputerSystem", credentials,
                    cancellationToken: cancellationToken);
                var list = instances.ToList();
                result.WmiAccessible = list.Count > 0;
                result.AuthorizationSuccess = list.Count > 0;
            }
            catch (CimException cex)
            {
                result.WmiAccessible = false;
                result.AuthorizationSuccess = false;
                result.ErrorMessage = $"WMI Hatası: {cex.Message}";
                AddWmiSuggestions(result, cex);
            }
            catch (UnauthorizedAccessException)
            {
                result.WmiAccessible = true;
                result.AuthorizationSuccess = false;
                result.ErrorMessage = "Yetkilendirme hatası: Erişim reddedildi.";
                result.Suggestions.Add("Kullanıcının uzak bilgisayarda admin yetkisi olmayabilir.");
                result.Suggestions.Add("Domain veya yerel yönetici hesabı ile tekrar deneyin.");
            }
            catch (Exception ex)
            {
                result.WmiAccessible = false;
                result.ErrorMessage = ex.Message;
                result.Suggestions.Add("Windows Firewall WMI bağlantısını engelliyor olabilir.");
                result.Suggestions.Add("WMI servisi (winmgmt) çalışmıyor olabilir.");
                result.Suggestions.Add("Uzak bilgisayarda 'Windows Management Instrumentation' kuralını açın.");
            }

            return result;
        }

        private static async Task<bool> CheckReachabilityAsync(string target, CancellationToken ct)
        {
            // 1. Try standard Ping
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(target, 2000);
                if (reply.Status == IPStatus.Success) return true;
            }
            catch { }

            // 2. Fallback: Many corporate Windows machines drop ICMP ping but have TCP 135 (RPC) or 445 (SMB) open!
            try
            {
                using var tcpClient = new TcpClient();
                var connectTask = tcpClient.ConnectAsync(target, 135);
                var timeoutTask = Task.Delay(2000, ct);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                if (completedTask == connectTask && tcpClient.Connected) return true;
            }
            catch { }

            try
            {
                using var tcpClient = new TcpClient();
                var connectTask = tcpClient.ConnectAsync(target, 445);
                var timeoutTask = Task.Delay(2000, ct);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                if (completedTask == connectTask && tcpClient.Connected) return true;
            }
            catch { }

            return false;
        }

        public async Task<IEnumerable<CimInstance>> QueryAsync(
            string target,
            string wmiClass,
            ConnectionCredentials credentials,
            string? condition = null,
            CancellationToken cancellationToken = default,
            string wmiNamespace = @"root\cimv2")
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                string query = string.IsNullOrWhiteSpace(condition)
                    ? $"SELECT * FROM {wmiClass}"
                    : $"SELECT * FROM {wmiClass} WHERE {condition}";

                using var session = CreateSession(target, credentials);

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var results = session.QueryInstances(wmiNamespace, "WQL", query);
                    return results.ToList();
                }
                finally
                {
                }
            }, cancellationToken);
        }

        public CimSession CreateSession(string target, ConnectionCredentials credentials)
        {
            bool isLocal = IsLocalhost(target);
            if (isLocal)
            {
                return CimSession.Create(null);
            }

            if (credentials.HasCredentials)
            {
                var options = BuildSessionOptions(credentials);
                return CimSession.Create(target, options);
            }
            else
            {
                var options = new DComSessionOptions
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };
                return CimSession.Create(target, options);
            }
        }

        private static DComSessionOptions BuildSessionOptions(ConnectionCredentials credentials)
        {
            var options = new DComSessionOptions
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            if (credentials.HasCredentials && credentials.Password != null)
            {
                string? domain = string.IsNullOrWhiteSpace(credentials.Domain) ? null : credentials.Domain;
                options.AddDestinationCredentials(
                    new CimCredential(
                        PasswordAuthenticationMechanism.Default,
                        domain,
                        credentials.Username,
                        credentials.Password));
            }

            return options;
        }

        private static bool IsLocalhost(string target)
        {
            return string.IsNullOrWhiteSpace(target) ||
                   target.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                   target == "127.0.0.1" ||
                   target == "::1" ||
                   target.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);
        }

        private static void AddWmiSuggestions(ConnectionTestResult result, CimException cex)
        {
            var msg = cex.Message.ToLowerInvariant();
            if (msg.Contains("access") || msg.Contains("denied") || msg.Contains("0x80070005"))
            {
                result.Suggestions.Add("Kullanıcının uzak bilgisayarda admin yetkisi olmayabilir.");
                result.Suggestions.Add("Domain admin veya hedef makinedeki yerel yönetici hesabıyla deneyin.");
            }
            else if (msg.Contains("rpc") || msg.Contains("0x800706ba"))
            {
                result.Suggestions.Add("Windows Firewall WMI/RPC bağlantısını engelliyor olabilir.");
                result.Suggestions.Add("Uzak bilgisayarda Windows Firewall 'Windows Yönetim Araçları (WMI)' kuralını etkinleştirin.");
            }
            else if (msg.Contains("0x80041003"))
            {
                result.Suggestions.Add("WMI servisi çalışmıyor veya izinleri kısıtlanmış olabilir.");
                result.Suggestions.Add("Uzak bilgisayarda 'winmgmt' servisini yeniden başlatın.");
            }
            else
            {
                result.Suggestions.Add("Windows Firewall WMI bağlantısını engelliyor olabilir.");
                result.Suggestions.Add("WMI servisi (winmgmt) çalışmıyor olabilir.");
            }
        }
    }
}
