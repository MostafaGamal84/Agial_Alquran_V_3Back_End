using Microsoft.AspNetCore.Mvc;
using Orbits.GeneralProject.BLL.AuthenticationService;
using Orbits.GeneralProject.BLL.BaseReponse;
using Orbits.GeneralProject.DTO.LoginDtos;
using Orbits.GeneralProject.DTO.UserDtos;

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

        [HttpPost("ForgetPassword"), ProducesResponseType(typeof(IResponse<string>), 200)]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordDto model)
            => Ok(await _authBLL.ForgetPassword(model));

        [HttpPost("ResetPassword"), ProducesResponseType(typeof(IResponse<string>), 200)]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
            => Ok(await _authBLL.ResetPassword(model));

        [HttpPost("ChangePassword"), ProducesResponseType(typeof(IResponse<string>), 200)]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto model)
            => Ok(await _authBLL.ChangePassword(model, UserId));

    }
}
