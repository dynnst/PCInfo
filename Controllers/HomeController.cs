using Microsoft.AspNetCore.Mvc;
using PCNetworkInspector.Services;

namespace PCNetworkInspector.Controllers
{
    public class HomeController : Controller
    {
        private readonly IInventoryService _inventoryService;

        public HomeController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        public async Task<IActionResult> Index()
        {
            var stats = await _inventoryService.GetDashboardStatsAsync();
            return View(stats);
        }
    }
}
