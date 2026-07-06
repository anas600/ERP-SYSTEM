using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Migration 015 — audit_log table (Sprint-4.5: Stability & Shipping).
///
/// Tracks CREATE/UPDATE/DELETE on Finance + Projects entities.
/// DEC-056: First deliverable of Sprint-4.5 audit subsystem.
/// </summary>
[Migration(20260706_120000)]
public class AddAuditLog : Migration
{
    public override void Up()
    {
        if (!Schema.Table("audit_log").Exists())
        {
            Create.Table("audit_log")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("tenant_id").AsGuid().NotNullable()
                .WithColumn("entity_type").AsString(50).NotNullable()
                .WithColumn("entity_id").AsGuid().NotNullable()
                .WithColumn("action").AsString(20).NotNullable() // CREATE / UPDATE / DELETE / RESTORE
                .WithColumn("user_id").AsGuid().Nullable()
                .WithColumn("changes").AsCustom("jsonb").Nullable()
                .WithColumn("ip_address").AsString(45).Nullable()
                .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

            Create.Index("ix_audit_log_entity")
                .OnTable("audit_log")
                .OnColumn("tenant_id").Ascending()
                .OnColumn("entity_type").Ascending()
                .OnColumn("entity_id").Ascending()
                .OnColumn("created_at").Descending();

            Create.Index("ix_audit_log_user")
                .OnTable("audit_log")
                .OnColumn("tenant_id").Ascending()
                .OnColumn("user_id").Ascending()
                .OnColumn("created_at").Descending();
        }
    }

    public override void Down()
    {
        if (Schema.Table("audit_log").Exists())
        {
            Delete.Table("audit_log");
        }
    }
}