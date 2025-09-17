using AutoMapper;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.BLL.Validation.CircleValidation;
using Orbits.GeneralProject.BLL.Validation.UserValidation;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.DTO.CircleDto;
using Orbits.GeneralProject.DTO.ManagerDto;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.UserDto;
using Orbits.GeneralProject.DTO.UserDtos;
using Orbits.GeneralProject.Repositroy.Base;
using System.Linq;
using System.Linq.Expressions;

namespace Orbits.GeneralProject.BLL.CircleService
{
    public class CircleBLL : BaseBLL, ICircleBLL
    {
        private readonly IRepository<Circle> _circleRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<ManagerCircle> _managerCircleRepository;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public CircleBLL(IMapper mapper, IRepository<Circle> circleRepository,
             IUnitOfWork unitOfWork,
             IHostEnvironment hostEnvironment, IRepository<ManagerCircle> managerCircleRepository, IRepository<User> userRepository) : base(mapper)
        {
            _circleRepository = circleRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _managerCircleRepository = managerCircleRepository;
            _userRepository = userRepository;
        }



        public IResponse<PagedResultDto<CircleDto>> GetPagedList(
     FilteredResultRequestDto pagedDto,
     int? managerId,
     int? teacherId,
     int userId)
        {
            var output = new Response<PagedResultDto<CircleDto>>();

            // Caller
            var me = _userRepository.GetById(userId);
            if (me == null) return output.AppendError(MessageCodes.NotFound);

            var userType = (UserTypesEnum)(me.UserTypeId ?? 0);
            string? searchWordLower = pagedDto.SearchTerm?.Trim()?.ToLower();

            // Explicit filters from query (?managerId=1220&teacherId=55); treat 0 as no filter
            int? mId = (managerId.HasValue && managerId.Value > 0) ? managerId.Value : (int?)null;
            int? tId = (teacherId.HasValue && teacherId.Value > 0) ? teacherId.Value : (int?)null;

            // Role defaults (used only when no explicit filter for that role)
            bool applyDefaultManager = (userType == UserTypesEnum.Manager) && !mId.HasValue;
            bool applyDefaultTeacher = (userType == UserTypesEnum.Teacher) && !tId.HasValue;
            bool applyStudentRestriction = (userType == UserTypesEnum.Student);

            // Predicate
            Expression<Func<Circle, bool>> combinedExpr = c =>
                // text search
                (searchWordLower == null || (c.Name != null && c.Name.ToLower().Contains(searchWordLower)))
                &&
                // role restrictions
                (!applyStudentRestriction || (me.CircleId != null && c.Id == me.CircleId.Value))
                &&
                (!applyDefaultManager || c.ManagerCircles.Any(mc => mc.ManagerId == userId))
                &&
                (!applyDefaultTeacher || c.TeacherId == userId)
                &&
                // explicit filters from query
                (!mId.HasValue || c.ManagerCircles.Any(mc => mc.ManagerId == mId.Value))
                &&
                (!tId.HasValue || c.TeacherId == tId.Value);

            // Includes
            var includes = new List<Expression<Func<Circle, object>>>
    {
        c => c.ManagerCircles
    };
            if (userType != UserTypesEnum.Student)
                includes.Add(c => c.Users);

            // Page
            var page = GetPagedList<CircleDto, Circle, int>(
                pagedDto,
                repository: _circleRepository,
                orderExpression: x => x.Id,
                searchExpression: combinedExpr,
                sortDirection: "DESC",
                disableFilter: true,
                excluededColumns: null,
                includeProperties: includes.ToArray()
            );

            // Post-shape Students per effective role
            switch (userType)
            {
                case UserTypesEnum.Manager:
                    {
                        var effectiveManagerId = mId ?? userId; // use query managerId if given, else current user
                        foreach (var c in page.Items)
                            if (c.Students != null)
                                c.Students = c.Students.Where(s => s.ManagerId == effectiveManagerId).ToList();
                        break;
                    }
                case UserTypesEnum.Teacher:
                    {
                        var effectiveTeacherId = tId ?? userId; // use query teacherId if given, else current user
                        foreach (var c in page.Items)
                            if (c.Students != null)
                                c.Students = c.Students.Where(s => s.TeacherId == effectiveTeacherId).ToList();
                        break;
                    }
                case UserTypesEnum.Student:
                    {
                        foreach (var c in page.Items)
                        {
                            c.Students = new List<UserReturnDto>();
                            if (me.ManagerId.HasValue && c.Managers != null)
                                c.Managers = c.Managers
                                    .Where(m => m.ManagerId == me.ManagerId.Value && m.CircleId == (me.CircleId ?? 0))
                                    .ToList();
                        }
                        break;
                    }
                    // Admin/others: leave as-is
            }

            return output.CreateResponse(page);
        }



