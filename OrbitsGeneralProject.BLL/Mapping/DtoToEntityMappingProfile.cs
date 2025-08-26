using AutoMapper;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO.Paging;
using Orbits.GeneralProject.DTO.UserDto;

namespace Orbits.GeneralProject.BLL.Mapping
{
    public class DtoToEntityMappingProfile : Profile
    {
        public DtoToEntityMappingProfile( )
        {

            CreateMap<CreateUserDto, Student>()
    .ForMember(dest => dest.IdNavigation, opt => opt.MapFrom(src => src));
            CreateMap<CreateUserDto, User>();

        }
    }
}