using AutoMapper;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.Helpers;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Enums;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.SubscribeDtos;
using Orbits.GeneralProject.Repositroy.Base;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private readonly IRepository<User> _UserRepository;
        private readonly IRepository<Nationality> _NationalityRepository;
        private readonly IUnitOfWork _unitOfWork;
        public SubscribeBLL(IMapper mapper, IRepository<Subscribe> SubscribeRepository, IUnitOfWork unitOfWork, IRepository<SubscribeType> subscribeTypeRepository, IRepository<StudentSubscribe> studentSubscribeRepository, IRepository<User> userRepository, IRepository<Nationality> nationalityRepository) : base(mapper)
        {
            _mapper = mapper;
            _SubscribeRepository = SubscribeRepository;
            _unitOfWork = unitOfWork;
            _SubscribeTypeRepository = subscribeTypeRepository;
            _StudentSubscribeRepository = studentSubscribeRepository;
            _UserRepository = userRepository;
            _NationalityRepository = nationalityRepository;
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
            SubscribeTypeCategory? requiredCategory = null;

            var filters = TryDeserializeFilters(pagedDto.Filter);
            int? studentIdFromFilters = ExtractIntFilterValue(filters, "StudentId", "studentId");
            int? residentIdFromFilters = ExtractIntFilterValue(filters, "ResidentId", "residentId");
            int? nationalityIdFromFilters = ExtractIntFilterValue(filters, "NationalityId", "nationalityId");

            int? studentId = pagedDto.StudentId ?? studentIdFromFilters;
            int? residentId = pagedDto.ResidentId ?? residentIdFromFilters;
            residentId ??= nationalityIdFromFilters;

            if (filters != null)
                pagedDto.Filter = filters.Count > 0 ? JsonConvert.SerializeObject(filters) : null;

            if (!residentId.HasValue && studentId.HasValue && studentId.Value > 0)
            {
                var student = _UserRepository.GetById(studentId.Value);
                if (student?.ResidentId != null && student.ResidentId.Value > 0)
                    residentId = student.ResidentId.Value;
                else if (student?.NationalityId != null && student.NationalityId.Value > 0)
                    residentId = student.NationalityId.Value;
            }

            if (residentId.HasValue && residentId.Value > 0)
            {
                var resident = _NationalityRepository.GetById(residentId.Value);
                requiredCategory = ResolveSubscribeTypeCategory(resident);
            }

            int? requiredGroupValue = requiredCategory.HasValue ? (int?)requiredCategory.Value : null;

            var list = GetPagedList<SubscribeTypeReDto, SubscribeType, int>(
                pagedDto,
                repository: _SubscribeTypeRepository,
                x => x.Id,
                searchExpression: x =>
                    (string.IsNullOrEmpty(searchWord) ||
                        (!string.IsNullOrEmpty(x.Name) && x.Name.Contains(searchWord))) &&
                    (requiredGroupValue == null || x.Group == requiredGroupValue),
                sortDirection: pagedDto.SortingDirection,
                disableFilter: true,
                excluededColumns: null);
            return output.CreateResponse(list);
        }

        public async Task<IResponse<SubscribeTypeStatisticsDto>> GetTypeStatisticsAsync()
        {
            Response<SubscribeTypeStatisticsDto> output = new();

            try
            {
                var subscribeTypes = await _SubscribeTypeRepository
                    .GetAll(true)
                    .AsNoTracking()
                    .Where(type => type.IsDeleted != true)
                    .Select(type => new
                    {
                        type.Id,
                        Name = (type.Name ?? string.Empty).Trim()
                    })
                    .ToListAsync();

                var activeTypeIds = subscribeTypes
                    .Select(type => type.Id)
                    .ToHashSet();

                var studentSubscriptions = await _StudentSubscribeRepository
                    .GetAll(true)
                    .AsNoTracking()
                    .Where(subscription => subscription.StudentSubscribeTypeId.HasValue)

                    .Select(subscription => new
                    {
                        TypeId = subscription.StudentSubscribeTypeId!.Value,
                        subscription.StudentId
                    })
                    .ToListAsync();

                var filteredSubscriptions = studentSubscriptions
                    .Where(subscription => activeTypeIds.Contains(subscription.TypeId))
                    .ToList();

                var groupedSubscriptions = filteredSubscriptions
                    .GroupBy(subscription => subscription.TypeId)
                    .ToDictionary(
                        group => group.Key,
                        group => new
                        {
                            SubscriptionCount = group.Count(),
                            UniqueStudentCount = group
                                .Select(subscription => subscription.StudentId)
                                .Where(studentId => studentId.HasValue)
                                .Select(studentId => studentId!.Value)
                                .Distinct()
                                .Count()
                        });

                int totalSubscriptions = groupedSubscriptions
                    .Values
                    .Sum(group => group.SubscriptionCount);

                int totalUniqueSubscribers = filteredSubscriptions
                    .Select(subscription => subscription.StudentId)
                    .Where(studentId => studentId.HasValue)
                    .Select(studentId => studentId!.Value)
                    .Distinct()
                    .Count();

                decimal CalculatePercentage(int value, int total) => total == 0
                    ? 0m
                    : Math.Round((decimal)value / total * 100m, 2, MidpointRounding.AwayFromZero);

                var breakdown = subscribeTypes
                    .Select(type =>
                    {
                        groupedSubscriptions.TryGetValue(type.Id, out var counts);

                        int subscriberCount = counts?.SubscriptionCount ?? 0;
                        string displayName = string.IsNullOrWhiteSpace(type.Name) ? "غير محدد" : type.Name;

                        return new SubscribeTypeBreakdownItemDto
                        {
                            SubscribeTypeId = type.Id,
                            TypeName = displayName,
                            SubscriberCount = subscriberCount,
                            Percentage = CalculatePercentage(subscriberCount, totalSubscriptions)
                        };
                    })
                    .OrderByDescending(item => item.SubscriberCount)
                    .ThenBy(item => item.TypeName)
                    .ToList();

                SubscribeTypeDistributionDto distribution = new()
                {
                    TotalValue = totalSubscriptions,
                    Slices = breakdown
                        .Select(item => new SubscribeTypeDistributionSliceDto
                        {
                            Label = item.TypeName,
                            Value = item.SubscriberCount,
                            Percentage = item.Percentage
                        })
                        .ToList()
                };

                SubscribeTypeStatisticsDto statistics = new()
                {
                    Distribution = distribution,
                    Breakdown = breakdown,
                    TotalSubscribers = totalSubscriptions,
                    UniqueSubscribers = totalUniqueSubscribers,
                    TotalSubscriptionTypes = subscribeTypes.Count
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

        private static Dictionary<string, FilterValue>? TryDeserializeFilters(string? serializedFilters)
        {
            if (string.IsNullOrWhiteSpace(serializedFilters))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, FilterValue>>(serializedFilters);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static int? ExtractIntFilterValue(Dictionary<string, FilterValue>? filters, params string[] keys)
        {
            if (filters == null || filters.Count == 0 || keys == null || keys.Length == 0)
                return null;

            foreach (var pair in filters.ToList())
            {
                var normalizedKey = NormalizeFilterKey(pair.Key);
                if (keys.Any(expected => KeyMatches(normalizedKey, expected)))
                {
                    filters.Remove(pair.Key);

                    if (int.TryParse(pair.Value?.Value, out int parsedValue))
                        return parsedValue;

                    return null;
                }
            }

            return null;
        }

        private static string NormalizeFilterKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Replace("-R2-", string.Empty);
        }

        private static bool KeyMatches(string key, string expected)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(expected))
                return false;

            if (key.Equals(expected, StringComparison.OrdinalIgnoreCase))
                return true;

            var segments = key.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 0 && segments[^1].Equals(expected, StringComparison.OrdinalIgnoreCase))
                return true;

            return key.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static SubscribeTypeCategory? ResolveSubscribeTypeCategory(Nationality? nationality)
        {
            var subscribeFor = NationalityClassificationHelper.ResolveSubscribeFor(nationality);

            return subscribeFor switch
            {
                SubscribeForEnum.Egyptian => SubscribeTypeCategory.Egyptian,
                SubscribeForEnum.Gulf => SubscribeTypeCategory.Arab,
                SubscribeForEnum.NonArab => SubscribeTypeCategory.Foreign,
                _ => null
            };
        }

    }
}
