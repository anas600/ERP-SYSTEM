# Workflow: Employee (الموظفين)

> **Audience:** Client stakeholders + future contributors.
> **Sprint:** 20 (Demo 2 — P1 docs).
> **Backend module:** `Modules/HR`.

---

## 1. Business Purpose (الغرض التجاري)

The **Employee** function manages every person employed by the company. Each employee record carries personal info, contact details, national ID, department, job title, hire date, base salary, and termination date. The employee is the anchor for:

- **Attendance** (الحضور) — check-in / check-out records.
- **Leave Requests** (الإجازات) — annual, sick, emergency.
- **Payroll Runs** — monthly salary computation per employee.
- **EOS (End of Service)** — end-of-service benefits calculation.
- **HR Reports** — headcount, payroll cost, leave balance.

Without an employee record, the system cannot pay anyone or track time.

---

## 2. User Roles (الأدوار)

| Role | Can list? | Can create? | Can edit? | Can deactivate? |
|---|---|---|---|---|
| **Admin** (مدير النظام) | ✅ | ✅ | ✅ | ✅ |
| **Accountant** (محاسب) | ✅ | ✅ | ✅ | ✅ |
| **HR Manager** (مدير موارد بشرية) | ✅ | ✅ | ✅ | ✅ |
| **Viewer** (مشاهد) | ✅ | ❌ | ❌ | ❌ |

**Why roles matter:** Employee records contain personal data (national ID, salary). Only Admin, Accountant, and HR Manager can modify. Deactivation is a soft delete (preserves history).

---

## 3. User Journey (رحلة المستخدم)

### 3.1 Browse the list
1. Open **الموظفين** (`/hr/employees`) from the sidebar.
2. The page loads all employees in a sortable table.
3. Each row shows: employee number, full name, department, job title, base salary, hire date, status.

### 3.2 Create a new employee
1. Click **موظف جديد** (top-right button).
2. Fill the form:
   - **Full Name** (required) — Arabic full name.
   - **Email** (optional) — company email.
   - **Phone** (optional).
   - **National ID** (optional) — Libyan national ID number.
   - **Department** (optional, dropdown) — from the departments list.
   - **Job Title** (optional) — e.g. "محاسب أول".
   - **Hire Date** (required, default today).
   - **Base Salary** (required, ≥ 0) — monthly salary in LYD.
3. Click **حفظ**.
4. The system auto-generates the employee number (e.g. `EMP-2026-0001`).

### 3.3 View an employee
1. Click on a row.
2. The view page shows: full info, recent attendance records, recent leave requests, recent payroll items.
3. From the view page you can:
   - Click **تعديل** to edit.
   - Click **حضور** to record attendance.
   - Click **إجازة** to create a leave request.
   - Click **كشف راتب** to view payslips.
   - Click **حساب نهاية الخدمة** to compute EOS.

### 3.4 Terminate an employee
1. Open the employee's view page, then click **تعديل**.
2. Set the **Termination Date**.
3. The employee is marked inactive after this date.
4. The EOS is auto-computed (use the EOS calculator for the precise amount).

---

## 4. API Contract (واجهة البرمجة)

Base path: `/api/hr/employees`

| Method | Path | Purpose | Returns |
|---|---|---|---|
| `GET` | `/api/hr/employees` | List all employees | `EmployeeResponse[]` |
| `GET` | `/api/hr/employees/{id}` | Get one employee | `EmployeeResponse` |
| `POST` | `/api/hr/employees` | Create an employee | `EmployeeResponse` (201) |
| `PUT` | `/api/hr/employees/{id}` | Update an employee | `EmployeeResponse` |

### Request body — `CreateEmployeeRequest`
```json
{
  "fullName": "أحمد محمد الفيتوري",
  "email": "ahmed@alfajr.ly",
  "phone": "+218 91 1234567",
  "nationalId": "1234567890",
  "departmentId": "guid",
  "jobTitle": "محاسب أول",
  "hireDate": "2026-08-01T00:00:00Z",
  "baseSalary": 1500.00
}
```

