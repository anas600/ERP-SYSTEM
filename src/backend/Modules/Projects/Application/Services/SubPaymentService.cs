using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Modules.Projects.Infrastructure;
using ERPSystem.Modules.Projects.Application.Events;
using ERPSystem.Shared.CompanyContext;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Projects.Application.Services;

// =============================================================================
// Sprint 64 / DEC-224 — Sub-Payment service.
//
// Manages sub_payments. CompanyId always comes from the JWT context
// (L19 / DEC-095), never from the request DTO. The service computes the
// outstanding balance per sub-contract via SUM queries on billings + payments.
// =============================================================================

/// <summary>Result envelope for SubPayment service calls.</summary>
public sealed class SubPaymentResult<T>
{
    public bool Succeeded { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public SubPaymentErrorCode? ErrorCode { get; init; }
    public static SubPaymentResult<T> Ok(T v) => new() { Succeeded = true, Value = v };
    public static SubPaymentResult<T> Fail(string e, SubPaymentErrorCode c) =>
        new() { Succeeded = false, Error = e, ErrorCode = c };
}

public enum SubPaymentErrorCode
{
    NotFound, AlreadyExists, ValidationError, Internal
}

public interface ISubPaymentService
{
    Task<SubPaymentResult<SubPaymentResponse>> CreateAsync(
        Guid userId, Guid subContractId, Guid subProgressBillingId,
        CreateSubPaymentRequest req, CancellationToken ct);

    Task<SubPaymentResult<SubPaymentResponse>> GetByIdAsync(
        Guid id, CancellationToken ct);

    Task<SubPaymentResult<IReadOnlyList<SubPaymentResponse>>> ListBySubContractAsync(
        Guid subContractId, CancellationToken ct);

    Task<SubPaymentResult<SubContractBalanceResponse>> GetBalanceAsync(
        Guid subContractId, CancellationToken ct);

    Task<SubPaymentResult<SubPaymentResponse>> ReleaseRetentionAsync(
        Guid userId, Guid subContractId, ReleaseRetentionRequest req, CancellationToken ct);
}

/// <summary>
/// Sprint 64 / DEC-224 — Sub-Payment service.
///
/// <para><b>Algorithm (GetBalanceAsync)</b>:</para>
/// <code>
/// totalBilledGross       = SUM(sub_progress_billings.gross_amount       WHERE status != 4)
/// totalRetentionWithheld = SUM(sub_progress_billings.retention_deducted WHERE status != 4)
/// totalPaid              = SUM(sub_payments.amount + sub_payments.retention_released)
/// outstandingBalance     = totalBilledGross - totalPaid
/// </code>
///
/// <para><b>Algorithm (ReleaseRetentionAsync)</b>:</para>
/// <list type="number">
///   <item>Validate req.Amount &gt; 0.</item>
///   <item>totalWithheld   = SUM(retention_deducted) on sub_progress_billings (status != 4).</item>
///   <item>totalReleased   = SUM(retention_released) on sub_payments.</item>
///   <item>availableForRelease = totalWithheld - totalReleased.</item>
///   <item>If req.Amount &gt; availableForRelease → 400 ValidationError.</item>
///   <item>Find the first approved billing (lowest id) to link the release payment to.</item>
///   <item>Create a new SubPayment with <c>retention_released = req.Amount</c>, <c>amount = 0</c>.</item>
/// </list>
///
/// <para><b>L19 / DEC-095</b>: CompanyId comes from <see cref="ICompanyContext"/>,
/// never from the request DTO.</para>
/// </summary>
public sealed class SubPaymentService : ISubPaymentService
{
    private readonly ISubPaymentRepository _payments;
    private readonly ISubProgressBillingRepository _billings;
    private readonly ISubContractRepository _subContracts;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<SubPaymentService> _logger;

    public SubPaymentService(
        ISubPaymentRepository payments,
        ISubProgressBillingRepository billings,
        ISubContractRepository subContracts,
        ICompanyContext companyContext,
        ILogger<SubPaymentService> logger)
    {
        _payments = payments;
        _billings = billings;
        _subContracts = subContracts;
        _companyContext = companyContext;
        _logger = logger;
    }

