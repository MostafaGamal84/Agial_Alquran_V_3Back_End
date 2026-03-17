using Microsoft.AspNetCore.Mvc;
using Orbits.GeneralProject.BLL.AuditLogService;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO.AuditLogDtos;
using Orbits.GeneralProject.DTO.Paging;

namespace OrbitsProject.API.Controllers
{
    public class AuditLogController : AppBaseController
    {
        private readonly IAuditLogBLL _auditLogBLL;

        public AuditLogController(IAuditLogBLL auditLogBLL)
        {
            _auditLogBLL = auditLogBLL;
        }

        [HttpGet("GetResultsByFilter"), ProducesResponseType(typeof(IResponse<PagedResultDto<AuditLogListItemDto>>), 200)]
        public async Task<IActionResult> GetResultsByFilter([FromQuery] AuditLogFilterDto paginationFilterModel)
            => Ok(await _auditLogBLL.GetPagedListAsync(paginationFilterModel));

        [HttpGet("GetFilterOptions"), ProducesResponseType(typeof(IResponse<AuditLogFilterOptionsDto>), 200)]
        public async Task<IActionResult> GetFilterOptions()
            => Ok(await _auditLogBLL.GetFilterOptionsAsync());
    }
}
