namespace ERPSystem.Modules.Identity.Entities;

/// <summary>
/// Sprint 63 (DEC-213) — Per-role module visibility flag.
/// <para>
/// Controls whether a role can SEE a given module in the sidebar (and on the server
/// side, whether <c>ModuleVisibilityController</c> includes that module in
/// <c>GET /api/me/visible-modules</c>). One row per (role, module) pair.
/// </para>
/// <para>
/// <b>Module names</b> match the 9-module architecture target: Identity, Companies, Finance,
/// Inventory, Procurement, AR, HR, Payroll, Projects, Dashboard (10 incl. Dashboard).
/// They are free-text labels (no FK to a modules table) so future module splits/merges
/// can be reflected without a schema change.
/// </para>
/// </summary>
public class ModuleVisibility
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public string Module { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
