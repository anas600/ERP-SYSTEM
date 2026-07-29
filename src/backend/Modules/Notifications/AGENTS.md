# 🔔 AGENTS.md — src/backend/Modules/Notifications/

> **Notifications module.** Read all parent AGENTS.md files first.

**Last updated:** 2026-07-29 (DOX framework applied)

---

## Purpose

In-app notifications (bell icon, popover, full list). Per-user, optionally per-company.

## Ownership

| Role | Owner |
|------|-------|
| **Authoring** | Jimi تنفيذي |
| **Real-time** | SignalR hub (when added) |

## Local Contracts

### Schema
- `notifications` — `id`, `user_id`, `company_id`, `type`, `title`, `body`, `is_read`, `read_at`, `created_at`.
- **Soft read** via `is_read` + `read_at`.

### Real-time
- **SignalR Hub** (future): `/hubs/notifications`.
- **Polling fallback:** every 30s when no SignalR.

## Work Guidance

### Adding a Notification Type
1. Add enum in `Domain/NotificationType.cs`.
2. Add notification service method in `Application/Services/NotificationService.cs`.
3. Add to UI via `frontend/app/(authenticated)/notifications/`.

## Verification

- [ ] `dotnet test --filter "Notifications"` — all green.
- [ ] No `tenant_id`.
- [ ] All notifications have `user_id`.

---

_Last updated: 2026-07-29 by Mavis (Muhammad mode) — DOX framework applied_
