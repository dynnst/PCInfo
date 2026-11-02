using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PCNetworkInspector.Data.Entities;
using PCNetworkInspector.Models;
using PCNetworkInspector.Services;

namespace PCNetworkInspector.Controllers
{
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly IExportService _exportService;

        public InventoryController(IInventoryService inventoryService, IExportService exportService)
        {
            _inventoryService = inventoryService;
            _exportService = exportService;
        }

        public async Task<IActionResult> Index(string? search, AssetCategory? category, AssetStatus? status, string? location)
        {
            ViewBag.Search = search;
            ViewBag.SelectedCategory = category;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedLocation = location;
            ViewBag.Locations = await _inventoryService.GetDistinctLocationsAsync();

            var devices = await _inventoryService.GetAllDevicesAsync(search, category, status, location);
            return View(devices);
        }

        [HttpGet]
        public async Task<IActionResult> GetDetails(int id)
        {
            var device = await _inventoryService.GetDeviceByIdAsync(id);
            if (device == null) return NotFound();
            return Json(device);
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromForm] DeviceEntity device, IFormFile? imageFile)
        {
            if (device == null) return BadRequest("Geçersiz veri");

            await _inventoryService.SaveDeviceAsync(device, imageFile);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _inventoryService.DeleteDeviceAsync(id);
            return Json(new { success });
        }

        [HttpGet]
        public IActionResult DownloadRdp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return BadRequest();

            string rdpContent = $"full address:s:{ip}\nprompt for credentials:i:1\nadministrative session:i:1\n";
            var bytes = Encoding.UTF8.GetBytes(rdpContent);
            return File(bytes, "application/x-rdp", $"{ip}.rdp");
        }

        public async Task<IActionResult> ExportExcel(string? search, AssetCategory? category, AssetStatus? status, string? location)
        {
            var devices = await _inventoryService.GetAllDevicesAsync(search, category, status, location);
            var fileBytes = await _exportService.ExportInventoryToExcelAsync(devices);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Sube_Envanter_Raporu_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        public async Task<IActionResult> ExportPdf(string? search, AssetCategory? category, AssetStatus? status, string? location)
        {
            var devices = await _inventoryService.GetAllDevicesAsync(search, category, status, location);
            var fileBytes = await _exportService.ExportInventoryToPdfAsync(devices);
            return File(fileBytes, "application/pdf", $"Sube_Envanter_Raporu_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }
    }
}
