using AutoMapper.QueryableExtensions;
using AutoMapper;
using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.Core.Infrastructure;
using System.Linq.Expressions;

namespace Orbits.GeneralProject.BLL.Mapping
{
    public static class AutoMapperExtenstions
    {
        public static IQueryable<TDestination> MapLocalized<TSource, TDestination>( this IMapper mapper, IQueryable<TSource> query, int langId )
        {
            return query.ProjectTo<TDestination>(mapper.ConfigurationProvider, new { langId = langId });
        }
        
        //public static IMappingExpression<TSource, TDestination> ForMemberLocalized<TSource, TDestination, TProperty>(
        //this IMappingExpression<TSource, TDestination> map,
        //Expression<Func<TDestination, string>> targetMember,
        //Expression<Func<TSource, TProperty>> sourceMember,
        //   int langId ) where TSource : EntityBase
        //{
           
        //    map.ForMember(
        //        targetMember,
        //        DXConstants.SupportedLanguage.DefaultLangId != langId
        //       ? opt => opt.MapFrom(src => Translate.Value(langId, nameof(sourceMember.Name), src.TranslationHeaderId ?? 0))
        //       : opt => opt.MapFrom(sourceMember)
        //            );
        //    #region Comment
        //    // map.ForMember(dest => targetMember, opts =>
        //    //        opts.MapFrom<LocalizationResolver, string>(sourceMember));
        //    // ////map.ForMember(targetMember, opt => opt.MapFrom(src => src.TranslationHeaderId == null || DXConstants.SupportedLanguage.DefaultLangId == langId ?  sourceMember.Compile().ToString() : Translate.Value(langId, nameof(sourceMember.Name), src.TranslationHeaderId ?? 0)));
        //    // //map.ForMember(targetMember, opt =>
        //    // //{
        //    // //    opt.Condition(src => src.TranslationHeaderId == null || DXConstants.SupportedLanguage.DefaultLangId == langId);
        //    // //    opt.MapFrom(sourceMember);
        //    // //});

        //    // //map.ForMember(targetMember, opt =>
        //    // //{
        //    // //    opt.Condition(src => src.TranslationHeaderId != null  && DXConstants.SupportedLanguage.DefaultLangId != langId,);
        //    // //    opt.MapFrom(src=>Translate.Value(langId, nameof(sourceMember.Name), src.TranslationHeaderId ?? 0));
        //    // //});

        //    ////else
        //    //   map.ForMember(targetMember, opt => opt.MapFrom(sourceMember));
        //    #endregion
        //    return map;
        //}

        #region Comment
        //public static IQueryable<TDestination> ProjectTo<TDestination>( this IQueryable source, IConfigurationProvider configuration, int langId,
        //params Expression<Func<TDestination, object>> [] membersToExpand ) =>
        ////source.ProjectTo(configuration, new { langId = langId }, membersToExpand);

        //    source.ProjectTo(configuration, new { langId = langId }, membersToExpand);
        #endregion
    }
}
    

