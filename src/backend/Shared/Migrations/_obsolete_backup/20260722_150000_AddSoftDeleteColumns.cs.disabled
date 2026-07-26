using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Migration 020 — DEC-052 P3: Soft delete for financial tables.
///
/// Adds `is_deleted` (default false) + `deleted_at` + `deleted_by` to:
/// - sales_invoices
/// - payments
/// - journal_entries
/// - users
///
/// Strategy: keep `deleted_at` (already used in vendors/customers — DEC-082) for
/// timestamp, add `is_deleted` boolean for fast filtering, and `deleted_by` for audit.
///
/// All changes idempotent (DO $$ ... END $$) for safe re-runs.
/// </summary>
[Migration(20260722_150000)]
public class AddSoftDeleteColumns : Migration
{
    private static readonly string[] Tables = new[]
    {
        "sales_invoices",
        "payments",
        "journal_entries",
        "users"
    };

    public override void Up()
    {
        foreach (var table in Tables)
        {
            // is_deleted: boolean, default false, fast index
            Execute.Sql($@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = '{table}' AND column_name = 'is_deleted'
                    ) THEN
                        ALTER TABLE {table} ADD COLUMN is_deleted BOOLEAN NOT NULL DEFAULT FALSE;
                        CREATE INDEX ix_{table}_is_deleted ON {table} (is_deleted);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = '{table}' AND column_name = 'deleted_by'
                    ) THEN
                        ALTER TABLE {table} ADD COLUMN deleted_by UUID NULL;
                    END IF;
                END $$;");
        }
    }

    public override void Down()
    {
        foreach (var table in Tables)
        {
            Execute.Sql($@"
                ALTER TABLE {table} DROP COLUMN IF EXISTS deleted_by;
                DROP INDEX IF EXISTS ix_{table}_is_deleted;
                ALTER TABLE {table} DROP COLUMN IF EXISTS is_deleted;");
        }
    }
}
