using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.DashboardDtos;

namespace Tibr.Application.Services.AdminServices
{
    public interface IAnalyticsService
    {
        Task<ReportsSummaryDto> GetReportsSummaryAsync(
            DateTime fromDate,
            DateTime toDate);

        Task<List<SalesReportDto>> GetSalesReportAsync(
            DateTime fromDate,
            DateTime toDate);

        Task<List<RevenueReportDto>> GetRevenueReportAsync(
           DateTime fromDate,
           DateTime toDate);


        Task<List<ProductPerformanceDto>> GetProductPerformanceReportAsync(
            DateTime fromDate,
            DateTime toDate);

        Task<List<InventoryDto>> GetInventoryReportAsync();

        Task<List<CustomerReportDto>> GetCustomerReportAsync();
        Task<List<MonthlySalesDto>> GetMonthlySalesChartAsync();
    }
}