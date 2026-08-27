using FluentMigrator;

namespace ERPSystem.Shared.Migrations;

/// <summary>
/// Sprint 61 — Wave 1A (DEC-192, DEC-193, DEC-194) — Engineer's Daily Report schema.
///
/// <para><b>Why</b>: per client meeting 22-Aug-2026 (CEO + محاسب), the Engineering / Construction
/// projects need a daily on-site report flow: the engineer writes a report (weather, work done,
/// issues), uploads photos, and the PM/Client approves or rejects it electronically. This is the
/// foundation schema (3 tables) for the new module.</para>
///
/// <para><b>DEC-192 — engineer_reports</b> (main table):</para>
/// <list type="bullet">
///   <item>One report per project per day (UNIQUE (project_id, report_date))</item>
///   <item>Status: Draft | Submitted | Approved | Rejected</item>
///   <item>Captures weather, work_done (required), issues (optional)</item>
///   <item>Created/Updated timestamps for audit</item>
/// </list>
///
/// <para><b>DEC-193 — engineer_report_photos</b> (1:N with engineer_reports):</para>
/// <list type="bullet">
///   <item>Photos stored on disk; table holds file_path + optional caption</item>
///   <item>company_id denormalized for FK performance (per DEC-192 design note)</item>
///   <item>ON DELETE CASCADE — deleting a report removes its photos</item>
/// </list>
///
/// <para><b>DEC-194 — engineer_report_signoffs</b> (1:N with engineer_reports):</para>
/// <list type="bullet">
///   <item>Electronic approval workflow — PM or Client signs off (approve/reject)</item>
///   <item>signer_role: 'PM' | 'Client' | 'Engineer'</item>
///   <item>approved: true = approved, false = rejected (single boolean, not status enum,
///         because the engineer_report.status is the source of truth for the final state)</item>
///   <item>ON DELETE CASCADE — deleting a report removes its signoffs</item>
/// </list>
///
/// <para><b>Idempotency</b>: every CREATE TABLE uses <c>IF NOT EXISTS</c> and every
/// CREATE INDEX uses a Postgres <c>IF NOT EXISTS</c> guard via <c>CREATE INDEX IF NOT EXISTS</c>.
/// UNIQUE constraints are created inside the CREATE TABLE block (Postgres supports them inline).
/// The migration is safely re-runnable.</para>
///
/// <para><b>Down()</b>: drops the 3 tables in reverse-dependency order (signoffs → photos →
/// reports). Each DROP uses IF EXISTS so re-running Down is a no-op.</para>
/// </summary>
[Migration(20260827_120000)]
public class Sprint61_EngineerReportSchema : Migration
{
    public override void Up()
    {
        // ============== DEC-192 — engineer_reports ==============
        Execute.Sql(@"
            CREATE TABLE IF NOT EXISTS engineer_reports (
                id UUID PRIMARY KEY,
                company_id UUID NOT NULL REFERENCES companies(id),
                project_id UUID NOT NULL REFERENCES projects(id),
                report_date DATE NOT NULL,
                engineer_id UUID NOT NULL REFERENCES users(id),
                status TEXT NOT NULL DEFAULT 'Draft',
                weather TEXT,
                work_done TEXT NOT NULL,
                issues TEXT,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                UNIQUE (project_id, report_date)
            );
        ");

        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_engineer_reports_company_project
                ON engineer_reports(company_id, project_id);
        ");

        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_engineer_reports_status
                ON engineer_reports(company_id, status);
        ");

        // ============== DEC-193 — engineer_report_photos ==============
        Execute.Sql(@"
            CREATE TABLE IF NOT EXISTS engineer_report_photos (
                id UUID PRIMARY KEY,
                report_id UUID NOT NULL REFERENCES engineer_reports(id) ON DELETE CASCADE,
                company_id UUID NOT NULL REFERENCES companies(id),
                file_path TEXT NOT NULL,
                caption TEXT,
                uploaded_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
        ");

        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_engineer_report_photos_report
                ON engineer_report_photos(report_id);
        ");

        // ============== DEC-194 — engineer_report_signoffs ==============
        Execute.Sql(@"
            CREATE TABLE IF NOT EXISTS engineer_report_signoffs (
                id UUID PRIMARY KEY,
                report_id UUID NOT NULL REFERENCES engineer_reports(id) ON DELETE CASCADE,
                company_id UUID NOT NULL REFERENCES companies(id),
                signer_id UUID NOT NULL REFERENCES users(id),
                signer_role TEXT NOT NULL,
                signed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                signature_text TEXT,
                comment TEXT,
                approved BOOLEAN NOT NULL
            );
        ");

        Execute.Sql(@"
            CREATE INDEX IF NOT EXISTS idx_engineer_report_signoffs_report
                ON engineer_report_signoffs(report_id);
        ");
    }

    public override void Down()
    {
        // Reverse-dependency order: drop child tables before parent.
        Execute.Sql("DROP TABLE IF EXISTS engineer_report_signoffs CASCADE;");
        Execute.Sql("DROP TABLE IF EXISTS engineer_report_photos CASCADE;");
        Execute.Sql("DROP TABLE IF EXISTS engineer_reports CASCADE;");
    }
}
