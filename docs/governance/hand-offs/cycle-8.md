# Cycle 8 — Notification Bell

**Full data:** [`docs/workflow/cycle-8.json`](../../workflow/cycle-8.json)
**Started:** 2026-07-28 02:59 UTC | **Estimate:** 60 min | **Owner:** Mavis Local

## Tasks
- **T1**: `NotificationService.cs` (CRUD + unread count)
- **T2**: `20260728_AddNotifications.cs` migration (idempotent)
- **T3**: `GET /api/me/notifications` endpoint
- **T4**: `notification-store.ts` (poll every 30s)
- **T5**: `NotificationBell.tsx` (header dropdown + badge)

## Permissions
Self-merge, --force-with-lease, skip Playwright, wide-permissions token.

## Verification
```bash
dotnet build && dotnet test
# T3: GET /api/me/notifications → { unread_count: N, items: [...] }
```

## Reference
Builds on cycle-6 (Activity Log) + cycle-7 (User Preferences). DEC-070 to DEC-073 apply.

— سيتي (Cloud), 2026-07-28 02:59 UTC
