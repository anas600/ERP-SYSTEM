# 📝 CHANGELOG — ERP-SYSTEM

> سجل التغييرات الموثّقة. **آخر إدخال في الأعلى.**

---

## 2026-06-17 — تسوية التوثيق مع الكود الفعلي

### 🎯 الهدف
التوفيق بين ملفات التوثيق (`AGENTS.md` و `README.md`) والكود الفعلي في الـ repo. اكتشفنا **6 فروقات جوهرية** بين التوثيق والواقع، وعدّلنا الملفات لتعكس الحقيقة.

### 📊 ملخص التغييرات

| # | الملف | المشكلة | الحل |
|---|------|--------|------|
| 1 | `AGENTS.md` (root) | PostgreSQL 16 (التوثيق) ≠ 15 (PLAN.md والكود المحلي) | توحيد على **15** في كل الملفات |
| 2 | `AGENTS.md` (root) | ذكر "shadcn/ui + Tailwind" — لكن `package.json` يحوي shadcn-ui كـ CLI فقط، لا توجد `components/ui/` | توثيق **Tailwind CSS** فقط مع تنبيه |
| 3 | `AGENTS.md` (root) | Phase status قديم (لم يذكر Phase 2.5+ Frontend) | إضافة Phase 2.5+ ✅ |
| 4 | `src/frontend/AGENTS.md` | يذكر shadcn + هيكل outdated | إعادة كتابة كاملة بهيكل Phase 2.5+ |
| 5 | `src/frontend/lib/api.ts` | 🐛 **bug:** `RegisterRequest` يحوي `subdomain` غير مستخدم في الـ backend | إزالة `subdomain`، إضافة `baseCurrency` |
| 6 | `src/frontend/lib/api.ts` | 🐛 **bug:** `LoginRequest` يحوي `tenantSubdomain` (string) لكن الـ backend يستقبل `tenantId` (Guid) | استبدال بـ `tenantId?: string` |
| 7 | `src/frontend/app/register/page.tsx` | 🐛 **bug:** حقل `subdomain` في الـ form يُرسل لكن الـ backend يتجاهله | إزالة الحقل، إضافة hint عن Slugify |
| 8 | `src/backend/Modules/Identity/AGENTS.md` | لا يذكر `BaseCurrency` ولا Slugify للـ Subdomain | إضافة قسم AuthResponse، توثيق Subdomain يُحسب تلقائياً |
| 9 | `infra/docker/AGENTS.md` | يذكر postgres:16-alpine (مخالف) + شرح ضعيف لـ init scripts | تحديث لـ 15-alpine + قسم init-scripts مفصّل |
| 10 | `infra/docker/docker-compose.dev.yml` | `postgres:16-alpine` | → `postgres:15-alpine` |
| 11 | `README.md` | الحالة = "Phase 0"، لا يذكر Frontend أو Setup بدون Docker | تحديث الحالة، إضافة رابط لـ SETUP-LOCAL.md |
| 12 | `docs/SETUP-LOCAL.md` | غير موجود | 🆕 جديد — دليل التشغيل بدون Docker |
| 13 | `docs/CHANGELOG.md` | غير موجود | 🆕 هذا الملف |
| 14 | `src/backend/Host/Program.cs` | 🔴 **bug:** `ConnectionMultiplexer.Connect(redisConn)` يفشل → `/health/live` و `/health/ready` يرجعون 500 | إضافة `AbortOnConnectFail = false` + `ConnectTimeout = 2000` |
| 15 | `src/backend/Modules/Projects/Application/Validators.cs` | 🔴 **bug:** `UpdateProjectRequestValidator` مفقود → `GET /api/projects` يرجع 500 | إنشاء `UpdateProjectRequestValidator` (Name, Budget, StartDate, EndDate) |
| 16 | `src/backend/AGENTS.md` | Phase status outdated: "📋 Phase 1" لـ Finance، "📋 Phase 2" لـ Projects/Inventory | تحديث لكل الموديولات "✅ مكتمل" |
| 17 | `src/backend/Host/AGENTS.md` | لا يذكر Health endpoints الفعلية ولا قواعد Validators | إضافة جدول Health endpoints + قاعدة "كل Request DTO يحتاج validator" |
| 18 | `docs/SMOKE-TEST-REPORT.md` | غير موجود | 🆕 تقرير backend-architect للـ smoke test |
| 19 | `docs/FINAL-INTEGRATION-REPORT.md` | غير موجود | 🆕 تقرير شامل نهائي |

---

### 🐛 تفاصيل الـ Bugs

#### Bug #1: `RegisterRequest` يحوي `subdomain` غير مستخدم

**قبل (frontend):**
```typescript
// src/frontend/lib/api.ts
export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
  tenantName: string;
  subdomain: string;        // ❌ الـ backend يتجاهله
}
```

**الـ backend (الكود الفعلي `AuthDtos.cs`):**
```csharp
public sealed class RegisterRequest
{
    public Guid TenantId { get; set; }            // Guid.Empty = tenant جديد
    public string Email { get; set; }
    public string Password { get; set; }
    public string FullName { get; set; }
    public string? TenantName { get; set; }       // لإنشاء tenant جديد
    public string BaseCurrency { get; set; } = "LYD";
    // ❌ لا يوجد "Subdomain" — يُحسب من TenantName
}
```

