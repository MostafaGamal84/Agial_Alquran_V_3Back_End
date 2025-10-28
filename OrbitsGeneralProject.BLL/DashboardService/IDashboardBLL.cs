using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO.Dashboard;

namespace Orbits.GeneralProject.BLL.DashboardService
{
    public interface IDashboardBLL
    {
        Task<IResponse<DashboardSummaryDto>> GetSummaryAsync();
        Task<IResponse<RepeatCustomerRateDto>> GetRepeatCustomerRateAsync(int months = 6);
        Task<IResponse<MonthlyRevenueChartDto>> GetMonthlyRevenueAsync(int months = 6);
        Task<IResponse<PieChartDto>> GetRevenueByCurrencyAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<IResponse<SubscriberTypeAnalyticsDto>> GetSubscribersByTypeAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<IResponse<RoleDashboardOverviewDto>> GetRoleOverviewAsync(int userId, DashboardRangeInputDto? range = null);
    }
}
