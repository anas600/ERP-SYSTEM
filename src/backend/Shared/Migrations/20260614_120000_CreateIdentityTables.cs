using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Migration 001 — إنشاء جداول Identity الأساسية
///
/// الجداول:
/// - tenants       : المستأجرون
/// - users         : المستخدمون
/// - roles         : الأدوار
/// - user_roles    : ربط المستخدمين بالأدوار
/// - refresh_tokens: توكنات التجديد (دورة حياة كاملة)
/// </summary>
[Migration(20260614_120000)]
public class CreateIdentityTables : Migration
{
    public override void Up()
    {
        // ============== tenants ==============
        Create.Table("tenants")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("subdomain").AsString(100).NotNullable().Unique()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("subscription_expires_at").AsDateTime().Nullable();

        // ============== roles ==============
        Create.Table("roles")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("description").AsString(500).NotNullable().WithDefaultValue(string.Empty)
            .WithColumn("created_at").AsDateTime().NotNullable();

        Create.Index("ix_roles_tenant_name")
            .OnTable("roles")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("name").Ascending()
            .WithOptions().Unique();

        Create.ForeignKey("fk_roles_tenant")
            .FromTable("roles").ForeignColumn("tenant_id")
            .ToTable("tenants").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        // ============== users ==============
        Create.Table("users")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("email").AsString(255).NotNullable()
            .WithColumn("password_hash").AsString(500).NotNullable()
            .WithColumn("full_name").AsString(200).NotNullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("two_factor_enabled").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable()
            .WithColumn("last_login_at").AsDateTime().Nullable();

        Create.Index("ix_users_tenant_email")
            .OnTable("users")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("email").Ascending()
            .WithOptions().Unique();

        Create.Index("ix_users_email")
            .OnTable("users")
            .OnColumn("email").Ascending();

        Create.ForeignKey("fk_users_tenant")
            .FromTable("users").ForeignColumn("tenant_id")
            .ToTable("tenants").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        // ============== user_roles ==============
        Create.Table("user_roles")
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("role_id").AsGuid().NotNullable()
            .WithColumn("assigned_at").AsDateTime().NotNullable();

        Create.PrimaryKey("pk_user_roles").OnTable("user_roles")
            .Columns("user_id", "role_id");

        Create.ForeignKey("fk_user_roles_user")
            .FromTable("user_roles").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("fk_user_roles_role")
            .FromTable("user_roles").ForeignColumn("role_id")
            .ToTable("roles").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        // ============== refresh_tokens ==============
        Create.Table("refresh_tokens")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("token_hash").AsString(500).NotNullable()
            .WithColumn("expires_at").AsDateTime().NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("revoked_at").AsDateTime().Nullable()
            .WithColumn("replaced_by_token_hash").AsString(500).Nullable()
            .WithColumn("revoked_reason").AsString(200).Nullable()
            .WithColumn("created_by_ip").AsString(45).Nullable()
            .WithColumn("revoked_by_ip").AsString(45).Nullable();

        Create.Index("ix_refresh_tokens_user")
            .OnTable("refresh_tokens")
            .OnColumn("user_id").Ascending();

        Create.Index("ix_refresh_tokens_hash")
            .OnTable("refresh_tokens")
            .OnColumn("token_hash").Ascending()
            .WithOptions().Unique();

        Create.ForeignKey("fk_refresh_tokens_user")
            .FromTable("refresh_tokens").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);
    }

    public override void Down()
    {
        Delete.Table("refresh_tokens");
        Delete.Table("user_roles");
        Delete.Table("users");
        Delete.Table("roles");
        Delete.Table("tenants");
    }
}
