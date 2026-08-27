using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Sprint 63 (DEC-211..214) — RBAC permission catalog + role-permission mapping + module visibility.
/// <para>
/// <b>Why</b>: per Sprint 63 hand-off (<c>docs/workflow/sprint-63.md</c>), the 9 modules need a
/// permission-aware authorization layer. The existing <c>roles</c> + <c>user_roles</c> tables
/// (Phase 6) are global and not permission-scoped. This migration creates the 3 new tables that
/// form the catalog layer:
/// <list type="bullet">
///   <item><b><c>permissions</c></b> — catalog of (resource, action) tuples, e.g. ("projects", "create").</item>
///   <item><b><c>role_permissions</c></b> — M2M roles → permissions (a role grants N permissions).</item>
///   <item><b><c>module_visibility</c></b> — per-role visibility flag for each module (controls
///         sidebar visibility on the FE; server also returns this so the BE can omit invisible modules).</item>
/// </list>
/// </para>
/// <para>
/// <b>DEC-211 — permissions</b>: <c>(id, code, resource, action, name, name_ar, module, created_at)</c>.
/// <c>code</c> is the canonical string used at authorization-check time (e.g. "projects.create").
/// <c>(resource, action)</c> is unique so the catalog cannot contain duplicates like
/// ("projects", "create") with two different codes.
/// </para>
/// <para>
/// <b>DEC-212 — role_permissions</b>: <c>(id, role_id, permission_id, created_at)</c> with FK
/// to <c>roles</c> and <c>permissions</c> (both ON DELETE CASCADE so role deletion cleans up).
/// Unique on <c>(role_id, permission_id)</c>.
/// </para>
/// <para>
/// <b>DEC-213 — module_visibility</b>: <c>(id, role_id, module, is_visible, created_at)</c>.
/// <c>module</c> is a free-text label (e.g. "Projects", "Finance") that matches the 9
/// module names in the architecture doc. Unique on <c>(role_id, module)</c>.
/// </para>
/// <para>
/// <b>Idempotency</b>: every DDL uses <c>IF NOT EXISTS</c> guards. Re-running this migration
/// after a successful run is a safe no-op. The seeding of rows happens in
/// <see cref="ERPSystem.Host.Bootstrap.RbacBootstrapHostedService"/> (post-migration), which
/// is also idempotent via <c>ON CONFLICT</c> / pre-check.
/// </para>
/// <para>
/// <b>Down()</b>: not supported. Drop the three tables manually if a rollback is required.
/// </para>
/// </summary>
[Migration(20260827_170000, TransactionBehavior.None)]
public class Sprint63_RbacPermissionCatalog : Migration
{
    public override void Up()
    {
        // ====================================================================
        // DEC-211 — permissions catalog
        // ====================================================================
        Execute.Sql(@"
            CREATE TABLE IF NOT EXISTS permissions (
                id UUID PRIMARY KEY,
                code TEXT NOT NULL UNIQUE,
                resource TEXT NOT NULL,
                action TEXT NOT NULL,
                name TEXT NOT NULL,
                name_ar TEXT,
                module TEXT NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT now()
            );
        ");

        Execute.Sql(@"
            CREATE UNIQUE INDEX IF NOT EXISTS ux_permissions_resource_action
                ON permissions(resource, action);
        ");

        // ====================================================================
        // DEC-212 — role_permissions (M2M)
        // ON DELETE CASCADE on both FKs: deleting a role or a permission
        // should clean up the mapping rows automatically.
        // ====================================================================
        Execute.Sql(@"
            CREATE TABLE IF NOT EXISTS role_permissions (
                id UUID PRIMARY KEY,
                role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
                permission_id UUID NOT NULL REFERENCES permissions(id) ON DELETE CASCADE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT now()
            );
        ");

        Execute.Sql(@"
            CREATE UNIQUE INDEX IF NOT EXISTS ux_role_permissions_role_perm
                ON role_permissions(role_id, permission_id);
        ");

        // ====================================================================
        // DEC-213/214 — module_visibility (per-role module access)
        // No FK to a modules table (modules are a free-text enum, not a
        // physical table). FK to roles only.
        // ====================================================================
        Execute.Sql(@"
            CREATE TABLE IF NOT EXISTS module_visibility (
                id UUID PRIMARY KEY,
                role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
                module TEXT NOT NULL,
                is_visible BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT now()
            );
        ");

        Execute.Sql(@"
            CREATE UNIQUE INDEX IF NOT EXISTS ux_module_visibility_role_module
                ON module_visibility(role_id, module);
        ");
    }

    public override void Down()
    {
        // Sprint 63 is forward-only; rollback is by restoring the pre-Sprint-63 git commit.
        throw new NotSupportedException(
            "Sprint 63 RBAC migration is not reversible. " +
            "Restore the pre-Sprint-63 git commit and re-run earlier migrations to revert.");
    }
}
