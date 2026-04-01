using HandlebarsDotNet;
using Hangfire;
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
using OrbitsProject.API.BackgroundJobs;

namespace OrbitsProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentSubscribController : AppBaseController
    {
        //To do Injection for Services BLL
        private readonly IStudentSubscribeBLL _StudentSubscribBLL;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public StudentSubscribController(
            IStudentSubscribeBLL StudentSubscribBLL,
            IBackgroundJobClient backgroundJobClient)
        {
            _StudentSubscribBLL = StudentSubscribBLL;
            _backgroundJobClient = backgroundJobClient;
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

        [HttpGet("GetStudentSubscribeHistory"), ProducesResponseType(typeof(IResponse<PagedResultDto<StudentSubscribeHistoryReDto>>), 200)]
        public async Task<IActionResult> GetStudentSubscribeHistory(
            [FromQuery] FilteredResultRequestDto paginationFilterModel,
            [FromQuery] int studentId)
             => Ok(_StudentSubscribBLL.GetStudentSubscribeHistory(paginationFilterModel, studentId));

        [HttpPost("Create"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> Create(AddStudentSubscribeDto model)
         => Ok(await _StudentSubscribBLL.AddAsync(
             model,
             User?.Identity?.IsAuthenticated == true ? UserId : null));

        [HttpPost("RunMonthlyRenewalNow"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public IActionResult RunMonthlyRenewalNow()
        {
            var jobId = _backgroundJobClient.Enqueue<IStudentSubscriptionRenewalJob>(
                job => job.RenewSubscriptionsAsync());

            return Ok(new Response<bool>().CreateResponse(!string.IsNullOrWhiteSpace(jobId)));
        }

        [HttpPost("RepairMissingInvoices"), ProducesResponseType(typeof(IResponse<RepairStudentSubscriptionsResultDto>), 200)]
        public async Task<IActionResult> RepairMissingInvoices([FromQuery] int? studentId)
            => Ok(await _StudentSubscribBLL.RepairMissingInvoicesAsync(
                studentId,
                User?.Identity?.IsAuthenticated == true ? UserId : null));
    }
}
