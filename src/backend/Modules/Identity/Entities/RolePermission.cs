namespace ERPSystem.Modules.Identity.Entities;

/// <summary>
/// Sprint 63 (DEC-212) — Role-to-Permission mapping row (M2M join).
/// <para>
/// A single row grants exactly one permission to exactly one role. The
/// <see cref="IPermissionService"/> resolves a user's effective permission set
/// by joining <c>users → user_roles → role_permissions → permissions</c>.
/// </para>
/// <para>
/// <b>No <c>UserId</c></b> on this row by design: the relationship is role → permission,
/// not user → permission. A user inherits all permissions of all roles they hold.
/// </para>
/// </summary>
public class RolePermission
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public DateTime CreatedAt { get; set; }
}
