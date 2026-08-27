using System.Data;
using Dapper;
using ERPSystem.Modules.Identity.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Identity.Infrastructure;

/// <summary>
/// Sprint 63 (DEC-212) — Dapper impl of <see cref="IRolePermissionRepository"/>.
/// <para>
/// All queries use snake_case column names (DB) mapped to PascalCase properties (entity)
/// via the <c>AS</c> aliases. No EF Core.
/// </para>
/// </summary>
public sealed class RolePermissionRepository : IRolePermissionRepository
{
    private readonly IDbConnectionFactory _db;

    public RolePermissionRepository(IDbConnectionFactory db) => _db = db;

    public async Task InsertAsync(RolePermission rolePermission, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        // Idempotent: re-inserting the same (role_id, permission_id) is a no-op.
        const string sql = @"
            INSERT INTO role_permissions (id, role_id, permission_id, created_at)
            VALUES (@Id, @RoleId, @PermissionId, @CreatedAt)
            ON CONFLICT (role_id, permission_id) DO NOTHING";
        await conn.ExecuteAsync(new CommandDefinition(sql, rolePermission, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Permission>> ListByRoleAsync(Guid roleId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT p.id, p.code, p.resource, p.action, p.name, p.name_ar AS NameAr, p.module, p.created_at AS CreatedAt
            FROM permissions p
            INNER JOIN role_permissions rp ON rp.permission_id = p.id
            WHERE rp.role_id = @RoleId
            ORDER BY p.module, p.resource, p.action";
        var rows = await conn.QueryAsync<Permission>(
            new CommandDefinition(sql, new { RoleId = roleId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<Permission>> ListByUserAsync(Guid userId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        // Effective permission set: union of all permissions granted to any of the user's roles.
        // DISTINCT prevents duplicates when a user holds multiple roles that share a permission.
        const string sql = @"
            SELECT DISTINCT p.id, p.code, p.resource, p.action, p.name, p.name_ar AS NameAr, p.module, p.created_at AS CreatedAt
            FROM permissions p
            INNER JOIN role_permissions rp ON rp.permission_id = p.id
            INNER JOIN user_roles ur ON ur.role_id = rp.role_id
            WHERE ur.user_id = @UserId
            ORDER BY p.module, p.resource, p.action";
        var rows = await conn.QueryAsync<Permission>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task DeleteAsync(Guid roleId, Guid permissionId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            DELETE FROM role_permissions
            WHERE role_id = @RoleId AND permission_id = @PermissionId";
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { RoleId = roleId, PermissionId = permissionId }, cancellationToken: ct));
    }
}
