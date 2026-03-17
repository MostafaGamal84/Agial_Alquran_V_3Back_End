using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO.AuditLogDtos;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.Repositroy.Base;
using System.Text.Json;

namespace Orbits.GeneralProject.BLL.AuditLogService
{
    public class AuditLogBLL : BaseBLL, IAuditLogBLL
    {
        private static readonly JsonSerializerOptions AuditJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly string[] DefaultActionTypes =
        {
            "Create",
            "Update",
            "Delete",
            "Restore"
        };

        private static readonly string[] DefaultEntityTypes =
        {
            nameof(User),
            nameof(Circle),
            nameof(CircleReport),
            nameof(Subscribe),
            nameof(SubscribeType),
            nameof(StudentSubscribe),
            nameof(StudentPayment),
            nameof(TeacherSallary),
            nameof(ManagerTeacher),
            nameof(ManagerStudent),
            nameof(ManagerCircle)
        };

        private readonly IRepository<AuditLog> _auditLogRepository;

        public AuditLogBLL(IMapper mapper, IRepository<AuditLog> auditLogRepository) : base(mapper)
        {
            _auditLogRepository = auditLogRepository;
        }

        public async Task<IResponse<PagedResultDto<AuditLogListItemDto>>> GetPagedListAsync(AuditLogFilterDto pagedDto)
        {
            Response<PagedResultDto<AuditLogListItemDto>> output = new();
            pagedDto ??= new AuditLogFilterDto();

            try
            {
                var searchTerm = pagedDto.SearchTerm?.Trim().ToLower();
                var query = _auditLogRepository
                    .GetAll(true)
                    .AsNoTracking()
                    .Include(x => x.Participants)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(x =>
                        (!string.IsNullOrWhiteSpace(x.Summary) && x.Summary.ToLower().Contains(searchTerm)) ||
                        (!string.IsNullOrWhiteSpace(x.ActorName) && x.ActorName.ToLower().Contains(searchTerm)) ||
                        (!string.IsNullOrWhiteSpace(x.EntityLabel) && x.EntityLabel.ToLower().Contains(searchTerm)) ||
                        (!string.IsNullOrWhiteSpace(x.EntityDisplayName) && x.EntityDisplayName.ToLower().Contains(searchTerm)));
                }

                if (!string.IsNullOrWhiteSpace(pagedDto.ActionType))
                {
                    query = query.Where(x => x.ActionType == pagedDto.ActionType);
                }

                if (!string.IsNullOrWhiteSpace(pagedDto.EntityType))
                {
                    query = query.Where(x => x.EntityType == pagedDto.EntityType);
                }

                if (pagedDto.ActorUserId.HasValue)
                {
                    query = query.Where(x => x.ActorUserId == pagedDto.ActorUserId.Value);
                }

                if (pagedDto.FromDate.HasValue)
                {
                    var fromDate = pagedDto.FromDate.Value.Date;
                    query = query.Where(x => x.CreatedAt >= fromDate);
                }

                if (pagedDto.ToDate.HasValue)
                {
                    var toExclusive = pagedDto.ToDate.Value.Date.AddDays(1);
                    query = query.Where(x => x.CreatedAt < toExclusive);
                }

                query = ApplyParticipantFilter(query, "Student", pagedDto.StudentId);
                query = ApplyParticipantFilter(query, "Manager", pagedDto.ManagerId);
                query = ApplyParticipantFilter(query, "Teacher", pagedDto.TeacherId);
                query = ApplyParticipantFilter(query, "Subscribe", pagedDto.SubscribeId);
                query = ApplyParticipantFilter(query, "SubscribeType", pagedDto.SubscribeTypeId);
                query = ApplyParticipantFilter(query, "Circle", pagedDto.CircleId);
                query = ApplyParticipantFilter(query, "StudentPayment", pagedDto.StudentPaymentId);
                query = ApplyParticipantFilter(query, "CircleReport", pagedDto.CircleReportId);

                query = ApplySorting(query, pagedDto.SortBy, pagedDto.SortingDirection);

                var skipCount = Math.Max(0, pagedDto.SkipCount);
                var maxResultCount = pagedDto.MaxResultCount <= 0 ? 25 : pagedDto.MaxResultCount;
                var totalCount = await query.CountAsync();

                var pageItems = await query
                    .Skip(skipCount)
                    .Take(maxResultCount)
                    .Select(x => new AuditLogProjection
                    {
                        Id = x.Id,
                        ActionType = x.ActionType,
                        EntityType = x.EntityType,
                        EntityId = x.EntityId,
                        EntityLabel = x.EntityLabel,
                        EntityDisplayName = x.EntityDisplayName,
                        Summary = x.Summary,
                        ActorUserId = x.ActorUserId,
                        ActorName = x.ActorName,
                        ActorRoleId = x.ActorRoleId,
                        CreatedAt = x.CreatedAt,
                        ChangesJson = x.ChangesJson,
                        Participants = x.Participants
                            .OrderBy(p => p.ParticipantType)
                            .ThenBy(p => p.DisplayName)
                            .Select(p => new AuditLogParticipantDto
                            {
                                ParticipantType = p.ParticipantType,
                                ParticipantId = p.ParticipantId,
                                ParticipantLabel = p.ParticipantLabel,
                                DisplayName = p.DisplayName
                            })
                            .ToList()
                    })
                    .ToListAsync();

                var result = new PagedResultDto<AuditLogListItemDto>
                {
                    TotalCount = totalCount,
                    Items = pageItems.Select(MapProjection).ToList()
                };

                return output.CreateResponse(result);
            }
            catch (Exception ex)
            {
                return output.CreateResponse(ex);
            }
        }

