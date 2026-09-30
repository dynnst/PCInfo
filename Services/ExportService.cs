using ClosedXML.Excel;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using PCNetworkInspector.Models;
using System.IO;
using System.Text;

namespace PCNetworkInspector.Services
{
    public class ExportService : IExportService
    {
        public async Task ExportToExcelAsync(IEnumerable<ComputerInfo> computers, string filePath)
        {
            await Task.Run(() =>
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("PC Bilgileri");

                // Header
                string[] headers = {
                    "Durum", "IP/Ad", "Bilgisayar Adı", "Üretici", "Model",
                    "Seri No", "İşlemci", "RAM (GB)", "İşletim Sistemi", "Domain"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(1, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2196F3");
                    cell.Style.Font.FontColor = XLColor.White;
                }

                int row = 2;
                foreach (var pc in computers)
                {
                    ws.Cell(row, 1).Value = pc.IsOnline ? "Online" : "Offline";
                    ws.Cell(row, 2).Value = pc.Target;
                    ws.Cell(row, 3).Value = pc.ComputerName;
                    ws.Cell(row, 4).Value = pc.Manufacturer;
                    ws.Cell(row, 5).Value = pc.Model;
                    ws.Cell(row, 6).Value = pc.SerialNumber;
                    ws.Cell(row, 7).Value = pc.Cpu?.Name ?? string.Empty;
                    ws.Cell(row, 8).Value = pc.Memory?.TotalGb.ToString("F1") ?? string.Empty;
                    ws.Cell(row, 9).Value = pc.Windows?.Caption ?? string.Empty;
                    ws.Cell(row, 10).Value = pc.Domain;

                    if (!pc.IsOnline)
                        ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEBEE");

                    row++;
                }

                ws.Columns().AdjustToContents();
                wb.SaveAs(filePath);
            });
        }

