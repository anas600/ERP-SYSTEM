# Workflow: Payroll Run (دورة الرواتب)

> **Audience:** Client stakeholders + future contributors.
> **Sprint:** 20 (Demo 2 — P1 docs).
> **Backend module:** `Modules/HR/Payroll`.

---

## 1. Business Purpose (الغرض التجاري)

The **Payroll Run** function computes and posts the monthly salary for all active employees. Each run covers a period (e.g. August 2026), produces a payslip per employee (gross, deductions, net), and posts a single journal entry for the entire batch. The payroll run is the anchor for:

- **Payslips** (كشوف الرواتب) — per-employee breakdown.
- **Salary Expense** (مصروف الرواتب) — Dr Salaries / Cr Salaries Payable + tax/insurance.
- **EOS** — feeds into end-of-service calculation.
- **Payroll Reports** — total gross, total net, by department, by month.

Without a payroll run, employees don't get paid (in the system).

---

## 2. User Roles (الأدوار)

| Role | Can list? | Can create? | Can process? | Can post? | Can cancel? |
|---|---|---|---|---|---|
| **Admin** (مدير النظام) | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Accountant** (محاسب) | ✅ | ✅ | ✅ | ✅ | ✅ |
| **HR Manager** (مدير موارد بشرية) | ✅ | ✅ | ✅ | ❌ | ❌ |
| **Viewer** (مشاهد) | ✅ | ❌ | ❌ | ❌ | ❌ |

**Why roles matter:** Posting a payroll run creates a journal entry (Dr Salaries / Cr Salaries Payable + liabilities). Only Admin and Accountant can post.

---

## 3. User Journey (رحلة المستخدم)

### 3.1 Browse the list
1. Open **Payroll** (`/hr/payroll`) from the sidebar.
2. The page loads all payroll runs in a sortable table.
3. Each row shows: period, total gross, total net, status (Draft / Processing / Posted / Cancelled), items count.

### 3.2 Create a new payroll run
1. Click **دورة رواتب جديدة** (top-right button).
2. Fill the form:
   - **Period Start** (required) — first day of the period.
   - **Period End** (required) — last day of the period.
   - **Notes** (optional).
3. Click **حفظ كمسودة** — run is in `Draft` status.

### 3.3 Process a payroll run
1. Open a Draft run's view page.
2. Click **معالجة** (Process).
3. The system:
   - Computes a payslip for each **active** employee.
   - Each payslip = base salary × (period days / total month days) + earnings - deductions.
   - Earnings: base salary + any overtime or bonuses (not in MVP).
   - Deductions: tax (configurable) + social insurance employee share.
   - Updates the run status to `Processing`.
   - Status becomes `Processed` when all payslips are computed.
4. Review the per-employee payslips on the run's view page.

### 3.4 Post a payroll run
1. Open a Processed run's view page.
2. Click **ترحيل** (Post).
3. The system:
   - Creates a journal entry (Dr 5210 Salaries Expense / Cr 2210 Salaries Payable + Cr 2240 Tax Payable + Cr 2250 Social Insurance Payable).
   - Updates the run status to `Posted`.
   - Sets the `postedAt` and `journalEntryId` fields.
4. Each employee's payslip becomes available for download / printing.

### 3.5 View a payslip
1. Open a posted run's view page.
2. Click on an employee in the list to see the full payslip (gross, deductions, net, components, payment days).

---

## 4. API Contract (واجهة البرمجة)

Base path: `/api/hr/payroll/runs`

| Method | Path | Purpose | Returns |
|---|---|---|---|
| `GET` | `/api/hr/payroll/runs` | List all runs | `PayrollRun[]` |
| `GET` | `/api/hr/payroll/runs/{id}` | Get one run | `PayrollRun` |
| `POST` | `/api/hr/payroll/runs` | Create a run (Draft) | `PayrollRun` (201) |
| `POST` | `/api/hr/payroll/runs/{id}/process` | Process a Draft run | `PayrollRun` |
| `POST` | `/api/hr/payroll/runs/{id}/post` | Post a Processed run | `PayrollRun` |
| `GET` | `/api/hr/payroll/runs/{id}/items` | List payslips | `PayrollItem[]` |
| `GET` | `/api/hr/payroll/runs/{runId}/items/{empId}/payslip` | Get one payslip | `Payslip` |
| `GET` | `/api/hr/payroll/eos/{empId}` | Compute EOS | `EosResponse` |

### Request body — `CreatePayrollRunRequest`
```json
{
  "periodStart": "2026-08-01T00:00:00Z",
  "periodEnd": "2026-08-31T00:00:00Z",
  "notes": "رواتب شهر أغسطس 2026"
}
```

### Response — `PayrollRun`
```json
{
  "id": "guid",
  "periodStart": "2026-08-01T00:00:00Z",
  "periodEnd": "2026-08-31T00:00:00Z",
  "status": 3,
  "totalGross": 25000.00,
  "totalNet": 22000.00,
  "processedAt": "2026-08-25T10:00:00Z",
  "postedAt": "2026-08-25T14:00:00Z",
  "notes": "رواتب شهر أغسطس 2026",
  "createdAt": "2026-08-25T09:00:00Z",
  "itemsCount": 10
}
```

### Status enum
| Value | Arabic | English |
|---|---|---|
| 1 | مسودة | Draft |
| 2 | قيد المعالجة | Processing |
| 3 | مُرحَّل | Posted |
| 4 | ملغي | Cancelled |

