using AutoMapper;
using FluentValidation.Results;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.SubscribeDtos;
using Orbits.GeneralProject.Repositroy.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.BLL.SubscribeService
{
    public class SubscribeBLL : BaseBLL, ISubscribeBLL
    {
        private readonly IMapper _mapper;
        private readonly IRepository<Subscribe> _SubscribeRepository;
        private readonly IRepository<SubscribeType> _SubscribeTypeRepository;
        private readonly IUnitOfWork _unitOfWork;
        public SubscribeBLL(IMapper mapper, IRepository<Subscribe> SubscribeRepository, IUnitOfWork unitOfWork, IRepository<SubscribeType> subscribeTypeRepository) : base(mapper)
        {
            _mapper = mapper;
            _SubscribeRepository = SubscribeRepository;
            _unitOfWork = unitOfWork;
            _SubscribeTypeRepository = subscribeTypeRepository;
        }
        public IResponse<PagedResultDto<SubscribeReDto>> GetPagedList(FilteredResultRequestDto pagedDto)
        {
            var searchWord = pagedDto.SearchTerm?.ToLower().Trim();
            var output = new Response<PagedResultDto<SubscribeReDto>>();
            var list = GetPagedList<SubscribeReDto, Subscribe, int>(pagedDto, repository: _SubscribeRepository, x => x.Id, searchExpression: x =>
                string.IsNullOrEmpty(searchWord) ||
                (!string.IsNullOrEmpty(searchWord) && x.Name.Contains(searchWord)), sortDirection: pagedDto.SortingDirection,
              disableFilter: true,
              excluededColumns: null);
            return output.CreateResponse(list);
        }
        public IResponse<PagedResultDto<SubscribeTypeReDto>> GetTypeResultsByFilter(FilteredResultRequestDto pagedDto)
        {
            var searchWord = pagedDto.SearchTerm?.ToLower().Trim();
            var output = new Response<PagedResultDto<SubscribeTypeReDto>>();
            var list = GetPagedList<SubscribeTypeReDto, SubscribeType, int>(pagedDto, repository: _SubscribeTypeRepository, x => x.Id, searchExpression: x =>
                string.IsNullOrEmpty(searchWord) ||
                (!string.IsNullOrEmpty(searchWord) && x.Name.Contains(searchWord)), sortDirection: pagedDto.SortingDirection,
              disableFilter: true,
              excluededColumns: null);
            return output.CreateResponse(list);
        }
        public async Task<IResponse<bool>> AddAsync(CreateSubscribeDto model, int userId)
        {
            Response<bool> output = new Response<bool>();
            Subscribe entity = _mapper.Map<CreateSubscribeDto, Subscribe>(model);
            entity.CreatedBy = userId;
            entity.CreatedAt = DateTime.Now;
            entity.IsDeleted = false;
            await _SubscribeRepository.AddAsync(entity);
            await _unitOfWork.CommitAsync();
            return output.CreateResponse(true);
        }

        public async Task<IResponse<bool>> AddSubscribeTypeAsync(CreateSubscribeTypeDto model, int userId)
        {
            Response<bool> output = new Response<bool>();
            SubscribeType entity = _mapper.Map<CreateSubscribeTypeDto, SubscribeType>(model);
            entity.CreatedBy = userId;
            entity.CreatedAt = DateTime.Now;
            entity.IsDeleted = false;
            await _SubscribeTypeRepository.AddAsync(entity);
            await _unitOfWork.CommitAsync();
            return output.CreateResponse(true);
        }
        public async Task<IResponse<bool>> Update(CreateSubscribeDto dto, int userId)
        {
            Response<bool> output = new Response<bool>();
            
            Subscribe entity = _SubscribeRepository.GetById(dto.Id);
            entity.ModefiedBy = userId;
            entity.ModefiedAt  = DateTime.Now;
            Subscribe result = _mapper.Map(dto, entity);
            await _unitOfWork.CommitAsync();
            return output.CreateResponse(true);
        }

        public async Task<IResponse<bool>> UpdateType(CreateSubscribeTypeDto dto, int userId)
        {
            Response<bool> output = new Response<bool>();

            SubscribeType entity = _SubscribeTypeRepository.GetById(dto.Id);
            entity.ModefiedBy = userId;
            entity.ModefiedAt = DateTime.Now;
            SubscribeType result = _mapper.Map(dto, entity);
            await _unitOfWork.CommitAsync();
            return output.CreateResponse(true);
        }
        public async Task<IResponse<bool>> Delete(int id)
        {
            Response<bool> output = new Response<bool>();
            Subscribe entity = _SubscribeRepository.GetById(id);
            if (entity == null)
                return output.AppendError(MessageCodes.NotFound);
            if (entity.StudentSubscribes.Count > 0)
            {
                return output.AppendError(MessageCodes.FailedToRemoveSubscribe);
            }
            entity.IsDeleted = true;
            _SubscribeRepository.Update(entity);
            await _unitOfWork.CommitAsync();
            return output.CreateResponse(data: true);
        }

        public async Task<IResponse<bool>> DeleteType(int id)
        {
            Response<bool> output = new Response<bool>();
            SubscribeType entity = _SubscribeTypeRepository.GetById(id);
            if (entity == null)
                return output.AppendError(MessageCodes.NotFound);
            if (entity.Subscribes.Count > 0 && entity.Subscribes.Where(x=>x.IsDeleted == false).Any())
            {
                return output.AppendError(MessageCodes.FailedToRemoveSubscribeType);
            }
            entity.IsDeleted = true;
            _SubscribeTypeRepository.Update(entity);
            await _unitOfWork.CommitAsync();
            return output.CreateResponse(data: true);
        }
      
    }
}
