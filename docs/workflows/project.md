# Workflow: Project (المشاريع)

> **Audience:** Client stakeholders + future contributors.
> **Sprint:** 20 (Demo 2 — P1 docs).
> **Backend module:** `Modules/Projects`.

---

## 1. Business Purpose (الغرض التجاري)

The **Project** function tracks long-running jobs or contracts (e.g. a construction project, an IT implementation, a service contract). Each project has a budget, start/end dates, an optional customer, and a cost center. The project is the anchor for:

- **Budget vs Actual** — actual revenue/expense per project vs the budget.
- **Project P&L** — net profit per project.
- **Sales Invoice** (with `projectId`) — revenue attributed to the project.
- **Vendor Bill** (with `projectId`) — expenses attributed to the project.
- **Project Tasks** — sub-units of work (out of scope for v1).

Without a project, the system cannot answer "is this customer work profitable?".

---

## 2. User Roles (الأدوار)

| Role | Can list? | Can create? | Can edit? | Can close? |
|---|---|---|---|---|
| **Admin** (مدير النظام) | ✅ | ✅ | ✅ | ✅ |
| **Accountant** (محاسب) | ✅ | ✅ | ✅ | ✅ |
| **Project Manager** (مدير مشروع) | ✅ | ✅ | ✅ | ❌ |
| **Viewer** (مشاهد) | ✅ | ❌ | ❌ | ❌ |

**Why roles matter:** Project budgets are financial commitments. Only Admin/Accountant can close (final status change) — but Project Managers can create and update day-to-day.

---

## 3. User Journey (رحلة المستخدم)

### 3.1 Browse the list
1. Open **المشاريع** (`/projects`) from the sidebar.
2. The page loads all projects in a sortable table.
3. Each row shows: code, name, customer, status, budget, start/end dates, actual revenue/expense.

### 3.2 Create a new project
1. Click **مشروع جديد** (top-right button).
2. Fill the form:
   - **Code** (required, unique, e.g. `PROJ-2026-001`) — auto-suggested.
   - **Name** (required) — project name.
   - **Description** (optional).
   - **Customer** (optional, dropdown) — if the project is for a specific customer.
   - **Budget** (required, ≥ 0) — total project budget in LYD.
   - **Start Date** (required).
   - **End Date** (optional) — expected end date.
3. Click **حفظ**.
4. The system auto-creates a **Cost Center** for the project (for tracking actuals).

### 3.3 View a project
1. Click on a row.
2. The view page shows: full project info, **Budget vs Actual** chart, list of invoices (revenue) attributed to the project, list of bills (expense) attributed to the project.
3. From the view page you can:
   - Click **تعديل** to edit.
   - Click **إغلاق** to close the project (status → Completed).

### 3.4 Attribute a transaction to a project
- **Sales Invoice**: in the new/edit form, select a project from the **Project** dropdown. The invoice's revenue is attributed to that project.
- **Vendor Bill**: in the new/edit form, select a project. The bill's expense is attributed.

### 3.5 Close a project
1. Open the project's view page.
2. Click **إغلاق**.
3. The status changes to `Completed`. No more transactions can be attributed.
4. The Budget vs Actual is final.

---

## 4. API Contract (واجهة البرمجة)

Base path: `/api/projects`

| Method | Path | Purpose | Returns |
|---|---|---|---|
| `GET` | `/api/projects` | List all projects | `ProjectResponse[]` |

### Request body — `CreateProjectRequest`
```json
{
  "companyId": "guid",
  "code": "PROJ-2026-001",
  "name": "تطوير نظام ERP لشركة الفجر",
  "description": "مشروع تطوير وتنفيذ نظام ERP متكامل",
  "customerId": "guid",
  "budget": 150000.00,
  "startDate": "2026-08-01T00:00:00Z",
  "endDate": "2027-01-31T00:00:00Z"
}
```

### Response — `ProjectResponse`
```json
{
  "id": "guid",
  "companyId": "guid",
  "costCenterId": "guid",
  "code": "PROJ-2026-001",
  "name": "تطوير نظام ERP لشركة الفجر",
  "description": "مشروع تطوير وتنفيذ نظام ERP متكامل",
  "customerId": "guid",
  "customerName": "شركة الفجر للتجارة",
  "status": 2,
  "budget": 150000.00,
  "startDate": "2026-08-01T00:00:00Z",
  "endDate": "2027-01-31T00:00:00Z",
  "isActive": true,
  "createdAt": "2026-08-01T09:00:00Z",
  "updatedAt": "2026-08-01T09:00:00Z"
}
```

