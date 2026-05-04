using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO;

namespace Orbits.GeneralProject.BLL.AcademicLookupService
{
    public interface IAcademicLookupBLL
    {
        Task<IResponse<List<LookupDto>>> GetSubjectsAsync();
        Task<IResponse<List<LookupDto>>> GetManagersAsync(int userId);
        Task<IResponse<List<LookupDto>>> GetTeachersAsync(int userId, int? managerId);
        Task<IResponse<List<LookupDto>>> GetTeachersForAssignmentAsync(int userId);
        Task<IResponse<List<LookupDto>>> GetCirclesAsync(int userId, int? managerId, int? teacherId);
        Task<IResponse<List<LookupDto>>> GetStudentsAsync(int userId, int? circleId);
        Task<IResponse<List<LookupDto>>> GetStudentsForAssignmentAsync(int userId);
    }
}
