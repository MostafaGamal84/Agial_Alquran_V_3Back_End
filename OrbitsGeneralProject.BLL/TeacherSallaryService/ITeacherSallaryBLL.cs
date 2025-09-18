using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO.TeacherSallaryDtos;

namespace Orbits.GeneralProject.BLL.TeacherSallaryService
{
    public interface ITeacherSallaryBLL
    {
        /// <summary>
        /// Generates (or refreshes) teacher salary invoices for the specified month.
        /// If no month is provided the previous calendar month is used.
        /// </summary>
        /// <param name="month">The month to generate invoices for. The day component is ignored.</param>
        /// <param name="createdBy">Optional user id used for audit fields.</param>
        /// <returns>A response describing how many invoices were created/updated.</returns>
        Task<IResponse<TeacherSallaryGenerationResultDto>> GenerateMonthlyInvoicesAsync(DateTime? month = null, int? createdBy = null);
    }
}
