using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Migration 004 — Projects Module
///
/// الجداول:
/// - projects
/// - project_tasks
/// - resources
/// - project_budgets
/// - resource_assignments
/// </summary>
[Migration(20260615_050000)]
public class CreateProjectsTables : Migration
{
    public override void Up()
    {
        // ============== projects ==============
        Create.Table("projects")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("company_id").AsGuid().NotNullable()
            .WithColumn("cost_center_id").AsGuid().NotNullable()
            .WithColumn("code").AsString(50).NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(int.MaxValue).Nullable()
            .WithColumn("customer_id").AsGuid().Nullable()
            .WithColumn("status").AsInt32().NotNullable().WithDefaultValue((int)ProjectStatusLocal.Planning)
            .WithColumn("budget").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("start_date").AsDateTime().NotNullable()
            .WithColumn("end_date").AsDateTime().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable()
            .WithColumn("updated_by").AsGuid().Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true);
        Create.Index("ix_projects_tenant_code").OnTable("projects")
            .OnColumn("tenant_id").Ascending().OnColumn("code").Ascending().WithOptions().Unique();
        Create.Index("ix_projects_tenant_company_status").OnTable("projects")
            .OnColumn("tenant_id").Ascending().OnColumn("company_id").Ascending().OnColumn("status").Ascending();
        Create.Index("ix_projects_tenant_active").OnTable("projects")
            .OnColumn("tenant_id").Ascending().OnColumn("is_active").Ascending();
        Create.ForeignKey("fk_projects_company").FromTable("projects").ForeignColumn("company_id")
            .ToTable("companies").PrimaryColumn("id").OnDelete(System.Data.Rule.None);
        Create.ForeignKey("fk_projects_cost_center").FromTable("projects").ForeignColumn("cost_center_id")
            .ToTable("cost_centers").PrimaryColumn("id").OnDelete(System.Data.Rule.None);

        // ============== project_tasks ==============
        Create.Table("project_tasks")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("project_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("description").AsString(int.MaxValue).Nullable()
            .WithColumn("status").AsInt32().NotNullable().WithDefaultValue((int)TaskStatusLocal.NotStarted)
            .WithColumn("estimated_hours").AsDecimal(10, 2).NotNullable().WithDefaultValue(0)
            .WithColumn("actual_hours").AsDecimal(10, 2).NotNullable().WithDefaultValue(0)
            .WithColumn("start_date").AsDateTime().Nullable()
            .WithColumn("end_date").AsDateTime().Nullable()
            .WithColumn("progress_percent").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();
        Create.Index("ix_project_tasks_project_status").OnTable("project_tasks")
            .OnColumn("tenant_id").Ascending().OnColumn("project_id").Ascending().OnColumn("status").Ascending();
        Create.ForeignKey("fk_project_tasks_project").FromTable("project_tasks").ForeignColumn("project_id")
            .ToTable("projects").PrimaryColumn("id").OnDelete(System.Data.Rule.Cascade);

        // ============== resources ==============
        Create.Table("resources")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("code").AsString(50).NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("type").AsInt32().NotNullable().WithDefaultValue((int)ResourceTypeLocal.Labor)
            .WithColumn("hourly_rate").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();
        Create.Index("ix_resources_tenant_code").OnTable("resources")
            .OnColumn("tenant_id").Ascending().OnColumn("code").Ascending().WithOptions().Unique();
        Create.Index("ix_resources_tenant_active").OnTable("resources")
            .OnColumn("tenant_id").Ascending().OnColumn("is_active").Ascending();

        // ============== project_budgets ==============
        Create.Table("project_budgets")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("project_id").AsGuid().NotNullable()
            .WithColumn("cost_center_id").AsGuid().NotNullable()
            .WithColumn("account_id").AsGuid().Nullable()
            .WithColumn("budget_amount").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("spent_amount").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("committed_amount").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("last_recalculated_at").AsDateTime().Nullable();
        Create.Index("ix_project_budgets_project").OnTable("project_budgets")
            .OnColumn("tenant_id").Ascending().OnColumn("project_id").Ascending().WithOptions().Unique();
        Create.Index("ix_project_budgets_cost_center").OnTable("project_budgets")
            .OnColumn("tenant_id").Ascending().OnColumn("cost_center_id").Ascending();
        Create.ForeignKey("fk_project_budgets_project").FromTable("project_budgets").ForeignColumn("project_id")
            .ToTable("projects").PrimaryColumn("id").OnDelete(System.Data.Rule.Cascade);
        Create.ForeignKey("fk_project_budgets_cost_center").FromTable("project_budgets").ForeignColumn("cost_center_id")
            .ToTable("cost_centers").PrimaryColumn("id").OnDelete(System.Data.Rule.None);
        Create.ForeignKey("fk_project_budgets_account").FromTable("project_budgets").ForeignColumn("account_id")
            .ToTable("accounts").PrimaryColumn("id").OnDelete(System.Data.Rule.SetNull);

        // ============== resource_assignments ==============
        Create.Table("resource_assignments")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("project_id").AsGuid().NotNullable()
            .WithColumn("task_id").AsGuid().NotNullable()
            .WithColumn("resource_id").AsGuid().NotNullable()
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("from").AsDateTime().NotNullable()
            .WithColumn("to").AsDateTime().NotNullable()
            .WithColumn("hourly_rate").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("created_at").AsDateTime().NotNullable();
        Create.Index("ix_resource_assignments_project").OnTable("resource_assignments")
            .OnColumn("tenant_id").Ascending().OnColumn("project_id").Ascending();
        Create.Index("ix_resource_assignments_task").OnTable("resource_assignments")
            .OnColumn("tenant_id").Ascending().OnColumn("task_id").Ascending();
        Create.ForeignKey("fk_resource_assignments_project").FromTable("resource_assignments").ForeignColumn("project_id")
            .ToTable("projects").PrimaryColumn("id").OnDelete(System.Data.Rule.Cascade);
        Create.ForeignKey("fk_resource_assignments_task").FromTable("resource_assignments").ForeignColumn("task_id")
            .ToTable("project_tasks").PrimaryColumn("id").OnDelete(System.Data.Rule.Cascade);
        Create.ForeignKey("fk_resource_assignments_resource").FromTable("resource_assignments").ForeignColumn("resource_id")
            .ToTable("resources").PrimaryColumn("id").OnDelete(System.Data.Rule.None);
    }

    public override void Down()
    {
        Delete.Table("resource_assignments");
        Delete.Table("project_budgets");
        Delete.Table("resources");
        Delete.Table("project_tasks");
        Delete.Table("projects");
    }
}

// local enums to avoid cross-module referencing in migration
internal enum ProjectStatusLocal { Planning = 1, Active = 2, OnHold = 3, Completed = 4, Cancelled = 5 }
internal enum TaskStatusLocal { NotStarted = 1, InProgress = 2, Blocked = 3, Completed = 4, Cancelled = 5 }
internal enum ResourceTypeLocal { Labor = 1, Equipment = 2, Material = 3, Service = 4 }
