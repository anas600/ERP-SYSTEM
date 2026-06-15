using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Migration 002 — إنشاء جداول Finance Core.
///
/// الجداول:
/// - accounts:        Chart of Accounts (with hierarchy via parent_account_id)
/// - journal_entries: رأس القيد (header)
/// - journal_lines:   سطور القيد (debit/credit منفصلين)
/// - posting_rules:   Rules Engine templates
/// </summary>
[Migration(20260614_180000)]
public class CreateFinanceTables : Migration
{
    public override void Up()
    {
        // ============== accounts ==============
        Create.Table("accounts")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("code").AsString(50).NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(500).Nullable()
            .WithColumn("type").AsInt32().NotNullable()
            .WithColumn("normal_balance").AsInt32().NotNullable()
            .WithColumn("parent_account_id").AsGuid().Nullable()
            .WithColumn("is_postable").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        Create.Index("ix_accounts_tenant_code")
            .OnTable("accounts")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("code").Ascending()
            .WithOptions().Unique();

        Create.Index("ix_accounts_tenant_parent")
            .OnTable("accounts")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("parent_account_id").Ascending();

        // Self-referencing FK: account.parent_account_id -> accounts.id
        // ملاحظة: في Postgres، self-FK يُنشأ بدون CASCADE لتجنّب الحلقات
        Create.ForeignKey("fk_accounts_parent")
            .FromTable("accounts").ForeignColumn("parent_account_id")
            .ToTable("accounts").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.SetNull);

        // ============== journal_entries ==============
        Create.Table("journal_entries")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("entry_number").AsString(50).NotNullable()
            .WithColumn("entry_date").AsDateTime().NotNullable()
            .WithColumn("description").AsString(500).NotNullable()
            .WithColumn("reference").AsString(200).Nullable()
            .WithColumn("status").AsInt32().NotNullable().WithDefaultValue((int)JournalEntryStatus.Draft)
            .WithColumn("created_by_user_id").AsGuid().NotNullable()
            .WithColumn("posted_at").AsDateTime().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        Create.Index("ix_journal_entries_tenant_number")
            .OnTable("journal_entries")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("entry_number").Ascending()
            .WithOptions().Unique();

        Create.Index("ix_journal_entries_tenant_date")
            .OnTable("journal_entries")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("entry_date").Descending();

        Create.Index("ix_journal_entries_status")
            .OnTable("journal_entries")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("status").Ascending();

        // ============== journal_lines ==============
        Create.Table("journal_lines")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("journal_entry_id").AsGuid().NotNullable()
            .WithColumn("account_id").AsGuid().NotNullable()
            .WithColumn("debit").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("credit").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("description").AsString(500).Nullable()
            .WithColumn("line_number").AsInt32().NotNullable();

        Create.Index("ix_journal_lines_entry")
            .OnTable("journal_lines")
            .OnColumn("journal_entry_id").Ascending();

        Create.Index("ix_journal_lines_account")
            .OnTable("journal_lines")
            .OnColumn("account_id").Ascending();

        Create.ForeignKey("fk_journal_lines_entry")
            .FromTable("journal_lines").ForeignColumn("journal_entry_id")
            .ToTable("journal_entries").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.Cascade);

        Create.ForeignKey("fk_journal_lines_account")
            .FromTable("journal_lines").ForeignColumn("account_id")
            .ToTable("accounts").PrimaryColumn("id")
            .OnDelete(System.Data.Rule.None); // لا نحذف حساب عليه حركات (تطبيق عبر FK constraint)

        // ============== posting_rules ==============
        Create.Table("posting_rules")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(500).Nullable()
            .WithColumn("event_type").AsInt32().NotNullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("template_json").AsString(int.MaxValue).NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();

        Create.Index("ix_posting_rules_tenant_event")
            .OnTable("posting_rules")
            .OnColumn("tenant_id").Ascending()
            .OnColumn("event_type").Ascending()
            .OnColumn("is_active").Ascending();
    }

    public override void Down()
    {
        Delete.Table("posting_rules");
        Delete.Table("journal_lines");
        Delete.Table("journal_entries");
        Delete.Table("accounts");
    }
}

/// <summary>enum محلي لتفادي cross-module referencing</summary>
internal enum JournalEntryStatus
{
    Draft = 1,
    Posted = 2,
    Reversed = 3,
}
