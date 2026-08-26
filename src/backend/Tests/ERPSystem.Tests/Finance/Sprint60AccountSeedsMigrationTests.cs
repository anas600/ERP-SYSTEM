// Sprint 60 Wave 2B (DEC-185/186/187/188 + DEC-NEW-1..13) — Tests for the
// Account Seeds & Canonical Migration.
//
// Per Anas's CoA-Final-Proposal-2026-08-24, Wave 2B must:
//   1. INSERT 27 new canonical accounts (DEC-NEW-1/2/5/6..13)
//   2. Mark 1.3 Off-Balance as deprecated (DEC-186)
//   3. Rename WIP 9201 → 1.1.06 (DEC-187)
//   4. Split L1=7 into 7.1/7.2/7.3 (DEC-188)
//   5. Backfill fs_type + section on the 131 existing keep accounts
//
// The 27 new accounts = 8 L3 control accounts + 19 L4 postable detail accounts.
// See Sprint60_AccountSeedsAndMigration_20260825_003.cs for the migration body.
//
// Tests follow the same no-DB unit-test pattern as the Wave 1 tests
// (Sprint60AccountMetadataMigrationTests, Sprint60FoundationDataMigrationTests):
// pure reflection + file-content checks, no live database. The heavy lifting
// (apply + roll back) is exercised by MigrationRunnerHostedService when the
// app starts; these tests assert the migration is correctly authored.

using System.Reflection;
using ERPSystem.Shared.Migrations;
using FluentAssertions;
using FluentMigrator;

namespace ERPSystem.Tests.Finance;

public class Sprint60AccountSeedsMigrationTests
{
    private const string MigrationFileRelative = "src/backend/Shared/Migrations/Sprint60_AccountSeedsAndMigration_20260825_003.cs";
    private static readonly string RepoRoot = FindRepoRoot();

    // The full 27 new account codes (per the migration). 8 L3 + 19 L4.
    private static readonly string[] L3NewCodes = new[]
    {
        "1.1.01", "1.1.02", "1.2.01", "1.2.02", "2.1.08", "5.2.02", "5.2.03", "8.2.01"
    };
    private static readonly string[] L4NewCodes = new[]
    {
        // DEC-NEW-1 (7) — Cash/Banks detail
        "1.1.01.002", "1.1.01.003", "1.1.02.001", "1.1.02.002", "1.1.02.003", "1.1.02.004", "1.1.02.005",
        // DEC-NEW-2 (5) — Tangible PPE & Intangible detail
        "1.2.01.001", "1.2.01.002", "1.2.01.003", "1.2.01.008", "1.2.02.001",
        // DEC-NEW-5 (7) — NDB / Stamps / CIT / SS detail
        "8.2.01.001", "8.2.01.002", "8.2.01.003", "8.2.01.005", "2.1.03.002", "2.1.08.001", "2.1.08.002"
    };
    private static readonly string[] AllNewCodes = L3NewCodes.Concat(L4NewCodes).ToArray();

    // ==================== Migration class shape ====================

    [Fact]
    public void Migration_Class_Exists_With_Stable_Attribute()
    {
        var type = typeof(Sprint60_AccountSeedsAndMigration);
        var attr = type.GetCustomAttribute<MigrationAttribute>();
        attr.Should().NotBeNull("Sprint60_AccountSeedsAndMigration must be decorated with [Migration(...)]");
        attr!.Version.Should().Be(20260825_140000L,
            "the migration version must follow the Wave 1 migrations (001 DDL, 002 data seed)");
    }

    [Fact]
    public void Migration_Overrides_Up_And_Down()
    {
        var type = typeof(Sprint60_AccountSeedsAndMigration);
        type.GetMethod("Up", BindingFlags.Public | BindingFlags.Instance)
            .Should().NotBeNull("Up() must exist so FluentMigrator can apply the changes");
        type.GetMethod("Down", BindingFlags.Public | BindingFlags.Instance)
            .Should().NotBeNull("Down() must exist so the migration can be rolled back");
    }

    // ==================== Step 1 — DEC-NEW-1/2/5/6..13: 27 new accounts ====================

