using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Migration 016 — soft delete columns (Sprint-4.5 T-011 / DEC-059).
///
/// Adds `deleted_at` (nullable timestamp) to finance + project tables.
/// History is preserved by marking rows as deleted instead of hard-deleting.
/// </summary>
[Migration(20260706_130000)]
public class AddSoftDelete : Migration
{
    // الجداول الأساسية (T-011 scope)
    private static readonly string[] PrimaryTables = new[]
    {
        "sales_invoices",
        "projects"
    };

    // جداول ثانوية (future PR — نفس النمط)
    private static readonly string[] SecondaryTables = new[]
    {
        "customers",
        "vendors",
        "employees"
    };

    public override void Up()
    {
        foreach (var table in PrimaryTables)
        {
            if (Schema.Table(table).Exists() && !Schema.Table(table).Column("deleted_at").Exists())
            {
                Alter.Table(table).AddColumn("deleted_at").AsDateTime().Nullable();
            }
        }

        // Index on deleted_at for fast "active records only" queries
        foreach (var table in PrimaryTables)
        {
            if (Schema.Table(table).Exists() && Schema.Table(table).Column("deleted_at").Exists())
            {
                // Index name: ix_<table>_deleted_at
                Create.Index($"ix_{table}_deleted_at")
                    .OnTable(table)
                    .OnColumn("deleted_at").Ascending();
            }
        }
    }

    public override void Down()
    {
        foreach (var table in PrimaryTables.Reverse())
        {
            if (Schema.Table(table).Exists() && Schema.Table(table).Column("deleted_at").Exists())
            {
                Delete.Index($"ix_{table}_deleted_at");
                Delete.Column("deleted_at").FromTable(table);
            }
        }
    }
}