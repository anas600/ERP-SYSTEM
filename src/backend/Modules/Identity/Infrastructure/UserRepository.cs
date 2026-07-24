using System.Data;
using Dapper;
using ERPSystem.Modules.Identity.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Identity.Infrastructure;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _db;

    public UserRepository(IDbConnectionFactory db) => _db = db;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"SELECT id, tenant_id AS TenantId, email, password_hash AS PasswordHash,
                                    full_name AS FullName, is_active AS IsActive,
                                    two_factor_enabled AS TwoFactorEnabled,
                                    created_at AS CreatedAt, updated_at AS UpdatedAt, last_login_at AS LastLoginAt
                             FROM users WHERE id = @Id LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<User>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"SELECT id, tenant_id AS TenantId, email, password_hash AS PasswordHash,
                                    full_name AS FullName, is_active AS IsActive,
                                    two_factor_enabled AS TwoFactorEnabled,
                                    created_at AS CreatedAt, updated_at AS UpdatedAt, last_login_at AS LastLoginAt
                             FROM users WHERE LOWER(email) = LOWER(@Email) LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<User>(new CommandDefinition(sql, new { Email = email }, cancellationToken: ct));
    }

    public async Task<User?> GetByEmailAndTenantAsync(string email, Guid tenantId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await GetByEmailAndTenantAsync(email, tenantId, conn, null, ct);
    }

    public async Task<User?> GetByEmailAndTenantAsync(string email, Guid tenantId, IDbConnection conn, IDbTransaction? tx, CancellationToken ct)
    {
        const string sql = @"SELECT id, tenant_id AS TenantId, email, password_hash AS PasswordHash,
                                    full_name AS FullName, is_active AS IsActive,
                                    two_factor_enabled AS TwoFactorEnabled,
                                    created_at AS CreatedAt, updated_at AS UpdatedAt, last_login_at AS LastLoginAt
                             FROM users WHERE LOWER(email) = LOWER(@Email) AND tenant_id = @TenantId LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<User>(new CommandDefinition(sql, new { Email = email, TenantId = tenantId }, transaction: tx, cancellationToken: ct));
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = "SELECT 1 FROM users WHERE LOWER(email) = LOWER(@Email) LIMIT 1";
        var hit = await conn.QueryFirstOrDefaultAsync<int?>(new CommandDefinition(sql, new { Email = email }, cancellationToken: ct));
        return hit.HasValue;
    }

    public async Task InsertAsync(User user, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await InsertAsync(user, conn, null, ct);
    }

    public async Task InsertAsync(User user, IDbConnection conn, IDbTransaction? tx, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO users (id, tenant_id, email, password_hash, full_name, is_active, two_factor_enabled, created_at, updated_at, last_login_at)
            VALUES (@Id, @TenantId, @Email, @PasswordHash, @FullName, @IsActive, @TwoFactorEnabled, @CreatedAt, @UpdatedAt, @LastLoginAt)";
        await conn.ExecuteAsync(new CommandDefinition(sql, user, transaction: tx, cancellationToken: ct));
    }

    public async Task UpdateLastLoginAsync(Guid userId, DateTime at, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = "UPDATE users SET last_login_at = @At, updated_at = @At WHERE id = @Id";
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = userId, At = at }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await GetRoleNamesAsync(userId, conn, null, ct);
    }

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(Guid userId, IDbConnection conn, IDbTransaction? tx, CancellationToken ct)
    {
        const string sql = @"SELECT r.name FROM roles r
                             INNER JOIN user_roles ur ON ur.role_id = r.id
                             WHERE ur.user_id = @UserId";
        var rows = await conn.QueryAsync<string>(new CommandDefinition(sql, new { UserId = userId }, transaction: tx, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await AssignRoleAsync(userId, roleId, conn, null, ct);
    }

    public async Task AssignRoleAsync(Guid userId, Guid roleId, IDbConnection conn, IDbTransaction? tx, CancellationToken ct)
    {
        const string sql = @"INSERT INTO user_roles (user_id, role_id, assigned_at)
                             VALUES (@UserId, @RoleId, @AssignedAt)
                             ON CONFLICT (user_id, role_id) DO NOTHING";
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            UserId = userId,
            RoleId = roleId,
            AssignedAt = DateTime.UtcNow
        }, transaction: tx, cancellationToken: ct));
    }

    // DEC-067-C: List users for admin page
    public async Task<IReadOnlyList<User>> ListAsync(Guid tenantId, int skip, int take, CancellationToken ct)
    {
        if (take is < 1 or > 200) take = 50;
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"SELECT id, tenant_id AS TenantId, email, full_name AS FullName,
                             is_active AS IsActive, two_factor_enabled AS TwoFactorEnabled,
                             created_at AS CreatedAt, updated_at AS UpdatedAt, last_login_at AS LastLoginAt
                             FROM users
                             WHERE tenant_id = @TenantId
                             ORDER BY created_at DESC
                             OFFSET @Skip LIMIT @Take";
        var rows = await conn.QueryAsync<User>(new CommandDefinition(sql,
            new { TenantId = tenantId, Skip = skip, Take = take }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<int> CountAsync(Guid tenantId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*)::int FROM users WHERE tenant_id = @TenantId",
            new { TenantId = tenantId }, cancellationToken: ct));
    }
}
