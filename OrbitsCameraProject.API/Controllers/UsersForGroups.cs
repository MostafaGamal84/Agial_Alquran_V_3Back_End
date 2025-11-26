using HandlebarsDotNet;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.UserService;
using Orbits.GeneralProject.BLL.UsersForGroupsService;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.DTO.LockUpDtos;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.UserDto;
using Orbits.GeneralProject.DTO.UserDtos;

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
            bool includeRelations = false)
               => Ok(_usersForGroupsBLL.GetUsersForSelects(paginationFilterModel, UserTypeId, UserId, managerId, teacherId, branchId, nationalityId, includeRelations));

        [HttpGet("GetUserDetails"), ProducesResponseType(typeof(IResponse<UserLockUpDto>), 200)]
        public async Task<IActionResult> GetUserDetails(int id)
              => Ok(_usersForGroupsBLL.GetUserDetails(id, UserId));



     
    }
}
