# 🎨 src/frontend/AGENTS.md

> Next.js 14 Frontend.

## شو فيه (حالياً)

```
frontend/
├── app/
│   └── page.tsx              # الصفحة الرئيسية (placeholder)
├── package.json              # Dependencies
└── (سيُضاف في Phase 1):
    ├── app/
    │   ├── auth/             # Login, Register pages
    │   ├── dashboard/
    │   └── api/              # Client-side API helpers
    ├── components/
    │   └── ui/               # shadcn components
    ├── lib/
    │   ├── api.ts            # Axios instance + interceptors
    │   └── auth.ts           # Token storage + refresh logic
    └── types/
```

## Tech Stack

- **Next.js 14** (App Router)
- **TypeScript 5.5+** (strict mode)
- **shadcn/ui** + **Tailwind CSS**
- **TanStack Query (React Query 5)** — server state
- **Axios** — HTTP client
- **Zod** — runtime validation
- **react-hook-form** + **@hookform/resolvers** — forms
- **date-fns** — date formatting
- **lucide-react** — icons

## Conventions

- **Functional components** فقط
- **Hooks** للمنطق المشترك (لا HOCs)
- **Server Components** افتراضياً، `'use client'` فقط عند الحاجة
- **API calls** عبر React Query (لا useEffect + fetch)
- **JWT storage**: `httpOnly cookies` (مستقبلي) أو `localStorage` (مؤقت في dev)
- **Axios interceptor** لتجديد Access Token تلقائياً
- **Comments بالعربي**، identifiers بالإنجليزي

## Auth Integration (Phase 0+)

- `POST /api/auth/login` → store tokens → redirect to `/dashboard`
- `POST /api/auth/refresh` → automatically on 401
- `GET /api/auth/me` → user info في React Query
- `POST /api/auth/logout` → clear tokens → redirect to `/login`

## لما تشتغل هنا

- بعد كل endpoint جديد في الـ Backend: أضف client helper في `lib/api.ts`
- صفحة جديدة: `app/<route>/page.tsx` + components folder
- Feature flag (مستقبلي): `if (featureFlags.X) { ... }`

## بعد التعديل

- `npm run type-check` نظيف
- `npm run lint` نظيف
- `npm run build` ينجح

## مرتبطة بـ

- [`../AGENTS.md`](../AGENTS.md)
- [`../backend/AGENTS.md`](../backend/AGENTS.md) — عقود الـ API
- [`../backend/Modules/Identity/AGENTS.md`](../backend/Modules/Identity/AGENTS.md) — Auth flow
