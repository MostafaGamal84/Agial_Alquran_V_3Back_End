using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO.AcademicCircleDtos;
using Orbits.GeneralProject.DTO.Paging;

namespace Orbits.GeneralProject.BLL.AcademicCircleService
{
    public interface IAcademicCircleBLL
    {
        Task<IResponse<AcademicCircleDto>> GetByIdAsync(int id, int userId);
        IResponse<PagedResultDto<AcademicCircleDto>> GetPagedList(FilteredResultRequestDto pagedDto, int? managerId, int? teacherId, int userId);
        Task<IResponse<bool>> AddAsync(CreateAcademicCircleDto model, int userId);
        Task<IResponse<bool>> UpdateAsync(UpdateAcademicCircleDto model, int userId);
        Task<IResponse<bool>> DeleteAsync(int id, int userId);
    }
}
