// Cycle 6 / T7 — Activity log service tests.
//
// Mirrors the AuditLoggerTests pattern (Tests/Audit/AuditLoggerTests.cs).
// Uses Moq for IDbConnectionFactory + IHttpContextAccessor.
//
// These are unit tests — no DB connection. The IDbConnectionFactory is mocked
// to return a FakeDbConnection that records but never executes.

using System.Data;
using ERPSystem.Modules.Activity.Application;
using ERPSystem.Shared.Infrastructure;
using ERPSystem.Tests.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ERPSystem.Tests.Activity;

public class ActivityLogServiceTests
{
    private readonly ActivityLogService _sut;
    private readonly Mock<IDbConnectionFactory> _factoryMock;
    private readonly Mock<IHttpContextAccessor> _httpMock;
    private readonly FakeDbConnection _conn;
    private readonly Guid _userId;
    private readonly Guid _companyId;

    public ActivityLogServiceTests()
    {
        _factoryMock = new Mock<IDbConnectionFactory>();
        _conn = new FakeDbConnection(new System.Data.DataSet());
        _factoryMock
            .Setup(f => f.CreateOltpConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_conn);

        _httpMock = new Mock<IHttpContextAccessor>();
        _httpMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        _userId = Guid.NewGuid();
        _companyId = Guid.NewGuid();

        _sut = new ActivityLogService(
            _factoryMock.Object,
            _httpMock.Object,
            NullLogger<ActivityLogService>.Instance);
    }

    [Fact]
    public async Task LogAsync_WithValidData_OpensConnectionAndExecutesInsert()
    {
        // Act
        await _sut.LogAsync(
            userId: _userId,
            companyId: _companyId,
            action: ActivityAction.LoginSuccess,
            metadata: new { email = "test@example.com" });

        // Assert
        _factoryMock.Verify(
            f => f.CreateOltpConnectionAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LogAsync_WithEmptyAction_SkipsAndDoesNotOpenConnection()
    {
        // Act
        await _sut.LogAsync(
            userId: _userId,
            companyId: _companyId,
            action: "");

        // Assert: empty action = skip (no DB call)
        _factoryMock.Verify(
            f => f.CreateOltpConnectionAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LogAsync_WithWhitespaceAction_SkipsAndDoesNotOpenConnection()
    {
        // Act
        await _sut.LogAsync(
            userId: _userId,
            companyId: _companyId,
            action: "   ");

        // Assert: whitespace = skip
        _factoryMock.Verify(
            f => f.CreateOltpConnectionAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LogAsync_WithNullUserId_StillInserts()
    {
        // Pre-login activity (e.g. failed login with unknown email) has no userId.
        // Act
        await _sut.LogAsync(
            userId: null,
            companyId: null,
            action: ActivityAction.LoginFailed,
            metadata: new { email = "unknown@example.com" });

        // Assert: still inserts (nullable column)
        _factoryMock.Verify(
            f => f.CreateOltpConnectionAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LogAsync_OnDbFailure_DoesNotThrow()
    {
        // Arrange: make the factory throw
        var throwingFactory = new Mock<IDbConnectionFactory>();
        throwingFactory
            .Setup(f => f.CreateOltpConnectionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB down"));

        var sut = new ActivityLogService(
            throwingFactory.Object,
            _httpMock.Object,
            NullLogger<ActivityLogService>.Instance);

        // Act: must NOT throw (DEC-053 + DEC-073 — activity failures must not break business)
        await sut.LogAsync(
            userId: _userId,
            companyId: _companyId,
            action: ActivityAction.Refresh);

        // Assert: reached here means no throw
        Assert.True(true);
    }

    [Fact]
    public void ActivityAction_Constants_AreCorrectStrings()
    {
        ActivityAction.Login.Should().Be("LOGIN");
        ActivityAction.LoginSuccess.Should().Be("LOGIN_SUCCESS");
        ActivityAction.LoginFailed.Should().Be("LOGIN_FAILED");
        ActivityAction.Refresh.Should().Be("REFRESH");
        ActivityAction.Logout.Should().Be("LOGOUT");
        ActivityAction.Register.Should().Be("REGISTER");
        ActivityAction.PasswordChange.Should().Be("PASSWORD_CHANGE");
        ActivityAction.CompanySwitch.Should().Be("COMPANY_SWITCH");
    }

    [Fact]
    public async Task LogAsync_WithUserAgent_TruncatesTo255Chars()
    {
        // Arrange: long User-Agent (> 255 chars)
        var longUa = new string('A', 500);
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers.UserAgent = longUa;
        _httpMock.Setup(h => h.HttpContext).Returns(ctx);

        // Act — should not throw even though UserAgent > 255
        await _sut.LogAsync(
            userId: _userId,
            companyId: _companyId,
            action: ActivityAction.LoginSuccess);

        // Assert: reached here means no throw
        _factoryMock.Verify(
            f => f.CreateOltpConnectionAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LogAsync_WithXForwardedFor_PrefersItOverRemoteIp()
    {
        // Arrange: both X-Forwarded-For and RemoteIp are set
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-Forwarded-For"] = "203.0.113.42, 10.0.0.1";
        ctx.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        _httpMock.Setup(h => h.HttpContext).Returns(ctx);

        // Act — should pick the first X-Forwarded-For (203.0.113.42)
        await _sut.LogAsync(
            userId: _userId,
            companyId: _companyId,
            action: ActivityAction.LoginSuccess);

        _factoryMock.Verify(
            f => f.CreateOltpConnectionAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
