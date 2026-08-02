using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Sprint 27 — Ensure company_id is populated in HR tables (DEC-091 audit).
///
/// Why: Sprint 25 caught the same bug in the Procurement module (Vendor, PurchaseOrder,
/// GoodsReceipt, VendorBill). The HR module (Employee, Department, LeaveRequest, Attendance)
/// had the identical pattern:
///   1. Entities had no CompanyId field
///   2. Services did not inject ICompanyContext
///   3. Repositories' INSERT statements omitted company_id
///
/// The DB tables (employees, departments, leave_requests, attendance) had
/// `company_id UUID NOT NULL` from the Sprint 22 schema, but the column would
/// have been set to NULL by the existing INSERTs — which would fail the NOT NULL
/// constraint. The fact that there are 0 rows in the HR tables today means the
/// inserts never happened, but the bug was real.
///
/// Sprint 27 fix (mirrors Sprint 25):
///   1. Add CompanyId to the 4 C# entities
///   2. Inject ICompanyContext into the 4 services
///   3. Set CompanyId = companyId in each service's CreateAsync
///   4. Add @CompanyId to each repository's INSERT
///   5. Add @CompanyId to each repository's SELECT (so callers see the field)
///
/// This migration handles step 6 — backfill any NULL company_id rows to the first
/// company (safe default in a single-deployment multi-company DB). Idempotent
/// (no-op when all rows already have company_id).
///
/// Up():
///   For each of employees, departments, leave_requests, attendance:
///     - UPDATE ... SET company_id = (first company) WHERE company_id IS NULL
///
/// Idempotency: the UPDATE has WHERE company_id IS NULL so re-runs are no-ops.
/// </summary>
[Migration(20260802_130000, TransactionBehavior.None)]
public class Sprint27_HrCompanyId : Migration
{
    public override void Up()
    {
        Execute.Sql("UPDATE departments SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1) WHERE company_id IS NULL;");
        Execute.Sql("UPDATE employees SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1) WHERE company_id IS NULL;");
        Execute.Sql("UPDATE leave_requests SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1) WHERE company_id IS NULL;");
        Execute.Sql("UPDATE attendance SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1) WHERE company_id IS NULL;");
    }

    public override void Down()
    {
        // No-op. Same rationale as Sprint 25: the columns are NOT NULL by design;
        // reverting to NULL would break every query that filters by company_id.
        throw new NotSupportedException(
            "Sprint 27 HR-company_id backfill is one-way. " +
            "The columns are NOT NULL by design; cannot revert to NULL state.");
    }
}
