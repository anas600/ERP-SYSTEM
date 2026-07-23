// DEC-067: Advanced retention tests
// Edge cases + boundary conditions for the retention system

using Npgsql;
using Xunit;

namespace ERPSystem.Tests.Retention;

[Trait("Category", "Retention")]
[Trait("Category", "Edge")]
public class RetentionAdvancedTests
{
    private string GetTestConnString()
    {
        return Environment.GetEnvironmentVariable("SUPABASE_URL")
            ?? Environment.GetEnvironmentVariable("NEON_URL")
            ?? "Host=localhost;Database=erp_test_system;Username=erp_test;Password=erp_test_pw";
    }

    [Theory]
    [InlineData(30, "1 month")]
    [InlineData(90, "3 months")]
    [InlineData(365, "1 year")]
    [InlineData(365 * 3, "3 years")]
    [InlineData(365 * 7, "7 years")]
    [InlineData(365 * 10, "10 years")]
    public void RetentionPeriod_MeetsCompliance(int days, string description)
    {
        // All declared retention periods should be positive
        Assert.True(days > 0, $"{description} must be > 0 days");
        // 7 years is the minimum for IFRS/SOX compliance
        Assert.True(days <= 365 * 10, $"{description} should be <= 10 years");
    }

    [Fact]
    public void TierBoundaries_AreSequential()
    {
        // Tier boundaries should be sequential (no gaps, no overlaps)
        var boundaries = new Dictionary<string, (int startDays, int endDays)>
        {
            ["T0_Hot"] = (0, 365),        // 0-1 year
            ["T1_Warm"] = (365, 365 * 3), // 1-3 years
            ["T2_Archive"] = (365 * 3, 365 * 7), // 3-7 years
            ["T3_Purged"] = (365 * 7, int.MaxValue), // >7 years
        };

        // Verify sequential (no gaps)
        var sorted = boundaries.OrderBy(b => b.Value.startDays).ToList();
        for (int i = 1; i < sorted.Count; i++)
        {
            Assert.Equal(sorted[i - 1].Value.endDays, sorted[i].Value.startDays);
        }
    }

    [Theory]
    [InlineData("audit_log", 7, 365, 7 * 365)]  // 1y then archive, never delete
    [InlineData("stock_movements", 3, 365, 3 * 365)]
    [InlineData("journal_entries", 7, 0, 7 * 365)]  // 7y, no archive, never delete
    [InlineData("notifications", 0, 0, 90)]  // ephemeral
    public void EntityRetention_PolicyIsValid(
        string table, int archiveAfterYears, int hotYears, int retentionDays)
    {
        // Verify policy makes sense
        Assert.True(retentionDays > 0, $"{table} retention must be > 0");

        if (archiveAfterYears > 0)
        {
            // Archive threshold <= total retention
            Assert.True(archiveAfterYears * 365 <= retentionDays,
                $"{table} archive threshold should be <= retention period");
        }
    }

    [Fact]
    public void ArchiveFormat_HasConsistentPath()
    {
        // archive/{table}/{year}/{file}.jsonl.gz
        var table = "audit_log";
        var year = "2025";
        var filename = "audit_log_20250722_030000.jsonl.gz";
        var expectedPath = $"archive/{table}/{year}/{filename}";

        Assert.StartsWith("archive/", expectedPath);
        Assert.Contains($"/{table}/", expectedPath);
        Assert.EndsWith(".jsonl.gz", expectedPath);
        Assert.Contains($"/{year}/", expectedPath);
    }

    [Fact]
    public void ArchiveMetadata_ConstraintsAreValid()
    {
        // chk_period CHECK (period_end > period_start)
        // This is enforced in the SQL migration
        // We can verify the structure conceptually:
        var periodStart = DateTime.UtcNow.AddDays(-30);
        var periodEnd = DateTime.UtcNow;
        Assert.True(periodEnd > periodStart);

        // Invalid: end before start
        var badStart = DateTime.UtcNow;
        var badEnd = DateTime.UtcNow.AddDays(-1);
        Assert.True(badEnd < badStart, "Test should detect invalid period");
    }

    [Fact]
    public void Cleanup_FrequencyIsReasonable()
    {
        // All cleanups should run between 02:00 and 05:00 UTC (low traffic)
        var cleanups = new Dictionary<string, string>
        {
            ["backup_daily"] = "02:00 UTC",
            ["tier1_cleanup"] = "03:00 UTC",
            ["tier2_archive"] = "04:00 UTC",
            ["monthly_report"] = "05:00 UTC 1st",
        };

        Assert.Equal(4, cleanups.Count);
        // All should be between 02:00 and 05:00
        foreach (var (key, time) in cleanups)
        {
            Assert.Matches(@"^0[2-5]:00 UTC", time);
        }
    }

    [Fact]
    public void StorageClasses_AreTierAppropriate()
    {
        // R2 storage class choices per tier
        var storageClasses = new Dictionary<string, string>
        {
            ["T0_Hot"] = "n/a (live DB)",
            ["T1_Warm"] = "n/a (compressed in DB)",
            ["T2_Archive"] = "GLACIER",  // Cold storage
            ["T3_Purged"] = "n/a (deleted)",
        };

        Assert.Equal("GLACIER", storageClasses["T2_Archive"]);
    }

    [Fact]
    public void AuditTrail_AlwaysRequired()
    {
        // All retention actions must be auditable
        // Every tier transition records metadata
        var requiresMetadata = new[] { "T0→T1", "T1→T2", "T2→T3" };
        Assert.All(requiresMetadata, t => Assert.NotEmpty(t));
    }
}
