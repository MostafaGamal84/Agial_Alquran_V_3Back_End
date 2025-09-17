using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.CircleReportService;
using Orbits.GeneralProject.BLL.CircleService;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO.CircleDto;
using Orbits.GeneralProject.DTO.CircleReportDtos;
using Orbits.GeneralProject.DTO.ManagerDto;
using Orbits.GeneralProject.DTO.Paging;

namespace OrbitsProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CircleReportController : AppBaseController
    {
        //To do Injection for Services BLL
        private readonly ICircleReportBLL _circleReportBLL;
        public CircleReportController(ICircleReportBLL circleReportBLL)
        {
            _circleReportBLL = circleReportBLL;
        }
        [HttpGet("GetResultsByFilter")]
        [ProducesResponseType(typeof(IResponse<PagedResultDto<CircleReportReDto>>), 200)]
        public IActionResult GetResultsByFilter(
    [FromQuery] FilteredResultRequestDto pagination,
    [FromQuery] int? circleId,
    [FromQuery] int? studentId)
        {
            return Ok(_circleReportBLL.GetPagedList(pagination, UserId, circleId, studentId));
        }

        [HttpPost("Create"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> Create(CircleReportAddDto model)
           => Ok(await _circleReportBLL.AddAsync(model, UserId));
   
   
        [HttpPost("Update"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> Update(CircleReportAddDto model)
           => Ok(await _circleReportBLL.Update(model, UserId));
    }

}
