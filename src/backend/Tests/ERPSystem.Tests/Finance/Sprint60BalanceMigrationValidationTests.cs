// Sprint 60 Wave 3A (DEC-189 + DEC-190) — Tests for the Balance Migration
// + CoA Validation migration and the CoAValidationService.
//
// Per Anas's CoA-Final-Proposal-2026-08-24, Wave 3A must:
//   1. Run read-only validation queries on the migrated CoA (DEC-190):
//      - journal_line integrity (no orphans)
//      - trial balance (Σ debit = Σ credit per company)
//      - (company_id, code) UNIQUE on accounts
//      - deprecated accounts with journal_lines (warning)
//   2. Promote the 27 'new' canonical accounts to 'migrated' (DEC-189).
//   3. Expose the same checks via a typed C# service so the FE / ops dashboard
//      can run them on demand without re-running the migration.
//
// The migration tests use the same no-DB pattern as Wave 1/2 (pure reflection
// + file-content checks). The service tests use FakeDbConnectionFactory
// (in-memory DataSet) to seed accounts + journal_lines and assert the
// CoAValidationResult shape.
//
// See:
//   - src/backend/Shared/Migrations/Sprint60_BalanceMigrationValidation_20260825_004.cs
//   - src/backend/Modules/Finance/Application/Services/CoAValidationService.cs

using System.Reflection;
using System.Data;
using Dapper;
using ERPSystem.Modules.Finance.Application.Services;
using ERPSystem.Shared.CompanyContext;
using ERPSystem.Shared.Migrations;
using ERPSystem.Tests.Common;
using FluentAssertions;
using FluentMigrator;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ERPSystem.Tests.Finance;

public class Sprint60BalanceMigrationValidationTests
{
    private const string MigrationFileRelative = "src/backend/Shared/Migrations/Sprint60_BalanceMigrationValidation_20260825_004.cs";
    private static readonly string RepoRoot = FindRepoRoot();

    // ====================================================================
    // Migration class shape (DEC-189 + DEC-190)
    // ====================================================================

    [Fact]
    public void Migration_Class_Exists_With_Stable_Attribute()
    {
        var type = typeof(Sprint60_BalanceMigrationValidation);
        var attr = type.GetCustomAttribute<MigrationAttribute>();
        attr.Should().NotBeNull("Sprint60_BalanceMigrationValidation must be decorated with [Migration(...)]");
        attr!.Version.Should().Be(20260825_150000L,
            "the migration version must be unique and ascending (after Wave 2B = 20260825_003)");
    }

    [Fact]
    public void Migration_Overrides_Up_And_Down()
    {
        var type = typeof(Sprint60_BalanceMigrationValidation);
        type.GetMethod("Up", BindingFlags.Public | BindingFlags.Instance)
            .Should().NotBeNull("Up() must exist so FluentMigrator can apply the changes");
        type.GetMethod("Down", BindingFlags.Public | BindingFlags.Instance)
            .Should().NotBeNull("Down() must exist so the migration can be rolled back");
    }

    [Fact]
    public void Migration_Validates_Journal_Line_Integrity()
    {
        // DEC-190: Up() must run a NOT EXISTS / LEFT JOIN check that finds
        // any journal_line whose account_id is no longer in the accounts
        // table. The check uses RAISE NOTICE to surface the count.
        var sql = LoadMigrationFile();
        var upSection = ExtractUpSection(sql);

        upSection.Should().Contain("LEFT JOIN accounts a",
            "the orphan check must LEFT JOIN against accounts to find null references");
        upSection.Should().Contain("journal_lines jl",
            "the check must scan journal_lines");
        upSection.Should().Contain("a.id IS NULL",
            "the orphan predicate must use a.id IS NULL");
        upSection.Should().Contain("orphan_count",
            "the count must be exposed via a local variable for the RAISE NOTICE");
    }

    [Fact]
    public void Migration_Validates_Trial_Balance_Per_Company()
    {
        // DEC-190: Up() must check Σ debit = Σ credit per company, joined
        // through journal_entries status=2 (Posted). Uses a LOOP with RAISE
        // NOTICE per company.
        var sql = LoadMigrationFile();
        var upSection = ExtractUpSection(sql);

        upSection.Should().Contain("SUM(jl.debit)",
            "the trial balance must sum debits on journal_lines");
        upSection.Should().Contain("SUM(jl.credit)",
            "the trial balance must sum credits on journal_lines");
        upSection.Should().Contain("je.status = 2",
            "the trial balance must only count Posted entries (status=2)");
        upSection.Should().Contain("GROUP BY jl.company_id",
            "the trial balance must be per-company");
        upSection.Should().Contain("Trial balance",
            "the RAISE NOTICE must mention trial balance");
    }