        public async Task ExportToCsvAsync(IEnumerable<ComputerInfo> computers, string filePath)
        {
            await Task.Run(() =>
            {
                var sb = new StringBuilder();
                sb.AppendLine("Durum,IP/Ad,Bilgisayar Adı,Üretici,Model,Seri No,İşlemci,RAM (GB),İşletim Sistemi,Domain");

                foreach (var pc in computers)
                {
                    sb.AppendLine(string.Join(",",
                        pc.IsOnline ? "Online" : "Offline",
                        CsvEscape(pc.Target),
                        CsvEscape(pc.ComputerName),
                        CsvEscape(pc.Manufacturer),
                        CsvEscape(pc.Model),
                        CsvEscape(pc.SerialNumber),
                        CsvEscape(pc.Cpu?.Name ?? string.Empty),
                        pc.Memory?.TotalGb.ToString("F1") ?? string.Empty,
                        CsvEscape(pc.Windows?.Caption ?? string.Empty),
                        CsvEscape(pc.Domain)
                    ));
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            });
        }

        public async Task ExportToPdfAsync(IEnumerable<ComputerInfo> computers, string filePath)
        {
            await Task.Run(() =>
            {
                using var writer = new PdfWriter(filePath);
                using var pdf = new PdfDocument(writer);
                using var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

                doc.Add(new Paragraph("PC Network Inspector - Rapor")
                    .SetFontSize(16)
                    .SetBold()
                    .SetMarginBottom(10));

                doc.Add(new Paragraph($"Oluşturma Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}")
                    .SetFontSize(10)
                    .SetMarginBottom(20));

                var table = new Table(UnitValue.CreatePercentArray(new float[] { 2, 3, 3, 3, 3, 3, 4, 2, 4, 3 }))
                    .UseAllAvailableWidth();

                string[] headers = { "Durum", "IP/Ad", "Bilgisayar", "Üretici", "Model", "Seri No", "CPU", "RAM", "OS", "Domain" };
                foreach (var h in headers)
                {
                    table.AddHeaderCell(new Cell()
                        .Add(new Paragraph(h).SetBold().SetFontSize(8))
                        .SetBackgroundColor(new DeviceRgb(33, 150, 243))
                        .SetFontColor(ColorConstants.WHITE));
                }

                foreach (var pc in computers)
                {
                    var bg = pc.IsOnline ? ColorConstants.WHITE : new DeviceRgb(255, 235, 238);
                    AddCell(table, pc.IsOnline ? "✓" : "✕", bg, 8);
                    AddCell(table, pc.Target, bg, 8);
                    AddCell(table, pc.ComputerName, bg, 8);
                    AddCell(table, pc.Manufacturer, bg, 8);
                    AddCell(table, pc.Model, bg, 8);
                    AddCell(table, pc.SerialNumber, bg, 8);
                    AddCell(table, pc.Cpu?.Name ?? "-", bg, 7);
                    AddCell(table, pc.Memory != null ? $"{pc.Memory.TotalGb:F1} GB" : "-", bg, 8);
                    AddCell(table, pc.Windows?.Caption ?? "-", bg, 7);
                    AddCell(table, pc.Domain, bg, 8);
                }

                doc.Add(table);
            });
        }

        public async Task ExportSingleToExcelAsync(ComputerInfo pc, string filePath)
        {
            await Task.Run(() =>
            {
                using var wb = new XLWorkbook();

                // Sheet 1: Genel
                var ws = wb.Worksheets.Add("Genel Bilgiler");
                AddSection(ws, "SİSTEM BİLGİLERİ", 1);
                AddRow(ws, "Bilgisayar Adı", pc.ComputerName, 3);
                AddRow(ws, "Üretici", pc.Manufacturer, 4);
                AddRow(ws, "Model", pc.Model, 5);
                AddRow(ws, "Seri No", pc.SerialNumber, 6);
                AddRow(ws, "Ürün Adı", pc.ProductName, 7);
                AddRow(ws, "Sistem Tipi", pc.SystemType, 8);
                AddRow(ws, "Domain", pc.Domain, 9);
                AddRow(ws, "Aktif Kullanıcı", pc.CurrentUser, 10);
                AddRow(ws, "Son Kullanıcı", pc.LastUser, 11);

                if (pc.Cpu != null)
                {
                    AddSection(ws, "İŞLEMCİ", 13);
                    AddRow(ws, "Model", pc.Cpu.Name, 14);
                    AddRow(ws, "Çekirdek", pc.Cpu.Cores.ToString(), 15);
                    AddRow(ws, "Thread", pc.Cpu.Threads.ToString(), 16);
                    AddRow(ws, "Maks Hız", pc.Cpu.MaxSpeedGhz, 17);
                }

                if (pc.Memory != null)
                {
                    AddSection(ws, "BELLEK", 19);
                    AddRow(ws, "Toplam RAM", $"{pc.Memory.TotalGb:F1} GB", 20);
                    int mr = 21;
                    foreach (var slot in pc.Memory.Slots)
                    {
                        AddRow(ws, slot.Tag, $"{slot.Manufacturer} {slot.CapacityGb:F1} GB {slot.SpeedMhz} MHz", mr++);
                    }
                }

                ws.Columns().AdjustToContents();
                wb.SaveAs(filePath);
            });
        }

        public async Task ExportSingleToPdfAsync(ComputerInfo pc, string filePath)
        {
            await Task.Run(() =>
            {
                using var writer = new PdfWriter(filePath);
                using var pdf = new PdfDocument(writer);
                using var doc = new Document(pdf);

                doc.Add(new Paragraph($"PC Network Inspector")
                    .SetFontSize(18).SetBold().SetMarginBottom(5));
                doc.Add(new Paragraph($"Bilgisayar: {pc.Target}  |  Sorgu Zamanı: {pc.QueryTime:dd.MM.yyyy HH:mm}")
                    .SetFontSize(10).SetMarginBottom(20));

                AddPdfSection(doc, "SİSTEM BİLGİLERİ");
                AddPdfRow(doc, "Bilgisayar Adı", pc.ComputerName);
                AddPdfRow(doc, "Üretici", pc.Manufacturer);
                AddPdfRow(doc, "Model", pc.Model);
                AddPdfRow(doc, "Seri No", pc.SerialNumber);
                AddPdfRow(doc, "Sistem Tipi", pc.SystemType);
                AddPdfRow(doc, "Domain", pc.Domain);
                AddPdfRow(doc, "Aktif Kullanıcı", pc.CurrentUser);

                if (pc.Cpu != null)
                {
                    AddPdfSection(doc, "İŞLEMCİ");
                    AddPdfRow(doc, "Model", pc.Cpu.Name);
                    AddPdfRow(doc, "Çekirdek / Thread", $"{pc.Cpu.Cores} / {pc.Cpu.Threads}");
                    AddPdfRow(doc, "Maks Hız", pc.Cpu.MaxSpeedGhz);
                }

                if (pc.Memory != null)
                {
                    AddPdfSection(doc, "BELLEK");
                    AddPdfRow(doc, "Toplam RAM", $"{pc.Memory.TotalGb:F1} GB");
                    foreach (var s in pc.Memory.Slots)
                        AddPdfRow(doc, s.Tag, $"{s.Manufacturer} {s.CapacityGb:F1} GB @ {s.SpeedMhz} MHz");
                }

                if (pc.Disks.Any())
                {
                    AddPdfSection(doc, "DİSKLER");
                    foreach (var d in pc.Disks)
                    {
                        AddPdfRow(doc, "Disk", $"{d.Model} ({d.MediaType}) {d.TotalSizeGb:F1} GB");
                        foreach (var ld in d.LogicalDisks)
                            AddPdfRow(doc, $"  {ld.DriveLetter}", $"Toplam: {ld.TotalSizeGb:F1} GB  Boş: {ld.FreeSpaceGb:F1} GB");
                    }
                }

                if (pc.Windows != null)
                {
                    AddPdfSection(doc, "İŞLETİM SİSTEMİ");
                    AddPdfRow(doc, "Sürüm", pc.Windows.Caption);
                    AddPdfRow(doc, "Build", pc.Windows.BuildNumber);
                    AddPdfRow(doc, "Mimari", pc.Windows.Architecture);
                    AddPdfRow(doc, "Son Açılış", pc.Windows.LastBootTime);
                }

                foreach (var net in pc.NetworkAdapters)
                {
                    AddPdfSection(doc, $"AĞ - {net.Name}");
                    AddPdfRow(doc, "MAC", net.MacAddress);
                    AddPdfRow(doc, "IP", string.Join(", ", net.IpAddresses));
                    AddPdfRow(doc, "Gateway", net.DefaultGateway);
                    AddPdfRow(doc, "DNS", string.Join(", ", net.DnsServers));
                }
            });
        }

        // Helpers
        private static void AddSection(IXLWorksheet ws, string title, int row)
        {
            var cell = ws.Cell(row, 1);
            cell.Value = title;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
            cell.Style.Font.FontColor = XLColor.White;
            ws.Range(row, 1, row, 2).Merge();
        }

        private static void AddRow(IXLWorksheet ws, string label, string value, int row)
        {
            ws.Cell(row, 1).Value = label;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 2).Value = value;
        }

        private static void AddPdfSection(Document doc, string title)
        {
            doc.Add(new Paragraph(title)
                .SetFontSize(12)
                .SetBold()
                .SetBackgroundColor(new DeviceRgb(21, 101, 192))
                .SetFontColor(ColorConstants.WHITE)
                .SetMarginTop(10)
                .SetPaddingLeft(5));
        }

        private static void AddPdfRow(Document doc, string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 3, 7 })).UseAllAvailableWidth();
            table.AddCell(new Cell().Add(new Paragraph(label).SetFontSize(9).SetBold())
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            table.AddCell(new Cell().Add(new Paragraph(value).SetFontSize(9))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            doc.Add(table);
        }

        private static void AddCell(Table table, string text, iText.Kernel.Colors.Color bg, int fontSize)
        {
            table.AddCell(new Cell()
                .Add(new Paragraph(text).SetFontSize(fontSize))
                .SetBackgroundColor(bg));
        }

        private static string CsvEscape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}
