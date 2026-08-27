using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Sprint 64 — Wave 2A (DEC-223 + DEC-224) — Sub-ProgressBilling + Sub-Payment schema.
///
/// <para><b>Why</b>: Tracks monthly progress billings from the subcontractor
/// (work done, % complete, retention withheld) and the actual payments made
/// against each billing. Together they form the sub-contract ledger that feeds
/// into the SubStatement (DEC-225 / Wave 3A) and the Project P&L (DEC-161).</para>
///
/// <para><b>DEC-223 — sub_progress_billings</b> (new table):</para>
/// <list type="bullet">
///   <item>Linked to sub_contracts (FK + same-company scope)</item>
///   <item>Captures: billing #, date, work completed % (cumulative), gross amount,
///         retention deducted, previous billings total, net payable</item>
///   <item><c>status</c>: 1=Draft, 2=Approved, 3=Paid, 4=Cancelled</item>
///   <item>UNIQUE (sub_contract_id, billing_number) — no duplicate billing numbers per sub-contract</item>
///   <item>Non-unique index on sub_contract_id for "all billings for a sub-contract" queries</item>
/// </list>
///
/// <para><b>DEC-224 — sub_payments</b> (new table):</para>
/// <list type="bullet">
///   <item>Linked to sub_contracts + sub_progress_billings (FK + same-company scope)</item>
///   <item>Captures: payment #, date, amount, retention released (for release-retention flow), method, ref</item>
///   <item><c>retention_released</c> = 0 for regular payments, > 0 for retention-release payments</item>
///   <item>UNIQUE (sub_contract_id, payment_number) — no duplicate payment numbers per sub-contract</item>
///   <item>Non-unique index on sub_progress_billing_id for "all payments for a billing" queries</item>
/// </list>
///
/// <para><b>Idempotency</b>: every CREATE TABLE / INDEX uses <c>IF NOT EXISTS</c>.
/// The migration is safely re-runnable against an already-migrated DB.</para>
///
/// <para><b>Article 3 compliance</b>: every table has <c>company_id NOT NULL</c>
/// (per L19 / L29 / L30 lessons). The service layer resolves the company from
/// the JWT context, never from the request DTO.</para>
///
/// <para><b>Down()</b>: drops the two new tables (CASCADE for FK safety).
/// Each DROP uses IF EXISTS so re-running Down is a no-op.</para>
/// </summary>
[Migration(20260827_190000)]
public class Sprint64_SubProgressBilling : Migration
{
    public override void Up()
    {
        // ============== DEC-223 — sub_progress_billings (new table) ==============
        Execute.Sql(@"
            CREATE TABLE IF NOT EXISTS sub_progress_billings (
                id UUID PRIMARY KEY,
                company_id UUID NOT NULL REFERENCES companies(id),
                sub_contract_id UUID NOT NULL REFERENCES sub_contracts(id),
                billing_number VARCHAR(50) NOT NULL,
                billing_date DATE NOT NULL,
                period_from DATE,
                period_to DATE,
                work_completed_percent NUMERIC(5,2) NOT NULL,
                gross_amount NUMERIC(18,4) NOT NULL DEFAULT 0,
                retention_deducted NUMERIC(18,4) NOT NULL DEFAULT 0,
                previous_billings_amount NUMERIC(18,4) NOT NULL DEFAULT 0,
                net_payable NUMERIC(18,4) NOT NULL DEFAULT 0,
                status INT NOT NULL DEFAULT 1,
                notes TEXT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
        ");

        // UNIQUE (sub_contract_id, billing_number) — no duplicate billing numbers per sub-contract.
        Execute.Sql(@"
            CREATE UNIQUE INDEX IF NOT EXISTS ux_sub_progress_billings_sub_contract_number
                ON sub_progress_billings(sub_contract_id, billing_number);
        ");

        // Non-unique index for "all billings for a sub-contract" queries.
        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_sub_progress_billings_sub_contract
                ON sub_progress_billings(sub_contract_id);
        ");

        // ============== DEC-224 — sub_payments (new table) ==============
        Execute.Sql(@"
            CREATE TABLE IF NOT EXISTS sub_payments (
                id UUID PRIMARY KEY,
                company_id UUID NOT NULL REFERENCES companies(id),
                sub_contract_id UUID NOT NULL REFERENCES sub_contracts(id),
                sub_progress_billing_id UUID NOT NULL REFERENCES sub_progress_billings(id),
                payment_number VARCHAR(50) NOT NULL,
                payment_date DATE NOT NULL,
                amount NUMERIC(18,4) NOT NULL,
                retention_released NUMERIC(18,4) NOT NULL DEFAULT 0,
                payment_method TEXT,
                reference_number TEXT,
                notes TEXT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
        ");

        // UNIQUE (sub_contract_id, payment_number) — no duplicate payment numbers per sub-contract.
        Execute.Sql(@"
            CREATE UNIQUE INDEX IF NOT EXISTS ux_sub_payments_sub_contract_number
                ON sub_payments(sub_contract_id, payment_number);
        ");

        // Non-unique index for "all payments for a billing" queries.
        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_sub_payments_sub_progress_billing
                ON sub_payments(sub_progress_billing_id);
        ");
    }

    public override void Down()
    {
        // Reverse order: drop the dependent table first, then the parent table.
        // IF EXISTS guards so re-running Down is a no-op.
        Execute.Sql("DROP TABLE IF EXISTS sub_payments CASCADE;");
        Execute.Sql("DROP TABLE IF EXISTS sub_progress_billings CASCADE;");
    }
}