### Response — `EmployeeResponse`
```json
{
  "id": "guid",
  "employeeNumber": "EMP-2026-0001",
  "fullName": "أحمد محمد الفيتوري",
  "email": "ahmed@alfajr.ly",
  "phone": "+218 91 1234567",
  "nationalId": "1234567890",
  "departmentId": "guid",
  "departmentName": "المحاسبة",
  "jobTitle": "محاسب أول",
  "hireDate": "2026-08-01T00:00:00Z",
  "terminationDate": null,
  "baseSalary": 1500.00,
  "isActive": true,
  "createdAt": "2026-08-01T09:00:00Z"
}
```

---

## 5. UI Pages (الصفحات)

| Path | File | Purpose |
|---|---|---|
| `/hr/employees` | `app/(authenticated)/hr/employees/page.tsx` | List |
| `/hr/employees/new` | `app/(authenticated)/hr/employees/new/page.tsx` | Create form |
| `/hr/employees/{id}` | `app/(authenticated)/hr/employees/[id]/page.tsx` | View + recent activity |

---

## 6. State Transitions (تحولات الحالة)

An employee has only one state: **Active** or **Terminated**.

```
┌─────────┐  terminationDate=null  ┌─────────┐
│ Terminat│ ──────────────────────▶│ Active  │
└─────────┘                         └─────────┘
     ▲                                  │
     │       terminationDate set        │
     └──────────────────────────────────┘
```

**Effect on other documents:**
- Terminated employees **cannot be selected** in new payroll runs.
- Terminated employees **can still receive** a final payroll run (for the termination month).
- Attendance and leave requests stop accruing after the termination date.

---

## 7. Edge Cases (الحالات الاستثنائية)

| Case | Handling |
|---|---|
| **Duplicate national ID** | The system warns but allows (some employees may share IDs in edge cases). |
| **Email conflict** | If a user account exists with the same email, the system suggests linking. |
| **Base salary change** | Allowed. Future payroll runs use the new salary. Historical payslips retain the old salary. |
| **Department deletion** | Employees in that department are unlinked (departmentId set to null) but not deleted. |
| **Termination date before hire date** | The form rejects. |
| **Termination retroactive** | Allowed. The system recomputes EOS. |
| **Employee with open leave** | Termination is allowed; the leave is auto-closed. |
| **Cross-company** | Employees are scoped to the active company. |
| **GDPR / data privacy** | Deactivation is a soft delete; full deletion requires an explicit HR purge (out of scope for v1). |

---

## 8. Bilingual Labels (التسميات ثنائية اللغة)

| Arabic | English | Where used |
|---|---|---|
| الموظفين | Employees | Sidebar, page title |
| موظف جديد | New Employee | Button |
| الرقم الوظيفي | Employee Number | Form label, table column |
| الاسم الكامل | Full Name | Form label, table column |
| الرقم الوطني | National ID | Form label |
| القسم | Department | Form label, table column |
| المسمى الوظيفي | Job Title | Form label, table column |
| تاريخ التعيين | Hire Date | Form label |
| تاريخ نهاية الخدمة | Termination Date | Form label |
| الراتب الأساسي | Base Salary | Form label, table column |
| نشط / منتهية خدمته | Active / Terminated | Badge |
| حفظ | Save | Button |
| تعديل | Edit | Button |
| رجوع | Back | Button |

---

## 9. Related Workflows (وظائف ذات صلة)

- **Attendance** (`docs/workflows/attendance.md`) — daily check-in / check-out.
- **Leave Request** (`docs/workflows/leave-request.md`) — annual / sick / emergency leave.
- **Payroll Run** (`docs/workflows/payroll-run.md`) — monthly salary computation.
- **EOS (End of Service)** — benefits on termination.

---

_Last updated: 2026-08-01 — Sprint 20 (Demo 2 — P1 docs)._