    public async Task<SubPaymentResult<SubPaymentResponse>> CreateAsync(
        Guid userId, Guid subContractId, Guid subProgressBillingId,
        CreateSubPaymentRequest req, CancellationToken ct)
    {
        // L19 / DEC-095: CompanyId from JWT, not from request.
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved");

        // Validate basic fields.
        if (string.IsNullOrWhiteSpace(req.PaymentNumber))
            return SubPaymentResult<SubPaymentResponse>.Fail(
                "رقم الدفعة مطلوب.", SubPaymentErrorCode.ValidationError);
        if (req.Amount <= 0m)
            return SubPaymentResult<SubPaymentResponse>.Fail(
                "قيمة الدفعة يجب أن تكون أكبر من صفر.", SubPaymentErrorCode.ValidationError);

        // SubContract must exist + same company.
        var sc = await _subContracts.GetByIdAsync(subContractId, ct);
        if (sc == null)
            return SubPaymentResult<SubPaymentResponse>.Fail(
                "العقد الباطن غير موجود.", SubPaymentErrorCode.NotFound);
        if (sc.CompanyId != companyId)
            return SubPaymentResult<SubPaymentResponse>.Fail(
                "العقد الباطن لا ينتمي لشركتك.", SubPaymentErrorCode.ValidationError);

        // Billing must exist, belong to the same sub-contract + same company.
        var billing = await _billings.GetByIdAsync(subProgressBillingId, ct);
        if (billing == null)
            return SubPaymentResult<SubPaymentResponse>.Fail(
                "المستخلص غير موجود.", SubPaymentErrorCode.NotFound);
        if (billing.SubContractId != subContractId)
            return SubPaymentResult<SubPaymentResponse>.Fail(
                "المستخلص لا ينتمي لهذا العقد الباطن.", SubPaymentErrorCode.ValidationError);
        if (billing.CompanyId != companyId)
            return SubPaymentResult<SubPaymentResponse>.Fail(
                "المستخلص لا ينتمي لشركتك.", SubPaymentErrorCode.ValidationError);
        if (billing.Status != (int)SubProgressBillingStatus.Approved)
            return SubPaymentResult<SubPaymentResponse>.Fail(
                "لا يمكن الدفع على مستخلص غير معتمد. اعتمد المستخلص أولاً.",
                SubPaymentErrorCode.ValidationError);

        // UNIQUE (sub_contract_id, payment_number) — check.
        var existing = await _payments.ListBySubContractAsync(subContractId, ct);
        if (existing.Any(p => string.Equals(p.PaymentNumber, req.PaymentNumber.Trim(),
                StringComparison.OrdinalIgnoreCase)))
            return SubPaymentResult<SubPaymentResponse>.Fail(
                $"يوجد بالفعل دفعة برقم ({req.PaymentNumber}) على هذا العقد.",
                SubPaymentErrorCode.AlreadyExists);

        var payment = new SubPayment
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            SubContractId = subContractId,
            SubProgressBillingId = subProgressBillingId,
            PaymentNumber = req.PaymentNumber.Trim(),
            PaymentDate = req.PaymentDate,
            Amount = req.Amount,
            RetentionReleased = 0m,
            PaymentMethod = req.PaymentMethod?.Trim(),
            ReferenceNumber = req.ReferenceNumber?.Trim(),
            Notes = req.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
        await _payments.InsertAsync(payment, ct);
        _logger.LogInformation(
            "SubPayment created {Id} subContract={SubId} billing={BillingId} amount={Amount} by {UserId}",
            payment.Id, subContractId, subProgressBillingId, req.Amount, userId);
        return SubPaymentResult<SubPaymentResponse>.Ok(MapToResponse(payment));
    }

    public async Task<SubPaymentResult<SubPaymentResponse>> GetByIdAsync(
        Guid id, CancellationToken ct)
    {
        var p = await _payments.GetByIdAsync(id, ct);
        if (p == null)
            return SubPaymentResult<SubPaymentResponse>.Fail(
                "الدفعة غير موجودة.", SubPaymentErrorCode.NotFound);
        return SubPaymentResult<SubPaymentResponse>.Ok(MapToResponse(p));
    }

    public async Task<SubPaymentResult<IReadOnlyList<SubPaymentResponse>>> ListBySubContractAsync(
        Guid subContractId, CancellationToken ct)
    {
        var rows = await _payments.ListBySubContractAsync(subContractId, ct);
        var list = rows.Select(MapToResponse).ToList();
        return SubPaymentResult<IReadOnlyList<SubPaymentResponse>>.Ok(list);
    }

