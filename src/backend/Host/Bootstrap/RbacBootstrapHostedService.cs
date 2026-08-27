// Sprint 63 (DEC-211..214) — RBAC foundation seed.
//
// Why this exists: Sprint 63 wires the system up to a permission-aware authorization
// layer. The 3 new tables (permissions, role_permissions, module_visibility) are created
// by the FluentMigrator migration Sprint63_RbacPermissionCatalog_20260827_170000.
// This hosted service seeds the 5 default role templates (admin, finance, hr, pm, readonly)
// with their permission grants + module visibility flags, all from
// Shared/SeedData/RbacSeedData.json (loaded via RbacSeedData).
//
// Order: runs AFTER DefaultHoldingBootstrapHostedService. The roles table is already
// populated by Phase 6 (4 default roles) and possibly extended by UserRepository code.
// The bootstrap is fully idempotent — re-running is a no-op.

using System.Data;
using Dapper;
using ERPSystem.Shared.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Host.Bootstrap;

/// <summary>
/// خدمة تعمل مرة واحدة مع بدء التطبيق — تبذر 5 أدوار افتراضية + ~82 صلاحية + مصفوفة
/// رؤية الوحدات (10 وحدات × 5 أدوار) في جداول <c>permissions</c> و
/// <c>role_permissions</c> و <c>module_visibility</c> (Sprint 63 / DEC-211..214).
/// <para>
/// <b>Idempotency</b>: أول خطوة هي التحقّق من وجود أي دور RBAC. إذا وُجد أي من
/// <c>(admin, finance, hr, pm, readonly)</c> (case-insensitive)، تنتهي الخدمة فوراً.
/// وإلا تبذر كل البيانات من <c>Shared/SeedData/RbacSeedData.json</c> عبر
/// <see cref="RbacSeedData.Load"/>. كل الـ INSERTs تستخدم <c>ON CONFLICT DO NOTHING</c>
/// أو pre-check لتكون آمنة عند التشغيل المتزامن.
/// </para>
/// <para>
/// <b>ملاحظة حول تعارض الأدوار</b>: Phase 6 كان يبذر 4 أدوار (Admin, Accountant,
/// ProjectManager, Viewer) بالأحرف الكبيرة. هذا الـ bootstrap يبذر 5 أدوار بأحرف
/// صغيرة (admin, finance, hr, pm, readonly). عمود <c>name</c> في <c>roles</c> له
/// <c>UNIQUE</c> غير حساس لحالة الأحرف، لذا <c>Admin</c> من Phase 6 يتعارض مع
/// <c>admin</c> الجديد. الحل: نستخدم <c>LOWER(name) = LOWER(@code)</c> للبحث، فإذا
/// وُجد دور (Admin أو admin) نستخدمه؛ وإلا نُدخل الاسم الجديد. كل الحالات idempotent.
/// </para>
/// </summary>
public sealed class RbacBootstrapHostedService : IHostedService
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<RbacBootstrapHostedService> _logger;

    public RbacBootstrapHostedService(
        IDbConnectionFactory db,
        ILogger<RbacBootstrapHostedService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Sprint63] RbacBootstrap starting");

        RbacSeedData.Seed seed;
        try
        {
            seed = RbacSeedData.Load();
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex,
                "[Sprint63] {Message} — RBAC bootstrap is a no-op. Authorization will deny everything (default-deny).",
                ex.Message);
            return;
        }
        catch (InvalidDataException ex)
        {
            _logger.LogError(ex,
                "[Sprint63] RbacSeedData.json is invalid: {Message} — bootstrap is a no-op.",
                ex.Message);
            return;
        }

        // Phase 6.3 hotfix: single ephemeral connection (mirrors DefaultHoldingBootstrap).
        using var conn = await _db.CreateEphemeralOltpConnectionAsync(cancellationToken);
        try
        {
            // 1) Idempotency check — لو في دور RBAC واحد على الأقل، نفترض أن البذر تمّ ونخرج.
            var existingRbacRole = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                @"SELECT COUNT(*) FROM roles WHERE LOWER(name) IN ('admin','finance','hr','pm','readonly')",
                cancellationToken: cancellationToken));
            if (existingRbacRole > 0)
            {
                _logger.LogInformation(
                    "[Sprint63] RBAC roles already seeded (count={Count}) — bootstrap is a no-op",
                    existingRbacRole);
                return;
            }

            // 2) ابذر الأدوار الخمسة.
            var roleIds = await SeedRolesAsync(conn, seed.Roles, cancellationToken);

            // 3) ابذر الصلاحيات.
            var permissionIdsByCode = await SeedPermissionsAsync(conn, seed.Permissions, cancellationToken);

            // 4) ابذر role_permissions.
            await SeedRolePermissionsAsync(conn, roleIds, permissionIdsByCode, seed.RolePermissions, cancellationToken);

            // 5) ابذر module_visibility.
            await SeedModuleVisibilityAsync(conn, roleIds, seed.ModuleVisibility, cancellationToken);

            _logger.LogInformation(
                "[Sprint63] RbacBootstrap completed (roles={Roles}, permissions={Perms})",
                roleIds.Count, permissionIdsByCode.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Sprint63] RbacBootstrap failed — application will start but RBAC will not be functional. " +
                "Check the logs above for the root cause.");
            // Non-fatal: better to start with empty RBAC than to crash the entire app.
            // The [RequirePermission] checks will simply deny everything (default-deny),
            // which is the safe failure mode.
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // ============ Seeding helpers (internal — tested via RbacSeedDataTests) ============

    private async Task<Dictionary<string, Guid>> SeedRolesAsync(
        IDbConnection conn, IReadOnlyList<RbacSeedData.Role> roles, CancellationToken ct)
    {
        var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in roles)
        {
            var existingId = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM roles WHERE LOWER(name) = LOWER(@Name) LIMIT 1",
                new { Name = r.Code }, cancellationToken: ct));

            if (existingId.HasValue)
            {
                result[r.Code] = existingId.Value;
                continue;
            }

            var newId = Guid.NewGuid();
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO roles (id, name, description, created_at)
                VALUES (@Id, @Name, @Description, @Now)
                ON CONFLICT (name) DO NOTHING",
                new
                {
                    Id = newId,
                    Name = r.Code,
                    Description = $"{r.Name} / {r.NameAr}",
                    Now = DateTime.UtcNow
                }, cancellationToken: ct));

            var finalId = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM roles WHERE LOWER(name) = LOWER(@Name) LIMIT 1",
                new { Name = r.Code }, cancellationToken: ct)) ?? newId;

            result[r.Code] = finalId;
        }
        return result;
    }

    private async Task<Dictionary<string, Guid>> SeedPermissionsAsync(
        IDbConnection conn, IReadOnlyList<RbacSeedData.Permission> perms, CancellationToken ct)
    {
        var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in perms)
        {
            var existingId = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM permissions WHERE code = @Code LIMIT 1",
                new { p.Code }, cancellationToken: ct));

            if (existingId.HasValue)
            {
                result[p.Code] = existingId.Value;
                continue;
            }

            var newId = Guid.NewGuid();
            await conn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO permissions (id, code, resource, action, name, name_ar, module, created_at)
                VALUES (@Id, @Code, @Resource, @Action, @Name, @NameAr, @Module, @Now)
                ON CONFLICT (code) DO NOTHING",
                new
                {
                    Id = newId,
                    p.Code,
                    p.Resource,
                    p.Action,
                    p.Name,
                    p.NameAr,
                    p.Module,
                    Now = DateTime.UtcNow
                }, cancellationToken: ct));

            var finalId = await conn.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                "SELECT id FROM permissions WHERE code = @Code LIMIT 1",
                new { p.Code }, cancellationToken: ct)) ?? newId;

            result[p.Code] = finalId;
        }
        _logger.LogInformation("[Sprint63] Seeded {Count} permissions", result.Count);
        return result;
    }

    private async Task SeedRolePermissionsAsync(
        IDbConnection conn,
        Dictionary<string, Guid> roleIds,
        Dictionary<string, Guid> permIds,
        Dictionary<string, List<string>> mapping,
        CancellationToken ct)
    {
        var inserted = 0;
        foreach (var (roleCode, codes) in mapping)
        {
            if (!roleIds.TryGetValue(roleCode, out var roleId))
            {
                _logger.LogWarning("[Sprint63] Role '{Role}' not found in seeded roles — skipping", roleCode);
                continue;
            }

            var resolvedCodes = codes.Contains("*")
                ? permIds.Keys.ToList()
                : codes;

            foreach (var code in resolvedCodes)
            {
                if (!permIds.TryGetValue(code, out var permId))
                {
                    _logger.LogWarning(
                        "[Sprint63] Permission code '{Code}' referenced in role '{Role}' but not in catalog — skipping",
                        code, roleCode);
                    continue;
                }
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO role_permissions (id, role_id, permission_id, created_at)
                    VALUES (@Id, @RoleId, @PermId, @Now)
                    ON CONFLICT (role_id, permission_id) DO NOTHING",
                    new
                    {
                        Id = Guid.NewGuid(),
                        RoleId = roleId,
                        PermId = permId,
                        Now = DateTime.UtcNow
                    }, cancellationToken: ct));
                inserted++;
            }
        }
        _logger.LogInformation("[Sprint63] Seeded {Count} role_permission mappings", inserted);
    }

    private async Task SeedModuleVisibilityAsync(
        IDbConnection conn,
        Dictionary<string, Guid> roleIds,
        Dictionary<string, Dictionary<string, bool>> visibility,
        CancellationToken ct)
    {
        var inserted = 0;
        foreach (var (roleCode, modules) in visibility)
        {
            if (!roleIds.TryGetValue(roleCode, out var roleId))
            {
                _logger.LogWarning("[Sprint63] Role '{Role}' not found in seeded roles — skipping module visibility", roleCode);
                continue;
            }

            foreach (var (module, isVisible) in modules)
            {
                await conn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO module_visibility (id, role_id, module, is_visible, created_at)
                    VALUES (@Id, @RoleId, @Module, @IsVisible, @Now)
                    ON CONFLICT (role_id, module) DO NOTHING",
                    new
                    {
                        Id = Guid.NewGuid(),
                        RoleId = roleId,
                        Module = module,
                        IsVisible = isVisible,
                        Now = DateTime.UtcNow
                    }, cancellationToken: ct));
                inserted++;
            }
        }
        _logger.LogInformation("[Sprint63] Seeded {Count} module_visibility rows", inserted);
    }
}
