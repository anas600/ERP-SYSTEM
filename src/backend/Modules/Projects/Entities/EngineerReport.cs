using System;

namespace ERPSystem.Modules.Projects.Entities;

/// <summary>
/// Sprint 61 (DEC-192) — Engineer's Daily Report workflow status.
///
/// <para>State machine (DEC-194 design):</para>
/// <list type="bullet">
///   <item><c>Draft</c> — engineer is still writing; editable</item>
///   <item><c>Submitted</c> — engineer has submitted for review; not editable, awaiting signoff</item>
///   <item><c>Approved</c> — PM or Client has approved via signoff; immutable</item>
///   <item><c>Rejected</c> — PM or Client has rejected via signoff; engineer must revise + resubmit</item>
/// </list>
///
/// <para>The string values are persisted to the <c>engineer_reports.status</c>
/// TEXT column (per DEC-192 — the schema uses TEXT, not int, to keep the
/// migration self-documenting and to allow future statuses without a schema
/// change).</para>
/// </summary>
public enum EngineerReportStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Rejected = 4
}

/// <summary>
/// Sprint 61 (DEC-192) — Engineer's Daily Report (تقرير المهندس اليومي).
///
/// <para>One report per project per day (enforced by UNIQUE (project_id, report_date)
/// at the DB level). The engineer writes weather, work_done (required), and issues
/// (optional). Photos and signoffs are stored in child tables
/// (<see cref="EngineerReportPhoto"/>, <see cref="EngineerReportSignoff"/>).</para>
///
/// <para><b>Constitution Article 3</b>: <c>CompanyId</c> is required and not nullable
/// (per L19 / L29 / L30 lessons). The service resolves the company from the JWT
/// context (never from the request DTO — see L19).</para>
/// </summary>
public class EngineerReport
{
    public Guid Id { get; set; }

    /// <summary>Sprint 61 (DEC-192) — Constitution Article 3, L19. Not nullable.</summary>
    public Guid CompanyId { get; set; }

    public Guid ProjectId { get; set; }
    public DateTime ReportDate { get; set; }
    public Guid EngineerId { get; set; }
    public EngineerReportStatus Status { get; set; } = EngineerReportStatus.Draft;
    public string? Weather { get; set; }
    public string WorkDone { get; set; } = string.Empty;
    public string? Issues { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
