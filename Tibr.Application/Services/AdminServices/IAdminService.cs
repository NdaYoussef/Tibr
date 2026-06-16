using System;
using System.Collections.Generic;
using System.Text;
using Tibr.Application.Dtos.DashboardDtos;

namespace Tibr.Application.Services.AdminServices
{
    public interface IAdminService
    {
        Task<DashboardDto> GetDashboardDataAsync();
        TimeSpan DashboardCacheDuration { get; }
        void InvalidateDashboardCache();
    }
}
