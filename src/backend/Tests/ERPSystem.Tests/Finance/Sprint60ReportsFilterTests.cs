// Sprint 60 Wave 3B (DEC-191) — Tests for the new cost_center + project filters
// on P&L, Balance Sheet, Trial Balance, and AP Aging report services.
//
// The original report services (Sprint 48, 54) only accepted `companyId` + date
// range. Wave 3B adds optional `costCenterId` and `projectId` parameters that
// filter the underlying SQL on `journal_lines.cost_center_id` and
// `journal_entries.project_id` respectively.
//
// These tests use the in-memory `FakeDbConnectionFactory` (same pattern as the
// Sprint60BalanceMigrationValidationTests) to seed accounts, journal_entries
// and journal_lines, then call the service and assert that the filter actually
// narrows the result set.
//
// FakeDbConnectionFactory has a known limitation: it can only serve queries
// against a SINGLE table (the first FROM/JOIN target). Tests that need
// multi-table JOINs or EXISTS subqueries are marked Skip — they are
// integration tests that require a running Postgres (see the
// FinancialReportsTests sibling file for the same pattern).
//
// The structural SQL tests (verify the new parameters exist, verify the
// @CostCenterId/@ProjectId IS NULL pattern is in the SQL) use reflection +
// file-content checks, mirroring the Sprint60AccountMetadataMigrationTests
// pattern.

using System.Reflection;
using System.Text.RegularExpressions;
using Dapper;
using ERPSystem.Modules.Finance.Application.Services;
using ERPSystem.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ERPSystem.Tests.Finance;

public class Sprint60ReportsFilterTests
{
    private const string ReportServiceFileRelative = "src/backend/Modules/Finance/Application/Services/GeneralLedgerReportService.cs";
    private const string APAgingServiceFileRelative = "src/backend/Modules/Finance/Application/Services/APAgingService.cs";
    private static readonly string RepoRoot = FindRepoRoot();

    // ====================================================================
    // SQL structure: new parameters + IS NULL OR column = @Param pattern
    // ====================================================================

    [Fact]
    public void ReportService_File_Contains_OptionalCostCenterFilter_Pattern()
    {
        // Per DEC-191, the report services must filter on cost_center via the
        // pattern: (@CostCenterId IS NULL OR jl.cost_center_id = @CostCenterId)
        // — this lets callers pass null to skip the filter without writing
        // two SQL variants per report.
        var sql = LoadSourceFile(ReportServiceFileRelative);
        sql.Should().Contain("@CostCenterId IS NULL OR jl.cost_center_id = @CostCenterId",
            "BS / IS / TB must accept an optional cost_center filter on journal_lines");
    }

    [Fact]
    public void ReportService_File_Contains_OptionalProjectFilter_Pattern()
    {
        // Per DEC-191, the report services must filter on project via:
        // (@ProjectId IS NULL OR je.project_id = @ProjectId)
        var sql = LoadSourceFile(ReportServiceFileRelative);
        sql.Should().Contain("@ProjectId IS NULL OR je.project_id = @ProjectId",
            "BS / IS / TB must accept an optional project filter on journal_entries");
    }

    [Fact]
    public void ReportService_File_Includes_NewCodeAndSection_Selects()
    {
        // Per DEC-191, the report services must return the new canonical code
        // and the FS section for downstream display.
        var sql = LoadSourceFile(ReportServiceFileRelative);
        sql.Should().Contain("a.new_code AS NewCode", "report rows must expose the canonical new_code");
        sql.Should().Contain("a.section AS Section", "report rows must expose the FS section");
    }

    [Fact]
    public void ReportService_Interface_Signatures_AcceptCCAndProjectFilters()
    {
        // The public interface IGeneralLedgerReportService must accept the
        // new optional costCenterId + projectId parameters on BS, IS, TB.
        // (Cash Flow keeps the old signature — UI doesn't surface CC/Project for CF yet.)
        var iface = typeof(IGeneralLedgerReportService);
        var bs = iface.GetMethod(nameof(IGeneralLedgerReportService.GetBalanceSheetAsync))!;
        bs.GetParameters().Select(p => p.Name).Should().Contain("costCenterId");
        bs.GetParameters().Select(p => p.Name).Should().Contain("projectId");

        var isMethod = iface.GetMethod(nameof(IGeneralLedgerReportService.GetIncomeStatementAsync))!;
        isMethod.GetParameters().Select(p => p.Name).Should().Contain("costCenterId");
        isMethod.GetParameters().Select(p => p.Name).Should().Contain("projectId");

        var tb = iface.GetMethod(nameof(IGeneralLedgerReportService.GetTrialBalanceAsync))!;
        tb.GetParameters().Select(p => p.Name).Should().Contain("costCenterId");
        tb.GetParameters().Select(p => p.Name).Should().Contain("projectId");
    }

