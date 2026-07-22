using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Migration 017 — DEC-052: Retention policy support (DL-141).
///
/// Adds retention-related indexes to speed up nightly cleanup jobs:
/// - outbox_events(processed_at) — for "delete processed > 30d"
/// - processed_events(processed_at) — for "delete > 30d"
/// - notifications(created_at) — for "delete read > 90d"
/// - refresh_tokens(expires_at) — for "delete expired > 30d"
///
/// Does NOT add archived_at columns (per DEC-052 P2 — not P1).
///
/// Indexes are created CONCURRENTLY to avoid table locks (PostgreSQL 11+).
/// </summary>
[Migration(20260722_120000)]
public class AddRetentionSupport : Migration
{
    public override void Up()
    {
        // 1. outbox_events(processed_at) — accelerate nightly cleanup
        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_outbox_processed_at
            ON outbox_events (processed_at)
            WHERE processed_at IS NOT NULL");

        // 2. processed_events(processed_at) — already exists in JSON, but verify
        //    (DEC-106 added ix_processed_events_tenant_processed which covers this)

        // 3. notifications(created_at) — for 90-day retention
        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_notifications_created_at
            ON notifications (created_at)
            WHERE deleted_at IS NULL");

        // 4. refresh_tokens(expires_at) — for "delete expired > 30d"
        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_refresh_tokens_expires_at
            ON refresh_tokens (expires_at)
            WHERE revoked_at IS NULL");

        // 5. stock_movements(occurred_at) — for 3-year cleanup (P2)
        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_stock_movements_occurred_at
            ON stock_movements (occurred_at)");

        // 6. audit_log(created_at) — for 7-year retention (P2)
        //    Already covered by ix_audit_log_user composite (DEC-079 JSON)
    }

    public override void Down()
    {
        Execute.Sql("DROP INDEX IF EXISTS ix_outbox_processed_at");
        Execute.Sql("DROP INDEX IF EXISTS ix_notifications_created_at");
        Execute.Sql("DROP INDEX IF EXISTS ix_refresh_tokens_expires_at");
        Execute.Sql("DROP INDEX IF EXISTS ix_stock_movements_occurred_at");
    }
}
