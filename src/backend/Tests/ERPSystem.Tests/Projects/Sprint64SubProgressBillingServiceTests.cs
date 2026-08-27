// Sprint 64 Wave 2A (DEC-223) — Tests for SubProgressBillingService (5 tests).
//
// All tests use fake repositories (in-memory dicts) — no DB needed.
// L19 / DEC-095: service uses ICompanyContext.CompanyId, not req.CompanyId.
// The algorithm: gross = contract_value × work_completed_percent / 100,
//   retention_deducted = (billing_count <= sub_contract.retention_release_billing) ? gross × retention_percent / 100 : 0,
//   net_payable = gross - retention_deducted.
//
// Coverage:
//   1. CreateAsync — first billing calculates gross + retention (L19)
//   2. CreateAsync — billing past retention_release_billing has retention_deducted = 0
//   3. CreateAsync — duplicate billing_number within the same sub-contract rejected (409)
//   4. UpdateAsync — valid request returns 200
//   5. ApproveAsync — Draft → Approved transition returns 200

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

public class Sprint64SubProgressBillingServiceTests
{
    // ===== Fakes =====

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
                .Where(b => b.SubContractId == subContractId)
                .Sum(b => b.GrossAmount));

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

        public Task InsertAsync(SubProgressBilling b, CancellationToken ct)
        {
            _items[b.Id] = b;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SubProgressBilling b, CancellationToken ct)
        {
            _items[b.Id] = b;
            return Task.CompletedTask;
        }

        public Task UpdateStatusAsync(Guid id, int status, DateTime updatedAt, CancellationToken ct)
        {
            if (_items.TryGetValue(id, out var b))
            {
                b.Status = status;
                b.UpdatedAt = updatedAt;
            }
            return Task.CompletedTask;
        }
    }

    internal class FakeSubContractRepository : ISubContractRepository
    {
        private readonly Dictionary<Guid, SubContract> _items = new();
        public void Seed(SubContract sc) => _items[sc.Id] = sc;

        public Task<SubContract?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(_items.TryGetValue(id, out var sc) ? sc : null);

        public Task<IReadOnlyList<SubContract>> ListByProjectAsync(
            Guid projectId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<SubContract>>(_items.Values
                .Where(s => s.ProjectId == projectId)
                .ToList());

        public Task<IReadOnlyList<SubContract>> ListBySubcontractorAsync(
            Guid subcontractorId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<SubContract>>(_items.Values
                .Where(s => s.SubcontractorId == subcontractorId)
                .ToList());

        public Task<int> CountBillingsAsync(Guid subContractId, CancellationToken ct) =>
            Task.FromResult(0);

        public Task InsertAsync(SubContract sc, CancellationToken ct) { _items[sc.Id] = sc; return Task.CompletedTask; }
        public Task UpdateAsync(SubContract sc, CancellationToken ct) { _items[sc.Id] = sc; return Task.CompletedTask; }
        public Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct) { _items.Remove(id); return Task.FromResult(true); }
    }

    private static (SubProgressBillingService svc, FakeSubProgressBillingRepository billingRepo,
        FakeSubContractRepository scRepo, Guid companyId, Guid subContractId)
        Build(decimal contractValue = 50_000m, decimal retentionPercent = 10m, int retentionReleaseBilling = 3)
    {
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
        var svc = new SubProgressBillingService(billingRepo, scRepo, ctx.Object,
            NullLogger<SubProgressBillingService>.Instance);
        return (svc, billingRepo, scRepo, companyId, sc.Id);
    }

    private static CreateSubProgressBillingRequest MakeCreate(
        string number = "B-001", decimal percent = 30m, string? notes = null) =>
        new(number, DateTime.UtcNow.Date, null, null, percent, notes);

    // ========== 1. First billing — calculates gross + retention ==========

    [Fact]
    public async Task CreateAsync_FirstBilling_CalculatesGrossAndRetention()
    {
        // Contract value 50,000, retention 10%, retention_release_billing = 3.
        // First billing @ 30% → gross = 15,000, retention = 1,500, net = 13,500.
        var (svc, _, _, companyId, subContractId) = Build();
        var req = MakeCreate(number: "B-001", percent: 30m);

        var r = await svc.CreateAsync(Guid.NewGuid(), subContractId, req, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.GrossAmount.Should().Be(15_000m);
        r.Value.RetentionDeducted.Should().Be(1_500m);
        r.Value.NetPayable.Should().Be(13_500m);
        r.Value.PreviousBillingsAmount.Should().Be(0m);
        r.Value.CompanyId.Should().Be(companyId, "L19 — CompanyId from ICompanyContext");
        r.Value.Status.Should().Be(1, "Draft");
        r.Value.StatusName.Should().Be("مسودة");
    }

    // ========== 2. Billing past retention_release_billing has 0 retention ==========

    [Fact]
    public async Task CreateAsync_AfterRetentionReleaseBilling_NoRetentionDeducted()
    {
        // Pre-seed 3 prior billings; the 4th billing should have retention = 0.
        var (svc, billingRepo, _, _, subContractId) = Build();
        for (int i = 1; i <= 3; i++)
        {
            billingRepo.Seed(new SubProgressBilling
            {
                Id = Guid.NewGuid(),
                CompanyId = Guid.NewGuid(),
                SubContractId = subContractId,
                BillingNumber = $"B-{i:D3}",
                BillingDate = DateTime.UtcNow.Date.AddDays(-30 + i),
                WorkCompletedPercent = 20m * i,
                GrossAmount = 10_000m * i,
                RetentionDeducted = 1_000m * i,
                NetPayable = 9_000m * i,
                Status = 2, // Approved
            });
        }
        // 4th billing at 100% (incremental 20% of 50,000 = 10,000) → no retention
        var req = MakeCreate(number: "B-004", percent: 100m);

        var r = await svc.CreateAsync(Guid.NewGuid(), subContractId, req, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.GrossAmount.Should().Be(50_000m);
        r.Value.RetentionDeducted.Should().Be(0m, "billing 4 is past retention_release_billing=3");
        r.Value.NetPayable.Should().Be(50_000m);
        r.Value.PreviousBillingsAmount.Should().Be(10_000m + 20_000m + 30_000m);
    }

    // ========== 3. Duplicate billing_number rejected ==========

    [Fact]
    public async Task CreateAsync_DuplicateBillingNumber_Returns409()
    {
        var (svc, billingRepo, _, _, subContractId) = Build();
        billingRepo.Seed(new SubProgressBilling
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            SubContractId = subContractId,
            BillingNumber = "B-001",
            BillingDate = DateTime.UtcNow.Date,
            WorkCompletedPercent = 25m,
            GrossAmount = 12_500m,
            Status = 2,
        });

        var r = await svc.CreateAsync(Guid.NewGuid(), subContractId,
            MakeCreate(number: "B-001", percent: 30m), CancellationToken.None);

        r.Succeeded.Should().BeFalse();
        r.ErrorCode.Should().Be(SubProgressBillingErrorCode.AlreadyExists);
        r.Error.Should().Contain("B-001");
    }

    // ========== 4. Update — valid request returns 200 ==========

    [Fact]
    public async Task UpdateAsync_ValidRequest_Returns200()
    {
        var (svc, _, _, _, subContractId) = Build();
        var created = await svc.CreateAsync(Guid.NewGuid(), subContractId,
            MakeCreate(number: "B-001", percent: 20m), CancellationToken.None);
        created.Succeeded.Should().BeTrue();

        var update = await svc.UpdateAsync(Guid.NewGuid(), created.Value!.Id,
            new UpdateSubProgressBillingRequest(
                PeriodFrom: DateTime.UtcNow.Date.AddDays(-30),
                PeriodTo: DateTime.UtcNow.Date,
                WorkCompletedPercent: 25m,
                Notes: "تعديل"),
            CancellationToken.None);

        update.Succeeded.Should().BeTrue();
        update.Value!.WorkCompletedPercent.Should().Be(25m);
        update.Value.GrossAmount.Should().Be(12_500m, "recomputed: 50,000 × 25% = 12,500");
        update.Value.RetentionDeducted.Should().Be(1_250m, "recomputed: 12,500 × 10% = 1,250");
        update.Value.NetPayable.Should().Be(11_250m);
        update.Value.Notes.Should().Be("تعديل");
    }

    // ========== 5. Approve — Draft → Approved ==========

    [Fact]
    public async Task ApproveAsync_DraftToApproved_Returns200()
    {
        var (svc, _, _, _, subContractId) = Build();
        var created = await svc.CreateAsync(Guid.NewGuid(), subContractId,
            MakeCreate(number: "B-001", percent: 20m), CancellationToken.None);
        created.Succeeded.Should().BeTrue();

        var r = await svc.ApproveAsync(Guid.NewGuid(), created.Value!.Id, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.Status.Should().Be(2);
        r.Value.StatusName.Should().Be("معتمد");
    }
}
