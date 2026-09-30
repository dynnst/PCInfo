namespace PCNetworkInspector.Models
{
    public class AppSettings
    {
        public int MaxConcurrentConnections { get; set; } = 10;
        public int ConnectionTimeoutSeconds { get; set; } = 30;
        public string DefaultExportPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }
}
