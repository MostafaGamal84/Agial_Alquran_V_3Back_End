using Orbits.GeneralProject.Core.Infrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace OrbitsProject.API.Infrastructure
{
    public sealed class HttpContextAuditUserContext : IAuditUserContext
    {
        private static readonly IReadOnlyDictionary<string, string> SourceScreenLabels =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["students"] = "شاشة الطلاب",
                ["teachers"] = "شاشة المعلمين",
                ["managers"] = "شاشة المشرفين",
                ["branch-managers"] = "شاشة قادة الفروع",
                ["circles"] = "شاشة الحلقات",
                ["reports"] = "شاشة التقارير",
                ["pricing"] = "شاشة الأسعار",
                ["site"] = "شاشة الموقع",
                ["settings"] = "شاشة الإعدادات",
                ["deleted-objects"] = "شاشة العناصر المحذوفة",
                ["operations-log"] = "شاشة سجل العمليات",
                ["dashboard"] = "شاشة لوحة التحكم",
                ["login"] = "شاشة تسجيل الدخول"
            };

        private static readonly IReadOnlyList<KeyValuePair<string, string>> SourceRouteLabels =
            new List<KeyValuePair<string, string>>
            {
                new("/online-course/student", "شاشة الطلاب"),
                new("/online-course/teacher", "شاشة المعلمين"),
                new("/online-course/manager", "شاشة المشرفين"),
                new("/online-course/branch-manager", "شاشة قادة الفروع"),
                new("/online-course/courses", "شاشة الحلقات"),
                new("/online-course/report", "شاشة التقارير"),
                new("/online-course/pricing", "شاشة الأسعار"),
                new("/online-course/site", "شاشة الموقع"),
                new("/online-course/setting", "شاشة الإعدادات"),
                new("/online-course/deleted-objects", "شاشة العناصر المحذوفة"),
                new("/online-course/operations-log", "شاشة سجل العمليات"),
                new("/online-course/dashboard", "شاشة لوحة التحكم"),
                new("/auth/login", "شاشة تسجيل الدخول")
            };

        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpContextAuditUserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? UserId => GetClaimValue(ClaimTypes.NameIdentifier);

        public int? RoleId => GetClaimValue(ClaimTypes.Role);

        public string? SourceScreen => ResolveSourceScreen();

        public string? SourceRoute => GetHeaderValue("X-Audit-Source-Route");

        public string? RequestPath => NormalizeValue(_httpContextAccessor.HttpContext?.Request?.Path.Value);

        public string? HttpMethod => NormalizeValue(_httpContextAccessor.HttpContext?.Request?.Method);

        private int? GetClaimValue(string claimType)
        {
            var claimValue = _httpContextAccessor.HttpContext?.User?.FindFirstValue(claimType);
            return int.TryParse(claimValue, out var parsedValue) ? parsedValue : null;
        }

        private string? GetHeaderValue(string headerName)
        {
            var headerValue = _httpContextAccessor.HttpContext?.Request?.Headers[headerName].FirstOrDefault();
            return NormalizeValue(headerValue);
        }

        private string? ResolveSourceScreen()
        {
            var sourceScreenKey = GetHeaderValue("X-Audit-Source-Screen");
            if (!string.IsNullOrWhiteSpace(sourceScreenKey) &&
                SourceScreenLabels.TryGetValue(sourceScreenKey, out var sourceScreenLabel))
            {
                return sourceScreenLabel;
            }

            var sourceRoute = GetHeaderValue("X-Audit-Source-Route");
            if (!string.IsNullOrWhiteSpace(sourceRoute))
            {
                foreach (var routeLabel in SourceRouteLabels)
                {
                    if (sourceRoute.Contains(routeLabel.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        return routeLabel.Value;
                    }
                }
            }

            return NormalizeValue(sourceScreenKey);
        }

        private static string? NormalizeValue(string? value)
        {
            var normalizedValue = value?.Trim();
            return string.IsNullOrWhiteSpace(normalizedValue) ? null : normalizedValue;
        }
    }
}
