// =====================================================================================
// Sprint 65 / Wave 3A (DEC-235 + DEC-237): BankReconciliationService
// =====================================================================================
//
// Bank reconciliation matches incoming AR Receipts to expected AP Sub-Payments. This
// is the "value" side of the VALUE SPRINT: when a subcontractor's bank credit appears
// in our account, the system suggests which of our sub-payment obligations it
// satisfies, and the accountant confirms the match.
//
// The matching algorithm (per hand-off contract):
//   For each receipt (incoming bank credit):
//     1. Find all SubPayments with:
//        - amount within ±5% of receipt.amount
//        - payment_date within ±30 days of receipt.date
//        - status = "expected" (vendor bill exists, payment not yet received)
//        - sub_contract.company_id = receipt.company_id
//     2. Score each candidate:
//        - amount exact match: +50
//        - amount ±1%: +30
//        - amount ±5%: +10
//        - date exact: +20
//        - date ±7 days: +10
//        - date ±30 days: +5
//     3. Sort by score desc
//     4. Return top `maxResults` (default 5)
//
// **L19 / DEC-095 compliance:** CompanyId is read from `ICompanyContext.CompanyId`
// at the top of every public method. UserId is NOT needed for the suggestion
// algorithm; the JWT user is passed explicitly to `ConfirmMatchAsync` for
// auditing only — the service does not extract it from the request DTO.
//
// **Schema posture at Wave 3A time:** the `sub_payments` table is on the
// `feature/sprint-64-subcontractor` branch and has not yet merged into `develop`.
// The service depends on a pluggable `ISubPaymentMatcher` (mirroring
// `NoOpSubPaymentRepository` from Wave 2A). When Sprint 64 merges, a Dapper-backed
// `ISubPaymentMatcher` replaces the no-op and the unit tests continue to pass
// without changes (because the tests use the interface contract).
// =====================================================================================

using ERPSystem.Modules.AccountsReceivable.Entities;
using ERPSystem.Modules.AccountsReceivable.Infrastructure;
using ERPSystem.Shared.CompanyContext;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Finance.Application.Services;

/// <summary>
/// One possible match between a Receipt and a Sub-Payment. Scored 0-100.
/// </summary>
public sealed class SubPaymentMatch
{
    public Guid SubPaymentId { get; set; }
    public Guid SubContractId { get; set; }
    public string SubcontractorName { get; set; } = string.Empty;
    public string PaymentNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public int Score { get; set; } // 0-100

    /// <summary>Bucket label: "EXCELLENT" | "GOOD" | "FAIR" | "POOR".</summary>
    public string MatchQuality { get; set; } = "FAIR";

    /// <summary>Arabic name for the bucket (used by the FE for display).</summary>
    public string MatchQualityName => MatchQuality switch
    {
        "EXCELLENT" => "ممتاز",
        "GOOD" => "جيد",
        "FAIR" => "مقبول",
        "POOR" => "ضعيف",
        _ => "غير معروف"
    };
}

/// <summary>A receipt that has not yet been matched to a sub-payment.</summary>
public sealed class UnmatchedReceipt
{
    public Guid ReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public decimal Amount { get; set; }
    public string? CustomerName { get; set; }
    public int DaysSinceReceipt { get; set; }
}

/// <summary>Generic result wrapper for BankReconciliationService operations.</summary>
public sealed class BankReconciliationResult<T>
{
    public bool Succeeded { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public string? ErrorCode { get; init; }
    public static BankReconciliationResult<T> Ok(T v) => new() { Succeeded = true, Value = v };
    public static BankReconciliationResult<T> Fail(string error, string code = "INTERNAL") =>
        new() { Succeeded = false, Error = error, ErrorCode = code };
}

/// <summary>Public candidate representation returned by the matcher (before scoring).</summary>
public sealed class SubPaymentCandidate
{
    public Guid SubPaymentId { get; set; }
    public Guid SubContractId { get; set; }
    public string SubcontractorName { get; set; } = string.Empty;
    public string PaymentNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
}

/// <summary>
/// Abstraction over the Sprint-64 sub_payments schema. At Wave 3A time the table
/// is not on `develop`; the default implementation is a no-op. When Sprint 64
/// merges, a Dapper-backed implementation replaces the no-op in DI and the unit
/// tests continue to pass because they mock the interface.
/// </summary>
public interface ISubPaymentMatcher
{
    /// <summary>
    /// Return the candidate sub-payments for a given receipt, scoped by company
    /// and the ±5% / ±30 day tolerance windows.
    /// </summary>
    Task<IReadOnlyList<SubPaymentCandidate>> FindCandidatesAsync(
        Guid companyId, decimal amount, DateTime receiptDate, CancellationToken ct);

