using Microsoft.EntityFrameworkCore.ChangeTracking;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.DTO.LockUpDtos;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.Repositroy.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Orbits.GeneralProject.BLL.Constants.DXConstants;

namespace Orbits.GeneralProject.BLL.LookUpService
{
    public interface ILookUpBLL
    {
      
        //Task<IResponse<object>> GetUsersByUserType(int UserTypeId, int userId);

         IResponse<PagedResultDto<UserLockUpDto>> GetUsersByUserType(FilteredResultRequestDto pagedDto, int UserTypeId, int userId);
        Task<IResponse<object>> GetAllNationality();
        Task<IResponse<List<LookupDto>>> GetAllSubscribesByTypeId(int? id);
        Task<IResponse<object>> GetAllGovernorate();

    }
}
