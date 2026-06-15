# 🔔 src/backend/Modules/Notifications/AGENTS.md

> Notifications Module — ✅ Phase 2.3 (in-app notifications)

## شو فيه

```
Notifications/
├── Entities/Notification.cs
├── Infrastructure/NotificationRepository.cs
└── Application/Services/NotificationService.cs
```

## Domain Model

`Notification` (in-DB):
- `Type` (string): "LowStock" حالياً، مستقبلياً "JournalPosted", "HighVariance"...
- `Title`, `Message`
- `ReferenceType` + `ReferenceId` (optional): Item, Project, JournalEntry...
- `IsRead`, `ReadAt`
- `UserId` (target user — حالياً نستخدم creator، مستقبلياً tenant-wide admin)

## Endpoints (3)

| Method | Path | الـ Function |
|--------|------|-------------|
| GET | /api/inventory/notifications | user notifications (paginated) |
| GET | /api/inventory/notifications/unread | unread + count |
| POST | /{id}/mark-read | mark as read |

## لما تشتغل هنا

- إضافة Type جديد: عدّل `NotificationService.CreateAsync` calls
- إضافة channel (email, push): أنشئ `IEmailSender` وادعوه من `NotificationService` (PR #8+)
- تحسين targeting: tenant-wide admin list بدلاً من creator-only

## مرتبطة بـ

- [`../../AGENTS.md`](../../AGENTS.md)
- [`../Inventory/AGENTS.md`](../Inventory/AGENTS.md) — يستدعي LowStock
- [`../Finance/AGENTS.md`](../Finance/AGENTS.md) — JournalPosted alerts (PR #7)
