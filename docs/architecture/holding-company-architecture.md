# 🏛️ الوثيقة الهندسية — معمارية نظام ERP-SYSTEM
## وفق مفهوم الشركة القابضة

**الإصدار:** v1.0
**التاريخ:** 2026-07-29
**المؤلف:** محمد (Mavis) — Strategic Advisor
**الحالة:** معمارية مُجمّدة (per CONSTITUTION.md + DEC-070)
**المرجع الأساسي:** `CONSTITUTION.md` (الموجود في جذر المستودع)

---

## 📑 المحتويات

1. [الملخص التنفيذي](#1-الملخص-التنفيذي)
2. [الأساس الدستوري](#2-الأساس-الدستوري)
3. [مفهوم الشركة القابضة](#3-مفهوم-الشركة-القابضة)
4. [المعمارية العامة للنظام](#4-المعمارية-العامة-للنظام)
5. [معمارية الـ Holding](#5-معمارية-الـ-holding)
6. [معمارية Multi-Company](#6-معمارية-multi-company)
7. [مخطط قاعدة البيانات](#7-مخطط-قاعدة-البيانات)
8. [تصميم الـ API](#8-تصميم-الـ-api)
9. [الأمان والمصادقة](#9-الأمان-والمصادقة)
10. [معمارية الواجهات](#10-معمارية-الواجهات)
11. [هيكل الـ Modules](#11-هيكل-الـ-modules)
12. [تدفق البيانات](#12-تدفق-البيانات)
13. [معمارية النشر](#13-معمارية-النشر)
14. [الـ Cross-cutting Concerns](#14-الـ-cross-cutting-concerns)
15. [القواعد الـ 10 الهندسية](#15-القواعد-الـ-10-الهندسية)
16. [القرارات المعمارية المجمّدة](#16-القرارات-المعمارية-المجمّدة)
17. [الـ Roadmap](#17-الـ-roadmap)

---

## 1. الملخص التنفيذي

### 🎯 الرؤية

نظام **ERP-SYSTEM** مصمم خصيصاً لـ **الشركة القابضة الليبية** (Holding Company) اللي تملك عدة شركات تابعة (Subsidiaries). النظام يخدم:

- **الشركة القابضة (Holding)** — كيان إداري/مالي مركزي
- **الشركات التابعة (Companies)** — كيانات تشغيلية مستقلة
- **المستخدمين (Users)** — عبر الشركات، بصلاحيات محددة

### 🏗️ المعمارية في جملة واحدة

> **Holding واحد (شركة أم) + شركات متعددة (تابعة) = نظام واحد متعدد الشركات (Multi-Company) — بدون Multi-Tenancy.**

### 🔑 المبدأ الأساسي

| ❌ اللي ما عندناش | ✅ اللي عندنا |
|------------------|--------------|
| Multi-Tenant | Multi-Company |
| `tenant_id` | `company_id` |
| `Tenant` entity | `Holding` entity (واحد) |
| `TenantContext` | `CompanyContext` |
| `TenantMiddleware` | `CompanyMiddleware` |
| `[TenantAuthorize]` | `[CompanyAuthorize]` |
| Tenant isolation كامل | Company isolation منطقي |

### 📊 الإحصائيات

| المقياس | القيمة |
|---------|--------|
| عدد الـ sprints المنفذة | 3 (من 4) |
| عدد الـ PRs المدمجة | 167+ PR |
| عدد الـ DECs | 70+ |
| المعمارية | Frozen (Phase 6 complete) |
| Database | PostgreSQL 17.6 (Supabase) |
| Backend Stack | C# / .NET 9 / Dapper / FluentMigrator |
| Frontend Stack | Next.js 14 / TypeScript |
| اللغات | Arabic (RTL) + English |

---

## 2. الأساس الدستوري

### 📜 CONSTITUTION.md (الجذر)

النظام يخضع لـ **دستور** مكتوب في `CONSTITUTION.md` بجذر المستودع، يحتوي على **7 مواد**:

| المادة | الموضوع | الأثر المعماري |
|--------|---------|----------------|
| **Article 1** | الـ Mission | هدف النظام |
| **Article 2** | الـ Scope | ما يدخل/لا يدخل |
| **Article 3** | **Multi-Company (لا Multi-Tenant)** | **جوهري** — يحدد كل المعمارية |
| **Article 4** | Data Ownership | ملكية البيانات لكل شركة |
| **Article 5** | Security Boundaries | حدود الأمان |
| **Article 6** | Development Process | عملية التطوير |
| **Article 7** | Amendment Process | تعديل الدستور |

### ⚖️ ترتيب الأولويات

```
CONSTITUTION.md (أعلى صلاحية)
    ↓
DEC-NNN (قرارات معمارية)
    ↓
AGENTS.md (إرشادات الفريق)
    ↓
CHANGELOG.md (تاريخ التغييرات)
```

**أي قرار يخالف CONSTITUTION = مرفوض تلقائياً.**

---

## 3. مفهوم الشركة القابضة

### 🏢 الهيكل التنظيمي

```
┌─────────────────────────────────────────────────────────────┐
│                    الشركة القابضة (Holding)                  │
│                     [مستوى إداري/مالي]                       │
│                                                              │
│  - Holding Dashboard (شامل)                                 │
│  - Consolidated Reports (موحد)                               │
│  - Cross-Company Analytics                                  │
│  - Treasury Management (إدارة الخزينة المركزية)             │
│  - Strategic Planning                                       │
└────────────────────┬────────────────────────────────────────┘
                     │ 1:N
       ┌─────────────┼─────────────┐
       ↓             ↓             ↓
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│  الشركة 1    │ │  الشركة 2    │ │  الشركة 3    │
│  [تابعة]     │ │  [تابعة]     │ │  [تابعة]     │
│              │ │              │ │              │
│ - عمليات     │ │ - عمليات     │ │ - عمليات     │
│ - موظفين     │ │ - موظفين     │ │ - موظفين     │
│ - تقارير     │ │ - تقارير     │ │ - تقارير     │
│   مستقلة     │ │   مستقلة     │ │   مستقلة     │
└──────────────┘ └──────────────┘ └──────────────┘
```

### 🎯 الفروقات الجوهرية

| الجانب | Multi-Tenant ❌ | Multi-Company ✅ |
|--------|----------------|------------------|
| **الـ Tenants** | عزل كامل | **لا يوجد** |
| **الـ Holding** | غير موجود | **موجود — واحد فقط** |
| **التقارير الموحدة** | صعبة/مستحيلة | **سهلة — join على company_id** |
| **Consolidation** | يدوي | **تلقائي** |
| **إدارة الشركة الأم** | غير ممكنة | **ممكنة — Holding Dashboard** |
| **Data leakage** | عزل كامل (قوي) | **عزل منطقي (لكن موحد)** |

### 💼 حالات الاستخدام

1. **موظف في الشركة 1** يفتح `/dashboard` → يشوف **فقط** بيانات الشركة 1
2. **مدير مالي في Holding** يفتح `/holding/dashboard` → يشوف **موحد** كل الشركات
3. **موظف في الشركة 1** يحاول يفتح بيانات الشركة 2 → **403 Forbidden**
4. **مدير الشركة 1** يدير موظفي الشركة 1 فقط
5. **Admin في Holding** يدير كل الشركات + الـ Holding نفسه

---

## 4. المعمارية العامة للنظام

### 🏛️ الطبقات (Layers)

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                        │
│  Next.js 14 (TypeScript) + RTL + Arabic                      │
│  - Pages (app/)                                             │
│  - Components (components/)                                  │
│  - Hooks (hooks/)                                            │
│  - API client (lib/api.ts)                                  │
└────────────────────────┬────────────────────────────────────┘
                         │ HTTPS + JWT
┌────────────────────────┴────────────────────────────────────┐
│                    Application Layer                         │
│  ASP.NET Core 9 (C#) + RESTful API                          │
│  - Controllers (Host/Controllers/)                           │
│  - Services (Modules/*/Application/Services/)                │
│  - DTOs (Modules/*/Application/DTOs/)                       │
│  - Middleware (JWT, Company, Logging)                       │
└────────────────────────┬────────────────────────────────────┘
                         │ SQL via Dapper
┌────────────────────────┴────────────────────────────────────┐
│                    Domain Layer                              │
│  - Entities (Modules/*/Domain/Entities/)                     │
│  - Business Rules                                            │
│  - Domain Events                                             │
└────────────────────────┬────────────────────────────────────┘
                         │ Repositories
┌────────────────────────┴────────────────────────────────────┐
│                  Infrastructure Layer                        │
│  - Repositories (Modules/*/Infrastructure/)                  │
│  - Migrations (FluentMigrator)                               │
│  - Dapper (.NET SQL micro-ORM)                               │
└────────────────────────┬────────────────────────────────────┘
                         │ PostgreSQL protocol
┌────────────────────────┴────────────────────────────────────┐
│                       Data Layer                              │
│  PostgreSQL 17.6 (Supabase)                                 │
│  - 34+ tables                                               │
│  - JSONB columns (flexible)                                  │
│  - Row-Level Security (RLS) for company_id                  │
└─────────────────────────────────────────────────────────────┘
```

### 🧩 الـ Patterns

| Pattern | الموقع | الغرض |
|---------|--------|-------|
| **Clean Architecture** | الكل | فصل الطبقات |
| **Repository** | `Modules/*/Infrastructure/Repositories` | عزل DB |
| **CQRS (خفيف)** | Services | فصل Read/Write |
| **Dependency Injection** | `Program.cs` | Loose coupling |
| **Middleware Pipeline** | `Host/Middleware/` | JWT, Company, Logging |
| **DTO Pattern** | `Modules/*/Application/DTOs` | Contract |
| **Migration** | FluentMigrator | Versioned schema |

---

## 5. معمارية الـ Holding

### 🏢 الـ Holding ككيان من الدرجة الأولى

الـ **Holding ليس tenant** — هو **شركة فعلية** في الـ schema، لكن بـ flag:

```sql
-- holdings table
CREATE TABLE holdings (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name TEXT NOT NULL,
  name_ar TEXT,                    -- Arabic name
  base_currency CHAR(3) NOT NULL,  -- e.g. LYD
  fiscal_year_start DATE,          -- e.g. '01-01'
  timezone TEXT DEFAULT 'Africa/Tripoli',
  locale TEXT DEFAULT 'ar-LY',     -- Arabic (Libya)
  settings JSONB DEFAULT '{}',
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

**ملاحظة:** عندنا **holding واحد فقط** (per CONSTITUTION Article 3). لو زاد → يحتاج amendment.

### 🎯 الـ Holding Dashboard

الـ `/holding/dashboard` يعرض:

| الـ Widget | المصدر | الحساب |
|-----------|--------|--------|
| **إجمالي الإيرادات** | كل الشركات | `SUM(companies.revenue)` |
| **إجمالي المصروفات** | كل الشركات | `SUM(companies.expenses)` |
| **صافي الربح** | كل الشركات | revenue - expenses |
| **عدد الشركات** | companies | `COUNT(*)` |
| **عدد الموظفين** | users + user_companies | `COUNT(DISTINCT user_id)` |
| **آخر المعاملات** | transactions | `ORDER BY created_at DESC LIMIT 20` |
| **Treasury Status** | bank_accounts | `SUM(balance)` |

### 📊 الـ Consolidated Reports

```sql
-- مثال: تقرير موحد لكل الشركات
SELECT 
  c.id AS company_id,
  c.name AS company_name,
  COALESCE(SUM(t.credit), 0) AS total_revenue,
  COALESCE(SUM(t.debit), 0) AS total_expenses
FROM companies c
LEFT JOIN transactions t ON t.company_id = c.id
WHERE c.holding_id = $1
GROUP BY c.id, c.name;
```

---

## 6. معمارية Multi-Company

### 🏢 الشركات (Companies)

الشركات تشكل **تسلسل هرمي ذاتي المرجع** (`self-referencing hierarchy`):
- **الـ Holding** (الجذر) = صف في `companies` بـ `is_group = true` و `parent_company_id IS NULL`.
- **الشركات التابعة** تشير لـ parent عبر `parent_company_id` (Self-FK → `companies.id`).
- **الشركات الوسيطة** (subsidiaries of subsidiaries) = صف بـ `is_group = false` و `parent_company_id` يشير لشركة أخرى.
- **لا يوجد جدول `holdings` منفصل** — كل الكيانات في `companies` (single-table design).

```sql
-- companies table (single-table self-referencing hierarchy)
CREATE TABLE companies (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  code VARCHAR(20) NOT NULL,
  name VARCHAR(200) NOT NULL,
  slug VARCHAR(100),                          -- URL-friendly, unique (added Sprint 1)
  legal_name VARCHAR(200),
  parent_company_id UUID                      -- self-FK (the Holding is parent_company_id = NULL with is_group = true)
    REFERENCES companies(id) ON DELETE SET NULL,
  is_group BOOLEAN NOT NULL DEFAULT false,    -- Holding identification: is_group=true + parent_company_id IS NULL
  base_currency CHAR(3) NOT NULL DEFAULT 'LYD',
  is_active BOOLEAN NOT NULL DEFAULT true,
  settings JSONB DEFAULT '{}',
  created_at TIMESTAMPTZ DEFAULT NOW(),
  updated_at TIMESTAMPTZ DEFAULT NOW(),
  CONSTRAINT uk_companies_code UNIQUE (code)
);
CREATE INDEX ix_companies_parent ON companies(parent_company_id);
CREATE INDEX ix_companies_slug ON companies(slug);
```

### 👥 المستخدمون والشركات (user_companies)

```sql
-- user_companies join table
CREATE TABLE user_companies (
  user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
  role TEXT NOT NULL,                  -- 'admin', 'manager', 'accountant', 'viewer'
  is_primary BOOLEAN DEFAULT false,   -- الشركة الأساسية
  joined_at TIMESTAMPTZ DEFAULT NOW(),
  PRIMARY KEY (user_id, company_id)
);

CREATE INDEX idx_user_companies_company ON user_companies(company_id);
CREATE INDEX idx_user_companies_user ON user_companies(user_id);
```

### 🔐 مبدأ العزل

> **كل صف في أي جدول "company-scoped" لازم يكون فيه `company_id`.**

**الـ Tables اللي فيها `company_id`:**
- `accounts`
- `transactions`
- `bank_accounts`
- `customers`
- `suppliers`
- `invoices`
- `reports`
- `audit_logs`

### 🔄 تدفق عزل البيانات

```
Request → JWT (company_ids[]) → CompanyContext
   ↓
Controller: X-Company-Id header (current company)
   ↓
Service: Filter queries by company_id
   ↓
Repository: WHERE company_id = $current_company
   ↓
DB: Row-Level Security (defense in depth)
```

> **ملاحظة (Sprint 10 — Jimi 3 fix):** Per Sprint 8 T4 refactor proposal — the architecture is single-table self-referencing, not the original two-table design. الـ Holding = `companies` row بـ `is_group=true`، لا جدول منفصل. انظر أيضاً: `docs/workflow/sprint-10-holding-refactor-phase-2.md` للـ Phase 2 (rename `Shared/MultiTenancy/`) و Phase 3 (scoped DI).

---

## 7. مخطط قاعدة البيانات

### 📊 الـ ERD (مختصر)

```
┌──────────────┐         ┌──────────────┐
│   holdings   │ 1     N │   companies  │
│              ├─────────┤              │
│ - id         │         │ - id         │
│ - name       │         │ - holding_id │
│ - currency   │         │ - name       │
└──────┬───────┘         │ - currency   │
       │ 1               └──────┬───────┘
       │ N                      │ N
       │                        │ N
       │                        │
       ↓ 1                    N ↓
┌──────────────┐         ┌──────────────┐
│    users     │  M    N │  user_companies│
│              ├─────────┤              │
│ - id         │         │ - user_id    │
│ - email      │         │ - company_id │
│ - name       │         │ - role       │
└──────┬───────┘         │ - is_primary │
       │ 1               └──────────────┘
       │ N
       ↓
┌──────────────┐  N
│ transactions ├──── 1 → companies
│              │
│ - id         │
│ - company_id │ (FK)
│ - account_id │
│ - amount     │
└──────────────┘
```

### 🗂️ الـ 34 جدول (categories)

| الفئة | الجداول |
|------|---------|
| **Identity** | `users`, `user_companies`, `roles`, `permissions`, `refresh_tokens` |
| **Organization** | `holdings`, `companies`, `departments`, `positions` |
| **Finance** | `accounts`, `transactions`, `bank_accounts`, `currencies`, `exchange_rates` |
| **Customers/Suppliers** | `customers`, `suppliers`, `contacts` |
| **Sales/Purchases** | `invoices`, `invoice_lines`, `purchase_orders` |
| **Inventory** | `items`, `warehouses`, `stock_movements` |
| **HR** | `employees`, `salaries`, `attendance` |
| **Reports** | `reports`, `report_templates` |
| **Audit** | `audit_logs`, `activity_logs`, `notifications` |
| **Settings** | `settings`, `feature_flags` |

### 🔒 Row-Level Security (RLS)

```sql
-- مثال: RLS على transactions
ALTER TABLE transactions ENABLE ROW LEVEL SECURITY;

CREATE POLICY company_isolation ON transactions
  USING (company_id = ANY(current_setting('app.company_ids')::uuid[]));
```

**Defense in depth** — حتى لو الـ app code فيه bug، الـ DB يرفض.

---

## 8. تصميم الـ API

### 🌐 REST Conventions

| Method | المسار | الغرض | Auth |
|--------|--------|-------|------|
| `GET` | `/api/companies` | قائمة الشركات (paginated) | ✅ JWT |
| `GET` | `/api/companies/{id}` | تفاصيل شركة | ✅ + company check |
| `POST` | `/api/companies` | إنشاء شركة (Holding admin) | ✅ + role check |
| `PUT` | `/api/companies/{id}` | تعديل شركة | ✅ + permission |
| `DELETE` | `/api/companies/{id}` | حذف (soft) | ✅ + role check |
| `GET` | `/api/users` | قائمة المستخدمين | ✅ JWT |
| `GET` | `/api/users/{id}/companies` | شركات المستخدم | ✅ |
| `GET` | `/api/activity/recent` | آخر النشاطات | ✅ + company filter |
| `GET` | `/api/notifications` | الإشعارات | ✅ + user filter |

### 📋 الـ Response Format

**Success:**
```json
{
  "items": [...],
  "total": 42,
  "page": 1,
  "pageSize": 20,
  "hasMore": true
}
```

**Error:**
```json
{
  "error": {
    "code": "FORBIDDEN_COMPANY_ACCESS",
    "message_ar": "ليس لديك صلاحية للوصول لهذه الشركة",
    "message_en": "You don't have access to this company",
    "traceId": "abc-123"
  }
}
```

### 🔑 الـ Headers المهمة

| Header | الغرض | مثال |
|--------|-------|------|
| `Authorization` | JWT token | `Bearer eyJhbGc...` |
| `X-Company-Id` | الشركة الحالية | `550e8400-e29b-41d4-a716-446655440000` |
| `Accept-Language` | اللغة | `ar-LY` أو `en-US` |
| `X-Request-Id` | Trace ID | UUID |

### 📊 Pagination

- Default: `?page=1&pageSize=20`
- Max: `pageSize=100` (clamped)
- Response: `{ items, total, page, pageSize, hasMore }`

---

## 9. الأمان والمصادقة

### 🔐 JWT Structure

```json
{
  "sub": "user-uuid",
  "email": "user@example.com",
  "holding_id": "holding-uuid",
  "company_ids": ["company-1-uuid", "company-2-uuid"],
  "primary_company_id": "company-1-uuid",
  "roles": ["manager", "accountant"],
  "permissions": ["read:transactions", "write:invoices"],
  "iat": 1690502400,
  "exp": 1690588800
}
```

### 🛡️ Auth Flow

```
1. User → POST /api/auth/login { email, password }
2. Server → Verify password (bcrypt)
3. Server → Load user_companies
4. Server → Generate JWT with company_ids[]
5. Server → Return { accessToken, refreshToken, user, companies }
6. Client → Store in httpOnly cookie + memory
7. Client → Subsequent requests: Authorization: Bearer <token>
8. Client → Set X-Company-Id header (active company)
```

### 🛡️ Company Switcher

المستخدم عنده عدة شركات → يقدر يبدّل بينهم:

```
GET /api/companies/me           → قائمة شركاتي
POST /api/auth/switch-company   → يولد JWT جديد بـ primary_company_id الجديد
   body: { company_id: "..." }
   response: { accessToken (new), company_id, role }
```

### 🔐 Permission Matrix

| Role | Companies | Users | Transactions | Reports |
|------|-----------|-------|--------------|---------|
| **holding_admin** | CRUD all | CRUD all | CRUD all | All |
| **company_admin** | R own | CRUD own | CRUD own | All own |
| **manager** | R own | R own | R/U own | All own |
| **accountant** | R own | R own | R/U own | All own |
| **viewer** | R own | R own | R own | R own |

### 🚫 Forbidden Patterns

- ❌ Cross-company access بدون permission
- ❌ Holding data leak لـ company user
- ❌ Soft-delete بدون audit log
- ❌ Password storage بدون bcrypt
- ❌ JWT في localStorage (XSS risk) → httpOnly cookie

---

## 10. معمارية الواجهات

### 🎨 Next.js 14 App Router

```
src/frontend/
├── app/                              # Routes (App Router)
│   ├── (authenticated)/              # Protected routes
│   │   ├── dashboard/
│   │   ├── holding/                  # Holding-level
│   │   │   └── dashboard/page.tsx
│   │   ├── admin/
│   │   │   ├── companies/
│   │   │   │   ├── page.tsx
│   │   │   │   └── [id]/page.tsx
│   │   │   └── users/
│   │   │       ├── page.tsx
│   │   │       └── [id]/page.tsx
│   │   ├── activity/
│   │   ├── notifications/
│   │   └── layout.tsx                # Auth check + sidebar
│   ├── (public)/                     # Public routes
│   │   ├── login/
│   │   └── register/
│   ├── layout.tsx                    # Root layout
│   └── page.tsx                      # Landing
├── components/                       # Reusable
│   ├── ui/                          # shadcn/ui
│   ├── forms/
│   ├── tables/
│   └── charts/
├── lib/                             # Utilities
│   ├── api.ts                       # API client
│   ├── auth.ts                      # Auth helpers
│   ├── i18n.ts                      # AR/EN
│   └── rtl.ts                       # RTL utilities
├── hooks/                           # Custom hooks
│   ├── useAuth.ts
│   ├── useCompany.ts                # Company context
│   └── useApi.ts
├── i18n/                            # Translations
│   ├── ar.json
│   └── en.json
└── types/                           # TypeScript types
    ├── api.d.ts
    ├── company.d.ts
    └── user.d.ts
```

### 🌐 RTL + Arabic

- **Default direction:** `dir="rtl"`
- **Default locale:** `ar-LY`
- **Font:** Tajawal / Cairo (Google Fonts)
- **Numbers:** English (1, 2, 3) per Anas's preference
- **Date format:** Arabic-Indic (الأحد، ١٢ مارس ٢٠٢٦) + Gregorian option

### 🔄 State Management

| State | Tool | Scope |
|-------|------|-------|
| **Auth** | Context + httpOnly cookie | Global |
| **Company (active)** | Context + localStorage | Global |
| **API cache** | TanStack Query (React Query) | Per-page |
| **Form state** | React Hook Form + Zod | Per-form |
| **UI state** | Zustand | Per-feature |

### 📊 الـ Company Context

```typescript
// hooks/useCompany.ts
export const useCompany = () => {
  const context = useContext(CompanyContext);
  if (!context) throw new Error('useCompany must be inside CompanyProvider');
  return context;
};

// في كل API call:
const { activeCompanyId } = useCompany();
const res = await api.get('/api/transactions', {
  headers: { 'X-Company-Id': activeCompanyId }
});
```

---

## 11. هيكل الـ Modules

### 📂 Backend Modules (C#)

```
src/backend/
├── Host/                            # Entry point
│   ├── Program.cs                   # DI registration
│   ├── Controllers/                 # API endpoints
│   │   ├── CompaniesController.cs
│   │   ├── UsersController.cs
│   │   ├── TransactionsController.cs
│   │   └── HoldingController.cs
│   ├── Middleware/
│   │   ├── JwtMiddleware.cs
│   │   ├── CompanyContextMiddleware.cs
│   │   ├── GlobalExceptionMiddleware.cs
│   │   └── RequestLoggingMiddleware.cs
│   └── appsettings.json
├── Modules/                         # Business modules
│   ├── Companies/
│   │   ├── Domain/
│   │   │   └── Entities/
│   │   │       └── Company.cs
│   │   ├── Application/
│   │   │   ├── Services/
│   │   │   │   └── CompanyService.cs
│   │   │   └── DTOs/
│   │   │       └── CompanyDto.cs
│   │   └── Infrastructure/
│   │       ├── Repositories/
│   │       │   └── CompanyRepository.cs
│   │       └── Migrations/          # FluentMigrator
│   │           ├── 001_CreateCompanies.cs
│   │           └── 002_AddHoldingFK.cs
│   ├── Identity/
│   │   ├── Domain/ (User.cs, UserCompany.cs)
│   │   ├── Application/ (AuthService.cs, UserService.cs)
│   │   └── Infrastructure/ (UserRepository.cs)
│   ├── Finance/
│   │   ├── (Accounts, Transactions, BankAccounts)
│   ├── Treasury/
│   │   └── (Holding-level financial ops)
│   └── Holding/
│       └── (Holding-specific operations)
├── Shared/                          # Cross-cutting
│   ├── Dapper/
│   ├── Auth/
│   ├── Logging/
│   └── Exceptions/
└── Tests/
    ├── ERPSystem.Tests/
    │   ├── Companies/
    │   │   └── CompaniesListTests.cs
    │   ├── Identity/
    │   │   └── UserCompanyAccessTests.cs
    │   └── Common/
    │       └── FakeDbConnectionFactory.cs
```

### 🎯 Module Independence

- كل module له **Domain** + **Application** + **Infrastructure**
- الـ modules ما تعتمد على بعض إلا عبر **interfaces**
- الـ DI في `Program.cs` يسجل الكل
- الـ Migrations مستقلة لكل module (FluentMigrator)

### 📂 Frontend Feature Modules

```
src/frontend/
├── app/(authenticated)/
│   ├── admin/companies/             # Companies feature
│   │   ├── page.tsx                 # List
│   │   └── [id]/page.tsx            # Detail
│   ├── admin/users/                 # Users feature
│   ├── activity/                    # Activity feature
│   └── notifications/               # Notifications feature
├── components/
│   ├── companies/                   # Company-specific components
│   ├── users/
│   └── shared/
└── lib/
    ├── api/
    │   ├── companies.ts             # Company API client
    │   ├── users.ts
    │   └── activity.ts
    └── hooks/
        ├── useCompanies.ts
        └── useUsers.ts
```

---

## 12. تدفق البيانات

### 🔄 Login Flow

```
[User Browser]
   ↓ POST /api/auth/login { email, password }
[API: AuthController]
   ↓ Verify password (bcrypt)
[DB: users + user_companies]
   ↓ User found, load companies + roles
[JWT Service]
   ↓ Generate JWT with company_ids[]
[Response]
   ↓ { accessToken, refreshToken, user, companies }
[Browser]
   ↓ Store in httpOnly cookie + memory
```

### 🔄 API Request Flow

```
[Browser]
   ↓ GET /api/transactions + JWT + X-Company-Id
[API: GlobalExceptionMiddleware]
   ↓ Catch all exceptions
[API: JwtMiddleware]
   ↓ Validate JWT, set HttpContext.User
[API: CompanyContextMiddleware]
   ↓ Set CompanyContext from X-Company-Id
[API: CompanyAuthorizationFilter]
   ↓ Verify user has access to company
[Controller: TransactionsController.Get]
   ↓ Call Service
[Service: TransactionService.GetRecent]
   ↓ Apply company filter
[Repository: TransactionRepository.GetRecent]
   ↓ SQL: WHERE company_id = $1
[DB: PostgreSQL + RLS]
   ↓ Return rows
[Response]
   ↓ JSON { items, total, page, ... }
[Browser]
   ↓ React Query cache
```

### 🔄 Cross-Company Report (Holding-level)

```
[User: holding_admin]
   ↓ GET /api/holding/reports/consolidated + JWT
[Controller: HoldingController.ConsolidatedReport]
   ↓ Skip company filter (holding-level)
[Service: ReportService.Consolidated]
   ↓ Query all companies
[Repository: ReportRepository.ConsolidatedQuery]
   ↓ SQL: 
        SELECT c.name, SUM(t.credit), SUM(t.debit)
        FROM companies c
        LEFT JOIN transactions t ON t.company_id = c.id
        WHERE c.holding_id = $1
        GROUP BY c.name
[DB]
   ↓ Return aggregated rows
[Response]
   ↓ JSON { companies: [{name, revenue, expenses, profit}] }
```

### 🔄 Notification Flow (Sprint 3)

```
[Event in System]
   ↓ e.g. "Invoice created" → MediatR event
[Notification Service]
   ↓ Look up subscribers (user_companies + permissions)
[DB: notifications table]
   ↓ Insert notification rows
[SignalR Hub]
   ↓ Push real-time to connected clients
[Browser]
   ↓ Bell icon updates, badge counter
```

---

## 13. معمارية النشر

### 🌍 الـ 3-Layer DB Architecture (Frozen per DEC-070)

```
┌─────────────────────────────────────────────┐
│  Layer 1: DEV (Active)                       │
│  - Branch: develop                          │
│  - DB: Supabase dev project                  │
│  - URL: dev.erp-system.internal              │
│  - Access: Mavis Local + Mavis Cloud        │
└─────────────────────────────────────────────┘
              ↓ (manual gate)
┌─────────────────────────────────────────────┐
│  Layer 2: STAGING (Frozen)                  │
│  - Branch: release/staging                  │
│  - DB: Supabase staging project             │
│  - Status: FROZEN per DEC-070               │
│  - Unlock: Anas explicit approval            │
└─────────────────────────────────────────────┘
              ↓ (manual gate)
┌─────────────────────────────────────────────┐
│  Layer 3: PRODUCTION (Frozen)               │
│  - Branch: main                            │
│  - DB: Supabase production project          │
│  - Status: FROZEN per DEC-070               │
│  - Unlock: Anas explicit approval            │
└─────────────────────────────────────────────┘
```

### 🏗️ الـ Environments

| Environment | Stack | Domain | Deploy |
|------------|-------|--------|--------|
| **Local Dev** | Docker Compose | `localhost:5001` | `dotnet run` |
| **Cloud Dev** | HF Space | `anas-assasket-erp-system.hf.space` | Auto on PR merge |
| **Staging** | (Frozen) | (Frozen) | (Frozen) |
| **Production** | (Frozen) | (Frozen) | (Frozen) |

### 🐳 Local Docker (per DEC-068)

```yaml
# docker-compose.yml
services:
  api:
    build: ./src/backend
    ports: ["5001:8080"]
    environment:
      - ConnectionStrings__Default=Host=db;Database=erp;...
    depends_on: [db]
  
  frontend:
    build: ./src/frontend
    ports: ["3000:3000"]
    environment:
      - NEXT_PUBLIC_API_URL=http://localhost:5001
  
  db:
    image: postgres:17
    ports: ["5432:5432"]
    volumes: [pgdata:/var/lib/postgresql/data]
  
  migrations:
    build: ./src/backend
    command: dotnet run --project Migrate.cs
    depends_on: [db]
```

### ☁️ HF Space (Dev only, NOT production)

- **Stack:** Docker Space (HF)
- **CI/CD:** GitHub Actions → HF
- **Database:** Supabase (external)
- **URL:** `https://anas-assasket-erp-system.hf.space/` (lowercase canonical)
- **Note:** Used for **testing only**, NOT production per DEC-068

### 🔄 CI/CD Pipeline

```
PR opened
  ↓
CI checks (6 required):
  - Backend Tests (.NET 9.0)
  - Frontend Build (Next.js 14)
  - CodeQL
  - TruffleHog
  - Analyze (javascript-typescript)
  - Analyze (csharp)
  ↓
All green → Review required (1)
  ↓
Approved + admin bypass (DEC-070) → Auto-merge
  ↓
develop updated → Trigger HF Space rebuild
  ↓
[Production: Frozen, manual]
```

### 🛡️ Branch Protection (per DEC-070)

| Setting | Value |
|---------|-------|
| Required checks | 6 (Backend, Frontend, CodeQL, TruffleHog, Analyze×2) |
| Required reviews | 1 |
| Admin bypass | ✅ ON |
| Force-pushes | ✅ ENABLED (`--force-with-lease`) |
| Enforce admins | false |
| Linear history | ON |

---

## 14. الـ Cross-cutting Concerns

### 📝 Logging

- **Library:** Serilog
- **Sinks:** Console (dev) + Supabase (prod) + Sentry (errors)
- **Format:** Structured JSON with traceId
- **Levels:** Information (default), Warning, Error, Critical

### 🔍 Observability

| Concern | Tool | Coverage |
|---------|------|----------|
| **Logs** | Serilog + Supabase | 100% |
| **Metrics** | (Future) Prometheus | TBD |
| **Tracing** | TraceId in headers | 100% |
| **Errors** | Sentry | 100% |
| **Uptime** | Bridge cron (HF) | HF Space only |

### 🧪 Testing

- **Unit tests:** xUnit (.NET) + Jest (TS)
- **Integration tests:** WebApplicationFactory + Supabase test DB
- **E2E tests:** (Optional, per DEC-070 — not required for merge)
- **Coverage:** Basic only (per DEC-071)

### 🔒 Security

- **Auth:** JWT (HS256) + Refresh token rotation
- **Password:** bcrypt (cost 12)
- **HTTPS:** Always (TLS 1.3)
- **CORS:** Whitelist only (config-based)
- **Rate limiting:** ASP.NET Core RateLimiter
- **Input validation:** FluentValidation
- **SQL injection:** Dapper parameterized queries (immune)
- **XSS:** React auto-escapes + CSP headers
- **CSRF:** SameSite cookie + CSRF token

### 📊 Audit

```sql
CREATE TABLE audit_logs (
  id UUID PRIMARY KEY,
  user_id UUID,
  company_id UUID,
  action TEXT,                  -- 'create', 'update', 'delete', 'view'
  entity_type TEXT,             -- 'transaction', 'invoice', etc.
  entity_id UUID,
  old_values JSONB,
  new_values JSONB,
  ip_address INET,
  user_agent TEXT,
  trace_id UUID,
  created_at TIMESTAMPTZ DEFAULT NOW()
);
```

---

## 15. القواعد الـ 10 الهندسية

### 📋 The 10 Soft Rules (per DEC-070)

| # | Rule | التوضيح |
|---|------|---------|
| 1 | **One Branch (develop only)** | كل التغييرات في develop، ما في فوضى فروع |
| 2 | **API-First** | Backend قبل Frontend، contract واضح |
| 3 | **Idempotent Migrations** | `IF EXISTS` في كل migration، يشتغل أكتر من مرة |
| 4 | **One Test Per Endpoint** | Smoke test كافي (per DEC-071)، مش coverage عالي |
| 5 | **company_id Only** | لا `tenant_id`، لا multi-tenancy |
| 6 | **No EF Core** | Dapper + FluentMigrator فقط (NO EF) |
| 7 | **Pre-Demo Data (real, not mocks)** | بيانات حقيقية للـ demo، not fakes |
| 8 | **No Secrets in Code** | Env vars only، not in code or chat |
| 9 | **Frontend-First Errors** | رسائل واضحة بـ AR + EN للـ user |
| 10 | **Document in AGENTS.md** | كل قرار جديد → documented |

### 🚫 الـ 5 Anti-Patterns

| ❌ | ✅ |
|----|----|
| Over-engineering | YAGNI |
| Premature optimization | Profile first |
| Speculative features | Build what you need |
| Custom solutions | Use libraries |
| Long sync tasks | Async / queue |

---

## 16. القرارات المعمارية المجمّدة

### 🧊 Frozen Decisions (per DECs)

| DEC | الموضوع | الأثر |
|-----|---------|-------|
| **DEC-070** | Staging/Production FREEZE | لا work بدون Anas approval |
| **DEC-071** | Basic tests only | لا coverage عالي مطلوب |
| **DEC-072** | Presence check protocol | تواصل async محدد |
| **CONSTITUTION Art. 3** | Multi-Company (not Multi-Tenant) | معمارية كاملة |
| **CONSTITUTION Art. 7** | Amendment process | تعديل الدستور = موافقة Anas |

### 🟢 Active Decisions

| DEC | الموضوع | الأثر |
|-----|---------|-------|
| **Sprint Model** | 4 sprints + verify | خطة زمنية محددة |
| **DEC-073** | Token rotation policy | $GITHUB_TOKEN refresh |
| **Branch Protection** | 6 required checks | quality gate |
| **Admin Bypass** | ON (per DEC-070) | Mavis Local self-merge |

### 📋 Recent PRs (sprint evidence)

| Sprint | PR | Description | Status |
|--------|-----|-------------|--------|
| 1 | #165 | Dashboard + Holding | ✅ MERGED |
| 2 | #166 | Companies + Users | ✅ MERGED |
| 3 | #167 | Activity + Notifications | ✅ MERGED |
| 4 | (next) | Polish + Demo Data | ⏳ PENDING |

---

## 17. الـ Roadmap

### 🗓️ Demo Timeline (per Demo Roadmap)

```
2026-07-28 03:53 UTC = Hour 0
   ↓
Hour 0.5-2.5: Sprint 1 (DONE) — Dashboard + Holding
Hour 2.5-4.5: Sprint 2 (DONE) — Companies + Users
Hour 4.5-6.0: Sprint 3 (DONE) — Activity + Notifications
Hour 6.0-8.0: Sprint 4 (PENDING) — Polish + Demo Data
Hour 8.0-10.0: Verify+Deploy (PENDING) — QA + Local Docker
   ↓
2026-07-28 13:53 UTC = DEMO DEADLINE
   ↓
(Actual: 7h delay due to power outage → extended deadline 2026-07-29 04:45 UTC)
```

### 📋 Sprint 4 Scope (Pending)

| Block | Tasks | Time |
|-------|-------|------|
| **A: Demo Data** | 4 companies, 10 users, 100 transactions, Arabic content | 30 min |
| **B: Polish** | RTL final check, loading states, empty states | 30 min |
| **C: Errors** | Arabic + English error messages, validation | 30 min |
| **D: Verify** | Local Docker run, smoke tests, screenshots | 30 min |

### 🎯 Post-Demo Roadmap (Future)

| Priority | Feature | Phase |
|----------|---------|-------|
| **High** | Staging layer setup | Post-Demo |
| **High** | Production layer setup | Post-Demo |
| **Medium** | Inventory module | Phase 7 |
| **Medium** | HR module (full) | Phase 7 |
| **Low** | Treasury advanced | Phase 8 |
| **Low** | Mobile app | Phase 9 |

---

## 📎 الملاحق

### Appendix A: Glossary

| المصطلح | English | الوصف |
|---------|---------|-------|
| **الشركة القابضة** | Holding Company | الشركة الأم |
| **الشركة التابعة** | Subsidiary | شركة تحت Holding |
| **المعمارية متعددة الشركات** | Multi-Company Architecture | معمارية 1:N Holding:Companies |
| **التأجير المتعدد** | Multi-Tenant | ❌ مش مستخدم |
| **عزل البيانات** | Data Isolation | company_id filter |
| **الـ RLS** | Row-Level Security | Defense in depth |

### Appendix B: References

- `CONSTITUTION.md` (root)
- `docs/workflow/architecture.md`
- `docs/workflow/demo-roadmap.md`
- `docs/workflow/sprint-*.md`
- `docs/governance/DEC-*.md`
- `AGENTS.md`
- `CHANGELOG.md`
- `README.md`

### Appendix C: Contacts

| Role | Name | Session |
|------|------|---------|
| **Project Owner** | Anas (أنس) | — |
| **Architect (Cloud)** | Mavis (ميفيز) | 406067545768199 |
| **Tech Lead (Local)** | Mavis Local | mvs_c39a4f3aaa474a9899f87a4cd49d3645 |
| **Strategic Advisor** | محمد (Muhammad) | this session |
| **Cloud Coordinator** | سيتي (Siti) | 406067545768199 |
| **DevOps** | ديف (Dev) | — |
| **Backend Jimi** | Jimi تنفيذي | 408773242015948 |
| **Frontend Jimi** | Jimi تحليلي v2 | 422569594278107 |

---

## ✍️ التوقيع

> **هذه الوثيقة هندسية — معمارية — مجمّدة. أي تعديل يحتاج موافقة Anas (Project Owner) + تحديث في CONSTITUTION.md أو DEC جديد.**

**المؤلف:** محمد (Mavis) — Strategic Advisor
**التاريخ:** 2026-07-29
**الإصدار:** v1.0
**الحالة:** 🟢 ACTIVE (Frozen until next amendment)

---

**🔚 نهاية الوثيقة**
