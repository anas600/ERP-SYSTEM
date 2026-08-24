using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Sprint 60 — Wave 1 (DEC-184) — Add Financial-Statement metadata columns to <c>accounts</c>.
///
/// <para><b>Why</b>: per Anas's CoA-Final-Proposal-2026-08-24, every account will eventually
/// carry metadata describing its role in the financial statements (balance sheet vs P&amp;L,
/// which section it belongs to, and whether it has been migrated to the new canonical
/// 4-level coding scheme). Wave 1 adds the columns + safe defaults so the schema is ready
/// for Wave 2 (the actual migration job) without breaking any existing reads/writes.</para>
///
/// <para><b>Columns added</b> (all nullable or have safe defaults — no break for existing data):</para>
/// <list type="bullet">
///   <item><c>fs_type</c> (TEXT) — 'BS' (Balance Sheet) or 'PL' (Profit &amp; Loss). NULL for legacy.</item>
///   <item><c>section</c> (TEXT) — 'Current Asset' | 'Non-Current Asset' | 'Current Liability' |
///         'Non-Current Liability' | 'Equity' | 'Revenue' | 'COGS' | 'OpEx' |
///         'Finance Income' | 'Finance Expense' | 'Tax' | 'Other' | 'Closing'. NULL for legacy.</item>
///   <item><c>is_canonical</c> (BOOLEAN DEFAULT TRUE) — TRUE for accounts using the canonical
///         4-level code. Set to FALSE for all existing rows (they still use the legacy code).</item>
///   <item><c>new_code</c> (TEXT) — the canonical 4-level code (e.g. '1.1.01.002'). NULL for legacy.</item>
///   <item><c>migration_status</c> (TEXT) — 'pending' | 'migrated' | 'new' | 'deprecated'.
///         Default 'pending' for existing rows (they still need migration).</item>
///   <item><c>migrated_at</c> (TIMESTAMPTZ NULLABLE) — when the account was migrated to canonical.
///         NULL for legacy / unmigrated.</item>
/// </list>
///
/// <para><b>Idempotency</b>: each ALTER uses <c>IF NOT EXISTS</c> (Postgres 9.6+), so the
/// migration is safely re-runnable.</para>
///
/// <para><b>Defaults for existing rows</b>:
/// <c>is_canonical = FALSE</c> and <c>migration_status = 'pending'</c>. Everything else NULL.</para>
///
/// <para><b>Down()</b>: drops the 6 columns. Safe because no Wave 2 code uses them yet.</para>
/// </summary>
[Migration(20260825_001)]
public class Sprint60_AddAccountFsMetadata : Migration
{
    public override void Up()
    {
        // Add 6 columns (all idempotent)
        Execute.Sql(@"
            ALTER TABLE accounts ADD COLUMN IF NOT EXISTS fs_type TEXT;
        ");
        Execute.Sql(@"
            ALTER TABLE accounts ADD COLUMN IF NOT EXISTS section TEXT;
        ");
        Execute.Sql(@"
            ALTER TABLE accounts ADD COLUMN IF NOT EXISTS is_canonical BOOLEAN NOT NULL DEFAULT TRUE;
        ");
        Execute.Sql(@"
            ALTER TABLE accounts ADD COLUMN IF NOT EXISTS new_code TEXT;
        ");
        Execute.Sql(@"
            ALTER TABLE accounts ADD COLUMN IF NOT EXISTS migration_status TEXT NOT NULL DEFAULT 'pending';
        ");
        Execute.Sql(@"
            ALTER TABLE accounts ADD COLUMN IF NOT EXISTS migrated_at TIMESTAMPTZ;
        ");

        // Mark all existing rows as legacy (not yet canonical) + pending migration.
        // Idempotent: only updates rows that are not yet marked.
        Execute.Sql(@"
            UPDATE accounts
            SET is_canonical = FALSE,
                migration_status = COALESCE(migration_status, 'pending')
            WHERE is_canonical = TRUE
              AND migration_status = 'pending';
        ");
    }

    public override void Down()
    {
        // Drop the 6 columns in reverse order. IF EXISTS so re-running Down is safe.
        Execute.Sql(@"ALTER TABLE accounts DROP COLUMN IF EXISTS migrated_at;");
        Execute.Sql(@"ALTER TABLE accounts DROP COLUMN IF EXISTS migration_status;");
        Execute.Sql(@"ALTER TABLE accounts DROP COLUMN IF EXISTS new_code;");
        Execute.Sql(@"ALTER TABLE accounts DROP COLUMN IF EXISTS is_canonical;");
        Execute.Sql(@"ALTER TABLE accounts DROP COLUMN IF EXISTS section;");
        Execute.Sql(@"ALTER TABLE accounts DROP COLUMN IF EXISTS fs_type;");
    }
}
