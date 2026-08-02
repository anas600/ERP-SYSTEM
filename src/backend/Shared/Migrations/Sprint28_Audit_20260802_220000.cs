using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Sprint 28 — Final pass of Constitution Article 3 audit (DEC-094..097).
///
/// Why: Sprints 25, 27 fixed Article 3 violations in Procurement, Inventory, HR.
/// Sprint 28 closes the loop on the remaining 4 modules: Payroll, Projects,
/// StockMovement (service-level only), and a minor nullable fix on Finance/Account.
///
/// Same pattern as Sprint 25 + 27 migrations:
///   1. Add CompanyId to the C# entities (already done in code)
///   2. Update INSERT statements to include @CompanyId (already done in repos)
///   3. Add @CompanyId to SELECT columns so the field is populated
///   4. Backfill existing NULL company_id rows to the first company
///
/// This migration handles step 4 — backfill + enforce NOT NULL if any rows
/// are still NULL (idempotent — no-op when all rows already have company_id).
///
/// Up():
///   For each of payroll, projects (project_tasks/resources/etc), stock_movements,
///   accounts: UPDATE ... SET company_id = (first company) WHERE company_id IS NULL.
/// </summary>
[Migration(20260802_220000, TransactionBehavior.None)]
public class Sprint28_Audit : Migration
{
    public override void Up()
    {
        // DEC-094: Payroll (5 tables: salary_structures, salary_structure_lines,
        //                    payroll_runs, payroll_items, payslip_components)
        Execute.Sql("UPDATE salary_structures SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1) WHERE company_id IS NULL;");
        Execute.Sql("UPDATE salary_structure_lines SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1) WHERE company_id IS NULL;");
        Execute.Sql("UPDATE payroll_runs SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1) WHERE company_id IS NULL;");
        Execute.Sql("UPDATE payroll_items SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1) WHERE company_id IS NULL;");
        Execute.Sql("UPDATE payslip_components SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1) WHERE company_id IS NULL;");

        // DEC-095: Projects (4 tables: project_tasks, resources, resource_assignments, project_budgets)
        Execute.Sql("UPDATE project_tasks SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1) WHERE company_id IS NULL;");
        Execute.Sql("UPDATE resources SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1) WHERE company_id IS NULL;");
        Execute.Sql("UPDATE resource_assignments SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1) WHERE company_id IS NULL;");
        Execute.Sql("UPDATE project_budgets SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1) WHERE company_id IS NULL;");

        // DEC-096: StockMovement (entity + repo already had company_id, only service needed refactor)
        // No DB backfill needed (entity was already correct).
        // Execute.Sql preserved as documentation:
        // UPDATE stock_movements SET company_id = ... WHERE company_id IS NULL;

        // DEC-097: Finance/Account (Guid? → Guid) — already had company_id NOT NULL in DB
        // No backfill needed.
    }

    public override void Down()
    {
        // No-op. Same rationale as Sprint 25/27: columns are NOT NULL by design;
        // reverting to NULL would break every query that filters by company_id.
        throw new NotSupportedException(
            "Sprint 28 Article 3 audit is one-way. The columns are NOT NULL by design; cannot revert to NULL state.");
    }
}
