using Microsoft.AspNetCore.Mvc;
using Orbits.GeneralProject.BLL.AuthenticationService;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.BLL.UserService;
using Orbits.GeneralProject.DTO.LoginDtos;

namespace OrbitsProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : AppBaseController
    {
        //To do Injection for Services BLL
        private readonly IAuthBLL _authBLL;

        public AccountController(IAuthBLL authBLL)
        {
            _authBLL = authBLL;
        }

        [HttpPost("Login"), ProducesResponseType(typeof(IResponse<VerifyLoginCodeDto>), 200)]
        public async Task<IActionResult> Login(LoginDto model)
     => Ok(await _authBLL.Login(model));

        [HttpPost("VerifyCode"), ProducesResponseType(typeof(IResponse<LoginResultDto>), 200)]
        public async Task<IActionResult> VerifyCode(VerifyLoginCodeDto model)
            => Ok(await _authBLL.VerifyCode(model));

    }
}
