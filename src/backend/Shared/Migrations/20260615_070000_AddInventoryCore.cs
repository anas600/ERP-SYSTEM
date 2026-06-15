using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Migration 005 — Inventory Core
///
/// الجداول:
/// - items
/// - warehouses
/// - units_of_measure
/// - item_categories
/// </summary>
[Migration(20260615_070000)]
public class AddInventoryCore : Migration
{
    public override void Up()
    {
        // ============== units_of_measure ==============
        Create.Table("units_of_measure")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("code").AsString(20).NotNullable()
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("symbol").AsString(20).Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable();
        Create.Index("ix_uom_tenant_code").OnTable("units_of_measure")
            .OnColumn("tenant_id").Ascending().OnColumn("code").Ascending().WithOptions().Unique();

        // ============== item_categories ==============
        Create.Table("item_categories")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("code").AsString(50).NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(500).Nullable()
            .WithColumn("parent_id").AsGuid().Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();
        Create.Index("ix_item_categories_tenant_code").OnTable("item_categories")
            .OnColumn("tenant_id").Ascending().OnColumn("code").Ascending().WithOptions().Unique();
        Create.Index("ix_item_categories_tenant_parent").OnTable("item_categories")
            .OnColumn("tenant_id").Ascending().OnColumn("parent_id").Ascending();
        Create.ForeignKey("fk_item_categories_parent").FromTable("item_categories").ForeignColumn("parent_id")
            .ToTable("item_categories").PrimaryColumn("id").OnDelete(System.Data.Rule.SetNull);

        // ============== items ==============
        Create.Table("items")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("company_id").AsGuid().NotNullable()
            .WithColumn("sku").AsString(50).NotNullable()
            .WithColumn("barcode").AsString(100).Nullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(int.MaxValue).Nullable()
            .WithColumn("category_id").AsGuid().Nullable()
            .WithColumn("unit_of_measure_id").AsGuid().Nullable()
            .WithColumn("item_type").AsInt32().NotNullable().WithDefaultValue((int)ItemTypeLocal.RawMaterial)
            .WithColumn("costing_method").AsInt32().NotNullable().WithDefaultValue((int)CostingMethodLocal.Average)
            .WithColumn("average_cost").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("standard_cost").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("inventory_account_id").AsGuid().Nullable()
            .WithColumn("cogs_account_id").AsGuid().Nullable()
            .WithColumn("sales_account_id").AsGuid().Nullable()
            .WithColumn("reorder_level").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("reorder_quantity").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable()
            .WithColumn("updated_by").AsGuid().Nullable();
        Create.Index("ix_items_tenant_sku").OnTable("items")
            .OnColumn("tenant_id").Ascending().OnColumn("sku").Ascending().WithOptions().Unique();
        Create.Index("ix_items_tenant_company_active").OnTable("items")
            .OnColumn("tenant_id").Ascending().OnColumn("company_id").Ascending().OnColumn("is_active").Ascending();
        Create.Index("ix_items_category").OnTable("items").OnColumn("category_id").Ascending();
        Create.Index("ix_items_barcode").OnTable("items").OnColumn("barcode").Ascending();
        Create.ForeignKey("fk_items_company").FromTable("items").ForeignColumn("company_id")
            .ToTable("companies").PrimaryColumn("id").OnDelete(System.Data.Rule.None);
        Create.ForeignKey("fk_items_category").FromTable("items").ForeignColumn("category_id")
            .ToTable("item_categories").PrimaryColumn("id").OnDelete(System.Data.Rule.SetNull);
        Create.ForeignKey("fk_items_uom").FromTable("items").ForeignColumn("unit_of_measure_id")
            .ToTable("units_of_measure").PrimaryColumn("id").OnDelete(System.Data.Rule.SetNull);
        Create.ForeignKey("fk_items_inventory_account").FromTable("items").ForeignColumn("inventory_account_id")
            .ToTable("accounts").PrimaryColumn("id").OnDelete(System.Data.Rule.SetNull);
        Create.ForeignKey("fk_items_cogs_account").FromTable("items").ForeignColumn("cogs_account_id")
            .ToTable("accounts").PrimaryColumn("id").OnDelete(System.Data.Rule.SetNull);
        Create.ForeignKey("fk_items_sales_account").FromTable("items").ForeignColumn("sales_account_id")
            .ToTable("accounts").PrimaryColumn("id").OnDelete(System.Data.Rule.SetNull);

        // ============== warehouses ==============
        Create.Table("warehouses")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("company_id").AsGuid().NotNullable()
            .WithColumn("code").AsString(50).NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("location").AsString(500).Nullable()
            .WithColumn("manager_user_id").AsGuid().Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable()
            .WithColumn("updated_by").AsGuid().Nullable();
        Create.Index("ix_warehouses_tenant_code").OnTable("warehouses")
            .OnColumn("tenant_id").Ascending().OnColumn("code").Ascending().WithOptions().Unique();
        Create.Index("ix_warehouses_tenant_company_active").OnTable("warehouses")
            .OnColumn("tenant_id").Ascending().OnColumn("company_id").Ascending().OnColumn("is_active").Ascending();
        Create.ForeignKey("fk_warehouses_company").FromTable("warehouses").ForeignColumn("company_id")
            .ToTable("companies").PrimaryColumn("id").OnDelete(System.Data.Rule.None);
    }

    public override void Down()
    {
        Delete.Table("warehouses");
        Delete.Table("items");
        Delete.Table("item_categories");
        Delete.Table("units_of_measure");
    }
}

internal enum ItemTypeLocal { RawMaterial = 1, FinishedGood = 2, Consumable = 3, Service = 4 }
internal enum CostingMethodLocal { FIFO = 1, LIFO = 2, Average = 3, Standard = 4 }
