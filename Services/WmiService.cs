using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using PCNetworkInspector.Models;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security;

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

            // Step 1: Ping
            bool isLocalhost = IsLocalhost(target);
            if (!isLocalhost)
            {
                try
                {
                    using var ping = new Ping();
                    var reply = await Task.Run(() => ping.Send(target, 3000), cancellationToken);
                    result.NetworkReachable = reply.Status == IPStatus.Success;
                }
                catch
                {
                    result.NetworkReachable = false;
                }

                if (!result.NetworkReachable)
                {
                    result.ErrorMessage = "Bilgisayara ağ üzerinden ulaşılamıyor.";
                    result.Suggestions.Add("Bilgisayar kapalı olabilir.");
                    result.Suggestions.Add("IP adresi veya bilgisayar adı yanlış olabilir.");
                    result.Suggestions.Add("Ağ bağlantısı yok veya bilgisayar farklı bir ağ segmentinde.");
                    return result;
                }
            }
            else
            {
                result.NetworkReachable = true;
            }

            // Step 2: WMI
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
                result.Suggestions.Add("Domain hesabı ile tekrar deneyin.");
            }
            catch (Exception ex)
            {
                result.WmiAccessible = false;
                result.ErrorMessage = ex.Message;
                result.Suggestions.Add("Windows Firewall WMI bağlantısını engelliyor olabilir.");
                result.Suggestions.Add("WMI servisi (winmgmt) çalışmıyor olabilir.");
                result.Suggestions.Add("Uzak bilgisayarda 'Windows Management Instrumentation' servisi başlatın.");
            }

            return result;
        }

        public async Task<IEnumerable<CimInstance>> QueryAsync(
            string target,
            string wmiClass,
            ConnectionCredentials credentials,
            string? condition = null,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                string query = string.IsNullOrWhiteSpace(condition)
                    ? $"SELECT * FROM {wmiClass}"
                    : $"SELECT * FROM {wmiClass} WHERE {condition}";

                bool isLocal = IsLocalhost(target);
                CimSession session;

                if (isLocal)
                {
                    session = CimSession.Create(null);
                }
                else if (credentials.HasCredentials)
                {
                    var options = BuildSessionOptions(credentials);
                    session = CimSession.Create(target, options);
                }
                else
                {
                    var options = new DComSessionOptions
                    {
                        Timeout = TimeSpan.FromSeconds(30)
                    };
                    session = CimSession.Create(target, options);
                }

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var results = session.QueryInstances(@"root\cimv2", "WQL", query);
                    return results.ToList();
                }
                finally
                {
                    session.Dispose();
                }
            }, cancellationToken);
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
                result.Suggestions.Add("Kullanıcının uzak bilgisayarda gerekli yetkisi olmayabilir.");
                result.Suggestions.Add("Domain admin hesabıyla deneyin.");
            }
            else if (msg.Contains("rpc") || msg.Contains("0x800706ba"))
            {
                result.Suggestions.Add("Windows Firewall WMI bağlantısını engelliyor olabilir.");
                result.Suggestions.Add("Uzak bilgisayarda 'Windows Uzaktan Yönetim' (WinRM) veya WMI kurallarını etkinleştirin.");
            }
            else if (msg.Contains("0x80041003"))
            {
                result.Suggestions.Add("WMI servisi çalışmıyor olabilir.");
                result.Suggestions.Add("Uzak bilgisayarda 'winmgmt' servisini başlatın.");
            }
            else
            {
                result.Suggestions.Add("Windows Firewall WMI bağlantısını engelliyor olabilir.");
                result.Suggestions.Add("WMI servisi (winmgmt) çalışmıyor olabilir.");
                result.Suggestions.Add("Uzak bilgisayarda 'Uzak Kayıt Defteri' servisinin çalıştığından emin olun.");
            }
        }
    }
}
