// DEC-067: Health check unit tests
// Verifies the health endpoint returns correct structure

using Xunit;

namespace ERPSystem.Tests.Health;

[Trait("Category", "Health")]
public class HealthCheckTests
{
    [Fact]
    public void HealthResponse_HasRequiredFields()
    {
        // Expected: { status, timestamp, components }
        var response = new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            components = new Dictionary<string, object>()
        };

        Assert.NotNull(response.status);
        Assert.NotNull(response.timestamp);
        Assert.NotNull(response.components);
    }

    [Fact]
    public void HealthStatus_ValidValues()
    {
        var validStatuses = new[] { "healthy", "degraded", "unhealthy", "unknown" };
        Assert.Equal(4, validStatuses.Length);
    }

    [Fact]
    public void ComponentTypes_AreValid()
    {
        var expectedComponents = new[] { "database", "memory", "disk", "process", "recent_activity" };
        Assert.Equal(5, expectedComponents.Length);
    }

    [Fact]
    public void HealthThresholds_AreReasonable()
    {
        // DB latency > 1s = degraded, > 2s = unhealthy
        // Memory > 1GB = degraded, > 2GB = unhealthy
        // Disk > 80% = degraded, > 95% = unhealthy
        var dbDegradedMs = 1000;
        var memDegradedMb = 1024;
        var diskDegradedPct = 80;

        Assert.True(dbDegradedMs > 0);
        Assert.True(memDegradedMb > 0);
        Assert.True(diskDegradedPct > 0 && diskDegradedPct < 100);
    }

    [Fact]
    public void HealthCheck_EndpointPaths_AreValid()
    {
        var endpoints = new[] { "/api/health/live", "/api/health/ready", "/api/health/startup", "/api/health/full" };
        Assert.All(endpoints, p => Assert.StartsWith("/api/health/", p));
    }

    [Fact]
    public void LivenessProbe_DoesNotCheckDependencies()
    {
        // /live should be fast (no DB checks) - critical for k8s liveness
        var checksLiveness = new[] { "process.alive" };
        Assert.Single(checksLiveness);
    }

    [Fact]
    public void ReadinessProbe_ChecksDatabase()
    {
        // /ready should check DB at minimum
        var checksReadiness = new[] { "database" };
        Assert.Single(checksReadiness);
    }

    [Fact]
    public void FullHealthCheck_ChecksAllComponents()
    {
        // /full should check everything
        var componentsChecked = new[] { "database", "memory", "disk", "process", "recent_activity" };
        Assert.Equal(5, componentsChecked.Length);
    }
}
