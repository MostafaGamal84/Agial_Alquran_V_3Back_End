using AutoMapper;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.BLL.Validation.CircleValidation;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Enums;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.CircleDto;
using Orbits.GeneralProject.DTO.LockUpDtos;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.StudentSubscribDtos;
using Orbits.GeneralProject.Repositroy.Base;
using System.Linq.Expressions;
using Twilio.TwiML.Voice;

namespace Orbits.GeneralProject.BLL.StudentSubscribeService
{
    public class StudentSubscribeBLL : BaseBLL, IStudentSubscribeBLL
    {
        private readonly IMapper _mapper;
        private readonly IRepository<User> _UserRepo;
        private readonly IRepository<StudentSubscribe> _StudentSubscribeRepo;
        private readonly IRepository<StudentPayment> _StudentPaymentRepo;
        private readonly IRepository<Subscribe> _SubscribeRepo;
        private readonly IRepository<SubscribeType> _SubscribeTypeRepo;
        private readonly IUnitOfWork _unitOfWork;


        public StudentSubscribeBLL(IMapper mapper, IRepository<User> UserRepo, IRepository<StudentSubscribe> studentSubscribeRepo, IRepository<Subscribe> subscribeRepo, IRepository<SubscribeType> subscribeTypeRepo, IRepository<StudentPayment> studentPaymentRepo, IUnitOfWork unitOfWork) : base(mapper)
        {
            _mapper = mapper;
            _UserRepo = UserRepo;
            _StudentSubscribeRepo = studentSubscribeRepo;
            _SubscribeRepo = subscribeRepo;
            _SubscribeTypeRepo = subscribeTypeRepo;
            _StudentPaymentRepo = studentPaymentRepo;
            _unitOfWork = unitOfWork;
        }




        public IResponse<PagedResultDto<ViewStudentSubscribeReDto>> GetStudents(
     FilteredResultRequestDto pagedDto, int userId,int? studentId, int? nationalityId)
        {
            var output = new Response<PagedResultDto<ViewStudentSubscribeReDto>>();
            var searchWord = pagedDto.SearchTerm?.Trim();
            var me = _UserRepo.GetById(userId);
            if (me == null) return output.AppendError(MessageCodes.NotFound);

            var sw = searchWord?.ToLower();

            // Build ONE predicate that includes:
            // - target user type (userTypeId)
            // - role-based restrictions (branch/manager/teacher)
            // - optional text search
            Expression<Func<User, bool>> predicate = x =>
                x.UserTypeId == (int)UserTypesEnum.Student
                // role-based restriction (applies only when the logged-in role matches)
                && (!(studentId.HasValue && studentId.Value > 0) || x.Id == studentId.Value)
                && (!(nationalityId.HasValue && nationalityId.Value > 0) || x.NationalityId == nationalityId.Value)
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
                    (x.Governorate != null && x.Governorate.Name != null && x.Governorate.Name.ToLower().Contains(sw))
                );

