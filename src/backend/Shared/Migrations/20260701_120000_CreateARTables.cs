using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Migration 010 — Accounts Receivable (Phase 5 Sprint 1)
///
/// الجداول:
/// - customers                              (العملاء — ماستر)
/// - sales_invoices + sales_invoice_lines   (فواتير المبيعات)
/// - receipts + receipt_allocations         (سندات القبض + تخصيصاتها)
///
/// Business Rules:
/// - SalesInvoice.Post (Draft → Sent) → JournalEntry (Dr 1230 AR / Cr 5110 Revenue)
/// - Receipt.Post → JournalEntry (Dr 1210 Cash / Cr 1230 AR) + تحديث الفواتير
/// - OnDelete: SetNull للـ customer_id في sales_invoices (لا نمسح الفواتير مع العميل)؛
///   Restrict للـ customer_id في receipts (لا نمسح العميل إذا عليه سندات)؛
///   Cascade للـ lines و allocations.
/// </summary>
[Migration(20260701_120000)]
public class CreateARTables : Migration
{
    public override void Up()
    {
        // ============== customers ==============
        Create.Table("customers")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("company_id").AsGuid().NotNullable()
            .WithColumn("code").AsString(50).NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("name_en").AsString(200).Nullable()
            .WithColumn("tax_id").AsString(50).Nullable()
            .WithColumn("email").AsString(200).Nullable()
            .WithColumn("phone").AsString(50).Nullable()
            .WithColumn("address").AsString(int.MaxValue).Nullable()
            .WithColumn("credit_limit").AsDecimal(18, 4).Nullable()
            .WithColumn("payment_terms_days").AsInt32().NotNullable().WithDefaultValue(30)
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable()
            .WithColumn("updated_by").AsGuid().Nullable();
        Create.Index("ix_customers_tenant_code").OnTable("customers")
            .OnColumn("tenant_id").Ascending().OnColumn("code").Ascending().WithOptions().Unique();
        Create.Index("ix_customers_tenant_active").OnTable("customers")
            .OnColumn("tenant_id").Ascending().OnColumn("is_active").Ascending();
        Create.Index("ix_customers_tax_id").OnTable("customers")
            .OnColumn("tax_id").Ascending();

        // ============== sales_invoices ==============
        Create.Table("sales_invoices")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("company_id").AsGuid().NotNullable()
            .WithColumn("customer_id").AsGuid().NotNullable()
            .WithColumn("invoice_number").AsString(50).NotNullable()
            .WithColumn("invoice_date").AsDateTime().NotNullable()
            .WithColumn("due_date").AsDateTime().Nullable()
            .WithColumn("currency_code").AsString(3).NotNullable().WithDefaultValue("LYD")
            .WithColumn("exchange_rate").AsDecimal(18, 8).NotNullable().WithDefaultValue(1)
            .WithColumn("subtotal").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("tax_amount").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("total_amount").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("paid_amount").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("status").AsString(20).NotNullable().WithDefaultValue("Draft")
            .WithColumn("notes").AsString(int.MaxValue).Nullable()
            .WithColumn("project_id").AsGuid().Nullable()
            .WithColumn("posted_at").AsDateTime().Nullable()
            .WithColumn("posted_by").AsGuid().Nullable()
            .WithColumn("journal_entry_id").AsGuid().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable()
            .WithColumn("updated_by").AsGuid().Nullable();
        Create.Index("ix_sinv_tenant_number").OnTable("sales_invoices")
            .OnColumn("tenant_id").Ascending().OnColumn("invoice_number").Ascending().WithOptions().Unique();
        Create.Index("ix_sinv_tenant_customer").OnTable("sales_invoices")
            .OnColumn("tenant_id").Ascending().OnColumn("customer_id").Ascending();
        Create.Index("ix_sinv_tenant_status").OnTable("sales_invoices")
            .OnColumn("tenant_id").Ascending().OnColumn("status").Ascending();
        Create.Index("ix_sinv_tenant_due").OnTable("sales_invoices")
            .OnColumn("tenant_id").Ascending().OnColumn("due_date").Ascending();
        Create.Index("ix_sinv_tenant_created").OnTable("sales_invoices")
            .OnColumn("tenant_id").Ascending().OnColumn("created_at").Descending();
        Create.ForeignKey("fk_sinv_customer").FromTable("sales_invoices").ForeignColumn("customer_id")
            .ToTable("customers").PrimaryColumn("id").OnDelete(System.Data.Rule.SetNull);

        // ============== sales_invoice_lines ==============
        Create.Table("sales_invoice_lines")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("sales_invoice_id").AsGuid().NotNullable()
            .WithColumn("item_id").AsGuid().Nullable()
            .WithColumn("description").AsString(500).NotNullable()
            .WithColumn("line_number").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("quantity").AsDecimal(18, 4).NotNullable()
            .WithColumn("unit_price").AsDecimal(18, 4).NotNullable()
            .WithColumn("tax_rate").AsDecimal(8, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("line_total").AsDecimal(18, 4).NotNullable();
        Create.Index("ix_sinvl_tenant_inv").OnTable("sales_invoice_lines")
            .OnColumn("tenant_id").Ascending().OnColumn("sales_invoice_id").Ascending();
        Create.Index("ix_sinvl_inv_order").OnTable("sales_invoice_lines")
            .OnColumn("sales_invoice_id").Ascending().OnColumn("line_number").Ascending();
        Create.ForeignKey("fk_sinvl_inv").FromTable("sales_invoice_lines").ForeignColumn("sales_invoice_id")
            .ToTable("sales_invoices").PrimaryColumn("id").OnDelete(System.Data.Rule.Cascade);

        // ============== receipts ==============
        Create.Table("receipts")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("company_id").AsGuid().NotNullable()
            .WithColumn("customer_id").AsGuid().NotNullable()
            .WithColumn("receipt_number").AsString(50).NotNullable()
            .WithColumn("receipt_date").AsDateTime().NotNullable()
            .WithColumn("amount").AsDecimal(18, 4).NotNullable()
            .WithColumn("currency_code").AsString(3).NotNullable().WithDefaultValue("LYD")
            .WithColumn("payment_method").AsString(20).Nullable()
            .WithColumn("notes").AsString(int.MaxValue).Nullable()
            .WithColumn("posted_at").AsDateTime().Nullable()
            .WithColumn("posted_by").AsGuid().Nullable()
            .WithColumn("journal_entry_id").AsGuid().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable()
            .WithColumn("updated_by").AsGuid().Nullable();
        Create.Index("ix_rc_tenant_number").OnTable("receipts")
            .OnColumn("tenant_id").Ascending().OnColumn("receipt_number").Ascending().WithOptions().Unique();
        Create.Index("ix_rc_tenant_customer").OnTable("receipts")
            .OnColumn("tenant_id").Ascending().OnColumn("customer_id").Ascending();
        Create.Index("ix_rc_tenant_date").OnTable("receipts")
            .OnColumn("tenant_id").Ascending().OnColumn("receipt_date").Descending();
        Create.ForeignKey("fk_rc_customer").FromTable("receipts").ForeignColumn("customer_id")
            .ToTable("customers").PrimaryColumn("id").OnDelete(System.Data.Rule.None); // RESTRICT-like (لا نسمح بحذف عميل عليه سندات)

        // ============== receipt_allocations ==============
        Create.Table("receipt_allocations")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("receipt_id").AsGuid().NotNullable()
            .WithColumn("sales_invoice_id").AsGuid().NotNullable()
            .WithColumn("amount_applied").AsDecimal(18, 4).NotNullable();
        Create.Index("ix_rca_tenant_rc").OnTable("receipt_allocations")
            .OnColumn("tenant_id").Ascending().OnColumn("receipt_id").Ascending();
        Create.Index("ix_rca_tenant_inv").OnTable("receipt_allocations")
            .OnColumn("tenant_id").Ascending().OnColumn("sales_invoice_id").Ascending();
        Create.ForeignKey("fk_rca_rc").FromTable("receipt_allocations").ForeignColumn("receipt_id")
            .ToTable("receipts").PrimaryColumn("id").OnDelete(System.Data.Rule.Cascade);
        Create.ForeignKey("fk_rca_inv").FromTable("receipt_allocations").ForeignColumn("sales_invoice_id")
            .ToTable("sales_invoices").PrimaryColumn("id").OnDelete(System.Data.Rule.None); // RESTRICT-like
    }

    public override void Down()
    {
        Delete.Table("receipt_allocations");
        Delete.Table("receipts");
        Delete.Table("sales_invoice_lines");
        Delete.Table("sales_invoices");
        Delete.Table("customers");
    }
}
