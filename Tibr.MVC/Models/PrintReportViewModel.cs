using Tibr.Application.Dtos.DashboardDtos;

namespace Tibr.MVC.Models
{
    public class PrintReportViewModel
    {
        public string ReportType { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public ReportsSummaryDto Summary { get; set; } = new();
        public List<string> TableHeaders { get; set; } = [];
        public List<List<string>> TableRows { get; set; } = [];
    }
}