### Status enum
| Value | Arabic | English |
|---|---|---|
| 1 | تخطيط | Planning |
| 2 | نشط | Active |
| 3 | معلق | OnHold |
| 4 | مكتمل | Completed |
| 5 | ملغي | Cancelled |

---

## 5. UI Pages (الصفحات)

| Path | File | Purpose |
|---|---|---|
| `/projects` | `app/(authenticated)/projects/page.tsx` | List |
| `/projects/new` | `app/(authenticated)/projects/new/page.tsx` | Create form |
| `/projects/{id}` | `app/(authenticated)/projects/[id]/page.tsx` | View + Budget vs Actual |

---

## 6. State Transitions (تحولات الحالة)

```
                  save
    ┌─────────┐ ────────▶ ┌─────────┐
    │  (new)  │            │Planning │
    └─────────┘            └─────────┘
                                │
                                │ activate
                                ▼
                           ┌─────────┐
                           │ Active  │ ◀─── resume ──── ┌─────────┐
                           └─────────┘                 │ OnHold  │
                                │                      └─────────┘
                                │ complete
                                ▼
                           ┌─────────┐
                           │Completed│
                           └─────────┘

    Any of {Planning, Active, OnHold} ──── cancel ────▶ ┌─────────┐
                                                       │Cancelled│
                                                       └─────────┘
```

**Important rules:**
- `Planning` → `Active` happens when the project starts.
- `Active` ↔ `OnHold` is reversible.
- `Completed` is terminal.
- `Cancelled` is terminal.

---

## 7. Edge Cases (الحالات الاستثنائية)

| Case | Handling |
|---|---|
| **Budget is 0** | Allowed (e.g. for internal projects with no revenue target). |
| **End date before start date** | The form rejects. |
| **Customer is inactive** | The customer dropdown only shows active customers. |
| **Project with attributed transactions** | The project can be edited, but the cost center / code cannot be changed. |
| **Close a project with open transactions** | The system warns but allows (you may have a final invoice pending). |
| **Reopen a completed project** | The system allows reactivation (status → Active). |
| **Code is locked** | Once created, `code` cannot be changed. |
| **Cross-company** | Projects are scoped to the active company. |
| **Cost Center auto-creation** | Each project gets its own cost center for tracking actuals. The cost center is auto-managed; you don't edit it directly. |

---

## 8. Bilingual Labels (التسميات ثنائية اللغة)

| Arabic | English | Where used |
|---|---|---|
| المشاريع | Projects | Sidebar, page title |
| مشروع جديد | New Project | Button |
| كود المشروع | Project Code | Form label, table column |
| اسم المشروع | Project Name | Form label, table column |
| العميل | Customer | Form label, table column |
| الميزانية | Budget | Form label, table column |
| تاريخ البدء | Start Date | Form label |
| تاريخ الانتهاء | End Date | Form label |
| الحالة | Status | Table column |
| تخطيط | Planning | Badge |
| نشط | Active | Badge |
| معلق | On Hold | Badge |
| مكتمل | Completed | Badge |
| ملغي | Cancelled | Badge |
| الميزانية مقابل الفعلي | Budget vs Actual | Report |
| صافي الربح | Net Profit | Report |
| الإيرادات الفعلية | Actual Revenue | Report |
| المصروفات الفعلية | Actual Expense | Report |
| حفظ | Save | Button |
| تعديل | Edit | Button |
| إغلاق | Close | Button |
| إلغاء | Cancel | Button |
| رجوع | Back | Button |

---

## 9. Related Workflows (وظائف ذات صلة)

- **Customer** (`docs/workflows/customer.md`) — optional reference for project.
- **Sales Invoice** — can attribute revenue to a project via `projectId`.
- **Vendor Bill** — can attribute expense to a project via `projectId`.
- **Cost Center** — auto-created per project, used for reporting.
- **Budget vs Actual Report** — main report fed by project-attributed transactions.

---

_Last updated: 2026-08-01 — Sprint 20 (Demo 2 — P1 docs)._
