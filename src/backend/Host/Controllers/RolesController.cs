using System.Security.Claims;
using Dapper;
using ERPSystem.Host.Authorization;
using ERPSystem.Modules.Identity.Infrastructure;
using ERPSystem.Shared.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Host.Controllers;

/// <summary>
/// Identity Management API — User CRUD (Admin only) + Role listing.
/// Phase 6.2: built on top of the new Multi-Company model.
/// </summary>
[ApiController]
[Route("api/identity")]
[Authorize(Policy = ERPSystem.Host.Auth.PolicyNames.AdminOnly)]
[RequirePermission("identity.users.view")]
public class RolesController : ControllerBase
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<RolesController> _logger;

    public RolesController(
        IUserRepository users, IRoleRepository roles, IDbConnectionFactory db,
        ILogger<RolesController> logger)
    {
        _users = users; _roles = roles; _db = db; _logger = logger;
    }

    private Guid? CallerUserId
    {
        get
        {
            var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(sub, out var g) ? g : null;
        }
    }

    // ============ Users CRUD ============

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers(
        [FromQuery] int skip = 0, [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var users = await _users.ListAsync(skip, take, ct);
        var total = await _users.CountAsync(ct);
        return Ok(new { count = total, items = users });
    }

    public sealed class CreateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public List<Guid>? RoleIds { get; set; }
        public Guid? DefaultCompanyId { get; set; }
    }

    public sealed class UpdateUserRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public bool? IsActive { get; set; }
        public List<Guid>? RoleIds { get; set; }
        public Guid? DefaultCompanyId { get; set; }
    }

    public sealed class AdminResetPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { error = "Email and password are required." });
        if (await _users.EmailExistsAsync(req.Email, ct))
            return BadRequest(new { error = "Email already exists." });

        // Use AuthService.Register to ensure password hashing and role assignment
        // but we'll do it inline here for simplicity
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);
        var userId = Guid.NewGuid();

        await conn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO users (id, email, password_hash, full_name, is_active, two_factor_enabled, created_at, updated_at)
            VALUES (@Id, @Email, @PasswordHash, @FullName, true, false, @Now, @Now)",
            new
            {
                Id = userId,
                Email = req.Email,
                PasswordHash = passwordHash,
                FullName = req.FullName,
                Now = DateTime.UtcNow
            }, cancellationToken: ct));

        // Assign roles
        if (req.RoleIds != null && req.RoleIds.Count > 0)
        {
            await _users.SetUserRolesAsync(userId, req.RoleIds, ct);
        }

        // Assign default company
        if (req.DefaultCompanyId.HasValue)
        {
            await _users.AssignUserToCompanyAsync(userId, req.DefaultCompanyId.Value, true, ct);
        }

        var user = await _users.GetByIdAsync(userId, ct);
        return CreatedAtAction(nameof(GetUser), new { id = userId }, user);
    }

    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user == null) return NotFound();
        var roles = await _users.GetUserRoleIdsAsync(id, ct);
        var companies = await _users.GetUserCompaniesAsync(id, ct);
        return Ok(new { user, roleIds = roles, companies });
    }

    [HttpPut("users/{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest req, CancellationToken ct)
    {
        var existing = await _users.GetByIdAsync(id, ct);
        if (existing == null) return NotFound();

        await _users.UpdateProfileAsync(id, req.FullName, req.Email, req.IsActive, ct);
        if (req.RoleIds != null)
        {
            await _users.SetUserRolesAsync(id, req.RoleIds, ct);
        }
        if (req.DefaultCompanyId.HasValue)
        {
            await _users.AssignUserToCompanyAsync(id, req.DefaultCompanyId.Value, true, ct);
        }
        return Ok(new { message = "User updated." });
    }

    [HttpPut("users/{id:guid}/password")]
    public async Task<IActionResult> AdminResetPassword(Guid id, [FromBody] AdminResetPasswordRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 6)
            return BadRequest(new { error = "Password must be at least 6 characters." });

        var hash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        await _users.UpdatePasswordAsync(id, hash, ct);
        return Ok(new { message = "Password reset successfully." });
    }

    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken ct)
    {
        await _users.DeleteAsync(id, ct);
        return NoContent();
    }

    // ============ Roles ============

    [HttpGet("roles")]
    public async Task<IActionResult> ListRoles(CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        const string sql = "SELECT id, name, description FROM roles ORDER BY name";
        var roles = await conn.QueryAsync<RoleListItem>(new CommandDefinition(sql, cancellationToken: ct));
        return Ok(roles.AsList());
    }

    private sealed class RoleListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
