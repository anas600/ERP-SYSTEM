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
// Sprint 64 / DEC-223 — Sub-ProgressBilling service.
//
// Manages sub_progress_billings. CompanyId always comes from the JWT context
// (L19 / DEC-095), never from the request DTO. The service computes
// gross_amount, retention_deducted, and net_payable on every create / update
// based on the parent SubContract terms.
// =============================================================================

/// <summary>Result envelope for SubProgressBilling service calls.</summary>
public sealed class SubProgressBillingResult<T>
{
    public bool Succeeded { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public SubProgressBillingErrorCode? ErrorCode { get; init; }
    public static SubProgressBillingResult<T> Ok(T v) => new() { Succeeded = true, Value = v };
    public static SubProgressBillingResult<T> Fail(string e, SubProgressBillingErrorCode c) =>
        new() { Succeeded = false, Error = e, ErrorCode = c };
}

public enum SubProgressBillingErrorCode
{
    NotFound, AlreadyExists, ValidationError, Internal
}

public interface ISubProgressBillingService
{
    Task<SubProgressBillingResult<SubProgressBillingResponse>> CreateAsync(
        Guid userId, Guid subContractId, CreateSubProgressBillingRequest req, CancellationToken ct);

    Task<SubProgressBillingResult<SubProgressBillingResponse>> UpdateAsync(
        Guid userId, Guid id, UpdateSubProgressBillingRequest req, CancellationToken ct);

    Task<SubProgressBillingResult<SubProgressBillingResponse>> GetByIdAsync(
        Guid id, CancellationToken ct);

    Task<SubProgressBillingResult<IReadOnlyList<SubProgressBillingResponse>>> ListBySubContractAsync(
        Guid subContractId, CancellationToken ct);

    Task<SubProgressBillingResult<SubProgressBillingResponse>> ApproveAsync(
        Guid userId, Guid id, CancellationToken ct);
}

/// <summary>
/// Sprint 64 / DEC-223 — Sub-ProgressBilling service.
///
/// <para><b>Algorithm (Create / Update)</b>:</para>
/// <code>
/// 1. Load subContract (verify exists, get contract_value, retention_percent, retention_release_billing)
/// 2. Count existing billings for this subContract (already-paid ones included)
/// 3. billingNumber — must be unique within the subContract
/// 4. gross = sub_contract.contract_value × (work_completed_percent / 100)
/// 5. previousBillingsAmount = SUM(gross) of all PRIOR billings
/// 6. currentCumulative = previousBillingsAmount + gross
/// 7. retentionDeducted = (current_billing_count &lt;= sub_contract.retention_release_billing)
///                           ? gross × retention_percent / 100
///                           : 0
/// 8. netPayable = gross - retentionDeducted
/// 9. Insert
/// </code>
///
/// <para><b>L19 / DEC-095</b>: CompanyId comes from <see cref="ICompanyContext"/>,
/// never from the request DTO.</para>
/// </summary>
public sealed class SubProgressBillingService : ISubProgressBillingService
{
    private readonly ISubProgressBillingRepository _billings;
    private readonly ISubContractRepository _subContracts;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<SubProgressBillingService> _logger;

    public SubProgressBillingService(
        ISubProgressBillingRepository billings,
        ISubContractRepository subContracts,
        ICompanyContext companyContext,
        ILogger<SubProgressBillingService> logger)
    {
        _billings = billings;
        _subContracts = subContracts;
        _companyContext = companyContext;
        _logger = logger;
    }

