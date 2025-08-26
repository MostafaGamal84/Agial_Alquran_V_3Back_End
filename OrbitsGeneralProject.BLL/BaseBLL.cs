using AutoMapper;

using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.BLL.DBQueriesDto;
using Orbits.GeneralProject.BLL.FilterBuilder;
using Orbits.GeneralProject.BLL.Mapping;
using Orbits.GeneralProject.BLL.StaticEnums;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Extensions;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.Translation;
using Orbits.GeneralProject.Repositroy.Base;
using Orbits.GeneralProject.Repositroy.Enums;

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

using System.Collections;
using System.Linq;
using System.Linq.Expressions;

using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Globalization;

namespace Orbits.GeneralProject.BLL
{
    public enum SystemLangEnum : int
    {
        en = 1,
        ar = 2,
        fr = 3,
    }

    public class FilterValue
    {
        public string Value { get; set; }
        public Operation Operator { get; set; } = Operation.Contains;
    }

    public abstract class BaseBLL
    {
        private readonly IMapper _mapper;

        public BaseBLL(IMapper mapper)
        {
            _mapper = mapper;
        }

        #region Translation Helpers

        protected IQueryable<TSourceEntity> Query<TSourceEntity, TMappedEntity>(IRepository<TSourceEntity> repository, Expression<Func<TSourceEntity, bool>> expression, bool disableFilter = false) where TSourceEntity : class where TMappedEntity : class
        {
            IQueryable<TSourceEntity> query
                = disableFilter
                ? repository.DisableFilter(nameof(DynamicFilters.Inactive)).Where(expression)
                : repository.Where(expression);

            return query;
        }

        protected TMappedEntity? LocalizeSingle<TSourceEntity, TMappedEntity>(IQueryable<TSourceEntity> query, int langId = DXConstants.SupportedLanguage.DefaultLangId) where TSourceEntity : class where TMappedEntity : class
        {
            var x = _mapper.MapLocalized<TSourceEntity, TMappedEntity>(query, langId).FirstOrDefault();
            var y = _mapper.Map<TMappedEntity>(x);
            return y;
        }

        protected TMappedEntity? LocalizeSingle<TSourceEntity, TMappedEntity>(IRepository<TSourceEntity> repository, Expression<Func<TSourceEntity, bool>> expression, bool disableFilter = true, int langId = DXConstants.SupportedLanguage.DefaultLangId) where TSourceEntity : class where TMappedEntity : class
        {
            IQueryable<TSourceEntity> query = Query<TSourceEntity, TMappedEntity>(repository, expression, disableFilter);
            //return ;
            return _mapper.Map<TMappedEntity>(_mapper.MapLocalized<TSourceEntity, TMappedEntity>(query, langId)?.FirstOrDefault());

        }

        protected List<TMappedEntity>? LocalizeMultiple<TSourceEntity, TMappedEntity>(IQueryable<TSourceEntity> query, int langId = DXConstants.SupportedLanguage.DefaultLangId) where TSourceEntity : class where TMappedEntity : class
        {
            return _mapper.MapLocalized<TSourceEntity, TMappedEntity>(query, langId).ToList();
        }

        protected TMappedEntity? LocalizeMultiple<TSourceEntity, TMappedEntity>(IRepository<TSourceEntity> repository, Expression<Func<TSourceEntity, bool>> expression, bool disableFilter = true, int langId = DXConstants.SupportedLanguage.DefaultLangId) where TSourceEntity : class where TMappedEntity : class
        {
            IQueryable<TSourceEntity> query = Query<TSourceEntity, TMappedEntity>(repository, expression, disableFilter);
            return _mapper.MapLocalized<TSourceEntity, TMappedEntity>(query, langId)?.FirstOrDefault();
        }

        #endregion Translation Helpers

        //private List<TranslationDetail> CreateTranslationDetails(List<TranslationDetailInputDto> details, string? fieldName)
        //{
        //    var languages = new List<LanaguageLookupDto>
        //    {
        //         new LanaguageLookupDto
        //         {
        //              Id =1,
        //               Name = "en",
        //                IsDefault = true,
        //                 langId = 1
        //         },
        //                 new LanaguageLookupDto
        //         {
        //              Id =1,
        //               Name = "ar",
        //                IsDefault = false,
        //                 langId = 2
        //         },
        //    };/* GetAllLanaguages();*/
        //    List<TranslationDetail> output = new List<TranslationDetail>();
        //    foreach (TranslationDetailInputDto item in details)
        //    {
        //        output.Add(new TranslationDetail
        //        {
        //            FieldName = fieldName,
        //            LanguageId = item.LanguageId,
        //            Translation = item.Translation,
        //        });
        //    }
        //    return output;
        //}

