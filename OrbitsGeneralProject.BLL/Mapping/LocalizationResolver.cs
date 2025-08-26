using AutoMapper;

using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO;
//using Orbits.GeneralProject.DTO.Branches;
using Orbits.GeneralProject.DTO.Translation;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Orbits.GeneralProject.BLL.Mapping
{

    //public class SetLanguageAction : IMappingAction<Branch, GetBranchOutputDto>
    //{
    //    private readonly IHttpContextAccessor _httpContextAccessor;

    //    public SetLanguageAction( IHttpContextAccessor httpContextAccessor )
    //    {
    //        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    //    }

    //    public void Process( Branch source, GetBranchOutputDto destination, ResolutionContext context )
    //    {
    //        destination.langId = Convert.ToInt32(_httpContextAccessor.HttpContext.Items ["LangId"]?.ToString() ?? DXConstants.SupportedLanguage.DefaultLangId.ToString());
    //    }

    
    //}
    //public class SetLanguageAction<TSource, TDestination> : IMappingAction<TSource, TDestination> where TSource : EntityBase where TDestination:BaseDto
    //{
    //    private readonly IHttpContextAccessor _httpContextAccessor;

    //    public SetLanguageAction( IHttpContextAccessor httpContextAccessor )
    //    {
    //        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    //    }

    //    //public void Process( Branch source, GetBranchOutputDto destination, ResolutionContext context )
    //    //{
    //    //    destination.langId = Convert.ToInt32(_httpContextAccessor.HttpContext.Items ["LangId"]?.ToString() ?? DXConstants.SupportedLanguage.DefaultLangId.ToString());
    //    //}

    //    //public void Process( TSource source, TDestination destination, ResolutionContext context )
    //    //{
    //    //    destination.langId = Convert.ToInt32(_httpContextAccessor.HttpContext.Items ["LangId"]?.ToString() ?? DXConstants.SupportedLanguage.DefaultLangId.ToString());
    //    //}
    //}

/*    public class TranslationHeaderToDetailsListConverter :
             ITypeConverter<TranslationHeaderInputDto, IEnumerable<TranslationDetail>>
    {
        public IEnumerable<TranslationDetail> Convert
        ( TranslationHeaderInputDto source, IEnumerable<TranslationDetail> destination, ResolutionContext context )
        {
            int i = 0;
            *//*first mapp from People, then from Team*//*
            foreach (var model in source.Details.Select
                    (e => context.Mapper.Map<TranslationDetail>(e)))
            {
                source.Details [i].FieldName = source.FieldName;
                source.Details [i].TranslationHeaderId = source.Id;
                context.Mapper.Map(source.Details [i++], model);
                yield return model;
            }
        }
    }*/
    //public class MyResolver : IValueResolver<object, object, string>
    //{
    //    private readonly IHttpContextAccessor _contextAccessor;



    //    public MyResolver( IHttpContextAccessor contextAccessor )
    //    {
    //        _contextAccessor = contextAccessor;
    //    }

    //    public string Resolve( object source, object target, string destMember, ResolutionContext context )
    //    {
    //        //check langId
    //        var ctx = context;
    //        var langId = 2;//            Convert.ToInt32(_contextAccessor.HttpContext.Items ["LangId"]?.ToString() ?? DXConstants.SupportedLanguage.DefaultLangId.ToString());
    //        if (((EntityBase)source).TranslationHeaderId == null || DXConstants.SupportedLanguage.DefaultLangId == langId)
    //        {
    //            return nameof(destMember);
    //        }
    //        else
    //        {
    //            return Translate.Value(langId, nameof(destMember), source.TranslationHeaderId ?? 0);
    //        }

    //    }
    //}
    public class LocalizationResolver : IMemberValueResolver<object, object, string, string>
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public LocalizationResolver( IHttpContextAccessor contextAccessor )
        {
            _contextAccessor = contextAccessor;
        }

        public string Resolve( object source, object destination, string sourceMember, string destMember, ResolutionContext context )
        {
            //check langId
            var langId = Convert.ToInt32(_contextAccessor.HttpContext.Items ["LangId"]?.ToString() ?? DXConstants.SupportedLanguage.DefaultLangId.ToString());
            var translationHeaderId = (int)source.GetType().GetProperty(name: "TranslationHeaderId").GetValue(source);

            if (translationHeaderId == null || DXConstants.SupportedLanguage.DefaultLangId == langId)
            {
                return sourceMember;
            }
            else
            {
                return Translate.Value(langId, nameof(sourceMember), translationHeaderId );
            }

        }
    }




}