    public async Task<SubProgressBillingResult<SubProgressBillingResponse>> CreateAsync(
        Guid userId, Guid subContractId, CreateSubProgressBillingRequest req, CancellationToken ct)
    {
        // L19 / DEC-095: CompanyId from JWT, not from request.
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved");

        // Validate basic fields.
        if (string.IsNullOrWhiteSpace(req.BillingNumber))
            return SubProgressBillingResult<SubProgressBillingResponse>.Fail(
                "رقم المستخلص مطلوب.", SubProgressBillingErrorCode.ValidationError);
        if (req.WorkCompletedPercent < 0m || req.WorkCompletedPercent > 100m)
            return SubProgressBillingResult<SubProgressBillingResponse>.Fail(
                "نسبة الإنجاز يجب أن تكون بين 0 و 100.", SubProgressBillingErrorCode.ValidationError);

        // SubContract must exist + same company.
        var sc = await _subContracts.GetByIdAsync(subContractId, ct);
        if (sc == null)
            return SubProgressBillingResult<SubProgressBillingResponse>.Fail(
                "العقد الباطن غير موجود.", SubProgressBillingErrorCode.NotFound);
        if (sc.CompanyId != companyId)
            return SubProgressBillingResult<SubProgressBillingResponse>.Fail(
                "العقد الباطن لا ينتمي لشركتك.", SubProgressBillingErrorCode.ValidationError);

        // UNIQUE (sub_contract_id, billing_number) — check first.
        var existing = await _billings.ListBySubContractAsync(subContractId, ct);
        if (existing.Any(b => string.Equals(b.BillingNumber, req.BillingNumber.Trim(),
                StringComparison.OrdinalIgnoreCase)))
            return SubProgressBillingResult<SubProgressBillingResponse>.Fail(
                $"يوجد بالفعل مستخلص برقم ({req.BillingNumber}) على هذا العقد.",
                SubProgressBillingErrorCode.AlreadyExists);

        // ===== Algorithm =====
        // Count existing billings BEFORE this insert; retention kicks in while n <= retention_release_billing.
        var priorCount = existing.Count;
        var previousBillingsAmount = existing.Sum(b => b.GrossAmount);
        var gross = Math.Round(sc.ContractValue * (req.WorkCompletedPercent / 100m), 4);
        var retentionDeducted = (priorCount + 1) <= sc.RetentionReleaseBilling
            ? Math.Round(gross * (sc.RetentionPercent / 100m), 4)
            : 0m;
        var netPayable = gross - retentionDeducted;

        var now = DateTime.UtcNow;
        var billing = new SubProgressBilling
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            SubContractId = subContractId,
            BillingNumber = req.BillingNumber.Trim(),
            BillingDate = req.BillingDate,
            PeriodFrom = req.PeriodFrom,
            PeriodTo = req.PeriodTo,
            WorkCompletedPercent = req.WorkCompletedPercent,
            GrossAmount = gross,
            RetentionDeducted = retentionDeducted,
            PreviousBillingsAmount = previousBillingsAmount,
            NetPayable = netPayable,
            Status = (int)SubProgressBillingStatus.Draft,
            Notes = req.Notes?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        await _billings.InsertAsync(billing, ct);
        _logger.LogInformation(
            "SubProgressBilling created {Id} subContract={SubId} gross={Gross} retention={Ret} net={Net} by {UserId}",
            billing.Id, subContractId, gross, retentionDeducted, netPayable, userId);
        return SubProgressBillingResult<SubProgressBillingResponse>.Ok(MapToResponse(billing));
    }

    public async Task<SubProgressBillingResult<SubProgressBillingResponse>> UpdateAsync(
        Guid userId, Guid id, UpdateSubProgressBillingRequest req, CancellationToken ct)
    {
        var billing = await _billings.GetByIdAsync(id, ct);
        if (billing == null)
            return SubProgressBillingResult<SubProgressBillingResponse>.Fail(
                "المستخلص غير موجود.", SubProgressBillingErrorCode.NotFound);

        if (billing.Status != (int)SubProgressBillingStatus.Draft)
            return SubProgressBillingResult<SubProgressBillingResponse>.Fail(
                "لا يمكن تعديل مستخلص غير مسودة. ألغِ المستخلص وأنشئ واحداً جديداً.",
                SubProgressBillingErrorCode.ValidationError);

        if (req.WorkCompletedPercent < 0m || req.WorkCompletedPercent > 100m)
            return SubProgressBillingResult<SubProgressBillingResponse>.Fail(
                "نسبة الإنجاز يجب أن تكون بين 0 و 100.", SubProgressBillingErrorCode.ValidationError);

        var sc = await _subContracts.GetByIdAsync(billing.SubContractId, ct);
        if (sc == null)
            return SubProgressBillingResult<SubProgressBillingResponse>.Fail(
                "العقد الباطن الأصلي غير موجود.", SubProgressBillingErrorCode.NotFound);

        // Re-derive previousBillingsAmount (exclude self).
        var all = await _billings.ListBySubContractAsync(billing.SubContractId, ct);
        var previousBillingsAmount = all
            .Where(b => b.Id != billing.Id)
            .Sum(b => b.GrossAmount);

        // Re-derive ordinal (this billing's position, 1-based).
        var ordinal = all.Count(b => b.Id != billing.Id) + 1;
        var gross = Math.Round(sc.ContractValue * (req.WorkCompletedPercent / 100m), 4);
        var retentionDeducted = ordinal <= sc.RetentionReleaseBilling
            ? Math.Round(gross * (sc.RetentionPercent / 100m), 4)
            : 0m;
        var netPayable = gross - retentionDeducted;

        billing.PeriodFrom = req.PeriodFrom;
        billing.PeriodTo = req.PeriodTo;
        billing.WorkCompletedPercent = req.WorkCompletedPercent;
        billing.GrossAmount = gross;
        billing.RetentionDeducted = retentionDeducted;
        billing.PreviousBillingsAmount = previousBillingsAmount;
        billing.NetPayable = netPayable;
        billing.Notes = req.Notes?.Trim();
        billing.UpdatedAt = DateTime.UtcNow;
        await _billings.UpdateAsync(billing, ct);
        _logger.LogInformation("SubProgressBilling updated {Id} gross={Gross} by {UserId}",
            billing.Id, gross, userId);
        return SubProgressBillingResult<SubProgressBillingResponse>.Ok(MapToResponse(billing));
    }

