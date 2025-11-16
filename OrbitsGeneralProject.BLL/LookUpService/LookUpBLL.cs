using AutoMapper;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.Helpers;
using Orbits.GeneralProject.BLL.LookUpService;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Enums;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.DTO.LockUpDtos;
using Orbits.GeneralProject.DTO.ManagerDto;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.RegionDtos;
using Orbits.GeneralProject.Repositroy.Base;
using System.Linq;
using System.Linq.Expressions;

namespace Orbits.GeneralProject.BLL.LookUpService
{
    public class LookUpBLL : BaseBLL, ILookUpBLL
    {
        private readonly IMapper _mapper;
        private readonly IRepository<User> _UserRepo;
        private readonly IRepository<Nationality> _nationalityRepo;
        private readonly IRepository<Governorate> _governorateRepo;
        private readonly IRepository<Subscribe> _SubscribeRepo;

        public LookUpBLL(IMapper mapper, IRepository<User> UserRepo, IRepository<Nationality> nationalityRepo, IRepository<Governorate> governorateRepo, IRepository<Subscribe> subscribeRepo) : base(mapper)
        {
            _mapper = mapper;
            _UserRepo = UserRepo;
            _nationalityRepo = nationalityRepo;
            _governorateRepo = governorateRepo;
            _SubscribeRepo = subscribeRepo;
        }

        public IResponse<PagedResultDto<UserLockUpDto>> GetUsersByUserType(
     FilteredResultRequestDto pagedDto, int userTypeId, int userId)
        {
            var output = new Response<PagedResultDto<UserLockUpDto>>();
            var searchWord = pagedDto.SearchTerm?.Trim();
            var me = _UserRepo.GetById(userId);
            if (me == null) return output.AppendError(MessageCodes.NotFound);

            var sw = searchWord?.ToLower();

            // Build ONE predicate that includes:
            // - target user type (userTypeId)
            // - role-based restrictions (branch/manager/teacher)
            // - optional text search
            Expression<Func<User, bool>> predicate = x =>
                x.UserTypeId == userTypeId
                // role-based restriction (applies only when the logged-in role matches)
                && (!(me.UserTypeId == (int)UserTypesEnum.BranchLeader) || x.BranchId == me.BranchId)
                && (!(me.UserTypeId == (int)UserTypesEnum.Manager) || x.ManagerId == me.Id)
                && (!(me.UserTypeId == (int)UserTypesEnum.Teacher) || x.TeacherId == me.Id)
                // optional search (grouped to avoid &&/|| precedence issues)
                && (
                    string.IsNullOrEmpty(sw) ||
                    (x.FullName != null && x.FullName.ToLower().Contains(sw)) ||
                    (x.Mobile != null && x.Mobile.ToLower().Contains(sw)) ||
                    (x.Email != null && x.Email.ToLower().Contains(sw)) ||
                    (x.Nationality != null && x.Nationality.Name != null && x.Nationality.Name.ToLower().Contains(sw)) ||
                    (x.Resident != null && x.Resident.Name != null && x.Resident.Name.ToLower().Contains(sw)) ||
                    (x.Governorate != null && x.Governorate.Name != null && x.Governorate.Name.ToLower().Contains(sw))
                );

            // IMPORTANT: pass the predicate to GetPagedList so filtering happens before paging
                var paged = GetPagedList<UserLockUpDto, User, int>(
                pagedDto,
                _UserRepo,
                x => x.Id,               // positional key selector
                searchExpression: predicate,
                sortDirection: "DESC",
                disableFilter: true,
                excluededColumns: null
            );

            //// If you want NotFound when there are no items:
            //if (paged == null || paged.Items == null || paged.Items.Count == 0)
            //    return output.CreateResponse(MessageCodes.NotFound);

            return output.CreateResponse(paged);
        }


        public async Task<IResponse<object>> GetAllNationality()
        {
            var output = new Response<object>();
            List<RegionDto> result = _nationalityRepo.GetAll()
                .Select(x => new RegionDto()
                {
                    Name = x.Name,
                    TelCode = x.TelCode,
                    Id = x.Id,
                }).ToList();
            return output.CreateResponse(result);
        }

        public async Task<IResponse<object>> GetAllGovernorate()
        {
            var output = new Response<object>();
            List<LookupDto> result = _governorateRepo.GetAll()
                .Select(x => new LookupDto()
                {
                    Name = x.Name,
                    Id = x.Id,
                }).ToList();
            return output.CreateResponse(result);
        }



        public async Task<IResponse<List<LookupDto>>> GetAllSubscribesByTypeId(int? id, int? studentId)
        {
            var output = new Response<List<LookupDto>>();
            IQueryable<Subscribe> query = _SubscribeRepo.Where(e => e.SubscribeTypeId == id);

            if (studentId.HasValue && studentId.Value > 0)
            {
                var student = _UserRepo.GetById(studentId.Value);
                if (student != null)
                {
                    Nationality? nationality = student.Nationality;
                    if (nationality == null && student.NationalityId.HasValue)
                    {
                        nationality = _nationalityRepo.GetById(student.NationalityId.Value);
                    }

                    var subscribeFor = NationalityClassificationHelper.ResolveSubscribeFor(nationality);
                    if (subscribeFor.HasValue)
                    {
                        SubscribeTypeCategory? targetCategory = subscribeFor.Value switch
                        {
                            SubscribeForEnum.Egyptian => SubscribeTypeCategory.Egyptian,
                            SubscribeForEnum.Gulf => SubscribeTypeCategory.Arab,
                            SubscribeForEnum.NonArab => SubscribeTypeCategory.Foreign,
                            _ => null
                        };

                        if (targetCategory.HasValue)
                        {
                            int groupValue = (int)targetCategory.Value;
                            query = query.Where(e => e.SubscribeType != null && e.SubscribeType.Group == groupValue);
                        }
                    }
                }
            }

            List<LookupDto> result = query
                .Select(x => new LookupDto
                {
                    Name = x.Name,
                    Id = x.Id
                }).ToList();
            return output.CreateResponse(result);
        }
    }
}
