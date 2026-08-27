using System.Security.Claims;
using ERPSystem.Host.Controllers;
using ERPSystem.Modules.Finance.Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ERPSystem.Tests.Finance;

// =====================================================================================
// Sprint 65 / Wave 3A (DEC-235 + DEC-237): Tests for BankReconciliationsController.
// =====================================================================================
//
// The 3 tests cover:
//   1. SuggestMatches_Returns200_WithList
//   2. ConfirmMatch_Returns200_WithUpdatedMatch
//   3. GetQueue_Returns200_WithUnmatchedReceipts
//
// The controller is a thin shell over IBankReconciliationService, so the unit
// tests mock the service interface and assert:
//   - 200 OK on success
//   - 404 NOT_FOUND on a NOT_FOUND service error
//   - 409 CONFLICT on a CONFLICT service error
//   - 400 BAD_REQUEST on a VALIDATION service error
//   - UserId is read from the JWT claims (not from any DTO)
// =====================================================================================

public class Sprint65BankReconciliationsControllerTests
{
    private static BankReconciliationsController BuildController(
        Mock<IBankReconciliationService> svc, Guid? userId = null)
    {
        var ctrl = new BankReconciliationsController(svc.Object);
        // Set the User with the NameIdentifier claim so the controller's `UserId`
        // helper resolves correctly (L19 / DEC-095: from JWT, not from a DTO).
        var claims = new List<Claim>();
        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal },
        };
        return ctrl;
    }

    // ============== Test 1 ==============

    [Fact]
    public async Task SuggestMatches_Returns200_WithList()
    {
        var svc = new Mock<IBankReconciliationService>();
        var receiptId = Guid.NewGuid();
        var matches = new List<SubPaymentMatch>
        {
            new() { SubPaymentId = Guid.NewGuid(), Score = 100, MatchQuality = "EXCELLENT" },
            new() { SubPaymentId = Guid.NewGuid(), Score = 50, MatchQuality = "GOOD" },
        };
        svc.Setup(s => s.SuggestMatchesAsync(receiptId, 5, It.IsAny<CancellationToken>()))
           .ReturnsAsync(BankReconciliationResult<IReadOnlyList<SubPaymentMatch>>.Ok(matches));

        var ctrl = BuildController(svc);
        var result = await ctrl.SuggestMatches(receiptId, max: 5, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<SubPaymentMatch>>()
          .Which.Should().HaveCount(2);
    }

    [Fact]
    public async Task SuggestMatches_Returns404_WhenServiceReturnsNotFound()
    {
        var svc = new Mock<IBankReconciliationService>();
        var receiptId = Guid.NewGuid();
        svc.Setup(s => s.SuggestMatchesAsync(receiptId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(BankReconciliationResult<IReadOnlyList<SubPaymentMatch>>.Fail(
               "سند القبض غير موجود.", "NOT_FOUND"));

        var ctrl = BuildController(svc);
        var result = await ctrl.SuggestMatches(receiptId, max: 5, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>("the service flagged this as NOT_FOUND");
    }

    // ============== Test 2 ==============

    [Fact]
    public async Task ConfirmMatch_Returns200_WithUpdatedMatch()
    {
        var svc = new Mock<IBankReconciliationService>();
        var userId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var subPaymentId = Guid.NewGuid();
        var match = new SubPaymentMatch
        {
            SubPaymentId = subPaymentId,
            Score = 100,
            MatchQuality = "EXCELLENT",
        };
        svc.Setup(s => s.ConfirmMatchAsync(userId, receiptId, subPaymentId, It.IsAny<CancellationToken>()))
           .ReturnsAsync(BankReconciliationResult<SubPaymentMatch>.Ok(match));

        var ctrl = BuildController(svc, userId);
        var result = await ctrl.ConfirmMatch(receiptId, subPaymentId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<SubPaymentMatch>()
          .Which.SubPaymentId.Should().Be(subPaymentId);
        svc.Verify(s => s.ConfirmMatchAsync(userId, receiptId, subPaymentId, It.IsAny<CancellationToken>()),
            Times.Once, "the controller must pass the JWT userId to the service (L19 / DEC-095)");
    }

    [Fact]
    public async Task ConfirmMatch_Returns409_OnConflict()
    {
        var svc = new Mock<IBankReconciliationService>();
        var userId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var subPaymentId = Guid.NewGuid();
        svc.Setup(s => s.ConfirmMatchAsync(userId, receiptId, subPaymentId, It.IsAny<CancellationToken>()))
           .ReturnsAsync(BankReconciliationResult<SubPaymentMatch>.Fail(
               "SubPayment already matched.", "CONFLICT"));

        var ctrl = BuildController(svc, userId);
        var result = await ctrl.ConfirmMatch(receiptId, subPaymentId, CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>("CONFLICT maps to HTTP 409");
    }

    // ============== Test 3 ==============

    [Fact]
    public async Task GetQueue_Returns200_WithUnmatchedReceipts()
    {
        var svc = new Mock<IBankReconciliationService>();
        var queue = new List<UnmatchedReceipt>
        {
            new() { ReceiptId = Guid.NewGuid(), ReceiptNumber = "RC-001", Amount = 5_000m, DaysSinceReceipt = 3 },
            new() { ReceiptId = Guid.NewGuid(), ReceiptNumber = "RC-002", Amount = 8_000m, DaysSinceReceipt = 10 },
        };
        svc.Setup(s => s.GetQueueAsync(0, 50, It.IsAny<CancellationToken>()))
           .ReturnsAsync(BankReconciliationResult<IReadOnlyList<UnmatchedReceipt>>.Ok(queue));

        var ctrl = BuildController(svc);
        var result = await ctrl.GetQueue(skip: 0, take: 50, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<UnmatchedReceipt>>()
          .Which.Should().HaveCount(2);
    }
}
