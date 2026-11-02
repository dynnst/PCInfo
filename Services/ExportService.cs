using ClosedXML.Excel;
using iText.IO.Font;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using PCNetworkInspector.Data.Entities;
using PCNetworkInspector.Models;

namespace PCNetworkInspector.Services
{
    public class ExportService : IExportService
    {
        private static PdfFont GetTurkishFont()
        {
            string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
            if (File.Exists(fontPath))
            {
                return PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H);
            }
            return PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);
        }

        public async Task<byte[]> ExportInventoryToExcelAsync(IEnumerable<DeviceEntity> devices)
        {
            return await Task.Run(() =>
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Şube Envanteri");

                string[] headers = {
                    "Demirbaş No", "Kategori", "Marka", "Model", "Seri Numarası",
                    "Mevki / Kurulduğu Yer", "Oda No", "Zimmetli Personel", "Durum",
                    "Tedarik Tarihi", "Teslim Tarihi", "IP Adresi", "Açıklama / Notlar"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = ws.Cell(1, i + 1);
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                    cell.Style.Font.FontColor = XLColor.White;
                }

                int row = 2;
                foreach (var d in devices)
                {
                    ws.Cell(row, 1).Value = d.AssetTag ?? "";
                    ws.Cell(row, 2).Value = d.Category.GetCategoryName();
                    ws.Cell(row, 3).Value = d.Brand;
                    ws.Cell(row, 4).Value = d.Model;
                    ws.Cell(row, 5).Value = d.SerialNumber;
                    ws.Cell(row, 6).Value = d.Location;
                    ws.Cell(row, 7).Value = d.RoomNumber ?? "";
                    ws.Cell(row, 8).Value = d.AssignedPerson ?? "";
                    ws.Cell(row, 9).Value = d.Status.GetStatusName();
                    ws.Cell(row, 10).Value = d.ProcurementDate?.ToString("dd.MM.yyyy") ?? "";
                    ws.Cell(row, 11).Value = d.DeliveryDate?.ToString("dd.MM.yyyy") ?? "";
                    ws.Cell(row, 12).Value = d.IpAddress ?? "";
                    ws.Cell(row, 13).Value = d.Notes ?? "";

                    if (d.Status == AssetStatus.InRepair)
                    {
                        ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
                    }
                    else if (d.Status == AssetStatus.Scrapped)
                    {
                        ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEE2E2");
                    }

                    row++;
                }

                ws.Columns().AdjustToContents();

                using var ms = new MemoryStream();
                wb.SaveAs(ms);
                return ms.ToArray();
            });
        }

        public async Task<byte[]> ExportInventoryToPdfAsync(IEnumerable<DeviceEntity> devices)
        {
            return await Task.Run(() =>
            {
                using var ms = new MemoryStream();
                using var writer = new PdfWriter(ms);
                using var pdf = new PdfDocument(writer);
                using var doc = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());

                var font = GetTurkishFont();
                doc.SetFont(font);

                doc.Add(new Paragraph("Şube Müdürlüğü Bilgi İşlem ve Donanım Envanter Raporu")
                    .SetFontSize(16).SetBold().SetFontColor(new DeviceRgb(30, 58, 138)));

                doc.Add(new Paragraph($"Rapor Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm:ss} | Toplam Donanım: {devices.Count()}")
                    .SetFontSize(9).SetFontColor(ColorConstants.GRAY).SetMarginBottom(12));

                var table = new Table(UnitValue.CreatePercentArray(new float[] { 2.5f, 3f, 3f, 3.5f, 4f, 2f, 3.5f, 2.5f, 3f })).UseAllAvailableWidth();

                string[] headers = { "Demirbaş No", "Kategori", "Marka / Model", "Seri Numarası", "Mevki / Konum", "Oda", "Zimmetli Personel", "Durum", "IP Adresi" };
                foreach (var h in headers)
                {
                    table.AddHeaderCell(new Cell()
                        .Add(new Paragraph(h).SetBold().SetFontSize(8))
                        .SetBackgroundColor(new DeviceRgb(30, 58, 138))
                        .SetFontColor(ColorConstants.WHITE));
                }

                foreach (var d in devices)
                {
                    table.AddCell(new Cell().Add(new Paragraph(d.AssetTag ?? "—").SetFontSize(7.5f)));
                    table.AddCell(new Cell().Add(new Paragraph(d.Category.GetCategoryName()).SetFontSize(7.5f)));
                    table.AddCell(new Cell().Add(new Paragraph($"{d.Brand} {d.Model}").SetFontSize(7.5f)));
                    table.AddCell(new Cell().Add(new Paragraph(d.SerialNumber).SetFontSize(7.5f)));
                    table.AddCell(new Cell().Add(new Paragraph(d.Location).SetFontSize(7.5f)));
                    table.AddCell(new Cell().Add(new Paragraph(d.RoomNumber ?? "—").SetFontSize(7.5f)));
                    table.AddCell(new Cell().Add(new Paragraph(d.AssignedPerson ?? "—").SetFontSize(7.5f)));
                    table.AddCell(new Cell().Add(new Paragraph(d.Status.GetStatusName()).SetFontSize(7.5f)));
                    table.AddCell(new Cell().Add(new Paragraph(d.IpAddress ?? "—").SetFontSize(7.5f)));
                }

                doc.Add(table);
                doc.Close();
                return ms.ToArray();
            });
        }

        public async Task<byte[]> ExportSinglePcToExcelAsync(ComputerInfo pc)
        {
            return await Task.Run(() =>
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Bilgisayar Detay");

                AddSection(ws, "SİSTEM BİLGİLERİ", 1);
                AddRow(ws, "Bilgisayar Adı", pc.ComputerName, 3);
                AddRow(ws, "Üretici", pc.Manufacturer, 4);
                AddRow(ws, "Model", pc.Model, 5);
                AddRow(ws, "Seri No", pc.SerialNumber, 6);
                AddRow(ws, "Sistem Tipi", pc.SystemType, 7);
                AddRow(ws, "Domain", pc.Domain, 8);
                AddRow(ws, "Aktif Kullanıcı", pc.CurrentUser, 9);
                AddRow(ws, "Son Kullanıcı", pc.LastUser, 10);

                int r = 12;
                if (pc.Cpu != null)
                {
                    AddSection(ws, "İŞLEMCİ", r++);
                    AddRow(ws, "Model", pc.Cpu.Name, ++r);
                    AddRow(ws, "Çekirdek / Thread", $"{pc.Cpu.Cores} / {pc.Cpu.Threads}", ++r);
                    AddRow(ws, "Maks Hız", pc.Cpu.MaxSpeedGhz, ++r);
                    r += 2;
                }

                if (pc.Memory != null)
                {
                    AddSection(ws, "BELLEK", r++);
                    AddRow(ws, "Toplam RAM", $"{pc.Memory.TotalGb:F1} GB", ++r);
                    foreach (var slot in pc.Memory.Slots)
                    {
                        AddRow(ws, slot.Tag, $"{slot.Manufacturer} {slot.CapacityGb:F1} GB {slot.SpeedMhz} MHz ({slot.PartNumber})", ++r);
                    }
                    r += 2;
                }

                if (pc.Monitors.Count > 0)
                {
                    AddSection(ws, "BAĞLI MONİTÖRLER", r++);
                    foreach (var m in pc.Monitors)
                    {
                        AddRow(ws, m.Model, $"{m.Manufacturer} | Seri No: {m.SerialNumber} | Üretim: {m.YearOfManufacture}", ++r);
                    }
                    r += 2;
                }

                ws.Columns().AdjustToContents();

                using var ms = new MemoryStream();
                wb.SaveAs(ms);
                return ms.ToArray();
            });
        }

        public async Task<byte[]> ExportSinglePcToPdfAsync(ComputerInfo pc)
        {
            return await Task.Run(() =>
            {
                using var ms = new MemoryStream();
                using var writer = new PdfWriter(ms);
                using var pdf = new PdfDocument(writer);
                using var doc = new Document(pdf);

                var font = GetTurkishFont();
                doc.SetFont(font);

                doc.Add(new Paragraph($"Bilgisayar Donanım Raporu: {pc.ComputerName}")
                    .SetFontSize(16).SetBold().SetFontColor(new DeviceRgb(30, 58, 138)).SetMarginBottom(4));

                doc.Add(new Paragraph($"Hedef: {pc.Target} | Rapor Zamanı: {pc.QueryTime:dd.MM.yyyy HH:mm}")
                    .SetFontSize(9).SetFontColor(ColorConstants.GRAY).SetMarginBottom(16));

                AddPdfSection(doc, "GENEL SİSTEM BİLGİLERİ");
                AddPdfRow(doc, "Bilgisayar Adı", pc.ComputerName);
                AddPdfRow(doc, "Üretici", pc.Manufacturer);
                AddPdfRow(doc, "Model", pc.Model);
                AddPdfRow(doc, "Seri No", pc.SerialNumber);
                AddPdfRow(doc, "Domain", pc.Domain);
                AddPdfRow(doc, "Kullanıcı", pc.CurrentUser);

                if (pc.Cpu != null)
                {
                    AddPdfSection(doc, "İŞLEMCİ (CPU)");
                    AddPdfRow(doc, "Model", pc.Cpu.Name);
                    AddPdfRow(doc, "Çekirdek / Thread", $"{pc.Cpu.Cores} / {pc.Cpu.Threads}");
                    AddPdfRow(doc, "Maksimum Hız", pc.Cpu.MaxSpeedGhz);
                }

                if (pc.Memory != null)
                {
                    AddPdfSection(doc, "BELLEK (RAM)");
                    AddPdfRow(doc, "Toplam RAM", $"{pc.Memory.TotalGb:F1} GB");
                    foreach (var s in pc.Memory.Slots)
                        AddPdfRow(doc, s.Tag, $"{s.Manufacturer} {s.CapacityGb:F1} GB @ {s.SpeedMhz} MHz");
                }

                if (pc.Monitors.Count > 0)
                {
                    AddPdfSection(doc, "BAĞLI MONİTÖRLER");
                    foreach (var m in pc.Monitors)
                        AddPdfRow(doc, m.Model, $"{m.Manufacturer} | Seri No: {m.SerialNumber} | Yıl: {m.YearOfManufacture}");
                }

                doc.Close();
                return ms.ToArray();
            });
        }

        private static void AddSection(IXLWorksheet ws, string title, int row)
        {
            var cell = ws.Cell(row, 1);
            cell.Value = title;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
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
                .SetFontSize(11)
                .SetBold()
                .SetBackgroundColor(new DeviceRgb(30, 58, 138))
                .SetFontColor(ColorConstants.WHITE)
                .SetMarginTop(10)
                .SetPadding(4));
        }

        private static void AddPdfRow(Document doc, string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 3, 7 })).UseAllAvailableWidth();
            table.AddCell(new Cell().Add(new Paragraph(label).SetFontSize(8.5f).SetBold()).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            table.AddCell(new Cell().Add(new Paragraph(value).SetFontSize(8.5f)).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
            doc.Add(table);
        }
    }
}