    [Fact]
    public void Migration_Promotes_New_Canonical_Accounts_To_Migrated()
    {
        // DEC-189: Up() must mark the 27 'new' canonical accounts (is_canonical=TRUE)
        // as 'migrated' with migrated_at=now(). This is the "migration is complete"
        // sentinel. Guarded with WHERE migration_status='new' so re-running is a no-op.
        var sql = LoadMigrationFile();
        var upSection = ExtractUpSection(sql);

        upSection.Should().Contain("UPDATE accounts",
            "the migration must contain an UPDATE statement on accounts");
        upSection.Should().Contain("migration_status = 'migrated'",
            "the UPDATE must set migration_status to 'migrated'");
        upSection.Should().Contain("migrated_at = now()",
            "the UPDATE must set migrated_at to the current time");
        upSection.Should().Contain("migration_status = 'new'",
            "the UPDATE must be guarded with WHERE migration_status = 'new' for idempotency");
        upSection.Should().Contain("is_canonical = TRUE",
            "the UPDATE must also guard on is_canonical = TRUE (defensive)");
    }

    [Fact]
    public void Migration_Down_Reverts_New_To_Migrated_Promotions()
    {
        // Down() must revert the Step 5 promotions. Uses migrated_at >=
        // NOW() - INTERVAL '1 hour' as a guard so it only reverts THIS
        // migration's work, not future migrations.
        var sql = LoadMigrationFile();
        var downSection = ExtractDownSection(sql);

        downSection.Should().Contain("UPDATE accounts",
            "Down() must contain an UPDATE statement on accounts");
        downSection.Should().Contain("migration_status = 'new'",
            "Down() must reset migration_status to 'new' on the 27 promoted rows");
        downSection.Should().Contain("migrated_at = NULL",
            "Down() must clear migrated_at");
        downSection.Should().Contain("INTERVAL '1 hour'",
            "Down() must use a time guard to only revert recent promotions");
    }

    [Fact]
    public void Migration_Resolves_Company_By_Constitutional_Code()
    {
        // Per Constitution Article 3 + Wave 1/2 pattern: company_id must
        // be resolved via the '000' code lookup, never hardcoded.
        var sql = LoadMigrationFile();
        sql.Should().Contain("code = '000'",
            "the migration must resolve the default holding company by its constitutional code '000'");
    }

    [Fact]
    public void Migration_File_Contains_No_Tenant_Id_Reference()
    {
        // Constitution Article 3 — company_id only, never tenant_id.
        var sql = LoadMigrationFile();
        sql.Should().NotContain("ten" + "ant_" + "id",
            "Constitution Article 3 — company_id only, never " + "ten" + "ant_" + "id");
    }