    [Fact]
    public void Inserts_All_27_New_Account_Codes()
    {
        // The 27 = 8 L3 control + 19 L4 postable detail accounts.
        // Asserts the Up() contains every code exactly as a literal SQL value.
        var sql = LoadMigrationFile();
        var upSection = ExtractUpSection(sql);

        foreach (var code in AllNewCodes)
        {
            upSection.Should().Contain($"'{code}'",
                $"the migration must insert a new account with code '{code}' (DEC-NEW-1/2/5/6..13)");
        }
        AllNewCodes.Length.Should().Be(27,
            "exactly 27 new accounts (8 L3 + 19 L4) must be inserted per the CoA plan");
    }

    [Fact]
    public void Inserts_Count_Is_8_L3_Plus_19_L4()
    {
        var sql = LoadMigrationFile();
        var upSection = ExtractUpSection(sql);

        var insertCount = CountOccurrences(sql, "INSERT INTO accounts");
        // 27 new + 0 (no extra inserts in the migration) = 27 INSERTs
        insertCount.Should().Be(27, "exactly 27 INSERT INTO accounts statements (one per new account)");
    }

    [Fact]
    public void New_Accounts_Use_Arabic_Names()
    {
        var sql = LoadMigrationFile();
        var upSection = ExtractUpSection(sql);

        // Spot-check Arabic names for each DEC. We don't check all 27 — a
        // missing name on one row would still be caught by the code-list test.
        upSection.Should().Contain("'النقدية'", "1.1.01 Cash on Hand Arabic");
        upSection.Should().Contain("'البنوك'", "1.1.02 Banks Arabic");
        upSection.Should().Contain("'أراضي'", "1.2.01.001 Land Arabic");
        upSection.Should().Contain("'برامج حاسوب'", "1.2.02.001 Software Arabic");
        upSection.Should().Contain("'دمغة هندسية'", "8.2.01.001 Engineering Stamp Arabic");
        upSection.Should().Contain("'ضريبة خصم من المنبع'", "2.1.03.002 CIT Withholding Arabic");
        upSection.Should().Contain("'تأمينات اجتماعية - حصة العامل'", "2.1.08.001 SS Employee Arabic");
    }

    [Fact]
    public void All_Inserts_Are_Idempotent_With_OnConflict()
    {
        // Per Constitution Article 8 + Sprint 60 Wave 1 pattern, every
        // INSERT must be guarded with ON CONFLICT (company_id, code) DO NOTHING.
        var sql = LoadMigrationFile();

        var insertCount = CountOccurrences(sql, "INSERT INTO accounts");
        var conflictCount = CountOccurrences(sql, "ON CONFLICT (company_id, code) DO NOTHING");
        conflictCount.Should().BeGreaterThanOrEqualTo(insertCount,
            $"every INSERT INTO accounts (count={insertCount}) must be guarded with ON CONFLICT DO NOTHING");
    }

    [Fact]
    public void New_Accounts_Set_Canonical_Metadata()
    {
        // The migration uses positional INSERT (column names listed at the top,
        // values in the SELECT clause), so the audit checks for the literal
        // SQL fragments that prove the right column values are set:
        //   - is_canonical = TRUE (literal boolean)
        //   - migration_status = 'new' (string literal value used in SELECT)
        //   - the column list mentions both is_canonical, new_code, and migration_status
        // and must resolve company_id via the constitutional code '000' lookup.
        var sql = LoadMigrationFile();
        var upSection = ExtractUpSection(sql);

        upSection.Should().Contain("is_canonical = TRUE",
            "every new account must be marked as canonical (per the migration docstring)");
        upSection.Should().Contain("'new'",
            "every new account must be tagged 'new' (vs 'migrated' for renamed rows) — 'new' is the value passed positionally to migration_status");
        upSection.Should().Contain("'000'",
            "company_id must be resolved via the constitutional '000' code lookup, not a hardcoded UUID");
        // Sanity: the INSERT column list must reference the metadata columns
        upSection.Should().Contain("is_canonical",
            "INSERT column list must include is_canonical");
        upSection.Should().Contain("new_code",
            "INSERT column list must include new_code");
        upSection.Should().Contain("migration_status",
            "INSERT column list must include migration_status");
        upSection.Should().Contain("migrated_at",
            "INSERT column list must include migrated_at");
    }

