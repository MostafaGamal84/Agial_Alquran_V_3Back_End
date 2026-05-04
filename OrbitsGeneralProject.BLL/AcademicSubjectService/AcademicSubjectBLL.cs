using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.Helpers;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.BLL.Validation.AcademicSubjectValidation;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.AcademicSubjectDtos;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.Repositroy.Base;

namespace Orbits.GeneralProject.BLL.AcademicSubjectService
{
    public class AcademicSubjectBLL : BaseBLL, IAcademicSubjectBLL
    {
        private readonly IRepository<AcademicSubject> _academicSubjectRepository;
        private readonly IRepository<AcademicReport> _academicReportRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AcademicSubjectBLL(
            IMapper mapper,
            IRepository<AcademicSubject> academicSubjectRepository,
            IRepository<AcademicReport> academicReportRepository,
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork) : base(mapper)
        {
            _academicSubjectRepository = academicSubjectRepository;
            _academicReportRepository = academicReportRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IResponse<AcademicSubjectDto>> GetByIdAsync(int id, int userId)
        {
            var output = new Response<AcademicSubjectDto>();
            var accessError = ValidateManagementAccess(userId);
            if (accessError != null)
            {
                return output.AppendError(accessError.Code, accessError.Message);
            }

            var subject = await _academicSubjectRepository
                .Where(x => x.Id == id)
                .Select(x => new AcademicSubjectDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    DisplayOrder = x.DisplayOrder,
                    ReportsCount = x.AcademicReports.Count(r => !r.IsDeleted),
                    IsUsed = x.AcademicReports.Any(r => !r.IsDeleted),
                    CreatedAt = x.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (subject == null)
            {
                return output.AppendError(MessageCodes.NotFound);
            }

            return output.CreateResponse(subject);
        }

        public IResponse<PagedResultDto<AcademicSubjectDto>> GetPagedList(FilteredResultRequestDto pagedDto, int userId)
        {
            var output = new Response<PagedResultDto<AcademicSubjectDto>>();
            var accessError = ValidateManagementAccess(userId);
            if (accessError != null)
            {
                return output.AppendError(accessError.Code, accessError.Message);
            }

            var searchTerm = pagedDto.SearchTerm?.Trim().ToLower();
            var query = _academicSubjectRepository
                .GetAll()
                .Select(x => new AcademicSubjectDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    DisplayOrder = x.DisplayOrder,
                    ReportsCount = x.AcademicReports.Count(r => !r.IsDeleted),
                    IsUsed = x.AcademicReports.Any(r => !r.IsDeleted),
                    CreatedAt = x.CreatedAt
                })
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(x => x.Name != null && x.Name.ToLower().Contains(searchTerm));
            }

            var totalCount = query.Count();
            var subjects = query
                .OrderBy(x => x.DisplayOrder ?? int.MaxValue)
                .ThenBy(x => x.Name)
                .Skip(pagedDto.SkipCount)
                .Take(pagedDto.MaxResultCount)
                .ToList();

            var result = new PagedResultDto<AcademicSubjectDto>(totalCount, subjects);
            return output.CreateResponse(result);
        }

        public async Task<IResponse<bool>> AddAsync(CreateAcademicSubjectDto model, int userId)
        {
            var output = new Response<bool>();
            var validationResult = new AcademicSubjectValidation().Validate(model);
            if (!validationResult.IsValid)
            {
                return output.AppendErrors(validationResult.Errors);
            }

            var accessError = ValidateManagementAccess(userId);
            if (accessError != null)
            {
                return output.AppendError(accessError.Code, accessError.Message);
            }

            var normalizedName = model.Name!.Trim();
            var nameExists = await _academicSubjectRepository.AnyAsync(x => x.Name != null && x.Name.ToLower() == normalizedName.ToLower());
            if (nameExists)
            {
                return output.AppendError(MessageCodes.BusinessValidationError, nameof(model.Name), "اسم المادة مستخدم بالفعل.");
            }

            var entity = new AcademicSubject
            {
                Name = normalizedName,
                DisplayOrder = ResolveDisplayOrder(model.DisplayOrder),
                CreatedAt = BusinessDateTime.UtcNow,
                CreatedBy = userId,
                IsDeleted = false
            };

            await _academicSubjectRepository.AddAsync(entity);
            await _unitOfWork.CommitAsync();

            return output.CreateResponse(true);
        }

