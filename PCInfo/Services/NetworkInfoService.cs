using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public class NetworkInfoService : INetworkInfoService
    {
        private readonly IWmiService _wmi;

        public NetworkInfoService(IWmiService wmi) => _wmi = wmi;

        public async Task<List<NetworkAdapterInfo>> GetNetworkInfoAsync(
            string target,
            ConnectionCredentials credentials,
            CancellationToken cancellationToken = default)
        {
            var result = new List<NetworkAdapterInfo>();

            try
            {
                var configs = await _wmi.QueryAsync(target, "Win32_NetworkAdapterConfiguration", credentials,
                    "IPEnabled = True", cancellationToken);

                // Build a map of index -> adapter name
                var adapterNames = new Dictionary<uint, string>();
                try
                {
                    var adapters = await _wmi.QueryAsync(target, "Win32_NetworkAdapter", credentials,
                        cancellationToken: cancellationToken);
                    foreach (var ad in adapters)
                    {
                        uint idx = 0;
                        if (ad.CimInstanceProperties["Index"]?.Value is uint i) idx = i;
                        string name = ad.CimInstanceProperties["Name"]?.Value?.ToString() ?? string.Empty;
                        string adapterType = ad.CimInstanceProperties["AdapterType"]?.Value?.ToString() ?? string.Empty;
                        adapterNames[idx] = $"{name}|{adapterType}";
                    }
                }
                catch { /* ignore */ }

                foreach (var cfg in configs)
                {
                    uint index = 0;
                    if (cfg.CimInstanceProperties["Index"]?.Value is uint i) index = i;

                    string adapterName = string.Empty;
                    string adapterType = string.Empty;
                    if (adapterNames.TryGetValue(index, out string? namePair))
                    {
                        var parts = namePair.Split('|', 2);
                        adapterName = parts[0];
                        adapterType = parts.Length > 1 ? parts[1] : string.Empty;
                    }

                    if (string.IsNullOrEmpty(adapterName))
                        adapterName = cfg.CimInstanceProperties["Description"]?.Value?.ToString() ?? "Bilinmiyor";

                    var ipv4List = new List<string>();
                    var ipv6List = new List<string>();

                    if (cfg.CimInstanceProperties["IPAddress"]?.Value is string[] ips)
                    {
                        foreach (var ip in ips)
                        {
                            if (ip.Contains(':')) ipv6List.Add(ip);
                            else ipv4List.Add(ip);
                        }
                    }

                    string gateway = string.Empty;
                    if (cfg.CimInstanceProperties["DefaultIPGateway"]?.Value is string[] gws && gws.Length > 0)
                        gateway = gws[0];

                    var dnsList = new List<string>();
                    if (cfg.CimInstanceProperties["DNSServerSearchOrder"]?.Value is string[] dns)
                        dnsList.AddRange(dns);

                    bool dhcp = cfg.CimInstanceProperties["DHCPEnabled"]?.Value is bool b && b;
                    string dhcpServer = cfg.CimInstanceProperties["DHCPServer"]?.Value?.ToString() ?? string.Empty;

                    string mac = cfg.CimInstanceProperties["MACAddress"]?.Value?.ToString() ?? string.Empty;

                    result.Add(new NetworkAdapterInfo
                    {
                        Name = adapterName,
                        MacAddress = mac,
                        IpAddresses = ipv4List,
                        IPv6Addresses = ipv6List,
                        DefaultGateway = gateway,
                        DhcpServer = dhcpServer,
                        DnsServers = dnsList,
                        DhcpEnabled = dhcp,
                        AdapterType = adapterType
                    });
                }
            }
            catch { /* return empty */ }

            return result;
        }
    }
}