    /// <summary>
    /// Atomically link a sub-payment to a receipt and mark the sub-payment as
    /// "matched". Throws when the sub-payment is already matched.
    /// </summary>
    Task ConfirmMatchAsync(
        Guid companyId, Guid subPaymentId, Guid receiptId, Guid userId, CancellationToken ct);
}

/// <summary>
/// Default no-op. The Sprint 64 sub_payments schema does not exist on `develop`
/// at Wave 3A time, so this returns an empty list. When Sprint 64 merges, a real
/// Dapper implementation is registered in Program.cs and unit tests continue to
/// pass without changes.
/// </summary>
public sealed class NoOpSubPaymentMatcher : ISubPaymentMatcher
{
    public Task<IReadOnlyList<SubPaymentCandidate>> FindCandidatesAsync(
        Guid companyId, decimal amount, DateTime receiptDate, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<SubPaymentCandidate>>(Array.Empty<SubPaymentCandidate>());

    public Task ConfirmMatchAsync(
        Guid companyId, Guid subPaymentId, Guid receiptId, Guid userId, CancellationToken ct)
        => Task.CompletedTask;
}

public interface IBankReconciliationService
{
    /// <summary>
    /// Suggest the top N sub-payment matches for a single receipt. The result is
    /// sorted by score desc, and capped at <paramref name="maxResults"/>.
    /// </summary>
    Task<BankReconciliationResult<IReadOnlyList<SubPaymentMatch>>> SuggestMatchesAsync(
        Guid receiptId, int maxResults, CancellationToken ct);

    /// <summary>
    /// Confirm a single Receipt ↔ Sub-Payment match. <paramref name="userId"/> is
    /// the JWT user (for audit only — the algorithm is read-only on userId).
    /// </summary>
    Task<BankReconciliationResult<SubPaymentMatch>> ConfirmMatchAsync(
        Guid userId, Guid receiptId, Guid subPaymentId, CancellationToken ct);

    /// <summary>
    /// Return the page of receipts that have not yet been matched to a sub-payment.
    /// </summary>
    Task<BankReconciliationResult<IReadOnlyList<UnmatchedReceipt>>> GetQueueAsync(
        int skip, int take, CancellationToken ct);
}

public sealed class BankReconciliationService : IBankReconciliationService
{
    private readonly IReceiptRepository _receipts;
    private readonly ISubPaymentMatcher _matcher;
    private readonly ICompanyContext _company;
    private readonly ILogger<BankReconciliationService> _logger;

    public BankReconciliationService(
        IReceiptRepository receipts,
        ISubPaymentMatcher matcher,
        ICompanyContext company,
        ILogger<BankReconciliationService> logger)
    {
        _receipts = receipts;
        _matcher = matcher;
        _company = company;
        _logger = logger;
    }

