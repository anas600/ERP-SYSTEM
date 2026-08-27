using Dapper;
using ERPSystem.Modules.Projects.Application;
using ERPSystem.Modules.Projects.Infrastructure;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Projects.Application.Services;

// =====================================================================================
// Sprint 65 / DEC-233: ProjectCostService — aggregates cost components for a project
// =====================================================================================
//
// The Project P&L (Sprint 57 / DEC-161) currently reads costs from `journal_lines` on
// Expense accounts. It deliberately EXCLUDES subcontractor payments, because the
// sub_payments schema (Sprint 64) is not yet merged into `develop` (it's on
// `feature/sprint-64-subcontractor` at the time of writing).
//
// This service is the bridge for Wave 2A (DEC-233): it gives the Project P&L a clean
// interface to read the subcontractor cost component without coupling the P&L service
// to the Sprint 64 schema. The Sprint 64 schema will plug in here when it merges.
//
// Algorithm (per project, scoped by ICompanyContext.CompanyId):
//   subcontractorCost = SUM(sub_payments.amount)
//                       WHERE sub_contract.project_id = @projectId
//                         AND sub_payments.status != 4 (cancelled)
//                         AND sub_payments.company_id = @companyId
//
// Until Sprint 64 lands, the ISubPaymentRepository implementation is a no-op (returns 0)
// so the service is unit-testable in isolation against the in-memory FakeDb. When the
// real schema lands, the no-op is replaced with a Dapper-backed implementation and the
// unit tests continue to pass without modification (because the tests mock the repo).
//
// L19 / DEC-095: CompanyId is read from `ICompanyContext.CompanyId` at the top of every
// public method. UserId is NOT needed here (read-only aggregation).
// =====================================================================================

/// <summary>Per-project cost breakdown across all five sources of cost.</summary>
public sealed class ProjectCostBreakdown
{
    public Guid ProjectId { get; set; }
    public decimal DirectLaborCost { get; set; }      // from journal_lines on labor accounts
    public decimal MaterialCost { get; set; }         // from inventory issue transactions
    public decimal SubcontractorCost { get; set; }    // from sub_payments (Sprint 64)
    public decimal EquipmentCost { get; set; }        // from equipment rental accounts
    public decimal OverheadAllocation { get; set; }    // from journal_lines on overhead accounts
    public decimal TotalCost => DirectLaborCost + MaterialCost + SubcontractorCost + EquipmentCost + OverheadAllocation;
}

/// <summary>Generic result wrapper for ProjectCostService operations.</summary>
public sealed class ProjectCostResult<T>
{
    public bool Succeeded { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public static ProjectCostResult<T> Ok(T v) => new() { Succeeded = true, Value = v };
    public static ProjectCostResult<T> Fail(string e) => new() { Succeeded = false, Error = e };
}

/// <summary>
/// Subcontractor cost repository — seam for cross-sprint dependency (L201).
/// The default implementation (NoOpSubcontractorCostRepository) returns 0 because
/// the sub_payments table is not yet on `develop`; when Sprint 64 merges, a Dapper-
/// backed implementation is registered in Program.cs and the unit tests continue to
/// pass without changes.
///
/// <para><b>Why renamed from <c>ISubPaymentRepository</c></b>: the real repository
/// lives in <c>ERPSystem.Modules.Projects.Infrastructure.ISubPaymentRepository</c>
/// (Sprint 64). Reusing the same name in this namespace causes a CS0104 ambiguous-
/// reference error in any consumer that imports both. <c>ISubcontractorCostRepository</c>
/// makes the seam explicit and intent-revealing.</para>
/// </summary>
public interface ISubcontractorCostRepository
{
    /// <summary>
    /// SUM of sub_payments.amount for the given project, excluding cancelled payments (status=4).
    /// Returns 0 if the project has no payments, no subcontracts, or the sub_payments table
    /// is not yet present (Sprint 64 pre-merge state).
    /// </summary>
    Task<decimal> SumActivePaymentsForProjectAsync(Guid companyId, Guid projectId, CancellationToken ct);
}

/// <summary>
/// Default no-op implementation. The Sprint 64 sub_payments schema does not exist on
/// `develop` at Wave 2A time, so this returns 0. When Sprint 64 merges, a real Dapper
/// implementation replaces this in DI.
/// </summary>
public sealed class NoOpSubcontractorCostRepository : ISubcontractorCostRepository
{
    public Task<decimal> SumActivePaymentsForProjectAsync(Guid companyId, Guid projectId, CancellationToken ct)
        => Task.FromResult(0m);
}

public interface IProjectCostService
{
    /// <summary>
    /// Full per-project cost breakdown across all 5 categories (labor, material,
    /// subcontractor, equipment, overhead). Used by the Dashboard cross-module
    /// "Project Profitability" widget.
    /// </summary>
    Task<ProjectCostResult<ProjectCostBreakdown>> GetBreakdownAsync(Guid projectId, CancellationToken ct);

