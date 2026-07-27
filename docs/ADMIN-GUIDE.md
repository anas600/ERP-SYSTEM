# 🔐 Admin Guide — ERP-SYSTEM v1.0.34

> **Target audience:** System administrators, IT staff, dev team leads
> **Scope:** Deployment, user management, backups, security, monitoring

---

## 🏗️ Architecture Overview

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Next.js 14     │────▶│  .NET 9 API     │────▶│  PostgreSQL 18  │
│  (port 3000)    │     │  (port 5000)    │     │  (port 5432)    │
│  React 18.3     │     │  Dapper ORM     │     │  Multi-Company  │
└─────────────────┘     └─────────────────┘     └─────────────────┘
        │                        │                        │
        └──── JWT Bearer ────────┘                        │
             X-Company-Id header                          │
                                                         │
                  erp_system_demo (DB) ──────────────────┘
                  41 tables, no tenant_id
```

---

## 🚀 Deployment

### Prerequisites
- **PostgreSQL 18** (port 5432, user `erp_user` with password `Demo1234`)
- **.NET 9 SDK** (for build)
- **Node.js 20+** (for frontend)
- **OS:** Windows 10/11 (tested), Linux compatible

### Production Checklist
- [ ] Set strong `erp_user` password (not `Demo1234`)
- [ ] Generate new JWT signing key (not the dev default)
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Enable HTTPS in both backend and frontend
- [ ] Configure CORS to allow only the production frontend domain
- [ ] Set up database backups (daily, retained 30 days)
- [ ] Configure logging (Serilog → file/Seq/Elasticsearch)
- [ ] Set up monitoring (health checks, alerts)

### Backend (`appsettings.json`)
```json
{
  "ConnectionStrings": {
    "OltpConnection": "Host=localhost;Port=5432;Database=erp_system_prod;User Id=erp_user;Password=STRONG_PASSWORD;Pooling=true;MinPoolSize=2;MaxPoolSize=20"
  },
  "JwtSettings": {
    "Secret": "GENERATE_64_CHAR_RANDOM_STRING_HERE",
    "Issuer": "ERP-SYSTEM",
    "Audience": "ERP-SYSTEM-Users",
    "AccessTokenExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  }
}
```

### Frontend (`.env.production`)
```
NEXT_PUBLIC_API_URL=https://api.your-domain.com
NODE_ENV=production
```

### Run as Windows Service (NSSM)
```powershell
# Install NSSM: choco install nssm
nssm install ERPBackend "F:\app\ERPSystem.Host.exe"
nssm set ERPBackend AppDirectory "F:\app"
nssm set ERPBackend AppStdout "F:\app\logs\backend.log"
nssm set ERPBackend AppStderr "F:\app\logs\backend.err.log"
nssm start ERPBackend

nssm install ERPFrontend "C:\Program Files\nodejs\node.exe" "F:\frontend\node_modules\next\dist\bin\next start -p 3000"
nssm set ERPFrontend AppDirectory "F:\frontend"
nssm set ERPFrontend AppEnvironmentExtra "NODE_ENV=production"
nssm start ERPFrontend
```

---

## 👥 User Management

### Roles (Built-in)
| Role | Permissions |
|------|-------------|
| `Admin` | Full access (users, companies, all data) |
| `Accountant` | Finance modules (AR, AP, reports) |
| `ProjectManager` | Projects, HR (read-only) |
| `Viewer` | Read-only across all modules |

### Create User
```sql
-- The frontend `/admin/users/new` is the recommended way.
-- Direct SQL (only for emergency):
INSERT INTO users (id, email, full_name, password_hash, is_active, created_at, created_by, updated_at, updated_by)
VALUES (
  gen_random_uuid(),
  'newuser@alfajr.local',
  'اسم الموظف',
  -- BCrypt hash of 'Demo1234' (cost 12): $2a$12$...
  '$2a$12$...',
  true,
  now(),
  '<ADMIN_USER_ID>',
  now(),
  '<ADMIN_USER_ID>'
);
```

### Reset Password (Direct SQL)
```sql
-- Generate BCrypt hash first (use any C#/Python BCrypt library, cost=12)
UPDATE users
SET password_hash = '$2a$12$NEW_BCRYPT_HASH',
    updated_at = now(),
    updated_by = '<ADMIN_ID>'
WHERE email = 'user@alfajr.local';
```

### Assign Company Access
```sql
INSERT INTO user_companies (user_id, company_id, is_default, assigned_at)
VALUES ('<USER_ID>', '<COMPANY_ID>', true, now());
```

---

## 🗄️ Database Operations

### Backup
```bash
pg_dump -U erp_user -h localhost -d erp_system_prod -F c -f backup_$(date +%Y%m%d).dump
```

### Restore
```bash
pg_restore -U erp_user -h localhost -d erp_system_restored -c backup_20260727.dump
```

### Reset Database (DESTRUCTIVE)
```powershell
psql -U postgres -c "DROP DATABASE erp_system_demo;"
psql -U postgres -c "CREATE DATABASE erp_system_demo OWNER erp_user ENCODING 'UTF8';"
# Backend will auto-create schema on next startup
# Then re-run seed: npm run seed:1year
```

### Vacuum & Analyze (monthly)
```sql
VACUUM ANALYZE;
REINDEX DATABASE erp_system_demo;
```

---

## 🔒 Security

### Authentication Flow
1. User submits email + password
2. Backend verifies BCrypt hash
3. Issues JWT (60 min) + Refresh Token (7 days)
4. Frontend stores in `localStorage`
5. All API calls include `Authorization: Bearer <jwt>` + `X-Company-Id: <uuid>`

### Password Policy (current)
- Min 8 characters (frontend validation)
- BCrypt cost 12 (backend)
- No complexity requirements yet (TODO: add for production)

### Authorization (Current)
- `[Authorize]` attribute on all controllers
- `X-Company-Id` required on all multi-company endpoints
- Multi-Company: user can only see data of companies they belong to (enforced via `user_companies`)

### Known Gaps (Pre-Prod TODO)
- [ ] CSRF tokens for state-changing requests
- [ ] Rate limiting on `/api/auth/login` (e.g. 5 attempts/minute)
- [ ] Account lockout after N failed attempts
- [ ] Password rotation policy
- [ ] 2FA (backend supports `twoFactorEnabled` flag but UI not built yet)
- [ ] Audit log of all admin actions

### Security Tests (in `tests/security.spec.ts`)
- 401 on missing/invalid token
- 400/401 on SQL injection in email
- 400/401 on NoSQL injection
- Multi-company isolation (X-Company-Id required)

---

## 📊 Monitoring

### Health Check Endpoints
- `GET /api/health` — overall status
- `GET /api/health/db` — DB connectivity
- `GET /api/health/ready` — readiness probe (K8s)

### Key Metrics to Monitor
- API response time (P50, P95, P99)
- DB connection pool usage
- Error rate (4xx, 5xx)
- Login success/failure rate
- Background job health (outbox processor)

### Logs Location
- Backend: `src\backend\Host\bin\Debug\net9.0\logs\*.log` (Serilog)
- Frontend: console + browser DevTools
- Database: PostgreSQL `log/` directory

---

## 🧪 Testing

### Continuous Integration (recommended)
```yaml
# .github/workflows/test.yml
name: Tests
on: [push, pull_request]
jobs:
  test:
    runs-on: windows-latest
    services:
      postgres:
        image: postgres:18
        env:
          POSTGRES_USER: erp_user
          POSTGRES_PASSWORD: Demo1234
          POSTGRES_DB: erp_system_test
        ports: ['5432:5432']
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '9.0' }
      - uses: actions/setup-node@v4
        with: { node-version: '20' }
      - run: dotnet build src/backend/Host
      - run: npx playwright install --with-deps
      - run: npm run test:e2e
```

### Manual Smoke Test
```powershell
# 1. Start backend
Set-Location "F:\minimaxDescktop2\ERP-SYstem\src\backend\Host\bin\Debug\net9.0"
.\ERPSystem.Host.exe

# 2. Start frontend
Set-Location "F:\minimaxDescktop2\ERP-SYstem\src\frontend"
npm run dev

# 3. Run E2E
Set-Location "F:\minimaxDescktop2\ERP-SYstem"
npm run test:e2e:smoke    # 39 endpoints, ~50s
```

---

## 🆘 Troubleshooting

### Backend won't start
- Check `logs/backend.err.log` (or console)
- Verify PostgreSQL is running: `pg_isready -h localhost -p 5432`
- Verify connection string in `appsettings.json`

### Login fails
- Check user exists: `SELECT email, is_active FROM users WHERE email='admin@alfajr.local';`
- Reset password (see above)
- Check JWT secret hasn't changed (would invalidate all tokens)

### Reports return empty
- Check `X-Company-Id` header is sent (Network tab)
- Verify company has data: `SELECT count(*) FROM sales_invoices WHERE company_id='<UUID>';`
- Check date range: `?from=2026-01-01&to=2026-12-31`

### Database locked / connection pool exhausted
- Increase `MaxPoolSize` in connection string
- Check for uncommitted transactions
- Restart backend (will release connections)

### Performance slow
- Run `VACUUM ANALYZE` on DB
- Check missing indexes: `EXPLAIN ANALYZE <slow query>;`
- Review Serilog logs for slow API endpoints

---

## 📞 Escalation Path
- **Level 1:** Check this guide + runbook
- **Level 2:** `docs/CHANGELOG.md` for recent changes
- **Level 3:** GitHub Issues
- **Level 4:** Project owner (Anas) — see `CONSTITUTION.md`
