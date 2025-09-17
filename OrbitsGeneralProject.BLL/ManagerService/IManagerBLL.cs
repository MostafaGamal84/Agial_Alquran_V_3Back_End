using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.DTO.ManagerDto;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.UserDto;
using Orbits.GeneralProject.DTO.UserDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.BLL.ManagerService
{
    public interface IManagerBLL
    {
        IResponse<PagedResultDto<ManagerDto>> GetPagedList(FilteredResultRequestDto pagedDto,int userId);
    }
}
