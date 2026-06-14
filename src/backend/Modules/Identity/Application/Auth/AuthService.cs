using System.Security.Claims;
using ERPSystem.Modules.Identity.Entities;
using ERPSystem.Modules.Identity.Infrastructure;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Modules.Identity.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly ITenantRepository _tenants;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IJwtTokenService _jwt;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IRoleRepository roles,
        ITenantRepository tenants,
        IRefreshTokenRepository refreshTokens,
        IJwtTokenService jwt,
        ILogger<AuthService> logger)
    {
        _users = users;
        _roles = roles;
        _tenants = tenants;
        _refreshTokens = refreshTokens;
        _jwt = jwt;
        _logger = logger;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, string? ip, CancellationToken ct)
    {
        // 1) قرار الـ Tenant: موجود أو جديد
        Tenant? tenant = null;
        var isNewTenant = false;

        if (request.TenantId != Guid.Empty)
        {
            tenant = await _tenants.GetByIdAsync(request.TenantId, ct);
            if (tenant == null) return AuthResult.Fail("المستأجر غير موجود.", AuthErrorCode.TenantNotFound);
            if (!tenant.IsActive) return AuthResult.Fail("المستأجر موقوف.", AuthErrorCode.TenantInactive);
        }
        else
        {
            // إنشاء مستأجر جديد
            tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = request.TenantName!,
                Subdomain = Slugify(request.TenantName!),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _tenants.InsertAsync(tenant, ct);
            isNewTenant = true;
            _logger.LogInformation("تم إنشاء مستأجر جديد: {TenantId} ({Name})", tenant.Id, tenant.Name);
        }

        // 2) فحص تكرار البريد على مستوى المستأجر
        var existing = await _users.GetByEmailAndTenantAsync(request.Email, tenant.Id, ct);
        if (existing != null)
        {
            return AuthResult.Fail("البريد الإلكتروني مستخدم داخل هذا المستأجر.", AuthErrorCode.UserAlreadyExists);
        }

        // 3) إنشاء المستخدم
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            FullName = request.FullName.Trim(),
            IsActive = true,
            TwoFactorEnabled = false,
            CreatedAt = now,
            UpdatedAt = now,
        };
        await _users.InsertAsync(user, ct);

        // 4) ضمان وجود أدوار افتراضية وربط Admin بأول مستخدم في الـ tenant الجديد
        await _roles.EnsureDefaultRolesAsync(tenant.Id, ct);
        if (isNewTenant)
        {
            var adminRole = await _roles.GetByNameAsync(tenant.Id, "Admin", ct);
            if (adminRole != null)
            {
                await _users.AssignRoleAsync(user.Id, adminRole.Id, ct);
            }
        }

        // 5) إصدار التوكنات
        var roles = await _users.GetRoleNamesAsync(user.Id, ct);
        return AuthResult.Ok(await BuildAuthResponseAsync(user, roles, ip, ct));
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, string? ip, CancellationToken ct)
    {
        User? user = null;

        if (request.TenantId.HasValue && request.TenantId.Value != Guid.Empty)
        {
            user = await _users.GetByEmailAndTenantAsync(request.Email, request.TenantId.Value, ct);
            var tenant = await _tenants.GetByIdAsync(request.TenantId.Value, ct);
            if (tenant == null) return AuthResult.Fail("المستأجر غير موجود.", AuthErrorCode.TenantNotFound);
            if (!tenant.IsActive) return AuthResult.Fail("المستأجر موقوف.", AuthErrorCode.TenantInactive);
        }
        else
        {
            // بحث شامل — للـ super-admin flows
            user = await _users.GetByEmailAsync(request.Email, ct);
        }

        if (user == null || !user.IsActive)
        {
            _logger.LogWarning("فشل تسجيل الدخول لـ {Email}: مستخدم غير موجود أو موقوف.", request.Email);
            return AuthResult.Fail("بيانات الدخول غير صحيحة.", AuthErrorCode.InvalidCredentials);
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("فشل تسجيل الدخول لـ {Email}: كلمة مرور خاطئة.", request.Email);
            return AuthResult.Fail("بيانات الدخول غير صحيحة.", AuthErrorCode.InvalidCredentials);
        }

        // تحديث LastLogin
        var now = DateTime.UtcNow;
        await _users.UpdateLastLoginAsync(user.Id, now, ct);

        var roles = await _users.GetRoleNamesAsync(user.Id, ct);
        return AuthResult.Ok(await BuildAuthResponseAsync(user, roles, ip, ct));
    }

    public async Task<AuthResult> RefreshAsync(RefreshTokenRequest request, string? ip, CancellationToken ct)
    {
        var principal = _jwt.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
        {
            return AuthResult.Fail("Access Token غير صالح.", AuthErrorCode.InvalidRefreshToken);
        }

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? principal.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return AuthResult.Fail("بيانات التوكن غير مكتملة.", AuthErrorCode.InvalidRefreshToken);
        }

        var tokenHash = _jwt.HashRefreshToken(request.RefreshToken);
        var stored = await _refreshTokens.GetByHashAsync(tokenHash, ct);
        if (stored == null || stored.UserId != userId)
        {
            return AuthResult.Fail("Refresh Token غير صالح.", AuthErrorCode.InvalidRefreshToken);
        }

        if (stored.IsRevoked)
        {
            // استخدام token ملغى = هجوم محتمل → نُلغي كل tokens للمستخدم
            _logger.LogWarning("محاولة استخدام Refresh Token ملغى للمستخدم {UserId} — سيتم إلغاء جميع الجلسات.", userId);
            await _refreshTokens.RevokeAllForUserAsync(userId, "Reuse of revoked token", ip, ct);
            return AuthResult.Fail("تم اكتشاف محاولة اختراق، تم إلغاء جميع جلساتك.", AuthErrorCode.RefreshTokenRevoked);
        }

        if (stored.IsExpired)
        {
            return AuthResult.Fail("Refresh Token منتهي الصلاحية.", AuthErrorCode.RefreshTokenExpired);
        }

        var user = await _users.GetByIdAsync(userId, ct);
        if (user == null || !user.IsActive)
        {
            return AuthResult.Fail("المستخدم غير مفعّل.", AuthErrorCode.UserInactive);
        }

        // Token rotation: نُلغي القديم ونصدر زوج جديد
        var (newRefreshToken, newRefreshHash, newRefreshExp) = _jwt.GenerateRefreshToken();
        await _refreshTokens.RevokeAsync(stored, "Rotated", newRefreshHash, ip, ct);

        var (accessToken, accessExp) = _jwt.GenerateAccessToken(user, await _users.GetRoleNamesAsync(user.Id, ct));
        await _refreshTokens.InsertAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = newRefreshHash,
            ExpiresAt = newRefreshExp,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ip
        }, ct);

        return AuthResult.Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiresAt = accessExp,
            RefreshTokenExpiresAt = newRefreshExp,
            User = await BuildUserInfoAsync(user, ct),
        });
    }

    public async Task RevokeAsync(Guid userId, string refreshToken, string? ip, CancellationToken ct)
    {
        var hash = _jwt.HashRefreshToken(refreshToken);
        var stored = await _refreshTokens.GetByHashAsync(hash, ct);
        if (stored != null && stored.UserId == userId && stored.IsActive)
        {
            await _refreshTokens.RevokeAsync(stored, "User logout", null, ip, ct);
        }
    }

    // ----- Helpers -----

    private async Task<AuthResponse> BuildAuthResponseAsync(User user, IReadOnlyList<string> roles, string? ip, CancellationToken ct)
    {
        var (accessToken, accessExp) = _jwt.GenerateAccessToken(user, roles);
        var (refreshToken, refreshHash, refreshExp) = _jwt.GenerateRefreshToken();

        await _refreshTokens.InsertAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = refreshExp,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ip
        }, ct);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = accessExp,
            RefreshTokenExpiresAt = refreshExp,
            User = new UserInfo
            {
                Id = user.Id,
                TenantId = user.TenantId,
                Email = user.Email,
                FullName = user.FullName,
                Roles = roles,
            }
        };
    }

    private async Task<UserInfo> BuildUserInfoAsync(User user, CancellationToken ct)
    {
        var roles = await _users.GetRoleNamesAsync(user.Id, ct);
        return new UserInfo
        {
            Id = user.Id,
            TenantId = user.TenantId,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles,
        };
    }

    private static string Slugify(string s)
    {
        var slug = s.ToLowerInvariant();
        var arr = slug.ToCharArray();
        for (var i = 0; i < arr.Length; i++)
        {
            if (!char.IsLetterOrDigit(arr[i])) arr[i] = '-';
        }
        return new string(arr).Trim('-');
    }
}
