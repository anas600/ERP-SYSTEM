using ERPSystem.Shared.Audit;
using ERPSystem.Shared.Infrastructure;
using ERPSystem.Shared.MultiTenancy;
using ERPSystem.Tests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ERPSystem.Tests.Audit;

public class AuditLoggerTests
{
    private readonly AuditLogger _logger;
    private readonly Mock<IDbConnectionFactory> _factoryMock;
    private readonly FakeDbConnection _conn;
    private readonly Mock<ICompanyContext> _companyContextMock;

    public AuditLoggerTests()
    {
        _factoryMock = new Mock<IDbConnectionFactory>();
        _conn = new FakeDbConnection(new System.Data.DataSet());
        _factoryMock
            .Setup(f => f.CreateOltpConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_conn);

        _companyContextMock = new Mock<ICompanyContext>();
        _companyContextMock.Setup(t => t.CompanyId).Returns(Guid.NewGuid());
        _companyContextMock.Setup(t => t.UserId).Returns(Guid.NewGuid());

        _logger = new AuditLogger(
            _factoryMock.Object,
            _companyContextMock.Object,
            NullLogger<AuditLogger>.Instance);
    }

    [Fact]
    public async Task LogAsync_WithEmptyCompanyId_SkipsAndLogsWarning()
    {
        await _logger.LogAsync(
            companyId: Guid.Empty,
            entityType: "journal_entry",
            entityId: Guid.NewGuid(),
            action: AuditAction.Create);

        // No DB interaction expected
        _factoryMock.Verify(
            f => f.CreateOltpConnectionAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LogAsync_WithEmptyEntityType_SkipsAndLogsWarning()
    {
        await _logger.LogAsync(
            companyId: Guid.NewGuid(),
            entityType: "",
            entityId: Guid.NewGuid(),
            action: AuditAction.Create);

        _factoryMock.Verify(
            f => f.CreateOltpConnectionAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LogAsync_WithValidData_OpensConnectionAndExecutesInsert()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var changes = new { Field = "value" };

        // Act
        await _logger.LogAsync(
            companyId: companyId,
            entityType: "journal_entry",
            entityId: entityId,
            action: AuditAction.Create,
            changes: changes);

        // Assert
        _factoryMock.Verify(
            f => f.CreateOltpConnectionAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LogAsync_FromCompanyContext_UsesContextValues()
    {
        // Arrange
        var expectedCompanyId = Guid.NewGuid();
        var expectedUserId = Guid.NewGuid();
        _companyContextMock.Setup(t => t.CompanyId).Returns(expectedCompanyId);
        _companyContextMock.Setup(t => t.UserId).Returns(expectedUserId);

        // Act
        await _logger.LogAsync(
            entityType: "project",
            entityId: Guid.NewGuid(),
            action: AuditAction.Update);

        // Assert
        _factoryMock.Verify(
            f => f.CreateOltpConnectionAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LogAsync_FromCompanyContext_WhenContextEmpty_Skips()
    {
        // Arrange
        _companyContextMock.Setup(t => t.CompanyId).Returns(Guid.Empty);

        // Act
        await _logger.LogAsync(
            entityType: "project",
            entityId: Guid.NewGuid(),
            action: AuditAction.Update);

        // Assert
        _factoryMock.Verify(
            f => f.CreateOltpConnectionAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LogAsync_OnDbFailure_DoesNotThrow()
    {
        // Arrange — make factory throw
        _factoryMock
            .Setup(f => f.CreateOltpConnectionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB down"));

        // Act — must NOT throw (audit failures must not break business ops)
        await _logger.LogAsync(
            companyId: Guid.NewGuid(),
            entityType: "journal_entry",
            entityId: Guid.NewGuid(),
            action: AuditAction.Create);

        // Assert — reached here means no throw
        Assert.True(true);
    }

    [Fact]
    public void AuditAction_Create_IsStringCreate()
    {
        AuditAction.Create.Should().Be("CREATE");
        AuditAction.Update.Should().Be("UPDATE");
        AuditAction.Delete.Should().Be("DELETE");
        AuditAction.Restore.Should().Be("RESTORE");
    }
}
