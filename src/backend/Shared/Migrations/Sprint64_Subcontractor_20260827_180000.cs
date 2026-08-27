using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Sprint 64 — Wave 1A (DEC-221 + DEC-222) — Subcontractor + Sub-Contract schema.
///
/// <para><b>Why</b>: Construction companies regularly hire subcontractors
/// (electricians, plumbers, carpenters, etc.) and need to track them as
/// first-class entities. A subcontractor is a third-party company or individual
/// that performs a portion of a project's work under a sub-contract.</para>
///
/// <para><b>DEC-221 — subcontractors</b> (new table):</para>
/// <list type="bullet">
///   <item>Master data: code, name (EN + AR), contact, trade specialty, tax id</item>
///   <item><c>is_active</c> flag: deactivation (soft) for terminated subcontractors</item>
///   <item>UNIQUE (company_id, code) — each company has its own code namespace</item>
/// </list>
///
/// <para><b>DEC-222 — sub_contracts</b> (new table):</para>
/// <list type="bullet">
///   <item>Linked to project + subcontractor (FK + same-company scope)</item>
///   <item>Captures: scope of work, contract value, retention % + release rule</item>
///   <item><c>status</c>: 1=Active, 2=Completed, 3=Cancelled (mirrors Contract pattern)</item>
///   <item>UNIQUE (project_id, contract_number) — no duplicate sub-contract numbers per project</item>
///   <item>Non-unique index on subcontractor_id for "all contracts for a sub" queries</item>
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
[Migration(20260827_180000)]
public class Sprint64_Subcontractor : Migration
{
    public override void Up()
    {
        // ============== DEC-221 — subcontractors (new table) ==============
        Execute.Sql(@"
            CREATE TABLE IF NOT EXISTS subcontractors (
                id UUID PRIMARY KEY,
                company_id UUID NOT NULL REFERENCES companies(id),
                code VARCHAR(20) NOT NULL,
                name TEXT NOT NULL,
                name_ar TEXT,
                contact_person TEXT,
                phone TEXT,
                email TEXT,
                trade_specialty TEXT,
                tax_id TEXT,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
        ");

        // UNIQUE (company_id, code) — each company has its own code namespace.
        Execute.Sql(@"
            CREATE UNIQUE INDEX IF NOT EXISTS ux_subcontractors_company_code
                ON subcontractors(company_id, code);
        ");

        // ============== DEC-222 — sub_contracts (new table) ==============
        Execute.Sql(@"
            CREATE TABLE IF NOT EXISTS sub_contracts (
                id UUID PRIMARY KEY,
                company_id UUID NOT NULL REFERENCES companies(id),
                project_id UUID NOT NULL REFERENCES projects(id),
                subcontractor_id UUID NOT NULL REFERENCES subcontractors(id),
                contract_number VARCHAR(50) NOT NULL,
                scope_of_work TEXT NOT NULL,
                contract_value NUMERIC(18,4) NOT NULL DEFAULT 0,
                retention_percent NUMERIC(5,2) NOT NULL DEFAULT 10.0,
                retention_release_billing INT NOT NULL DEFAULT 3,
                start_date DATE,
                end_date DATE,
                status INT NOT NULL DEFAULT 1,
                notes TEXT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
        ");

        // UNIQUE (project_id, contract_number) — no duplicate contract numbers per project.
        Execute.Sql(@"
            CREATE UNIQUE INDEX IF NOT EXISTS ux_sub_contracts_project_number
                ON sub_contracts(project_id, contract_number);
        ");

        // Non-unique index for "all contracts for a subcontractor" queries.
        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS ix_sub_contracts_subcontractor
                ON sub_contracts(subcontractor_id);
        ");
    }

    public override void Down()
    {
        // Reverse order: drop the dependent table first, then the master table.
        // IF EXISTS guards so re-running Down is a no-op.
        Execute.Sql("DROP TABLE IF EXISTS sub_contracts CASCADE;");
        Execute.Sql("DROP TABLE IF EXISTS subcontractors CASCADE;");
    }
}
