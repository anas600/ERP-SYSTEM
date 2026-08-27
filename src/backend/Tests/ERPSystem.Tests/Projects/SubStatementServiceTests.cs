// Sprint 64 Wave 3A (DEC-225) — Tests for SubStatementService (6 tests).
//
// All tests use fake repositories (in-memory dicts) — no DB needed.
// L19 / DEC-095: the service uses ICompanyContext.CompanyId, not req.CompanyId.
//
// Coverage:
//   1. GetBySubContractAsync — happy path, computes totals + health=OK
//   2. GetBySubContractAsync — health = SETTLED when outstanding = 0
//   3. GetBySubContractAsync — health = OVERDUE when last billing > 60 days
//   4. GetBySubContractAsync — 404 when sub-contract does not exist
//   5. GetBySubContractAsync — excluded billings (status=4) are skipped
//   6. GetBySubcontractorAndProjectAsync — aggregates across all sub-contracts

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

public class SubStatementServiceTests
{
    // ===== Fakes =====

    internal class FakeSubContractRepository : ISubContractRepository
    {
        private readonly Dictionary<Guid, SubContract> _items = new();
        public void Seed(SubContract sc) => _items[sc.Id] = sc;

        public Task<SubContract?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(_items.TryGetValue(id, out var sc) ? sc : null);

        public Task<IReadOnlyList<SubContract>> ListByProjectAsync(Guid projectId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<SubContract>>(_items.Values
                .Where(sc => sc.ProjectId == projectId)
                .ToList());

        public Task<IReadOnlyList<SubContract>> ListBySubcontractorAsync(Guid subcontractorId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<SubContract>>(_items.Values
                .Where(sc => sc.SubcontractorId == subcontractorId)
                .ToList());

        public Task<int> CountBillingsAsync(Guid subContractId, CancellationToken ct) => Task.FromResult(0);
        public Task InsertAsync(SubContract sc, CancellationToken ct) { _items[sc.Id] = sc; return Task.CompletedTask; }
        public Task UpdateAsync(SubContract sc, CancellationToken ct) { _items[sc.Id] = sc; return Task.CompletedTask; }
        public Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct) { _items.Remove(id); return Task.FromResult(true); }
    }

    internal class FakeSubcontractorRepository : ISubcontractorRepository
    {
        private readonly Dictionary<Guid, Subcontractor> _items = new();
        public void Seed(Subcontractor s) => _items[s.Id] = s;

        public Task<Subcontractor?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(_items.TryGetValue(id, out var s) ? s : null);

        public Task<Subcontractor?> GetByCodeAsync(Guid companyId, string code, CancellationToken ct) =>
            Task.FromResult(_items.Values.FirstOrDefault(s => s.CompanyId == companyId && s.Code == code));

        public Task<IReadOnlyList<Subcontractor>> ListAsync(
            Guid companyId, bool? isActive, string? tradeSpecialty,
            int skip, int take, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Subcontractor>>(_items.Values.ToList());

        public Task InsertAsync(Subcontractor s, CancellationToken ct) { _items[s.Id] = s; return Task.CompletedTask; }
        public Task UpdateAsync(Subcontractor s, CancellationToken ct) { _items[s.Id] = s; return Task.CompletedTask; }
        public Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct) { _items.Remove(id); return Task.FromResult(true); }
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
                .ToList());

        public Task<int> CountBySubContractAsync(Guid subContractId, CancellationToken ct) =>
            Task.FromResult(_items.Values.Count(b => b.SubContractId == subContractId));

        public Task<decimal> SumBySubContractAsync(Guid subContractId, CancellationToken ct) =>
            Task.FromResult(_items.Values.Where(b => b.SubContractId == subContractId).Sum(b => b.GrossAmount));

        public Task<decimal> SumGrossNonCancelledBySubContractAsync(Guid subContractId, CancellationToken ct) =>
            Task.FromResult(_items.Values
                .Where(b => b.SubContractId == subContractId && b.Status != 4)
                .Sum(b => b.GrossAmount));

        public Task<decimal> SumRetentionNonCancelledBySubContractAsync(Guid subContractId, CancellationToken ct) =>
            Task.FromResult(_items.Values
                .Where(b => b.SubContractId == subContractId && b.Status != 4)
                .Sum(b => b.RetentionDeducted));

        public Task InsertAsync(SubProgressBilling b, CancellationToken ct) { _items[b.Id] = b; return Task.CompletedTask; }
        public Task UpdateAsync(SubProgressBilling b, CancellationToken ct) { _items[b.Id] = b; return Task.CompletedTask; }
        public Task UpdateStatusAsync(Guid id, int status, DateTime updatedAt, CancellationToken ct) => Task.CompletedTask;
    }

