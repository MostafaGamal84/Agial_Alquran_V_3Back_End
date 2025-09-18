using Microsoft.AspNetCore.Mvc;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.TeacherSallaryService;
using Orbits.GeneralProject.DTO.TeacherSallaryDtos;

namespace OrbitsProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeacherSallaryController : AppBaseController
    {
        private readonly ITeacherSallaryBLL _teacherSallaryBll;

        public TeacherSallaryController(ITeacherSallaryBLL teacherSallaryBll)
        {
            _teacherSallaryBll = teacherSallaryBll;
        }

        /// <summary>
        /// Generates teacher salary invoices for the specified month. If no month is provided
        /// the previous calendar month is used.
        /// </summary>
        /// <param name="month">Optional month (the day component is ignored).</param>
        [HttpPost("GenerateMonthly")]
        [ProducesResponseType(typeof(IResponse<TeacherSallaryGenerationResultDto>), 200)]
        public async Task<IActionResult> GenerateMonthly([FromQuery] DateTime? month = null)
        {
            var result = await _teacherSallaryBll.GenerateMonthlyInvoicesAsync(month, UserId);
            return Ok(result);
        }
    }
}
