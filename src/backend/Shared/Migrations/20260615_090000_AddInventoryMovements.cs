using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Migration 006 — Inventory Movements (CQRS) + Notifications
///
/// الجداول:
/// - stock_movements: Aggregate Root (Draft → Posted)
/// - stock_levels: Read Model (denormalized, optimistic version)
/// - stock_reservations: holds for projects/orders
/// - notifications: in-app alerts (LowStock, etc.)
/// </summary>
[Migration(20260615_090000)]
public class AddInventoryMovements : Migration
{
    public override void Up()
    {
        // ============== stock_movements ==============
        Create.Table("stock_movements")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("company_id").AsGuid().NotNullable()
            .WithColumn("reference").AsString(50).NotNullable()
            .WithColumn("type").AsInt32().NotNullable()
            .WithColumn("movement_date").AsDateTime().NotNullable()
            .WithColumn("item_id").AsGuid().NotNullable()
            .WithColumn("warehouse_id").AsGuid().NotNullable()
            .WithColumn("quantity").AsDecimal(18, 4).NotNullable()
            .WithColumn("unit_cost").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("project_id").AsGuid().Nullable()
            .WithColumn("cost_center_id").AsGuid().Nullable()
            .WithColumn("destination_warehouse_id").AsGuid().Nullable()
            .WithColumn("source_type").AsString(50).Nullable()
            .WithColumn("source_id").AsGuid().Nullable()
            .WithColumn("notes").AsString(int.MaxValue).Nullable()
            .WithColumn("status").AsInt32().NotNullable().WithDefaultValue(1) // Draft
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("posted_at").AsDateTime().Nullable()
            .WithColumn("reversed_by_movement_id").AsGuid().Nullable();
        Create.Index("ix_stock_movements_tenant_status_date").OnTable("stock_movements")
            .OnColumn("tenant_id").Ascending().OnColumn("status").Ascending().OnColumn("movement_date").Descending();
        Create.Index("ix_stock_movements_tenant_item_warehouse").OnTable("stock_movements")
            .OnColumn("tenant_id").Ascending().OnColumn("item_id").Ascending().OnColumn("warehouse_id").Ascending();
        Create.Index("ix_stock_movements_tenant_reference").OnTable("stock_movements")
            .OnColumn("tenant_id").Ascending().OnColumn("source_type").Ascending().OnColumn("source_id").Ascending();
        Create.Index("ix_stock_movements_tenant_company").OnTable("stock_movements")
            .OnColumn("tenant_id").Ascending().OnColumn("company_id").Ascending();
        Create.ForeignKey("fk_stock_movements_company").FromTable("stock_movements").ForeignColumn("company_id")
            .ToTable("companies").PrimaryColumn("id").OnDelete(System.Data.Rule.None);
        Create.ForeignKey("fk_stock_movements_item").FromTable("stock_movements").ForeignColumn("item_id")
            .ToTable("items").PrimaryColumn("id").OnDelete(System.Data.Rule.None);
        Create.ForeignKey("fk_stock_movements_warehouse").FromTable("stock_movements").ForeignColumn("warehouse_id")
            .ToTable("warehouses").PrimaryColumn("id").OnDelete(System.Data.Rule.None);
        Create.ForeignKey("fk_stock_movements_destination_warehouse").FromTable("stock_movements").ForeignColumn("destination_warehouse_id")
            .ToTable("warehouses").PrimaryColumn("id").OnDelete(System.Data.Rule.SetNull);

        // ============== stock_levels ==============
        Create.Table("stock_levels")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("company_id").AsGuid().NotNullable()
            .WithColumn("item_id").AsGuid().NotNullable()
            .WithColumn("warehouse_id").AsGuid().NotNullable()
            .WithColumn("quantity_on_hand").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("quantity_reserved").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("average_cost").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("last_movement_at").AsDateTime().NotNullable()
            .WithColumn("version").AsInt32().NotNullable().WithDefaultValue(0);
        Create.Index("ix_stock_levels_tenant_item_warehouse").OnTable("stock_levels")
            .OnColumn("tenant_id").Ascending().OnColumn("item_id").Ascending().OnColumn("warehouse_id").Ascending().WithOptions().Unique();
        Create.Index("ix_stock_levels_tenant_company").OnTable("stock_levels")
            .OnColumn("tenant_id").Ascending().OnColumn("company_id").Ascending();
        Create.ForeignKey("fk_stock_levels_company").FromTable("stock_levels").ForeignColumn("company_id")
            .ToTable("companies").PrimaryColumn("id").OnDelete(System.Data.Rule.None);
        Create.ForeignKey("fk_stock_levels_item").FromTable("stock_levels").ForeignColumn("item_id")
            .ToTable("items").PrimaryColumn("id").OnDelete(System.Data.Rule.None);
        Create.ForeignKey("fk_stock_levels_warehouse").FromTable("stock_levels").ForeignColumn("warehouse_id")
            .ToTable("warehouses").PrimaryColumn("id").OnDelete(System.Data.Rule.None);

        // ============== stock_reservations ==============
        Create.Table("stock_reservations")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("item_id").AsGuid().NotNullable()
            .WithColumn("warehouse_id").AsGuid().NotNullable()
            .WithColumn("quantity").AsDecimal(18, 4).NotNullable()
            .WithColumn("reference_type").AsString(50).NotNullable()
            .WithColumn("reference_id").AsGuid().NotNullable()
            .WithColumn("expires_at").AsDateTime().NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsGuid().NotNullable();
        Create.Index("ix_stock_reservations_tenant_reference").OnTable("stock_reservations")
            .OnColumn("tenant_id").Ascending().OnColumn("reference_type").Ascending().OnColumn("reference_id").Ascending();
        Create.Index("ix_stock_reservations_tenant_item_warehouse").OnTable("stock_reservations")
            .OnColumn("tenant_id").Ascending().OnColumn("item_id").Ascending().OnColumn("warehouse_id").Ascending();
        Create.Index("ix_stock_reservations_expires").OnTable("stock_reservations")
            .OnColumn("expires_at").Ascending();
        Create.ForeignKey("fk_stock_reservations_item").FromTable("stock_reservations").ForeignColumn("item_id")
            .ToTable("items").PrimaryColumn("id").OnDelete(System.Data.Rule.None);
        Create.ForeignKey("fk_stock_reservations_warehouse").FromTable("stock_reservations").ForeignColumn("warehouse_id")
            .ToTable("warehouses").PrimaryColumn("id").OnDelete(System.Data.Rule.None);

        // ============== notifications ==============
        Create.Table("notifications")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("type").AsString(50).NotNullable()
            .WithColumn("title").AsString(200).NotNullable()
            .WithColumn("message").AsString(int.MaxValue).NotNullable()
            .WithColumn("reference_type").AsString(50).Nullable()
            .WithColumn("reference_id").AsGuid().Nullable()
            .WithColumn("is_read").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("read_at").AsDateTime().Nullable();
        Create.Index("ix_notifications_tenant_user").OnTable("notifications")
            .OnColumn("tenant_id").Ascending().OnColumn("user_id").Ascending();
        Create.Index("ix_notifications_tenant_user_unread").OnTable("notifications")
            .OnColumn("tenant_id").Ascending().OnColumn("user_id").Ascending().OnColumn("is_read").Ascending();
        Create.Index("ix_notifications_tenant_type").OnTable("notifications")
            .OnColumn("tenant_id").Ascending().OnColumn("type").Ascending();
        Create.Index("ix_notifications_tenant_created").OnTable("notifications")
            .OnColumn("tenant_id").Ascending().OnColumn("created_at").Descending();
    }

    public override void Down()
    {
        Delete.Table("notifications");
        Delete.Table("stock_reservations");
        Delete.Table("stock_levels");
        Delete.Table("stock_movements");
    }
}
