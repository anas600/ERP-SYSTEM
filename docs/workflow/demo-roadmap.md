# 🚀 Demo Roadmap (10-Hour Sprint Plan)

> **Target:** MFA Holding demo version (on-premise, client-facing)
> **Total time:** 10 hours
> **Methodology:** Agile (1.5-2h sprints)
> **Owner:** Mavis Local (Tech Lead) + 2 Jimis (parallel)
> **Coordinator:** سيتي (Cloud)

---

## 📅 Timeline

| Hour | Sprint | Title | Focus |
|------|--------|-------|-------|
| 0.0 | 0 | Setup + Plan | Architecture + Demo data |
| 0.5 | 1 | Dashboard + Holding | Top-level views |
| 2.5 | 2 | Companies + Users | Multi-company mgmt |
| 4.5 | 3 | Activity + Notifications | User engagement |
| 6.0 | 4 | Polish + Demo Data | Final touches |
| 8.0 | — | Verification + Deploy | Final QA |
| 10.0 | — | **Demo Ready** | ✅ |

---

## 🏃 Sprint 0: Setup (0.5h)

**Owner:** Mavis Local + 1 Jimi (Backend)

**Tasks:**
- [ ] Verify environment (HF Space, local Docker, Supabase dev)
- [ ] Confirm wide-permissions GITHUB_TOKEN works
- [ ] Seed demo data: 1 Holding, 3 Companies, 5 Users
- [ ] Add 1 demo transaction per company
- [ ] Test login + JWT + X-Company-Id

**Verification:**
```bash
dotnet build && dotnet test
# Login as demo@mfaholding.local
# Verify: GET /api/me/companies returns 3
```

**Definition of done:**
- Demo user can log in
- Can switch between 3 companies
- Sees 1 transaction per company

---

## 📊 Sprint 1: Dashboard + Holding (2h)

**Owner:** Mavis Local + 1 Jimi (Frontend) + 1 Jimi (Backend)

**Block A (Backend, ~1h):**
- [ ] `GET /api/dashboard/summary` endpoint
  - Returns: total companies, total users, recent activity count, transaction count
- [ ] Use existing tables: companies, users, activity_log, ledger_entries

**Block B (Frontend, ~1h):**
- [ ] `app/admin/dashboard/page.tsx` (new)
  - 4 KPI cards (Companies, Users, Activity, Transactions)
  - Quick links to Companies, Users, Activity Log
- [ ] `app/holding/page.tsx` (new)
  - Holding overview + sub-companies list
  - "Switch Active Company" dropdown

**Verification:**
- Dashboard shows 4 KPIs with real data
- Holding page shows 1 Holding + 3 companies
- Company switcher works

---

## 🏢 Sprint 2: Companies + Users (2h)

**Owner:** Mavis Local + 1 Jimi (Frontend) + 1 Jimi (Backend)

**Block A (Backend, ~1h):**
- [ ] `GET /api/companies` — list with pagination
- [ ] `GET /api/companies/{id}` — details
- [ ] `POST /api/companies` — create (idempotent on name)
- [ ] `GET /api/users` — list with company filter
- [ ] `GET /api/users/{id}/companies` — assigned companies

**Block B (Frontend, ~1h):**
- [ ] `app/admin/companies/page.tsx` — table view
- [ ] `app/admin/companies/[id]/page.tsx` — details
- [ ] `app/admin/users/page.tsx` — table view
- [ ] `app/admin/users/[id]/page.tsx` — details + assigned companies

**Verification:**
- Companies list shows 3 demo companies
- User details shows assigned companies
- Can navigate between companies and users

---

## 🔔 Sprint 3: Activity + Notifications (1.5h)

**Owner:** Mavis Local + 1 Jimi (Frontend) + 1 Jimi (Backend)

**Block A (Backend, ~0.5h):**
- [ ] `GET /api/activity/recent` — last 20 activities
- [ ] Reuse cycle-6 Activity Log

**Block B (Frontend, ~1h):**
- [ ] `app/activity/page.tsx` — activity feed
  - Time, user, action, entity
  - Filter by action type
- [ ] `app/notifications/page.tsx` — notifications list
  - Unread/read tabs
  - Mark as read button

**Verification:**
- Activity feed shows last 20 actions
- Notifications page shows unread count
- Click notification → mark as read

---

## ✨ Sprint 4: Polish + Demo Data (2h)

**Owner:** Mavis Local + 1 Jimi (Frontend) + 1 Jimi (Backend)

**Block A (Demo Data, ~0.5h):**
- [ ] Add 50+ demo transactions across companies
- [ ] Add 5+ activity log entries per day for last 7 days
- [ ] Add realistic Arabic names + descriptions

**Block B (Polish, ~1.5h):**
- [ ] RTL layout fixes (Arabic text)
- [ ] Loading states (skeleton screens)
- [ ] Error states (Arabic + English)
- [ ] Empty states (with CTAs)
- [ ] Mobile responsive (basic)

**Verification:**
- Demo data realistic and Arabic-friendly
- All pages have loading/error/empty states
- Mobile view works (responsive)

---

## 🔍 Verification + Deploy (2h)

**Owner:** Mavis Local + سيتي (review)

**Tasks:**
- [ ] Run full E2E test suite
- [ ] Verify on local Docker (matches HF Space parity)
- [ ] Build production bundle
- [ ] Deploy to client's local environment
- [ ] Smoke test with demo user

**Definition of done:**
- Demo accessible at client's local URL
- All 4 sprints work end-to-end
- Zero critical bugs

---

## 📊 Success Metrics

| Metric | Target | How to Measure |
|--------|--------|----------------|
| **Total time** | ≤ 10 hours | Clock from sprint 0 → demo ready |
| **Critical bugs** | 0 | Smoke test + manual walkthrough |
| **Demo user can:** | Login → Switch company → See all data | Manual test |
| **Pages** | 6+ (Dashboard, Holding, Companies, Users, Activity, Notifications) | URL count |
| **API endpoints** | 10+ | Swagger count |

---

## 🚨 Risk Mitigation

| Risk | Mitigation |
|------|------------|
| **Jimi blocked on unclear task** | Mavis Local clarifies in 5 min, escalates to Siti if needed |
| **CI fails repeatedly** | Mavis Local fixes, --force-with-lease to retry |
| **Local Docker issues** | Fall back to HF Space URL (already running) |
| **Time runs out** | Cut Sprint 4 (polish), ship core features |
| **Demo data unrealistic** | Pre-seed during Sprint 0, Mavis Local validates |

---

## 🤝 Coordination Protocol (V2)

### Mavis Local's role:
- **Plan internally** — break sprint into Jimi tasks
- **Delegate to 2 Jimis** — Frontend + Backend in parallel
- **Verify** — check Jimi work before PR
- **Open PR** — squash merge via --admin

### Siti's role (me):
- **Write hand-off** — short, JSON-referenced
- **Set architectural constraints** — at sprint start
- **Review PRs** — verify they meet constraints
- **Merge + Close cycle** — within 15 min of PR ready
- **Update roadmap** — on each cycle close

### Communication:
- **Telegram** — for state changes (PR open, CI green, cycle closed)
- **Hand-off files** — for sprint scope + constraints
- **docs/workflow/** — for JSON-driven cycle data

---

*Last updated: 2026-07-28 03:45 UTC by سيتي (Cloud)*
