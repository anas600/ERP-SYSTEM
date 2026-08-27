using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Modules.Projects.Infrastructure;
using ERPSystem.Shared.CompanyContext;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Projects.Application.Services;

// =============================================================================
// Sprint 64 / DEC-225 — Sub-Statement service.
//
// Computes the per-sub-contract P&L and the per-(subcontractor, project)
// summary by aggregating sub_progress_billings + sub_payments via SUM queries
// against the existing repositories (no new DB tables or columns).
//
// L19 / DEC-095: company scoping happens at the repository level (every
// SELECT/INSERT is keyed by company_id). The service itself does not need to
// pass company_id for read paths because the row already carries it and the
// sub_contract lookup validates ownership.
// =============================================================================

/// <summary>Result envelope for SubStatement service calls.</summary>
public sealed class SubStatementResult<T>
{
    public bool Succeeded { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public SubStatementErrorCode? ErrorCode { get; init; }
    public static SubStatementResult<T> Ok(T v) => new() { Succeeded = true, Value = v };
    public static SubStatementResult<T> Fail(string e, SubStatementErrorCode c) =>
        new() { Succeeded = false, Error = e, ErrorCode = c };
}

public enum SubStatementErrorCode
{
    NotFound, ValidationError, Internal
}

public interface ISubStatementService
{
    Task<SubStatementResult<SubStatementResponse>> GetBySubContractAsync(
        Guid subContractId, CancellationToken ct);

    Task<SubStatementResult<SubStatementSummaryResponse>> GetBySubcontractorAndProjectAsync(
        Guid subcontractorId, Guid projectId, CancellationToken ct);
}

/// <summary>
/// Sprint 64 / DEC-225 — Sub-Statement service.
///
/// <para><b>Algorithm (GetBySubContractAsync)</b>:</para>
/// <list type="number">
///   <item>Load the sub-contract (404 if missing).</item>
///   <item>Load the subcontractor master record (for name + code display).</item>
///   <item>Roll up <c>sub_progress_billings</c>: totalBilledGross, totalRetentionWithheld, billingCount, first/last billing date, work-completed sum (capped at 100).</item>
///   <item>Roll up <c>sub_payments</c>: totalPaid (amount + retention_released), totalRetentionReleased, lastPaymentDate.</item>
///   <item>outstandingBalance = totalBilledGross − totalPaid.</item>
///   <item>healthStatus = SETTLED if outstanding == 0 AND totalBilledGross &gt; 0; OVERDUE if lastBillingDate &gt; 60 days ago AND outstanding &gt; 0; else OK.</item>
/// </list>
///
/// <para><b>Algorithm (GetBySubcontractorAndProjectAsync)</b>: aggregates the
/// per-contract P&L across every sub-contract that links the given
/// subcontractor to the given project. Fields use the same SUM-based formulas
/// as the single-contract endpoint.</para>
///
/// <para><b>L19 / DEC-095</b>: company scoping happens at the repository layer.
/// The SubContract lookup already filters by id; the service additionally
/// validates <c>CompanyId</c> to refuse cross-company reads.</para>
/// </summary>
public sealed class SubStatementService : ISubStatementService
{
    private readonly ISubContractRepository _subContracts;
    private readonly ISubcontractorRepository _subcontractors;
    private readonly ISubProgressBillingRepository _billings;
    private readonly ISubPaymentRepository _payments;
    private readonly IProjectRepository _projects;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<SubStatementService> _logger;

    public SubStatementService(
        ISubContractRepository subContracts,
        ISubcontractorRepository subcontractors,
        ISubProgressBillingRepository billings,
        ISubPaymentRepository payments,
        IProjectRepository projects,
        ICompanyContext companyContext,
        ILogger<SubStatementService> logger)
    {
        _subContracts = subContracts;
        _subcontractors = subcontractors;
        _billings = billings;
        _payments = payments;
        _projects = projects;
        _companyContext = companyContext;
        _logger = logger;
    }

