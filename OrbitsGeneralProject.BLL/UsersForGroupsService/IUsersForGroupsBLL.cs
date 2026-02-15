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

namespace Orbits.GeneralProject.BLL.UsersForGroupsService
{
    public interface IUsersForGroupsBLL
    {
      

         IResponse<PagedResultDto<UserLockUpDto>> GetUsersForSelects(FilteredResultRequestDto pagedDto, int UserTypeId, int userId, int? managerId,int? teacherId, int? branchId, int? nationalityId, bool includeRelations = false, int? targetUserId = null);
         IResponse<UserLockUpDto> GetUserDetails(int targetUserId, int requesterId);
         IResponse<PagedResultDto<UserLockUpDto>> GetDeletedUsersByType(FilteredResultRequestDto pagedDto, int userTypeId);

    }
}
