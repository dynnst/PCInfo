using Microsoft.AspNetCore.Mvc;
using PCNetworkInspector.Models;
using PCNetworkInspector.Services;

namespace PCNetworkInspector.Controllers
{
    public class ScannerController : Controller
    {
        private readonly ISmartScannerService _scannerService;
        private readonly IInventoryService _inventoryService;

        public ScannerController(
            ISmartScannerService scannerService,
            IInventoryService inventoryService)
        {
            _scannerService = scannerService;
            _inventoryService = inventoryService;
        }

        public IActionResult Index()
        {
            ViewBag.DefaultRange = "10.7.202.1 - 10.7.202.500";
            return View();
        }

        [HttpPost]
        public IActionResult StartScan(string? rangeInput, int concurrency = 30, int timeoutMs = 1200)
        {
            if (string.IsNullOrWhiteSpace(rangeInput))
                rangeInput = "10.7.202.1 - 10.7.202.500";

            // Fire and forget scan in background task; SignalR delivers real-time updates!
            _ = Task.Run(async () =>
            {
                try
                {
                    await _scannerService.StartScanAsync(rangeInput, concurrency, timeoutMs);
                }
                catch { }
            });

            return Json(new { success = true, message = "Tarama başlatıldı" });
        }

        [HttpPost]
        public IActionResult StopScan()
        {
            _scannerService.StopScan();
            return Json(new { success = true, message = "Tarama durduruldu" });
        }

        [HttpPost]
        public async Task<IActionResult> SaveToInventory([FromBody] List<ScanItemResult> items)
        {
            if (items == null || items.Count == 0)
                return Json(new { success = false, message = "Kaydedilecek cihaz seçilmedi" });

            int count = await _inventoryService.AddFromScanResultsAsync(items);
            return Json(new { success = true, count, message = $"{count} adet cihaz envantere başarıyla eklendi." });
        }
    }
}
