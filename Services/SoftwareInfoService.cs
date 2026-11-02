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

            // Query registry paths via CIM StdRegProv (fast and does not trigger MSI self-repair)
            await QueryRegistryViaCimAsync(target, credentials,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                "x64", result, cancellationToken);

            await QueryRegistryViaCimAsync(target, credentials,
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
                "x86", result, cancellationToken);

            // If registry query returned nothing, fallback to Win32_Product
            if (result.Count == 0)
            {
                await TryWin32ProductAsync(target, credentials, result, cancellationToken);
            }

            return result
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .GroupBy(s => s.Name)
                .Select(g => g.First())
                .OrderBy(s => s.Name)
                .ToList();
        }

        private async Task QueryRegistryViaCimAsync(
            string target,
            ConnectionCredentials credentials,
            string regPath,
            string arch,
            List<InstalledSoftware> result,
            CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using var session = _wmi.CreateSession(target, credentials);

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

                            // Skip Windows updates and hotfixes
                            if (name.StartsWith("KB") || name.StartsWith("Update for")) continue;

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
                catch { /* ignore registry query errors */ }
            }, cancellationToken);
        }

        private static string ReadRegString(
            CimSession session,
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