        #region Add , Update and GetTranslation

        /*protected virtual TEntity AddOrUpdateTranslation<TEntity>(TEntity entity, IRepository<TranslationHeader> translationRepository, IRepository<TranslationDetail> translationDetailsRepository, TranslationHeaderInputDto translationDto) where TEntity : EntityBase
        {
            var tableId = (int)EnumHelper<TableNameEnum>.GetValueFromName(nameof(TEntity));
            if (tableId == 0)
                return null;

            //Get Entity by Id

            //var translationHeaderId = entity.TranslationHeaderId ?? 0;
            var translationHeaderId = (int)entity.GetType().GetProperty("TranslationHeaderId").GetValue(entity);

            TranslationHeader translationEntity;
            //Create
            if (translationHeaderId == 0)
            {
                translationEntity = _mapper.Map<TranslationHeader>(translationDto);
                translationRepository.Add(translationEntity);
                return entity;
            }
            //Update
            else
            {
                var translationDetails = _mapper.Map<IEnumerable<TranslationDetail>>(translationDto).ToList();
                translationEntity = translationRepository.GetById(translationHeaderId);
                //translationEntity = _mapper.Map(translationDto, translationEntity);
                translationDetailsRepository.UpdateCrossTable(translationDetails, x => x.TranslationHeaderId == translationHeaderId, "TranslationDetails");
                return entity;
            }
        }*/

        //protected virtual TEntity AddOrUpdateTranslation<TEntity>(IHttpContextAccessor context, TEntity entity, TranslationHeaderInputDto translationDto) where TEntity : EntityBase
        //{
        //    var tableId = (int)EnumHelper<TableNameEnum>.GetValueFromName(nameof(TEntity));
        //    if (tableId == 0)
        //        return null;

        //    var _translationRepository = (IRepository<TranslationHeader>)context.HttpContext.RequestServices.GetService(typeof(IRepository<TranslationHeader>));
        //    var _translationDetailsRepository = (IRepository<TranslationDetail>)context.HttpContext.RequestServices.GetService(typeof(IRepository<TranslationDetail>));

        //    //Get Entity by Id

        //    //var translationHeaderId = entity.TranslationHeaderId ?? 0;
        //    var translationHeaderId = (int)entity.GetType().GetProperty("TranslationHeaderId").GetValue(entity);

        //    TranslationHeader translationEntity;
        //    //Create
        //    if (translationHeaderId == 0)
        //    {
        //        translationEntity = _mapper.Map<TranslationHeader>(translationDto);
        //        _translationRepository.Add(translationEntity);
        //        return entity;
        //    }
        //    //Update
        //    else
        //    {
        //        var translationDetails = _mapper.Map<IEnumerable<TranslationDetail>>(translationDto).ToList();
        //        translationEntity = _translationRepository.GetById(translationHeaderId);
        //        //translationEntity = _mapper.Map(translationDto, translationEntity);
        //        _translationDetailsRepository.UpdateCrossTable(translationDetails, x => x.TranslationHeaderId == translationHeaderId, "TranslationDetails");
        //        return entity;
        //    }
        //}

        //public List<TranslationHeaderInputDto> GetTranslation<TEntity>(IHttpContextAccessor context, TEntity entity, string fieldName) where TEntity : EntityBase
        //{
        //    var _translationRepository = (IRepository<TranslationHeader>)context.HttpContext.RequestServices.GetService(typeof(IRepository<TranslationHeader>));
        //    var translationHeaderId = (int)entity.GetType().GetProperty("TranslationHeaderId").GetValue(entity);

        //    var translationEntity = _translationRepository
        //        .Where(x => x.Id == translationHeaderId)
        //        .WhereIf(x => x.TranslationDetails.Any(d => d.FieldName == fieldName), !string.IsNullOrWhiteSpace(fieldName))
        //        .ToList();
        //    return _mapper.Map<List<TranslationHeaderInputDto>>(translationEntity);
        //}

