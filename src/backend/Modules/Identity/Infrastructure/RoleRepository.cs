using System.Data;
using Dapper;
using ERPSystem.Modules.Identity.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Identity.Infrastructure;

public sealed class RoleRepository : IRoleRepository
{
    private readonly IDbConnectionFactory _db;

    public RoleRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Role?> GetByNameAsync(string name, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        return await GetByNameAsync(name, conn, null, ct);
    }

    public async Task<Role?> GetByNameAsync(string name, IDbConnection conn, IDbTransaction? tx, CancellationToken ct)
    {
        const string sql = @"SELECT id, name, description, created_at AS CreatedAt
                             FROM roles WHERE LOWER(name) = LOWER(@Name) LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<Role>(new CommandDefinition(sql, new { Name = name }, transaction: tx, cancellationToken: ct));
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"SELECT id, name, description, created_at AS CreatedAt
                             FROM roles WHERE id = @Id LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<Role>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    /// <summary>
    /// Sprint 63 (DEC-216): returns the user ids that currently hold
    /// <paramref name="roleId"/> (via the <c>user_roles</c> join table).
    /// Used by AdminPermissionsController to invalidate the IPermissionService
    /// cache for every member of a role. Returns an empty list when the
    /// role has no members.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> GetUserIdsInRoleAsync(Guid roleId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = "SELECT user_id FROM user_roles WHERE role_id = @RoleId";
        var rows = await conn.QueryAsync<Guid>(
            new CommandDefinition(sql, new { RoleId = roleId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task InsertAsync(Role role, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await InsertAsync(role, conn, null, ct);
    }

    public async Task InsertAsync(Role role, IDbConnection conn, IDbTransaction? tx, CancellationToken ct)
    {
        const string sql = @"INSERT INTO roles (id, name, description, created_at)
                             VALUES (@Id, @Name, @Description, @CreatedAt)";
        await conn.ExecuteAsync(new CommandDefinition(sql, role, transaction: tx, cancellationToken: ct));
    }

    public async Task EnsureDefaultRolesAsync(CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        await EnsureDefaultRolesAsync(conn, null, ct);
    }

    public async Task EnsureDefaultRolesAsync(IDbConnection conn, IDbTransaction? tx, CancellationToken ct)
    {
        // الأدوار الافتراضية للنظام (global — لا توجد عزل بين المستأجرين في Phase 6.1b)
        var defaults = new (string Name, string Description)[]
        {
            ("Admin", "مدير النظام — صلاحيات كاملة"),
            ("Accountant", "محاسب — يدير القيود والفواتير"),
            ("ProjectManager", "مدير مشاريع"),
            ("Viewer", "صلاحيات قراءة فقط"),
        };

        foreach (var (name, desc) in defaults)
        {
            var existing = await GetByNameAsync(name, conn, tx, ct);
            if (existing == null)
            {
                await InsertAsync(new Role
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Description = desc,
                    CreatedAt = DateTime.UtcNow
                }, conn, tx, ct);
            }
        }
    }
}
