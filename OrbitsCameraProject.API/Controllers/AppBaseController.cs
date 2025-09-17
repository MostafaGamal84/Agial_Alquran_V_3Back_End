using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace OrbitsProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("FixedPolicy")]
    public class AppBaseController : ControllerBase
    {
        public int UserId
        {
            get
            {
                var raw = User?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrWhiteSpace(raw))
                    throw new UnauthorizedAccessException("User id claim is missing from the token.");

                if (!int.TryParse(raw, out var id))
                    throw new UnauthorizedAccessException("User id claim is invalid (not a number).");

                return id;
            }
        }
        public int? RoleId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.Role) != null ? int.Parse(User.FindFirstValue(ClaimTypes.Role)) : null;
            }
             set
            {

            }

        }
        

    }
}
