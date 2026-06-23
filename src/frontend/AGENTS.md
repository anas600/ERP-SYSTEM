# 🎨 src/frontend/AGENTS.md

> Next.js 14 Frontend — Phase 2.5+ (مكتمل).

## شو فيه (حالياً)

```
frontend/
├── app/
│   ├── page.tsx                  # الصفحة الرئيسية
│   ├── layout.tsx                # Root layout (RTL, dir="rtl")
│   ├── globals.css               # Tailwind directives + globals
│   ├── login/page.tsx            # POST /api/auth/login
│   ├── register/page.tsx         # POST /api/auth/register
│   ├── dashboard/page.tsx        # Auth-gated landing
│   ├── finance/accounts/page.tsx # GET/POST /api/finance/accounts
│   ├── inventory/items/page.tsx  # GET /api/inventory/items
│   └── projects/page.tsx         # GET /api/projects
├── lib/
│   └── api.ts                    # Axios instance + JWT interceptors + Auth helpers
├── package.json
├── next.config.js
├── tailwind.config.js
└── tsconfig.json
```

## Tech Stack (الفعلية في package.json)

| الحزمة | الإصدار | الغرض |
|------|---------|-------|
| **next** | 14.2.0 | Framework |
| **react** + **react-dom** | 18.3 | UI runtime |
| **typescript** | 5.5+ | Strict mode |
| **tailwindcss** | 3.4 | **UI الوحيد المُطبَّق** |
| **axios** | 1.7+ | HTTP client + interceptors |
| **@tanstack/react-query** | 5.0+ | Server state (مُثبَّت) |
| **react-hook-form** | 7.52+ | Forms (مُثبَّت) |
| **zod** | 3.23+ | Runtime validation (مُثبَّت) |
| **@hookform/resolvers** | 3.7+ | zod ↔ react-hook-form |
| **date-fns** | 3.6+ | Date formatting |
| **lucide-react** | 0.400+ | Icons |
| **shadcn-ui** | 0.8.0 | ⚠️ **CLI generator فقط** (لم يُستخدم لإضافة components) |

> **ملاحظة مهمة:** AGENTS السابقة ذكرت "shadcn/ui" كـ UI components. هذا غير دقيق في يونيو 2026:
> - `shadcn-ui` package موجودة في devDependencies كـ **CLI** (لتوليد components).
> - لم يُشغَّل `shadcn-ui init` ولا `shadcn-ui add`، فمجلد `components/ui/` **غير موجود**.
> - كل الـ UI حالياً مكتوب بـ **Tailwind classes مباشرة** (انظر `app/register/page.tsx`).

## Conventions

- **Functional components** فقط
- **Hooks** للمنطق المشترك (لا HOCs)
- **Client Components** فقط عند الحاجة (`'use client'`)، الباقي Server Components
- **API calls** عبر `lib/api.ts` (Axios + interceptors)
- **JWT storage**: `localStorage` (مؤقت في dev) — `accessToken`, `refreshToken`, `user`
- **Axios interceptor** يجدد Access Token تلقائياً على 401
- **Comments بالعربي**، identifiers بالإنجليزي
- **RTL**: كل الـ pages تحوي `dir="rtl"` على `<main>` أو `<html>`

## Auth Integration (الفعلية)

### Auth API Contracts (مطابقة لـ `lib/api.ts` و `AuthDtos.cs`)

```typescript
// POST /api/auth/register
interface RegisterRequest {
  email: string;            // required, email format
  password: string;         // required, ≥8 chars, [A-Z], [a-z], [0-9]
  fullName: string;         // required, ≤200 chars
  tenantName: string;       // مطلوب لإنشاء tenant جديد (يُولّد Subdomain تلقائياً)
  baseCurrency?: string;    // optional, default "LYD"
  // ❌ لا يوجد حقل "subdomain" — يُحسب تلقائياً عبر Slugify(TenantName)
}

// POST /api/auth/login
interface LoginRequest {
  email: string;
  password: string;
  tenantId?: string;        // optional (Guid) — إن لم يُرسل، بحث شامل
  // ❌ لا يوجد "tenantSubdomain" — الـ backend لا يستقبله
}

// AuthResponse (مشترك بين register و login)
interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;  // ISO datetime
  refreshTokenExpiresAt: string; // ISO datetime
  user: UserInfo;
  holdingCompanyId: string;      // Guid — للـ multi-company bootstrap
}

interface UserInfo {
  id: string;             // Guid
  tenantId: string;       // Guid
  email: string;
  fullName: string;
  roles: string[];        // ["Admin", "Accountant", "ProjectManager", "Viewer"]
}
```

### مسارات الـ Auth

| المسار | الـ Method | يطلب Auth | الوصف |
|------|------------|----------|-------|
| `/api/auth/register` | POST | لا | ينشئ tenant + user، أو يضيف user لـ tenant موجود |
| `/api/auth/login` | POST | لا | يرجع tokens |
| `/api/auth/refresh` | POST | لا | يجدد tokens (Token rotation) |
| `/api/auth/logout` | POST | نعم | يلغي Refresh Token |
| `/api/auth/me` | GET | نعم | يرجع UserInfo من JWT claims |

### Flow (صفحة `/register`)

1. المستخدم يدخل: fullName, email, password, tenantName
2. `authApi.register({...})` → `POST /api/auth/register`
3. الـ backend يحسب `Subdomain = Slugify(tenantName)` تلقائياً
4. ينشئ `Tenant` + `User` + 4 default roles + يربط User بدور `Admin`
5. يخزّن `accessToken` + `refreshToken` + `user` في `localStorage`
6. `router.push('/dashboard')`

### Flow (صفحة `/login`)

1. المستخدم يدخل: email, password
2. `authApi.login({email, password})` → `POST /api/auth/login`
3. يخزّن tokens في `localStorage`
4. `router.push('/dashboard')`

## لما تشتغل هنا

- إضافة صفحة: `app/<route>/page.tsx` (functional component)
- إضافة API helper: في `lib/api.ts` (grouped: `authApi`, `financeApi`, `inventoryApi`, `projectsApi`)
- Feature جديدة: استخدم `react-hook-form` + `zod` (مُثبَّتان لكن غير مستخدمتين بعد في كل الـ pages)
- لإضافة shadcn فعلاً: `npx shadcn-ui@latest init` ثم `npx shadcn-ui@latest add button` (يتطلب قرار معماري)

## بعد التعديل

- `npm run type-check` نظيف
- `npm run lint` نظيف
- `npm run build` ينجح
- اختبار التسجيل والدخول من المتصفح

## مرتبطة بـ

- [`../AGENTS.md`](../AGENTS.md) — root
- [`../backend/AGENTS.md`](../backend/AGENTS.md) — عقود الـ API
- [`../backend/Modules/Identity/AGENTS.md`](../backend/Modules/Identity/AGENTS.md) — Auth flow
- [`../../AGENTS.md`](../../AGENTS.md) — tech stack الكامل
