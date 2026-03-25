namespace Orbits.GeneralProject.Core.Infrastructure
{
    public interface IAuditUserContext
    {
        int? UserId { get; }

        int? RoleId { get; }

        string? SourceScreen { get; }

        string? SourceRoute { get; }

        string? RequestPath { get; }

        string? HttpMethod { get; }
    }

    public sealed class NullAuditUserContext : IAuditUserContext
    {
        public int? UserId => null;

        public int? RoleId => null;

        public string? SourceScreen => null;

        public string? SourceRoute => null;

        public string? RequestPath => null;

        public string? HttpMethod => null;
    }
}
