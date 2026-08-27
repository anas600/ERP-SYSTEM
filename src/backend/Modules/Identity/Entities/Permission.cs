namespace ERPSystem.Modules.Identity.Entities;

/// <summary>
/// Sprint 63 (DEC-211) — Permission catalog entry.
/// <para>
/// Each row is a (resource, action) tuple that maps to a permission code (e.g. "projects.create").
/// The catalog is global (not per-company) and immutable at runtime — the bootstrap
/// (<see cref="ERPSystem.Host.Bootstrap.RbacBootstrapHostedService"/>) seeds the rows on
/// first startup; the AdminPermissionsController (Wave 2A) can add/remove entries later.
/// </para>
/// <para>
/// <b>L19 / DEC-095</b>: there is no <c>UserId</c> or <c>CompanyId</c> on a permission —
/// the catalog is global. Per-user authorization is resolved at runtime by
/// <see cref="ERPSystem.Modules.Identity.Application.Services.IPermissionService"/>
/// via a join through <c>users → user_roles → role_permissions → permissions</c>.
/// </para>
/// </summary>
public class Permission
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Module { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
