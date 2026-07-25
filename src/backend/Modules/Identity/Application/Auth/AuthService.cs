using System.Data;
using System.Security.Claims;
using ERPSystem.Modules.Companies.Infrastructure;
using ERPSystem.Modules.Identity.Entities;
using ERPSystem.Modules.Identity.Infrastructure;
using ERPSystem.Shared.Infrastructure;

namespace ERPSystem.Modules.Identity.Application.Auth;

/// <summary>
/// Phase 6.1c: Multi-Company model. Register creates the first user under the
/// default Holding Company (which is auto-seeded at startup by
/// <c>DefaultHoldingBootstrapHostedService</c>). The user is linked to the
/// Holding via the <c>user_companies</c> join table with <c>is_default = true</c>.
///
/// Atomicity: the entire register flow runs inside a single connection +
/// single transaction (DEC-091). Any step failure (or HF Space proxy timeout
/// that drops the connection) triggers automatic rollback, so the database
/// never ends up with orphan users. (The failure mode is now "no orphan
/// users" — not "no orphan tenants" — because tenants are no longer
/// created at register time.)
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IJwtTokenService _jwt;
    private readonly ICompanyRepository _companies;
    private readonly ILogger<AuthService> _logger;
    private readonly IDbConnectionFactory _db; // P1-9: needed for the single-conn register tx
    private readonly Guid _holdingCompanyId;

    public AuthService(
        IUserRepository u,
        IRoleRepository r,
        IRefreshTokenRepository rt,
        IJwtTokenService j,
        ICompanyRepository companies,
        ILogger<AuthService> l,
        IDbConnectionFactory db,
        IConfiguration config)
    {
        _users = u; _roles = r; _refreshTokens = rt; _jwt = j;
        _companies = companies; _logger = l; _db = db;
        // Phase 6.1c: Holding Company is fixed per deployment (single Holding).
        // Read from config (appsettings.json) — defaulting to the canonical
        // Phase 6.0 fixed UUID if not set.
        _holdingCompanyId = Guid.Parse(
            config["MultiCompany:HoldingCompanyId"]
            ?? "00000000-0000-0000-0000-000000000001");
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest req, string? ip, CancellationToken ct)
    {
        using var conn = await _db.CreateOltpConnectionAsync(ct);
        using var tx = conn.BeginTransaction();
        try
        {
            // 1. Validate email is not already taken
            if (await _users.GetByEmailAsync(req.Email, ct) != null)
                return AuthResult.Fail("البريد مستخدم.", AuthErrorCode.UserAlreadyExists);

            // 2. Verify the Holding Company exists (seeded by DefaultHoldingBootstrapHostedService)
            var holding = await _companies.GetByIdAsync(_holdingCompanyId, ct);
            if (holding == null)
                return AuthResult.Fail("الشركة القابضة غير مهيأة. حاول مرة أخرى بعد قليل.", AuthErrorCode.InternalError);

            // 3. Create the user
            var now = DateTime.UtcNow;
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = req.Email.Trim().ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password, 12),
                FullName = req.FullName.Trim(),
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            await _users.InsertAsync(user, conn, tx, ct);

            // 4. Ensure default roles exist + assign Admin
            await _roles.EnsureDefaultRolesAsync(conn, tx, ct);
            var admin = await _roles.GetByNameAsync("Admin", conn, tx, ct);
            if (admin != null)
                await _users.AssignRoleAsync(user.Id, admin.Id, conn, tx, ct);

            // 5. Link user to the Holding (default company)
            await _users.AssignUserToCompanyAsync(user.Id, _holdingCompanyId, isDefault: true, conn, tx, ct);

            // 6. Build the response (also inside the tx — refresh token rolls back if anything throws)
            var response = await BuildAsync(user, _holdingCompanyId, ip, conn, tx, ct);
            tx.Commit();
            return AuthResult.Ok(response);
        }
        catch
        {
            try { tx.Rollback(); } catch { /* best-effort */ }
            throw;
        }
    }

    public async Task<AuthResult> LoginAsync(LoginRequest req, string? ip, CancellationToken ct)
    {
        var user = await _users.GetByEmailAsync(req.Email, ct);
        if (user == null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return AuthResult.Fail("بيانات الدخول غير صحيحة.", AuthErrorCode.InvalidCredentials);

        var defaultCompany = await _users.GetDefaultCompanyAsync(user.Id, ct);
        if (defaultCompany == null)
            return AuthResult.Fail("المستخدم غير مربوط بأي شركة. تواصل مع الإدارة.", AuthErrorCode.NoCompaniesAssigned);

        await _users.UpdateLastLoginAsync(user.Id, DateTime.UtcNow, ct);
        return AuthResult.Ok(await BuildAsync(user, _holdingCompanyId, ip, ct));
    }

    public async Task<AuthResult> RefreshAsync(RefreshTokenRequest req, string? ip, CancellationToken ct)
    {
        var principal = _jwt.GetPrincipalFromExpiredToken(req.AccessToken);
        if (principal == null) return AuthResult.Fail("Access Token غير صالح.", AuthErrorCode.InvalidRefreshToken);
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? principal.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId)) return AuthResult.Fail("بيانات التوكن غير مكتملة.", AuthErrorCode.InvalidRefreshToken);
        var stored = await _refreshTokens.GetByHashAsync(_jwt.HashRefreshToken(req.RefreshToken), ct);
        if (stored == null || stored.UserId != userId) return AuthResult.Fail("Refresh Token غير صالح.", AuthErrorCode.InvalidRefreshToken);
        if (stored.IsRevoked) { await _refreshTokens.RevokeAllForUserAsync(userId, "Reuse of revoked", ip, ct); return AuthResult.Fail("تم اكتشاف محاولة اختراق.", AuthErrorCode.RefreshTokenRevoked); }
        if (stored.IsExpired) return AuthResult.Fail("Refresh Token منتهي.", AuthErrorCode.RefreshTokenExpired);
        var user = await _users.GetByIdAsync(userId, ct);
        if (user == null || !user.IsActive) return AuthResult.Fail("المستخدم غير مفعّل.", AuthErrorCode.UserInactive);

        var (newRt, newRtHash, newRtExp) = _jwt.GenerateRefreshToken();
        await _refreshTokens.RevokeAsync(stored, "Rotated", newRtHash, ip, ct);
        await _refreshTokens.InsertAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = newRtHash,
            ExpiresAt = newRtExp,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ip
        }, ct);

        return AuthResult.Ok(await BuildAsync(user, _holdingCompanyId, ip, ct));
    }

    public async Task RevokeAsync(Guid userId, string refreshToken, string? ip, CancellationToken ct)
    {
        var stored = await _refreshTokens.GetByHashAsync(_jwt.HashRefreshToken(refreshToken), ct);
        if (stored != null && stored.UserId == userId && stored.IsActive)
            await _refreshTokens.RevokeAsync(stored, "User logout", null, ip, ct);
    }

    public async Task<GetUserCompaniesResponse> GetUserCompaniesAsync(Guid userId, CancellationToken ct)
    {
        var links = await _users.GetUserCompaniesAsync(userId, ct);
        if (links.Count == 0)
            return new GetUserCompaniesResponse { UserId = userId, DefaultCompanyId = Guid.Empty, Companies = Array.Empty<UserCompanyInfo>() };
        var defaultId = links.FirstOrDefault(l => l.IsDefault)?.CompanyId ?? links[0].CompanyId;
        return new GetUserCompaniesResponse
        {
            UserId = userId,
            DefaultCompanyId = defaultId,
            Companies = links.Select(l => new UserCompanyInfo
            {
                CompanyId = l.CompanyId,
                Code = l.CompanyCode,
                Name = l.CompanyName,
                IsDefault = l.IsDefault,
                IsHolding = l.IsHolding
            }).ToList()
        };
    }

    // Tx-aware BuildAsync for the register flow — refresh token insert rolls back
    // together with the user/company insert if anything later throws.
    private async Task<AuthResponse> BuildAsync(User user, Guid holdingId, string? ip, IDbConnection conn, IDbTransaction? tx, CancellationToken ct)
    {
        var roles = await _users.GetRoleNamesAsync(user.Id, conn, tx, ct);
        var links = await _users.GetUserCompaniesAsync(user.Id, ct);
        var defaultLink = links.FirstOrDefault(l => l.IsDefault) ?? links[0];
        var (at, atExp) = _jwt.GenerateAccessToken(user, roles, defaultLink.CompanyId, links.Select(l => l.CompanyId).ToList());
        var (rt, rtHash, rtExp) = _jwt.GenerateRefreshToken();
        await _refreshTokens.InsertAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = rtHash,
            ExpiresAt = rtExp,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ip
        }, conn, tx, ct);
        return new AuthResponse
        {
            AccessToken = at,
            RefreshToken = rt,
            AccessTokenExpiresAt = atExp,
            RefreshTokenExpiresAt = rtExp,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Roles = roles,
                DefaultCompanyId = defaultLink.CompanyId,
                Companies = links.Select(l => new UserCompanyInfo
                {
                    CompanyId = l.CompanyId,
                    Code = l.CompanyCode,
                    Name = l.CompanyName,
                    IsDefault = l.IsDefault,
                    IsHolding = l.IsHolding
                }).ToList()
            },
            HoldingCompanyId = holdingId
        };
    }

    // Back-compat BuildAsync for Login/Refresh (no shared conn).
    private async Task<AuthResponse> BuildAsync(User user, Guid holdingId, string? ip, CancellationToken ct)
    {
        var roles = await _users.GetRoleNamesAsync(user.Id, ct);
        var links = await _users.GetUserCompaniesAsync(user.Id, ct);
        var defaultLink = links.FirstOrDefault(l => l.IsDefault) ?? links[0];
        var (at, atExp) = _jwt.GenerateAccessToken(user, roles, defaultLink.CompanyId, links.Select(l => l.CompanyId).ToList());
        var (rt, rtHash, rtExp) = _jwt.GenerateRefreshToken();
        await _refreshTokens.InsertAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = rtHash,
            ExpiresAt = rtExp,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ip
        }, ct);
        return new AuthResponse
        {
            AccessToken = at,
            RefreshToken = rt,
            AccessTokenExpiresAt = atExp,
            RefreshTokenExpiresAt = rtExp,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Roles = roles,
                DefaultCompanyId = defaultLink.CompanyId,
                Companies = links.Select(l => new UserCompanyInfo
                {
                    CompanyId = l.CompanyId,
                    Code = l.CompanyCode,
                    Name = l.CompanyName,
                    IsDefault = l.IsDefault,
                    IsHolding = l.IsHolding
                }).ToList()
            },
            HoldingCompanyId = holdingId
        };
    }
}