        public async Task<IResponse<bool>> AddAsync(CreateCircleDto model, int userId)
        {
            var output = new Response<bool>();

            // 1) Validate DTO
            var validator = new CircleValidation();
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                return output.AppendErrors(validationResult.Errors);

            // 2) Name unique?
            if (await _circleRepository.AnyAsync(x => x.Name!.Trim().ToLower() == model.Name!.Trim().ToLower()))
                return output.CreateResponse(MessageCodes.NameAlreadyExists);

          

            // 4) Map & create the circle
            var entity = _mapper.Map<CreateCircleDto, Circle>(model);
            entity.CreatedBy = userId;
            entity.CreatedAt = DateTime.UtcNow;
            entity.IsDeleted = false;


            // 4a) Save circle to get the generated Id
            var created = await _circleRepository.AddAsync(entity);
            var User = await _userRepository.GetByIdAsync(userId);
            await _unitOfWork.CommitAsync(); // after this, created.Id is available
            if (User.UserTypeId == (int)UserTypesEnum.Manager)
            {
                model.Managers.Add(userId);
            }
        
            // 5) Add managers (now that we have circle Id)
            if (model.Managers != null && model.Managers.Count > 0)
            {
               

                // Or create directly:
                created.ManagerCircles = model.Managers
                    .Distinct()
                    .Select(mgrId => new ManagerCircle
                    {
                        CircleId = created.Id,
                        ManagerId = mgrId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId
                    })
                    .ToList();
            }

            // 6) Link students to this circle (set Users.CircleId = created.Id)
            if (model.StudentsIds != null && model.StudentsIds.Count > 0)
            {
                
                var students =  _userRepository
                    .Where(u =>
                        model.StudentsIds.Contains(u.Id) &&
                        !u.IsDeleted  &&
                u.UserTypeId == (int)UserTypesEnum.Student);


                foreach (var s in students)
                {
                    s.CircleId = created.Id;
                    s.ModefiedAt = DateTime.UtcNow;
                    s.ModefiedBy = userId;
                }
            }

            // 7) Persist managers + students updates
            await _unitOfWork.CommitAsync();

            // await _unitOfWork.CommitTransactionAsync();

            return output.CreateResponse(data: true);
        }

