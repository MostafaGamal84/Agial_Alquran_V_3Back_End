using HandlebarsDotNet;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.ManagerService;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.DTO.ManagerDto;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.UserDtos;

namespace OrbitsProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManagerController : AppBaseController
    {
        //To do Injection for Services BLL
        private readonly IManagerBLL _managerBLL;
        public ManagerController(IManagerBLL managerBLL)
        {
            _managerBLL = managerBLL;
        }
        [HttpGet("GetResultsByFilter"), ProducesResponseType(typeof(IResponse<PagedResultDto<ManagerDto>>), 200)]
        public async Task<IActionResult> GetResultsByFilter([FromQuery] FilteredResultRequestDto paginationFilterModel, int userId)
             => Ok(_managerBLL.GetPagedList(paginationFilterModel, UserId));

    }
}
