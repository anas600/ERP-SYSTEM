using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Sprint 61 — Wave 1A (DEC-195 foundation) — Optional seed for Engineer's Daily Report.
///
/// <para><b>Why</b>: per the sprint plan, this seed is optional and can be empty. We keep it as
/// a placeholder no-op (with a single documentation comment) so the migration version slot is
/// reserved for a future Sprint 61+ patch that may need to backfill demo data without colliding
/// with the schema migration's 14-digit timestamp.</para>
///
/// <para><b>Idempotency</b>: no DDL is executed, so re-running is trivially safe.</para>
///
/// <para><b>Down()</b>: no-op (nothing to reverse).</para>
/// </summary>
[Migration(20260827_130000)]
public class Sprint61_EngineerReportSeed : Migration
{
    public override void Up()
    {
        // Intentionally empty for Sprint 61 Wave 1A. The schema migration above
        // (Sprint61_EngineerReportSchema_20260827_120000) is sufficient for
        // Wave 2A to start building repositories, services, and controllers
        // against an empty engineer_reports / engineer_report_photos /
        // engineer_report_signoffs schema.
        //
        // Future sprints (e.g. demo data seeding for Trust Mode verification)
        // can INSERT example reports here behind ON CONFLICT (project_id,
        // report_date) DO NOTHING so the seed remains idempotent.
    }

    public override void Down()
    {
        // No-op: this seed never inserts anything, so nothing to reverse.
    }
}
