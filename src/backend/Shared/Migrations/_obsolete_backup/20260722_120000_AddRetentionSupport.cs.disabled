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
        // DEC-096: Wrap each CREATE INDEX in DO $$ ... IF EXISTS(table) ... so the
        // migration succeeds whether or not the table is present. The notifications
        // table doesn't have `deleted_at` column either — the partial index below
        // would fail with 42703 if the table exists but the column is missing.
        // We skip the WHERE clause when the column is absent.

        // 1. outbox_events(processed_at)
        Execute.Sql(@"
            DO $$ BEGIN
                IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'outbox_events')
                  AND EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'outbox_events' AND column_name = 'processed_at') THEN
                    CREATE INDEX IF NOT EXISTS ix_outbox_processed_at ON outbox_events (processed_at) WHERE processed_at IS NOT NULL;
                ELSE
                    RAISE NOTICE 'Skipping ix_outbox_processed_at: outbox_events or processed_at column missing';
                END IF;
            END $$");

        // 2. processed_events — covered by ix_processed_events_tenant_processed (DEC-106)

        // 3. notifications(created_at) — defensive: skip WHERE clause if deleted_at missing
        Execute.Sql(@"
            DO $$
            DECLARE has_deleted_at boolean;
            BEGIN
                IF NOT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'notifications') THEN
                    RAISE NOTICE 'Skipping ix_notifications_created_at: notifications table missing';
                    RETURN;
                END IF;
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name = 'notifications' AND column_name = 'deleted_at'
                ) INTO has_deleted_at;
                IF has_deleted_at THEN
                    CREATE INDEX IF NOT EXISTS ix_notifications_created_at ON notifications (created_at) WHERE deleted_at IS NULL;
                ELSE
                    CREATE INDEX IF NOT EXISTS ix_notifications_created_at ON notifications (created_at);
                END IF;
            END $$");

        // 4. refresh_tokens(expires_at)
        Execute.Sql(@"
            DO $$ BEGIN
                IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'refresh_tokens')
                  AND EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'refresh_tokens' AND column_name = 'revoked_at')
                  AND EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'refresh_tokens' AND column_name = 'expires_at') THEN
                    CREATE INDEX IF NOT EXISTS ix_refresh_tokens_expires_at ON refresh_tokens (expires_at) WHERE revoked_at IS NULL;
                ELSE
                    RAISE NOTICE 'Skipping ix_refresh_tokens_expires_at: refresh_tokens table or required columns missing';
                END IF;
            END $$");

        // 5. stock_movements(occurred_at)
        Execute.Sql(@"
            DO $$ BEGIN
                IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'stock_movements')
                  AND EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'stock_movements' AND column_name = 'occurred_at') THEN
                    CREATE INDEX IF NOT EXISTS ix_stock_movements_occurred_at ON stock_movements (occurred_at);
                ELSE
                    RAISE NOTICE 'Skipping ix_stock_movements_occurred_at: stock_movements or occurred_at column missing';
                END IF;
            END $$");

        // 6. audit_log(created_at) — covered by ix_audit_log_user composite
    }

    public override void Down()
    {
        Execute.Sql("DROP INDEX IF EXISTS ix_outbox_processed_at");
        Execute.Sql("DROP INDEX IF EXISTS ix_notifications_created_at");
        Execute.Sql("DROP INDEX IF EXISTS ix_refresh_tokens_expires_at");
        Execute.Sql("DROP INDEX IF EXISTS ix_stock_movements_occurred_at");
    }
}
