using Microsoft.AspNetCore.Mvc;
using Orbits.GeneralProject.BLL.AcademicLookupService;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO;

namespace OrbitsProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AcademicLookupController : AppBaseController
    {
        private readonly IAcademicLookupBLL _academicLookupBLL;

        public AcademicLookupController(IAcademicLookupBLL academicLookupBLL)
        {
            _academicLookupBLL = academicLookupBLL;
        }

        [HttpGet("Subjects"), ProducesResponseType(typeof(IResponse<List<LookupDto>>), 200)]
        public async Task<IActionResult> GetSubjects()
            => Ok(await _academicLookupBLL.GetSubjectsAsync());

        [HttpGet("Managers"), ProducesResponseType(typeof(IResponse<List<LookupDto>>), 200)]
        public async Task<IActionResult> GetManagers()
            => Ok(await _academicLookupBLL.GetManagersAsync(UserId));

        [HttpGet("Teachers"), ProducesResponseType(typeof(IResponse<List<LookupDto>>), 200)]
        public async Task<IActionResult> GetTeachers([FromQuery] int? managerId)
            => Ok(await _academicLookupBLL.GetTeachersAsync(UserId, managerId));

        [HttpGet("TeachersForAssignment"), ProducesResponseType(typeof(IResponse<List<LookupDto>>), 200)]
        public async Task<IActionResult> GetTeachersForAssignment()
            => Ok(await _academicLookupBLL.GetTeachersForAssignmentAsync(UserId));

        [HttpGet("Circles"), ProducesResponseType(typeof(IResponse<List<LookupDto>>), 200)]
        public async Task<IActionResult> GetCircles([FromQuery] int? managerId, [FromQuery] int? teacherId)
            => Ok(await _academicLookupBLL.GetCirclesAsync(UserId, managerId, teacherId));

        [HttpGet("Students"), ProducesResponseType(typeof(IResponse<List<LookupDto>>), 200)]
        public async Task<IActionResult> GetStudents([FromQuery] int? circleId)
            => Ok(await _academicLookupBLL.GetStudentsAsync(UserId, circleId));

        [HttpGet("StudentsForAssignment"), ProducesResponseType(typeof(IResponse<List<LookupDto>>), 200)]
        public async Task<IActionResult> GetStudentsForAssignment()
            => Ok(await _academicLookupBLL.GetStudentsForAssignmentAsync(UserId));
    }
}
