using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.DTO.CircleDto;
using Orbits.GeneralProject.DTO.ManagerDto;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.UserDto;
using Orbits.GeneralProject.DTO.UserDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.BLL.CircleService
{
    public interface ICircleBLL
    {
        public IResponse<PagedResultDto<CircleDto>> GetPagedList(FilteredResultRequestDto pagedDto, int? managerId, int? teacherId, int userId);

        Task<IResponse<IEnumerable<UpcomingCircleDto>>> GetUpcomingAsync(int userId, int? managerId = null, int? teacherId = null, int take = 4);

        Task<IResponse<bool>> AddAsync(CreateCircleDto model, int userId);

        Task<IResponse<bool>> Update(UpdateCircleDto dto, int userId);
        Task<IResponse<bool>> DeleteAsync(int id, int userId);


    }
}
