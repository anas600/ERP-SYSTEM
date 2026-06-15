# 🚪 src/backend/Host/AGENTS.md

> نقطة الدخول للـ Backend (Program.cs + Controllers + Swagger).

## شو فيه

- `Program.cs` — composition root: DI registrations، middleware pipeline
- `Controllers/` — كل الـ HTTP endpoints
- `Swagger/` — Swagger customizations
- `appsettings.json` + `appsettings.Development.json` — التهيئة
- `ERP-SYSTEM.csproj` — الـ NuGet dependencies + Compile Includes للموديولات

## Conventions

- **Controllers فقط في Host** — منطق الأعمال في `Application/` layers
- **DI registrations في Program.cs** مرتبة: Logging → Config → Infrastructure → Application → Auth → API
- **Endpoint route**: دائماً `api/<module>/<resource>` — مثال: `api/auth/login`
- **Response shape**: إما typed DTO أو `ProblemDetails` للأخطاء
- **Authorize**: استخدم `[Authorize]` على الـ controller أو action، استخدم `[AllowAnonymous]` فقط على `register/login/refresh`

## لما تشتغل هنا

- عند إضافة module جديد:
  1. أنشئ folder في `Modules/<Name>/`
  2. عرّف الـ interfaces و DI registrations في Program.cs
  3. أنشئ Controller في `Host/Controllers/<Name>Controller.cs`
  4. أضف الـ migration في `Shared/Migrations/`
- عند تغيير DI: حدّث قسم DI registrations في هذا الـ AGENTS.md

## بعد التعديل

- تأكد أن `dotnet build` نظيف
- Swagger UI يعرض الـ endpoints الجديدة
- Health endpoints (`/health`, `/health/ready`) تشتغل

## مرتبطة بـ

- [`../AGENTS.md`](../AGENTS.md)
- [`../Shared/AGENTS.md`](../Shared/AGENTS.md)
