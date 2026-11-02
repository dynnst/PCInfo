using System.Security;
using Microsoft.AspNetCore.Mvc;
using PCNetworkInspector.Models;
using PCNetworkInspector.Services;

namespace PCNetworkInspector.Controllers
{
    public class InspectRequest
    {
        public string Target { get; set; } = string.Empty;
        public bool UseCurrentUser { get; set; } = true;
        public string? Username { get; set; }
        public string? Domain { get; set; }
        public string? Password { get; set; }
    }

    public class InspectorController : Controller
    {
        private readonly IComputerInfoService _computerInfoService;
        private readonly ISoftwareInfoService _softwareInfoService;
        private readonly IInventoryService _inventoryService;
        private readonly IExportService _exportService;

        public InspectorController(
            IComputerInfoService computerInfoService,
            ISoftwareInfoService softwareInfoService,
            IInventoryService inventoryService,
            IExportService exportService)
        {
            _computerInfoService = computerInfoService;
            _softwareInfoService = softwareInfoService;
            _inventoryService = inventoryService;
            _exportService = exportService;
        }

        public IActionResult Index(string? ip)
        {
            ViewBag.TargetIp = ip ?? string.Empty;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Query([FromBody] InspectRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Target))
                return Json(new { success = false, message = "Lütfen bir IP veya bilgisayar adı girin." });

            var creds = BuildCredentials(req);

            try
            {
                var info = await _computerInfoService.GetComputerInfoAsync(req.Target.Trim(), creds);
                if (info.IsOnline)
                {
                    // Auto-update inventory with found specs
                    await _inventoryService.UpdateDeviceInfoFromWmiAsync(req.Target.Trim(), info);
                    return Json(new { success = true, data = info });
                }
                else
                {
                    return Json(new { success = false, message = info.ErrorMessage ?? "Bilgisayara bağlanılamadı." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSoftware([FromBody] InspectRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Target))
                return Json(new { success = false, message = "Hedef bilgisayar belirtilmedi." });

            var creds = BuildCredentials(req);

            try
            {
                var software = await _softwareInfoService.GetInstalledSoftwareAsync(req.Target.Trim(), creds);
                return Json(new { success = true, data = software });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(string target, string? user, string? domain, string? pass)
        {
            var req = new InspectRequest
            {
                Target = target,
                UseCurrentUser = string.IsNullOrWhiteSpace(user),
                Username = user,
                Domain = domain,
                Password = pass
            };

            var creds = BuildCredentials(req);
            var info = await _computerInfoService.GetComputerInfoAsync(target, creds);
            var bytes = await _exportService.ExportSinglePcToExcelAsync(info);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{info.ComputerName}_rapor.xlsx");
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(string target, string? user, string? domain, string? pass)
        {
            var req = new InspectRequest
            {
                Target = target,
                UseCurrentUser = string.IsNullOrWhiteSpace(user),
                Username = user,
                Domain = domain,
                Password = pass
            };

            var creds = BuildCredentials(req);
            var info = await _computerInfoService.GetComputerInfoAsync(target, creds);
            var bytes = await _exportService.ExportSinglePcToPdfAsync(info);
            return File(bytes, "application/pdf", $"{info.ComputerName}_rapor.pdf");
        }

        private static ConnectionCredentials BuildCredentials(InspectRequest req)
        {
            SecureString? secPass = null;
            if (!string.IsNullOrEmpty(req.Password))
            {
                secPass = new SecureString();
                foreach (char c in req.Password) secPass.AppendChar(c);
                secPass.MakeReadOnly();
            }

            return new ConnectionCredentials
            {
                UseCurrentUser = req.UseCurrentUser,
                Username = req.UseCurrentUser ? null : req.Username,
                Domain = req.UseCurrentUser ? null : req.Domain,
                Password = req.UseCurrentUser ? null : secPass
            };
        }
    }
}
