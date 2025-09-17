using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.DTO.LockUpDtos;
using Orbits.GeneralProject.DTO.UserDto;
using Orbits.GeneralProject.DTO.UserDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.BLL.UserService
{
    public interface IUserBLL
    {
        Task<IResponse<bool>> Add(DTO.UserDto.CreateUserDto createUserDto);
        Task<IResponse<bool>> Update(UpdateUserDto updateUserDto, int? userid);
        Task<IResponse<UserLockUpDto>> GetProfile(int userId);
        Task<IResponse<bool>> DisableUser(int id, bool statue);



    }
}
