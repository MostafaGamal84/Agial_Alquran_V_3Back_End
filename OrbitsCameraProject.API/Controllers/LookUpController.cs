using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.LookUpService;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.DTO.LockUpDtos;
using Orbits.GeneralProject.DTO.ManagerDto;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.RegionDtos;
namespace OrbitsProject.API.Controllers
{
    public class LookUpController : AppBaseController
    {
        //To do Injection for Services BLL
        private readonly ILookUpBLL _lookUpService;

        public LookUpController(ILookUpBLL lookUpService)
        {
            _lookUpService = lookUpService;
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary> GetUsersByUserType  </summary>
        /// 
        /// <input>  empty   </input>
        /// 
        /// <value> success if GetUsersByUserType , error by didnt GetUsersByUserType </value>
        ///-------------------------------------------------------------------------------------------------
        [HttpGet("GetUsersByUserType"), ProducesResponseType(typeof(IResponse<PagedResultDto<UserLockUpDto>>), 200)]
        public async Task<IActionResult> GetUsersByUserType([FromQuery] FilteredResultRequestDto paginationFilterModel, int UserTypeId)
            => Ok( _lookUpService.GetUsersByUserType( paginationFilterModel ,UserTypeId, UserId));

        [HttpGet("GetAllNationality"), ProducesResponseType(typeof(IResponse<List<RegionDto>>), 200)]
        public async Task<IActionResult> GetAllNationality()
           => Ok(await _lookUpService.GetAllNationality());

        [HttpGet("GetAllGovernorate"), ProducesResponseType(typeof(IResponse<List<LookupDto>>), 200)]
        public async Task<IActionResult> GetAllGovernorate()
          => Ok(await _lookUpService.GetAllGovernorate());

        [HttpGet("GetAllSubscribesByTypeId"), ProducesResponseType(typeof(IResponse<List<LookupDto>>), 200)]
        public async Task<IActionResult> GetAllSubscribesByTypeId(int? id, int? studentId)
           => Ok(await _lookUpService.GetAllSubscribesByTypeId(id, studentId));


    }
}
