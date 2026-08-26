// Sprint 60 Wave 1 (DEC-NEW-14) — Tests for the foundation cost-centers data seed.
//
// Per Anas's CoA-Final-Proposal-2026-08-24, Wave 1 must add 4 cost centers
// for the default holding company: CC-CONSTR, CC-REST, CC-ADMIN, CC-WORKSHOP.
// These tests verify the migration (no live DB) — same pattern as the DEC-184
// account-metadata test in Sprint60AccountMetadataMigrationTests.

using System.Reflection;
using ERPSystem.Shared.Migrations;
using FluentAssertions;
using FluentMigrator;

namespace ERPSystem.Tests.Companies;

public class Sprint60FoundationDataMigrationTests
{
    private const string MigrationFileRelative = "src/backend/Shared/Migrations/Sprint60_FoundationDataSeed_20260825_002.cs";
    private static readonly string RepoRoot = FindRepoRoot();

    // ============ Migration class shape ============

    [Fact]
    public void Migration_Class_Exists_With_Stable_Attribute()
    {
        var type = typeof(Sprint60_FoundationDataSeed);
        var attr = type.GetCustomAttribute<MigrationAttribute>();
        attr.Should().NotBeNull("Sprint60_FoundationDataSeed must be decorated with [Migration(...)]");
        attr!.Version.Should().Be(20260825_130000L,
            "the migration version must follow the DEC-184 migration (20260825_001)");
    }

    [Fact]
    public void Migration_Overrides_Up_And_Down()
    {
        var type = typeof(Sprint60_FoundationDataSeed);
        type.GetMethod("Up", BindingFlags.Public | BindingFlags.Instance)
            .Should().NotBeNull("Up() must exist");
        type.GetMethod("Down", BindingFlags.Public | BindingFlags.Instance)
            .Should().NotBeNull("Down() must exist for reversibility");
    }

    // ============ DEC-NEW-14: 4 cost centers ============

    [Fact]
    public void CostCenters_Up_Seeds_All_4_Expected_Codes()
    {
        var sql = LoadMigrationFile();
        var upSection = ExtractUpSection(sql);

        var expected = new[] { "CC-CONSTR", "CC-REST", "CC-ADMIN", "CC-WORKSHOP" };
        foreach (var code in expected)
        {
            upSection.Should().Contain($"'{code}'",
                $"the migration must seed the cost center with code '{code}' (DEC-NEW-14)");
        }
    }

    [Fact]
    public void CostCenters_Up_Inserts_With_Arabic_Names()
    {
        var sql = LoadMigrationFile();
        var upSection = ExtractUpSection(sql);

        // Arabic names are part of the deliverable per Anas's CoA proposal
        upSection.Should().Contain("'قسم المقاولات'", "CC-CONSTR Arabic name");
        upSection.Should().Contain("'قسم المطاعم'", "CC-REST Arabic name");
        upSection.Should().Contain("'الإدارة'", "CC-ADMIN Arabic name");
        upSection.Should().Contain("'الورشة'", "CC-WORKSHOP Arabic name");
    }

    [Fact]
    public void CostCenters_Up_Is_Idempotent_With_OnConflict()
    {
        var sql = LoadMigrationFile();

        // The migration is expected to be re-runnable. Every cost_center INSERT
        // must use ON CONFLICT (company_id, code) DO NOTHING.
        var insertCount = CountOccurrences(sql, "INSERT INTO cost_centers");
        var conflictCount = CountOccurrences(sql, "ON CONFLICT (company_id, code) DO NOTHING");
        conflictCount.Should().BeGreaterThanOrEqualTo(insertCount,
            $"every INSERT INTO cost_centers (count={insertCount}) must be guarded with ON CONFLICT DO NOTHING");
        insertCount.Should().Be(4, "exactly 4 cost centers are inserted (CC-CONSTR/REST/ADMIN/WORKSHOP)");
    }

    [Fact]
    public void CostCenters_Down_Deletes_The_4_New_Codes()
    {
        var sql = LoadMigrationFile();
        var downSection = ExtractDownSection(sql);

        var expected = new[] { "CC-CONSTR", "CC-REST", "CC-ADMIN", "CC-WORKSHOP" };
        foreach (var code in expected)
        {
            downSection.Should().Contain($"'{code}'",
                $"Down() must remove the cost center with code '{code}'");
        }
        downSection.Should().Contain("DELETE FROM cost_centers",
            "Down() must use DELETE FROM cost_centers to reverse the seed");
    }

    // ============ Architectural compliance ============

    [Fact]
    public void Migration_File_Contains_No_Tenant_Id_Reference()
    {
        var sql = LoadMigrationFile();
        sql.Should().NotContain("ten" + "ant_" + "id",
            "Constitution Article 3 — company_id only, never " + "ten" + "ant_" + "id");
    }

    [Fact]
    public void CostCenters_Insert_References_Default_Holding_By_Constitutional_Code()
    {
        // The default holding company is identified by code = '000' per CONSTITUTION.md §3.2.
        // The migration must look up the company by this code (not a hardcoded UUID).
        var sql = LoadMigrationFile();
        sql.Should().Contain("c.code = '000'",
            "the migration must resolve the default holding company by its constitutional code '000'");
    }

    // ============ Helpers ============

    private static string LoadMigrationFile()
    {
        var path = Path.Combine(RepoRoot, MigrationFileRelative.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(path).Should().BeTrue($"migration file must exist at {path}");
        return File.ReadAllText(path);
    }

    private static int CountOccurrences(string haystack, string needle)
        => System.Text.RegularExpressions.Regex.Matches(haystack, System.Text.RegularExpressions.Regex.Escape(needle)).Count;

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
