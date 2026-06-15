using System.Security.Claims;
using ERPSystem.Modules.Identity.Application.Auth;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshValidator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshValidator,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
        _logger = logger;
    }

    /// <summary>تسجيل مستخدم جديد (داخل tenant موجود أو إنشاء tenant جديد)</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var validation = await _registerValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))
            });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.RegisterAsync(request, ip, ct);

        if (!result.Succeeded)
        {
            _logger.LogWarning("فشل تسجيل المستخدم {Email}: {Error}", request.Email, result.Error);
            return BadRequest(new ProblemDetails
            {
                Title = "Registration Failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = result.Error,
            });
        }

        return Ok(result.Response);
    }

    /// <summary>تسجيل دخول</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var validation = await _loginValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))
            });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.LoginAsync(request, ip, ct);

        if (!result.Succeeded)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication Failed",
                Status = StatusCodes.Status401Unauthorized,
                Detail = result.Error,
            });
        }

        return Ok(result.Response);
    }

    /// <summary>تجديد Access Token</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var validation = await _refreshValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))
            });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _authService.RefreshAsync(request, ip, ct);

        if (!result.Succeeded)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Refresh Failed",
                Status = StatusCodes.Status401Unauthorized,
                Detail = result.Error,
            });
        }

        return Ok(result.Response);
    }

    /// <summary>إلغاء Refresh Token (logout)</summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _authService.RevokeAsync(userId, request.RefreshToken, ip, ct);
        }
        return NoContent();
    }

    /// <summary>معلومات المستخدم الحالي (للـ client)</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
    public IActionResult Me()
    {
        return Ok(new UserInfo
        {
            Id = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")!.Value),
            TenantId = Guid.Parse(User.FindFirst("tenant_id")!.Value),
            Email = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value ?? string.Empty,
            FullName = User.FindFirst("full_name")?.Value ?? string.Empty,
            Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
        });
    }
}
