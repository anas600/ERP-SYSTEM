using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Migration 010 — Payroll Core (Phase 4)
///
/// الجداول:
/// - payroll.salary_structures            (هيكل الراتب — تعريف عام، مع currency)
/// - payroll.salary_structure_lines       (مكوّنات الهيكل: earnings / deductions)
/// - payroll.payroll_runs                 (الـ Aggregate Root: Draft → Processing → Posted)
/// - payroll.payroll_items                (قسيمة راتب لكل موظف داخل run)
/// - payroll.payslip_components           (تفاصيل مكوّنات القسيمة: earnings / deductions)
///
/// Business Rules:
/// - SalaryStructure.code فريد داخل الـ tenant
/// - PayrollRun.State machine: Draft → Processing → Posted (لا رجوع)
/// - عند Post: total_gross / total_net تُحدّث، posted_at يُسجّل
/// - PayrollItem.payment_days: عدد أيام العمل الفعلية (default 30)
/// - عند حذف موظف/هيكل: ON DELETE RESTRICT (لا نحذف تاريخ payroll — لـ SOX)
/// </summary>
[Migration(20260624_100000)]
public class CreatePayrollTables : Migration
{
    public override void Up()
    {
        // ============== salary_structures ==============
        Create.Table("salary_structures")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("code").AsString(50).NotNullable()
            .WithColumn("currency").AsString(3).NotNullable().WithDefaultValue("LYD")
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable()
            .WithColumn("updated_by").AsGuid().Nullable();
        Create.Index("ix_salary_structures_tenant_code").OnTable("salary_structures")
            .OnColumn("tenant_id").Ascending().OnColumn("code").Ascending().WithOptions().Unique();
        Create.Index("ix_salary_structures_tenant_active").OnTable("salary_structures")
            .OnColumn("tenant_id").Ascending().OnColumn("is_active").Ascending();

        // ============== salary_structure_lines ==============
        Create.Table("salary_structure_lines")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("salary_structure_id").AsGuid().NotNullable()
            .WithColumn("type").AsString(20).NotNullable()           // earning | deduction
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("formula").AsString(500).Nullable()          // مثال: "base * 0.10" (لاحقاً)
            .WithColumn("amount").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("sort_order").AsInt32().NotNullable().WithDefaultValue(0);
        Create.Index("ix_salary_structure_lines_tenant_structure").OnTable("salary_structure_lines")
            .OnColumn("tenant_id").Ascending().OnColumn("salary_structure_id").Ascending();
        Create.Index("ix_salary_structure_lines_structure_order").OnTable("salary_structure_lines")
            .OnColumn("salary_structure_id").Ascending().OnColumn("sort_order").Ascending();
        Create.ForeignKey("fk_salary_structure_lines_structure").FromTable("salary_structure_lines").ForeignColumn("salary_structure_id")
            .ToTable("salary_structures").PrimaryColumn("id").OnDelete(System.Data.Rule.None); // لا نحذف هيكل عليه تاريخ payroll

        // ============== payroll_runs ==============
        Create.Table("payroll_runs")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("period_start").AsDateTime().NotNullable()
            .WithColumn("period_end").AsDateTime().NotNullable()
            .WithColumn("status").AsString(20).NotNullable().WithDefaultValue("Draft")   // Draft | Processing | Posted | Cancelled
            .WithColumn("total_gross").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("total_net").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("processed_at").AsDateTime().Nullable()
            .WithColumn("posted_at").AsDateTime().Nullable()
            .WithColumn("notes").AsString(int.MaxValue).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable()
            .WithColumn("updated_by").AsGuid().Nullable();
        Create.Index("ix_payroll_runs_tenant_status").OnTable("payroll_runs")
            .OnColumn("tenant_id").Ascending().OnColumn("status").Ascending();
        Create.Index("ix_payroll_runs_tenant_period").OnTable("payroll_runs")
            .OnColumn("tenant_id").Ascending().OnColumn("period_start").Descending();
        Create.Index("ix_payroll_runs_tenant_created").OnTable("payroll_runs")
            .OnColumn("tenant_id").Ascending().OnColumn("created_at").Descending();

        // ============== payroll_items ==============
        Create.Table("payroll_items")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("payroll_run_id").AsGuid().NotNullable()
            .WithColumn("employee_id").AsGuid().NotNullable()
            .WithColumn("base_salary").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("gross_salary").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("tax_amount").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("social_insurance_employee").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("net_salary").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("status").AsString(20).NotNullable().WithDefaultValue("Draft")   // Draft | Processed | Posted
            .WithColumn("payment_days").AsInt32().NotNullable().WithDefaultValue(30)
            .WithColumn("notes").AsString(500).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();
        Create.Index("ix_payroll_items_tenant_run").OnTable("payroll_items")
            .OnColumn("tenant_id").Ascending().OnColumn("payroll_run_id").Ascending();
        Create.Index("ix_payroll_items_tenant_employee").OnTable("payroll_items")
            .OnColumn("tenant_id").Ascending().OnColumn("employee_id").Ascending();
        Create.Index("ix_payroll_items_tenant_status").OnTable("payroll_items")
            .OnColumn("tenant_id").Ascending().OnColumn("status").Ascending();
        Create.Index("ix_payroll_items_run_employee").OnTable("payroll_items")
            .OnColumn("payroll_run_id").Ascending().OnColumn("employee_id").Ascending().WithOptions().Unique();
        Create.ForeignKey("fk_payroll_items_run").FromTable("payroll_items").ForeignColumn("payroll_run_id")
            .ToTable("payroll_runs").PrimaryColumn("id").OnDelete(System.Data.Rule.None); // لا نحذف run مُرحَّل (SOX)
        Create.ForeignKey("fk_payroll_items_employee").FromTable("payroll_items").ForeignColumn("employee_id")
            .ToTable("employees").PrimaryColumn("id").OnDelete(System.Data.Rule.None); // لا نحذف موظف عليه تاريخ payroll

        // ============== payslip_components ==============
        Create.Table("payslip_components")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("payroll_item_id").AsGuid().NotNullable()
            .WithColumn("component_type").AsString(20).NotNullable()      // earning | deduction
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("amount").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("sort_order").AsInt32().NotNullable().WithDefaultValue(0);
        Create.Index("ix_payslip_components_tenant_item").OnTable("payslip_components")
            .OnColumn("tenant_id").Ascending().OnColumn("payroll_item_id").Ascending();
        Create.Index("ix_payslip_components_item_order").OnTable("payslip_components")
            .OnColumn("payroll_item_id").Ascending().OnColumn("sort_order").Ascending();
        Create.ForeignKey("fk_payslip_components_item").FromTable("payslip_components").ForeignColumn("payroll_item_id")
            .ToTable("payroll_items").PrimaryColumn("id").OnDelete(System.Data.Rule.Cascade);
    }

    public override void Down()
    {
        Delete.Table("payslip_components");
        Delete.Table("payroll_items");
        Delete.Table("payroll_runs");
        Delete.Table("salary_structure_lines");
        Delete.Table("salary_structures");
    }
}