    public async Task<SubStatementResult<SubStatementResponse>> GetBySubContractAsync(
        Guid subContractId, CancellationToken ct)
    {
        var companyId = _companyContext.CompanyId;
        if (companyId == null)
            return SubStatementResult<SubStatementResponse>.Fail(
                "لم يتم تحديد الشركة الحالية.", SubStatementErrorCode.ValidationError);

        var sc = await _subContracts.GetByIdAsync(subContractId, ct);
        if (sc == null)
            return SubStatementResult<SubStatementResponse>.Fail(
                "العقد الباطن غير موجود.", SubStatementErrorCode.NotFound);
        if (sc.CompanyId != companyId)
            return SubStatementResult<SubStatementResponse>.Fail(
                "العقد الباطن لا ينتمي لشركتك.", SubStatementErrorCode.ValidationError);

        var sub = await _subcontractors.GetByIdAsync(sc.SubcontractorId, ct);
        if (sub == null)
            return SubStatementResult<SubStatementResponse>.Fail(
                "المقاول الباطن المرتبط بالعقد غير موجود.", SubStatementErrorCode.NotFound);

        // --- Roll up sub_progress_billings (status != 4 means exclude Cancelled) ---
        var billingRows = await _billings.ListBySubContractAsync(subContractId, ct);
        var activeBillings = billingRows.Where(b => b.Status != (int)SubProgressBillingStatus.Cancelled).ToList();

        var totalBilledGross = activeBillings.Sum(b => b.GrossAmount);
        var totalRetentionWithheld = activeBillings.Sum(b => b.RetentionDeducted);
        var billingCount = activeBillings.Count;
        var firstBillingDate = activeBillings.Count > 0
            ? activeBillings.Min(b => b.BillingDate)
            : (DateTime?)null;
        var lastBillingDate = activeBillings.Count > 0
            ? activeBillings.Max(b => b.BillingDate)
            : (DateTime?)null;
        var workCompletedSum = activeBillings.Sum(b => b.WorkCompletedPercent);
        var workCompletedToDate = Math.Min(100m, workCompletedSum);

        // --- Roll up sub_payments (amount + retention_released) ---
        var paymentRows = await _payments.ListBySubContractAsync(subContractId, ct);
        var totalRetentionReleased = paymentRows.Sum(p => p.RetentionReleased);
        var totalPaid = paymentRows.Sum(p => p.Amount + p.RetentionReleased);
        var lastPaymentDate = paymentRows.Count > 0
            ? paymentRows.Max(p => p.PaymentDate)
            : (DateTime?)null;

        var outstandingBalance = totalBilledGross - totalPaid;

        // --- Health status ---
        string healthStatus;
        if (outstandingBalance == 0m && totalBilledGross > 0m)
            healthStatus = "SETTLED";
        else if (outstandingBalance > 0m
                 && lastBillingDate.HasValue
                 && (DateTime.UtcNow.Date - lastBillingDate.Value.Date).TotalDays > 60)
            healthStatus = "OVERDUE";
        else
            healthStatus = "OK";

        var response = new SubStatementResponse
        {
            SubContractId = sc.Id,
            SubcontractorName = sub.Name,
            SubContractorCode = sub.Code,
            ContractNumber = sc.ContractNumber,
            ScopeOfWork = sc.ScopeOfWork,
            ContractValue = sc.ContractValue,
            TotalBilledGross = totalBilledGross,
            TotalRetentionWithheld = totalRetentionWithheld,
            TotalRetentionReleased = totalRetentionReleased,
            TotalPaid = totalPaid,
            OutstandingBalance = outstandingBalance,
            WorkCompletedToDate = workCompletedToDate,
            BillingCount = billingCount,
            FirstBillingDate = firstBillingDate,
            LastBillingDate = lastBillingDate,
            LastPaymentDate = lastPaymentDate,
            Status = sc.Status,
            HealthStatus = healthStatus,
        };

        _logger.LogInformation(
            "SubStatement built subContract={SubId} billed={Billed} paid={Paid} outstanding={Outstanding} health={Health}",
            subContractId, totalBilledGross, totalPaid, outstandingBalance, healthStatus);

        return SubStatementResult<SubStatementResponse>.Ok(response);
    }

    public async Task<SubStatementResult<SubStatementSummaryResponse>> GetBySubcontractorAndProjectAsync(
        Guid subcontractorId, Guid projectId, CancellationToken ct)
    {
        var companyId = _companyContext.CompanyId;
        if (companyId == null)
            return SubStatementResult<SubStatementSummaryResponse>.Fail(
                "لم يتم تحديد الشركة الحالية.", SubStatementErrorCode.ValidationError);

        var sub = await _subcontractors.GetByIdAsync(subcontractorId, ct);
        if (sub == null)
            return SubStatementResult<SubStatementSummaryResponse>.Fail(
                "المقاول الباطن غير موجود.", SubStatementErrorCode.NotFound);
        if (sub.CompanyId != companyId)
            return SubStatementResult<SubStatementSummaryResponse>.Fail(
                "المقاول الباطن لا ينتمي لشركتك.", SubStatementErrorCode.ValidationError);

        var project = await _projects.GetByIdAsync(projectId, ct);
        if (project == null)
            return SubStatementResult<SubStatementSummaryResponse>.Fail(
                "المشروع غير موجود.", SubStatementErrorCode.NotFound);
        if (project.CompanyId != companyId)
            return SubStatementResult<SubStatementSummaryResponse>.Fail(
                "المشروع لا ينتمي لشركتك.", SubStatementErrorCode.ValidationError);

        // All sub-contracts for this (subcontractor, project) pair.
        var subContracts = (await _subContracts.ListBySubcontractorAsync(subcontractorId, ct))
            .Where(sc => sc.ProjectId == projectId)
            .ToList();

        if (subContracts.Count == 0)
            return SubStatementResult<SubStatementSummaryResponse>.Fail(
                "لا يوجد عقود باطن لهذا المقاول على هذا المشروع.", SubStatementErrorCode.NotFound);

        decimal totalContractValue = 0m;
        decimal totalBilled = 0m;
        decimal totalPaid = 0m;

        foreach (var sc in subContracts)
        {
            totalContractValue += sc.ContractValue;

            // Billed gross (exclude cancelled billings).
            var billingRows = await _billings.ListBySubContractAsync(sc.Id, ct);
            totalBilled += billingRows
                .Where(b => b.Status != (int)SubProgressBillingStatus.Cancelled)
                .Sum(b => b.GrossAmount);

            // Paid = amount + retention_released.
            var paymentRows = await _payments.ListBySubContractAsync(sc.Id, ct);
            totalPaid += paymentRows.Sum(p => p.Amount + p.RetentionReleased);
        }

        var totalOutstanding = totalBilled - totalPaid;

        var summary = new SubStatementSummaryResponse
        {
            SubcontractorId = sub.Id,
            SubcontractorName = sub.Name,
            ProjectId = project.Id,
            ProjectName = project.Name,
            SubContractCount = subContracts.Count,
            TotalContractValue = totalContractValue,
            TotalBilled = totalBilled,
            TotalPaid = totalPaid,
            TotalOutstanding = totalOutstanding,
        };

        _logger.LogInformation(
            "SubStatement summary built sub={SubId} project={ProjectId} contracts={Count} outstanding={Outstanding}",
            subcontractorId, projectId, subContracts.Count, totalOutstanding);

        return SubStatementResult<SubStatementSummaryResponse>.Ok(summary);
    }
}