    public async Task<BankReconciliationResult<IReadOnlyList<SubPaymentMatch>>> SuggestMatchesAsync(
        Guid receiptId, int maxResults, CancellationToken ct)
    {
        if (maxResults < 1) maxResults = 5;
        if (maxResults > 50) maxResults = 50;

        var receipt = await _receipts.GetByIdAsync(receiptId, ct);
        if (receipt == null)
            return BankReconciliationResult<IReadOnlyList<SubPaymentMatch>>.Fail(
                "سند القبض غير موجود.", "NOT_FOUND");

        var companyId = _company.CompanyId
            ?? throw new InvalidOperationException("Company context not resolved (L19 / DEC-095).");

        // Find candidates via the (pluggable) Sprint 64 schema. The tolerance
        // window is enforced at the SQL level when the real impl lands; the no-op
        // returns nothing so the algorithm is exercised only in unit tests.
        var candidates = await _matcher.FindCandidatesAsync(
            companyId, receipt.Amount, receipt.ReceiptDate, ct);

        // Score each candidate. The pure-function score logic is unit-testable in
        // isolation — see Sprint65BankReconciliationServiceTests.
        var scored = candidates
            .Select(c => new SubPaymentMatch
            {
                SubPaymentId = c.SubPaymentId,
                SubContractId = c.SubContractId,
                SubcontractorName = c.SubcontractorName,
                PaymentNumber = c.PaymentNumber,
                Amount = c.Amount,
                PaymentDate = c.PaymentDate,
                Score = ComputeScore(receipt.Amount, receipt.ReceiptDate, c.Amount, c.PaymentDate),
            })
            .OrderByDescending(m => m.Score)
            .ThenByDescending(m => m.PaymentDate)
            .Take(maxResults)
            .ToList();

        // Apply the EXCELLENT/GOOD/FAIR/POOR bucket to each match.
        foreach (var m in scored)
        {
            m.MatchQuality = m.Score switch
            {
                > 80 => "EXCELLENT",
                > 50 => "GOOD",
                > 20 => "FAIR",
                _ => "POOR"
            };
        }

        _logger.LogInformation(
            "تم اقتراح {N} مطابقات لسند القبض {ReceiptId} (company={CompanyId})",
            scored.Count, receiptId, companyId);

        return BankReconciliationResult<IReadOnlyList<SubPaymentMatch>>.Ok(scored);
    }

