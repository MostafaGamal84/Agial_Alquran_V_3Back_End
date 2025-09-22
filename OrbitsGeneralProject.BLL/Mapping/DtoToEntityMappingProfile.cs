using AutoMapper;
using Orbits.GeneralProject.Core.Entities;
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
            CreateMap<CreateCircleDto, Circle>()
                .ForMember(d => d.Time, o => o.MapFrom(s => s.DayId));
            CreateMap<UpdateCircleDto, Circle>()
                .ForMember(d => d.Time, o => o.MapFrom(s => s.DayId));
            CreateMap<ManagerCirclesDto, ManagerCircle>();
            CreateMap<CircleReportAddDto, CircleReport>()
            .ForMember(x => x.StudentId, xx => xx.MapFrom(c => c.StudentId))
            .ForMember(x => x.TeacherId, xx => xx.MapFrom(c => c.TeacherId));
            CreateMap<CreateSubscribeDto, Subscribe>();
            CreateMap<CreateSubscribeTypeDto, SubscribeType>();

            ;

        }
    }
}