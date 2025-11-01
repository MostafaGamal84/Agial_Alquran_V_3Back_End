using Microsoft.EntityFrameworkCore.ChangeTracking;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.DTO.CircleDto;
using Orbits.GeneralProject.DTO.LockUpDtos;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.StudentSubscribDtos;
using Orbits.GeneralProject.Repositroy.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static Orbits.GeneralProject.BLL.Constants.DXConstants;

namespace Orbits.GeneralProject.BLL.StudentSubscribeService
{
    public interface IStudentSubscribeBLL
    {

        Task<IResponse<bool>> AddAsync(AddStudentSubscribeDto model, int userId);

        IResponse<PagedResultDto<ViewStudentSubscribeReDto>> GetStudents(
            FilteredResultRequestDto pagedDto,
            int userId,
            int? studentId,
            int? nationalityId);
        IResponse<PagedResultDto<ViewStudentSubscribeReDto>> GetStudentSubscribesWithPayment(
            FilteredResultRequestDto pagedDto,
            int? studentId,
            int? nationalityId);

    }
}
