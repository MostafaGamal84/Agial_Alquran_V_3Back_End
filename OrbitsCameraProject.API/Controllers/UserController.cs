using HandlebarsDotNet;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.UserService;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.DTO;
using Orbits.GeneralProject.DTO.UserDto;
using Orbits.GeneralProject.DTO.UserDtos;

namespace OrbitsProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : AppBaseController
    {
        //To do Injection for Services BLL
        private readonly IUserBLL _userBLL;
        public UserController(IUserBLL userBLL)
        {
            _userBLL = userBLL;
        }

        [HttpPost("Create"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> Create(Orbits.GeneralProject.DTO.UserDto.CreateUserDto model) => Ok(await _userBLL.Add(model));

        [HttpPost("Update"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> Update(UpdateUserDto model) => Ok(await _userBLL.Update(model, UserId));

        [HttpGet("GetProfile"), HttpGet("Profile"), ProducesResponseType(typeof(IResponse<ProfileDto>), 200)]
        public async Task<IActionResult> GetProfile()
            => Ok(await _userBLL.GetProfile(UserId));

        [HttpPut("Profile"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> UpdateProfile(UpdateProfileDto model)
            => Ok(await _userBLL.UpdateProfile(model, UserId));

        [HttpGet("DisableUser"), ProducesResponseType(typeof(IResponse<bool>), 200)]
        public async Task<IActionResult> DisableUser(int id, bool statue) => Ok(await _userBLL.DisableUser(id, statue));
        //[HttpGet("GetById"), ProducesResponseType(typeof(IResponse<UserDetailsDto>), 200)]
        //public async Task<IActionResult> GetById(int id) => Ok(await _userBLL.GetById(id, UserId));
    }
}
