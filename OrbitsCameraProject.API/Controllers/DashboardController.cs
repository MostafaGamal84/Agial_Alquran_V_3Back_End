using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.DashboardService;
using Orbits.GeneralProject.DTO.Dashboard;

namespace OrbitsProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : AppBaseController
    {
        private readonly IDashboardBLL _dashboardBLL;

        public DashboardController(IDashboardBLL dashboardBLL)
        {
            _dashboardBLL = dashboardBLL;
        }

        [HttpGet("overview")]
        [Authorize]
        [ProducesResponseType(typeof(IResponse<RoleDashboardOverviewDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRoleOverview([FromQuery] DashboardRangeInputDto? range = null)
        {
            return Ok(await _dashboardBLL.GetRoleOverviewAsync(UserId, range));
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            return Ok(await _dashboardBLL.GetSummaryAsync());
        }

        [HttpGet("repeat-customers")]
        public async Task<IActionResult> GetRepeatCustomers([FromQuery] int months = 6)
        {
            return Ok(await _dashboardBLL.GetRepeatCustomerRateAsync(months));
        }

        [HttpGet("monthly-revenue")]
        public async Task<IActionResult> GetMonthlyRevenue([FromQuery] int months = 6)
        {
            return Ok(await _dashboardBLL.GetMonthlyRevenueAsync(months));
        }

        [HttpGet("revenue-by-currency")]
        public async Task<IActionResult> GetRevenueByCurrency([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            return Ok(await _dashboardBLL.GetRevenueByCurrencyAsync(startDate, endDate));
        }

        [HttpGet("subscribers/by-type")]
        public async Task<IActionResult> GetSubscribersByType([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            return Ok(await _dashboardBLL.GetSubscribersByTypeAsync(startDate, endDate));
        }
    }
}
