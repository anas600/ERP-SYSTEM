using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Migration 008 — Procurement Core (Phase 3)
///
/// الجداول:
/// - vendors                                  (المورّدون)
/// - purchase_orders + purchase_order_lines   (أوامر الشراء)
/// - goods_receipts + goods_receipt_lines     (سندات الاستلام)
/// - vendor_bills + vendor_bill_lines         (فواتير المورّدين)
///
/// Business Rules:
/// - GR يُنشأ فقط لـ PO في حالة Approved أو Sent
/// - Bill يُنشأ فقط لـ GR في حالة Received
/// - عند Post Bill → JournalEntry (Dr Inventory / Cr A/P) — يدوياً في الـ service
/// </summary>
[Migration(20260623_120000)]
public class CreateProcurementTables : Migration
{
    public override void Up()
    {
        // ============== vendors ==============
        Create.Table("vendors")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("code").AsString(50).NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("email").AsString(200).Nullable()
            .WithColumn("phone").AsString(50).Nullable()
            .WithColumn("address").AsString(int.MaxValue).Nullable()
            .WithColumn("tax_number").AsString(50).Nullable()
            .WithColumn("currency").AsString(3).NotNullable().WithDefaultValue("LYD")
            .WithColumn("payment_terms").AsString(20).NotNullable().WithDefaultValue("Net30")
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable()
            .WithColumn("updated_by").AsGuid().Nullable();
        Create.Index("ix_vendors_tenant_code").OnTable("vendors")
            .OnColumn("tenant_id").Ascending().OnColumn("code").Ascending().WithOptions().Unique();
        Create.Index("ix_vendors_tenant_active").OnTable("vendors")
            .OnColumn("tenant_id").Ascending().OnColumn("is_active").Ascending();
        Create.Index("ix_vendors_tax_number").OnTable("vendors").OnColumn("tax_number").Ascending();

        // ============== purchase_orders ==============
        Create.Table("purchase_orders")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("po_number").AsString(50).NotNullable()
            .WithColumn("vendor_id").AsGuid().NotNullable()
            .WithColumn("status").AsString(20).NotNullable().WithDefaultValue("Draft")
            .WithColumn("order_date").AsDateTime().NotNullable()
            .WithColumn("expected_date").AsDateTime().Nullable()
            .WithColumn("currency").AsString(3).NotNullable().WithDefaultValue("LYD")
            .WithColumn("sub_total").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("tax_amount").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("total_amount").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("notes").AsString(int.MaxValue).Nullable()
            .WithColumn("approved_at").AsDateTime().Nullable()
            .WithColumn("approved_by").AsGuid().Nullable()
            .WithColumn("sent_at").AsDateTime().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable()
            .WithColumn("updated_by").AsGuid().Nullable();
        Create.Index("ix_pos_tenant_po_number").OnTable("purchase_orders")
            .OnColumn("tenant_id").Ascending().OnColumn("po_number").Ascending().WithOptions().Unique();
        Create.Index("ix_pos_tenant_vendor").OnTable("purchase_orders")
            .OnColumn("tenant_id").Ascending().OnColumn("vendor_id").Ascending();
        Create.Index("ix_pos_tenant_status").OnTable("purchase_orders")
            .OnColumn("tenant_id").Ascending().OnColumn("status").Ascending();
        Create.Index("ix_pos_tenant_created").OnTable("purchase_orders")
            .OnColumn("tenant_id").Ascending().OnColumn("created_at").Descending();
        Create.ForeignKey("fk_pos_vendor").FromTable("purchase_orders").ForeignColumn("vendor_id")
            .ToTable("vendors").PrimaryColumn("id").OnDelete(System.Data.Rule.None);

        // ============== purchase_order_lines ==============
        Create.Table("purchase_order_lines")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("purchase_order_id").AsGuid().NotNullable()
            .WithColumn("item_id").AsGuid().NotNullable()
            .WithColumn("quantity").AsDecimal(18, 4).NotNullable()
            .WithColumn("unit_price").AsDecimal(18, 4).NotNullable()
            .WithColumn("tax_rate").AsDecimal(8, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("sub_total").AsDecimal(18, 4).NotNullable()
            .WithColumn("line_order").AsInt32().NotNullable().WithDefaultValue(0);
        Create.Index("ix_pol_tenant_po").OnTable("purchase_order_lines")
            .OnColumn("tenant_id").Ascending().OnColumn("purchase_order_id").Ascending();
        Create.Index("ix_pol_po_order").OnTable("purchase_order_lines")
            .OnColumn("purchase_order_id").Ascending().OnColumn("line_order").Ascending();
        Create.ForeignKey("fk_pol_po").FromTable("purchase_order_lines").ForeignColumn("purchase_order_id")
            .ToTable("purchase_orders").PrimaryColumn("id").OnDelete(System.Data.Rule.Cascade);
        Create.ForeignKey("fk_pol_item").FromTable("purchase_order_lines").ForeignColumn("item_id")
            .ToTable("items").PrimaryColumn("id").OnDelete(System.Data.Rule.None);

        // ============== goods_receipts ==============
        Create.Table("goods_receipts")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("gr_number").AsString(50).NotNullable()
            .WithColumn("purchase_order_id").AsGuid().NotNullable()
            .WithColumn("status").AsString(20).NotNullable().WithDefaultValue("Draft")
            .WithColumn("received_date").AsDateTime().NotNullable()
            .WithColumn("warehouse_id").AsGuid().NotNullable()
            .WithColumn("notes").AsString(int.MaxValue).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable()
            .WithColumn("updated_by").AsGuid().Nullable();
        Create.Index("ix_grs_tenant_gr_number").OnTable("goods_receipts")
            .OnColumn("tenant_id").Ascending().OnColumn("gr_number").Ascending().WithOptions().Unique();
        Create.Index("ix_grs_tenant_po").OnTable("goods_receipts")
            .OnColumn("tenant_id").Ascending().OnColumn("purchase_order_id").Ascending();
        Create.Index("ix_grs_tenant_status").OnTable("goods_receipts")
            .OnColumn("tenant_id").Ascending().OnColumn("status").Ascending();
        Create.Index("ix_grs_tenant_warehouse").OnTable("goods_receipts")
            .OnColumn("tenant_id").Ascending().OnColumn("warehouse_id").Ascending();
        Create.ForeignKey("fk_grs_po").FromTable("goods_receipts").ForeignColumn("purchase_order_id")
            .ToTable("purchase_orders").PrimaryColumn("id").OnDelete(System.Data.Rule.None);
        Create.ForeignKey("fk_grs_warehouse").FromTable("goods_receipts").ForeignColumn("warehouse_id")
            .ToTable("warehouses").PrimaryColumn("id").OnDelete(System.Data.Rule.None);

        // ============== goods_receipt_lines ==============
        Create.Table("goods_receipt_lines")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("goods_receipt_id").AsGuid().NotNullable()
            .WithColumn("item_id").AsGuid().NotNullable()
            .WithColumn("quantity").AsDecimal(18, 4).NotNullable()
            .WithColumn("unit_cost").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("notes").AsString(500).Nullable()
            .WithColumn("line_order").AsInt32().NotNullable().WithDefaultValue(0);
        Create.Index("ix_grl_tenant_gr").OnTable("goods_receipt_lines")
            .OnColumn("tenant_id").Ascending().OnColumn("goods_receipt_id").Ascending();
        Create.Index("ix_grl_gr_order").OnTable("goods_receipt_lines")
            .OnColumn("goods_receipt_id").Ascending().OnColumn("line_order").Ascending();
        Create.ForeignKey("fk_grl_gr").FromTable("goods_receipt_lines").ForeignColumn("goods_receipt_id")
            .ToTable("goods_receipts").PrimaryColumn("id").OnDelete(System.Data.Rule.Cascade);
        Create.ForeignKey("fk_grl_item").FromTable("goods_receipt_lines").ForeignColumn("item_id")
            .ToTable("items").PrimaryColumn("id").OnDelete(System.Data.Rule.None);

        // ============== vendor_bills ==============
        Create.Table("vendor_bills")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("bill_number").AsString(50).NotNullable()
            .WithColumn("goods_receipt_id").AsGuid().NotNullable()
            .WithColumn("vendor_id").AsGuid().NotNullable()
            .WithColumn("status").AsString(20).NotNullable().WithDefaultValue("Draft")
            .WithColumn("bill_date").AsDateTime().NotNullable()
            .WithColumn("due_date").AsDateTime().Nullable()
            .WithColumn("currency").AsString(3).NotNullable().WithDefaultValue("LYD")
            .WithColumn("sub_total").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("tax_amount").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("total_amount").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("notes").AsString(int.MaxValue).Nullable()
            .WithColumn("journal_entry_id").AsGuid().Nullable()
            .WithColumn("posted_at").AsDateTime().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable()
            .WithColumn("updated_by").AsGuid().Nullable();
        Create.Index("ix_vbs_tenant_bill_number").OnTable("vendor_bills")
            .OnColumn("tenant_id").Ascending().OnColumn("bill_number").Ascending().WithOptions().Unique();
        Create.Index("ix_vbs_tenant_gr").OnTable("vendor_bills")
            .OnColumn("tenant_id").Ascending().OnColumn("goods_receipt_id").Ascending();
        Create.Index("ix_vbs_tenant_vendor").OnTable("vendor_bills")
            .OnColumn("tenant_id").Ascending().OnColumn("vendor_id").Ascending();
        Create.Index("ix_vbs_tenant_status").OnTable("vendor_bills")
            .OnColumn("tenant_id").Ascending().OnColumn("status").Ascending();
        Create.ForeignKey("fk_vbs_gr").FromTable("vendor_bills").ForeignColumn("goods_receipt_id")
            .ToTable("goods_receipts").PrimaryColumn("id").OnDelete(System.Data.Rule.None);
        Create.ForeignKey("fk_vbs_vendor").FromTable("vendor_bills").ForeignColumn("vendor_id")
            .ToTable("vendors").PrimaryColumn("id").OnDelete(System.Data.Rule.None);

        // ============== vendor_bill_lines ==============
        Create.Table("vendor_bill_lines")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("vendor_bill_id").AsGuid().NotNullable()
            .WithColumn("item_id").AsGuid().NotNullable()
            .WithColumn("quantity").AsDecimal(18, 4).NotNullable()
            .WithColumn("unit_price").AsDecimal(18, 4).NotNullable()
            .WithColumn("tax_rate").AsDecimal(8, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("sub_total").AsDecimal(18, 4).NotNullable()
            .WithColumn("line_order").AsInt32().NotNullable().WithDefaultValue(0);
        Create.Index("ix_vbl_tenant_vb").OnTable("vendor_bill_lines")
            .OnColumn("tenant_id").Ascending().OnColumn("vendor_bill_id").Ascending();
        Create.Index("ix_vbl_vb_order").OnTable("vendor_bill_lines")
            .OnColumn("vendor_bill_id").Ascending().OnColumn("line_order").Ascending();
        Create.ForeignKey("fk_vbl_vb").FromTable("vendor_bill_lines").ForeignColumn("vendor_bill_id")
            .ToTable("vendor_bills").PrimaryColumn("id").OnDelete(System.Data.Rule.Cascade);
        Create.ForeignKey("fk_vbl_item").FromTable("vendor_bill_lines").ForeignColumn("item_id")
            .ToTable("items").PrimaryColumn("id").OnDelete(System.Data.Rule.None);
    }

    public override void Down()
    {
        Delete.Table("vendor_bill_lines");
        Delete.Table("vendor_bills");
        Delete.Table("goods_receipt_lines");
        Delete.Table("goods_receipts");
        Delete.Table("purchase_order_lines");
        Delete.Table("purchase_orders");
        Delete.Table("vendors");
    }
}