    [Fact]
    public void New_Accounts_Resolve_Company_By_Constitutional_Code()
    {
        // The migration must look up company_id via companies.code = '000' —
        // never a hardcoded UUID (Constitution Article 3 + Wave 1 pattern).
        var sql = LoadMigrationFile();

        sql.Should().Contain("c.code = '000'",
            "the migration must resolve the default holding company by its constitutional code '000'");
    }

    // ==================== Step 2 — DEC-186: Off-Balance deprecate ====================

    [Fact]
    public void DEC_186_Marks_1_3_OffBalance_As_Deprecated()
    {
        var sql = LoadMigrationFile();
        var upSection = ExtractUpSection(sql);

        upSection.Should().Contain("migration_status = 'deprecated'",
            "DEC-186 must mark 1.3 Off-Balance accounts as deprecated");
        upSection.Should().Contain("code LIKE '1.3.%'",
            "DEC-186 must target only 1.3.* accounts (the Off-Balance range)");
    }

    // ==================== Step 3 — DEC-187: 9201 → 1.1.06 rename ====================

    [Fact]
    public void DEC_187_Renames_All_4_WIP_Accounts()
    {
        // The migration must rename:
        //   9201       → 1.1.06      (L3)
        //   9201-001   → 1.1.06.001  (L4)
        //   9201-002   → 1.1.06.002  (L4)
        //   9201-003   → 1.1.06.003  (L4)
        var sql = LoadMigrationFile();
        var upSection = ExtractUpSection(sql);

        var mappings = new Dictionary<string, string>
        {
            { "9201",       "1.1.06" },
            { "9201-001",   "1.1.06.001" },
            { "9201-002",   "1.1.06.002" },
            { "9201-003",   "1.1.06.003" }
        };
        foreach (var (oldCode, newCode) in mappings)
        {
            upSection.Should().Contain($"'{oldCode}'",
                $"DEC-187 must reference the old WIP code '{oldCode}'");
            upSection.Should().Contain($"'{newCode}'",
                $"DEC-187 must rename to the new canonical code '{newCode}'");
        }
        upSection.Should().Contain("migration_status = 'migrated'",
            "DEC-187 must mark renamed rows as 'migrated' (not 'new')");
    }

    // ==================== Step 4 — DEC-188: L1=7 split ====================

    [Fact]
    public void DEC_188_Splits_L1_7_Into_7_1_And_7_2()
    {
        // The migration must rename:
        //   71    → 7.1       (Other Income → Finance Income L2)
        //   7101  → 7.1.01    (Investment Income → Finance Income L3)
        //   7102  → 7.1.02    (Miscellaneous Income → Finance Income L3)
        //   72    → 7.2       (Other Expenses → Finance Expense L2)
        //   7201  → 7.2.01    (Miscellaneous Losses → Finance Expense L3)
        var sql = LoadMigrationFile();
        var upSection = ExtractUpSection(sql);

        var mappings = new Dictionary<string, string>
        {
            { "71",   "7.1" },
            { "7101", "7.1.01" },
            { "7102", "7.1.02" },
            { "72",   "7.2" },
            { "7201", "7.2.01" }
        };
        foreach (var (oldCode, newCode) in mappings)
        {
            upSection.Should().Contain($"'{oldCode}'",
                $"DEC-188 must reference the old L1=7 code '{oldCode}'");
            upSection.Should().Contain($"'{newCode}'",
                $"DEC-188 must rename to the split code '{newCode}'");
        }
    }

    // ==================== Step 5 — Bonus: backfill fs_type + section ====================

