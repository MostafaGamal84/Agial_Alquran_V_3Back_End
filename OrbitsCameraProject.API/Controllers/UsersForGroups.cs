using HandlebarsDotNet;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.UserService;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.BLL.UsersForGroupsService;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.DTO.LockUpDtos;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.UserDto;
using Orbits.GeneralProject.DTO.UserDtos;
using System.Collections.Generic;
using System.Linq;

namespace OrbitsProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersForGroupsController : AppBaseController
    {
        //To do Injection for Services BLL
        private readonly IUsersForGroupsBLL _usersForGroupsBLL;
        public UsersForGroupsController( IUsersForGroupsBLL usersForGroupsBLL)
        {
            _usersForGroupsBLL = usersForGroupsBLL;
        }

        [HttpGet("GetUsersForSelects"), ProducesResponseType(typeof(IResponse<PagedResultDto<UserLockUpDto>>), 200)]
        public async Task<IActionResult> GetUsersForSelects(
            [FromQuery] FilteredResultRequestDto paginationFilterModel,
            int UserTypeId,
            int managerId,
            int teacherId,
            int branchId,
            int? nationalityId,
            [FromQuery(Name = "managerIds")] string? managerIds,
            [FromQuery(Name = "managerIds[]")] int[]? managerIdsArray,
            bool includeRelations = false)
        {
            var parsedManagerIds = new List<int>();

            if (!string.IsNullOrWhiteSpace(managerIds))
            {
                var managerIdsFromCsv = managerIds
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => int.TryParse(id, out var parsedId) ? parsedId : (int?)null)
                    .Where(id => id.HasValue && id.Value > 0)
                    .Select(id => id!.Value);

                parsedManagerIds.AddRange(managerIdsFromCsv);
            }

            if (managerIdsArray != null && managerIdsArray.Length > 0)
            {
                parsedManagerIds.AddRange(managerIdsArray.Where(id => id > 0));
            }

            return Ok(_usersForGroupsBLL.GetUsersForSelects(
                paginationFilterModel,
                UserTypeId,
                UserId,
                managerId,
                parsedManagerIds,
                teacherId,
                branchId,
                nationalityId,
                includeRelations,
                null));
        }


        [HttpGet("DeletedStudents"), ProducesResponseType(typeof(IResponse<PagedResultDto<UserLockUpDto>>), 200)]
        public IActionResult GetDeletedStudents([FromQuery] FilteredResultRequestDto paginationFilterModel)
            => Ok(_usersForGroupsBLL.GetDeletedUsersByType(paginationFilterModel, (int)UserTypesEnum.Student, UserId));

        [HttpGet("DeletedTeachers"), ProducesResponseType(typeof(IResponse<PagedResultDto<UserLockUpDto>>), 200)]
        public IActionResult GetDeletedTeachers([FromQuery] FilteredResultRequestDto paginationFilterModel)
            => Ok(_usersForGroupsBLL.GetDeletedUsersByType(paginationFilterModel, (int)UserTypesEnum.Teacher, UserId));

        [HttpGet("DeletedManagers"), ProducesResponseType(typeof(IResponse<PagedResultDto<UserLockUpDto>>), 200)]
        public IActionResult GetDeletedManagers([FromQuery] FilteredResultRequestDto paginationFilterModel)
            => Ok(_usersForGroupsBLL.GetDeletedUsersByType(paginationFilterModel, (int)UserTypesEnum.Manager, UserId));

        [HttpGet("DeletedBranchLeaders"), ProducesResponseType(typeof(IResponse<PagedResultDto<UserLockUpDto>>), 200)]
        public IActionResult GetDeletedBranchLeaders([FromQuery] FilteredResultRequestDto paginationFilterModel)
            => Ok(_usersForGroupsBLL.GetDeletedUsersByType(paginationFilterModel, (int)UserTypesEnum.BranchLeader, UserId));

        [HttpGet("GetUserDetails"), ProducesResponseType(typeof(IResponse<UserLockUpDto>), 200)]
        public async Task<IActionResult> GetUserDetails(int id)
              => Ok(_usersForGroupsBLL.GetUserDetails(id, UserId));



     
    }
}
