using Dapper;
using ERPSystem.Modules.AccountsReceivable.Entities;
using ERPSystem.Modules.AccountsReceivable.Infrastructure;
using ERPSystem.Modules.Finance.Application.Services;
using ERPSystem.Shared.CompanyContext;
using ERPSystem.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ERPSystem.Tests.Finance;

// =====================================================================================
// Sprint 65 / Wave 3A (DEC-235 + DEC-237): Tests for BankReconciliationService.
// =====================================================================================
//
// The 6 tests cover:
//   1. SuggestMatchesAsync_FindsExactMatch_ReturnsScore100     — exact amount + same date → 100
//   2. SuggestMatchesAsync_FindsWithin5Percent_ReturnsWithDiscountedScore
//   3. SuggestMatchesAsync_NoMatches_ReturnsEmpty
//   4. SuggestMatchesAsync_OrdersByScoreDesc
//   5. ConfirmMatchAsync_ValidPair_UpdatesBoth
//   6. ConfirmMatchAsync_DuplicateConfirm_ThrowsConflict
//
// L19 / DEC-095: companyId comes from ICompanyContext (not from any DTO).
// =====================================================================================

public class Sprint65BankReconciliationServiceTests
{
    // ===================== Test helpers =====================

    internal static class TestCompanyContextFactory
    {
        public static ICompanyContext Create(Guid companyId)
        {
            var m = new Mock<ICompanyContext>();
            m.Setup(c => c.CompanyId).Returns(companyId);
            return m.Object;
        }
    }

    /// <summary>
    /// Fake <see cref="IReceiptRepository"/> backed by an in-memory dictionary.
    /// Only the methods used by the service are implemented; the rest throw.
    /// </summary>
    private sealed class FakeReceiptRepository : IReceiptRepository
    {
        public Dictionary<Guid, Receipt> ById { get; } = new();
        public List<Receipt> ListedReceipts { get; set; } = new();

        public Task<Receipt?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(ById.TryGetValue(id, out var r) ? r : null);

        public Task<Receipt?> GetByReceiptNumberAsync(string receiptNumber, CancellationToken ct) =>
            Task.FromResult(ById.Values.FirstOrDefault(r => r.ReceiptNumber == receiptNumber));