        public async Task<IResponse<AuditLogFilterOptionsDto>> GetFilterOptionsAsync()
        {
            Response<AuditLogFilterOptionsDto> output = new();

            try
            {
                var query = _auditLogRepository
                    .GetAll(true)
                    .AsNoTracking();

                var actionTypes = await query
                    .Where(x => !string.IsNullOrWhiteSpace(x.ActionType))
                    .Select(x => x.ActionType!)
                    .Distinct()
                    .ToListAsync();

                var entityTypes = await query
                    .Where(x => !string.IsNullOrWhiteSpace(x.EntityType))
                    .Select(x => x.EntityType!)
                    .Distinct()
                    .ToListAsync();

                var options = new AuditLogFilterOptionsDto
                {
                    ActionTypes = DefaultActionTypes
                        .Concat(actionTypes)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Select(x => new AuditLogFilterOptionDto
                        {
                            Value = x,
                            Label = x
                        })
                        .ToList(),
                    EntityTypes = DefaultEntityTypes
                        .Concat(entityTypes)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Select(x => new AuditLogFilterOptionDto
                        {
                            Value = x,
                            Label = x
                        })
                        .ToList()
                };

                return output.CreateResponse(options);
            }
            catch (Exception ex)
            {
                return output.CreateResponse(ex);
            }
        }

        private static IQueryable<AuditLog> ApplyParticipantFilter(
            IQueryable<AuditLog> query,
            string participantType,
            int? participantId)
        {
            if (!participantId.HasValue || participantId.Value <= 0)
            {
                return query;
            }

            return query.Where(x => x.Participants.Any(p =>
                p.ParticipantType == participantType &&
                p.ParticipantId == participantId.Value));
        }

        private static IQueryable<AuditLog> ApplySorting(
            IQueryable<AuditLog> query,
            string? sortBy,
            string? sortingDirection)
        {
            var sortKey = (sortBy ?? string.Empty).Trim();
            var isAscending = string.Equals(sortingDirection, "ASC", StringComparison.OrdinalIgnoreCase);

            return sortKey switch
            {
                nameof(AuditLog.ActionType) => isAscending
                    ? query.OrderBy(x => x.ActionType).ThenByDescending(x => x.CreatedAt)
                    : query.OrderByDescending(x => x.ActionType).ThenByDescending(x => x.CreatedAt),
                nameof(AuditLog.EntityType) => isAscending
                    ? query.OrderBy(x => x.EntityType).ThenByDescending(x => x.CreatedAt)
                    : query.OrderByDescending(x => x.EntityType).ThenByDescending(x => x.CreatedAt),
                nameof(AuditLog.ActorName) => isAscending
                    ? query.OrderBy(x => x.ActorName).ThenByDescending(x => x.CreatedAt)
                    : query.OrderByDescending(x => x.ActorName).ThenByDescending(x => x.CreatedAt),
                _ => isAscending
                    ? query.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id)
                    : query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            };
        }

        private static AuditLogListItemDto MapProjection(AuditLogProjection projection)
        {
            return new AuditLogListItemDto
            {
                Id = projection.Id,
                ActionType = projection.ActionType,
                EntityType = projection.EntityType,
                EntityId = projection.EntityId,
                EntityLabel = projection.EntityLabel,
                EntityDisplayName = projection.EntityDisplayName,
                Summary = projection.Summary,
                ActorUserId = projection.ActorUserId,
                ActorName = projection.ActorName,
                ActorRoleId = projection.ActorRoleId,
                CreatedAt = projection.CreatedAt,
                Changes = DeserializeChanges(projection.ChangesJson),
                Participants = projection.Participants
            };
        }

        private static IReadOnlyList<AuditLogChangeDto> DeserializeChanges(string? changesJson)
        {
            if (string.IsNullOrWhiteSpace(changesJson))
            {
                return Array.Empty<AuditLogChangeDto>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<AuditLogChangeDto>>(changesJson, AuditJsonOptions)
                    ?? new List<AuditLogChangeDto>();
            }
            catch
            {
                return Array.Empty<AuditLogChangeDto>();
            }
        }

        private sealed class AuditLogProjection
        {
            public int Id { get; set; }
            public string? ActionType { get; set; }
            public string? EntityType { get; set; }
            public int? EntityId { get; set; }
            public string? EntityLabel { get; set; }
            public string? EntityDisplayName { get; set; }
            public string? Summary { get; set; }
            public int? ActorUserId { get; set; }
            public string? ActorName { get; set; }
            public int? ActorRoleId { get; set; }
            public DateTime CreatedAt { get; set; }
            public string? ChangesJson { get; set; }
            public IReadOnlyList<AuditLogParticipantDto> Participants { get; set; } = Array.Empty<AuditLogParticipantDto>();
        }
    }
}
