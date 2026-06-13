# 🏢 ERP-SYSTEM

نظام ERP متكامل (MVP) يتكون من 3 وحدات أساسية: **المالية (Finance)** + **المشاريع (Projects)** + **المخزون (Inventory)**.

## 🎯 الهدف

إطلاق نظام ERP قابل للاستخدام على VPS، مبني على Modular Monolith + Event-Driven Architecture، قابل للتحويل لاحقاً إلى Microservices.

## 🛠️ التقنيات

- **Backend:** ASP.NET Core 8 (C#) + Vertical Slices
- **Database:** PostgreSQL 16 + Dapper + FluentMigrator + MartenDB
- **Frontend:** Next.js 14 + TypeScript + shadcn/ui
- **Cache/Queue:** Redis
- **Event Bus:** MartenDB Inline + (مستقبلاً: Kafka/RabbitMQ)
- **Containerization:** Docker + Docker Compose
- **CI/CD:** GitHub Actions

## 🏗️ البنية المعمارية

```
┌─────────────────────────────────────────────────┐
│              Next.js Frontend                   │
└─────────────────────┬───────────────────────────┘
                      │ REST / GraphQL
┌─────────────────────▼───────────────────────────┐
│         ASP.NET Core Modular Monolith           │
│  ┌──────────┐ ┌──────────┐ ┌──────────────┐    │
│  │ Identity │ │ Finance  │ │   Projects   │    │
│  └──────────┘ └──────────┘ └──────────────┘    │
│  ┌──────────────────────────────────────────┐   │
│  │           Inventory Module               │   │
│  └──────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────┐   │
│  │      Shared (Event Bus, Logging)         │   │
│  └──────────────────────────────────────────┘   │
└─────────────────────┬───────────────────────────┘
                      │
        ┌─────────────┼─────────────┐
        ▼             ▼             ▼
   ┌────────┐   ┌──────────┐   ┌─────────┐
   │ Postgres│  │  MartenDB │   │  Redis  │
   │  (OLTP) │  │  (Events) │   │ (Cache) │
   └────────┘   └──────────┘   └─────────┘
```

## 📁 هيكل المشروع

```
ERP-SYSTEM/
├── docs/                    # التوثيق + خطة المشروع
│   └── PLAN.md             # الخطة الكاملة
├── src/
│   ├── backend/            # ASP.NET Core API
│   │   ├── Modules/
│   │   │   ├── Identity/   # المستخدمين والصلاحيات
│   │   │   ├── Finance/    # المحاسبة
│   │   │   ├── Projects/   # المشاريع
│   │   │   └── Inventory/  # المخزون
│   │   ├── Shared/         # كود مشترك
│   │   └── Host/           # نقطة الدخول
│   └── frontend/           # Next.js UI
├── infra/                   # Docker, CI/CD
│   ├── docker/
│   └── .github/
└── README.md
```

## 🚀 البدء السريع

```bash
# استنساخ المشروع
git clone https://github.com/anas600/ERP-SYSTEM.git
cd ERP-SYSTEM

# تشغيل قاعدة البيانات والـ Cache
docker compose -f infra/docker/docker-compose.dev.yml up -d

# تشغيل الـ Backend
cd src/backend
dotnet run

# تشغيل الـ Frontend
cd src/frontend
npm install
npm run dev
```

## 📊 الـ Modules

| Module | الوصف | الحالة |
|--------|-------|--------|
| Identity | إدارة المستخدمين، المستأجرين، الصلاحيات (RBAC) | 🚧 التطوير |
| Finance | الحسابات، القيود، الفواتير، المدفوعات، محرك القواعد | 📋 مخطط |
| Projects | المشاريع، المهام، الميزانيات، الموارد | 📋 مخطط |
| Inventory | المنتجات، المخازن، الحركات، التقييم المالي | 📋 مخطط |

## 🔗 التكاملات بين الـ Modules

- 📦 استلام بضاعة → `StockReceived` event → Finance يُنشئ Journal Entry
- 🏗️ مشروع يطلب مواد → `ProjectMaterialRequested` → Inventory يحجز الكمية
- 👷 تخصيص موظف لمشروع → `ResourceAssigned` → Finance يحتسب التكلفة

## 📅 الجدول الزمني

| المرحلة | المدة | المحتوى |
|---------|-------|---------|
| Phase 0 | أسبوع 1 | Foundation + Identity |
| Phase 1 | أسابيع 2-4 | Finance Core |
| Phase 2 | أسابيع 5-7 | Projects + Inventory |
| Phase 3 | أسابيع 8-10 | Polish + Deploy |

## 📜 الترخيص

Private - جميع الحقوق محفوظة © 2026

## 📞 التواصل

- GitHub: [@anas600](https://github.com/anas600)
- المشروع: [anas600/ERP-SYSTEM](https://github.com/anas600/ERP-SYSTEM)
