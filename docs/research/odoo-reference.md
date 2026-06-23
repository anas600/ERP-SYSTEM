# Odoo كمرجع Industry-Standard — ملف موجز

> **الغرض:** مرجع مكثّف عن **Odoo** كنظام ERP مفتوح المصدر تجاري رائد، لاستخلاص الأنماط المعمارية و UX/UI التي يمكن تبنّيها في **ERP-SYSTEM**. هذا ملف موجز — التفاصيل الكاملة في `daftra-features.md` و `erpnext-features.md`.
> **الإصدار المرجعي:** Odoo 19.0 (الأحدث وقت البحث، 2025).

---

## 1. لماذا Odoo كمرجع؟

**Odoo** هو أكبر **open-source ERP تجاري** في العالم — **+12 مليون مستخدم**، **+44,000 تطبيق** على Odoo App Store، نموذج **Open Core** (Community Edition مجاني + Enterprise Edition مدفوع). يُعدّ **مرجع الصناعة (industry-standard)** لأنه يجمع بين مرونة المصدر المفتوح، نضج الهندسة المعمارية، وتغطية كاملة لدورة أعمال SMB و Enterprise.

---

## 2. أهم 10 أنماط معمارية تبنّاها Odoo

### 2.1 Modular Apps
كل وظيفة في Odoo هي **module** مستقل داخل مجلد `addons/` (`__manifest__.py` + Python + XML + static assets). يمكن تركيب/إزالة أي module دون لمس الباقي.

### 2.2 Inheriting & Extending (لا تُعدّل الـ core أبدًا)
آليتان رسميتان:
- **`_inherit`** على Python class لإضافة/تعديل methods، constraints على model موجود
- **View inheritance** عبر `<record model="ir.ui.view"><field name="inherit_id"/>...` بـ XPath

> "Odoo's inheritance features provide an effective extensibility mechanism. They allow you to extend existing third-party apps without changing them directly." — Odoo 19 Docs, Ch.12

### 2.3 Cron Jobs & Scheduled Actions (`ir.cron`)
مهام دورية تُعرَّف في `ir.cron` — اسم، interval (`numbercall`, `interval_number`, `interval_type`)، كود Python، retry logic، وتسجيل executions. أمثلة: تذكيرات الفواتير، إعادة حساب المخزون، مزامنة البنك.

### 2.4 Wizards (معالجات للعمليات المعقدة)
**`TransientModel`** = model مؤقت يُحذف تلقائيًا بعد العملية. يُستخدم للعمليات متعددة الخطوات (تأكيد طلب، استيراد CSV، إقفال سنة مالية).

### 2.5 Server Actions (`ir.actions.server`)
إجراءات قابلة لإعادة الاستخدام تُحفَّز من زر في الـ Form View، أو من Cron. أنواعها: Execute Python Code، Create Record، Write Record، Multi، Send Email، Webhook.

### 2.6 Email Templates (`mail.template`)
قوالب بريد بـ QWeb + placeholders تُرسَل تلقائيًا عند أحداث. تدعم لغات متعددة و layouts قابلة للتخصيص.

### 2.7 QWeb Templates
محرك templating XML — يُستخدم لـ: **PDF Reports** (فواتير، عروض) عبر `wkhtmltopdf`، **Web pages**، **Kanban cards**، **Email body**.

### 2.8 Chatter (نشاط/محادثات على الـ record)
كل model يرث من **`mail.thread`** يحصل تلقائيًا على: شريط رسائل، Followers، Activities المجدولة، Tracking للحقول المهمة. يظهر في أسفل Form View.

### 2.9 Multi-Company بـ Allowed Companies
المستخدم يفعّل عدة شركات في نفس الجلسة (`allowed_companies`) — يرى ويعدّل سجلات كل الشركات. يُطبَّق عبر `company_id` + `check_company=True` + `company_dependent=True` على الحقول المختلفة.

### 2.10 Record Rules (`ir.rule`) للـ Multi-Tenancy و Security
قواعد على مستوى الـ record (Domain filters) مرتبطة بـ Groups — تتحكم في **من يرى/يعدّل أي صف**. تُستخدم لـ Row-Level Security + Multi-Company isolation.

---

