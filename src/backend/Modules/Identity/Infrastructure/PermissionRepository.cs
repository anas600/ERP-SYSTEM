using System.Data;
using Dapper;
using ERPSystem.Modules.Identity.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Identity.Infrastructure;

/// <summary>
/// Sprint 63 (DEC-211) — Dapper impl of <see cref="IPermissionRepository"/>.
/// <para>
/// All queries use snake_case column names (DB) mapped to PascalCase properties (entity)
/// via the <c>AS</c> aliases. No EF Core.
/// </para>
/// </summary>
public sealed class PermissionRepository : IPermissionRepository
{
    private readonly IDbConnectionFactory _db;

    public PermissionRepository(IDbConnectionFactory db) => _db = db;

    public async Task<Permission?> GetByCodeAsync(string code, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT id, code, resource, action, name, name_ar AS NameAr, module, created_at AS CreatedAt
            FROM permissions
            WHERE code = @Code
            LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<Permission>(
            new CommandDefinition(sql, new { Code = code }, cancellationToken: ct));
    }

    public async Task<Permission?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT id, code, resource, action, name, name_ar AS NameAr, module, created_at AS CreatedAt
            FROM permissions
            WHERE id = @Id
            LIMIT 1";
        return await conn.QueryFirstOrDefaultAsync<Permission>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Permission>> ListAsync(CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT id, code, resource, action, name, name_ar AS NameAr, module, created_at AS CreatedAt
            FROM permissions
            ORDER BY module, resource, action";
        var rows = await conn.QueryAsync<Permission>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.AsList();
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

    public async Task InsertAsync(Permission permission, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        // ON CONFLICT (code) DO NOTHING — idempotent for re-runs of the bootstrap.
        const string sql = @"
            INSERT INTO permissions (id, code, resource, action, name, name_ar, module, created_at)
            VALUES (@Id, @Code, @Resource, @Action, @Name, @NameAr, @Module, @CreatedAt)
            ON CONFLICT (code) DO NOTHING";
        await conn.ExecuteAsync(new CommandDefinition(sql, permission, cancellationToken: ct));
    }
}
