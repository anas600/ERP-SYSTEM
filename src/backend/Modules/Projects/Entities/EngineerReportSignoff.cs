using System;

namespace ERPSystem.Modules.Projects.Entities;

/// <summary>
/// Sprint 61 (DEC-194) — Electronic signoff for an Engineer's Daily Report.
///
/// <para>When a report is in <see cref="EngineerReportStatus.Submitted"/>, a PM or
/// Client reviews it and either approves or rejects it. Each signoff event is
/// recorded as a row in this table so the full audit trail is preserved (who
/// decided what, when, with what optional comment).</para>
///
/// <para><b>signer_role</b> is one of: 'PM' (Project Manager), 'Client'
/// (customer representative), or 'Engineer' (self-acknowledgment before
/// submission). The string is stored as TEXT (per the schema) so future roles
/// can be added without a schema change.</para>
///
/// <para><b>approved</b> is a single boolean (not a status enum) because the
/// parent report's <see cref="EngineerReport.Status"/> is the source of truth
/// for the final report state. A row in this table is a single signoff event;
/// a report can have multiple signoffs over its lifetime (e.g. Rejected →
/// resubmitted → Approved by a different signer).</para>
///
/// <para><b>ON DELETE CASCADE</b> (set in the migration): when the parent report
/// is deleted, all of its signoffs go with it.</para>
/// </summary>
public class EngineerReportSignoff
{
    public Guid Id { get; set; }

    /// <summary>Sprint 61 (DEC-194) — Constitution Article 3, L19. Not nullable.</summary>
    public Guid CompanyId { get; set; }

    public Guid ReportId { get; set; }
    public Guid SignerId { get; set; }

    /// <summary>'PM' | 'Client' | 'Engineer' (per DEC-194 — TEXT, not enum int, for future-proofing).</summary>
    public string SignerRole { get; set; } = string.Empty;

    public DateTime SignedAt { get; set; }

    /// <summary>Optional typed signature (e.g. "Anas Assaket — 2026-08-27").</summary>
    public string? SignatureText { get; set; }

    public string? Comment { get; set; }

    /// <summary>true = approved, false = rejected.</summary>
    public bool Approved { get; set; }
}
