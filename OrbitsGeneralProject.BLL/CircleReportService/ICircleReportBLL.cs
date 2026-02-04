using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.DTO.CircleDto;
using Orbits.GeneralProject.DTO.CircleReportDtos;
using Orbits.GeneralProject.DTO.ManagerDto;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.UserDto;
using Orbits.GeneralProject.DTO.UserDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.BLL.CircleReportService
{
    public interface ICircleReportBLL
    {

        IResponse<PagedResultDto<CircleReportReDto>> GetPagedList(FilteredResultRequestDto pagedDto,int userId, int? circleId,int? studentId, int? nationalityId);

        Task<IResponse<bool>> AddAsync(CircleReportAddDto model, int userId);
        Task<IResponse<bool>> Update(CircleReportAddDto model, int userId);
        Task<IResponse<bool>> DeleteAsync(int id, int userId);


    }
}
