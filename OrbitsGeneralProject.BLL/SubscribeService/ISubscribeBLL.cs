using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.SubscribeDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.BLL.SubscribeService
{
    public interface ISubscribeBLL
    {
        Task<IResponse<bool>> AddAsync(CreateSubscribeDto model, int userId);
        Task<IResponse<bool>> AddSubscribeTypeAsync(CreateSubscribeTypeDto model, int userId);
        IResponse<PagedResultDto<SubscribeReDto>> GetPagedList(FilteredResultRequestDto pagedDto);
        IResponse<PagedResultDto<SubscribeTypeReDto>> GetTypeResultsByFilter(FilteredResultRequestDto pagedDto);
        Task<IResponse<SubscribeTypeStatisticsDto>> GetTypeStatisticsAsync();
        Task<IResponse<bool>> Delete(int id);
        Task<IResponse<bool>> DeleteType(int id);
        //Task<IResponse<SubscribeDto>> GetSubscribeById(int id);
        Task<IResponse<bool>> Update(CreateSubscribeDto dto, int userId);
        Task<IResponse<bool>> UpdateType(CreateSubscribeTypeDto dto, int userId);
    }
}
