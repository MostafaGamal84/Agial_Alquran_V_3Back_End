using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.CircleService;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO.CircleDto;
using Orbits.GeneralProject.DTO.Paging;

namespace OrbitsProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CircleController : AppBaseController
    {
        //To do Injection for Services BLL
        private readonly ICircleBLL _circleBLL;
        public CircleController(ICircleBLL circleBLL)
        {
            _circleBLL = circleBLL;
        }

        [HttpGet("GetResultsByFilter"), ProducesResponseType(typeof(IResponse<PagedResultDto<CircleDto>>), 200)]
        public async Task<IActionResult> GetResultsByFilter([FromQuery] FilteredResultRequestDto paginationFilterModel ,int? managerId, int? teacherId)
           => Ok(_circleBLL.GetPagedList(paginationFilterModel, managerId, teacherId, UserId));


        [HttpPost("Create"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> Create(CreateCircleDto model)
           => Ok(await _circleBLL.AddAsync(model, 1200));

        [HttpPost("Update"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> Update(UpdateCircleDto model)
         => Ok(await _circleBLL.Update(model, UserId));

        //[Authorize(Roles = UserTypesEnum.Admin)]
        [HttpPost("Delete"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> Delete(int id)
           => Ok(await _circleBLL.DeleteAsync(id, UserId));
    }
}
