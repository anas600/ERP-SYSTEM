---
title: ERP-SYSTEM
emoji: 🏢
colorFrom: blue
colorTo: indigo
sdk: docker
app_port: 7860
pinned: false
license: mit
short_description: Full-Stack Multi-Tenant ERP System (Finance + HR + Payroll + Projects + Inventory)
---

# ERP-SYSTEM

A complete **Multi-Tenant Modular Monolith ERP system** with:

- 💰 **Finance**: Chart of Accounts, Journal Entries, General Ledger, Posting Rules
- 📦 **Inventory**: Items, Warehouses, Stock Movements (CQRS), Low-stock alerts
- 📊 **Projects**: Project Management, Tasks, Resources, Budgets
- 👥 **HR + Payroll**: Employees, Attendance, Leaves, Payroll engine, EOS
- 💳 **Payments**: AP/AR Payments, Allocations
- 🧾 **Accounts Receivable**: Customers, Sales Invoices, Receipts, Aging reports
- 🛒 **Procurement**: Purchase Orders, Goods Receipts, Vendor Bills
- 🔄 **Event Sourcing**: Outbox pattern + Postgres LISTEN/NOTIFY
- 🔐 **JWT + Multi-tenancy**: Full isolation per tenant

## 🏗️ Architecture

```
Internet → :7860 (Caddy reverse proxy)
              ├── /api/*     → :5000 (ASP.NET Core 9 API)
              └── /*         → :3000 (Next.js 14 Frontend)
```

The container runs **3 processes** managed by `supervisord`:
- **API** (.NET 9) on port 5000
- **Frontend** (Next.js 14) on port 3000
- **Caddy** (reverse proxy) on port 7860 (public)

## 🔧 Required Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `DB_CONNECTION` | PostgreSQL connection string (full URL) | `Host=ep-xxx.neon.tech;Database=erp_system;Username=erp_user;Password=xxx;SSL Mode=Require` |
| `DB_EVENTS_CONNECTION` | PostgreSQL events store connection | `Host=ep-xxx.neon.tech;Database=erp_events;Username=erp_user;Password=xxx;SSL Mode=Require` |
| `JWT_SECRET` | JWT signing secret (min 64 chars) | `your-64-character-secret-here-replace-this` |

### 📝 How to set:

1. Go to your Space **Settings** → **Variables and secrets**
2. Add each variable above with the appropriate value
3. Restart the Space

## 🗄️ Database Setup (PostgreSQL)

The system needs a PostgreSQL 15+ database. **Hugging Face Spaces does not provide a database**, so you must use an external one. Here are the best free options:

### Option 1: **Neon.tech** (Recommended) ⭐
- Free tier: 0.5 GB storage, 190 compute hours/month
- Serverless Postgres with auto-scaling
- Built-in connection pooling
- Branch databases (great for dev/staging)
- **Steps**:
  1. Sign up at https://neon.tech
  2. Create a new project
  3. Create 2 databases: `erp_system` and `erp_events`
  4. Copy the connection string for each
  5. Set as environment variables above

### Option 2: **Supabase**
- Free tier: 500 MB storage, unlimited API requests
- Built-in dashboard + SQL editor
- Connection string with pooling available

### Option 3: **Railway PostgreSQL**
- $1/month after trial (essentially free for low-traffic)
- One-click setup

## 🚀 First-time Setup

When the Space starts, the **MigrationRunnerHostedService** will automatically:
1. Connect to your PostgreSQL
2. Apply all 14 migrations to both databases
3. Create all tables (Identity, Finance, Projects, HR, Payroll, etc.)
4. Seed default data (CoA, etc.)

You can then:
1. Open the Space URL in your browser
2. Click "Register" to create your first tenant + admin user
3. Start using the system!

## 🔐 Default Test Users

After registering, you can log in with the email/password you created. The first user becomes the **Admin** of a new tenant.

## 📊 Tech Stack

- **Backend**: C# / .NET 9, Dapper, FluentMigrator, MartenDB
- **Frontend**: Next.js 14, TypeScript, Tailwind CSS, TanStack Query
- **Database**: PostgreSQL 15+ (external)
- **Auth**: JWT + BCrypt + Refresh tokens
- **Events**: Postgres LISTEN/NOTIFY + Outbox pattern
- **CI/CD**: GitHub Actions

## 📜 License

MIT
