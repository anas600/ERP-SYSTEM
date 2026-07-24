using System.Data;
using ERPSystem.Modules.Identity.Entities;

namespace ERPSystem.Modules.Identity.Infrastructure;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<User?> GetByEmailAndTenantAsync(string email, Guid tenantId, CancellationToken ct);
    Task<User?> GetByEmailAndTenantAsync(string email, Guid tenantId, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    Task InsertAsync(User user, CancellationToken ct);
    Task InsertAsync(User user, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload
    Task UpdateLastLoginAsync(Guid userId, DateTime at, CancellationToken ct);
    Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload
    Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken ct);
    Task AssignRoleAsync(Guid userId, Guid roleId, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload
    Task<IReadOnlyList<User>> ListAsync(Guid tenantId, int skip, int take, CancellationToken ct); // DEC-067-C
    Task<int> CountAsync(Guid tenantId, CancellationToken ct); // DEC-067-C
}

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(Guid tenantId, string name, CancellationToken ct);
    Task<Role?> GetByNameAsync(Guid tenantId, string name, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload
    Task<Role?> GetByIdAsync(Guid id, CancellationToken ct);
    Task InsertAsync(Role role, CancellationToken ct);
    Task InsertAsync(Role role, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload (called by EnsureDefaultRolesAsync inside the tx)
    Task EnsureDefaultRolesAsync(Guid tenantId, CancellationToken ct);
    Task EnsureDefaultRolesAsync(Guid tenantId, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload
}

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken ct);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct);
    Task InsertAsync(Tenant tenant, CancellationToken ct);
    Task InsertAsync(Tenant tenant, IDbConnection conn, IDbTransaction? tx, CancellationToken ct); // P1-9: transactional overload
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
