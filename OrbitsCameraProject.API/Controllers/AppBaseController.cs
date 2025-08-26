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
        public int? UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier)!=null ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)):null;
            }
             set { }
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
