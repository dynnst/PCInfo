namespace PCNetworkInspector.Models
{
    public class ConnectionTestResult
    {
        public bool NetworkReachable { get; set; }
        public bool WmiAccessible { get; set; }
        public bool AuthorizationSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> Suggestions { get; set; } = new();

        public bool IsFullySuccessful =>
            NetworkReachable && WmiAccessible && AuthorizationSuccess;
    }
}