    /// <summary>
    /// Lightweight — only returns the subcontractor cost (the only one that doesn't come
    /// from journal_lines). Called by ProjectPnLService to fold into TotalCosts.
    /// </summary>
    Task<ProjectCostResult<decimal>> GetSubcontractorCostAsync(Guid projectId, CancellationToken ct);
}

public sealed class ProjectCostService : IProjectCostService
{
    private readonly IProjectRepository _projects;
    private readonly IDbConnectionFactory _db;
    private readonly ISubcontractorCostRepository _subPayments;
    private readonly ERPSystem.Shared.CompanyContext.ICompanyContext _company;
    private readonly Microsoft.Extensions.Logging.ILogger<ProjectCostService> _logger;

    public ProjectCostService(
        IProjectRepository projects,
        IDbConnectionFactory db,
        ISubcontractorCostRepository subPayments,
        ERPSystem.Shared.CompanyContext.ICompanyContext company,
        Microsoft.Extensions.Logging.ILogger<ProjectCostService> logger)
    {
        _projects = projects;
        _db = db;
        _subPayments = subPayments;
        _company = company;
        _logger = logger;
    }

    public async Task<ProjectCostResult<ProjectCostBreakdown>> GetBreakdownAsync(Guid projectId, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(projectId, ct);
        if (project == null)
            return ProjectCostResult<ProjectCostBreakdown>.Fail("المشروع غير موجود.");

        var companyId = _company.CompanyId
            ?? throw new InvalidOperationException("Company context not resolved (L19 / DEC-095).");

        using var conn = await _db.CreateOltpConnectionAsync(ct);

        // The cost center id we use to scope journal_lines is the project's own
        // cost_center_id (auto-created in ProjectService.CreateAsync, Sprint 22).
        var costCenterId = project.CostCenterId;

        // SUM(debit - credit) on Expense accounts grouped by category. We do this in a
        // single query and pick the rows back by account code prefix to avoid 5 separate
        // round-trips. The account-code categories are stable per the Sprint 60 CoA:
        //   5xxx Labor  (5,100 sub labor, 5,110 sub direct, etc.)
        //   5,2xx Material (5200..5299)
        //   5,3xx Equipment / rental
        //   5,4xx Overhead allocation
        //   5,5xx Subcontractor (the pre-Sprint-64 path; Wave 2A prefers the sub_payments
        //   sum so this bucket is left as 0 for now to avoid double-counting).
        const string categorySql = @"
            SELECT a.code AS Code,
                   COALESCE(SUM(jl.debit - jl.credit), 0) AS Amount
            FROM journal_lines jl
            INNER JOIN journal_entries je ON je.id = jl.journal_entry_id
            INNER JOIN accounts a ON a.id = jl.account_id
            WHERE je.cost_center_id = @CostCenterId
              AND je.status = 2
              AND a.type = 5
              AND (jl.debit - jl.credit) > 0
            GROUP BY a.code;";

        var rows = (await conn.QueryAsync<(string Code, decimal Amount)>(new CommandDefinition(
            categorySql,
            new { CostCenterId = costCenterId },
            cancellationToken: ct))).ToList();

        decimal SumWhere(Func<string, bool> predicate) =>
            rows.Where(r => predicate(r.Code)).Sum(r => r.Amount);

        // Subcontractor cost is the only category that does NOT come from journal_lines.
        // The Sprint 60 default CoA bucket 5500 is intentionally excluded here so the
        // sub_payments sum (when Sprint 64 lands) is the single source of truth for
        // subcontractor cost.
        var subcontractorCost = await _subPayments.SumActivePaymentsForProjectAsync(companyId, projectId, ct);

        var breakdown = new ProjectCostBreakdown
        {
            ProjectId = projectId,
            DirectLaborCost = SumWhere(c => c.StartsWith("51") || c.StartsWith("5100")),
            MaterialCost = SumWhere(c => c.StartsWith("52")),
            SubcontractorCost = subcontractorCost,
            EquipmentCost = SumWhere(c => c.StartsWith("53")),
            OverheadAllocation = SumWhere(c => c.StartsWith("54")),
        };

        return ProjectCostResult<ProjectCostBreakdown>.Ok(breakdown);
    }

    public async Task<ProjectCostResult<decimal>> GetSubcontractorCostAsync(Guid projectId, CancellationToken ct)
    {
        var project = await _projects.GetByIdAsync(projectId, ct);
        if (project == null)
            return ProjectCostResult<decimal>.Fail("المشروع غير موجود.");

        var companyId = _company.CompanyId
            ?? throw new InvalidOperationException("Company context not resolved (L19 / DEC-095).");

        var amount = await _subPayments.SumActivePaymentsForProjectAsync(companyId, projectId, ct);
        return ProjectCostResult<decimal>.Ok(amount);
    }
}
