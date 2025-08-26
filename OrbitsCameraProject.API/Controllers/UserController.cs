using Microsoft.AspNetCore.Mvc;
using Orbits.GeneralProject.DTO;
using HandlebarsDotNet;
using OfficeOpenXml;
using Orbits.GeneralProject.Core.Entities;
using Orbits.GeneralProject.BLL.UserService;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO.UserDto;

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
        public async Task<IActionResult> Create(CreateUserDto model) => Ok(await _userBLL.Add(model));

    }
}
