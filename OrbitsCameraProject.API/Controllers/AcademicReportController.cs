using Microsoft.AspNetCore.Mvc;
using Orbits.GeneralProject.BLL.AcademicReportService;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO.AcademicReportDtos;
using Orbits.GeneralProject.DTO.Paging;

namespace OrbitsProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AcademicReportController : AppBaseController
    {
        private readonly IAcademicReportBLL _academicReportBLL;

        public AcademicReportController(IAcademicReportBLL academicReportBLL)
        {
            _academicReportBLL = academicReportBLL;
        }

        [HttpGet("Get"), ProducesResponseType(typeof(IResponse<AcademicReportReDto>), 200)]
        public async Task<IActionResult> Get(int id)
            => Ok(await _academicReportBLL.GetByIdAsync(id, UserId));

        [HttpGet("GetResultsByFilter"), ProducesResponseType(typeof(IResponse<PagedResultDto<AcademicReportReDto>>), 200)]
        public IActionResult GetResultsByFilter(
            [FromQuery] FilteredResultRequestDto paginationFilterModel,
            [FromQuery] int? circleId,
            [FromQuery] int? studentId,
            [FromQuery] int? teacherId,
            [FromQuery] int? subjectId)
            => Ok(_academicReportBLL.GetPagedList(paginationFilterModel, circleId, studentId, teacherId, subjectId, UserId));

        [HttpPost("Create"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> Create(AcademicReportAddDto model)
            => Ok(await _academicReportBLL.AddAsync(model, UserId));

        [HttpPost("Update"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> Update(AcademicReportAddDto model)
            => Ok(await _academicReportBLL.UpdateAsync(model, UserId));

        [HttpPost("Delete"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> Delete(int id)
            => Ok(await _academicReportBLL.DeleteAsync(id, UserId));
    }
}