    [Fact]
    public void Migration_File_Contains_No_Hardcoded_Account_UUIDs()
    {
        // The migration is read-only + safe-UPDATE — no account UUIDs should
        // appear in literal form. We allow the deterministic '00000000-...'
        // project placeholder if it ever shows up, but per Wave 2B pattern
        // all account id lookups go through gen_random_uuid() or subqueries.
        var sql = LoadMigrationFile();
        var uuidPattern = new System.Text.RegularExpressions.Regex(
            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
        var matches = uuidPattern.Matches(sql);
        matches.Count.Should().Be(0,
            $"the migration must not hardcode any UUIDs; found {matches.Count} UUID-shaped literal(s)");
    }

    // ====================================================================
    // CoAValidationService tests (DEC-190)
    // ====================================================================

    [Fact]
    public async Task Service_HappyPath_NoIssues_IsValidTrue()
    {
        // All-canonical state, balanced trial balance, no orphans → IsValid=true, no errors.
        var db = new FakeDbConnectionFactory();
        var companyId = Guid.NewGuid();
        SeedAccount(db, companyId, "1.1.01", "Cash", isCanonical: true, migrationStatus: "migrated");
        SeedAccount(db, companyId, "1.1.02", "Banks", isCanonical: true, migrationStatus: "migrated");
        SeedJournalLine(db, companyId, "1.1.01", debit: 500m, credit: 0m);
        SeedJournalLine(db, companyId, "1.1.02", debit: 0m, credit: 500m);

        var svc = BuildService(db);
        var result = await svc.ValidateAsync(companyId, TestCompanyContextFactory.Create(companyId), CancellationToken.None);

        result.IsValid.Should().BeTrue("all-canonical + balanced state has no errors");
        result.ErrorCount.Should().Be(0);
        // legacy_count is 0 because we seeded accounts as is_canonical=TRUE.
        result.WarningCount.Should().Be(0);
    }

    [Fact]
    public async Task Service_DuplicateCodes_ProducesError()
    {
        // Two rows with the same (company_id, code) → DUPLICATE_CODE error.
        var db = new FakeDbConnectionFactory();
        var companyId = Guid.NewGuid();
        SeedAccount(db, companyId, "1.1.01", "Cash A", isCanonical: true, migrationStatus: "migrated");
        SeedAccount(db, companyId, "1.1.01", "Cash B", isCanonical: true, migrationStatus: "migrated");
        SeedAccount(db, companyId, "1.1.02", "Banks", isCanonical: true, migrationStatus: "migrated");

        var svc = BuildService(db);
        var result = await svc.ValidateAsync(companyId, TestCompanyContextFactory.Create(companyId), CancellationToken.None);

        result.IsValid.Should().BeFalse("duplicate codes flip IsValid to false");
        result.Issues.Should().Contain(i => i.Code == ValidationCode.DuplicateCode && i.AccountCode == "1.1.01");
    }

    [Fact]
    public async Task Service_OrphanJournalLine_ProducesError()
    {
        // A journal_line whose account_id is not in the accounts table → ORPHAN_JOURNAL_LINE error.
        var db = new FakeDbConnectionFactory();
        var companyId = Guid.NewGuid();
        // Only seed accounts — no journal_lines via the helper. Add a raw journal_line with a fake account id.
        db.AddRow("journal_lines",
            "id", Guid.NewGuid(),
            "journal_entry_id", Guid.NewGuid(),
            "account_id", Guid.NewGuid(),  // ← not present in accounts
            "company_id", companyId,
            "debit", 100m,
            "credit", 0m,
            "line_number", 1);

        var svc = BuildService(db);
        var result = await svc.ValidateAsync(companyId, TestCompanyContextFactory.Create(companyId), CancellationToken.None);

        result.IsValid.Should().BeFalse("orphan journal_lines flip IsValid to false");
        result.Issues.Should().Contain(i => i.Code == ValidationCode.OrphanJournalLine);
    }

    [Fact]
    public async Task Service_TrialBalanceMismatch_ProducesError()
    {
        // Dr ≠ Cr on posted journal_lines → TRIAL_BALANCE_MISMATCH error.
        var db = new FakeDbConnectionFactory();
        var companyId = Guid.NewGuid();
        SeedAccount(db, companyId, "1.1.01", "Cash", isCanonical: true, migrationStatus: "migrated");
        SeedAccount(db, companyId, "4.1.01", "Revenue", isCanonical: true, migrationStatus: "migrated");
        // Dr=1000 but Cr=200 → variance=800
        SeedJournalLine(db, companyId, "1.1.01", debit: 1000m, credit: 0m);
        SeedJournalLine(db, companyId, "4.1.01", debit: 0m, credit: 200m);

        var svc = BuildService(db);
        var result = await svc.ValidateAsync(companyId, TestCompanyContextFactory.Create(companyId), CancellationToken.None);

        result.IsValid.Should().BeFalse("trial balance mismatch flips IsValid to false");
        var issue = result.Issues.Single(i => i.Code == ValidationCode.TrialBalanceMismatch);
        issue.Severity.Should().Be(ValidationSeverity.Error);
        issue.Message.Should().Contain("Dr=").And.Contain("Cr=");
    }

    [Fact]
    public async Task Service_LegacyAccount_ProducesWarningNotError()
    {
        // An account with is_canonical=FALSE + migration_status='pending' → LEGACY_ACCOUNT
        // WARNING. Does NOT flip IsValid to false (the 131 keep accounts are
        // intentionally left on the legacy code per Wave 2B).
        var db = new FakeDbConnectionFactory();
        var companyId = Guid.NewGuid();
        SeedAccount(db, companyId, "1.1.01", "Cash", isCanonical: true, migrationStatus: "migrated");
        SeedAccount(db, companyId, "1101", "Old Cash", isCanonical: false, migrationStatus: "pending");

        var svc = BuildService(db);
        var result = await svc.ValidateAsync(companyId, TestCompanyContextFactory.Create(companyId), CancellationToken.None);

        result.IsValid.Should().BeTrue("legacy accounts are a warning, not an error");
        result.WarningCount.Should().BeGreaterThan(0);
        result.Issues.Should().Contain(i => i.Code == ValidationCode.LegacyAccount && i.Severity == ValidationSeverity.Warning);
    }

    [Fact]
    public async Task Service_InvalidCodeFormat_ProducesError()
    {
        // A code that matches neither canonical nor legacy pattern → INVALID_CODE_FORMAT error.
        // Example: "abc" or "12.34.56.78.90" (5 parts, not allowed).
        var db = new FakeDbConnectionFactory();
        var companyId = Guid.NewGuid();
        SeedAccount(db, companyId, "1.1.01", "Cash", isCanonical: true, migrationStatus: "migrated");
        SeedAccount(db, companyId, "abc", "Bogus", isCanonical: true, migrationStatus: "new");

        var svc = BuildService(db);
        var result = await svc.ValidateAsync(companyId, TestCompanyContextFactory.Create(companyId), CancellationToken.None);

        result.IsValid.Should().BeFalse("invalid code format flips IsValid to false");
        result.Issues.Should().Contain(i => i.Code == ValidationCode.InvalidCodeFormat && i.AccountCode == "abc");
    }

    // ====================================================================
    // Helpers
    // ====================================================================

    private static CoAValidationService BuildService(FakeDbConnectionFactory db) =>
        new(db, NullLogger<CoAValidationService>.Instance);

    private static void SeedAccount(
        FakeDbConnectionFactory db,
        Guid companyId,
        string code,
        string name,
        bool isCanonical,
        string migrationStatus)
    {
        db.AddRow("accounts",
            "id", Guid.NewGuid(),
            "company_id", companyId,
            "code", code,
            "name", name,
            "description", (string?)null,
            "type", 1,
            "normal_balance", 1,
            "parent_account_id", (Guid?)null,
            "is_postable", true,
            "is_active", true,
            "is_intercompany", false,
            "level", (short)3,
            "fs_type", "BS",
            "section", "Current Asset",
            "is_canonical", isCanonical,
            "new_code", isCanonical ? code : (string?)null,
            "migration_status", migrationStatus,
            "migrated_at", (DateTime?)null,
            "created_at", DateTime.UtcNow,
            "updated_at", DateTime.UtcNow);
    }

    private static void SeedJournalLine(
        FakeDbConnectionFactory db,
        Guid companyId,
        string accountCode,
        decimal debit,
        decimal credit)
    {
        // Find the existing account by (company_id, code) so we don't re-add
        // it (which would create a duplicate and break the duplicate-code
        // check in the happy-path test).
        var accountsTable = db.Data.Tables["accounts"];
        accountsTable.Should().NotBeNull($"the 'accounts' table must exist (call SeedAccount before SeedJournalLine for code '{accountCode}')");
        DataRow? accountRow = null;
        if (accountsTable != null)
        {
            foreach (DataRow row in accountsTable.Rows)
            {
                if ((string)row["code"] == accountCode && (Guid)row["company_id"] == companyId)
                {
                    accountRow = row;
                    break;
                }
            }
        }
        // The Should().NotBeNull() is for the test failure message.
        // We copy into a non-nullable local to satisfy the NRT flow.
        DataRow? maybeRow = accountRow;
        DataRow nonNullRow = maybeRow ?? throw new InvalidOperationException(
            $"an account with code '{accountCode}' must be seeded before calling SeedJournalLine");
        Guid accountId = (Guid)nonNullRow["id"];

        var entryId = Guid.NewGuid();
        db.AddRow("journal_entries",
            "id", entryId,
            "entry_number", "JE-2026-0001",
            "company_id", companyId,
            "entry_date", DateTime.UtcNow.Date,
            "description", "Test entry",
            "status", 2,  // Posted
            "created_by_user_id", Guid.NewGuid(),
            "created_at", DateTime.UtcNow,
            "updated_at", DateTime.UtcNow);

        db.AddRow("journal_lines",
            "id", Guid.NewGuid(),
            "journal_entry_id", entryId,
            "account_id", accountId,
            "company_id", companyId,
            "debit", debit,
            "credit", credit,
            "line_number", 1);
    }

    private static string LoadMigrationFile()
    {
        var path = Path.Combine(RepoRoot, MigrationFileRelative.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(path).Should().BeTrue($"migration file must exist at {path}");
        return File.ReadAllText(path);
    }

    private static string ExtractUpSection(string sql)
    {
        var idx = sql.IndexOf("public override void Up()", StringComparison.Ordinal);
        idx.Should().BeGreaterThan(0, "Up() method must exist");
        var downIdx = sql.IndexOf("public override void Down()", StringComparison.Ordinal);
        return sql.Substring(idx, downIdx - idx);
    }

    private static string ExtractDownSection(string sql)
    {
        var idx = sql.IndexOf("public override void Down()", StringComparison.Ordinal);
        idx.Should().BeGreaterThan(0, "Down() method must exist");
        return sql.Substring(idx);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "CHANGELOG.md")) &&
                Directory.Exists(Path.Combine(dir.FullName, "src", "backend")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not find repo root (no CHANGELOG.md + src/backend ancestor).");
    }
}

// =====================================================================
// Test helper: ICompanyContext stub (mirrors the pattern in
// src/backend/Tests/ERPSystem.Tests/Projects/ProjectServiceTests.cs)
// =====================================================================
internal static class TestCompanyContextFactory
{
    public static ICompanyContext Create() => Create(Guid.NewGuid());
    public static ICompanyContext Create(Guid companyId)
    {
        var m = new Mock<ICompanyContext>();
        m.Setup(c => c.CompanyId).Returns(companyId);
        return m.Object;
    }
}
