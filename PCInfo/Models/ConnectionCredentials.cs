using System.Security;

namespace PCNetworkInspector.Models
{
    public class ConnectionCredentials
    {
        public bool UseCurrentUser { get; set; } = true;
        public string? Username { get; set; }
        public string? Domain { get; set; }
        public SecureString? Password { get; set; }

        public bool HasCredentials =>
            !UseCurrentUser &&
            !string.IsNullOrWhiteSpace(Username) &&
            Password != null && Password.Length > 0;
    }
}