**`AuthService.cs` (السطر 33):**
```csharp
tenant = new Tenant {
    Id = Guid.NewGuid(),
    Name = req.TenantName!,
    Subdomain = Slugify(req.TenantName!),  // ✅ يُحسب تلقائياً
    IsActive = true,
    CreatedAt = DateTime.UtcNow
};
```

**النتيجة:** الـ backend كان يحفظ `Subdomain = Slugify("شركة الأمل")` (مثلاً: `"shrkt-laml"`) ويتجاهل ما يرسله الـ frontend.

**الإصلاح:**
- إزالة `subdomain` من `RegisterRequest` في `lib/api.ts`
- إزالة حقل الـ subdomain من `app/register/page.tsx`
- إضافة hint: "سيُولَّد subdomain شركتك تلقائياً من اسمها"

#### Bug #2: `LoginRequest` يحوي `tenantSubdomain` (string) بدل `tenantId` (Guid)

**قبل (frontend):**
```typescript
export interface LoginRequest {
  email: string;
  password: string;
  tenantSubdomain?: string;  // ❌ الـ backend لا يتعرف عليه
}
```

**الـ backend:**
```csharp
public sealed class LoginRequest {
    public string Email { get; set; }
    public string Password { get; set; }
    public Guid? TenantId { get; set; }          // ✅ Guid? فقط
}
```

**الإصلاح:** استبدال `tenantSubdomain` بـ `tenantId?: string` (Guid as string).

---

### 🔄 التغييرات في Tech Stack

| البُعد | القديم | الجديد | السبب |
|------|--------|--------|-------|
| **PostgreSQL** | 16 (AGENTS) / 15 (PLAN.md) | **15** موحّد | PLAN.md v2.0 يعتمد 15 (متوفر، API مستقر)؛ الكود المحلي مُختبَر على 15.18 |
| **Frontend UI** | shadcn/ui + Tailwind | **Tailwind CSS** (shadcn CLI غير مستخدم) | `package.json` يحوي `shadcn-ui@0.8.0` كـ CLI فقط؛ لا `components/ui/` |

---

### 🆕 ملف جديد: `docs/SETUP-LOCAL.md`

دليل عملي مُبسَّط للتشغيل بدون Docker، يستهدف:
- مطوّر يعمل على **Windows** مع PostgreSQL 15 محلي (مثل المالك في يونيو 2026)
- البيئات التي لا يتوفر فيها Docker
- الـ quick testing / prototyping

**يغطّي:**
- تثبيت PostgreSQL 15 (Windows installer)
- إنشاء user + databases عبر `psql`
- تشغيل الـ Backend (dotnet run)
- تشغيل الـ Frontend (npm run dev)
- Health checks
- إنشاء أول حساب عبر API
- Troubleshooting شائع

---

### 📁 الملفات المُعدَّلة

```
ERP-SYSTEM/
├── AGENTS.md                                          [تعديل] PostgreSQL 15, shadcn, Phase 2.5+
├── README.md                                          [تعديل] الحالة، رابط SETUP-LOCAL
├── docs/
│   ├── CHANGELOG.md                                   [جديد] هذا الملف
│   ├── SETUP-LOCAL.md                                 [جديد] دليل التشغيل بدون Docker
├── infra/
│   └── docker/
│       ├── AGENTS.md                                  [تعديل] init-scripts مفصّل، 15-alpine
│       └── docker-compose.dev.yml                     [تعديل] postgres:15-alpine
└── src/
    ├── backend/
    │   └── Modules/
    │       └── Identity/
    │           └── AGENTS.md                          [تعديل] BaseCurrency, Slugify, AuthResponse
    └── frontend/
        ├── AGENTS.md                                  [إعادة كتابة] Phase 2.5+ الفعلي
        ├── app/
        │   └── register/
        │       └── page.tsx                           [إصلاح bug] إزالة حقل subdomain
        └── lib/
            └── api.ts                                 [إصلاح bug] RegisterRequest/LoginRequest
```

---

### ✅ التحقق

- [x] `dotnet build` يمر بدون أخطاء
- [x] الـ migrations تُطبَّق بنجاح على PostgreSQL 15.18 محلي
- [x] الـ Backend يستمع على `http://localhost:5000`
- [x] `GET /health` → 200، `GET /health/ready` → 200 (Postgres ready)
- [x] الـ Frontend types في `lib/api.ts` تطابق `AuthDtos.cs`
- [x] لا حقول `subdomain` أو `tenantSubdomain` في الـ frontend types

---

### 📚 مرجع

- **التقرير الذي قاد لهذه التسوية:** التحليل الذي قارن `ERP-SETUP-GUIDE.md` (دليل السحابة الأصلي) مع الكود الفعلي، واكتشف 6 فروقات.
- **PLAN.md v2.0:** يوثّق أن PostgreSQL انخفض من 16 إلى 15 في مرحلة لاحقة من التطوير.
- **الكود الفعلي المعتمد كأساس لكل التعديلات:** `AuthDtos.cs`، `AuthService.cs`، `Program.cs`، `lib/api.ts`، `register/page.tsx`.

---

## [أقدم] — لم تُوثَّق تغييرات سابقة في هذا الملف

> AGENTS السابقة لم تكن تحتفظ بـ CHANGELOG. هذا أول إدخال رسمي.
> التغييرات السابقة موثّقة في git history عبر commit messages.
