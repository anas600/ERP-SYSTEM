using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

[Migration(20260615_020000)]
public class AddMultiCompanySupport : Migration
{
    public override void Up()
    {
        Create.Table("companies")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("code").AsString(20).NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("legal_name").AsString(200).Nullable()
            .WithColumn("parent_company_id").AsGuid().Nullable()
            .WithColumn("is_group").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("base_currency").AsString(3).NotNullable().WithDefaultValue("LYD")
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();
        Create.Index("ix_companies_tenant_code").OnTable("companies")
            .OnColumn("tenant_id").Ascending().OnColumn("code").Ascending().WithOptions().Unique();
        Create.Index("ix_companies_tenant_parent").OnTable("companies")
            .OnColumn("tenant_id").Ascending().OnColumn("parent_company_id").Ascending();
        Create.ForeignKey("fk_companies_parent").FromTable("companies").ForeignColumn("parent_company_id")
            .ToTable("companies").PrimaryColumn("id").OnDelete(System.Data.Rule.SetNull);

        Create.Table("cost_centers")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("company_id").AsGuid().Nullable()
            .WithColumn("code").AsString(20).NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("type").AsInt32().NotNullable()
            .WithColumn("parent_id").AsGuid().Nullable()
            .WithColumn("budget_amount").AsDecimal(18, 4).Nullable()
            .WithColumn("start_date").AsDateTime().Nullable()
            .WithColumn("end_date").AsDateTime().Nullable()
            .WithColumn("sku").AsString(50).Nullable()
            .WithColumn("location").AsString(int.MaxValue).Nullable()
            .WithColumn("activity_category").AsString(50).Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();
        Create.Index("ix_cc_tenant_code").OnTable("cost_centers")
            .OnColumn("tenant_id").Ascending().OnColumn("code").Ascending().WithOptions().Unique();
        Create.Index("ix_cc_tenant_type").OnTable("cost_centers")
            .OnColumn("tenant_id").Ascending().OnColumn("type").Ascending();
        Create.Index("ix_cc_company").OnTable("cost_centers").OnColumn("company_id").Ascending();
        Create.Index("ix_cc_tenant_parent").OnTable("cost_centers")
            .OnColumn("tenant_id").Ascending().OnColumn("parent_id").Ascending();
        Create.ForeignKey("fk_cc_company").FromTable("cost_centers").ForeignColumn("company_id")
            .ToTable("companies").PrimaryColumn("id").OnDelete(System.Data.Rule.SetNull);
        Create.ForeignKey("fk_cc_parent").FromTable("cost_centers").ForeignColumn("parent_id")
            .ToTable("cost_centers").PrimaryColumn("id").OnDelete(System.Data.Rule.SetNull);

        Alter.Table("accounts")
            .AddColumn("company_id").AsGuid().Nullable()
            .AddColumn("is_intercompany").AsBoolean().NotNullable().WithDefaultValue(false);
        Create.Index("ix_accounts_company").OnTable("accounts").OnColumn("company_id").Ascending();
        Create.ForeignKey("fk_accounts_company").FromTable("accounts").ForeignColumn("company_id")
            .ToTable("companies").PrimaryColumn("id").OnDelete(System.Data.Rule.SetNull);

        Alter.Table("journal_entries").AddColumn("company_id").AsGuid().Nullable();
        Create.Index("ix_je_company").OnTable("journal_entries").OnColumn("company_id").Ascending();
        Create.ForeignKey("fk_je_company").FromTable("journal_entries").ForeignColumn("company_id")
            .ToTable("companies").PrimaryColumn("id").OnDelete(System.Data.Rule.SetNull);

        Alter.Table("journal_lines")
            .AddColumn("company_id").AsGuid().Nullable()
            .AddColumn("cost_center_id").AsGuid().Nullable();
        Create.Index("ix_jl_company").OnTable("journal_lines").OnColumn("company_id").Ascending();
        Create.Index("ix_jl_cost_center").OnTable("journal_lines").OnColumn("cost_center_id").Ascending();
        Create.ForeignKey("fk_jl_company").FromTable("journal_lines").ForeignColumn("company_id")
            .ToTable("companies").PrimaryColumn("id").OnDelete(System.Data.Rule.SetNull);
        Create.ForeignKey("fk_jl_cost_center").FromTable("journal_lines").ForeignColumn("cost_center_id")
            .ToTable("cost_centers").PrimaryColumn("id").OnDelete(System.Data.Rule.SetNull);
    }
    public override void Down()
    {
        Delete.ForeignKey("fk_jl_cost_center").OnTable("journal_lines");
        Delete.ForeignKey("fk_jl_company").OnTable("journal_lines");
        Delete.Column("cost_center_id").FromTable("journal_lines");
        Delete.Column("company_id").FromTable("journal_lines");
        Delete.ForeignKey("fk_je_company").OnTable("journal_entries");
        Delete.Column("company_id").FromTable("journal_entries");
        Delete.ForeignKey("fk_accounts_company").OnTable("accounts");
        Delete.Column("is_intercompany").FromTable("accounts");
        Delete.Column("company_id").FromTable("accounts");
        Delete.Table("cost_centers");
        Delete.Table("companies");
    }
}