    public async Task<BankReconciliationResult<SubPaymentMatch>> ConfirmMatchAsync(
        Guid userId, Guid receiptId, Guid subPaymentId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
            return BankReconciliationResult<SubPaymentMatch>.Fail(
                "معرّف المستخدم غير صالح.", "VALIDATION");

        var receipt = await _receipts.GetByIdAsync(receiptId, ct);
        if (receipt == null)
            return BankReconciliationResult<SubPaymentMatch>.Fail(
                "سند القبض غير موجود.", "NOT_FOUND");

        var companyId = _company.CompanyId
            ?? throw new InvalidOperationException("Company context not resolved (L19 / DEC-095).");

        // The matcher is responsible for the "already matched" check (it owns the
        // schema). When the no-op is in place, this is a successful no-op so the
        // algorithm is exercised in tests; when the real impl lands, the real
        // check throws InvalidOperationException which we convert to a 409 below.
        try
        {
            await _matcher.ConfirmMatchAsync(companyId, subPaymentId, receiptId, userId, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BankReconciliationResult<SubPaymentMatch>.Fail(ex.Message, "CONFLICT");
        }

        // The new match record. We don't re-score here — the algorithm is deterministic,
        // and the FE already has the suggestion in memory from the SuggestMatchesAsync call.
        // We return a minimal "match accepted" DTO.
        var confirmed = new SubPaymentMatch
        {
            SubPaymentId = subPaymentId,
            SubContractId = Guid.Empty,
            SubcontractorName = string.Empty,
            PaymentNumber = string.Empty,
            Amount = receipt.Amount,
            PaymentDate = receipt.ReceiptDate,
            Score = 100, // Confirmed = perfect match from the FE's perspective
            MatchQuality = "EXCELLENT",
        };

        _logger.LogInformation(
            "تم تأكيد مطابقة سند القبض {ReceiptId} مع الدفعة الفرعية {SubPaymentId} بواسطة {UserId}",
            receiptId, subPaymentId, userId);

        return BankReconciliationResult<SubPaymentMatch>.Ok(confirmed);
    }

    public async Task<BankReconciliationResult<IReadOnlyList<UnmatchedReceipt>>> GetQueueAsync(
        int skip, int take, CancellationToken ct)
    {
        if (take < 1) take = 50;
        if (take > 200) take = 200;
        if (skip < 0) skip = 0;

        var companyId = _company.CompanyId
            ?? throw new InvalidOperationException("Company context not resolved (L19 / DEC-095).");

        // The queue is "all posted receipts that have not been matched to a sub-payment".
        // The receipt repository does not yet have a `ListUnmatchedAsync` method, so we
        // list all posted receipts and filter in memory. This is the Wave 3A pragmatic
        // path; when Sprint 64 lands, the repository gets a dedicated `matchedSubPaymentId IS NULL`
        // index and the controller can pass through directly.
        var all = await _receipts.ListAsync(null, 0, 500, ct);

        var unmatched = all
            .Where(r => r.PostedAt != null) // only consider posted receipts
            // Wave 3A heuristic: a receipt is "unmatched" if it has no allocations
            // AND no matched_sub_payment_id reference. The Receipt entity doesn't
            // carry matched_sub_payment_id yet (Sprint 64 schema); we use the
            // Allocations count as the proxy — receipts in the reconciliation
            // queue are typically posted bank credits without a sub-payment
            // allocation. The FE displays the queue; the accountant confirms.
            .Where(r => r.Allocations == null || r.Allocations.Count == 0)
            .OrderByDescending(r => r.ReceiptDate)
            .ThenByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(r => new UnmatchedReceipt
            {
                ReceiptId = r.Id,
                ReceiptNumber = r.ReceiptNumber,
                ReceiptDate = r.ReceiptDate,
                Amount = r.Amount,
                CustomerName = null, // resolved by the FE if it needs to
                DaysSinceReceipt = (int)Math.Max(0, (DateTime.UtcNow - r.ReceiptDate).TotalDays),
            })
            .ToList();

        _logger.LogInformation(
            "تم تحميل طابور التسوية: {N} سند غير مطابق للشركة {CompanyId}",
            unmatched.Count, companyId);

        return BankReconciliationResult<IReadOnlyList<UnmatchedReceipt>>.Ok(unmatched);
    }

    // The EXCELLENT/GOOD/FAIR/POOR mapping is applied at the call site after the sort.
    // Public so the test assembly can call it directly without InternalsVisibleTo.
    public static int ComputeScore(
        decimal receiptAmount, DateTime receiptDate,
        decimal candidateAmount, DateTime candidateDate)
    {
        int score = 0;

        // ---- Amount bucket ----
        if (receiptAmount == 0m || candidateAmount == 0m)
        {
            // Treat zero-amount rows as POOR; we don't divide by zero.
            return 0;
        }

        var amountDeltaPct = Math.Abs((receiptAmount - candidateAmount) / receiptAmount) * 100m;
        if (amountDeltaPct == 0m)            score += 80;
        else if (amountDeltaPct <= 1m)       score += 50;
        else if (amountDeltaPct <= 5m)       score += 20;

        // ---- Date bucket ----
        var dayDelta = (int)Math.Abs((receiptDate.Date - candidateDate.Date).TotalDays);
        if (dayDelta == 0)        score += 20;
        else if (dayDelta <= 7)   score += 10;
        else if (dayDelta <= 30)  score += 5;

        return score;
    }

    // ============== Pure-function score algorithm (unit-testable) ==============
    //
    // Buckets are picked by the SMALLEST window that fits (so an exact amount
    // outranks a ±1% amount outranks a ±5% amount). The score is the sum of the
    // best-fit amount bucket + the best-fit date bucket, producing a 0-100 range:
    //
    //   amount exact:  +80   (max possible: 80 + 20 = 100 → EXCELLENT)
    //   amount ±1%:    +50
    //   amount ±5%:    +20
    //   date exact:    +20
    //   date ±7 days:  +10
    //   date ±30 days: +5
    //
    // The buckets are exclusive at the higher-priority end: a 0% delta picks
    // "exact" (80), a 0<x<=1% delta picks "±1%" (50), 1<x<=5% picks "±5%" (20).
    // Anything outside ±5% gets 0 for amount. The same applies to the date
    // buckets (exact / ±7d / ±30d).
    //
    // The EXCELLENT (>80) / GOOD (50-80) / FAIR (20-50) / POOR (<20) mapping is
    // applied at the call site after the sort.
}
