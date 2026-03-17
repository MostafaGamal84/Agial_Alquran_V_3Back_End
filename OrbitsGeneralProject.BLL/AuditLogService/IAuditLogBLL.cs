using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO.AuditLogDtos;
using Orbits.GeneralProject.DTO.Paging;

namespace Orbits.GeneralProject.BLL.AuditLogService
{
    public interface IAuditLogBLL
    {
        Task<IResponse<PagedResultDto<AuditLogListItemDto>>> GetPagedListAsync(AuditLogFilterDto pagedDto);
        Task<IResponse<AuditLogFilterOptionsDto>> GetFilterOptionsAsync();
    }
}
