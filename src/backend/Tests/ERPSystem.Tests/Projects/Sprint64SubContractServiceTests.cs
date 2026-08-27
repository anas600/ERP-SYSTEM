// Sprint 64 Wave 1A (DEC-222) — Tests for SubContractService (5 tests).
//
// All tests use fake repositories (in-memory dicts) — no DB needed.
// L19 / DEC-095: service uses ICompanyContext.CompanyId, not req.CompanyId.
// The service also cross-checks that project + subcontractor belong to the
// same company (defense in depth).
//
// Coverage:
//   1. Create — happy path (L19 + project/sub same company)
//   2. Update — happy path
//   3. Create — invalid retention percent rejected
//   4. SoftDelete — happy path (returns true)
//   5. ListByProject — returns contracts scoped to the project

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

public class Sprint64SubContractServiceTests
{
    // ===== Fakes =====

    internal class FakeSubContractRepository : ISubContractRepository
    {
        private readonly Dictionary<Guid, SubContract> _items = new();

        public Task<SubContract?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(_items.TryGetValue(id, out var s) ? s : null);

        public Task<IReadOnlyList<SubContract>> ListByProjectAsync(Guid projectId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<SubContract>>(_items.Values
                .Where(s => s.ProjectId == projectId)
                .OrderBy(s => s.ContractNumber)
                .ToList());

        public Task<IReadOnlyList<SubContract>> ListBySubcontractorAsync(Guid subcontractorId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<SubContract>>(_items.Values
                .Where(s => s.SubcontractorId == subcontractorId)
                .ToList());

        public Task<int> CountBillingsAsync(Guid subContractId, CancellationToken ct) =>
            Task.FromResult(0); // Wave 1A — no sub_progress_billings yet

        public Task InsertAsync(SubContract s, CancellationToken ct)
        {
            _items[s.Id] = s;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(SubContract s, CancellationToken ct)
        {
            _items[s.Id] = s;
            return Task.CompletedTask;
        }

        public Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct)
        {
            return Task.FromResult(_items.Remove(id));
        }
    }

    internal class FakeSubcontractorRepository : ISubcontractorRepository
    {
        private readonly Dictionary<Guid, Subcontractor> _items = new();
        public void Seed(Subcontractor s) => _items[s.Id] = s;

        public Task<Subcontractor?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(_items.TryGetValue(id, out var s) ? s : null);

        public Task<Subcontractor?> GetByCodeAsync(Guid companyId, string code, CancellationToken ct) =>
            Task.FromResult(_items.Values.FirstOrDefault(s =>
                s.CompanyId == companyId &&
                string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<Subcontractor>> ListAsync(
            Guid companyId, bool? isActive, string? tradeSpecialty, int skip, int take, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Subcontractor>>(_items.Values.ToList());

        public Task InsertAsync(Subcontractor s, CancellationToken ct) { _items[s.Id] = s; return Task.CompletedTask; }
        public Task UpdateAsync(Subcontractor s, CancellationToken ct) { _items[s.Id] = s; return Task.CompletedTask; }
        public Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct) { _items.Remove(id); return Task.FromResult(true); }
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
            Guid? companyId, ProjectStatus? status, bool includeInactive, int skip, int take, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Project>>(_items.Values.ToList());

        public Task InsertAsync(Project project, CancellationToken ct) { _items[project.Id] = project; return Task.CompletedTask; }
        public Task UpdateAsync(Project project, CancellationToken ct) { _items[project.Id] = project; return Task.CompletedTask; }
    }

    private static (SubContractService svc, FakeSubContractRepository scRepo, FakeSubcontractorRepository subRepo,
        FakeProjectRepository projRepo, Guid companyId, Guid projectId, Guid subcontractorId)
        Build()
    {
        var scRepo = new FakeSubContractRepository();
        var subRepo = new FakeSubcontractorRepository();
        var projRepo = new FakeProjectRepository();
        var companyId = Guid.NewGuid();

        // Seed a project + subcontractor in the same company
        var project = new Project
        {
            Id = Guid.NewGuid(), CompanyId = companyId, Code = "PRJ-001",
            Name = "مشروع اختبار", CostCenterId = Guid.NewGuid(),
        };
        projRepo.Seed(project);

        var sub = new Subcontractor
        {
            Id = Guid.NewGuid(), CompanyId = companyId, Code = "ELEC-001",
            Name = "مقاول كهرباء", IsActive = true,
        };
        subRepo.Seed(sub);

        var ctx = new Mock<ICompanyContext>();
        ctx.Setup(c => c.CompanyId).Returns(companyId);
        var svc = new SubContractService(scRepo, projRepo, subRepo, ctx.Object,
            NullLogger<SubContractService>.Instance);
        return (svc, scRepo, subRepo, projRepo, companyId, project.Id, sub.Id);
    }

    private static CreateSubContractRequest MakeCreate(Guid subcontractorId) =>
        new(
            SubcontractorId: subcontractorId,
            ContractNumber: "SC-001",
            ScopeOfWork: "أعمال الكهرباء",
            ContractValue: 50_000m,
            RetentionPercent: 10.0m,
            RetentionReleaseBilling: 3,
            StartDate: DateTime.UtcNow.Date,
            EndDate: null,
            Notes: null);

    // ========== 1. Create — happy path (L19) ==========

    [Fact]
    public async Task CreateAsync_ValidRequest_Returns201()
    {
        var (svc, scRepo, _, _, companyId, projectId, subId) = Build();

        var r = await svc.CreateAsync(Guid.NewGuid(), projectId, MakeCreate(subId), CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.ContractNumber.Should().Be("SC-001");
        r.Value.CompanyId.Should().Be(companyId, "L19 — CompanyId from ICompanyContext");
        r.Value.ProjectId.Should().Be(projectId);
        r.Value.SubcontractorId.Should().Be(subId);
        r.Value.Status.Should().Be(1, "default status = Active");
        r.Value.StatusName.Should().Be("نشط");
    }

    // ========== 2. Update — happy path ==========

    [Fact]
    public async Task UpdateAsync_ValidRequest_Returns200()
    {
        var (svc, _, _, _, _, projectId, subId) = Build();
        var created = await svc.CreateAsync(Guid.NewGuid(), projectId, MakeCreate(subId),
            CancellationToken.None);
        created.Succeeded.Should().BeTrue();

        var update = await svc.UpdateAsync(Guid.NewGuid(), created.Value!.Id,
            new UpdateSubContractRequest(
                ScopeOfWork: "أعمال الكهرباء والسباكة",
                ContractValue: 75_000m,
                RetentionPercent: 5.0m,
                RetentionReleaseBilling: 2,
                StartDate: created.Value.StartDate,
                EndDate: null,
                Status: 2, // Completed
                Notes: "تم الانتهاء"),
            CancellationToken.None);

        update.Succeeded.Should().BeTrue();
        update.Value!.ContractValue.Should().Be(75_000m);
        update.Value.RetentionPercent.Should().Be(5.0m);
        update.Value.Status.Should().Be(2);
        update.Value.StatusName.Should().Be("مكتمل");
    }

    // ========== 3. Create — invalid retention percent rejected ==========

    [Fact]
    public async Task CreateAsync_InvalidRetentionPercent_Returns400()
    {
        var (svc, _, _, _, _, projectId, subId) = Build();
        var req = new CreateSubContractRequest(
            SubcontractorId: subId,
            ContractNumber: "SC-BAD",
            ScopeOfWork: "أعمال سباكة",
            ContractValue: 10_000m,
            RetentionPercent: 150m,  // > 100 — must be rejected
            RetentionReleaseBilling: 3,
            StartDate: null, EndDate: null, Notes: null);

        var r = await svc.CreateAsync(Guid.NewGuid(), projectId, req, CancellationToken.None);

        r.Succeeded.Should().BeFalse();
        r.ErrorCode.Should().Be(SubContractErrorCode.ValidationError);
        r.Error.Should().Contain("الاحتجاز");
    }

    // ========== 4. SoftDelete — happy path ==========

    [Fact]
    public async Task SoftDeleteAsync_ValidId_Returns204()
    {
        var (svc, _, _, _, _, projectId, subId) = Build();
        var created = await svc.CreateAsync(Guid.NewGuid(), projectId, MakeCreate(subId),
            CancellationToken.None);

        var r = await svc.SoftDeleteAsync(Guid.NewGuid(), created.Value!.Id, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value.Should().BeTrue();

        var fetched = await svc.GetByIdAsync(created.Value!.Id, CancellationToken.None);
        fetched.Succeeded.Should().BeFalse("the row was deleted");
    }

    // ========== 5. ListByProject — returns contracts scoped to the project ==========

    [Fact]
    public async Task ListByProjectAsync_ReturnsMatchingForProject()
    {
        var (svc, scRepo, _, _, _, projectId, subId) = Build();
        await svc.CreateAsync(Guid.NewGuid(), projectId,
            MakeCreate(subId) with { ContractNumber = "SC-A" }, CancellationToken.None);
        await svc.CreateAsync(Guid.NewGuid(), projectId,
            MakeCreate(subId) with { ContractNumber = "SC-B" }, CancellationToken.None);

        var r = await svc.ListByProjectAsync(projectId, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.Should().HaveCount(2);
        r.Value!.Select(s => s.ContractNumber).Should().Contain(new[] { "SC-A", "SC-B" });
    }
}
