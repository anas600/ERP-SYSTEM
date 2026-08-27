// Sprint 64 Wave 1A (DEC-221) — Tests for SubcontractorService (5 tests).
//
// All tests use a fake ISubcontractorRepository (in-memory dict) — no DB needed.
// L19 / DEC-095: service uses ICompanyContext.CompanyId, not req.CompanyId — covered
// in CreateAsync_ValidRequest_Returns201 and ListAsync_FilterByTradeSpecialty_ReturnsMatching.
//
// Coverage:
//   1. Create — happy path (L19: companyId from ICompanyContext)
//   2. Create — duplicate code within the same company rejected
//   3. Update — happy path
//   4. SoftDelete — happy path (returns true, sets is_active=false)
//   5. List — filter by tradeSpecialty returns matching rows

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

public class Sprint64SubcontractorServiceTests
{
    // ===== Fakes =====

    internal class FakeSubcontractorRepository : ISubcontractorRepository
    {
        private readonly Dictionary<Guid, Subcontractor> _items = new();

        public Task<Subcontractor?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(_items.TryGetValue(id, out var s) ? s : null);

        public Task<Subcontractor?> GetByCodeAsync(Guid companyId, string code, CancellationToken ct) =>
            Task.FromResult(_items.Values.FirstOrDefault(s =>
                s.CompanyId == companyId &&
                string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<Subcontractor>> ListAsync(
            Guid companyId, bool? isActive, string? tradeSpecialty, int skip, int take, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Subcontractor>>(_items.Values
                .Where(s => s.CompanyId == companyId
                    && (isActive == null || s.IsActive == isActive)
                    && (tradeSpecialty == null ||
                        string.Equals(s.TradeSpecialty, tradeSpecialty, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(s => s.Code)
                .Skip(skip)
                .Take(take)
                .ToList());

        public Task InsertAsync(Subcontractor s, CancellationToken ct)
        {
            _items[s.Id] = s;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Subcontractor s, CancellationToken ct)
        {
            _items[s.Id] = s;
            return Task.CompletedTask;
        }

        public Task<bool> SoftDeleteAsync(Guid id, CancellationToken ct)
        {
            if (!_items.TryGetValue(id, out var s)) return Task.FromResult(false);
            if (!s.IsActive) return Task.FromResult(false);
            s.IsActive = false;
            s.UpdatedAt = DateTime.UtcNow;
            return Task.FromResult(true);
        }
    }

    private static (SubcontractorService svc, FakeSubcontractorRepository repo, Guid companyId)
        Build(Guid? companyId = null)
    {
        var repo = new FakeSubcontractorRepository();
        var cid = companyId ?? Guid.NewGuid();
        var ctx = new Mock<ICompanyContext>();
        ctx.Setup(c => c.CompanyId).Returns(cid);
        var svc = new SubcontractorService(repo, ctx.Object,
            NullLogger<SubcontractorService>.Instance);
        return (svc, repo, cid);
    }

    private static CreateSubcontractorRequest MakeCreate(
        string code = "ELEC-001",
        string name = "مقاول كهرباء",
        string? tradeSpecialty = "electrical") =>
        new(code, name, null, "أحمد", "091-1234567", "ahmed@example.com", tradeSpecialty, "TAX-001");

    // ========== 1. Create — happy path (L19) ==========

    [Fact]
    public async Task CreateAsync_ValidRequest_Returns201()
    {
        var (svc, repo, companyId) = Build();
        var req = MakeCreate();

        var r = await svc.CreateAsync(Guid.NewGuid(), req, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.Code.Should().Be("ELEC-001");
        r.Value.CompanyId.Should().Be(companyId, "L19 — CompanyId from ICompanyContext, not request");
        r.Value.IsActive.Should().BeTrue();

        // Verify the row is persisted with the right company
        var stored = await repo.GetByCodeAsync(companyId, "ELEC-001", CancellationToken.None);
        stored.Should().NotBeNull();
        stored!.CompanyId.Should().Be(companyId);
    }

    // ========== 2. Create — duplicate code rejected ==========

    [Fact]
    public async Task CreateAsync_DuplicateCode_Returns409()
    {
        var (svc, _, _) = Build();
        var first = await svc.CreateAsync(Guid.NewGuid(), MakeCreate(code: "PLMB-001"),
            CancellationToken.None);
        first.Succeeded.Should().BeTrue();

        var second = await svc.CreateAsync(Guid.NewGuid(), MakeCreate(code: "PLMB-001", name: "B"),
            CancellationToken.None);

        second.Succeeded.Should().BeFalse();
        second.ErrorCode.Should().Be(SubcontractorErrorCode.AlreadyExists);
    }

    // ========== 3. Update — happy path ==========

    [Fact]
    public async Task UpdateAsync_ValidRequest_Returns200()
    {
        var (svc, _, _) = Build();
        var create = await svc.CreateAsync(Guid.NewGuid(), MakeCreate(), CancellationToken.None);
        create.Succeeded.Should().BeTrue();

        var update = await svc.UpdateAsync(Guid.NewGuid(), create.Value!.Id,
            new UpdateSubcontractorRequest(
                Name: "مقاول محدّث",
                NameAr: "مقاول محدّث",
                ContactPerson: "خالد",
                Phone: "092-7654321",
                Email: "khaled@example.com",
                TradeSpecialty: "electrical",
                TaxId: "TAX-002",
                IsActive: true),
            CancellationToken.None);

        update.Succeeded.Should().BeTrue();
        update.Value!.Name.Should().Be("مقاول محدّث");
        update.Value.Phone.Should().Be("092-7654321");
        update.Value.IsActive.Should().BeTrue();
    }

    // ========== 4. SoftDelete — happy path ==========

    [Fact]
    public async Task SoftDeleteAsync_ValidId_Returns204()
    {
        var (svc, _, _) = Build();
        var create = await svc.CreateAsync(Guid.NewGuid(), MakeCreate(), CancellationToken.None);
        var r = await svc.SoftDeleteAsync(Guid.NewGuid(), create.Value!.Id, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value.Should().BeTrue();

        var fetched = await svc.GetByIdAsync(create.Value!.Id, CancellationToken.None);
        fetched.Succeeded.Should().BeTrue();
        fetched.Value!.IsActive.Should().BeFalse();
    }

    // ========== 5. List — filter by tradeSpecialty returns matching ==========

    [Fact]
    public async Task ListAsync_FilterByTradeSpecialty_ReturnsMatching()
    {
        var (svc, _, companyId) = Build();
        await svc.CreateAsync(Guid.NewGuid(), MakeCreate(code: "ELEC-001", tradeSpecialty: "electrical"),
            CancellationToken.None);
        await svc.CreateAsync(Guid.NewGuid(), MakeCreate(code: "PLMB-001", tradeSpecialty: "plumbing"),
            CancellationToken.None);
        await svc.CreateAsync(Guid.NewGuid(), MakeCreate(code: "ELEC-002", tradeSpecialty: "electrical"),
            CancellationToken.None);

        var r = await svc.ListAsync(null, "electrical", 0, 50, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value!.Should().HaveCount(2, "two subcontractors have trade_specialty=electrical");
        r.Value!.All(s => s.TradeSpecialty == "electrical").Should().BeTrue();

        // Verify L19: passing a different company would return 0
        var otherCtx = new Mock<ICompanyContext>();
        otherCtx.Setup(c => c.CompanyId).Returns(Guid.NewGuid());
        var otherSvc = new SubcontractorService(new FakeSubcontractorRepository(), otherCtx.Object,
            NullLogger<SubcontractorService>.Instance);
        var other = await otherSvc.ListAsync(null, "electrical", 0, 50, CancellationToken.None);
        other.Value!.Should().BeEmpty("L19 — no subcontractor should leak to a different companyId");
    }
}
