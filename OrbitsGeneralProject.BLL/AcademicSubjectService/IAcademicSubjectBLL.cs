using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO.AcademicSubjectDtos;
using Orbits.GeneralProject.DTO.Paging;

namespace Orbits.GeneralProject.BLL.AcademicSubjectService
{
    public interface IAcademicSubjectBLL
    {
        Task<IResponse<AcademicSubjectDto>> GetByIdAsync(int id, int userId);
        IResponse<PagedResultDto<AcademicSubjectDto>> GetPagedList(FilteredResultRequestDto pagedDto, int userId);
        Task<IResponse<bool>> AddAsync(CreateAcademicSubjectDto model, int userId);
        Task<IResponse<bool>> UpdateAsync(UpdateAcademicSubjectDto model, int userId);
        Task<IResponse<bool>> DeleteAsync(int id, int userId);
    }
}
