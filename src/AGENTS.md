# 💻 src/AGENTS.md

> كل الـ source code للمشروع (backend + frontend).

## شو فيه

- `backend/` — كود C# / ASP.NET Core
- `frontend/` — كود Next.js / TypeScript

## Conventions

- **Linting** و **formatting** تلقائي على الـ CI — لا تتجاوز warnings
- **No dead code** — لا تترك commented-out code
- **TODOs**: اكتب `// TODO(name): description` لقبولها مؤقتاً، ثم أنشئ issue

## لما تشتغل هنا

- قبل تعديل، اقرأ AGENTS.md للمجلد الفرعي
- حافظ على الـ boundary بين backend و frontend (لا تحطّ logic في الـ frontend)

## بعد التعديل

- شغّل `dotnet test` و `npm test` قبل commit
- تأكد من الـ build للطرفين

## مرتبطة بـ

- [`../AGENTS.md`](../AGENTS.md) — root
- [`backend/AGENTS.md`](backend/AGENTS.md)
- [`frontend/AGENTS.md`](frontend/AGENTS.md)