### Payslip — `PayrollItem`
```json
{
  "id": "guid",
  "payrollRunId": "guid",
  "employeeId": "guid",
  "employeeNumber": "EMP-2026-0001",
  "employeeName": "أحمد محمد الفيتوري",
  "baseSalary": 1500.00,
  "grossSalary": 1500.00,
  "taxAmount": 75.00,
  "socialInsuranceEmployee": 45.00,
  "netSalary": 1380.00,
  "status": 3,
  "paymentDays": 31,
  "notes": null,
  "components": [
    {
      "id": "guid",
      "componentType": 1,
      "name": "الراتب الأساسي",
      "amount": 1500.00,
      "sortOrder": 1
    }
  ]
}
```

### EOS — `EosResponse`
```json
{
  "employeeId": "guid",
  "employeeNumber": "EMP-2026-0001",
  "employeeName": "أحمد محمد الفيتوري",
  "hireDate": "2020-01-01T00:00:00Z",
  "terminationDate": "2026-08-31T00:00:00Z",
  "yearsOfService": 6.66,
  "monthlySalary": 1500.00,
  "eosAmount": 14985.00,
  "formula": "6 years * 1 month salary + 8 months * (1/12 * monthly salary) = 6 * 1500 + 8 * 125 = 9000 + 1000 = 10000"
}
```

---

## 5. UI Pages (الصفحات)

| Path | File | Purpose |
|---|---|---|
| `/hr/payroll` | `app/(authenticated)/hr/payroll/page.tsx` | List runs |
| `/hr/payroll/new` | `app/(authenticated)/hr/payroll/new/page.tsx` | Create form |
| `/hr/payroll/{id}` | `app/(authenticated)/hr/payroll/[id]/page.tsx` | View run + items |

---

## 6. State Transitions (تحولات الحالة)

```
                  save
    ┌─────────┐ ────────▶ ┌─────────┐
    │  (new)  │            │  Draft  │
    └─────────┘            └─────────┘
                                │
                                │ process
                                ▼
                           ┌────────────┐
                           │ Processing │ ──── complete ────▶ ┌─────────┐
                           └────────────┘                    │Processed│
                                                             └─────────┘
                                                                  │
                                                                  │ post
                                                                  ▼
                                                             ┌─────────┐
                                                             │ Posted  │ ── pay run ─▶ (stays Posted)
                                                             └─────────┘
                                                                  │
                                                                  │ cancel (Draft/Processing only)
                                                                  ▼
                                                             ┌─────────┐
                                                             │Cancelled│
                                                             └─────────┘
```

**Important rules:**
- Only `Draft` and `Processing` can be cancelled.
- Once `Posted`, the run is locked. To fix, post an offsetting adjustment run.

---

## 7. Edge Cases (الحالات الاستثنائية)

| Case | Handling |
|---|---|
| **No active employees** | The run processes with 0 items. |
| **Employee hired mid-period** | Prorated salary (base × days_in_period / days_in_month). |
| **Employee terminated mid-period** | Prorated salary up to termination date. |
| **Tax rate is 0** | Allowed (e.g. for tax-exempt categories). |
| **Period crosses month boundary** | The system warns. Best practice: one run per month. |
| **Post without processing** | The form rejects. The run must be `Processed` first. |
| **Cancel after Posted** | The form rejects. Must post an offsetting adjustment. |
| **Negative net salary** | The form warns (over-deductions). |
| **EOS for < 1 year service** | The system computes a partial EOS per Libyan labor law. |
| **Cross-company** | Runs are scoped to the active company. |

---

## 8. Bilingual Labels (التسميات ثنائية اللغة)

| Arabic | English | Where used |
|---|---|---|
| Payroll / الرواتب | Payroll | Sidebar, page title |
| دورة رواتب جديدة | New Payroll Run | Button |
| الفترة | Period | Form label, table column |
| إجمالي الراتب الإجمالي | Total Gross | Table column |
| إجمالي الصافي | Total Net | Table column |
| عدد الموظفين | Items Count | Table column |
| مسودة | Draft | Badge |
| قيد المعالجة | Processing | Badge |
| مُرحَّل | Posted | Badge |
| ملغي | Cancelled | Badge |
| الراتب الأساسي | Base Salary | Payslip |
| الراتب الإجمالي | Gross Salary | Payslip |
| الضريبة | Tax | Payslip |
| التأمينات الاجتماعية | Social Insurance | Payslip |
| صافي الراتب | Net Salary | Payslip |
| أيام العمل | Payment Days | Payslip |
| حفظ كمسودة | Save as Draft | Button |
| معالجة | Process | Button |
| ترحيل | Post | Button |
| إلغاء | Cancel | Button |
| تعديل | Edit | Button |
| رجوع | Back | Button |
| كشف الراتب | Payslip | View |
| حساب نهاية الخدمة | Compute EOS | View (employee page) |

---

## 9. Related Workflows (وظائف ذات صلة)

- **Employee** (`docs/workflows/employee.md`) — payslips are per active employee.
- **Attendance / Leave** — affect payment days (if integrated; out of scope for v1).
- **EOS (End of Service)** — separate calculator, also in the Payroll module.
- **Journal Entry** — posting creates a JE.
- **Chart of Accounts** — uses Salaries Payable, Tax Payable, Social Insurance Payable.

---

_Last updated: 2026-08-01 — Sprint 20 (Demo 2 — P1 docs)._
