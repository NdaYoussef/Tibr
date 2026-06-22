using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos.DashboardDtos;
using Tibr.Application.Services.AdminServices;
using Tibr.MVC.Models;

namespace Tibr.MVC.Controllers
{
    public class ExportController : Controller
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly ILogger<ExportController> _logger;

        public ExportController(
            IAnalyticsService analyticsService,
            ILogger<ExportController> logger)
        {
            _analyticsService = analyticsService;
            _logger = logger;
        }

        // ── GET /Export/Excel?reportType=sales&from=...&to=... ───────
        [HttpGet]
        public async Task<IActionResult> Excel(
            string reportType = "sales",
            DateTime? from = null,
            DateTime? to = null)
        {
            var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
            var toDate = to ?? DateTime.UtcNow;

            using var wb = new XLWorkbook();

            // Also add summary sheet on every export
            var summary = await _analyticsService.GetReportsSummaryAsync(fromDate, toDate);
            AddSummarySheet(wb, summary, fromDate, toDate);

            switch (reportType.ToLower())
            {
                case "sales":
                case "revenue":
                    await AddSalesSheet(wb, fromDate, toDate);
                    break;

                case "products":
                    await AddProductPerformanceSheet(wb, fromDate, toDate);
                    break;

                case "customers":
                    await AddCustomerSheet(wb);
                    break;

                case "inventory":
                    await AddInventorySheet(wb);
                    break;

                default:
                    await AddSalesSheet(wb, fromDate, toDate);
                    break;
            }

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            var fileName = $"Tibr_{reportType}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // ── GET /Export/Pdf?reportType=sales&from=...&to=... ─────────
        // Returns a printable HTML page — browser print dialog handles PDF.
        [HttpGet]
        public async Task<IActionResult> Pdf(
     string reportType = "sales",
     DateTime? from = null,
     DateTime? to = null)
        {
            var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
            var toDate = to ?? DateTime.UtcNow;

            var summary = await _analyticsService.GetReportsSummaryAsync(fromDate, toDate);

            var tableRows = reportType.ToLower() switch
            {
                "sales" or "revenue" => await BuildSalesRows(fromDate, toDate),
                "products" => await BuildProductRows(fromDate, toDate),
                "customers" => await BuildCustomerRows(),
                "inventory" => await BuildInventoryRows(),
                _ => await BuildSalesRows(fromDate, toDate)
            };

            var vm = new PrintReportViewModel
            {
                ReportType = reportType,
                FromDate = fromDate,
                ToDate = toDate,
                Summary = summary,
                TableRows = tableRows,    // List<List<string>> — generic rows
                TableHeaders = GetHeaders(reportType)
            };

            // No layout — standalone printable page
            return View("PrintReport", vm);
        }

        // EXCEL SHEET BUILDERS
        private static void AddSummarySheet(IXLWorkbook wb, ReportsSummaryDto summary, DateTime from, DateTime to)
        {
            var ws = wb.Worksheets.Add("Summary");

            StyleHeader(ws, "Tibr Report Summary", 1, 2);
            ws.Cell(2, 1).Value = $"Period: {from:dd MMM yyyy} – {to:dd MMM yyyy}";
            ws.Cell(2, 1).Style.Font.Italic = true;

            ws.Cell(4, 1).Value = "Metric";
            ws.Cell(4, 2).Value = "Value";
            StyleTableHeader(ws.Row(4), 2);

            ws.Cell(5, 1).Value = "Total Revenue"; ws.Cell(5, 2).Value = summary.TotalRevenue;
            ws.Cell(6, 1).Value = "Total Orders"; ws.Cell(6, 2).Value = summary.TotalOrders;
            ws.Cell(7, 1).Value = "Total Customers"; ws.Cell(7, 2).Value = summary.TotalCustomers;
            ws.Cell(8, 1).Value = "Total Products"; ws.Cell(8, 2).Value = summary.TotalProducts;

            ws.Cell(5, 2).Style.NumberFormat.Format = "#,##0.00";
            ws.Columns().AdjustToContents();
        }

        private async Task AddSalesSheet(IXLWorkbook wb, DateTime from, DateTime to)
        {
            var rows = await _analyticsService.GetSalesReportAsync(from, to);
            var ws = wb.Worksheets.Add("Sales Report");

            StyleHeader(ws, "Sales Report", 1, 6);

            string[] headers = ["Order Number", "Customer", "Date", "Amount (EGP)", "Order Status", "Payment Status"];
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(3, c + 1).Value = headers[c];
            StyleTableHeader(ws.Row(3), headers.Length);

            int row = 4;
            foreach (var r in rows)
            {
                ws.Cell(row, 1).Value = r.OrderNumber;
                ws.Cell(row, 2).Value = r.CustomerName;
                ws.Cell(row, 3).Value = r.Date.ToString("dd MMM yyyy HH:mm");
                ws.Cell(row, 4).Value = r.Amount;
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 5).Value = r.OrderStatus;
                ws.Cell(row, 6).Value = r.PaymentStatus;
                row++;
            }

            StyleDataRows(ws, 4, row - 1, 6);
            ws.Columns().AdjustToContents();
        }

