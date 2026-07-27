# 🔐 src/backend/Modules/Identity/AGENTS.md

> Identity Module — Phase 0 (✅ مكتمل).
>
> محدّث: 2026-07-24 — **Release v5.0.1: RegisterAsync صار atomic (DEC-091) — single conn + single tx**
>
> **Phase 6 (2026-07-27) — Multi-Company update:** Per Constitution Article 3, this module now uses `ICompanyContext` (instead of removed `ITenantContext`). All queries filter by `company_id` (instead of removed `tenant_id`). Users are global, companies are many. JWT carries `default_company_id` + `company_ids[]`. See root [AGENTS.md](../../../../AGENTS.md#-multi-company-convention-per-constitution-article-3) and [docs/PHASE6-RELEASE-NOTES.md](../../../../PHASE6-RELEASE-NOTES.md) for migration guide.

## شو فيه

```
Identity/
├── Entities/
│   ├── User.cs            # User entity + navigations
│   ├── Role.cs            # Role + UserRole join
│   └── RefreshToken.cs    # JWT refresh token (rotation + reuse detection)
│
│   > Note: `Tenant.cs` was removed in Phase 6.1b (Constitution Article 3 — Multi-Company, NOT Multi-Tenant). The tenant root is now the `Holding Company` row in the `companies` table.
├── Application/
│   └── Auth/
│       ├── AuthDtos.cs         # RegisterRequest, LoginRequest, AuthResponse, UserInfo
│       ├── IAuthService.cs     # Contract
│       ├── AuthService.cs      # Implementation (Register, Login, Refresh, Revoke)
│       ├── IJwtTokenService.cs # Contract
│       ├── JwtTokenService.cs  # JWT generation + validation
│       ├── JwtSettings.cs      # Config binding
│       └── Validators.cs       # FluentValidation rules
└── Infrastructure/
    ├── IRepositories.cs        # All repository contracts
    ├── UserRepository.cs       # Dapper queries
    ├── RoleRepository.cs       # + EnsureDefaultRolesAsync
    └── RefreshTokenRepository.cs
```

> Note: `TenantRepository.cs` was removed in Phase 6.1b. Company access goes via `UserCompanyRepository` (in the Companies module) — users → companies is a many-to-many via `user_companies`.

## Domain Model

### User
- مرتبط بـ **شركة أو أكثر** عبر `user_companies` (Constitution Article 3.1)
- `Email` فريد **عبر النظام كله** (one global email per user — no per-tenant duplicate)
- `PasswordHash` — BCrypt، workFactor 12
- `IsActive`, `TwoFactorEnabled` (للمرحلة القادمة)

### Role
- 4 أدوار افتراضية تُنشأ تلقائياً عند أول user (under the default Holding Company):
  - **Admin** — كامل الصلاحيات
  - **Accountant** — Finance فقط
  - **ProjectManager** — Projects فقط
  - **Viewer** — قراءة فقط

### RefreshToken
- `TokenHash` (SHA-256 base64) — لا نخزن النص الصريح
- `ExpiresAt`، `RevokedAt`، `ReplacedByTokenHash`
- `IsActive = RevokedAt == null && Now < ExpiresAt`
- **Token Rotation**: كل refresh يُلغي القديم ويُولّد جديد
- **Reuse Detection**: استخدام refresh ملغى = `RevokeAllForUserAsync` (defense in depth)

## Auth Flows

### 1. Register

> **Per Constitution Article 3.3:** "Register = create the first user under the default Holding Company (no tenant creation wizard)."

```
POST /api/auth/register
Body: {
  email: string,
  password: string,       // ≥8 chars, [A-Z], [a-z], [0-9]
  fullName: string,
  baseCurrency?: string   // default "LYD"
}
```

- **Behavior:** ينشئ first user + يعيّنه Admin في الـ default Holding Company (الموجود مسبقاً من `SeedDefaultHoldingAsync`)
- **لا** إنشاء tenant، **لا** `tenantName` field، **لا** `subdomain` (كل هذه أُزيلت في Phase 6.0/6.1b)
- `EnsureDefaultRolesAsync(companyId)` يضمن وجود الأدوار الأربعة تحت الـ Holding
- `BaseCurrency` يُمرر لـ `ICompanyBootstrap.OnCompanyCreatedAsync` (لإنشاء الـ default CoA بنفس العملة)

#### 🛡️ Atomicity (DEC-091, Release v5.0.1)

**الـ Register flow atomic** — يستخدم `IDbTransaction` واحد عبر كل الـ inserts:

```csharp
using var conn = await _db.CreateOltpConnectionAsync(ct);
using var tx = conn.BeginTransaction();
try
{
    // 1. user insert
    // 2. EnsureDefaultRolesAsync (4 default roles — under default Holding)
    // 3. admin role assign
    // 4. user_companies link (user → default Holding)
    // 5. GetRoleNamesAsync
    // 6. BuildAsync → refresh token insert
    tx.Commit();
}
catch
{
    try { tx.Rollback(); } catch { /* best-effort */ }
    throw;
}
```

**الـ repos تأخذ overloads جديدة `(IDbConnection, IDbTransaction?, ct)`:**
- `UserRepository.InsertAsync(user, conn, tx, ct)` + `GetByEmailAsync` + `GetRoleNamesAsync` + `AssignRoleAsync`
- `RoleRepository.EnsureDefaultRolesAsync(companyId, conn, tx, ct)` + `GetByNameAsync`
- `UserCompanyRepository.LinkAsync(userId, companyId, conn, tx, ct)`
- `RefreshTokenRepository.InsertAsync(rt, conn, tx, ct)` (يُستدعى من `BuildAsync`)
- الـ signatures القديمة `(ct)` preserved كـ back-compat wrappers

**Trigger:** HF Space proxy كان يقطع الاتصال بعد 60s timeout، مما يترك orphan users (User + DefaultRoles بدون user_companies link) — قبل الـ fix، كان 15 orphan registrations في Supabase (pre-Phase 6 cleanup).

**Audit:** أي service method جديد يـ insert في >1 جدول → استخدم نفس الـ pattern. DEC-091 يحدد القاعدة.

### 2. Login

```
POST /api/auth/login
Body: { email: string, password: string }
```

- بحث بـ `Email` فقط (لا يوجد `tenantId` بعد الآن — Constitution Article 3)
- BCrypt.Verify + LastLogin update
- الـ JWT يحمل: `user_id`, `default_company_id`, `company_ids[]`, roles

### AuthResponse (مشترك بين register و login)

```csharp
{
  AccessToken: string,
  RefreshToken: string,
  AccessTokenExpiresAt: DateTime,
  RefreshTokenExpiresAt: DateTime,
  User: UserInfo,
  DefaultCompanyId: Guid,   // للـ company switcher
  CompanyIds: Guid[]        // الشركات التي للمستخدم access عليها
}
```

### 3. Refresh

```
POST /api/auth/refresh
Body: { accessToken, refreshToken }
```

- يفك Access Token (يقبل منتهي الصلاحية)
- يتحقق من RefreshToken في DB
- Rotation: يلغي القديم + يولد جديد

### 4. Logout

```
POST /api/auth/logout (Bearer required)
Body: { refreshToken }
```

- يلغي الـ Refresh Token المحدد

### 5. Me

```
GET /api/auth/me (Bearer required)
```

- يرجع UserInfo من الـ claims

## لما تشتغل هنا

- إضافة permission جديد: عدّل `UserRole` و أضف permission claims
- إضافة 2FA: فعّل `TwoFactorEnabled` logic و أضف endpoint جديد
- إضافة audit log: أنشئ `IdentityAudit` entity + migration

## بعد التعديل

- إذا أضفت entity جديد: اكتب migration جديدة (لا تعدّل القديمة)
- إذا غيّرت auth flow: حدّث قسم "Auth Flows" أعلاه
- أضف unit tests في `Tests/Auth/`

## مرتبطة بـ

- [`../../AGENTS.md`](../../AGENTS.md)
- [`../AGENTS.md`](../AGENTS.md)
- [`../../Shared/AGENTS.md`](../../Shared/AGENTS.md) — CompanyContextMiddleware, Migrations
- [`../../Host/AGENTS.md`](../../Host/AGENTS.md) — AuthController
- [`../Finance/AGENTS.md`](../Finance/AGENTS.md) — Company bootstrap (HoldingCompany + CoA)
- [`../Procurement/AGENTS.md`](../Procurement/AGENTS.md) — Phase 3
- [`../HR/AGENTS.md`](../HR/AGENTS.md) — Phase 3.5
- [`../Payroll/AGENTS.md`](../Payroll/AGENTS.md) — Phase 4


---

## 🤝 Cross-Team Coordination (Brainstorming Lab)

This project works with an analytical team via the **Brainstorming Lab**.

- **When to read from hub**: ONLY when explicitly instructed by the analytical team
- **Default**: Work from local context (this file + root `AGENTS.md` + source code)
- **Hub repo**: https://github.com/anas600/brainstorming-lab/tree/main/portals/02-session-002/

See root [`AGENTS.md`](../../../../AGENTS.md) for full cross-team protocol.

Token-efficient: ~50 tokens per cross-team directive (vs 500+ for full re-paste).
