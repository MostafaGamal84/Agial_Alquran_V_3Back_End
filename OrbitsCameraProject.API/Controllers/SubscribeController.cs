using Microsoft.AspNetCore.Mvc;
using Orbits.GeneralProject.DTO.Paging;
using System.Security.Claims;
using Orbits.GeneralProject.BLL.BaseReponse;
using Microsoft.AspNetCore.Authorization;
using Orbits.GeneralProject.DTO.SubscribeDtos;
using Orbits.GeneralProject.BLL.SubscribeService;

namespace OrbitsProject.API.Controllers
{
    public class SubscribeController : AppBaseController
    {
        private readonly ISubscribeBLL _SubscribeBLL;

        public SubscribeController(ISubscribeBLL SubscribeBLL)
        {
            _SubscribeBLL = SubscribeBLL;
        }

        [HttpGet("GetResultsByFilter"), ProducesResponseType(typeof(IResponse<PagedResultDto<SubscribeReDto>>), 200)]
        public IActionResult GetResultsByFilter([FromQuery] FilteredResultRequestDto paginationFilterModel) => Ok(_SubscribeBLL.GetPagedList(paginationFilterModel));

        [HttpGet("GetTypeResultsByFilter"), ProducesResponseType(typeof(IResponse<PagedResultDto<SubscribeTypeReDto>>), 200)]
        public IActionResult GetTypeResultsByFilter([FromQuery] FilteredResultRequestDto paginationFilterModel) => Ok(_SubscribeBLL.GetTypeResultsByFilter(paginationFilterModel));

        [HttpPost("GetTypeResultsByFilter"), ProducesResponseType(typeof(IResponse<PagedResultDto<SubscribeTypeReDto>>), 200)]
        public IActionResult GetTypeResultsByFilterPost([FromBody] FilteredResultRequestDto paginationFilterModel) => Ok(_SubscribeBLL.GetTypeResultsByFilter(paginationFilterModel));

        [HttpGet("TypeStatistics"), ProducesResponseType(typeof(IResponse<SubscribeTypeStatisticsDto>), 200)]
        public async Task<IActionResult> GetTypeStatistics() => Ok(await _SubscribeBLL.GetTypeStatisticsAsync());

        [HttpPost("CreateSubscribe"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> CreateSubscribe(CreateSubscribeDto model) => Ok(await _SubscribeBLL.AddAsync(model, UserId));
        [HttpPost("CreateSubscribeType"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> CreateSubscribeType(CreateSubscribeTypeDto model) => Ok(await _SubscribeBLL.AddSubscribeTypeAsync(model, UserId));

        [HttpPost("Update"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> Update(CreateSubscribeDto model) => Ok(await _SubscribeBLL.Update(model, UserId));


        [HttpPost("UpdateType"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> UpdateTypeUpdate(CreateSubscribeTypeDto model) => Ok(await _SubscribeBLL.UpdateType(model, UserId));
        [HttpGet("Delete"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> Delete(int id) => Ok(await _SubscribeBLL.Delete(id));

        [HttpGet("DeleteType"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> DeleteType(int id) => Ok(await _SubscribeBLL.DeleteType(id));

        
    }
}
