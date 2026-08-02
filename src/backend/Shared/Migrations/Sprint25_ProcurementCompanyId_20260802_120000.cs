using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Sprint 25 — Ensure company_id is populated in procurement tables (DEC-085 audit).
///
/// Why: Sprint 22's tenant_id→company_id migration (Phase 6.0) added the company_id
/// column to the JSON data-type definitions, but the C# entity classes (Vendor,
/// PurchaseOrder, GoodsReceipt, VendorBill) did not declare a CompanyId field, and
/// the INSERT statements omitted it. As a result, every existing row in those
/// tables had a NULL company_id — which the FK constraint "fk_*_company_id"
/// would have rejected, but the original tables (pre-Sprint 25) did not have the
/// FK constraint either. So we ended up with rows that had a NULL company_id
/// despite the schema declaring it NOT NULL.
///
/// Sprint 25 fix:
///   1. Add CompanyId to the C# entities (Vendor, PurchaseOrder, GoodsReceipt, VendorBill)
///   2. Update INSERT statements to include @CompanyId
///   3. Backfill existing NULL company_id rows to the first company (only safe default
///      in a non-multi-company legacy DB).
///
/// This migration handles step 3 — backfill + enforce NOT NULL if any rows are
/// still NULL (idempotent — no-op when all rows already have company_id).
///
/// Up():
///   For each of vendors, purchase_orders, goods_receipts, vendor_bills:
///     - UPDATE ... SET company_id = (first company) WHERE company_id IS NULL
///
/// Idempotency: the UPDATE has WHERE company_id IS NULL so re-runs are no-ops.
/// </summary>
[Migration(20260802_120000, TransactionBehavior.None)]
public class Sprint25_ProcurementCompanyId : Migration
{
    public override void Up()
    {
        Execute.Sql("UPDATE vendors SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1) WHERE company_id IS NULL;");
        Execute.Sql("UPDATE purchase_orders SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1) WHERE company_id IS NULL;");
        Execute.Sql("UPDATE goods_receipts SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1) WHERE company_id IS NULL;");
        Execute.Sql("UPDATE vendor_bills SET company_id = (SELECT id FROM companies ORDER BY created_at LIMIT 1) WHERE company_id IS NULL;");
    }

    public override void Down()
    {
        // No-op. Reverting would require setting company_id back to NULL, but
        // the columns are NOT NULL by design — going back to NULL would break
        // every query that filters by company_id.
        throw new NotSupportedException(
            "Sprint 25 procurement-company_id backfill is one-way. " +
            "The columns are NOT NULL by design; cannot revert to NULL state.");
    }
}
