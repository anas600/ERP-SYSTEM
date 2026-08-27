using System.Data;
using Dapper;
using ERPSystem.Modules.Identity.Entities;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Identity.Infrastructure;

/// <summary>
/// Sprint 63 (DEC-213) — Dapper impl of <see cref="IModuleVisibilityRepository"/>.
/// <para>
/// All queries use snake_case column names (DB) mapped to PascalCase properties (entity)
/// via the <c>AS</c> aliases. No EF Core.
/// </para>
/// </summary>
public sealed class ModuleVisibilityRepository : IModuleVisibilityRepository
{
    private readonly IDbConnectionFactory _db;

    public ModuleVisibilityRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<ModuleVisibility>> ListByRoleAsync(Guid roleId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            SELECT id, role_id AS RoleId, module, is_visible AS IsVisible, created_at AS CreatedAt
            FROM module_visibility
            WHERE role_id = @RoleId
            ORDER BY module";
        var rows = await conn.QueryAsync<ModuleVisibility>(
            new CommandDefinition(sql, new { RoleId = roleId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<ModuleVisibility>> ListByUserAsync(Guid userId, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        // DISTINCT on module because a user with multiple roles can see the same module
        // through several roles.
        const string sql = @"
            SELECT DISTINCT mv.id, mv.role_id AS RoleId, mv.module, mv.is_visible AS IsVisible, mv.created_at AS CreatedAt
            FROM module_visibility mv
            INNER JOIN user_roles ur ON ur.role_id = mv.role_id
            WHERE ur.user_id = @UserId
              AND mv.is_visible = TRUE
            ORDER BY mv.module";
        var rows = await conn.QueryAsync<ModuleVisibility>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task InsertAsync(ModuleVisibility visibility, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        // Idempotent: re-inserting the same (role_id, module) is a no-op.
        const string sql = @"
            INSERT INTO module_visibility (id, role_id, module, is_visible, created_at)
            VALUES (@Id, @RoleId, @Module, @IsVisible, @CreatedAt)
            ON CONFLICT (role_id, module) DO NOTHING";
        await conn.ExecuteAsync(new CommandDefinition(sql, visibility, cancellationToken: ct));
    }

    public async Task UpdateAsync(Guid roleId, string module, bool isVisible, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            UPDATE module_visibility
            SET is_visible = @IsVisible
            WHERE role_id = @RoleId AND module = @Module";
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { RoleId = roleId, Module = module, IsVisible = isVisible }, cancellationToken: ct));
    }

    public async Task DeleteAsync(Guid roleId, string module, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = @"
            DELETE FROM module_visibility
            WHERE role_id = @RoleId AND module = @Module";
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { RoleId = roleId, Module = module }, cancellationToken: ct));
    }
}
