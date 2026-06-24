using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Migration 009 — HR Core (Phase 3.5)
///
/// الجداول:
/// - hr.departments
/// - hr.employees
/// - hr.attendance
/// - hr.leave_requests
///
/// Business Rules:
/// - Department: Code فريد داخل الـ tenant
/// - Employee: Email فريد داخل الـ tenant
/// - Attendance: CheckIn/CheckOut متتابع (لا تكرار)
/// - LeaveRequest: EndDate >= StartDate + لا يتعارض مع إجازة أخرى للموظف نفسه
/// </summary>
[Migration(20260623_130000)]
public class CreateHRTables : Migration
{
    public override void Up()
    {
        // ============== departments ==============
        Create.Table("departments")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("code").AsString(50).NotNullable()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("parent_id").AsGuid().Nullable()
            .WithColumn("manager_id").AsGuid().Nullable()
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();
        Create.Index("ix_departments_tenant_code").OnTable("departments")
            .OnColumn("tenant_id").Ascending().OnColumn("code").Ascending().WithOptions().Unique();
        Create.Index("ix_departments_tenant_parent").OnTable("departments")
            .OnColumn("tenant_id").Ascending().OnColumn("parent_id").Ascending();
        Create.ForeignKey("fk_departments_parent").FromTable("departments").ForeignColumn("parent_id")
            .ToTable("departments").PrimaryColumn("id").OnDelete(System.Data.Rule.SetNull);

        // ============== employees ==============
        Create.Table("employees")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("employee_number").AsString(50).NotNullable()
            .WithColumn("full_name").AsString(200).NotNullable()
            .WithColumn("email").AsString(200).Nullable()
            .WithColumn("phone").AsString(50).Nullable()
            .WithColumn("national_id").AsString(50).Nullable()
            .WithColumn("department_id").AsGuid().Nullable()
            .WithColumn("job_title").AsString(100).Nullable()
            .WithColumn("hire_date").AsDateTime().NotNullable()
            .WithColumn("termination_date").AsDateTime().Nullable()
            .WithColumn("base_salary").AsDecimal(18, 4).NotNullable().WithDefaultValue(0)
            .WithColumn("is_active").AsBoolean().NotNullable().WithDefaultValue(true)
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable()
            .WithColumn("updated_by").AsGuid().Nullable();
        Create.Index("ix_employees_tenant_number").OnTable("employees")
            .OnColumn("tenant_id").Ascending().OnColumn("employee_number").Ascending().WithOptions().Unique();
        Create.Index("ix_employees_tenant_email").OnTable("employees")
            .OnColumn("tenant_id").Ascending().OnColumn("email").Ascending();
        Create.Index("ix_employees_tenant_department").OnTable("employees")
            .OnColumn("tenant_id").Ascending().OnColumn("department_id").Ascending();
        Create.Index("ix_employees_tenant_active").OnTable("employees")
            .OnColumn("tenant_id").Ascending().OnColumn("is_active").Ascending();
        Create.ForeignKey("fk_employees_department").FromTable("employees").ForeignColumn("department_id")
            .ToTable("departments").PrimaryColumn("id").OnDelete(System.Data.Rule.SetNull);

        // ============== attendance ==============
        Create.Table("attendance")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("employee_id").AsGuid().NotNullable()
            .WithColumn("type").AsString(20).NotNullable()
            .WithColumn("timestamp").AsDateTime().NotNullable()
            .WithColumn("notes").AsString(500).Nullable()
            .WithColumn("ip_address").AsString(50).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable();
        Create.Index("ix_attendance_tenant_employee").OnTable("attendance")
            .OnColumn("tenant_id").Ascending().OnColumn("employee_id").Ascending();
        Create.Index("ix_attendance_employee_ts").OnTable("attendance")
            .OnColumn("employee_id").Ascending().OnColumn("timestamp").Descending();
        Create.ForeignKey("fk_attendance_employee").FromTable("attendance").ForeignColumn("employee_id")
            .ToTable("employees").PrimaryColumn("id").OnDelete(System.Data.Rule.Cascade);

        // ============== leave_requests ==============
        Create.Table("leave_requests")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("tenant_id").AsGuid().NotNullable()
            .WithColumn("employee_id").AsGuid().NotNullable()
            .WithColumn("leave_type").AsString(20).NotNullable()
            .WithColumn("start_date").AsDateTime().NotNullable()
            .WithColumn("end_date").AsDateTime().NotNullable()
            .WithColumn("total_days").AsInt32().NotNullable()
            .WithColumn("status").AsString(20).NotNullable().WithDefaultValue("Pending")
            .WithColumn("reason").AsString(int.MaxValue).Nullable()
            .WithColumn("approver_id").AsGuid().Nullable()
            .WithColumn("approved_at").AsDateTime().Nullable()
            .WithColumn("notes").AsString(int.MaxValue).Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable()
            .WithColumn("created_by").AsGuid().NotNullable()
            .WithColumn("updated_at").AsDateTime().NotNullable();
        Create.Index("ix_leaves_tenant_employee").OnTable("leave_requests")
            .OnColumn("tenant_id").Ascending().OnColumn("employee_id").Ascending();
        Create.Index("ix_leaves_tenant_status").OnTable("leave_requests")
            .OnColumn("tenant_id").Ascending().OnColumn("status").Ascending();
        Create.Index("ix_leaves_employee_dates").OnTable("leave_requests")
            .OnColumn("employee_id").Ascending().OnColumn("start_date").Ascending().OnColumn("end_date").Ascending();
        Create.ForeignKey("fk_leaves_employee").FromTable("leave_requests").ForeignColumn("employee_id")
            .ToTable("employees").PrimaryColumn("id").OnDelete(System.Data.Rule.Cascade);
    }

    public override void Down()
    {
        Delete.Table("leave_requests");
        Delete.Table("attendance");
        Delete.Table("employees");
        Delete.Table("departments");
    }
}
