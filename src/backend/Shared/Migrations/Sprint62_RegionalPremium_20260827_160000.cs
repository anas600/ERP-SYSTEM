using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Sprint 62 — Wave 1A (DEC-197) — Regional Premium schema.
///
/// <para><b>Why</b>: per client meeting 27-Aug-2026 (Anas + محاسب), Libyan construction
/// projects in certain regions (NDB-Oil, NDB-Gas, parts of Tripoli/Benghazi/Misrata)
/// require the contractor to withhold three statutory deductions on every progress
/// billing — NDB (1.5%) + CIT (5%) + SS (variable). Until now these were
/// post-billing manual journal entries; DEC-197 makes them automatic in the
/// billing calculation.</para>
///
/// <para><b>DEC-197 — regional_premiums</b> (new table):</para>
/// <list type="bullet">
///   <item>One row per (project_id, region) — UNIQUE constraint enforces this</item>
///   <item>Stores NDB% / CIT% / SS% percentages (DECIMAL(5,2))</item>
///   <item><c>is_active</c> flag: only the active row is applied in calculations;
///         historical rows can be retained when rates change</item>
///   <item>Defaults: NDB=1.5, CIT=5.0, SS=0.0 (typical Libyan fixed-price contract)</item>
/// </list>
///
/// <para><b>DEC-197 — progress_billings (schema evolution)</b>:</para>
/// <list type="bullet">
///   <item>Added <c>regional_premium_deducted NUMERIC(18,4) NOT NULL DEFAULT 0</c></item>
///   <item>Added <c>net_amount_after_premium NUMERIC(18,4) NOT NULL DEFAULT 0</c></item>
///   <item>Existing rows: both default to 0 (no premium applied retroactively —
///         historical billings remain as they were posted)</item>
/// </list>
///
/// <para><b>Idempotency</b>: CREATE TABLE uses <c>IF NOT EXISTS</c>; CREATE INDEX uses
/// <c>IF NOT EXISTS</c>; the ALTER TABLE … ADD COLUMN uses Postgres' <c>IF NOT EXISTS</c>
/// guard (added in PG 9.6+). The migration is safely re-runnable against an
/// already-migrated DB.</para>
///
/// <para><b>Down()</b>: drops the new table and the two new columns. Each DROP uses
/// IF EXISTS so re-running Down is a no-op.</para>
///
/// <para><b>Article 3 compliance</b>: every table / column has <c>company_id NOT NULL</c>
/// on regional_premiums. progress_billings already had company_id (Sprint 22) — the
/// two new columns inherit the same company scoping through the existing column.</para>
/// </summary>
[Migration(20260827_160000)]
public class Sprint62_RegionalPremium : Migration
{
    public override void Up()
    {
        // ============== DEC-197 — regional_premiums (new table) ==============
        Execute.Sql(@"
            CREATE TABLE IF NOT EXISTS regional_premiums (
                id UUID PRIMARY KEY,
                company_id UUID NOT NULL REFERENCES companies(id),
                project_id UUID NOT NULL REFERENCES projects(id),
                region TEXT NOT NULL,
                ndb_percent DECIMAL(5,2) NOT NULL DEFAULT 1.5,
                cit_percent DECIMAL(5,2) NOT NULL DEFAULT 5.0,
                ss_percent DECIMAL(5,2) NOT NULL DEFAULT 0.0,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                UNIQUE (project_id, region)
            );
        ");

        // Lookup index for ListByProjectAsync (active premium lookup during billing).
        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_regional_premiums_company_project
                ON regional_premiums(company_id, project_id);
        ");

        // ============== DEC-197 — progress_billings (extend) ==============
        // Two new columns capture the regional premium deduction + the post-premium
        // net amount. Idempotent via Postgres IF NOT EXISTS (PG 9.6+).
        Execute.Sql(@"
            ALTER TABLE progress_billings
                ADD COLUMN IF NOT EXISTS regional_premium_deducted NUMERIC(18,4) NOT NULL DEFAULT 0;
        ");

        Execute.Sql(@"
            ALTER TABLE progress_billings
                ADD COLUMN IF NOT EXISTS net_amount_after_premium NUMERIC(18,4) NOT NULL DEFAULT 0;
        ");
    }

    public override void Down()
    {
        // Reverse order: drop new columns first, then drop the new table.
        // IF EXISTS guards so re-running Down is a no-op.
        Execute.Sql("ALTER TABLE progress_billings DROP COLUMN IF EXISTS net_amount_after_premium;");
        Execute.Sql("ALTER TABLE progress_billings DROP COLUMN IF EXISTS regional_premium_deducted;");
        Execute.Sql("DROP TABLE IF EXISTS regional_premiums CASCADE;");
    }
}
