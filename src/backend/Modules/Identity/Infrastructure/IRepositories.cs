using System.Data;
using ERPSystem.Modules.Identity.Entities;

namespace ERPSystem.Modules.Identity.Infrastructure;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    // Phase 6.1b: GetByEmailAndTenantAsync removed — users are now global. Callers use GetByEmailAsync.
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    Task InsertAsync(User user, CancellationToken ct);
    Task InsertAsync(User user, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload
    Task UpdateLastLoginAsync(Guid userId, DateTime at, CancellationToken ct);
    Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload
    Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken ct);
    Task AssignRoleAsync(Guid userId, Guid roleId, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload
    Task<IReadOnlyList<User>> ListAsync(int skip, int take, CancellationToken ct); // DEC-067-C
    Task<int> CountAsync(CancellationToken ct); // DEC-067-C
    // Sprint 2 (T4 / Block A): company-scoped user list. Returns users assigned
    // to the given company via user_companies. Used by GET /api/users?company_id={guid}.
    Task<IReadOnlyList<User>> ListByCompanyAsync(Guid companyId, int skip, int take, CancellationToken ct);
    Task<int> CountByCompanyAsync(Guid companyId, CancellationToken ct);

    // Phase 6.1c: user → companies mapping (multi-company model).
    Task<IReadOnlyList<UserCompanyLink>> GetUserCompaniesAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<UserCompanyLink>> GetUserCompaniesAsync(Guid userId, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // Sprint 61 (L49): connection-aware overload for the register flow
    Task<UserCompanyLink?> GetDefaultCompanyAsync(Guid userId, CancellationToken ct);
    Task AssignUserToCompanyAsync(Guid userId, Guid companyId, bool isDefault, CancellationToken ct);
    Task AssignUserToCompanyAsync(Guid userId, Guid companyId, bool isDefault, IDbConnection conn, IDbTransaction? tx, CancellationToken ct);

    // Phase 6.2: User CRUD for Admin
    Task UpdatePasswordAsync(Guid userId, string passwordHash, CancellationToken ct);
    Task UpdateProfileAsync(Guid userId, string? fullName, string? email, bool? isActive, CancellationToken ct);
    Task DeleteAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<Guid>> GetUserRoleIdsAsync(Guid userId, CancellationToken ct);
    Task SetUserRolesAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken ct);
}

/// <summary>
/// Joined record for a (user, company, default flag) row in <c>user_companies</c>.
/// Phase 6.1c: multi-company model — a user can belong to multiple companies.
/// </summary>
public sealed class UserCompanyLink
{
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsHolding { get; set; }
    public DateTime AssignedAt { get; set; }
}

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name, CancellationToken ct);
    Task<Role?> GetByNameAsync(string name, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload
    Task<Role?> GetByIdAsync(Guid id, CancellationToken ct);
    Task InsertAsync(Role role, CancellationToken ct);
    Task InsertAsync(Role role, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload (called by EnsureDefaultRolesAsync inside the tx)
    Task EnsureDefaultRolesAsync(CancellationToken ct);
    Task EnsureDefaultRolesAsync(IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload
}

public interface IRefreshTokenRepository
{
    Task InsertAsync(RefreshToken token, CancellationToken ct);
    Task InsertAsync(RefreshToken token, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct);
    Task<IReadOnlyList<RefreshToken>> GetActiveByUserAsync(Guid userId, CancellationToken ct);
    Task RevokeAsync(RefreshToken token, string reason, string? replacedByHash, string? ip, CancellationToken ct);
    Task RevokeAllForUserAsync(Guid userId, string reason, string? ip, CancellationToken ct);
}

// =====================================================================
// Sprint 63 (DEC-211..214) — RBAC foundation
// =====================================================================

/// <summary>
/// Sprint 63 (DEC-211) — Permission catalog repository.
/// <para>
/// Read-mostly (the catalog is seeded at startup and rarely mutated). Used by
/// <see cref="ERPSystem.Modules.Identity.Application.Services.IPermissionService"/>
/// to resolve a user's effective permission set.
/// </para>
/// </summary>
public interface IPermissionRepository
{
    Task<Permission?> GetByCodeAsync(string code, CancellationToken ct);
    Task<Permission?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Permission>> ListAsync(CancellationToken ct);
    Task<IReadOnlyList<Permission>> ListByRoleAsync(Guid roleId, CancellationToken ct);
    Task InsertAsync(Permission permission, CancellationToken ct);
}

/// <summary>
/// Sprint 63 (DEC-212) — Role-to-Permission M2M repository.
/// <para>
/// Used by the bootstrap to seed the 5 default role templates, and (later) by
/// <c>AdminPermissionsController</c> to grant/revoke permissions per role.
/// </para>
/// </summary>
public interface IRolePermissionRepository
{
    Task InsertAsync(RolePermission rolePermission, CancellationToken ct);
    Task<IReadOnlyList<Permission>> ListByRoleAsync(Guid roleId, CancellationToken ct);
    /// <summary>
    /// Joins <c>users → user_roles → role_permissions → permissions</c> and returns
    /// the user's effective permission set. Used by <c>IPermissionService</c>.
    /// </summary>
    Task<IReadOnlyList<Permission>> ListByUserAsync(Guid userId, CancellationToken ct);
    Task DeleteAsync(Guid roleId, Guid permissionId, CancellationToken ct);
}

/// <summary>
/// Sprint 63 (DEC-213) — Module visibility per role.
/// <para>
/// Used by the bootstrap to seed the 5×10 module matrix, and (later) by the
/// admin UI to toggle visibility per role.
/// </para>
/// </summary>
public interface IModuleVisibilityRepository
{
    Task<IReadOnlyList<ModuleVisibility>> ListByRoleAsync(Guid roleId, CancellationToken ct);
    /// <summary>
    /// Joins <c>users → user_roles → module_visibility</c> and returns the visible
    /// modules for a user (filters out rows where <c>is_visible = false</c>).
    /// </summary>
    Task<IReadOnlyList<ModuleVisibility>> ListByUserAsync(Guid userId, CancellationToken ct);
    Task InsertAsync(ModuleVisibility visibility, CancellationToken ct);
    Task UpdateAsync(Guid roleId, string module, bool isVisible, CancellationToken ct);
    Task DeleteAsync(Guid roleId, string module, CancellationToken ct);
}