        public async Task<IResponse<bool>> UpdateAsync(UpdateAcademicSubjectDto model, int userId)
        {
            var output = new Response<bool>();
            var validationResult = new AcademicSubjectValidation().Validate(model);
            if (!validationResult.IsValid)
            {
                return output.AppendErrors(validationResult.Errors);
            }

            if (model.Id <= 0)
            {
                return output.AppendError(MessageCodes.BusinessValidationError, nameof(model.Id), "معرف المادة غير صحيح.");
            }

            var accessError = ValidateManagementAccess(userId);
            if (accessError != null)
            {
                return output.AppendError(accessError.Code, accessError.Message);
            }

            var entity = await _academicSubjectRepository.Where(x => x.Id == model.Id).FirstOrDefaultAsync();
            if (entity == null)
            {
                return output.AppendError(MessageCodes.NotFound);
            }

            var normalizedName = model.Name!.Trim();
            var nameExists = await _academicSubjectRepository.AnyAsync(x =>
                x.Id != model.Id
                && x.Name != null
                && x.Name.ToLower() == normalizedName.ToLower());

            if (nameExists)
            {
                return output.AppendError(MessageCodes.BusinessValidationError, nameof(model.Name), "اسم المادة مستخدم بالفعل.");
            }

            entity.Name = normalizedName;
            entity.DisplayOrder = model.DisplayOrder;
            entity.ModefiedAt = BusinessDateTime.UtcNow;
            entity.ModefiedBy = userId;

            await _unitOfWork.CommitAsync();

            return output.CreateResponse(true);
        }

        public async Task<IResponse<bool>> DeleteAsync(int id, int userId)
        {
            var output = new Response<bool>();
            var accessError = ValidateManagementAccess(userId);
            if (accessError != null)
            {
                return output.AppendError(accessError.Code, accessError.Message);
            }

            var entity = await _academicSubjectRepository.Where(x => x.Id == id).FirstOrDefaultAsync();
            if (entity == null)
            {
                return output.AppendError(MessageCodes.NotFound);
            }

            var hasReports = await _academicReportRepository.AnyAsync(x => x.SubjectId == id);
            if (hasReports)
            {
                return output.CreateResponse(MessageCodes.HasRelation);
            }

            entity.IsDeleted = true;
            entity.ModefiedAt = BusinessDateTime.UtcNow;
            entity.ModefiedBy = userId;

            await _unitOfWork.CommitAsync();
            return output.CreateResponse(true);
        }

        private int ResolveDisplayOrder(int? requestedDisplayOrder)
        {
            if (requestedDisplayOrder.HasValue && requestedDisplayOrder.Value > 0)
            {
                return requestedDisplayOrder.Value;
            }

            return (_academicSubjectRepository.GetAll().Max(x => (int?)x.DisplayOrder) ?? 0) + 1;
        }

        private AccessValidationResult? ValidateManagementAccess(int userId)
        {
            var me = _userRepository.GetById(userId);
            if (me == null)
            {
                return new AccessValidationResult(MessageCodes.NotFound, "المستخدم الحالي غير موجود.");
            }

            if (!EducationSystemTypeHelper.CanAccessAcademicModule(me.UserTypeId, me.EducationSystemTypeId))
            {
                return new AccessValidationResult(MessageCodes.BusinessValidationError, "المستخدم غير مفعل لنظام المواد الدراسية.");
            }

            var userType = (UserTypesEnum)(me.UserTypeId ?? 0);
            if (userType != UserTypesEnum.Admin
                && userType != UserTypesEnum.Manager
                && userType != UserTypesEnum.BranchLeader)
            {
                return new AccessValidationResult(MessageCodes.UnAuthorizedAccess, "غير مسموح لك بإدارة المواد الدراسية.");
            }

            return null;
        }

        private sealed record AccessValidationResult(MessageCodes Code, string Message);
    }
}