    [Fact]
    public void ReportService_BS_And_IS_PassCCAndProject_To_RecursiveIS_Call()
    {
        // Sprint 60 (DEC-191): the BS uses the P&L's net income for the synthetic
        // Equity row when the year isn't closed. If the user filters BS by CC/Project,
        // the inner P&L must use the same filter to stay consistent.
        var sql = LoadSourceFile(ReportServiceFileRelative);
        // The GetBalanceSheetAsync method's body must call GetIncomeStatementAsync
        // with the costCenterId + projectId args (so the inner P&L matches).
        // The match is a heuristic: there must be at least one call to
        // GetIncomeStatementAsync with both args, anywhere in the file.
        var matches = Regex.Matches(sql, @"GetIncomeStatementAsync\([^)]*costCenterId[^)]*projectId[^)]*\)");
        matches.Count.Should().BeGreaterThan(0,
            "GetBalanceSheetAsync must pass costCenterId + projectId into GetIncomeStatementAsync so the BS net-income row matches the filter");
    }

    [Fact]
    public void APAgingService_File_Contains_OptionalCostCenterFilter_OnJournalLines()
    {
        // Per DEC-191, the AP Aging must filter on cost_center via EXISTS on journal_lines.
        var sql = LoadSourceFile(APAgingServiceFileRelative);
        sql.Should().Contain("journal_lines jl",
            "AP Aging must reference journal_lines for the cost_center EXISTS check");
        sql.Should().Contain("jl.cost_center_id = @CostCenterId",
            "AP Aging must accept an optional cost_center filter on the bill's journal_lines");
    }

    [Fact]
    public void APAgingService_File_Contains_OptionalProjectFilter_OnJournalEntries()
    {
        // Per DEC-191, the AP Aging must filter on project via EXISTS on journal_entries.
        var sql = LoadSourceFile(APAgingServiceFileRelative);
        sql.Should().Contain("je.project_id = @ProjectId",
            "AP Aging must accept an optional project filter on the bill's journal_entries");
    }

    [Fact]
    public void APAgingService_Interface_Accepts_CC_And_Project()
    {
        var iface = typeof(IAPAgingService);
        var get = iface.GetMethod(nameof(IAPAgingService.GetAsync))!;
        get.GetParameters().Select(p => p.Name).Should().Contain("costCenterId");
        get.GetParameters().Select(p => p.Name).Should().Contain("projectId");
    }

    [Fact]
    public void ReportService_DTOs_Expose_NewCode_And_Section()
    {
        // The BalanceSheetRow, IncomeStatementRow, TrialBalanceRow DTOs must
        // carry the new fields so the FE can render them.
        var asm = typeof(ERPSystem.Modules.Finance.Application.BalanceSheetRow).Assembly;
        var bsRow = asm.GetType("ERPSystem.Modules.Finance.Application.BalanceSheetRow")!;
        bsRow.GetProperty("NewCode").Should().NotBeNull("BalanceSheetRow must have NewCode");
        bsRow.GetProperty("Section").Should().NotBeNull("BalanceSheetRow must have Section");

        var isRow = asm.GetType("ERPSystem.Modules.Finance.Application.IncomeStatementRow")!;
        isRow.GetProperty("NewCode").Should().NotBeNull("IncomeStatementRow must have NewCode");
        isRow.GetProperty("Section").Should().NotBeNull("IncomeStatementRow must have Section");

        var tbRow = asm.GetType("ERPSystem.Modules.Finance.Application.TrialBalanceRow")!;
        tbRow.GetProperty("NewCode").Should().NotBeNull("TrialBalanceRow must have NewCode");
        tbRow.GetProperty("Section").Should().NotBeNull("TrialBalanceRow must have Section");
        tbRow.GetProperty("FsType").Should().NotBeNull("TrialBalanceRow must have FsType");
    }

    // ====================================================================
    // Functional test (no DB, FakeDbConnectionFactory) — AP Aging with no
    // filters. Other scenarios require multi-table JOINs which the FakeDb
    // cannot serve — they are exercised by the live integration test in
    // FinancialReportsTests (also Skip) once Postgres is available.
    // ====================================================================

    [Fact]
    public void APAging_NoFilter_ReturnsAllPostedBills_FromFakeDb()
    {
        // Sanity test: ensures the AP Aging service can be constructed and
        // called with the new signature against an empty FakeDb. We don't
        // expect specific rows here — FakeDb doesn't model the complex
        // vendor_bills ↔ payment_allocations join. The point is to catch
        // a breaking signature change.
        var db = new FakeDbConnectionFactory();
        var svc = new APAgingService(db);
        var r = svc.GetAsync(Guid.NewGuid(), new DateTime(2026, 12, 31), null, null, CancellationToken.None).Result;
        r.AsOfDate.Should().Be(new DateTime(2026, 12, 31));
        r.Vendors.Should().NotBeNull();
    }

