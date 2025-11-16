using AutoMapper;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.Core.Enums;
using Orbits.GeneralProject.DTO.CircleDto;
using Orbits.GeneralProject.DTO.CircleReportDtos;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.SubscribeDtos;
using Orbits.GeneralProject.DTO.UserDto;
using Orbits.GeneralProject.DTO.UserDtos;

namespace Orbits.GeneralProject.BLL.Mapping
{
    public class DtoToEntityMappingProfile : Profile
    {
        public DtoToEntityMappingProfile( )
        {

            CreateMap<DTO.UserDto.CreateUserDto, User>();
            CreateMap<UpdateUserDto, User>();
            CreateMap<CreateCircleDto, Circle>();
            CreateMap<UpdateCircleDto, Circle>();
            CreateMap<ManagerCirclesDto, ManagerCircle>();
            CreateMap<CircleReportAddDto, CircleReport>()
            .ForMember(x => x.StudentId, xx => xx.MapFrom(c => c.StudentId))
            .ForMember(x => x.TeacherId, xx => xx.MapFrom(c => c.TeacherId));
            CreateMap<CreateSubscribeDto, Subscribe>();
            CreateMap<CreateSubscribeTypeDto, SubscribeType>()
                .ForMember(d => d.Group, o => o.MapFrom(s => s.Group.HasValue ? (int?)s.Group.Value : null));

            ;

        }
    }
}