            // IMPORTANT: pass the predicate to GetPagedList so filtering happens before paging
            var paged = GetPagedList<ViewStudentSubscribeReDto, User, int>(
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

        public IResponse<PagedResultDto<ViewStudentSubscribeReDto>> GetStudentSubscribesWithPayment(
    FilteredResultRequestDto pagedDto,  int? studentId, int? nationalityId)
        {
            var output = new Response<PagedResultDto<ViewStudentSubscribeReDto>>();
            var searchWord = pagedDto.SearchTerm?.Trim();

            var sw = searchWord?.ToLower();

            // Build ONE predicate that includes:
            // - target user type (userTypeId)
            // - role-based restrictions (branch/manager/teacher)
            // - optional text search
            Expression<Func<StudentSubscribe, bool>> predicate = x =>
                x.StudentPaymentId != null
               
                // role-based restriction (applies only when the logged-in role matches)
                && (!(studentId.HasValue && studentId.Value > 0) || x.StudentId == studentId.Value)
                && (!(nationalityId.HasValue && nationalityId.Value > 0) || (x.Student != null && x.Student.NationalityId == nationalityId.Value))
               
                // optional search (grouped to avoid &&/|| precedence issues)
                && (
                    string.IsNullOrEmpty(sw) 
                    //(x.FullName != null && x.FullName.ToLower().Contains(sw)) ||
                    //(x.Mobile != null && x.Mobile.ToLower().Contains(sw)) ||
                    //(x.Email != null && x.Email.ToLower().Contains(sw)) ||
                    //(x.Nationality != null && x.Nationality.Name != null && x.Nationality.Name.ToLower().Contains(sw)) ||
                    //(x.Governorate != null && x.Governorate.Name != null && x.Governorate.Name.ToLower().Contains(sw))
                );

            // IMPORTANT: pass the predicate to GetPagedList so filtering happens before paging
            var paged = GetPagedList<ViewStudentSubscribeReDto, StudentSubscribe, int>(
                pagedDto,
                _StudentSubscribeRepo,
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
        public async Task<IResponse<bool>> AddAsync(AddStudentSubscribeDto model, int userId)
        {
            var output = new Response<bool>();
            var subscribe = _SubscribeRepo.GetById(model.StudentSubscribeId.Value);
            if (subscribe == null)
            {
                return output.AppendError(MessageCodes.NotFound);
            }
            var student = _UserRepo.GetById(model.StudentId.Value);
            var subscribeGroup = ResolveSubscribeGroup(subscribe);
            if (!subscribeGroup.HasValue)
            {
                return output.AppendError(
                    MessageCodes.InputValidationError,
                    nameof(AddStudentSubscribeDto.StudentSubscribeId),
                    "Subscribe type group is not configured for this plan.");
            }

            var (Amount, Currency) = ResolvePaymentDetails(subscribe, subscribeGroup.Value);
            var studentPayment = new StudentPayment
            {
                CreatedAt = DateTime.Now,
                CreatedBy = userId,
                StudentId = model.StudentId.Value,
                StudentSubscribeId = model.StudentSubscribeId.Value,
                Amount = Amount,
                CurrencyId = Currency,
                PayStatue = false,
                IsCancelled = false,
            };
            var studentPaymentAdd = await _StudentPaymentRepo.AddAsync(studentPayment);
            await _unitOfWork.CommitAsync(); 

            var studentSubscribe = new StudentSubscribe
            {
                CreatedAt = DateTime.Now,
                CreatedBy = userId,
                StudentId = model.StudentId.Value,
                StudentSubscribeId = model.StudentSubscribeId.Value,
                RemainingMinutes = subscribe.Minutes,
                StudentSubscribeTypeId = subscribe.SubscribeTypeId,
                PayStatus = false,
                StudentPaymentId = studentPaymentAdd.Id
            };

            // 4a) Save circle to get the generated Id
            var studentSubscribeAdd = await _StudentSubscribeRepo.AddAsync(studentSubscribe);
            await _unitOfWork.CommitAsync(); // after this, created.Id is available
           
                return output.CreateResponse(data: true);
        }

        private SubscribeTypeCategory? ResolveSubscribeGroup(Subscribe subscribe)
        {
            var category = ConvertGroup(subscribe.SubscribeType?.Group);
            if (category.HasValue)
            {
                return category;
            }

            if (!subscribe.SubscribeTypeId.HasValue)
            {
                return null;
            }

            var subscribeType = _SubscribeTypeRepo.GetById(subscribe.SubscribeTypeId.Value);
            return ConvertGroup(subscribeType?.Group);
        }

        private static SubscribeTypeCategory? ConvertGroup(int? groupValue)
            => groupValue.HasValue ? (SubscribeTypeCategory?)groupValue.Value : null;

        private static (int Amount, int Currency) ResolvePaymentDetails(Subscribe subscribe, SubscribeTypeCategory group)
        {
            return group switch
            {
                SubscribeTypeCategory.Egyptian => ((int)subscribe.Leprice, (int)CurrencyEnum.LE),
                SubscribeTypeCategory.Arab => ((int)subscribe.Sarprice, (int)CurrencyEnum.SAR),
                SubscribeTypeCategory.Foreign => ((int)subscribe.Usdprice, (int)CurrencyEnum.USD),
                _ => throw new ArgumentOutOfRangeException(nameof(group), group, "Unsupported subscription group")
            };
        }

    }
}
