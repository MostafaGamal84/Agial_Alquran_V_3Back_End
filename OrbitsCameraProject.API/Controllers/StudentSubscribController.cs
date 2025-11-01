using HandlebarsDotNet;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.StudentSubscribeService;
using Orbits.GeneralProject.BLL.UserService;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.DTO.CircleDto;
using Orbits.GeneralProject.DTO.LockUpDtos;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.StudentSubscribDtos;
using Orbits.GeneralProject.DTO.UserDto;
using Orbits.GeneralProject.DTO.UserDtos;

namespace OrbitsProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentSubscribController : AppBaseController
    {
        //To do Injection for Services BLL
        private readonly IStudentSubscribeBLL _StudentSubscribBLL;
        public StudentSubscribController(IStudentSubscribeBLL StudentSubscribBLL)
        {
            _StudentSubscribBLL = StudentSubscribBLL;
        }

        [HttpGet("GetStudents"), ProducesResponseType(typeof(IResponse<PagedResultDto<ViewStudentSubscribeReDto>>), 200)]
        public async Task<IActionResult> GetStudents(
            [FromQuery] FilteredResultRequestDto paginationFilterModel,
            [FromQuery] int studentId,
            [FromQuery] int? nationalityId)
               => Ok(_StudentSubscribBLL.GetStudents(paginationFilterModel, UserId, studentId, nationalityId));

        [HttpGet("GetStudentSubscribesWithPayment"), ProducesResponseType(typeof(IResponse<PagedResultDto<ViewStudentSubscribeReDto>>), 200)]
        public async Task<IActionResult> GetStudentSubscribesWithPayment(
            [FromQuery] FilteredResultRequestDto paginationFilterModel,
            [FromQuery] int studentId,
            [FromQuery] int? nationalityId)
             => Ok(_StudentSubscribBLL.GetStudentSubscribesWithPayment(paginationFilterModel, studentId, nationalityId));

        [HttpPost("Create"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> Create(AddStudentSubscribeDto model)
         => Ok(await _StudentSubscribBLL.AddAsync(model, 1200));
    }
}