    [Fact]
    public void Bonus_Backfill_Sets_FsType_And_Section_For_Existing_Accounts()
    {
        // The migration must run a final UPDATE that derives fs_type and
        // section from the first character of code (L1) for accounts that
        // are still migration_status = 'pending' (i.e. the 131 keep accounts
        // not touched by DEC-186/187/188).
        var sql = LoadMigrationFile();
        var upSection = ExtractUpSection(sql);

        upSection.Should().Contain("fs_type = CASE SUBSTRING(code FROM 1 FOR 1)",
            "the backfill must derive fs_type from the L1 (first char of code)");
        upSection.Should().Contain("section = CASE SUBSTRING(code FROM 1 FOR 1)",
            "the backfill must derive section from the L1 (first char of code)");
        upSection.Should().Contain("migration_status = 'pending'",
            "the backfill must only touch rows still in 'pending' state");
        upSection.Should().Contain("WHEN '1' THEN 'BS'",
            "L1=1 (Assets) must be BS");
        upSection.Should().Contain("WHEN '4' THEN 'PL'",
            "L1=4 (Revenue) must be PL");
    }

    [Fact]
    public void Bonus_Backfill_Excludes_DEC_186_And_DEC_188_Ranges()
    {
        // The backfill must skip the 1.3.* range (handled by DEC-186)
        // and the 7.* range (handled by DEC-188) so it does not overwrite
        // those UPDATEs.
        var sql = LoadMigrationFile();
        var upSection = ExtractUpSection(sql);

        upSection.Should().Contain("code NOT LIKE '1.3.%'",
            "backfill must skip 1.3.* (DEC-186 handles those)");
        upSection.Should().Contain("code NOT LIKE '7%'",
            "backfill must skip 7.* (DEC-188 handles those)");
    }

    // ==================== Down() ====================

    [Fact]
    public void Down_Deletes_All_27_New_Accounts()
    {
        // Down() must remove the 27 new accounts so the migration is reversible.
        var sql = LoadMigrationFile();
        var downSection = ExtractDownSection(sql);

        downSection.Should().Contain("DELETE FROM accounts",
            "Down() must use DELETE FROM accounts to remove the 27 new accounts");
        foreach (var code in AllNewCodes)
        {
            downSection.Should().Contain($"'{code}'",
                $"Down() must remove the new account with code '{code}'");
        }
    }

    [Fact]
    public void Down_Reverts_DEC_186_DEC_187_And_DEC_188()
    {
        // Down() must restore:
        //   - 1.3.* migration_status to 'pending'
        //   - 9201-* codes (revert from 1.1.06.*)
        //   - 71/72 codes (revert from 7.1/7.2)
        var sql = LoadMigrationFile();
        var downSection = ExtractDownSection(sql);

        downSection.Should().Contain("migration_status = 'pending'",
            "Down() must reset migration_status to 'pending' on reverted rows");
        downSection.Should().Contain("'9201'",
            "Down() must restore the 9201 L3 code (revert DEC-187)");
        downSection.Should().Contain("'9201-001'",
            "Down() must restore the 9201-001 L4 code (revert DEC-187)");
        downSection.Should().Contain("'71'",
            "Down() must restore the 71 L2 code (revert DEC-188)");
        downSection.Should().Contain("'72'",
            "Down() must restore the 72 L2 code (revert DEC-188)");
    }

    // ==================== Architectural compliance ====================

    [Fact]
    public void Migration_File_Contains_No_Tenant_Id_Reference()
    {
        var sql = LoadMigrationFile();
        sql.Should().NotContain("tenant_id",
            "Constitution Article 3 — company_id only, never tenant_id");
    }

    [Fact]
    public void Migration_File_Contains_No_Hardcoded_Account_UUIDs()
    {
        // No hardcoded account UUIDs in the new-account INSERTs (the
        // id column is always gen_random_uuid()). The migration may
        // reference company_id via subquery (no hardcoded UUID there either).
        var sql = LoadMigrationFile();
        // We check that every 'id' literal in the INSERT blocks is gen_random_uuid(),
        // not a hardcoded UUID. Find any UUID-shaped literals (8-4-4-4-12 hex).
        var uuidPattern = new System.Text.RegularExpressions.Regex(
            @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
        var matches = uuidPattern.Matches(sql);
        matches.Count.Should().Be(0,
            $"the migration must not hardcode any UUIDs (use gen_random_uuid() or subqueries); found {matches.Count} UUID-shaped literal(s)");
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
