using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO.AcademicReportDtos;
using Orbits.GeneralProject.DTO.Paging;

namespace Orbits.GeneralProject.BLL.AcademicReportService
{
    public interface IAcademicReportBLL
    {
        Task<IResponse<AcademicReportReDto>> GetByIdAsync(int id, int userId);
        IResponse<PagedResultDto<AcademicReportReDto>> GetPagedList(
            FilteredResultRequestDto pagedDto,
            int? circleId,
            int? studentId,
            int? teacherId,
            int? subjectId,
            int userId);
        Task<IResponse<bool>> AddAsync(AcademicReportAddDto model, int userId);
        Task<IResponse<bool>> UpdateAsync(AcademicReportAddDto model, int userId);
        Task<IResponse<bool>> DeleteAsync(int id, int userId);
    }
}
