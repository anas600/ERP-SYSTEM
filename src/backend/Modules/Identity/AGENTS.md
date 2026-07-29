# 🔐 AGENTS.md — src/backend/Modules/Identity/

> **Identity module** (users + auth). Read all parent AGENTS.md files first.

**Last updated:** 2026-07-29 (DOX framework applied)

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

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
