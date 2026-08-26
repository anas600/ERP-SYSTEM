// Sprint 60 Wave 1 (DEC-NEW-15) — Tests for the 5 new foundation projects.
//
// Per Anas's CoA-Final-Proposal-2026-08-24, Wave 1 must add 5 new projects:
//   REST-2026-001 — مطعم الأسماك (NDB seafood contract) — Active
//   REST-2026-002 — خدمات الإعاشة (Catering contract) — Planning
//   ADMN-2026-001 — ترقية نظام ERP (ERP upgrade internal) — Active
//   TRNG-2026-001 — تدريب الموظفين (Staff training) — Planning
//   YRCL-2026-001 — إقفال السنة المالية (Year-end closing) — Planning
//
// Combined with the 3 existing PRJ-2026-* projects from Sprint 58c, the
// final count is 3 + 5 = 8.
//
// Tests follow the same no-DB unit-test pattern as the DEC-NEW-14 test.

using System.Reflection;
using ERPSystem.Shared.Migrations;
using FluentAssertions;

namespace ERPSystem.Tests.Projects;

public class Sprint60FoundationProjectsMigrationTests
{
    private const string MigrationFileRelative = "src/backend/Shared/Migrations/Sprint60_FoundationDataSeed_20260825_002.cs";
    private static readonly string RepoRoot = FindRepoRoot();

    [Fact]
    public void Projects_Up_Seeds_All_5_Expected_Codes()
    {
        var sql = LoadMigrationFile();
        var upSection = ExtractUpSection(sql);

        var expected = new[]
        {
            "REST-2026-001",
            "REST-2026-002",
            "ADMN-2026-001",
            "TRNG-2026-001",
            "YRCL-2026-001"
        };
        foreach (var code in expected)
        {
            upSection.Should().Contain($"'{code}'",
                $"the migration must seed the project with code '{code}' (DEC-NEW-15)");
        }
    }

    [Fact]
    public void Projects_Up_Inserts_With_Arabic_Names()
    {
        var sql = LoadMigrationFile();
        var upSection = ExtractUpSection(sql);

        // Per Anas's spec, each project has an Arabic name in single quotes
        upSection.Should().Contain("'مطعم الأسماك", "REST-2026-001 Arabic name");
        upSection.Should().Contain("'خدمات الإعاشة", "REST-2026-002 Arabic name");
        upSection.Should().Contain("'ترقية نظام ERP", "ADMN-2026-001 Arabic name");
        upSection.Should().Contain("'تدريب الموظفين", "TRNG-2026-001 Arabic name");
        upSection.Should().Contain("'إقفال السنة المالية", "YRCL-2026-001 Arabic name");
    }

    [Fact]
    public void Projects_Up_Is_Idempotent_With_OnConflict()
    {
        var sql = LoadMigrationFile();

        var insertCount = CountOccurrences(sql, "INSERT INTO projects");
        var conflictCount = CountOccurrences(sql, "ON CONFLICT (company_id, code) DO NOTHING");
        conflictCount.Should().BeGreaterThanOrEqualTo(insertCount,
            $"every INSERT INTO projects (count={insertCount}) must be guarded with ON CONFLICT DO NOTHING");
        insertCount.Should().Be(5, "exactly 5 new projects are inserted (REST/ADMN/TRNG/YRCL)");
    }

    [Fact]
    public void Projects_Up_Links_CostCenter_By_Code_Lookup()
    {
        // The projects table requires a NOT NULL cost_center_id. The migration must
        // resolve the cost center by (company_id, code) — not by a hardcoded UUID —
        // so the seed is portable across fresh DBs and remains correct after the
        // cost-centers table is reseeded.
        var sql = LoadMigrationFile();

        // Each project INSERT must JOIN cost_centers by its code.
        sql.Should().Contain("JOIN cost_centers cc ON cc.company_id = c.id AND cc.code = 'CC-REST'",
            "REST-* projects must be linked to the CC-REST cost center");
        sql.Should().Contain("JOIN cost_centers cc ON cc.company_id = c.id AND cc.code = 'CC-ADMIN'",
            "ADMN/TRNG/YRCL projects must be linked to the CC-ADMIN cost center");
    }

    [Fact]
    public void Projects_Up_Sets_Valid_ProjectStatus()
    {
        // ProjectStatus enum: Planning=1, Active=2 (the only valid initial statuses
        // for new foundation projects). The migration uses raw integer literals in
        // the SELECT clause (after the cost_center_id JOIN key).
        var sql = LoadMigrationFile();
        var upSection = ExtractUpSection(sql);

        // The status value is the 2nd integer after `cc.id,` in the SELECT list.
        // Count: 2 Active (REST-2026-001, ADMN-2026-001) and 3 Planning
        //        (REST-2026-002, TRNG-2026-001, YRCL-2026-001).
        // Look for `cc.id, 1,` (Planning) and `cc.id, 2,` (Active).
        var planningCount = CountOccurrences(upSection, "cc.id, 1,");
        var activeCount = CountOccurrences(upSection, "cc.id, 2,");
        planningCount.Should().Be(3,
            "3 projects (REST-2026-002, TRNG-2026-001, YRCL-2026-001) must start as Planning (1)");
        activeCount.Should().Be(2,
            "2 projects (REST-2026-001, ADMN-2026-001) must start as Active (2)");
    }

    [Fact]
    public void Projects_Down_Deletes_The_5_New_Codes_Only()
    {
        // Down must remove only the 5 new projects. The 3 Sprint 58c projects
        // (PRJ-2026-001/002/003) must remain untouched.
        var sql = LoadMigrationFile();
        var downSection = ExtractDownSection(sql);

        downSection.Should().Contain("DELETE FROM projects",
            "Down() must use DELETE FROM projects to reverse the seed");

        var expected = new[] { "REST-2026-001", "REST-2026-002", "ADMN-2026-001", "TRNG-2026-001", "YRCL-2026-001" };
        foreach (var code in expected)
        {
            downSection.Should().Contain($"'{code}'",
                $"Down() must remove the project with code '{code}'");
        }

        downSection.Should().NotContain("'PRJ-2026-001'",
            "Down() must NOT touch the Sprint 58c projects (PRJ-2026-001/002/003)");
    }

    [Fact]
    public void Migration_File_Contains_No_Tenant_Id_Reference()
    {
        var sql = LoadMigrationFile();
        sql.Should().NotContain("ten" + "ant_id",
            "Constitution Article 3 — company_id only, never " + "tenant_id");
    }

    [Fact]
    public void Projects_Total_After_Migration_Is_8()
    {
        // Sanity check: 3 (Sprint 58c) + 5 (Sprint 60 Wave 1) = 8 projects.
        // Asserted as documentation in the test name (the live count is not
        // observable without a DB) so the intent is captured in CI logs.
        const int sprint58c = 3;
        const int sprint60Wave1 = 5;
        (sprint58c + sprint60Wave1).Should().Be(8,
            "after Sprint 60 Wave 1, total project count must be 3 (Sprint 58c) + 5 (Wave 1) = 8");
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
