# 🔐 AGENTS.md — src/backend/Modules/Identity/

> **Identity module** (users + auth). Read all parent AGENTS.md files first.

**Last updated:** 2026-08-27 (Sprint 61 Wave 1B — 5 permanent fixes)

---

## Purpose

Manages user identities, authentication (JWT + BCrypt), and the `user_companies` join table. This is the security boundary.

## Ownership

| Role | Owner |
|------|-------|
| **Authoring** | Jimi تنفيذي (Identity specialist) |
| **Security review** | Anas (Project Owner) for any auth changes |

## Local Contracts

### Schema
- `users` — `id`, `email`, `password_hash`, `name`, etc.
- `user_companies` — `user_id`, `company_id`, `role`, `is_primary` (composite PK).
- `refresh_tokens` — for token rotation.

### Auth
- **BCrypt cost 12** for passwords.
- **JWT HS256** with `company_ids[]` claim.
- **Refresh token rotation** on every login.
- **No long-lived tokens.** Max 24h access token.

### Authorization
- **Roles:** `holding_admin`, `company_admin`, `manager`, `accountant`, `viewer`.
- **Permissions matrix** in `Identity/Application/Permissions.cs`.
- **Cross-company access** requires `holding_admin` role.

## Sprint 61 Wave 1B — 5 permanent fixes (DEC-196, DEC-197, DEC-198)

Per Sprint 60 lessons (L47, L48, L49, L51, L175). See `docs/CHANGELOG.md` for the full entry.

### L49 — Connection-aware `GetUserCompaniesAsync`
- The tx-aware `BuildAsync` now calls the **new** connection-aware overload:
  `GetUserCompaniesAsync(Guid userId, IDbConnection conn, IDbTransaction? tx, CancellationToken ct)`.
- The connection-less overload is kept for `LoginAsync` and `RefreshAsync` (no shared conn).
- **Rule:** if you add a code path that writes `users` / `user_companies` / `user_roles`
  and then reads back via the user repo inside the same transaction, use the
  conn+tx overload. The pool (Supabase pgbouncer) cannot see uncommitted writes
  on a fresh connection, so the old overload returns an empty list and the next
  line crashes with `ArgumentOutOfRangeException`.

### L175 — `POST /api/auth/admin-bootstrap` (one-shot first admin)
- **AllowAnonymous** endpoint for brand-new deployments. Body: `{email, password, fullName}`.
- Returns **409 Conflict** if any user already exists — the regular register / login flow
  is the only way to onboard more users. This is the idempotency guard.
- Returns **500** if the Holding Company has not been seeded yet.
- Returns **201 Created** with `{userId, email, fullName, role: "Admin", companyId, createdAt}`.
- The endpoint exists to close the chicken-and-egg gap on fresh deployments
  (L48 + L49 alone are not enough — they assume at least one user can log in).

## Work Guidance

### Adding an Auth Endpoint
1. Add to `Identity/Application/Services/AuthService.cs`.
2. Generate JWT with `company_ids[]` from `user_companies`.
3. Add rate limiting (5 attempts per 15 min).
4. Add audit log entry.

### Adding a New Role
1. Update `Identity/Domain/Roles.cs`.
2. Update permission matrix in `Identity/Application/Permissions.cs`.
3. Add migration if needed.
4. Document in this AGENTS.md.

## Verification

- [ ] `dotnet test --filter "Identity"` — all green.
- [ ] No `tenant_id`: `grep -r "tenant" src/backend/Modules/Identity/`.
- [ ] No plaintext passwords.
- [ ] JWT secret in env var, not code.

---

_Last updated: 2026-08-27 by Worker 1B (Sprint 61 Wave 1B) — DOX pass for L47+L48+L49+L51+L175_
_Previous: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