    internal class FakeSubPaymentRepository : ISubPaymentRepository
    {
        private readonly Dictionary<Guid, SubPayment> _items = new();
        public void Seed(SubPayment p) => _items[p.Id] = p;

        public Task<SubPayment?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(_items.TryGetValue(id, out var p) ? p : null);

        public Task<IReadOnlyList<SubPayment>> ListBySubContractAsync(Guid subContractId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<SubPayment>>(_items.Values
                .Where(p => p.SubContractId == subContractId)
                .OrderBy(p => p.PaymentDate)
                .ToList());

        public Task<IReadOnlyList<SubPayment>> ListBySubProgressBillingAsync(Guid subProgressBillingId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<SubPayment>>(_items.Values
                .Where(p => p.SubProgressBillingId == subProgressBillingId)
                .ToList());

        public Task<decimal> SumPaidBySubContractAsync(Guid subContractId, CancellationToken ct) =>
            Task.FromResult(_items.Values
                .Where(p => p.SubContractId == subContractId)
                .Sum(p => p.Amount + p.RetentionReleased));

        public Task<decimal> SumRetentionReleasedBySubContractAsync(Guid subContractId, CancellationToken ct) =>
            Task.FromResult(_items.Values
                .Where(p => p.SubContractId == subContractId)
                .Sum(p => p.RetentionReleased));

        public Task InsertAsync(SubPayment p, CancellationToken ct) { _items[p.Id] = p; return Task.CompletedTask; }
    }

    internal class FakeProjectRepository : IProjectRepository
    {
        private readonly Dictionary<Guid, Project> _items = new();
        public void Seed(Project p) => _items[p.Id] = p;

        public Task<Project?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(_items.TryGetValue(id, out var p) ? p : null);

        public Task<Project?> GetByCodeAsync(string code, CancellationToken ct) =>
            Task.FromResult(_items.Values.FirstOrDefault(p => p.Code == code));

        public Task<IReadOnlyList<Project>> ListAsync(
            Guid? companyId, ProjectStatus? status, bool includeInactive,
            int skip, int take, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Project>>(_items.Values.ToList());

        public Task InsertAsync(Project p, CancellationToken ct) { _items[p.Id] = p; return Task.CompletedTask; }
        public Task UpdateAsync(Project p, CancellationToken ct) { _items[p.Id] = p; return Task.CompletedTask; }
    }

    // ===== Build helpers =====

    private static (SubStatementService svc, FakeSubContractRepository scRepo,
        FakeSubcontractorRepository subRepo, FakeSubProgressBillingRepository billingRepo,
        FakeSubPaymentRepository paymentRepo, FakeProjectRepository projectRepo,
        Guid companyId, Guid subContractId, Guid subcontractorId, Guid projectId)
        Build()
    {
        var scRepo = new FakeSubContractRepository();
        var subRepo = new FakeSubcontractorRepository();
        var billingRepo = new FakeSubProgressBillingRepository();
        var paymentRepo = new FakeSubPaymentRepository();
        var projectRepo = new FakeProjectRepository();
        var companyId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var subcontractorId = Guid.NewGuid();
        var subContractId = Guid.NewGuid();

        var project = new Project
        {
            Id = projectId,
            CompanyId = companyId,
            Code = "P-001",
            Name = "مشروع سكني",
            Status = ProjectStatus.Active,
        };
        projectRepo.Seed(project);

        var sub = new Subcontractor
        {
            Id = subcontractorId,
            CompanyId = companyId,
            Code = "ELEC-001",
            Name = "مقاول الكهرباء",
            IsActive = true,
        };
        subRepo.Seed(sub);

        var sc = new SubContract
        {
            Id = subContractId,
            CompanyId = companyId,
            ProjectId = projectId,
            SubcontractorId = subcontractorId,
            ContractNumber = "SC-001",
            ScopeOfWork = "أعمال الكهرباء",
            ContractValue = 50_000m,
            RetentionPercent = 10m,
            RetentionReleaseBilling = 3,
            Status = 1,
        };
        scRepo.Seed(sc);

        var ctx = new Mock<ICompanyContext>();
        ctx.Setup(c => c.CompanyId).Returns(companyId);

        var svc = new SubStatementService(
            scRepo, subRepo, billingRepo, paymentRepo, projectRepo,
            ctx.Object, NullLogger<SubStatementService>.Instance);
        return (svc, scRepo, subRepo, billingRepo, paymentRepo, projectRepo,
            companyId, subContractId, subcontractorId, projectId);
    }

    // ===== 1. Happy path =====

    [Fact]
    public async Task GetBySubContractAsync_ComputesPnlAndHealthOK()
    {
        var (svc, _, _, billingRepo, paymentRepo, _, _, subContractId, _, _) = Build();

        // 2 active billings, 1 payment. Outstanding = 30000 - 13500 = 16500. Health = OK.
        billingRepo.Seed(new SubProgressBilling
        {
            Id = Guid.NewGuid(), CompanyId = Guid.NewGuid(), SubContractId = subContractId,
            BillingNumber = "B-001", BillingDate = DateTime.UtcNow.Date.AddDays(-10),
            WorkCompletedPercent = 30m, GrossAmount = 15_000m, RetentionDeducted = 1_500m,
            NetPayable = 13_500m, Status = 2,
        });
        billingRepo.Seed(new SubProgressBilling
        {
            Id = Guid.NewGuid(), CompanyId = Guid.NewGuid(), SubContractId = subContractId,
            BillingNumber = "B-002", BillingDate = DateTime.UtcNow.Date.AddDays(-2),
            WorkCompletedPercent = 30m, GrossAmount = 15_000m, RetentionDeducted = 1_500m,
            NetPayable = 13_500m, Status = 2,
        });
        paymentRepo.Seed(new SubPayment
        {
            Id = Guid.NewGuid(), CompanyId = Guid.NewGuid(), SubContractId = subContractId,
            SubProgressBillingId = Guid.NewGuid(), PaymentNumber = "P-001",
            PaymentDate = DateTime.UtcNow.Date.AddDays(-5),
            Amount = 13_500m, RetentionReleased = 0m, CreatedAt = DateTime.UtcNow,
        });

        var r = await svc.GetBySubContractAsync(subContractId, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.TotalBilledGross.Should().Be(30_000m);
        r.Value!.TotalRetentionWithheld.Should().Be(3_000m);
        r.Value!.TotalPaid.Should().Be(13_500m);
        r.Value!.OutstandingBalance.Should().Be(16_500m);
        r.Value!.BillingCount.Should().Be(2);
        r.Value!.WorkCompletedToDate.Should().Be(60m);
        r.Value!.HealthStatus.Should().Be("OK");
        r.Value!.HealthStatusName.Should().Be("حالة جيدة");
        r.Value!.StatusName.Should().Be("نشط");
    }

    // ===== 2. Settled =====

    [Fact]
    public async Task GetBySubContractAsync_HealthSettled_WhenOutstandingZero()
    {
        var (svc, _, _, billingRepo, paymentRepo, _, _, subContractId, _, _) = Build();

        billingRepo.Seed(new SubProgressBilling
        {
            Id = Guid.NewGuid(), CompanyId = Guid.NewGuid(), SubContractId = subContractId,
            BillingNumber = "B-001", BillingDate = DateTime.UtcNow.Date,
            WorkCompletedPercent = 100m, GrossAmount = 10_000m, RetentionDeducted = 0m,
            NetPayable = 10_000m, Status = 3,
        });
        // 10,000 paid (no retention). Outstanding = 0 → SETTLED.
        paymentRepo.Seed(new SubPayment
        {
            Id = Guid.NewGuid(), CompanyId = Guid.NewGuid(), SubContractId = subContractId,
            SubProgressBillingId = Guid.NewGuid(), PaymentNumber = "P-001",
            PaymentDate = DateTime.UtcNow.Date,
            Amount = 10_000m, RetentionReleased = 0m, CreatedAt = DateTime.UtcNow,
        });

        var r = await svc.GetBySubContractAsync(subContractId, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.OutstandingBalance.Should().Be(0m);
        r.Value!.HealthStatus.Should().Be("SETTLED");
    }

    // ===== 3. Overdue =====

    [Fact]
    public async Task GetBySubContractAsync_HealthOverdue_WhenLastBillingOver60Days()
    {
        var (svc, _, _, billingRepo, _, _, _, subContractId, _, _) = Build();

        // Last billing 90 days ago, no payment at all → outstanding > 0 → OVERDUE.
        billingRepo.Seed(new SubProgressBilling
        {
            Id = Guid.NewGuid(), CompanyId = Guid.NewGuid(), SubContractId = subContractId,
            BillingNumber = "B-001", BillingDate = DateTime.UtcNow.Date.AddDays(-90),
            WorkCompletedPercent = 20m, GrossAmount = 10_000m, RetentionDeducted = 1_000m,
            NetPayable = 9_000m, Status = 2,
        });

        var r = await svc.GetBySubContractAsync(subContractId, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.OutstandingBalance.Should().Be(10_000m);
        r.Value!.HealthStatus.Should().Be("OVERDUE");
    }

    // ===== 4. Not found =====

    [Fact]
    public async Task GetBySubContractAsync_Returns404_WhenSubContractMissing()
    {
        var (svc, _, _, _, _, _, _, _, _, _) = Build();

        var r = await svc.GetBySubContractAsync(Guid.NewGuid(), CancellationToken.None);

        r.Succeeded.Should().BeFalse();
        r.ErrorCode.Should().Be(SubStatementErrorCode.NotFound);
        r.Error.Should().Contain("غير موجود");
    }

    // ===== 5. Cancelled billings excluded =====

    [Fact]
    public async Task GetBySubContractAsync_ExcludesCancelledBillings()
    {
        var (svc, _, _, billingRepo, _, _, _, subContractId, _, _) = Build();

        billingRepo.Seed(new SubProgressBilling
        {
            Id = Guid.NewGuid(), CompanyId = Guid.NewGuid(), SubContractId = subContractId,
            BillingNumber = "B-001", BillingDate = DateTime.UtcNow.Date,
            WorkCompletedPercent = 30m, GrossAmount = 15_000m, RetentionDeducted = 1_500m,
            NetPayable = 13_500m, Status = 2,  // active
        });
        billingRepo.Seed(new SubProgressBilling
        {
            Id = Guid.NewGuid(), CompanyId = Guid.NewGuid(), SubContractId = subContractId,
            BillingNumber = "B-002", BillingDate = DateTime.UtcNow.Date,
            WorkCompletedPercent = 30m, GrossAmount = 15_000m, RetentionDeducted = 1_500m,
            NetPayable = 13_500m, Status = 4,  // Cancelled — must be excluded
        });

        var r = await svc.GetBySubContractAsync(subContractId, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.TotalBilledGross.Should().Be(15_000m,
            "cancelled billings must not be included in the statement");
        r.Value!.BillingCount.Should().Be(1);
    }

    // ===== 6. Summary aggregates across multiple sub-contracts =====

    [Fact]
    public async Task GetBySubcontractorAndProjectAsync_AggregatesAcrossSubContracts()
    {
        var (svc, scRepo, _, billingRepo, paymentRepo, _, companyId,
             subContractId, subcontractorId, projectId) = Build();

        // Add a second sub-contract for the same (subcontractor, project) pair.
        var sc2 = new SubContract
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProjectId = projectId,
            SubcontractorId = subcontractorId,
            ContractNumber = "SC-002",
            ScopeOfWork = "أعمال إضافية",
            ContractValue = 20_000m,
            RetentionPercent = 10m,
            RetentionReleaseBilling = 3,
            Status = 1,
        };
        scRepo.Seed(sc2);

        // SC-001: 1 billing (10,000) + 1 payment (5,000)
        billingRepo.Seed(new SubProgressBilling
        {
            Id = Guid.NewGuid(), CompanyId = Guid.NewGuid(), SubContractId = subContractId,
            BillingNumber = "B-001", BillingDate = DateTime.UtcNow.Date,
            WorkCompletedPercent = 20m, GrossAmount = 10_000m, RetentionDeducted = 1_000m,
            NetPayable = 9_000m, Status = 2,
        });
        paymentRepo.Seed(new SubPayment
        {
            Id = Guid.NewGuid(), CompanyId = Guid.NewGuid(), SubContractId = subContractId,
            SubProgressBillingId = Guid.NewGuid(), PaymentNumber = "P-001",
            PaymentDate = DateTime.UtcNow.Date,
            Amount = 5_000m, RetentionReleased = 0m, CreatedAt = DateTime.UtcNow,
        });

        // SC-002: 1 billing (8,000) + 0 payments
        billingRepo.Seed(new SubProgressBilling
        {
            Id = Guid.NewGuid(), CompanyId = Guid.NewGuid(), SubContractId = sc2.Id,
            BillingNumber = "B-001", BillingDate = DateTime.UtcNow.Date,
            WorkCompletedPercent = 40m, GrossAmount = 8_000m, RetentionDeducted = 800m,
            NetPayable = 7_200m, Status = 2,
        });

        var r = await svc.GetBySubcontractorAndProjectAsync(subcontractorId, projectId, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.SubContractCount.Should().Be(2);
        r.Value!.TotalContractValue.Should().Be(70_000m, "50,000 + 20,000");
        r.Value!.TotalBilled.Should().Be(18_000m, "10,000 + 8,000");
        r.Value!.TotalPaid.Should().Be(5_000m, "only SC-001 has payments");
        r.Value!.TotalOutstanding.Should().Be(13_000m, "18,000 - 5,000");
    }
}
