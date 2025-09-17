using AutoMapper;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.BLL.Validation.UserValidation;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.ManagerDto;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.UserDto;
using Orbits.GeneralProject.DTO.UserDtos;
using Orbits.GeneralProject.Repositroy.Base;
using System.Linq.Expressions;

namespace Orbits.GeneralProject.BLL.ManagerService
{
    public class ManagerBLL : BaseBLL, IManagerBLL
    {
        private readonly IRepository<User> _userRepository;
  
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public ManagerBLL(IMapper mapper, IRepository<User> userrepository,
             IUnitOfWork unitOfWork,
             IHostEnvironment hostEnvironment) : base(mapper)
        {
            _userRepository = userrepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
           
        }

        public IResponse<PagedResultDto<ManagerDto>> GetPagedList(FilteredResultRequestDto pagedDto, int userId)
        {
            var output = new Response<PagedResultDto<ManagerDto>>();

            var searchWord = pagedDto.SearchTerm?.Trim();
            var me = _userRepository.Where(u => u.Id == userId)
                                    .Select(u => new { u.Id, u.UserTypeId, u.BranchId })
                                    .FirstOrDefault();
            if (me == null) return output.AppendError(MessageCodes.NotFound);

            bool isBranchLeader = me.UserTypeId == (int)UserTypesEnum.BranchLeader;
            int? branchId = isBranchLeader ? me.BranchId : null;

            // Base filter: only managers (+ same branch if BranchLeader)
            // Optional search across fields (case-insensitive) when searchWord provided
            Expression<Func<User, bool>> predicate = x =>
                x.UserTypeId == (int)UserTypesEnum.Manager &&
                (!isBranchLeader || x.BranchId == branchId) &&
                (string.IsNullOrEmpty(searchWord) ||
                    (x.FullName != null && x.FullName.ToLower().Contains(searchWord.ToLower())) ||
                    (x.Mobile != null && x.Mobile.ToLower().Contains(searchWord.ToLower())) ||
                    (x.Email != null && x.Email.ToLower().Contains(searchWord.ToLower())) ||
                    (x.Nationality != null && x.Nationality.Name != null &&
                     x.Nationality.Name.ToLower().Contains(searchWord.ToLower()))
                );

            var list = GetPagedList<ManagerDto, User, int>(
                pagedDto,
                repository: _userRepository,
                x => x.Id, // here it was just a positional lambda
                searchExpression: predicate,
                sortDirection: "DESC",
                disableFilter: true,
                excluededColumns: null
            );

            return output.CreateResponse(list);
        }
    }
}


