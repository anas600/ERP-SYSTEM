# بحث شامل عن ERPNext

> **التاريخ:** 2026-06-23 · **المؤلف:** Mavis Team (تولي مباشر بعد timeout) · **الغرض:** دراسة تنافسية لـ ERP-SYSTEM
> **الملخص:** ERPNext هو النظام الأكثر نضجاً في فئته مفتوحة المصدر، ويقدم **30+ موديول** متكامل مع framework قوي (Frappe) يتيح تخصيصاً عميقاً بدون تعديل الـ core. **الدرس الأهم:** نقدر نأخذ منه **Custom Doctypes + Workflow Engine + Print Formatter + Audit Trail** كأنماط معمارية.

---

## 1. نظرة عامة

| الخاصية | القيمة |
|---------|--------|
| الاسم | ERPNext (Frappe Technologies Pvt. Ltd., الهند) |
| الترخيص | **GPLv3** — مفتوح المصدر بالكامل |
| اللغة الأساسية | Python + JavaScript |
| الإصدار الحالي | v14 / v15 (إصدار 14 في 2022، وما زال نشطاً مع ترقيات 2026) |
| نموذج النشر | Self-hosted (bench CLI + Docker) أو Frappe Cloud (مدفوع) |
| عدد الموديولات | 30+ موديول أساسي + تطبيقات منفصلة (HRMS, LMS, Healthcare, Education) |
| الفئة المستهدفة | SMB ومتوسط السوق، مع توسع للشركات الكبيرة |
| المجتمع | آلاف المساهمين، توثيق ممتاز، دعم احترافي |
| التقييم | 4.5/5 في معظم المراجعات (2026) |

**أبرز ما يميزه:** **Low-code customization** — أي مستخدم يقدر يضيف Custom Fields و Doctypes من الـ UI بدون كود.

---

## 2. قائمة الموديولات الكاملة (30+)

### الموديولات الأساسية
1. **Accounting** — General Ledger, AR/AP, Multi-currency, Budgeting, Taxation, Financial Statements
2. **Selling** — Quotations, Sales Orders, Delivery Notes, Sales Invoices, Recurring Invoices
3. **Buying** — Purchase Requests, RFQ, Purchase Orders, Purchase Receipts, Purchase Invoices
4. **Stock / Inventory** — Items, Warehouses, Stock Entries, Batch/Serial, Stock Reconciliation, Packing Slips
5. **Manufacturing** — BOM (Bill of Materials), Work Orders, Production Planning, Job Cards, Subcontracting
6. **Projects** — Tasks, Timesheets, Project Budgeting, Profitability Analysis
7. **CRM** — Leads, Opportunities, Customers, Sales Pipeline, Newsletter
8. **HR & Payroll** (تطبيق منفصل: Frappe HR) — Employees, Attendance, Leave, Payroll, Appraisal, Loans
9. **Assets** — Fixed Assets, Depreciation, Asset Movement
10. **POS** — Point of Sale, POS Profile, Offline mode
11. **Quality** — Quality Inspection, Quality Goal, Quality Procedure
12. **Maintenance** — Maintenance Schedule, Maintenance Visit

### الموديولات المتقدمة / الخاصة بالصناعات
13. **Website** — Web Pages, Blogs, Contact Us
14. **eCommerce** — Shopping Cart, Products, Orders
15. **HelpDesk** — Tickets, SLA, Knowledge Base
16. **Healthcare** (تطبيق منفصل) — Patient, Practitioner, Appointment, Lab Test
17. **Education** (تطبيق منفصل) — Student, Program, Course, Fees
18. **Agriculture** (تطبيق منفصل) — Crop, Land, Task
19. **Non-Profit** — Membership, Donor, Grant
20. **Hospitality** — Restaurant, Hotel, Room

### الـ Framework Layer (مشترك)
21. **Custom Fields** — إضافة حقول لأي DocType بدون كود
22. **Custom DocType** — إنشاء entities جديدة
23. **Workflow Engine** — قواعد انتقال الحالات
24. **Print Formatter** — قوالب طباعة ديناميكية (HTML + CSS)
25. **Email Templates** — قوالب بريد مع متغيرات
26. **Scheduled Job** — Cron jobs عبر `hooks.py`
27. **Audit Trail** — Version tracking كامل على كل record
28. **Data Import** — CSV/Excel import مع validation
29. **Report Builder** — Query Reports + Script Reports
30. **Chatter** — Activity feed + comments على كل record

---

## 3. أفضل 15 ميزة تقنية فريدة في ERPNext