        public Task<IReadOnlyList<Receipt>> ListAsync(Guid? customerId, int skip, int take, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Receipt>>(ListedReceipts.ToList());

        public Task InsertAsync(Receipt r, CancellationToken ct) { ById[r.Id] = r; return Task.CompletedTask; }
        public Task UpdateAsync(Receipt r, CancellationToken ct) { ById[r.Id] = r; return Task.CompletedTask; }
        public Task InsertAllocationsAsync(Guid receiptId, IEnumerable<ReceiptAllocation> allocations, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<ReceiptAllocation>> GetAllocationsAsync(Guid receiptId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ReceiptAllocation>>(new List<ReceiptAllocation>());
    }

    /// <summary>
    /// Fake <see cref="ISubPaymentMatcher"/> with a scripted candidate list and a
    /// flag-throwing "duplicate confirm" path. The unit tests drive the algorithm
    /// via this seam, so the tests do not need the real Sprint 64 schema.
    /// </summary>
    private sealed class FakeSubPaymentMatcher : ISubPaymentMatcher
    {
        public Func<Guid, decimal, DateTime, IReadOnlyList<SubPaymentCandidate>> ScriptedCandidates { get; set; }
            = (_, _, _) => Array.Empty<SubPaymentCandidate>();

        public List<(Guid CompanyId, Guid SubPaymentId, Guid ReceiptId, Guid UserId)> Confirmed { get; } = new();
        public bool ThrowOnConfirm { get; set; } = false;

        public Task<IReadOnlyList<SubPaymentCandidate>> FindCandidatesAsync(
            Guid companyId, decimal amount, DateTime receiptDate, CancellationToken ct) =>
            Task.FromResult(ScriptedCandidates(companyId, amount, receiptDate));

        public Task ConfirmMatchAsync(
            Guid companyId, Guid subPaymentId, Guid receiptId, Guid userId, CancellationToken ct)
        {
            if (ThrowOnConfirm)
                throw new InvalidOperationException("SubPayment already matched.");
            Confirmed.Add((companyId, subPaymentId, receiptId, userId));
            return Task.CompletedTask;
        }
    }

    private static (BankReconciliationService svc, FakeReceiptRepository receipts, FakeSubPaymentMatcher matcher, Guid companyId)
        Build(Guid? companyIdOverride = null)
    {
        var receipts = new FakeReceiptRepository();
        var matcher = new FakeSubPaymentMatcher();
        var companyId = companyIdOverride ?? Guid.NewGuid();
        var ctx = TestCompanyContextFactory.Create(companyId);
        var svc = new BankReconciliationService(
            receipts, matcher, ctx, NullLogger<BankReconciliationService>.Instance);
        return (svc, receipts, matcher, companyId);
    }

    private static Receipt SeedReceipt(FakeReceiptRepository repo, Guid receiptId, Guid companyId, decimal amount, DateTime date)
    {
        var r = new Receipt
        {
            Id = receiptId,
            CompanyId = companyId,
            CustomerId = Guid.NewGuid(),
            ReceiptNumber = $"RC-TEST-{receiptId.ToString().Substring(0, 8)}",
            ReceiptDate = date,
            Amount = amount,
            CurrencyCode = "LYD",
            CreatedAt = date,
            CreatedBy = Guid.NewGuid(),
            UpdatedAt = date,
            PostedAt = date, // posted so it shows up in the queue
        };
        repo.ById[receiptId] = r;
        return r;
    }

    // ============== Test 1 — exact match ==============

    [Fact]
    public async Task SuggestMatchesAsync_FindsExactMatch_ReturnsScore100()
    {
        var receiptId = Guid.NewGuid();
        var (svc, receipts, matcher, companyId) = Build();
        SeedReceipt(receipts, receiptId, companyId, 10_000m, new DateTime(2026, 8, 1));
        matcher.ScriptedCandidates = (_, amount, date) => new[]
        {
            new SubPaymentCandidate
            {
                SubPaymentId = Guid.NewGuid(),
                SubContractId = Guid.NewGuid(),
                SubcontractorName = "مقاول الفجر",
                PaymentNumber = "SP-001",
                Amount = amount,        // exact amount
                PaymentDate = date,     // exact date
            }
        };

        var r = await svc.SuggestMatchesAsync(receiptId, maxResults: 5, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.Should().HaveCount(1);
        r.Value![0].Score.Should().Be(100, "exact amount (+80) + exact date (+20) = 100");
        r.Value![0].MatchQuality.Should().Be("EXCELLENT");
    }

    // ============== Test 2 — within 5% ==============

    [Fact]
    public async Task SuggestMatchesAsync_FindsWithin5Percent_ReturnsWithDiscountedScore()
    {
        var receiptId = Guid.NewGuid();
        var (svc, receipts, matcher, companyId) = Build();
        // Receipt: 10000 LYD, 2026-08-15
        SeedReceipt(receipts, receiptId, companyId, 10_000m, new DateTime(2026, 8, 15));
        // Candidate: 9800 LYD (-2%, within 5%), 2026-08-20 (+5 days, within 7 days)
        matcher.ScriptedCandidates = (_, _, _) => new[]
        {
            new SubPaymentCandidate
            {
                SubPaymentId = Guid.NewGuid(),
                SubContractId = Guid.NewGuid(),
                SubcontractorName = "مقاول آخر",
                PaymentNumber = "SP-002",
                Amount = 9_800m,
                PaymentDate = new DateTime(2026, 8, 20),
            }
        };

        var r = await svc.SuggestMatchesAsync(receiptId, maxResults: 5, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.Should().HaveCount(1);
        // -2% amount → ±5% bucket → +20
        // +5 days date → ±7 days bucket → +10
        // Total = 30 → FAIR
        r.Value![0].Score.Should().Be(30);
        r.Value![0].MatchQuality.Should().Be("FAIR");
    }

    // ============== Test 3 — no matches ==============

    [Fact]
    public async Task SuggestMatchesAsync_NoMatches_ReturnsEmpty()
    {
        var receiptId = Guid.NewGuid();
        var (svc, receipts, matcher, companyId) = Build();
        SeedReceipt(receipts, receiptId, companyId, 5_000m, DateTime.UtcNow);
        // matcher returns empty list (Sprint 64 pre-merge default)
        matcher.ScriptedCandidates = (_, _, _) => Array.Empty<SubPaymentCandidate>();

        var r = await svc.SuggestMatchesAsync(receiptId, maxResults: 5, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.Should().BeEmpty("the matcher returned no candidates (Sprint 64 pre-merge)");
    }

    // ============== Test 4 — ordering by score ==============

    [Fact]
    public async Task SuggestMatchesAsync_OrdersByScoreDesc()
    {
        var receiptId = Guid.NewGuid();
        var (svc, receipts, matcher, companyId) = Build();
        var refDate = new DateTime(2026, 8, 1);
        SeedReceipt(receipts, receiptId, companyId, 10_000m, refDate);
        matcher.ScriptedCandidates = (_, _, _) => new[]
        {
            new SubPaymentCandidate
            {
                SubPaymentId = Guid.NewGuid(), SubContractId = Guid.NewGuid(),
                SubcontractorName = "Weak match", PaymentNumber = "SP-WEAK",
                Amount = 7_000m, PaymentDate = refDate.AddDays(20), // -30% (out of ±5%) + 20d
            },
            new SubPaymentCandidate
            {
                SubPaymentId = Guid.NewGuid(), SubContractId = Guid.NewGuid(),
                SubcontractorName = "Perfect match", PaymentNumber = "SP-PERFECT",
                Amount = 10_000m, PaymentDate = refDate, // exact + exact
            },
            new SubPaymentCandidate
            {
                SubPaymentId = Guid.NewGuid(), SubContractId = Guid.NewGuid(),
                SubcontractorName = "Decent match", PaymentNumber = "SP-DECENT",
                Amount = 9_900m, PaymentDate = refDate.AddDays(3), // -1% + 3d (within ±7d)
            }
        };

        var r = await svc.SuggestMatchesAsync(receiptId, maxResults: 5, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.Should().HaveCount(3);
        r.Value![0].SubcontractorName.Should().Be("Perfect match", "exact+exact = 80+20 = 100 (top)");
        r.Value![1].SubcontractorName.Should().Be("Decent match", "±1%+±7d = 50+10 = 60 (middle)");
        r.Value![2].SubcontractorName.Should().Be("Weak match", "out of ±5%+±30d = 0+5 = 5 (bottom)");
    }

    // ============== Test 5 — confirm valid pair ==============

    [Fact]
    public async Task ConfirmMatchAsync_ValidPair_UpdatesBoth()
    {
        var receiptId = Guid.NewGuid();
        var subPaymentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (svc, receipts, matcher, companyId) = Build();
        SeedReceipt(receipts, receiptId, companyId, 12_500m, new DateTime(2026, 8, 10));

        var r = await svc.ConfirmMatchAsync(userId, receiptId, subPaymentId, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.SubPaymentId.Should().Be(subPaymentId);
        r.Value!.Score.Should().Be(100, "confirmed = perfect match from the FE's perspective");
        r.Value!.MatchQuality.Should().Be("EXCELLENT");
        matcher.Confirmed.Should().ContainSingle(c =>
            c.CompanyId == companyId && c.SubPaymentId == subPaymentId &&
            c.ReceiptId == receiptId && c.UserId == userId,
            "the matcher should have received the link with the JWT userId");
    }

    // ============== Test 6 — duplicate confirm ==============

    [Fact]
    public async Task ConfirmMatchAsync_DuplicateConfirm_ThrowsConflict()
    {
        var receiptId = Guid.NewGuid();
        var subPaymentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (svc, receipts, matcher, companyId) = Build();
        SeedReceipt(receipts, receiptId, companyId, 1_000m, DateTime.UtcNow);
        matcher.ThrowOnConfirm = true; // simulates "already matched"

        var r = await svc.ConfirmMatchAsync(userId, receiptId, subPaymentId, CancellationToken.None);

        r.Succeeded.Should().BeFalse();
        r.ErrorCode.Should().Be("CONFLICT", "duplicate confirm should map to HTTP 409");
        r.Error.Should().Contain("already matched");
    }

    // ============== Score-algorithm unit test (pure function) ==============

    [Fact]
    public void ComputeScore_PureFunction_BucketsWorkAsExpected()
    {
        // Exact match
        BankReconciliationService.ComputeScore(10_000m, new DateTime(2026, 1, 1), 10_000m, new DateTime(2026, 1, 1))
            .Should().Be(100, "exact amount +80 + exact date +20");

        // ±1% amount + same date
        BankReconciliationService.ComputeScore(10_000m, new DateTime(2026, 1, 1), 9_900m, new DateTime(2026, 1, 1))
            .Should().Be(70, "±1% +50 + exact date +20");

        // ±5% amount + ±7d date
        BankReconciliationService.ComputeScore(10_000m, new DateTime(2026, 1, 1), 9_500m, new DateTime(2026, 1, 8))
            .Should().Be(30, "±5% +20 + ±7d +10");

        // ±5% amount + ±30d date
        BankReconciliationService.ComputeScore(10_000m, new DateTime(2026, 1, 1), 9_500m, new DateTime(2026, 1, 31))
            .Should().Be(25, "±5% +20 + ±30d +5");

        // Way out of range → 0
        BankReconciliationService.ComputeScore(10_000m, new DateTime(2026, 1, 1), 1_000m, new DateTime(2026, 6, 1))
            .Should().Be(0, "out of all buckets = POOR");
    }
}
