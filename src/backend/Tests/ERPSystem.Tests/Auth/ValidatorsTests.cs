using ERPSystem.Modules.Identity.Application.Auth;
using FluentAssertions;

namespace ERPSystem.Tests.Auth;

/// <summary>
/// Phase 6.1c: Multi-Company model. The RegisterRequest no longer has
/// companyId/TenantName fields (the Holding Company is auto-seeded and
/// the new user is auto-linked). The validator now only checks the
/// email/password/fullName fields.
/// </summary>
public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var req = new RegisterRequest
        {
            Email = "user@example.com",
            Password = "Strong1Pass",
            FullName = "Test User"
        };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("user@")]
    public void InvalidEmail_FailsValidation(string email)
    {
        var req = new RegisterRequest
        {
            Email = email,
            Password = "Strong1Pass",
            FullName = "Test"
        };
        _validator.Validate(req).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("short1A")]        // < 8 chars
    [InlineData("alllowercase1")]  // no uppercase
    [InlineData("ALLUPPERCASE1")]  // no lowercase
    [InlineData("NoDigitsHere")]   // no digit
    public void WeakPassword_FailsValidation(string password)
    {
        var req = new RegisterRequest
        {
            Email = "user@example.com",
            Password = password,
            FullName = "Test"
        };
        _validator.Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void MissingEmail_FailsValidation()
    {
        var req = new RegisterRequest
        {
            Email = "",
            Password = "Strong1Pass",
            FullName = "Test"
        };
        _validator.Validate(req).IsValid.Should().BeFalse();
    }
}
