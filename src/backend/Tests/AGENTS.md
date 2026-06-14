# 🧪 src/backend/Tests/AGENTS.md

> Unit + Integration tests.

## شو فيه

```
Tests/
└── ERPSystem.Tests/
    ├── ERPSystem.Tests.csproj
    └── Auth/
        ├── JwtTokenServiceTests.cs    # Token generation/validation
        └── ValidatorsTests.cs          # FluentValidation rules
```

## Conventions

- **xUnit** (لا NUnit ولا MSTest)
- **FluentAssertions** للـ assertions (`result.Should().Be...`)
- **Naming**: `<Class>Tests` لكل class يُختبر
- **Arrange-Act-Assert** pattern بوضوح
- **Tests مستقلة** — لا تعتمد على ترتيب التنفيذ
- **Mock dependencies** عبر Moq أو NSubstitute (عند الحاجة لـ DB)

## Types of Tests

### Unit Tests (سريع، لا IO)

- Pure logic: Validators، Token generation، Password hashing
- الـ Target: `Application/`, `Shared/`

### Integration Tests (يحتاج DB)

- Repository queries
- Auth flow كامل
- الـ Target: `Infrastructure/`, `Host/Controllers/`
- **يحتاج** Postgres test DB (موجود في CI)

## لما تضيف feature جديد

1. اكتب unit tests للـ services
2. اكتب integration tests للـ endpoints (إن أمكن)
3. تأكد: `dotnet test` يمر محلياً + على CI

## CI Integration

- `dotnet test` يعمل على CI مع Postgres + Redis services
- Test results تُرفع كـ artifact
- Coverage report (مستقبلي)

## مرتبطة بـ

- [`../AGENTS.md`](../AGENTS.md)
- [`../Host/AGENTS.md`](../Host/AGENTS.md)
- [`../Modules/Identity/AGENTS.md`](../Modules/Identity/AGENTS.md) — Patterns مستهدفة
