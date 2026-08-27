using System;

namespace ERPSystem.Modules.Projects.Entities;

/// <summary>
/// Sprint 61 (DEC-193) — Photo attached to an Engineer's Daily Report.
///
/// <para>Photos are stored on disk (under <c>wwwroot/uploads/engineer-reports/{reportId}/</c>,
/// gitignored). The table holds the file path and an optional caption. Multiple photos
/// per report are allowed (1:N relationship).</para>
///
/// <para><b>company_id denormalized</b> (per DEC-193 design note): we duplicate
/// <c>company_id</c> here even though it is reachable via <c>report_id</c> →
/// <c>engineer_reports.company_id</c>. This avoids a 2-table JOIN on every photo
/// query, and matches the pattern used in <c>boq_lines</c> + <c>boq_subitems</c>.</para>
///
/// <para><b>ON DELETE CASCADE</b> (set in the migration): when the parent report is
/// deleted, all of its photos go with it. The application layer is expected to
/// also delete the underlying file on disk in the same transaction.</para>
/// </summary>
public class EngineerReportPhoto
{
    public Guid Id { get; set; }

    /// <summary>Sprint 61 (DEC-193) — Constitution Article 3, L19. Not nullable.</summary>
    public Guid CompanyId { get; set; }

    public Guid ReportId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public DateTime UploadedAt { get; set; }
}
