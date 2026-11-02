using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public interface IHikvisionService
    {
        Task<CameraDeviceInfo> GetCameraInfoAsync(
            string ip,
            string username = "admin",
            string password = "",
            int port = 80,
            CancellationToken cancellationToken = default);

        Task<byte[]?> GetSnapshotAsync(
            string ip,
            string username = "admin",
            string password = "",
            int channel = 101,
            int port = 80,
            CancellationToken cancellationToken = default);

        Task<bool> IsCameraReachableAsync(string ip, int port = 80, int timeoutMs = 1500);
    }
}
