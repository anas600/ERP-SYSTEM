using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Migration 018 — DEC-052 P2: T1 warm storage (table partitioning by year).
///
/// Strategy: Convert high-volume tables to partitioned tables for T1 warm tier.
/// Hot data (current year) stays in default partition; older data moves to yearly partitions.
/// Combined with Tier 2 archive (move to R2) for full lifecycle.
///
/// Approach (PostgreSQL native partitioning):
/// - For large log tables (audit_log): partition by RANGE(created_at) — yearly
/// - For stock_movements: keep flat (less volume, T2 archive suffices)
///
/// Why partitioning?
/// - Faster cleanup (DROP PARTITION is O(1) vs DELETE which is O(n))
/// - Better query performance (partition pruning)
/// - Easier tiered storage management
///
/// Note: FluentMigrator runs migrations against fresh DB.
[Migration(20260722_130000)]
public class AddRetentionTier1Warm : Migration
{
    public override void Up()
    {
        // 1. audit_log: Convert to partitioned table (yearly)
        //    Step 1: Rename existing
        //    Step 2: Create partitioned parent
        //    Step 3: Recreate indexes on parent
        //    Step 4: Create default partition
        //    Step 5: (Future migrations will add yearly partitions)
        Execute.Sql(@"
            -- Rename existing
            ALTER TABLE audit_log RENAME TO audit_log_legacy;

            -- Create partitioned parent
            CREATE TABLE audit_log (
                id BIGSERIAL,
                tenant_id UUID NOT NULL,
                entity_type VARCHAR(100) NOT NULL,
                entity_id UUID,
                action VARCHAR(50) NOT NULL,
                user_id UUID,
                changes JSONB,
                ip_address INET,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                PRIMARY KEY (id, created_at)
            ) PARTITION BY RANGE (created_at);

            -- Create default partition for current data
            CREATE TABLE audit_log_default PARTITION OF audit_log DEFAULT;

            -- Migrate existing data
            INSERT INTO audit_log (id, tenant_id, entity_type, entity_id, action, user_id, changes, ip_address, created_at)
            SELECT id, tenant_id, entity_type, entity_id, action, user_id, changes, ip_address, created_at
            FROM audit_log_legacy;

            -- Reset sequence
            SELECT setval('audit_log_id_seq', (SELECT MAX(id) FROM audit_log_legacy));

            -- Drop legacy
            DROP TABLE audit_log_legacy;

            -- Recreate indexes on parent (auto-propagated to partitions)
            CREATE INDEX IF NOT EXISTS ix_audit_log_tenant_user
                ON audit_log (tenant_id, user_id, created_at);
            CREATE INDEX IF NOT EXISTS ix_audit_log_entity
                ON audit_log (entity_type, entity_id);");

        // 2. Pre-create yearly partitions (current year + 2 ahead)
        var currentYear = DateTime.UtcNow.Year;
        for (int year = currentYear; year <= currentYear + 2; year++)
        {
            var start = $"{year}-01-01";
            var end = $"{year + 1}-01-01";
            Execute.Sql($@"
                CREATE TABLE IF NOT EXISTS audit_log_y{year}
                PARTITION OF audit_log
                FOR VALUES FROM ('{start}') TO ('{end}');");
        }

        // 3. stock_movements: add 'archived' flag for T1 warm tier
        //    (No partitioning — moves to R2 instead at T2)
        Execute.Sql(@"
            ALTER TABLE stock_movements
            ADD COLUMN IF NOT EXISTS archived_at TIMESTAMPTZ NULL;

            CREATE INDEX IF NOT EXISTS ix_stock_movements_archived_at
            ON stock_movements (archived_at)
            WHERE archived_at IS NOT NULL;");

        // 4. audit_log: add 'archived' flag for tracking T2 export
        Execute.Sql(@"
            ALTER TABLE audit_log
            ADD COLUMN IF NOT EXISTS archived_at TIMESTAMPTZ NULL;

            CREATE INDEX IF NOT EXISTS ix_audit_log_archived_at
            ON audit_log (archived_at)
            WHERE archived_at IS NOT NULL;");
    }

    public override void Down()
    {
        // Reverse: drop new columns + recreate flat table
        Execute.Sql("ALTER TABLE audit_log DROP COLUMN IF EXISTS archived_at;");
        Execute.Sql("ALTER TABLE stock_movements DROP COLUMN IF EXISTS archived_at;");

        // Drop partitioned table (cascade drops all partitions)
        Execute.Sql(@"
            ALTER TABLE audit_log RENAME TO audit_log_partitioned;

            CREATE TABLE audit_log (
                id BIGSERIAL PRIMARY KEY,
                tenant_id UUID NOT NULL,
                entity_type VARCHAR(100) NOT NULL,
                entity_id UUID,
                action VARCHAR(50) NOT NULL,
                user_id UUID,
                changes JSONB,
                ip_address INET,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            INSERT INTO audit_log (id, tenant_id, entity_type, entity_id, action, user_id, changes, ip_address, created_at)
            SELECT id, tenant_id, entity_type, entity_id, action, user_id, changes, ip_address, created_at
            FROM audit_log_partitioned;

            SELECT setval('audit_log_id_seq', (SELECT MAX(id) FROM audit_log_partitioned));

            DROP TABLE audit_log_partitioned CASCADE;

            CREATE INDEX IF NOT EXISTS ix_audit_log_tenant_user
                ON audit_log (tenant_id, user_id, created_at);
            CREATE INDEX IF NOT EXISTS ix_audit_log_entity
                ON audit_log (entity_type, entity_id);");
    }
}
