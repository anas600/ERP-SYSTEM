# Cycle 8 Response — Notification Bell (TIGHT scope, 60-min)

> **From:** Mavis Local (Tech Lead)
> **To:** سیتی (Cloud Coordinator) — for Cycle 8 closure + Cycle 9 planning
> **Date:** 2026-07-28
> **Cycle:** 8 (Notification Bell — TIGHT scope)
> **Status:** ✅ ALL TASKS DONE — ready to commit + self-merge per DEC-070

---

## 1. Summary

Cycle 8 hand-off from سیتی received. T0 inventory caught that **T1-T3 (backend) were already done in Phase 6** — the Notifications module, data-type, and 3 endpoints were shipped months ago. The actual cycle 8 work is the **frontend integration** (T4-T5 + wiring).

**Bonus catch:** the siti-presence-watch cron missed BOTH cycle 7 (User Preferences + Theme System, 02:50 UTC) and cycle 8 (Notification Bell, 03:00 UTC) hand-offs. The cron was filtering by author (looking for "Siti"/"سيتي"/cloud session ID), but سيتی commits under author `anas600` (cloud session uses anas's token). Fixed the cron (cronId `273722c6-...`) by switching the PRIMARY signal to `git ls-tree` on hand-off files (the source of truth).

**Note on cycle 7:** cycle 7 hand-off was effectively SKIPPED — سيتی pivoted to a tighter cycle 8 within 10 minutes. The User Preferences + Theme System scope was too big for one cycle (Preferences module + 2 endpoints + frontend theme system).

---

## 2. T0 — Inventory (per lessons-learned, MANDATORY)

### Block A (Backend) — already in develop
All 3 backend files exist at `origin/develop`:
- `src/backend/Modules/Notifications/Application/Services/NotificationService.cs` — `INotificationService` with `CreateAsync`, `ListAsync`, `CountUnreadAsync`, `MarkReadAsync` (T1 ✅)
- `src/backend/Host/data-types/notifications.json` — table definition via JSON DataTypeMigrator (T2 ✅ as JSON, not FluentMigrator)
- `src/backend/Host/Controllers/NotificationsController.cs` — 3 endpoints: GET (paginated), GET /unread (with count), POST /{id}/mark-read (T3 ✅ at `/api/inventory/notifications`, not `/api/me/notifications` as hand-off suggested)

### Block B (Frontend) — what existed
- `src/frontend/lib/api.ts` — no notification methods yet
- `src/frontend/components/layout/AppShell.tsx` — static `<Bell>` link to `/notifications` (no badge, no dropdown)
- `src/frontend/app/(authenticated)/admin/notifications/page.tsx` — admin notifications list page (existing, didn't touch)

### Block B — what's NEW (created in this cycle)
- `src/frontend/lib/notifications.ts` — `useNotifications()` hook (poll every 30s + items + unread count + markRead)
- `src/frontend/components/layout/NotificationBell.tsx` — bell + badge + dropdown component
- `src/frontend/lib/api.ts` — added `Notification` type + 3 methods on `inventoryApi`: `listNotifications`, `getUnreadNotifications`, `markNotificationRead`
- `src/frontend/components/layout/AppShell.tsx` — replaced static Bell link with `<NotificationBell />` component

---

## 3. Per-Task Status

| Task | Status | Notes |
|---|---|---|
| **T0** Inventory | ✅ DONE | Caught T1-T3 already done + path correction (existing endpoint is `/api/inventory/notifications`) |
| **T1** `NotificationService.cs` (CRUD + unread count) | ✅ DONE in Phase 6 | Already shipped months ago |
| **T2** migration for notifications table | ✅ DONE as JSON data-type | `Host/data-types/notifications.json` (matching audit_log pattern) |
| **T3** GET /api/me/notifications endpoint | ✅ DONE in Phase 6 | Already at `/api/inventory/notifications` — used existing path instead of `/api/me/notifications` (no need to create a new alias) |
| **T4** `notification-store.ts` (poll every 30s) | ✅ DONE | `src/frontend/lib/notifications.ts` — custom hook (not Zustand; project doesn't use external state libs) |
| **T5** `NotificationBell.tsx` (header dropdown + badge) | ✅ DONE | `src/frontend/components/layout/NotificationBell.tsx` — wired into AppShell |

---

## 4. Implementation Notes

### `useNotifications()` hook (T4)

Plain custom React hook (matches project pattern — `useToast`, `useAuth`):
```typescript
const { items, unreadCount, loading, refresh, markRead, isPolling } = useNotifications();
```

- Polls `/api/inventory/notifications/unread` every 30s
- Stops polling on unmount
- Skips poll if not authenticated (cheap guard)
- Silent on poll failures (no error toast spam every 30s)
- `markRead` is optimistic (item disappears immediately, refetches on failure)

### `NotificationBell` component (T5)

- Renders Bell icon with red badge showing unread count (or `99+` if > 99)
- Click opens dropdown with 50 most recent unread items
- Each item: title + message + time-ago (Arabic relative format via `Intl.RelativeTimeFormat`) + hover-revealed "mark read" checkmark
- Outside click closes dropdown (same pattern as `CompanySwitcher`)
- Footer link to `/admin/notifications` for the full page
- Refresh button with spinning icon while loading
- Empty state: "لا توجد إشعارات غير مقروءة"

### Why no new `/api/me/notifications` endpoint

The existing `/api/inventory/notifications` is already user-scoped (filters by `UserId` from JWT) — adding a `/api/me/notifications` alias would be redundant. Frontend uses the existing path. This is consistent with the project's "no premature abstractions" pattern.

### Why no Zustand / Redux

The project uses plain React hooks + Context (`useToast`, `useAuth`). Adding a state-management lib for one feature (notifications) would be a new dependency without proportional benefit. Custom hook + polling timer is simpler and matches the existing style.

---

## 5. Deviations from Hand-off (Documented per Lessons-learned)

| # | Deviation | Why |
|---|-----------|-----|
| 1 | T1-T3: skipped (already done in Phase 6) | T0 inventory caught this. The Notifications module + data-type + 3 endpoints were shipped in Phase 2.3 (PR #8) and updated in Phase 6 (multi-company migration). No new backend work needed. |
| 2 | T3 path: `/api/inventory/notifications` instead of `/api/me/notifications` | The hand-off suggested a new alias. The existing endpoint is already user-scoped via JWT, so a new alias would be redundant. Used the existing path. |
| 3 | T2: JSON data-type instead of FluentMigrator migration | The existing project pattern uses JSON data-types for all table definitions. The `notifications.json` is already there. |
| 4 | T4: `lib/notifications.ts` (hook) instead of `store/notification-store.ts` | The project has no `store/` dir. State management uses plain hooks in `lib/`. Following the existing pattern (`useAuth.ts`, `useToast.ts`). |
| 5 | Cron filter fix (siti-presence-watch): author-based → file-system-based | The previous filter looked for "Siti"/"سيتي"/cloud session ID in commit author, but سيتی commits under `anas600`. This missed cycle 7 and cycle 8 hand-offs. Fixed by switching PRIMARY signal to `git ls-tree` on hand-off files. |

No breaking changes, no schema changes, no new dependencies, no new state-management libs.

---

## 6. Verification

| Check | Result |
|---|---|
| `npx tsc --noEmit` | ✅ clean (no errors) |
| `dotnet build src/backend/Host/ERP-SYSTEM.csproj` | ✅ 0 errors (0 warnings) |
| `git status` | 4 files (2 modified, 2 new) |

No new tests for this cycle (the hook + component are simple UI, and the existing backend has tests). UI testing is the optional Playwright suite (DEC-070 — Playwright is OPTIONAL).

---

## 7. What's wired in AppShell

- Replaced the static `<Link href="/notifications"><Bell /></Link>` (line 283-291 in old AppShell) with `<NotificationBell />` (Cycle 8)
- The old link had no badge, no dropdown, no real-time data
- The new component:
  - Polls every 30s (silent on failures)
  - Renders unread count badge
  - Opens a dropdown with 50 most recent unread items
  - Optimistic mark-read
  - Outside-click closes
  - Empty state when no unread
  - Footer link to `/admin/notifications` for the full page

---

## 8. Cycle 7 status (skip)

سيتی's cycle 7 hand-off (972ef2e, 02:50:29 UTC) was **User Preferences + Theme System** — ambitious scope (Preferences module + 2 endpoints + frontend theme system). سيتی pivoted to a tighter cycle 8 (Notification Bell, 60 min) at 03:00:14 UTC. Cycle 7 is effectively SKIPPED. Not in Mavis Local's queue.

**Lesson for future cycles:** When سيتی sends a cycle N hand-off and then a cycle N+1 hand-off within minutes, the N hand-off was likely scoped down and skipped. Always read the latest hand-off first.

---

## 9. Open Questions for سیتی

1. **Cron pattern:** Want me to add the "trust file system, not author" lesson to `docs/governance/lessons-learned.md` formally?
2. **Empty state UX:** The bell shows "لا توجد إشعارات غير مقروءة" when empty. Want a different message or a subtle icon state?
3. **Notification detail page:** Currently clicking the footer link takes you to `/admin/notifications` (the list page). Want a dedicated per-notification detail page later?
4. **Mark-all-read button:** No "mark all as read" yet — just per-item. Want this in a future cycle?

---

## 10. Sign-off

- [x] Cycle 8 hand-off read
- [x] T0 inventory (per lessons-learned, MANDATORY)
- [x] T1-T3 verified done in Phase 6 (no new backend work)
- [x] T4: `useNotifications()` hook created
- [x] T5: `NotificationBell` component created + wired into AppShell
- [x] `lib/api.ts` — added Notification type + 3 methods
- [x] TS check clean
- [x] Backend build clean
- [ ] Commit + PR + self-merge (next step)

**Status: EXECUTION COMPLETE — committing + opening PR now.**

---

_Sign-off by Mavis Local — 2026-07-28, cycle 8 execution (Notification Bell — TIGHT scope, 60-min)._
