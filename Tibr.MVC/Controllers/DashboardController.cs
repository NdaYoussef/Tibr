using Mapster;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos.DashboardDtos;
using Tibr.Application.Services.AdminServices;
using Tibr.MVC.Constants;
using Tibr.MVC.Models;

namespace Tibr.MVC.Controllers
{

    public class DashboardController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IAnalyticsService _analyticsService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IAdminService adminService,
            IAnalyticsService analyticsService,
            ILogger<DashboardController> logger)
        {
            _adminService = adminService;
            _analyticsService = analyticsService;
            _logger = logger;
        }

        //  /Dashboard/Index ─────────────────────
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Dashboard Index requested");

            var dto = await _adminService.GetDashboardDataAsync();

            var vm = dto.Adapt<DashboardViewModel>();

            return View(vm);
        }

        // ── GET /Dashboard/Reports ────────────────────────────────────
        public async Task<IActionResult> Reports(
            string reportType = ReportTypes.Sales,
            DateTime? from = null,
            DateTime? to = null)
        {
            var now = DateTime.UtcNow;
            var fromDate = from ?? now.AddMonths(-1);
            var toDate = to ?? now;

            _logger.LogInformation(
                "Reports requested — type={Type}, from={From}, to={To}",
                reportType, fromDate, toDate);

            var summary = await _analyticsService.GetReportsSummaryAsync(fromDate, toDate);
            var monthly = await _analyticsService.GetMonthlySalesChartAsync();

            
            var sales = IsType(reportType, ReportTypes.Sales, ReportTypes.Revenue)
                 ? await _analyticsService.GetSalesReportAsync(fromDate, toDate)
                 : new List<SalesReportDto>();


            var products = IsType(reportType, ReportTypes.Products)
                ? await _analyticsService.GetProductPerformanceReportAsync(fromDate, toDate)
                : new List<ProductPerformanceDto>();

            var inventory = IsType(reportType, ReportTypes.Inventory)
                ? await _analyticsService.GetInventoryReportAsync()
                : new List<InventoryDto>();

            var customers = IsType(reportType, ReportTypes.Customers)
                 ? await _analyticsService.GetCustomerReportAsync()
                 : new List<CustomerReportDto>();


            // Map each DTO result to its ViewModel equivalent
            var vm = new ReportsViewModel
            {
                ReportType = reportType,
                FromDate = fromDate,
                ToDate = toDate,
                Summary = summary.Adapt<ReportsSummaryViewModel>(),
                MonthlySales = monthly.Adapt<List<MonthlySalesPoint>>(),
                SalesRows = sales.Adapt<List<SalesReportRow>>(),
                ProductRows = products.Adapt<List<ProductPerformanceRow>>(),
                InventoryRows = inventory.Adapt<List<InventoryRow>>(),
                CustomerRows = customers.Adapt<List<CustomerReportRow>>()
            };

            return View(vm);
        }

        // ── POST /Dashboard/InvalidateCache ──────────────────────────
        // Expose cache invalidation as an endpoint so other controllers
        // can call it via HttpClient after writes, or it can be
        // triggered manually from an admin UI button.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult InvalidateCache()
        {
            _adminService.InvalidateDashboardCache();
            _logger.LogInformation("Dashboard cache manually invalidated");
            TempData["Success"] = "Cache dashboard تم مسحه بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        //  Helper 
        private static bool IsType(string actual, params string[] expected)
            => expected.Contains(actual, StringComparer.OrdinalIgnoreCase);
    }
}