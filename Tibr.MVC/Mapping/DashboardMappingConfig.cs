using Mapster;
using Tibr.Application.Dtos.DashboardDtos;
using Tibr.MVC.Models;


namespace Tibr.MVC.Mapping
{
    public class DashboardMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // ── DashboardDto → DashboardViewModel ────────────────────────
            config.NewConfig<DashboardDto, DashboardViewModel>()
                .Map(dest => dest.RecentOrders, src => src.RecentOrders)
                .Map(dest => dest.BestSellers, src => src.BestSellers)
                .Map(dest => dest.MonthlySales, src => src.MonthlySales)
                .Map(dest => dest.DailySales, src => src.DailySales)
                .Map(dest => dest.CustomerGrowth, src => src.CustomerGrowth);

            // ── Nested DTO → ViewModel types ─────────────────────────────
            config.NewConfig<RecentOrderDto, RecentOrderRow>();
            config.NewConfig<BestSellerDto, BestSellerRow>();
            config.NewConfig<MonthlySalesPointDto, MonthlySalesPoint>();
            config.NewConfig<DailySalesPointDto, DailySalesPoint>();
            config.NewConfig<CustomerGrowthPointDto, CustomerGrowthPoint>();

            // ── Report DTOs → ViewModel types ────────────────────────────
            config.NewConfig<ReportsSummaryDto, ReportsSummaryViewModel>();
            config.NewConfig<SalesReportDto, SalesReportRow>();
            config.NewConfig<RevenueReportDto, RevenueReportRow>();
            config.NewConfig<ProductPerformanceDto, ProductPerformanceRow>();
            config.NewConfig<InventoryDto, InventoryRow>();
            config.NewConfig<CustomerReportDto, CustomerReportRow>();

            config.NewConfig<MonthlySalesDto, MonthlySalesPoint>();
        }
    }
}