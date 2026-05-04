using Microsoft.AspNetCore.Mvc;
using Orbits.GeneralProject.BLL.AcademicSubjectService;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO.AcademicSubjectDtos;
using Orbits.GeneralProject.DTO.Paging;

namespace OrbitsProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AcademicSubjectController : AppBaseController
    {
        private readonly IAcademicSubjectBLL _academicSubjectBLL;

        public AcademicSubjectController(IAcademicSubjectBLL academicSubjectBLL)
        {
            _academicSubjectBLL = academicSubjectBLL;
        }

        [HttpGet("Get"), ProducesResponseType(typeof(IResponse<AcademicSubjectDto>), 200)]
        public async Task<IActionResult> Get(int id)
            => Ok(await _academicSubjectBLL.GetByIdAsync(id, UserId));

        [HttpGet("GetResultsByFilter"), ProducesResponseType(typeof(IResponse<PagedResultDto<AcademicSubjectDto>>), 200)]
        public IActionResult GetResultsByFilter([FromQuery] FilteredResultRequestDto paginationFilterModel)
            => Ok(_academicSubjectBLL.GetPagedList(paginationFilterModel, UserId));

        [HttpPost("Create"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> Create(CreateAcademicSubjectDto model)
            => Ok(await _academicSubjectBLL.AddAsync(model, UserId));

        [HttpPost("Update"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> Update(UpdateAcademicSubjectDto model)
            => Ok(await _academicSubjectBLL.UpdateAsync(model, UserId));

        [HttpPost("Delete"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> Delete(int id)
            => Ok(await _academicSubjectBLL.DeleteAsync(id, UserId));
    }
}
