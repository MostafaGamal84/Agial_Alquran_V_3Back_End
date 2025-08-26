using AutoMapper;

using Orbits.GeneralProject.BLL.Constants;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.DTO.Translation;
using System.Runtime.Serialization;
using Azure.Core;
using Orbits.GeneralProject.Core.Infrastructure;
using Orbits.GeneralProject.DTO.UserDto;

namespace Orbits.GeneralProject.BLL.Mapping
{
    public class EntityToDtoMappingProfile : Profile
    {
        public EntityToDtoMappingProfile( )
        {
        }
    }
}
