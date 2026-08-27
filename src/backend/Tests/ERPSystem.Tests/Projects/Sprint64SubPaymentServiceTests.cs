// Sprint 64 Wave 2A (DEC-224) — Tests for SubPaymentService (5 tests).
//
// All tests use fake repositories (in-memory dicts) — no DB needed.
// L19 / DEC-095: service uses ICompanyContext.CompanyId, not req.CompanyId.
//
// Coverage:
//   1. CreateAsync — valid request returns 201
//   2. GetBalanceAsync — calculates outstanding = totalBilledGross - totalPaid
//   3. GetBalanceAsync — with retention withheld, the withheld amount stays in outstanding
//   4. ReleaseRetentionAsync — valid request returns 200
//   5. ReleaseRetentionAsync — exceeding the available retention returns 400

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ERPSystem.Modules.Projects.Application.Dtos;
using ERPSystem.Modules.Projects.Application.Services;
using ERPSystem.Modules.Projects.Entities;
using ERPSystem.Modules.Projects.Infrastructure;
using ERPSystem.Shared.CompanyContext;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ERPSystem.Tests.Projects;

public class Sprint64SubPaymentServiceTests
{
    // ===== Fakes (shared shape with the other test classes) =====

    internal class FakeSubPaymentRepository : ISubPaymentRepository
    {
        private readonly Dictionary<Guid, SubPayment> _items = new();
        public void Seed(SubPayment p) => _items[p.Id] = p;

        public Task<SubPayment?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(_items.TryGetValue(id, out var p) ? p : null);

