# 📦 Hand-Off v1 — Cycle 7: User Preferences + Theme System

> **From:** سيتي (Cloud Coordinator) — Session 406067545768199, Cloud  
> **To:** Mavis Local (Tech Lead) — your session, Windows  
> **Cycle:** 7 / 20 — **ACTIVE ✅**  
> **Created:** 2026-07-28 02:50 UTC

---

## 🎯 Cycle 7 Scope

### Block A (Mavis Local) — User Preferences Module

**Background:** First user-facing feature (Activity Log) shipped in cycle 6. Now we add user preferences so users can customize their experience.

**Tasks:**

- **T1**: Create `src/backend/Modules/Preferences/Application/UserPreferenceService.cs` (~120 lines)
  - Methods: `GetAsync(userId)`, `SetAsync(userId, key, value)`, `GetAllAsync(userId)`
  - Validates keys against allowed list (security: no arbitrary keys)
  - Caches in-memory for 5 min
  - JSON serialization for complex values

- **T2**: Add `user_preferences` table migration (FluentMigrator, idempotent)
  - File: `src/backend/Host/Migrations/20260728_AddUserPreferences.cs`
  - Columns: id, user_id, key, value (jsonb), created_at, updated_at
  - Indexes: UNIQUE(user_id, key), (user_id)

- **T3**: Add `GET /api/me/preferences` endpoint
  - Returns all user prefs as `{key: value}` object
  - Auth: requires authenticated user
  - File: `src/backend/Modules/Preferences/Endpoints/GetMyPreferences.cs`

- **T4**: Add `PUT /api/me/preferences/{key}` endpoint
  - Body: `{value: any}` (JSON)
  - Validates key is in allowed list
  - Returns updated pref
  - File: `src/backend/Modules/Preferences/Endpoints/SetMyPreference.cs`

- **T5**: Add 1 test case
  - File: `src/backend/Tests/ERPSystem.Tests/Preferences/UserPreferenceServiceTests.cs`
  - Tests: get, set, get-all, validation failure
  - ~80 lines

**Estimated time:** 3-4 hours

### Block B (Mavis Local) — Theme System (Frontend)

**Background:** With preferences in place, the user can store their theme. This block adds the frontend integration.

**Tasks:**

- **T6**: Add theme store/hook
  - File: `src/frontend/lib/theme-store.ts` (new, ~50 lines)
  - Zustand or similar state management
  - Persists to `/api/me/preferences` on change

- **T7**: Add theme provider
  - File: `src/frontend/app/providers/ThemeProvider.tsx` (new, ~60 lines)
  - Supports: `light`, `dark`, `system`
  - Respects `prefers-color-scheme` when `system`
  - Applies CSS variables to `<html>` element

- **T8**: Add theme toggle to header
  - File: `src/frontend/app/_components/ThemeToggle.tsx` (new, ~40 lines)
  - 3-state button (light/dark/system)
  - Persists via theme-store → API

**Estimated time:** 2 hours

---

## 🛡️ Permissions (DEC-070 + DEC-071 + DEC-072 + DEC-073)

- ✅ Self-merge (--admin flag)
- ✅ --force-with-lease
- ✅ Skip Playwright (optional)
- ✅ Risk tolerance on develop
- ✅ Lead Jimis
- ✅ Wide-permissions GITHUB_TOKEN
- ❌ NO staging/production (frozen)
- ❌ NO HF Space production app
- ❌ NO main branch

---

## 🔧 Verification

```bash
# 1. Build
npx tsc --noEmit
dotnet build Host/ERP-SYSTEM.csproj

# 2. The new tests
dotnet test --filter "UserPreferenceService"

# 3. API smoke
curl -X GET http://localhost:5000/api/me/preferences \
  -H "Authorization: Bearer <token>"
```

---

## 🚨 Pre-Hand-off Verification (per lessons-learned)

**Before starting:**
1. `git log origin/develop --oneline | head -10`
2. Check if `user_preferences` table already exists (avoid duplicate migration)
3. Check if any theme code exists (extend, don't replace)
4. Document inventory in response

---

## 📡 Async Protocol

- 3 crons active: presence-check, monitor-cycle-5, monitor-cycle-6, monitor-cycle-7
- Wait — the monitor-cycle-5/6 are no longer needed (those cycles are done)
- I'll clean up the old crons and keep only the active one
- New cron: `monitor-cycle-7-pr-merge` (will create)

---

## 🚀 When Ready to Start

1. Read this hand-off ✅
2. Verify with `git log origin/develop --oneline | head -10`
3. Create feature branch: `git checkout -b feature/cycle-7-preferences-theme`
4. Do the work (5-6 hours total)
5. Open PR to develop
6. Say "ready for merge"
7. I (سيتي) merge

**You have full authority. Go. 🎯**

---

**Signed:** سيتي (Cloud Coordinator) — Session 406067545768199, Cloud  
**Authority:** DEC-070 + DEC-071 + DEC-072 + DEC-073  
**Date:** 2026-07-28 02:50 UTC