| # | الميزة | الوصف | الفائدة لنظامنا |
|---|--------|-------|-----------------|
| 1 | **Custom Fields بدون كود** | إضافة/تعديل/حذف حقول على أي DocType من الـ UI، يبقى بعد الـ upgrade | لو بنينا Admin UI لتعديل الـ schemas، نوفر migration overhead |
| 2 | **Custom DocTypes** | إنشاء entities جديدة بالكامل مع جداول DB تلقائياً | ممكن نضيف **Cost Center Custom Fields** مثلاً |
| 3 | **Workflow Engine** | قواعد declarative: "if state=Draft and amount>10000 then require CFO approval" | نحتاجه للـ **PO Approval** في Procurement |
| 4 | **Print Formatter** | قوالب HTML/CSS مرتبطة بـ DocType | نحتاجه لـ **طباعة PO / GR / Bill** |
| 5 | **Scheduled Jobs (hooks.py)** | Cron jobs declarative داخل الـ app | لتنظيف الـ Outbox، تجديد Tokens، Backup |
| 6 | **Realtime Updates** | WebSocket-based UI updates (Socket.IO) | الـ Dashboard KPIs ممكن تتحدث realtime |
| 7 | **Role-Based Field Permissions** | ليس فقط row-level، بل **field-level** — "HR Manager يرى BaseSalary، Sales لا" | نحتاجه في HR (حماية الرواتب) |
| 8 | **Multi-Company مع Allowed Companies** | المستخدم يقدر يبدّل بين شركات متعددة في session واحدة | نقدر نضيفه في Phase 4 (Multi-Company) |
| 9 | **Audit Trail Version Tracking** | كل تعديل على record يحفظ snapshot كامل (متى، مين، القديم، الجديد) | **ضروري للـ SOX compliance** (Finance) |
| 10 | **Data Import Tool** | CSV/Excel → DocType مع validation | مفيد لـ **Bulk Import Customers/Vendors/Items** |
| 11 | **Query Reports + Script Reports** | بناء تقارير معقدة بـ SQL (Query) أو Python (Script) | **12 reports اللي بنيناها** ممكن تستفيد |
| 12 | **Chatter (Activity Feed)** | تعليقات + @mentions + attachments على كل record | **ممتاز للـ Project tasks** (نضيفه على Projects) |
| 13 | **Document State Machine** | Draft → Submitted → Cancelled → Amended pattern | موجود عندنا بشكل ضمني، يفضل formalize |
| 14 | **Naming Series** | Auto-generated names: `PO-2026-0001` معاد تكوينها | نقدر نضيفه للـ PO numbering |
| 15 | **Server Scripts (Python)** | Custom Python logic تشتغل server-side على events | بديل خفيف للـ plugins |

---

## 4. هيكل قاعدة البيانات / الـ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Frappe Framework                          │
│  (Python backend + Node.js realtime + MariaDB/Postgres)     │
└─────────────────────────┬───────────────────────────────────┘
                          │
        ┌─────────────────┼─────────────────┐
        ▼                 ▼                 ▼
  ┌──────────┐    ┌──────────────┐    ┌──────────────┐
  │ MariaDB  │    │    Redis     │    │   Socket.IO  │
  │ أو PG 15 │    │ (cache+queue)│    │  (realtime)  │
  └──────────┘    └──────────────┘    └──────────────┘

ERPNext stacks:
- Python 3.10+
- Frappe Framework 14.x
- MariaDB 10.6+ (default) / PostgreSQL 14+ supported
- Redis 6+
- Node.js 18+ (realtime)
- Nginx + Supervisor (production)
- bench CLI = "Django management command" للـ Frappe
```

**الـ Schema pattern:** كل DocType = جدول DB + metadata + hooks + permissions.

**الـ API pattern:** REST API auto-generated من الـ DocType metadata. كل DocType يعرض GET/POST/PUT/DELETE endpoints مجاناً.

---

## 5. API capabilities

| الـ Capability | الوصف |
|----------------|-------|
| **REST API** | auto-generated لكل DocType: `GET /api/resource/DocType/{name}`, `POST /api/resource/DocType`, `PUT /api/resource/DocType/{name}` |
| **Authentication** | Token-based (Bearer) أو Session-based (cookie) |
| **Webhooks** | يقدر يطلق HTTP request عند أي event (Insert/Update/Submit) |
| **Frappe Client** | Python client للـ integration: `frappe.get_doc("DocType", "name")` |
| **Background Jobs** | RQ (Redis Queue) للـ long-running tasks |
| **Realtime** | Socket.IO subscription: `frappe.realtime.on("doc_update", callback)` |

**الدرس لنا:** لو بنينا **metadata-driven controllers** (نمط [DynamicEndpoint])، نقدر نولّد CRUD APIs تلقائياً.

---

## 6. روابط المصادر

- [ERPNext Official Site](https://erpnext.com/) — الموقع الرسمي
- [ERPNext GitHub](https://github.com/frappe/erpnext) — الكود المصدري
- [Frappe Framework](https://frappeframework.com/) — الـ framework الأساسي
- [ERPNext Documentation](https://docs.frappe.io/erpnext) — التوثيق الرسمي
- [Frappe Forum](https://discuss.frappe.io/) — مجتمع المناقشة
- [Managely: Every Module](https://managely.cloud/en/blog/what-is-erpnext) — breakdown كامل للـ 30+ موديول
- [Aurigait 2026 Guide](https://aurigait.com/blog/blog-erpnext-guide-2026/) — pricing + implementation
- [DataValue Features](https://datavalue.solutions/erpnext-features/) — analysis للـ CRM/HR
- [NocoBase comparison](https://www.cnblogs.com/nocobase/p/19642111) — مقارنة NocoBase vs Odoo vs ERPNext
- [Crunchbase Profile](https://www.crunchbase.com/organization/erpnext) — معلومات الشركة

---

## 7. ملخص تنفيذي — ماذا نستفيد لـ ERP-SYSTEM؟

### أنماط معمارية نأخذها (Top 5):
1. **Workflow Engine** — للـ PO Approval في Procurement (نطبقه Phase 3)
2. **Audit Trail Version Tracking** — مطلب SOX لـ Finance (Phase 3.5)
3. **Print Formatter** — لطباعة PO/GR/Bill (Phase 3.1)
4. **Naming Series** — لـ PO numbering التلقائي (Phase 3)
5. **Chatter / Activity Feed** — على Project tasks (تحسين للـ Projects)

### ما **لا** نأخذه (over-engineering):
- Custom DocTypes بدون كود (يتطلب Admin UI معقد، مافيش وقت)
- Realtime WebSocket (Next.js + refetch كافٍ للـ MVP)
- Multi-Company Allowed Switching (نؤجله Phase 4)

### أولويات التنفيذ:
1. **Workflow Engine** ← نحتاجه للـ PO Approval مباشرة
2. **Print Templates** ← فوري للقيمة المرئية
3. **Audit Trail** ← ضروري لـ SOX readiness