        #endregion Add , Update and GetTranslation
        /// <summary>
        /// get max number in Repositry based on filterexpress with max selection based on maxExpression
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository"></param>
        /// <param name="filterExpression"></param>
        /// <param name="maxExpression"></param>
        /// <returns></returns>
        protected int GetNextEntryNumber<T>
            (IRepository<T> repository,
            Expression<Func<T, int>> maxExpression,
            Expression<Func<T, bool>>? filterExpression = null)
            where T : EntityBase
        {
            try
            {
                var nextNumber = (
                     repository
                    .DisableFilter(nameof(DynamicFilters.Inactive))
                    .WhereIf(filterExpression, filterExpression != null)
                    .Max(maxExpression)) + 1;


                return nextNumber;
            }
            catch (Exception ex)
            {
                return 1;
            }
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Entity has references. </summary>
        ///
        /// <param name="id">               The identifier. </param>
        /// <param name="schemaName">       Name of the schema. </param>
        /// <param name="parentTableName">  Name of the parent table. </param>
        /// <param name="repository">       The entity repository. </param>
        /// <param name="excludedTables">   (Optional) The excluded tables comma seperated and must be
        ///                                 quoted with square brackets ex.:[SchemaName].[TableName],
        ///                                 [SchemaName].[TableName]. </param>
        ///
        /// <returns>   A CheckDto. </returns>
        ///-------------------------------------------------------------------------------------------------

        protected virtual CheckDto EntityHasReferences<T>(long id, string schemaName, string parentTableName, IRepository<T> repository, string excludedTables = "") where T : class
        {
            Hashtable hashParam = new Hashtable
            {
                ["RefId"] = id,
                ["SchemaName"] = schemaName,
                ["ParentTableName"] = parentTableName,
                ["ExcludedTables"] = excludedTables
            };
            var result = repository.ExecuteSQLQuery<CheckDto>("EXEC CheckIfDependenciesExist @RefId, @SchemaName, @ParentTableName, @ExcludedTables", hashParam).FirstOrDefault();

            return result;
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Entity has references. </summary>
        ///
        /// <param name="id">               The identifier. </param>
        /// <param name="repository">       The entity repository. </param>
        /// <param name="excludedTables">   (Optional) The excluded tables comma seperated and must be
        ///                                 quoted with square brackets ex.:[SchemaName].[TableName],
        ///                                 [SchemaName].[TableName]. </param>
        ///
        /// <returns>   A CheckDto. </returns>
        ///-------------------------------------------------------------------------------------------------
        protected virtual CheckDto EntityHasReferences<T>(long id, IRepository<T> repository, string excludedTables = "") where T : class
        {
            var dicOfSchemaAndTable = repository.GetEntitySchema();
            Hashtable hashParam = new Hashtable
            {
                ["RefId"] = id,
                ["SchemaName"] = dicOfSchemaAndTable.FirstOrDefault().Key,
                ["ParentTableName"] = dicOfSchemaAndTable.FirstOrDefault().Value,
                ["ExcludedTables"] = excludedTables
            };
            List<Type> types = new List<Type>();
            types.Add(new CheckDto().GetType());

            var proc = repository.ExecuteStoredProcedure("CheckIfDependenciesExist", hashParam, types).FirstOrDefault();

            var result = proc.Cast<CheckDto>().FirstOrDefault();
            // var result = repository.ExecuteSQLQuery<CheckDto>("EXEC CheckIfDependenciesExist @RefId, @SchemaName, @ParentTableName, @ExcludedTables", hashParam).FirstOrDefault();

            return result;
        }
        public static bool RelationsCheckOnEntity(object entity, string excludedProberties = "")
        {
            var excludedProb = excludedProberties.Split(',');
            var propertiesCollections =
       entity.GetType().GetProperties()
           .Where(m =>
                   m.PropertyType.IsGenericType && m.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>));
            var propertiesIEntityBase =
       entity.GetType().GetProperties()
           .Where(m =>
                    m.PropertyType.BaseType == typeof(EntityBase));
            var canUpdateICollection = (from prop in propertiesCollections select prop.GetValue(entity) into propValue select propValue as IList).All(propList => propList == null || propList.Count <= 0);
            var canUpdateEntityBase = (from prop in propertiesIEntityBase select prop.GetValue(entity) into propValue select propValue as IList).All(propList => propList == null);
            //  var propertiesList = entity.GetType().GetProperties();
            // if(excludedProb.Any())
            //     propertiesList= propertiesList.Where(x=> !excludedProb.Contains(x.Name)).ToArray();
            return false;// (from prop in propertiesList  where prop.PropertyType.IsGenericType select prop.GetValue(entity) into propValue select propValue as IList).All(propList => propList == null || propList.Count <= 0);
        }

        protected virtual PagedResultDto<T1> GetPagedList<T1, T2, TKey>(FilteredResultRequestDto pagedDto,
                                                                IRepository<T2> repository,
                                                                Expression<Func<T2, TKey>> orderExpression,
                                                                Expression<Func<T2, bool>> searchExpression,
                                                                string sortDirection = nameof(SortingDirection.ASC),
                                                                bool disableFilter = false,
                                                                List<string> excluededColumns = null,
                                                                params Expression<Func<T2, object>>[] includeProperties
                                                                 ) where T1 : class where T2 : class
        {
            PagedResultDto<T1> result = new PagedResultDto<T1>();

            try
            {
                Dictionary<string, FilterValue> filters = null;
                if (pagedDto.Filter != null)
                    filters = JsonConvert.DeserializeObject<Dictionary<string, FilterValue>>(pagedDto.Filter);

                #region Exclude Filters not exist in Entity

                if (excluededColumns != null && excluededColumns.Any() && filters != null && filters.Any())
                {
                    foreach (var column in excluededColumns)
                    {
                        var excludedFilter = filters.FirstOrDefault(t => t.Key.Contains(column));
                        if (filters.Any(t => t.Key.Contains(column)))
                            filters.Remove(excludedFilter.Key);
                    }
                }

                #endregion Exclude Filters not exist in Entity

                var filterBuilder = new FilterBuilder.Generic.Filter<T2>();
                //01- Get All Case - Without Search terms & No Filters
                if (searchExpression == null && (filters == null || filters.Count == 0))
                {
                    var entityList = repository.PagedResult<T2>(pagedDto.MaxResultCount, pagedDto.SkipCount, pagedDto.SortBy /*orderExpression*/, sortDirection, disableFilter, includeProperties);
                    // x => x.Id.ToString()
                    result.Items = _mapper.Map<List<T1>>(entityList.Result);
                    result.TotalCount = entityList.ResultCount;
                }
                //02- Get All filtered - With Search term & With providing Filters
                else if (searchExpression != null && filters?.Count > 0)
                {
                    foreach (var item in filters)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Key) && item.Key != "undefined" && item.Value != null)
                            filterBuilder.By(item.Key.Replace("-R2-", ""), item.Value.Operator, item.Value.Value);
                    }

                    var entityList = repository.PagedResult<T2>(pagedDto.MaxResultCount, pagedDto.SkipCount/*orderExpression*/, filterBuilder.BuildExpression().And(searchExpression), pagedDto.SortBy, sortDirection, disableFilter, includeProperties);
                    //x => x.Id.ToString()
                    result.Items = _mapper.Map<List<T1>>(entityList.Result);
                    result.TotalCount = entityList.ResultCount;
                }
                //03- Get All filtered - Without Search term but With providing Filters
                else if (searchExpression == null && filters.Count > 0)
                {
                    foreach (var item in filters)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Key) && item.Key != "undefined" && item.Value != null)
                            //if (!string.IsNullOrWhiteSpace(item.Value.Value))
                            filterBuilder.By(item.Key.Replace("-R2-", ""), item.Value.Operator, item.Value.Value);
                    }

                    var entityList = repository.PagedResult<T2>(pagedDto.MaxResultCount, pagedDto.SkipCount /*orderExpression*/, filterBuilder.BuildExpression(), pagedDto.SortBy, sortDirection, disableFilter, includeProperties);
                    //x => x.Id.ToString()
                    result.Items = _mapper.Map<List<T1>>(entityList.Result);
                    result.TotalCount = entityList.ResultCount;
                }
                //04- Get Specific entity - With Search term but Without providing any Filters
                else
                {
                    var entityList = repository.PagedResult<T2>(pagedDto.MaxResultCount, pagedDto.SkipCount /*orderExpression*/, searchExpression, pagedDto.SortBy, sortDirection, disableFilter, includeProperties); //x => x.Id.ToString(), queryExpression
                    result.Items = _mapper.Map<List<T1>>(entityList.Result);
                    result.TotalCount = entityList.ResultCount;
                }
                return result;
            }
            catch (Exception ex)
            {
                return result;
            }
        }

        protected virtual PagedResultDto<T1> GetLocalizedPagedList<T1, T2, TKey>(FilteredResultRequestDto pagedDto,
                                                             IRepository<T2> repository,
                                                             Expression<Func<T2, TKey>> defaultOrderExpression,
                                                             Expression<Func<T2, bool>> searchExpression,
                                                             string sortDirection = nameof(SortingDirection.ASC),
                                                             bool disableFilter = true,
                                                             int langId = DXConstants.SupportedLanguage.DefaultLangId,
                                                             List<string> excluededColumns = null,
                                                             params Expression<Func<T2, object>>[] includeProperties
                                                              ) where T1 : class where T2 : class
        {
            PagedResultDto<T1> result = new PagedResultDto<T1>();

            try
            {
                Dictionary<string, FilterValue> filters = null;
                if (pagedDto.Filter != null)
                    filters = JsonConvert.DeserializeObject<Dictionary<string, FilterValue>>(pagedDto.Filter);

                #region Exclude Filters not exist in Entity

                if (excluededColumns != null && excluededColumns.Any() && filters != null && filters.Any())
                {
                    foreach (var column in excluededColumns)
                    {
                        var excludedFilter = filters.FirstOrDefault(t => t.Key.Contains(column));
                        if (filters.Any(t => t.Key.Contains(column)))
                            filters.Remove(excludedFilter.Key);
                    }
                }

                #endregion Exclude Filters not exist in Entity

                var filterBuilder = new FilterBuilder.Generic.Filter<T2>();
                //01- Get All Case - Without Search terms & No Filters
                if (searchExpression == null && (filters == null || filters.Count == 0))
                {

                    var entityQuery = repository.PagedQueryable<T2>(pagedDto.MaxResultCount, pagedDto.SkipCount, pagedDto.SortBy /*orderExpression*/, sortDirection, disableFilter, includeProperties);
                    result.Items = LocalizeMultiple<T2, T1>(query: entityQuery.Query, langId: langId).ToList();
                    result.TotalCount = entityQuery.ResultCount;
                }
                //02- Get All filtered - With Search term & With providing Filters
                else if (searchExpression != null && filters?.Count > 0)
                {
                    foreach (var item in filters)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Key) && item.Key != "undefined" && item.Value != null)
                            filterBuilder.By(item.Key.Replace("-R2-", ""), item.Value.Operator, item.Value.Value);
                    }
                    var entityQuery = repository.PagedQueryable<T2>(pagedDto.MaxResultCount, pagedDto.SkipCount, pagedDto.SortBy /*orderExpression*/, filterBuilder.BuildExpression().And(searchExpression), sortDirection, disableFilter, includeProperties);
                    result.Items = LocalizeMultiple<T2, T1>(query: entityQuery.Query, langId: langId).ToList();
                    result.TotalCount = entityQuery.ResultCount;
                }
                //03- Get All filtered - Without Search term but With providing Filters
                else if (searchExpression == null && filters.Count > 0)
                {
                    foreach (var item in filters)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Key) && item.Key != "undefined" && item.Value != null)
                            //if (!string.IsNullOrWhiteSpace(item.Value.Value))
                            filterBuilder.By(item.Key.Replace("-R2-", ""), item.Value.Operator, item.Value.Value);
                    }
                    var entityQuery = repository.PagedQueryable<T2>(pagedDto.MaxResultCount, pagedDto.SkipCount, pagedDto.SortBy /*orderExpression*/, filterBuilder.BuildExpression(), sortDirection, disableFilter, includeProperties);
                    result.Items = LocalizeMultiple<T2, T1>(query: entityQuery.Query, langId: langId).ToList();
                    result.TotalCount = entityQuery.ResultCount;

                }
                //04- Get Specific entity - With Search term but Without providing any Filters
                else
                {
                    var entityQuery = repository.PagedQueryable<T2>(pagedDto.MaxResultCount, pagedDto.SkipCount, pagedDto.SortBy /*orderExpression*/, searchExpression, sortDirection, disableFilter, includeProperties); //x => x.Id.ToString(), queryExpression
                    result.Items = LocalizeMultiple<T2, T1>(query: entityQuery.Query, langId: langId).ToList();
                    result.TotalCount = entityQuery.ResultCount;
                }
                return result;
            }
            catch (Exception ex)
            {
                return result;
            }
        }


        /// <summary>
        /// Convert the Hijri date to a Gregorian Date By Using HijriCalendar 
        /// <param name="hijri">DateTime Of Hijri Date.</param>
        /// </summary>

        public static DateTime ConvertHijriToGregorian(DateTime hijri)
        {
            // Create a new instance of the HijriCalendar
            HijriCalendar hijriCalendar = new HijriCalendar();
            
            // Convert the Hijri date to a Gregorian date
            DateTime gregorianDate = new DateTime(hijri.Year, hijri.Month, hijri.Day, hour: hijri.Hour, minute: hijri.Minute, hijri.Second, hijriCalendar).AddDays(1);

            return gregorianDate;
        }

        /// <summary>
        /// Convert the Gregorian date to a Hijri Date By Using HijriCalendar 
        /// <param name="hijri">DateTime Of Hijri Date.</param>
        /// </summary>

        public static DateTime ConvertGregorianToHijri(DateTime gregorian)
        {
            CultureInfo arSA = CultureInfo.CreateSpecificCulture("ar-SA");
            return DateTime.Parse(gregorian.ToString("MM/dd/yyyy", arSA));
        }

    }
}