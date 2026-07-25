using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Phase 6.0 — Initial Schema (Clean Slate).
///
/// Refs: CONSTITUTION.md §3 (Multi-Company, no Multi-Tenancy), docs/PHASE6-PLAN.md
///
/// Purpose: Drop every table created by the v5.0.4 multi-tenant schema and let
/// the JSON DataTypeMigrator rebuild them on next startup WITHOUT the `tenant_id`
/// column. The new schema uses `company_id` for inner-company isolation and the
/// `user_companies` join table for user-to-company access.
///
/// Up():
///   1. DELETE FROM "VersionInfo" — clears all migration history so the existing
///      NoOp C# migrations (DEC-080/082) re-run on next startup. They are
///      idempotent and will skip themselves because the new tables (without
///      `tenant_id`) are the source of truth. If a JSON migrator creates a
///      table with the new shape first, the C# migration's CREATE TABLE IF NOT
///      EXISTS will be a no-op.
///   2. DROP TABLE IF EXISTS ... CASCADE for every business table. The list
///      includes tables from every spec module (Identity, Companies, Finance,
///      Projects, Inventory, Procurement, AR, Payments, HR, Payroll, Shared)
///      plus tables that may or may not exist yet (project_tasks, resources,
///      resource_assignments, project_budgets, purchase_orders, etc.). The
///      `IF EXISTS` makes the list safe to re-run.
///   3. DROP SEQUENCE IF EXISTS for any auto-increment sequences (audit_log).
///
/// Down(): not supported. To revert, restore the v5.0.4 git commit and run its
/// migrations. The C# code refactor (Phase 6.1) will remove the old C#
/// migration files in a follow-up PR; this migration only clears state.
///
/// Idempotency: all DDL uses IF EXISTS / IF NOT EXISTS guards. Safe to re-run
/// on a fresh database, partially-built database, or post-deploy database.
/// </summary>
[Migration(20260725_120000)]
public class Phase6_InitialSchema : Migration
{
    public override void Up()
    {
        // 1) Clear FluentMigrator version history so the old NoOp migrations
        //    re-run (as no-ops) on next startup. The tables they would create
        //    already exist (with the new shape), so their CREATE TABLE IF NOT
        //    EXISTS is harmless.
        Execute.Sql("DELETE FROM \"VersionInfo\";");

        // 2) Drop all business tables. The list covers every spec module.
        //    `user_companies` is included so a re-run after Phase 6.1 starts
        //    modifying the table does not leave a stale copy. `companies` is
        //    included so the new shape (no `tenant_id`, no `tenant_id` index)
        //    is rebuilt cleanly.
        //
        //    Single DROP TABLE statement, comma-separated, with CASCADE so
        //    dependent FKs and indexes disappear automatically. CASCADE here
        //    is destructive by design — this is a clean-slate migration.
        Execute.Sql(@"
            DROP TABLE IF EXISTS
                user_companies,
                users,
                roles,
                user_roles,
                refresh_tokens,
                password_reset_tokens,
                tenants,
                companies,
                cost_centers,
                accounts,
                journal_entries,
                journal_lines,
                posting_rules,
                projects,
                project_tasks,
                resources,
                resource_assignments,
                project_budgets,
                items,
                item_categories,
                warehouses,
                units_of_measure,
                stock_levels,
                stock_movements,
                stock_reservations,
                vendors,
                purchase_orders,
                purchase_order_lines,
                goods_receipts,
                goods_receipt_lines,
                vendor_bills,
                vendor_bill_lines,
                customers,
                sales_invoices,
                sales_invoice_lines,
                receipts,
                receipt_allocations,
                payments,
                payment_allocations,
                departments,
                employees,
                attendance,
                leave_requests,
                salary_structures,
                salary_structure_lines,
                payroll_runs,
                payroll_items,
                payslip_components,
                notifications,
                outbox_events,
                processed_events,
                audit_log,
                archive_metadata
            CASCADE;
        ");

        // 3) Drop auto-increment sequences that may not be tied to a column
        //    (audit_log uses bigint with nextval). The CASCADE on the table
        //    above should drop the sequence too, but be explicit for safety.
        Execute.Sql("DROP SEQUENCE IF EXISTS audit_log_id_seq CASCADE;");
    }

    public override void Down()
    {
        // Phase 6.0 is a destructive clean-slate. To revert, restore the
        // v5.0.4 git commit and run its migrations. There is no forward
        // path back to a multi-tenant schema.
        throw new NotSupportedException(
            "Clean Slate migration is not reversible. " +
            "To revert to v5.0.4, restore the v5.0.4 git commit and re-run its migrations.");
    }
}
