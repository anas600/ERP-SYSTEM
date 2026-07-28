# Sprint 3: Activity + Notifications

**Goal:** Show user activity + notification bell (client demo visual features)
**Time:** 1.5 hours | **Owner:** Mavis Local + 2 Jimis (FE+BE parallel)
**Refs:** [architecture.md](architecture.md) | [demo-roadmap.md](demo-roadmap.md) | [sprint-2.md](sprint-2.md)

## Block A (Backend Jimi — 30 min)

- [ ] **T1**: `GET /api/activity/recent?limit=20` — recent activity feed
  - Returns: `[{ id, userId, userName, action, entityType, entityId, timestamp, metadata }]`
  - Sorted DESC by timestamp
  - Filter by company_id (security)
- [ ] **T1b**: 1 unit test (ActivityFeedTests)

## Block B (Frontend Jimi — 1h)

- [ ] **T2**: `app/(authenticated)/activity/page.tsx` — activity feed UI
  - List view with timeline styling
  - Empty state (Arabic + English)
  - Loading state (skeleton)
- [ ] **T5**: Update `app/(authenticated)/layout.tsx` (or similar) — add notification bell
  - Bell icon in top bar
  - Popover with recent notifications
  - Mark as read action
- [ ] **T5b**: Update `app/(authenticated)/notifications/page.tsx` — full notifications page

## Block C (Mavis Local — 30 min)

- [ ] **T3**: Verify Backend smoke (call API, check response shape)
- [ ] **T6**: Verify Frontend (load pages, check visual)
- [ ] **T7**: Open PR (`feature/sprint-3-activity-notifications`, squash, --admin)

## Permissions (per DEC-070)
- ✅ Self-merge, --force-with-lease, skip Playwright
- ✅ Wide-permissions GITHUB_TOKEN
- ✅ Spawn 2 Jimis (FE+BE) in parallel

## Verification
```bash
# Backend
curl -H "Authorization: Bearer $TOKEN" "https://localhost:5001/api/activity/recent?limit=20"
# Expected: JSON array of 20 activity items, DESC by timestamp

# Frontend (visual)
# T2: /activity page shows timeline of activity
# T5: Bell icon in top bar, click → popover
# T5b: /notifications page shows full list
```

## Definition of Done
- [ ] `/api/activity/recent` returns real data (from Sprint 2 schema)
- [ ] `/activity` page shows the feed with RTL + Arabic
- [ ] Bell icon appears in top bar
- [ ] Bell popover shows recent items
- [ ] All tests pass
- [ ] CI green on PR

## Notes
- Activity log schema already exists (Sprint 1 + Cycle 6 PR #161)
- Notification bell UI already prototyped (Cycle 8 PR #162)
- This sprint = wire them together + add full activity page

## Next Sprint
Sprint 4 (Polish + Demo Data, 2h) — last sprint before Verify+Deploy

— سيتي (Cloud), 2026-07-28 22:45 UTC