## 3. أهم 3 أنماط UX/UI

### 3.1 View Types متعددة لنفس الـ Model
نفس الـ model يمكن تقديمه بأكثر من view type:
- **`Form`** — صفحة تفاصيل السجل (مع tabs، status bar، buttons في الـ header)
- **`List`** (كان Tree) — جدول قابل للترتيب والتحرير inline
- **`Kanban`** — بطاقات قابلة للسحب والإفلات بين الأعمدة
- **`Calendar`** / **`Graph`** / **`Pivot`** / **`Gantt`** / **`Activity`**

> المستخدم **يختار** الواجهة الأنسب لمهمته — دون قفز بين شاشات.

### 3.2 Search View: Filters / Group By / Search
شريط بحث موحد في كل List/Kanban view يحوي: **Search box** + **Filters** مُعرَّفة مسبقًا (My Drafts, Late Invoices) + **Group By** + **Comparison** بين فترتين + **Favorites** لحفظ تركيبات البحث.

### 3.3 Dashboard بـ KPI Tiles + Cohort Charts
Dashboards (Accounting, Inventory, Purchase) تجمع: **KPI Tiles** بأرقام كبيرة + نسبة التغيير، **Charts** تفاعلية (line, bar, pie)، **Quick Action Buttons**، **Drill-down** من الـ tile إلى List View مع filter مُطبَّق.

---

## 4. دروس مستفادة لـ ERP-SYSTEM

| # | النمط | التطبيق |
|---|------|---------|
| 1 | **Modular Apps** | كل module في مجلد منفصل، مع manifest يوضح الـ dependencies |
| 2 | **Inheritance** | قاعدة "لا تعدّل كود core من module آخر" — extension hooks واضحة |
| 3 | **Cron / Scheduled Jobs** | HostedService بسيط للـ handlers الدورية (تقارير، تسويات، notifications) |
| 4 | **Wizards** | للعمليات متعددة الخطوات (Import، Period Close، Setup) |
| 5 | **Server Actions** | مكتبة إجراءات قابلة لإعادة الاستخدام، تُربط بأزرار |
| 6 | **Email Templates** | قوالب موحدة بالعربية + الإنجليزية + placeholders |
| 7 | **QWeb للطباعة** | محرر قوالب تقارير/PDF مع variables و conditionals |
| 8 | **Chatter** | كل entity جوهري يحصل على Activity Log + Comments + Audit Trail |
| 9 | **Multi-Company (future)** | `company_id` على entities المالية + Rules للعزل |
| 10 | **Record Rules** | استكمال TenantContext عبر Rule-based filter بدل hardcoded WHERE |

**UX Quick Wins:** Multi-View (Table/Cards/Timeline) + Search موحد + Dashboard بكروت KPI + Drill-down.

---

## 5. روابط مرجعية رسمية (3 روابط)

1. [Architecture Overview (Server Framework 101) — Odoo 19.0](https://www.odoo.com/documentation/19.0/developer/tutorials/server_framework_101/01_architecture.html)
2. [Actions Reference (Server Actions, Scheduled Actions, Wizards) — Odoo 19.0](https://www.odoo.com/documentation/19.0/developer/reference/backend/actions.html)
3. [Multi-company Guidelines — Odoo Developer Docs](https://www.odoo.com/documentation/19.0/developer/howtos/company.html)

---

## 6. أولوية التطبيق

| المرحلة | الأنماط الواجب تبنيها |
|--------|----------------------|
| **Phase 2.5+** (الحالي) | Modular Apps + Search View — البنية modular + استكمال شريط البحث الموحد |
| **Phase 3** | Inheritance + Server Actions + Email Templates — تمهيد لـ Open API و Studio-like UI |
| **Phase 4** | Chatter + Wizard + Cron — Collaboration + أتمتة التقارير الدورية |
| **Phase 5+** | QWeb + Multi-Company + Record Rules — ميزات Enterprise-level |

---

> **ملخص تنفيذي:** Odoo الأقوى في **Modular Apps + Inheritance + Record Rules + Multi-View**. هذه الأربعة نقتبسها في ERP-SYSTEM. الباقي (QWeb، Studio، Open API) مؤجل لـ Phase 4+.
