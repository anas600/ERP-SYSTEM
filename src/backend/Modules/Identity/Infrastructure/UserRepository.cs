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
        const string sql = @"SELECT id, email, password_hash AS PasswordHash,
                                    full_name AS FullName, is_active AS IsActive,
                                    two_factor_enabled AS TwoFactorEnabled,
                                    created_at AS CreatedAt, updated_at AS UpdatedAt, last_login_at AS LastLoginAt
                             FROM users WHERE id = @Id LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<User>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"SELECT id, email, password_hash AS PasswordHash,
                                    full_name AS FullName, is_active AS IsActive,
                                    two_factor_enabled AS TwoFactorEnabled,
                                    created_at AS CreatedAt, updated_at AS UpdatedAt, last_login_at AS LastLoginAt
                             FROM users WHERE LOWER(email) = LOWER(@Email) LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<User>(new CommandDefinition(sql, new { Email = email }, cancellationToken: ct));
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
            INSERT INTO users (id, email, password_hash, full_name, is_active, two_factor_enabled, created_at, updated_at, last_login_at)
            VALUES (@Id, @Email, @PasswordHash, @FullName, @IsActive, @TwoFactorEnabled, @CreatedAt, @UpdatedAt, @LastLoginAt)";
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
    public async Task<IReadOnlyList<User>> ListAsync(int skip, int take, CancellationToken ct)
    {
        if (take is < 1 or > 200) take = 50;
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"SELECT id, email, full_name AS FullName,
                             is_active AS IsActive, two_factor_enabled AS TwoFactorEnabled,
                             created_at AS CreatedAt, updated_at AS UpdatedAt, last_login_at AS LastLoginAt
                             FROM users
                             ORDER BY created_at DESC
                             OFFSET @Skip LIMIT @Take";
        var rows = await conn.QueryAsync<User>(new CommandDefinition(sql,
            new { Skip = skip, Take = take }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<int> CountAsync(CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*)::int FROM users",
            cancellationToken: ct));
    }

    // ============ Phase 6.1c: user → companies (multi-company model) ============

    public async Task<IReadOnlyList<UserCompanyLink>> GetUserCompaniesAsync(Guid userId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        // is_holding = (parent_company_id IS NULL AND is_group = true)
        const string sql = @"SELECT uc.user_id AS UserId, uc.company_id AS CompanyId,
                                    c.code AS CompanyCode, c.name AS CompanyName,
                                    uc.is_default AS IsDefault,
                                    (c.parent_company_id IS NULL AND c.is_group = true) AS IsHolding,
                                    uc.assigned_at AS AssignedAt
                             FROM user_companies uc
                             INNER JOIN companies c ON c.id = uc.company_id
                             WHERE uc.user_id = @UserId
                             ORDER BY uc.is_default DESC, c.code";
        var rows = await conn.QueryAsync<UserCompanyLink>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<UserCompanyLink?> GetDefaultCompanyAsync(Guid userId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"SELECT uc.user_id AS UserId, uc.company_id AS CompanyId,
                                    c.code AS CompanyCode, c.name AS CompanyName,
                                    uc.is_default AS IsDefault,
                                    (c.parent_company_id IS NULL AND c.is_group = true) AS IsHolding,
                                    uc.assigned_at AS AssignedAt
                             FROM user_companies uc
                             INNER JOIN companies c ON c.id = uc.company_id
                             WHERE uc.user_id = @UserId
                             ORDER BY uc.is_default DESC
                             LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<UserCompanyLink>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
    }

    public async Task AssignUserToCompanyAsync(Guid userId, Guid companyId, bool isDefault, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await AssignUserToCompanyAsync(userId, companyId, isDefault, conn, null, ct);
    }

    public async Task AssignUserToCompanyAsync(Guid userId, Guid companyId, bool isDefault, IDbConnection conn, IDbTransaction? tx, CancellationToken ct)
    {
        // Idempotent insert. If is_default = true, demote any prior default to false first.
        if (isDefault)
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE user_companies SET is_default = false WHERE user_id = @UserId",
                new { UserId = userId }, transaction: tx, cancellationToken: ct));
        }
        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO user_companies (user_id, company_id, is_default, assigned_at)
            VALUES (@UserId, @CompanyId, @IsDefault, @AssignedAt)
            ON CONFLICT (user_id, company_id) DO UPDATE SET is_default = EXCLUDED.is_default",
            new
            {
                UserId = userId,
                CompanyId = companyId,
                IsDefault = isDefault,
                AssignedAt = DateTime.UtcNow
            }, transaction: tx, cancellationToken: ct));
    }

    // ============ Phase 6.2: Admin User CRUD ============

    public async Task UpdatePasswordAsync(Guid userId, string passwordHash, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"UPDATE users SET password_hash = @PasswordHash, updated_at = @UpdatedAt
                             WHERE id = @UserId";
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            UserId = userId,
            PasswordHash = passwordHash,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken: ct));
    }

    public async Task UpdateProfileAsync(Guid userId, string? fullName, string? email, bool? isActive, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        // Use COALESCE to only update fields that were provided
        const string sql = @"UPDATE users
                             SET full_name = COALESCE(@FullName, full_name),
                                 email = COALESCE(@Email, email),
                                 is_active = COALESCE(@IsActive, is_active),
                                 updated_at = @UpdatedAt
                             WHERE id = @UserId";
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            UserId = userId,
            FullName = fullName,
            Email = email,
            IsActive = isActive,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken: ct));
    }

    public async Task DeleteAsync(Guid userId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        // Soft delete: just deactivate
        const string sql = "UPDATE users SET is_active = false, updated_at = @UpdatedAt WHERE id = @UserId";
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            UserId = userId,
            UpdatedAt = DateTime.UtcNow
        }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Guid>> GetUserRoleIdsAsync(Guid userId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = "SELECT role_id FROM user_roles WHERE user_id = @UserId";
        var rows = await conn.QueryAsync<Guid>(new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task SetUserRolesAsync(Guid userId, IEnumerable<Guid> roleIds, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string deleteSql = "DELETE FROM user_roles WHERE user_id = @UserId";
        await conn.ExecuteAsync(new CommandDefinition(deleteSql, new { UserId = userId }, cancellationToken: ct));

        const string insertSql = "INSERT INTO user_roles (user_id, role_id) VALUES (@UserId, @RoleId)";
        foreach (var roleId in roleIds)
        {
            await conn.ExecuteAsync(new CommandDefinition(insertSql, new { UserId = userId, RoleId = roleId }, cancellationToken: ct));
        }
    }
}
