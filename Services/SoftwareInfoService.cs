using Microsoft.Management.Infrastructure;
using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public class SoftwareInfoService : ISoftwareInfoService
    {
        private readonly IWmiService _wmi;

        public SoftwareInfoService(IWmiService wmi) => _wmi = wmi;

        public async Task<List<InstalledSoftware>> GetInstalledSoftwareAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default)
        {
            var result = new List<InstalledSoftware>();

            // Win32_Product is slow and can trigger repairs — use registry via WMI instead
            await QueryRegistryPathAsync(target, credentials,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                "x64", result, cancellationToken);

            await QueryRegistryPathAsync(target, credentials,
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
                "x86", result, cancellationToken);

            return result
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .OrderBy(s => s.Name)
                .ToList();
        }

        private async Task QueryRegistryPathAsync(
            string target,
            ConnectionCredentials credentials,
            string regPath,
            string arch,
            List<InstalledSoftware> result,
            CancellationToken cancellationToken)
        {
            try
            {
                // Use StdRegProv via WMI to read registry
                var instances = await _wmi.QueryAsync(target, "Win32_Product", credentials,
                    cancellationToken: cancellationToken);

                // Win32_Product approach as fallback (slow but works)
                // We try registry first via CIM
                var regItems = await QueryRegistryViaCimAsync(target, credentials, regPath, arch, cancellationToken);
                result.AddRange(regItems);
            }
            catch
            {
                // If registry query fails, try Win32_Product
                await TryWin32ProductAsync(target, credentials, result, cancellationToken);
            }
        }

        private async Task<List<InstalledSoftware>> QueryRegistryViaCimAsync(
            string target,
            ConnectionCredentials credentials,
            string regPath,
            string arch,
            CancellationToken cancellationToken)
        {
            var result = new List<InstalledSoftware>();

            await Task.Run(() =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    bool isLocal = string.IsNullOrWhiteSpace(target) ||
                                   target.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                                   target == "127.0.0.1";

                    using var session = isLocal
                        ? Microsoft.Management.Infrastructure.CimSession.Create(null)
                        : Microsoft.Management.Infrastructure.CimSession.Create(target);

                    const uint HKLM = 0x80000002;

                    // Enumerate subkeys
                    using var enumParams = new CimMethodParametersCollection();
                    enumParams.Add(CimMethodParameter.Create("hDefKey", HKLM, CimFlags.In));
                    enumParams.Add(CimMethodParameter.Create("sSubKeyName", regPath, CimFlags.In));

                    var enumResult = session.InvokeMethod(
                        @"root\cimv2",
                        "StdRegProv",
                        "EnumKey",
                        enumParams);

                    if (enumResult?.OutParameters?["sNames"]?.Value is string[] subKeys)
                    {
                        foreach (var subKey in subKeys)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            string fullPath = $@"{regPath}\{subKey}";
                            string name = ReadRegString(session, HKLM, fullPath, "DisplayName");
                            if (string.IsNullOrWhiteSpace(name)) continue;

                            string version = ReadRegString(session, HKLM, fullPath, "DisplayVersion");
                            string publisher = ReadRegString(session, HKLM, fullPath, "Publisher");
                            string installDate = FormatInstallDate(ReadRegString(session, HKLM, fullPath, "InstallDate"));

                            result.Add(new InstalledSoftware
                            {
                                Name = name,
                                Version = version,
                                Publisher = publisher,
                                InstallDate = installDate,
                                Architecture = arch
                            });
                        }
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch { /* ignore per-registry errors */ }
            }, cancellationToken);

            return result;
        }

        private static string ReadRegString(
            Microsoft.Management.Infrastructure.CimSession session,
            uint hive,
            string path,
            string valueName)
        {
            try
            {
                using var p = new CimMethodParametersCollection();
                p.Add(CimMethodParameter.Create("hDefKey", hive, CimFlags.In));
                p.Add(CimMethodParameter.Create("sSubKeyName", path, CimFlags.In));
                p.Add(CimMethodParameter.Create("sValueName", valueName, CimFlags.In));

                var r = session.InvokeMethod(@"root\cimv2", "StdRegProv", "GetStringValue", p);
                return r?.OutParameters?["sValue"]?.Value?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task TryWin32ProductAsync(
            string target,
            ConnectionCredentials credentials,
            List<InstalledSoftware> result,
            CancellationToken cancellationToken)
        {
            try
            {
                // Only do this if result is empty to avoid duplication
                if (result.Count > 0) return;

                var instances = await _wmi.QueryAsync(target, "Win32_Product", credentials,
                    cancellationToken: cancellationToken);

                foreach (var p in instances)
                {
                    result.Add(new InstalledSoftware
                    {
                        Name = p.CimInstanceProperties["Name"]?.Value?.ToString() ?? string.Empty,
                        Version = p.CimInstanceProperties["Version"]?.Value?.ToString() ?? string.Empty,
                        Publisher = p.CimInstanceProperties["Vendor"]?.Value?.ToString() ?? string.Empty,
                        InstallDate = FormatInstallDate(p.CimInstanceProperties["InstallDate"]?.Value?.ToString()),
                        Architecture = "x64"
                    });
                }
            }
            catch { /* ignore */ }
        }

        private static string FormatInstallDate(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            if (raw.Length == 8 &&
                int.TryParse(raw[..4], out int y) &&
                int.TryParse(raw[4..6], out int m) &&
                int.TryParse(raw[6..8], out int d))
            {
                return $"{d:D2}.{m:D2}.{y}";
            }
            return raw;
        }
    }
}
