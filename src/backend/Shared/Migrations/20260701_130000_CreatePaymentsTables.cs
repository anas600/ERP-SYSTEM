using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Migration 011 — إنشاء جداول Payments (Phase 5.A).
///
/// - payments: رأس سند الدفع/القبض
///   - party_type: "Customer" | "Vendor" (مفتوح — يدعم كلا الـ streams)
///   - party_id: Guid → customers (مستقبلي) أو vendors (Procurement)
///   - status: Draft | Posted | Cancelled
///   - journal_entry_id: ربط بالقيد المُنشأ عند الترحيل
///
/// - payment_allocations: تخصيصات الـ Payment على فواتير
///   - ref_type: "SalesInvoice" | "VendorBill"
///   - sum(amount_applied) ≤ payments.amount (الباقي = On Account)
/// </summary>
[Migration(20260701_130000)]
public class CreatePaymentsTables : Migration
{
    public override void Up()
    {
        // ============== payments ==============
        Create.Table("payments")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("company_id").AsGuid().Nullable()
            .WithColumn("party_type").AsString(20).NotNullable()
            .WithColumn("party_id").AsGuid().NotNullable()
            .WithColumn("payment_number").AsString(50).NotNullable()
            .WithColumn("payment_date").AsDateTime().NotNullable()
            .WithColumn("amount").AsDecimal(18, 4).NotNullable()
            .WithColumn("currency_code").AsString(3).NotNullable().WithDefaultValue("LYD")
            .WithColumn("payment_method").AsString(20).NotNullable().WithDefaultValue("Cash")
            .WithColumn("bank_account_id").AsGuid().Nullable()
            .WithColumn("notes").AsString(1000).Nullable()
            .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(1)  // Draft
            .WithColumn("posted_at").AsDateTime().Nullable()
            .WithColumn("posted_by").AsGuid().Nullable()
            .WithColumn("journal_entry_id").AsGuid().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable()
            .WithColumn("updated_by").AsGuid().Nullable();

        Create.Index("ix_payments_tenant_number")
            .OnTable("payments")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("payment_number").Ascending()
            .WithOptions().Unique();

        Create.Index("ix_payments_tenant_party")
            .OnTable("payments")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("party_type").Ascending()
            .OnColumn("party_id").Ascending();

        Create.Index("ix_payments_tenant_status")
            .OnTable("payments")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("status").Ascending();

        Create.Index("ix_payments_tenant_date")
            .OnTable("payments")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("payment_date").Descending();

        // ============== payment_allocations ==============
        Create.Table("payment_allocations")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("payment_id").AsGuid().NotNullable()
            .WithColumn("ref_type").AsString(20).NotNullable()
            .WithColumn("ref_id").AsGuid().NotNullable()
            .WithColumn("amount_applied").AsDecimal(18, 4).NotNullable();

        Create.Index("ix_pa_payment").OnTable("payment_allocations")
            .OnColumn("payment_id").Ascending();
        Create.Index("ix_pa_ref").OnTable("payment_allocations")
            .OnColumn("ref_type").Ascending()
            .OnColumn("ref_id").Ascending();
        Create.Index("ix_pa_tenant").OnTable("payment_allocations")
            .OnColumn("tenant_id").Ascending();

        // FK to payments: cascade delete (لو حُذف الـ header، تختفي الـ allocations)
        Create.ForeignKey("fk_pa_payment")
            .FromTable("payment_allocations").ForeignColumn("payment_id")
            .ToTable("payments").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        // FK to journal_entries (nullable — يُملأ فقط عند Post)
        Create.ForeignKey("fk_payments_je")
            .FromTable("payments").ForeignColumn("journal_entry_id")
            .ToTable("journal_entries").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.SetNull);
    }

    public override void Down()
    {
        Delete.ForeignKey("fk_payments_je").OnTable("payments");
        Delete.ForeignKey("fk_pa_payment").OnTable("payment_allocations");
        Delete.Table("payment_allocations");
        Delete.Table("payments");
    }
}