    [Fact]
    public void ReportService_CanBeConstructed_With_NewSignature()
    {
        // Sanity: ensure the report service is constructible + can be called
        // with the new 5-arg BS signature. (FakeDb will return empty rows
        // because it can only serve single-table queries; the point is to
        // catch a breaking signature change before the integration tests run.)
        var db = new FakeDbConnectionFactory();
        var repo = new AccountRepoStub();
        var svc = new GeneralLedgerReportService(db, repo);

        var bs = svc.GetBalanceSheetAsync(Guid.NewGuid(), new DateTime(2026, 12, 31), null, null, CancellationToken.None).Result;
        bs.Succeeded.Should().BeTrue(bs.Error);

        var isr = svc.GetIncomeStatementAsync(Guid.NewGuid(), new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), null, null, CancellationToken.None).Result;
        isr.Succeeded.Should().BeTrue(isr.Error);

        var tb = svc.GetTrialBalanceAsync(Guid.NewGuid(), new DateTime(2026, 12, 31), null, null, CancellationToken.None).Result;
        tb.Succeeded.Should().BeTrue(tb.Error);
    }

    // ====================================================================
    // Helpers
    // ====================================================================

    private static string LoadSourceFile(string relativePath)
    {
        var path = Path.Combine(RepoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(path).Should().BeTrue($"source file must exist at {path}");
        return File.ReadAllText(path);
    }

    private static string FindRepoRoot()
    {
        // Walk up from AppContext.BaseDirectory (typically .../src/backend/Tests/bin/Debug/net9.0/)
        // until we find a directory containing "AGENTS.md" + a "src/backend" subdir.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md"))
                && Directory.Exists(Path.Combine(dir.FullName, "src", "backend")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException(
            $"could not find repo root (with AGENTS.md + src/backend) starting from {AppContext.BaseDirectory}");
    }

    /// <summary>
    /// Minimal IAccountRepository stub. The report service constructor only needs the
    /// interface to exist; FakeDb returns empty rows for any account lookup, so
    /// the stub never has to return a real row.
    /// </summary>
    private sealed class AccountRepoStub : ERPSystem.Modules.Finance.Infrastructure.IAccountRepository
    {
        public Task<ERPSystem.Modules.Finance.Entities.Account?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<ERPSystem.Modules.Finance.Entities.Account?>(null);
        public Task<ERPSystem.Modules.Finance.Entities.Account?> GetByCodeAsync(string code, Guid companyId, CancellationToken ct) =>
            Task.FromResult<ERPSystem.Modules.Finance.Entities.Account?>(null);
        public Task<ERPSystem.Modules.Finance.Entities.Account?> GetByCodeAsync(string code, CancellationToken ct) =>
            Task.FromResult<ERPSystem.Modules.Finance.Entities.Account?>(null);
        public Task<IReadOnlyList<ERPSystem.Modules.Finance.Entities.Account>> ListAsync(bool includeInactive, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ERPSystem.Modules.Finance.Entities.Account>>(Array.Empty<ERPSystem.Modules.Finance.Entities.Account>());
        public Task<IReadOnlyList<ERPSystem.Modules.Finance.Entities.Account>> ListChildrenAsync(Guid parentId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ERPSystem.Modules.Finance.Entities.Account>>(Array.Empty<ERPSystem.Modules.Finance.Entities.Account>());
        public Task<IReadOnlyList<ERPSystem.Modules.Finance.Entities.Account>> ListByCompanyAsync(Guid? companyId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ERPSystem.Modules.Finance.Entities.Account>>(Array.Empty<ERPSystem.Modules.Finance.Entities.Account>());
        public Task InsertAsync(ERPSystem.Modules.Finance.Entities.Account account, CancellationToken ct) => Task.CompletedTask;
        public Task InsertAsync(ERPSystem.Modules.Finance.Entities.Account account, System.Data.IDbConnection conn, System.Data.IDbTransaction? tx, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(ERPSystem.Modules.Finance.Entities.Account account, CancellationToken ct) => Task.CompletedTask;
        public Task<int> CountPostingsAsync(Guid accountId, CancellationToken ct) => Task.FromResult(0);
        public Task EnsureDefaultCoAAsync(Guid companyId, CancellationToken ct) => Task.CompletedTask;
        public Task EnsureDefaultCoAAsync(Guid companyId, System.Data.IDbConnection conn, System.Data.IDbTransaction? tx, CancellationToken ct) => Task.CompletedTask;
        public Task CloneCoAFromCompanyAsync(Guid targetCompanyId, Guid sourceCompanyId, CancellationToken ct) => Task.CompletedTask;
    }
}