    public async Task<SubProgressBillingResult<SubProgressBillingResponse>> GetByIdAsync(
        Guid id, CancellationToken ct)
    {
        var billing = await _billings.GetByIdAsync(id, ct);
        if (billing == null)
            return SubProgressBillingResult<SubProgressBillingResponse>.Fail(
                "المستخلص غير موجود.", SubProgressBillingErrorCode.NotFound);
        return SubProgressBillingResult<SubProgressBillingResponse>.Ok(MapToResponse(billing));
    }

    public async Task<SubProgressBillingResult<IReadOnlyList<SubProgressBillingResponse>>> ListBySubContractAsync(
        Guid subContractId, CancellationToken ct)
    {
        var rows = await _billings.ListBySubContractAsync(subContractId, ct);
        var list = rows.Select(MapToResponse).ToList();
        return SubProgressBillingResult<IReadOnlyList<SubProgressBillingResponse>>.Ok(list);
    }

    public async Task<SubProgressBillingResult<SubProgressBillingResponse>> ApproveAsync(
        Guid userId, Guid id, CancellationToken ct)
    {
        var billing = await _billings.GetByIdAsync(id, ct);
        if (billing == null)
            return SubProgressBillingResult<SubProgressBillingResponse>.Fail(
                "المستخلص غير موجود.", SubProgressBillingErrorCode.NotFound);

        if (billing.Status != (int)SubProgressBillingStatus.Draft)
            return SubProgressBillingResult<SubProgressBillingResponse>.Fail(
                "لا يمكن اعتماد مستخلص غير مسودة.",
                SubProgressBillingErrorCode.ValidationError);

        billing.Status = (int)SubProgressBillingStatus.Approved;
        billing.UpdatedAt = DateTime.UtcNow;
        await _billings.UpdateStatusAsync(billing.Id, billing.Status, billing.UpdatedAt, ct);
        _logger.LogInformation("SubProgressBilling approved {Id} by {UserId}", billing.Id, userId);
        return SubProgressBillingResult<SubProgressBillingResponse>.Ok(MapToResponse(billing));
    }

    // ===== Helpers =====

    private static SubProgressBillingResponse MapToResponse(SubProgressBilling b) => new(
        b.Id, b.CompanyId, b.SubContractId, b.BillingNumber,
        b.BillingDate, b.PeriodFrom, b.PeriodTo,
        b.WorkCompletedPercent, b.GrossAmount, b.RetentionDeducted,
        b.PreviousBillingsAmount, b.NetPayable,
        b.Status, StatusName(b.Status), b.Notes,
        b.CreatedAt, b.UpdatedAt);

    private static string StatusName(int status) => status switch
    {
        1 => "مسودة",
        2 => "معتمد",
        3 => "مدفوع",
        4 => "ملغى",
        _ => "غير معروف",
    };
}
