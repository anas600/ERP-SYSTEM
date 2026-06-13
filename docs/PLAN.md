# خطة تطوير وتنفيذ النظام المالي (FinTech MVP)

> **الإصدار:** 1.0
> **التاريخ:** 2026-06-13
> **المنهجية:** Agile (Scrum) + Iterative MVP
> **الهدف:** إطلاق نسخة MVP قابلة للاستخدام على VPS في أسرع وقت

---

## 0️⃣ التحديث: نظام المخازن مدمج في الخطة

النظام يتكون من **3 Modules أساسية + 1 Module للدعم**:

| Module | الوصف | الأولوية في MVP |
|--------|-------|-----------------|
| **Identity** | إدارة المستخدمين، المستأجرين، الصلاحيات | Phase 0 |
| **Finance** | الحسابات، القيود، الفواتير، المدفوعات، محرك القواعد | Phase 1-2 |
| **Projects** | المشاريع، المهام، الميزانيات، الموارد | Phase 2.2 |
| **Inventory** | المنتجات، المخازن، الحركات، التقييم المالي | Phase 2.3 |
| **Shared** | Event Bus، Multi-tenancy، Logging، إلخ | Phase 0 |

---

## 1️⃣ التقنيات المختارة

- **Backend:** ASP.NET Core 8 (C#) + Modular Monolith + Vertical Slices
- **Database:** PostgreSQL 16 + Dapper + FluentMigrator + MartenDB (Event Store)
- **Frontend:** Next.js 14 + TypeScript + shadcn/ui
- **Cache/Queue:** Redis
- **Containerization:** Docker + Docker Compose
- **Reverse Proxy:** Nginx + Let's Encrypt
- **Monitoring:** Uptime Kuma + Grafana + Loki

**المدة التقديرية لإطلاق MVP:** 8-10 أسابيع

---

## 2️⃣ المرحلة 0 — Foundation (الأسبوع 1)

- [ ] Monorepo setup
- [ ] Docker Compose (Postgres, Redis, MartenDB, App)
- [ ] CI/CD pipeline (GitHub Actions)
- [ ] Logging + Health checks
- [ ] Identity Module (Users, Tenants, RBAC)

---

## 3️⃣ المرحلة 1 — Finance Core (الأسابيع 2-4)

- [ ] Chart of Accounts
- [ ] Journal Entries + Double-Entry validation
- [ ] General Ledger + Trial Balance
- [ ] Rules Engine (Auto-posting)
- [ ] Multi-tenancy pattern

---

## 4️⃣ المرحلة 2 — Operations (الأسابيع 5-7)

### Sprint 2.1: Invoices & Payments
- [ ] Invoice Aggregate
- [ ] Payment Aggregate
- [ ] Customer/Vendor entities
- [ ] Auto-posting to GL
- [ ] UI: Invoices

### Sprint 2.2: Projects Module
- [ ] Project, Task, Resource entities
- [ ] Project Budget (linked to Finance)
- [ ] Event-driven integration

### Sprint 2.3: Inventory Module
- [ ] Items + Warehouses
- [ ] Stock Movements (Receive, Issue, Transfer)
- [ ] Stock Levels (CQRS projections)
- [ ] Event: StockReceived → Finance Journal Entry

---

## 5️⃣ المرحلة 3 — Polish & Deploy (الأسابيع 8-10)

- [ ] Unit + Integration Tests (70% coverage)
- [ ] UAT + Bug Fixing
- [ ] Load Testing
- [ ] VPS Provisioning + Domain + SSL
- [ ] Production deployment

---

## 6️⃣ قرارات معتمدة

1. ✅ Dapper + FluentMigrator + MartenDB (بدل EF Core)
2. ✅ Next.js (Frontend)
3. ✅ Event-Driven Architecture بين الـ Modules
4. ✅ Modular Monolith → قابل للتحويل لـ Microservices

---

**ملاحظة:** هذه خطة قابلة للتعديل. سنعيد تقييمها بعد كل Sprint بناءً على ما تعلمناه.
