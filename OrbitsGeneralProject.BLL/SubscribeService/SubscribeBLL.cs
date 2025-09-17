using AutoMapper;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
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
        private readonly IRepository<StudentSubscribe> _StudentSubscribeRepository;
        private readonly IUnitOfWork _unitOfWork;
        public SubscribeBLL(IMapper mapper, IRepository<Subscribe> SubscribeRepository, IUnitOfWork unitOfWork, IRepository<SubscribeType> subscribeTypeRepository, IRepository<StudentSubscribe> studentSubscribeRepository) : base(mapper)
        {
            _mapper = mapper;
            _SubscribeRepository = SubscribeRepository;
            _unitOfWork = unitOfWork;
            _SubscribeTypeRepository = subscribeTypeRepository;
            _StudentSubscribeRepository = studentSubscribeRepository;
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

        public async Task<IResponse<SubscribeTypeStatisticsDto>> GetTypeStatisticsAsync()
        {
            Response<SubscribeTypeStatisticsDto> output = new();

            try
            {
                var studentSubscribesQuery = _StudentSubscribeRepository
                    .GetAll(true)
                    .Where(x => x.StudentSubscribeTypeId.HasValue);

                var subscriptions = await studentSubscribesQuery
                    .Select(x => new
                    {
                        x.StudentId,
                        x.StudentSubscribeTypeId,
                        SubscribeTypeName = x.StudentSubscribeType != null ? x.StudentSubscribeType.Name : null,
                        SubscribeName = x.StudentSubscribeNavigation != null ? x.StudentSubscribeNavigation.Name : null
                    })
                    .ToListAsync();

                var normalizedData = subscriptions
                    .Select(entry =>
                    {
                        string typeName = entry.SubscribeTypeName ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(typeName))
                        {
                            typeName = entry.SubscribeName ?? string.Empty;
                        }

                        if (string.IsNullOrWhiteSpace(typeName))
                        {
                            typeName = "Uncategorized";
                        }

                        return new
                        {
                            entry.StudentId,
                            TypeId = entry.StudentSubscribeTypeId,
                            TypeName = typeName
                        };
                    })
                    .ToList();

                var breakdown = normalizedData
                    .GroupBy(entry => new { entry.TypeId, entry.TypeName })
                    .Select(group => new SubscribeTypeStatisticItemDto
                    {
                        SubscribeTypeId = group.Key.TypeId,
                        Name = group.Key.TypeName,
                        SubscriptionCount = group.Count(),
                        UniqueStudentCount = group
                            .Select(entry => entry.StudentId)
                            .Where(id => id.HasValue)
                            .Select(id => id!.Value)
                            .Distinct()
                            .Count()
                    })
                    .OrderByDescending(item => item.SubscriptionCount)
                    .ToList();

                int totalSubscriptions = breakdown.Sum(item => item.SubscriptionCount);
                int uniqueSubscribers = normalizedData
                    .Select(entry => entry.StudentId)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .Distinct()
                    .Count();

                foreach (var item in breakdown)
                {
                    item.Percentage = totalSubscriptions == 0
                        ? 0m
                        : Math.Round((decimal)item.SubscriptionCount / totalSubscriptions * 100m, 2, MidpointRounding.AwayFromZero);
                }

                SubscribeTypeStatisticsDto statistics = new()
                {
                    Labels = breakdown.Select(item => item.Name).ToList(),
                    Series = breakdown.Select(item => item.SubscriptionCount).ToList(),
                    Items = breakdown,
                    TotalSubscriptions = totalSubscriptions,
                    UniqueSubscribers = uniqueSubscribers
                };

                return output.CreateResponse(statistics);
            }
            catch (Exception ex)
            {
                return output.CreateResponse(ex);
            }
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
