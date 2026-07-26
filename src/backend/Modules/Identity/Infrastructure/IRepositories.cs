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

    // Phase 6.1c: user → companies mapping (multi-company model).
    Task<IReadOnlyList<UserCompanyLink>> GetUserCompaniesAsync(Guid userId, CancellationToken ct);
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
