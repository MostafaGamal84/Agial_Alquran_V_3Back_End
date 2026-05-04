using Microsoft.AspNetCore.Mvc;
using Orbits.GeneralProject.BLL.AcademicCircleService;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO.AcademicCircleDtos;
using Orbits.GeneralProject.DTO.Paging;

namespace OrbitsProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AcademicCircleController : AppBaseController
    {
        private readonly IAcademicCircleBLL _academicCircleBLL;

        public AcademicCircleController(IAcademicCircleBLL academicCircleBLL)
        {
            _academicCircleBLL = academicCircleBLL;
        }

        [HttpGet("Get"), ProducesResponseType(typeof(IResponse<AcademicCircleDto>), 200)]
        public async Task<IActionResult> Get(int id)
            => Ok(await _academicCircleBLL.GetByIdAsync(id, UserId));

        [HttpGet("GetResultsByFilter"), ProducesResponseType(typeof(IResponse<PagedResultDto<AcademicCircleDto>>), 200)]
        public IActionResult GetResultsByFilter([FromQuery] FilteredResultRequestDto paginationFilterModel, int? managerId, int? teacherId)
            => Ok(_academicCircleBLL.GetPagedList(paginationFilterModel, managerId, teacherId, UserId));

        [HttpPost("Create"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> Create(CreateAcademicCircleDto model)
            => Ok(await _academicCircleBLL.AddAsync(model, UserId));

        [HttpPost("Update"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> Update(UpdateAcademicCircleDto model)
            => Ok(await _academicCircleBLL.UpdateAsync(model, UserId));

        [HttpPost("Delete"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> Delete(int id)
            => Ok(await _academicCircleBLL.DeleteAsync(id, UserId));
    }
}
