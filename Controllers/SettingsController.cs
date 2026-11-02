using Microsoft.AspNetCore.Mvc;
using PCNetworkInspector.Models;

namespace PCNetworkInspector.Controllers
{
    public class SettingsController : Controller
    {
        private readonly IConfiguration _config;

        public SettingsController(IConfiguration config)
        {
            _config = config;
        }

        public IActionResult Index()
        {
            var settings = new AppSettings
            {
                MaxConcurrentConnections = _config.GetValue<int>("NetworkScanner:MaxConcurrency", 30),
                ConnectionTimeoutSeconds = _config.GetValue<int>("NetworkScanner:TimeoutSeconds", 30),
                DefaultExportPath = _config.GetConnectionString("DefaultConnection") ?? "Server=localhost;Database=PCNetworkInventory;Trusted_Connection=True;TrustServerCertificate=True;"
            };
            return View(settings);
        }
    }
}
