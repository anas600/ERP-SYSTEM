// Sprint 6 (T3 / Wrap-up) — ChartOfAccountsService tests.
//
// Goal: cover the gap for the CoA endpoints (added in Sprint 4-5, exposed via
// AccountsController). The Sprint 5 demo V2 added CoA management UI; the BE
// service has had ZERO test coverage in the Tests project (only Validators +
// DoubleEntryValidation exist under Tests/Finance/). This file adds 1 smoke
// test per the worker contract "1 test per endpoint" rule, starting with
// ListAsync — the most-read endpoint and the cheapest to mock.
//
// Test approach: same Moq-for-repository pattern as CompaniesListTests (which
// mocks IAccountRepository with MockBehavior.Strict). The service has no
// ICompanyContext dependency (CoA is a global reference table, not
// per-company), so the test setup is just: mock repo + service.
//
// What this test asserts:
//   1. ListAsync(includeInactive=false) returns a successful FinanceResult.
//   2. The mapped AccountResponse list preserves the repo's row count.
//   3. Each AccountResponse carries the right Id/Code/Name/Type/IsActive
//      from the source Account (the mapping is non-trivial — see
//      ChartOfAccountsService.MapToResponse).
//
// What this test does NOT assert (out of scope for a smoke test):
//   - The SQL `WHERE is_active` filter (FakeDb ignores WHERE; that's the
//     integration test's job).
//   - The other 4 endpoints (GetByIdAsync, GetByCodeAsync, CreateAsync,
//     DeleteAsync) — they need their own focused tests (deferred to a
//     later sprint to avoid over-scoping the gap-fill task).
//
// Why no ICompanyContext: Chart of Accounts is the canonical reference table
// for the entire multi-company deployment. A company's CoA is shared across
// all companies in the Holding (the 47 default accounts seeded by
// DefaultCoASeed are inserted ONCE at bootstrap and shared). So the service
// has no CompanyContext filter.

using ERPSystem.Modules.Finance.Application.Services;
using ERPSystem.Modules.Finance.Entities;
using ERPSystem.Modules.Finance.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ERPSystem.Tests.Finance;

public class ChartOfAccountsServiceTests
{
    private static (ChartOfAccountsService svc, Mock<IAccountRepository> repoMock) BuildService()
    {
        var repoMock = new Mock<IAccountRepository>(MockBehavior.Strict);
        var svc = new ChartOfAccountsService(
            repoMock.Object,
            NullLogger<ChartOfAccountsService>.Instance);
        return (svc, repoMock);
    }

    /// <summary>
    /// Happy path: ListAsync(includeInactive=false) returns a successful
    /// FinanceResult with the same count as the repository, and each
    /// AccountResponse preserves the source Account's Id/Code/Name/Type.
    /// </summary>
    [Fact]
    public async Task ListAsync_HappyPath_MapsAllAccountsToResponses()
    {
        // Arrange — 3 canonical demo accounts (one per major AccountType
        // except Liability/Equity which are not in the smoke fixture).
        var repoAccounts = new List<Account>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CompanyId = null,
                Code = "1000",
                Name = "Cash",
                Description = "Cash on hand",
                Type = AccountType.Asset,
                NormalBalance = NormalBalance.Debit,
                ParentAccountId = null,
                IsPostable = true,
                IsActive = true,
            },
            new()
            {
                Id = Guid.NewGuid(),
                CompanyId = null,
                Code = "4000",
                Name = "Sales Revenue",
                Description = "Revenue from sales",
                Type = AccountType.Revenue,
                NormalBalance = NormalBalance.Credit,
                ParentAccountId = null,
                IsPostable = true,
                IsActive = true,
            },
            new()
            {
                Id = Guid.NewGuid(),
                CompanyId = null,
                Code = "5000",
                Name = "Office Supplies",
                Description = "Stationery and consumables",
                Type = AccountType.Expense,
                NormalBalance = NormalBalance.Debit,
                ParentAccountId = null,
                IsPostable = true,
                IsActive = true,
            },
        };

        var (svc, repoMock) = BuildService();
        repoMock
            .Setup(r => r.ListAsync(It.Is<bool>(b => b == false), It.IsAny<CancellationToken>()))
            .ReturnsAsync(repoAccounts);

        // Act
        var r = await svc.ListAsync(includeInactive: false, CancellationToken.None);

        // Assert — successful result, count matches, mapping preserved.
        r.Succeeded.Should().BeTrue("ListAsync never fails — it just wraps the repo result");
        r.Value.Should().NotBeNull();
        r.Value!.Should().HaveCount(3, "the repo returned 3 accounts and the service maps 1:1");

        var cash = r.Value!.Single(a => a.Code == "1000");
        cash.Id.Should().Be(repoAccounts[0].Id, "mapping must preserve Id");
        cash.Name.Should().Be("Cash", "mapping must preserve Name");
        cash.Type.Should().Be(AccountType.Asset, "mapping must preserve Type");
        cash.IsActive.Should().BeTrue("mapping must preserve IsActive");
        cash.NormalBalance.Should().Be(NormalBalance.Debit, "mapping must preserve NormalBalance");

        var sales = r.Value!.Single(a => a.Code == "4000");
        sales.Type.Should().Be(AccountType.Revenue);
        sales.NormalBalance.Should().Be(NormalBalance.Credit,
            "Revenue accounts are credit-normal per the chart-of-accounts spec");

        // Verify the mock was called exactly once with includeInactive=false.
        repoMock.Verify(
            r => r.ListAsync(It.Is<bool>(b => b == false), It.IsAny<CancellationToken>()),
            Times.Once,
            "ListAsync must call the repository exactly once per call");
    }

    /// <summary>
    /// Edge case: ListAsync on an empty repository returns an empty
    /// IReadOnlyList, not null, not an error. The FE renders an empty
    /// state for this — the contract is "200 OK with []" not "404".
    /// </summary>
    [Fact]
    public async Task ListAsync_EmptyRepository_ReturnsEmptyList()
    {
        var (svc, repoMock) = BuildService();
        repoMock
            .Setup(r => r.ListAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Account>());

        var r = await svc.ListAsync(includeInactive: false, CancellationToken.None);

        r.Succeeded.Should().BeTrue();
        r.Value.Should().NotBeNull("a null list would be a contract bug — FE expects an array");
        r.Value!.Should().BeEmpty("no accounts seeded → empty result, not an error");
    }
}