    public async Task<SubPaymentResult<SubContractBalanceResponse>> GetBalanceAsync(
        Guid subContractId, CancellationToken ct)
    {
        var sc = await _subContracts.GetByIdAsync(subContractId, ct);
        if (sc == null)
            return SubPaymentResult<SubContractBalanceResponse>.Fail(
                "العقد الباطن غير موجود.", SubPaymentErrorCode.NotFound);

        var totalBilledGross = await _billings
            .SumGrossNonCancelledBySubContractAsync(subContractId, ct);
        var totalRetentionWithheld = await _billings
            .SumRetentionNonCancelledBySubContractAsync(subContractId, ct);
        var totalPaid = await _payments
            .SumPaidBySubContractAsync(subContractId, ct);

        // Outstanding = totalBilledGross - totalPaid.
        // (The withheld retention is part of totalBilledGross but not yet paid —
        //  it stays in outstanding until released.)
        var outstanding = totalBilledGross - totalPaid;

        var balance = new SubContractBalanceResponse(
            SubContractId: sc.Id,
            ContractNumber: sc.ContractNumber,
            ContractValue: sc.ContractValue,
            TotalBilledGross: totalBilledGross,
            TotalRetentionWithheld: totalRetentionWithheld,
            TotalPaid: totalPaid,
            OutstandingBalance: outstanding);
        return SubPaymentResult<SubContractBalanceResponse>.Ok(balance);
    }

    public async Task<SubPaymentResult<SubPaymentResponse>> ReleaseRetentionAsync(
        Guid userId, Guid subContractId, ReleaseRetentionRequest req, CancellationToken ct)
    {
        // L19 / DEC-095: CompanyId from JWT, not from request.
        var companyId = _companyContext.CompanyId
            ?? throw new InvalidOperationException("Company not resolved");

        if (req.Amount <= 0m)
            return SubPaymentResult<SubPaymentResponse>.Fail(
                "قيمة التحرير يجب أن تكون أكبر من صفر.", SubPaymentErrorCode.ValidationError);

        var sc = await _subContracts.GetByIdAsync(subContractId, ct);
        if (sc == null)
            return SubPaymentResult<SubPaymentResponse>.Fail(
                "العقد الباطن غير موجود.", SubPaymentErrorCode.NotFound);
        if (sc.CompanyId != companyId)
            return SubPaymentResult<SubPaymentResponse>.Fail(
                "العقد الباطن لا ينتمي لشركتك.", SubPaymentErrorCode.ValidationError);

        var totalWithheld = await _billings
            .SumRetentionNonCancelledBySubContractAsync(subContractId, ct);
        var totalReleased = await _payments
            .SumRetentionReleasedBySubContractAsync(subContractId, ct);
        var availableForRelease = totalWithheld - totalReleased;
        if (req.Amount > availableForRelease)
            return SubPaymentResult<SubPaymentResponse>.Fail(
                $"قيمة التحرير ({req.Amount:N4}) تتجاوز المتاح ({availableForRelease:N4}).",
                SubPaymentErrorCode.ValidationError);

        // Find the first approved (or paid) billing to link this retention-release payment to.
        var billings = await _billings.ListBySubContractAsync(subContractId, ct);
        var firstApproved = billings
            .Where(b => b.Status == (int)SubProgressBillingStatus.Approved
                     || b.Status == (int)SubProgressBillingStatus.Paid)
            .OrderBy(b => b.BillingDate)
            .ThenBy(b => b.BillingNumber)
            .FirstOrDefault();
        if (firstApproved == null)
            return SubPaymentResult<SubPaymentResponse>.Fail(
                "لا يوجد مستخلص معتمد لربط الدفعة به. اعتمد مستخلصاً أولاً.",
                SubPaymentErrorCode.ValidationError);

        // Generate a unique payment number for the release (REL-NNN).
        var existing = await _payments.ListBySubContractAsync(subContractId, ct);
        var paymentNumber = $"REL-{existing.Count + 1:D3}";

        var payment = new SubPayment
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            SubContractId = subContractId,
            SubProgressBillingId = firstApproved.Id,
            PaymentNumber = paymentNumber,
            PaymentDate = req.ReleaseDate,
            Amount = 0m,
            RetentionReleased = req.Amount,
            PaymentMethod = "retention_release",
            ReferenceNumber = null,
            Notes = req.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
        await _payments.InsertAsync(payment, ct);
        _logger.LogInformation(
            "Retention released {Id} subContract={SubId} amount={Amount} available_before={Avail} by {UserId}",
            payment.Id, subContractId, req.Amount, availableForRelease, userId);
        return SubPaymentResult<SubPaymentResponse>.Ok(MapToResponse(payment));
    }

    // ===== Helpers =====

    private static SubPaymentResponse MapToResponse(SubPayment p) => new(
        p.Id, p.CompanyId, p.SubContractId, p.SubProgressBillingId,
        p.PaymentNumber, p.PaymentDate, p.Amount, p.RetentionReleased,
        p.PaymentMethod, p.ReferenceNumber, p.Notes, p.CreatedAt);
}
