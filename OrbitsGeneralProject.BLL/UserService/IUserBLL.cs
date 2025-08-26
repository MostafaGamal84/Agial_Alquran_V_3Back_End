using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.DTO.UserDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.BLL.UserService
{
    public interface IUserBLL
    {
        Task<IResponse<bool>> Add(CreateUserDto createUserDto);
    }
}