        public Task<IReadOnlyList<SubPayment>> ListBySubContractAsync(
            Guid subContractId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<SubPayment>>(_items.Values
                .Where(p => p.SubContractId == subContractId)
                .OrderBy(p => p.PaymentDate)
                .ThenBy(p => p.PaymentNumber)
                .ToList());

        public Task<IReadOnlyList<SubPayment>> ListBySubProgressBillingAsync(
            Guid subProgressBillingId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<SubPayment>>(_items.Values
                .Where(p => p.SubProgressBillingId == subProgressBillingId)
                .ToList());

        public Task<decimal> SumPaidBySubContractAsync(Guid subContractId, CancellationToken ct) =>
            Task.FromResult(_items.Values
                .Where(p => p.SubContractId == subContractId)
                .Sum(p => p.Amount + p.RetentionReleased));

        public Task<decimal> SumRetentionReleasedBySubContractAsync(
            Guid subContractId, CancellationToken ct) =>
            Task.FromResult(_items.Values
                .Where(p => p.SubContractId == subContractId)
                .Sum(p => p.RetentionReleased));

        public Task InsertAsync(SubPayment p, CancellationToken ct)
        {
            _items[p.Id] = p;
            return Task.CompletedTask;
        }
    }

    internal class FakeSubProgressBillingRepository : ISubProgressBillingRepository
    {
        private readonly Dictionary<Guid, SubProgressBilling> _items = new();
        public void Seed(SubProgressBilling b) => _items[b.Id] = b;

        public Task<SubProgressBilling?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(_items.TryGetValue(id, out var b) ? b : null);

        public Task<IReadOnlyList<SubProgressBilling>> ListBySubContractAsync(
            Guid subContractId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<SubProgressBilling>>(_items.Values
                .Where(b => b.SubContractId == subContractId)
                .OrderBy(b => b.BillingDate)
                .ThenBy(b => b.BillingNumber)
                .ToList());

        public Task<int> CountBySubContractAsync(Guid subContractId, CancellationToken ct) =>
            Task.FromResult(_items.Values.Count(b => b.SubContractId == subContractId));

        public Task<decimal> SumBySubContractAsync(Guid subContractId, CancellationToken ct) =>
            Task.FromResult(_items.Values
                .Where(b => b.SubContractId == subContractId).Sum(b => b.GrossAmount));

        public Task<decimal> SumGrossNonCancelledBySubContractAsync(
            Guid subContractId, CancellationToken ct) =>
            Task.FromResult(_items.Values
                .Where(b => b.SubContractId == subContractId && b.Status != 4)
                .Sum(b => b.GrossAmount));

        public Task<decimal> SumRetentionNonCancelledBySubContractAsync(
            Guid subContractId, CancellationToken ct) =>
            Task.FromResult(_items.Values
                .Where(b => b.SubContractId == subContractId && b.Status != 4)
                .Sum(b => b.RetentionDeducted));

        public Task InsertAsync(SubProgressBilling b, CancellationToken ct) { _items[b.Id] = b; return Task.CompletedTask; }
        public Task UpdateAsync(SubProgressBilling b, CancellationToken ct) { _items[b.Id] = b; return Task.CompletedTask; }
        public Task UpdateStatusAsync(Guid id, int status, DateTime updatedAt, CancellationToken ct) { return Task.CompletedTask; }
    }

    internal class FakeSubContractRepository : ISubContractRepository
    {
        private readonly Dictionary<Guid, SubContract> _items = new();
        public void Seed(SubContract sc) => _items[sc.Id] = sc;

        public Task<SubContract?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(_items.TryGetValue(id, out var sc) ? sc : null);

        public Task<IReadOnlyList<SubContract>> ListByProjectAsync(Guid projectId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<SubContract>>(_items.Values.ToList());
        public Task<IReadOnlyList<SubContract>> ListBySubcontractorAsync(Guid subcontractorId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<SubContract>>(_items.Values.ToList());
        public Task<int> CountBillingsAsync(Guid subContractId, CancellationToken ct) => Task.FromResult(0);
        public Task InsertAsync(SubContract sc, CancellationToken ct) { _items[sc.Id] = sc; return Task.CompletedTask; }
        public Task UpdateAsync(SubContract sc, CancellationToken ct) { _items[sc.Id] = sc; return Task.CompletedTask; }
        public Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct) { _items.Remove(id); return Task.FromResult(true); }
    }

    private static (SubPaymentService svc, FakeSubPaymentRepository paymentRepo,
        FakeSubProgressBillingRepository billingRepo, FakeSubContractRepository scRepo,
        Guid companyId, Guid subContractId)
        Build(decimal contractValue = 50_000m, decimal retentionPercent = 10m, int retentionReleaseBilling = 3)
    {
        var paymentRepo = new FakeSubPaymentRepository();
        var billingRepo = new FakeSubProgressBillingRepository();
        var scRepo = new FakeSubContractRepository();
        var companyId = Guid.NewGuid();

        var sc = new SubContract
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProjectId = Guid.NewGuid(),
            SubcontractorId = Guid.NewGuid(),
            ContractNumber = "SC-001",
            ScopeOfWork = "أعمال الكهرباء",
            ContractValue = contractValue,
            RetentionPercent = retentionPercent,
            RetentionReleaseBilling = retentionReleaseBilling,
            Status = 1,
        };
        scRepo.Seed(sc);

        var ctx = new Mock<ICompanyContext>();
        ctx.Setup(c => c.CompanyId).Returns(companyId);
        var svc = new SubPaymentService(paymentRepo, billingRepo, scRepo, ctx.Object,
            NullLogger<SubPaymentService>.Instance);
        return (svc, paymentRepo, billingRepo, scRepo, companyId, sc.Id);
    }

    // ========== 1. Create — happy path ==========

    [Fact]
    public async Task CreateAsync_ValidRequest_Returns201()
    {
        var (svc, _, billingRepo, _, companyId, subContractId) = Build();
        var billing = new SubProgressBilling
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            SubContractId = subContractId,
            BillingNumber = "B-001",
            BillingDate = DateTime.UtcNow.Date,
            WorkCompletedPercent = 30m,
            GrossAmount = 15_000m,
            NetPayable = 13_500m,
            Status = 2, // Approved
        };
        billingRepo.Seed(billing);

        var r = await svc.CreateAsync(Guid.NewGuid(), subContractId, billing.Id,
            new CreateSubPaymentRequest("P-001", DateTime.UtcNow.Date, 5_000m,
                "bank_transfer", "REF-001", null),
            CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.Amount.Should().Be(5_000m);
        r.Value.RetentionReleased.Should().Be(0m);
        r.Value.PaymentMethod.Should().Be("bank_transfer");
        r.Value.ReferenceNumber.Should().Be("REF-001");
        r.Value.SubProgressBillingId.Should().Be(billing.Id);
    }

    // ========== 2. GetBalance — calculates outstanding correctly ==========

    [Fact]
    public async Task GetBalanceAsync_CalculatesOutstandingCorrectly()
    {
        // Contract 50,000. Two billings (15,000 + 15,000) = 30,000 gross, 3,000 retention.
        // One payment of 20,000. Outstanding = 30,000 - 20,000 = 10,000.
        var (svc, paymentRepo, billingRepo, _, _, subContractId) = Build();

        var b1 = new SubProgressBilling
        {
            Id = Guid.NewGuid(), CompanyId = Guid.NewGuid(), SubContractId = subContractId,
            BillingNumber = "B-001", BillingDate = DateTime.UtcNow.Date.AddDays(-30),
            WorkCompletedPercent = 30m, GrossAmount = 15_000m, RetentionDeducted = 1_500m,
            NetPayable = 13_500m, Status = 2,
        };
        var b2 = new SubProgressBilling
        {
            Id = Guid.NewGuid(), CompanyId = Guid.NewGuid(), SubContractId = subContractId,
            BillingNumber = "B-002", BillingDate = DateTime.UtcNow.Date,
            WorkCompletedPercent = 30m, GrossAmount = 15_000m, RetentionDeducted = 1_500m,
            NetPayable = 13_500m, Status = 2,
        };
        billingRepo.Seed(b1);
        billingRepo.Seed(b2);
        paymentRepo.Seed(new SubPayment
        {
            Id = Guid.NewGuid(), CompanyId = Guid.NewGuid(), SubContractId = subContractId,
            SubProgressBillingId = b1.Id, PaymentNumber = "P-001",
            PaymentDate = DateTime.UtcNow.Date.AddDays(-25), Amount = 20_000m,
            RetentionReleased = 0m, CreatedAt = DateTime.UtcNow,
        });

        var r = await svc.GetBalanceAsync(subContractId, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.TotalBilledGross.Should().Be(30_000m);
        r.Value.TotalRetentionWithheld.Should().Be(3_000m);
        r.Value.TotalPaid.Should().Be(20_000m);
        r.Value.OutstandingBalance.Should().Be(10_000m,
            "outstanding = totalBilledGross (30,000) - totalPaid (20,000) = 10,000");
    }

    // ========== 3. GetBalance — retention withheld is part of outstanding ==========

    [Fact]
    public async Task GetBalanceAsync_WithRetentionHeld_ShowsInBalance()
    {
        // Contract 50,000. One billing: gross=10,000, retention=1,000, net=9,000.
        // No payment yet. Outstanding should equal the full gross (10,000),
        // because the withheld retention (1,000) is still owed until released.
        var (svc, _, billingRepo, _, _, subContractId) = Build();
        billingRepo.Seed(new SubProgressBilling
        {
            Id = Guid.NewGuid(), CompanyId = Guid.NewGuid(), SubContractId = subContractId,
            BillingNumber = "B-001", BillingDate = DateTime.UtcNow.Date,
            WorkCompletedPercent = 20m, GrossAmount = 10_000m, RetentionDeducted = 1_000m,
            NetPayable = 9_000m, Status = 2,
        });

        var r = await svc.GetBalanceAsync(subContractId, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.TotalBilledGross.Should().Be(10_000m);
        r.Value.TotalRetentionWithheld.Should().Be(1_000m);
        r.Value.TotalPaid.Should().Be(0m);
        r.Value.OutstandingBalance.Should().Be(10_000m,
            "withheld retention stays in outstanding until released");
    }

    // ========== 4. ReleaseRetention — valid request ==========

    [Fact]
    public async Task ReleaseRetentionAsync_ValidRequest_Returns200()
    {
        // Two billings with total retention = 1,500 + 1,500 = 3,000.
        // Release 1,000 → success, creates a SubPayment with retention_released=1,000.
        var (svc, _, billingRepo, _, _, subContractId) = Build();
        var b1 = new SubProgressBilling
        {
            Id = Guid.NewGuid(), CompanyId = Guid.NewGuid(), SubContractId = subContractId,
            BillingNumber = "B-001", BillingDate = DateTime.UtcNow.Date.AddDays(-30),
            WorkCompletedPercent = 30m, GrossAmount = 15_000m, RetentionDeducted = 1_500m,
            NetPayable = 13_500m, Status = 2,
        };
        billingRepo.Seed(b1);

        var r = await svc.ReleaseRetentionAsync(Guid.NewGuid(), subContractId,
            new ReleaseRetentionRequest(DateTime.UtcNow.Date, 1_000m, "تحرير جزء"),
            CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.Amount.Should().Be(0m, "retention-release payments have amount=0");
        r.Value.RetentionReleased.Should().Be(1_000m);
        r.Value.SubProgressBillingId.Should().Be(b1.Id, "linked to the first approved billing");
        r.Value.PaymentNumber.Should().StartWith("REL-");
    }

    // ========== 5. ReleaseRetention — exceeds available ==========

    [Fact]
    public async Task ReleaseRetentionAsync_ExceedingWithheld_Returns400()
    {
        // Withheld = 1,500. Try to release 5,000.
        var (svc, _, billingRepo, _, _, subContractId) = Build();
        billingRepo.Seed(new SubProgressBilling
        {
            Id = Guid.NewGuid(), CompanyId = Guid.NewGuid(), SubContractId = subContractId,
            BillingNumber = "B-001", BillingDate = DateTime.UtcNow.Date,
            WorkCompletedPercent = 30m, GrossAmount = 15_000m, RetentionDeducted = 1_500m,
            NetPayable = 13_500m, Status = 2,
        });

        var r = await svc.ReleaseRetentionAsync(Guid.NewGuid(), subContractId,
            new ReleaseRetentionRequest(DateTime.UtcNow.Date, 5_000m, null),
            CancellationToken.None);

        r.Succeeded.Should().BeFalse();
        r.ErrorCode.Should().Be(SubPaymentErrorCode.ValidationError);
        r.Error.Should().Contain("تتجاوز المتاح");
    }
}