        public async Task<IResponse<bool>> Update(UpdateCircleDto dto, int userId)
        {
            var output = new Response<bool>();
            var validator = new CircleUpdateValidation();
            var vr = validator.Validate(dto);
            if (!vr.IsValid) return output.AppendErrors(vr.Errors);

            bool nameExists = await _circleRepository.AnyAsync(
                x => x.Name!.Trim().ToLower() == dto.Name!.Trim().ToLower() && x.Id != dto.Id);
            if (nameExists) return output.CreateResponse(MessageCodes.NameAlreadyExists);

            var entity = _circleRepository.GetById(dto.Id);
            if (entity == null) return output.AppendError(MessageCodes.NotFound);
            var User = await _userRepository.GetByIdAsync(userId);

            // Update managers (replace all with incoming if provided)
            if (dto.Managers != null && dto.Managers.Count > 0)
            {
                

                // Clean & distinct (ignore zeros if they sometimes sneak in)
                var targetIds = dto.Managers.Where(id => id > 0).Distinct().ToList();
                var targetSet = new HashSet<int>(targetIds);

                // Delete existing links for this circle where ManagerId is in dto.Managers
                if (entity.ManagerCircles?.Any() == true)
                {
                    var toDelete = entity.ManagerCircles
                       .Where(x => x.CircleId == dto.Id && targetSet.Contains(x.ManagerId.Value))
                       .ToList();
                    if (User.UserTypeId == (int)UserTypesEnum.Admin)
                    {
                         toDelete = entity.ManagerCircles
                        .Where(x => x.CircleId == dto.Id )
                        .ToList();
                    }
                   

                    if (toDelete.Count > 0)
                        _managerCircleRepository.DeleteRange(toDelete);
                }

                // Recreate links for provided managers
                if (targetIds.Count > 0)
                    _managerCircleRepository.Add(addCirclesManagers(dto.Id, targetIds,userId));
            }

            // ===== Students sync =====
            // If dto.StudentsIds == null => leave students untouched.
            // If dto.StudentsIds is provided (even empty) => treat as the new source of truth.
            if (dto.StudentsIds != null)
            {
                var incomingIds = dto.StudentsIds.Distinct().ToList();

                var currentStudents =  _userRepository.Where(u =>
                    u.CircleId == dto.Id && !u.IsDeleted &&
                    u.UserTypeId == (int)UserTypesEnum.Student);

                var currentIds = currentStudents.Select(s => s.Id).ToList();

                var toAdd = incomingIds.Except(currentIds).ToList();
                var toRemove = currentIds.Except(incomingIds).ToList();

                if (toAdd.Count > 0)
                {
                    var addStudents =  _userRepository.Where(u =>
                        toAdd.Contains(u.Id) && !u.IsDeleted &&
                        u.UserTypeId == (int)UserTypesEnum.Student);

                    foreach (var s in addStudents)
                    {
                        s.CircleId = dto.Id;
                        s.ModefiedAt = DateTime.UtcNow;
                        s.ModefiedBy = userId;
                    }
                }

                foreach (var s in currentStudents.Where(s => toRemove.Contains(s.Id)))
                {
                    s.CircleId = null;
                    s.ModefiedAt = DateTime.UtcNow;
                    s.ModefiedBy = userId;
                }   
            
        }

            // Update circle fields
            entity.ModefiedAt = DateTime.UtcNow;
            entity.ModefiedBy = userId;
            _mapper.Map(dto, entity);

            await _unitOfWork.CommitAsync();
            return output.CreateResponse(data: true);
        }

       

        public async Task<IResponse<bool>> DeleteAsync(int id ,int userId)
        {
            Response<bool> output = new Response<bool>();

            Circle entity = await _circleRepository.GetByIdAsync(id);
            if (entity == null)
                return output.AppendError(MessageCodes.NotFound);
            entity.ModefiedBy = userId;
            entity.ModefiedAt = DateTime.UtcNow;
            entity.IsDeleted = true;
            await _unitOfWork.CommitAsync();
            return output.CreateResponse(data: true);

        }
        private List<ManagerCircle> addCirclesManagers(int circleId, List<int>? managerIdS,int userId)
        {
            List<ManagerCircle> ManagerCirclelist = new List<ManagerCircle>();
            foreach (var managerId in managerIdS!)
            {
                ManagerCirclelist.Add(new ManagerCircle
                {
                    CircleId = circleId,
                    ManagerId = managerId,
                    ModefiedAt = DateTime.UtcNow,
                    ModefiedBy= userId
                    
                });
            }
            return ManagerCirclelist;
        }
    }





    }
