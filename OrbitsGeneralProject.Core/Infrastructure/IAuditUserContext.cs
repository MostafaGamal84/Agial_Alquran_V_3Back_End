namespace Orbits.GeneralProject.Core.Infrastructure
{
    public interface IAuditUserContext
    {
        int? UserId { get; }

        int? RoleId { get; }
    }

    public sealed class NullAuditUserContext : IAuditUserContext
    {
        public int? UserId => null;

        public int? RoleId => null;
    }
}