        private async Task AddProductPerformanceSheet(IXLWorkbook wb, DateTime from, DateTime to)
        {
            var rows = await _analyticsService.GetProductPerformanceReportAsync(from, to);
            var ws = wb.Worksheets.Add("Product Performance");

            StyleHeader(ws, "Product Performance Report", 1, 6);

            string[] headers = ["Product Name", "Metal Type", "Units Sold", "Revenue (EGP)", "Current Stock", "Status"];
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(3, c + 1).Value = headers[c];
            StyleTableHeader(ws.Row(3), headers.Length);

            int row = 4;
            foreach (var r in rows)
            {
                ws.Cell(row, 1).Value = r.ProductName;
                ws.Cell(row, 2).Value = r.MetalType;
                ws.Cell(row, 3).Value = r.TotalSold;
                ws.Cell(row, 4).Value = r.Revenue;
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 5).Value = r.Stock;
                ws.Cell(row, 6).Value = r.Status;
                row++;
            }

            StyleDataRows(ws, 4, row - 1, 6);
            ws.Columns().AdjustToContents();
        }

        private async Task AddInventorySheet(IXLWorkbook wb)
        {
            var rows = await _analyticsService.GetInventoryReportAsync();
            var ws = wb.Worksheets.Add("Inventory");

            StyleHeader(ws, "Inventory Report", 1, 7);

            string[] headers = ["Product Name", "Category", "Metal Type", "Stock (g)", "Stock Status", "Buy Price", "Sell Price"];
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(3, c + 1).Value = headers[c];
            StyleTableHeader(ws.Row(3), headers.Length);

            int row = 4;
            foreach (var r in rows)
            {
                ws.Cell(row, 1).Value = r.ProductName;
                ws.Cell(row, 2).Value = r.Category;
                ws.Cell(row, 3).Value = r.MetalType;
                ws.Cell(row, 4).Value = r.Stock;
                ws.Cell(row, 5).Value = r.StockStatus;
                ws.Cell(row, 6).Value = r.BuyPrice;
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 7).Value = r.SellPrice;
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";

                // Colour-code stock status
                var statusCell = ws.Cell(row, 5);
                statusCell.Style.Font.FontColor = r.StockStatus switch
                {
                    "Out" => XLColor.Red,
                    "Low" => XLColor.Orange,
                    _ => XLColor.DarkGreen
                };
                row++;
            }

            StyleDataRows(ws, 4, row - 1, 7);
            ws.Columns().AdjustToContents();
        }

        private async Task AddCustomerSheet(IXLWorkbook wb)
        {
            var rows = await _analyticsService.GetCustomerReportAsync();
            var ws = wb.Worksheets.Add("Customers");

            StyleHeader(ws, "Customer Report", 1, 6);

            string[] headers = ["Customer Name", "Email", "Total Orders", "Total Spent (EGP)", "Joined", "KYC Status"];
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(3, c + 1).Value = headers[c];
            StyleTableHeader(ws.Row(3), headers.Length);

            int row = 4;
            foreach (var r in rows)
            {
                ws.Cell(row, 1).Value = r.CustomerName;
                ws.Cell(row, 2).Value = r.Email;
                ws.Cell(row, 3).Value = r.TotalOrders;
                ws.Cell(row, 4).Value = r.TotalSpent;
                ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 5).Value = r.JoinedAt.ToString("dd MMM yyyy");
                ws.Cell(row, 6).Value = r.KycStatus;
                row++;
            }

            StyleDataRows(ws, 4, row - 1, 6);
            ws.Columns().AdjustToContents();
        }

        // EXCEL STYLING HELPERS

        private static void StyleHeader(IXLWorksheet ws, string title, int row, int colSpan)
        {
            var range = ws.Range(row, 1, row, colSpan);
            range.Merge();
            range.Value = title;
            range.Style.Font.Bold = true;
            range.Style.Font.FontSize = 14;
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#C5A028");
            range.Style.Font.FontColor = XLColor.White;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        private static void StyleTableHeader(IXLRow row, int colCount)
        {
            for (int c = 1; c <= colCount; c++)
            {
                var cell = row.Cell(c);
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a1a1a");
                cell.Style.Font.FontColor = XLColor.FromHtml("#F2CA50");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Medium;
                cell.Style.Border.BottomBorderColor = XLColor.FromHtml("#C5A028");
            }
        }

        private static void StyleDataRows(IXLWorksheet ws, int startRow, int endRow, int colCount)
        {
            if (startRow > endRow) return;
            for (int r = startRow; r <= endRow; r++)
            {
                for (int c = 1; c <= colCount; c++)
                {
                    var cell = ws.Cell(r, c);
                    cell.Style.Fill.BackgroundColor = r % 2 == 0
                        ? XLColor.FromHtml("#F8F9FB")
                        : XLColor.White;
                    cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.BottomBorderColor = XLColor.FromHtml("#E5E7EB");
                }
            }
        }

        private static List<string> GetHeaders(string reportType) =>
     reportType.ToLower() switch
     {
         "sales" or "revenue" => ["Order #", "Customer", "Date", "Amount", "Status", "Payment"],
         "products" => ["Product", "Metal", "Units Sold", "Revenue", "Stock", "Status"],
         "customers" => ["Name", "Email", "Orders", "Total Spent", "Joined", "KYC"],
         "inventory" => ["Product", "Category", "Metal", "Stock (g)", "Status", "Buy Price", "Sell Price"],
         _ => ["Order #", "Customer", "Date", "Amount", "Status", "Payment"]
     };

        private async Task<List<List<string>>> BuildSalesRows(DateTime from, DateTime to)
        {
            var rows = await _analyticsService.GetSalesReportAsync(from, to);
            return rows.Select(r => new List<string>
    {
        r.OrderNumber, r.CustomerName,
        r.Date.ToString("dd MMM yyyy"),
        r.Amount.ToString("N2") + " EGP",
        r.OrderStatus, r.PaymentStatus
    }).ToList();
        }

        private async Task<List<List<string>>> BuildProductRows(DateTime from, DateTime to)
        {
            var rows = await _analyticsService.GetProductPerformanceReportAsync(from, to);
            return rows.Select(r => new List<string>
    {
        r.ProductName, r.MetalType,
        r.TotalSold.ToString(),
        r.Revenue.ToString("N2") + " EGP",
        r.Stock.ToString(), r.Status
    }).ToList();
        }

        private async Task<List<List<string>>> BuildCustomerRows()
        {
            var rows = await _analyticsService.GetCustomerReportAsync();
            return rows.Select(r => new List<string>
    {
        r.CustomerName, r.Email,
        r.TotalOrders.ToString(),
        r.TotalSpent.ToString("N2") + " EGP",
        r.JoinedAt.ToString("dd MMM yyyy"),
        r.KycStatus
    }).ToList();
        }

        private async Task<List<List<string>>> BuildInventoryRows()
        {
            var rows = await _analyticsService.GetInventoryReportAsync();
            return rows.Select(r => new List<string>
    {
        r.ProductName, r.Category, r.MetalType,
        r.Stock.ToString(), r.StockStatus,
        r.BuyPrice.ToString("N2"), r.SellPrice.ToString("N2")
    }).ToList();
        }






    }
}
