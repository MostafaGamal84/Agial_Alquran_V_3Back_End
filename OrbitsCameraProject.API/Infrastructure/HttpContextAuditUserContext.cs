using Orbits.GeneralProject.Core.Infrastructure;
using System.Security.Claims;

namespace OrbitsProject.API.Infrastructure
{
    public sealed class HttpContextAuditUserContext : IAuditUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpContextAuditUserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? UserId => GetClaimValue(ClaimTypes.NameIdentifier);

        public int? RoleId => GetClaimValue(ClaimTypes.Role);

        private int? GetClaimValue(string claimType)
        {
            var claimValue = _httpContextAccessor.HttpContext?.User?.FindFirstValue(claimType);
            return int.TryParse(claimValue, out var parsedValue) ? parsedValue : null;
        }
    }
}
