using Microsoft.AspNetCore.Mvc;
using PCNetworkInspector.Models;
using PCNetworkInspector.Services;

namespace PCNetworkInspector.Controllers
{
    public class CameraRequest
    {
        public string IpAddress { get; set; } = string.Empty;
        public string Username { get; set; } = "admin";
        public string Password { get; set; } = string.Empty;
        public int Port { get; set; } = 80;
    }

    public class CameraController : Controller
    {
        private readonly IHikvisionService _hikvisionService;
        private readonly IInventoryService _inventoryService;

        public CameraController(
            IHikvisionService hikvisionService,
            IInventoryService inventoryService)
        {
            _hikvisionService = hikvisionService;
            _inventoryService = inventoryService;
        }

        public IActionResult Index(string? ip)
        {
            ViewBag.TargetIp = ip ?? string.Empty;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetInfo([FromBody] CameraRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.IpAddress))
                return Json(new { success = false, message = "IP adresi girilmedi." });

            var info = await _hikvisionService.GetCameraInfoAsync(
                req.IpAddress.Trim(),
                req.Username,
                req.Password,
                req.Port);

            if (info.IsOnline)
            {
                await _inventoryService.UpdateCameraInfoAsync(req.IpAddress.Trim(), info);
            }

            return Json(new { success = info.IsOnline, data = info, error = info.ErrorMessage });
        }

        [HttpGet]
        public async Task<IActionResult> Snapshot(string ip, string username = "admin", string password = "", int channel = 101, int port = 80)
        {
            if (string.IsNullOrWhiteSpace(ip)) return NotFound();

            var bytes = await _hikvisionService.GetSnapshotAsync(ip, username, password, channel, port);
            if (bytes == null || bytes.Length == 0)
            {
                // Return a placeholder transparent image or not found
                return NotFound("Kamera görüntüsü alınamadı");
            }

            return File(bytes, "image/jpeg");
        }
    }
}
