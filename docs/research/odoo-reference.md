# Odoo كمرجع Industry-Standard — ملف موجز

> **الغرض:** مرجع مكثّف عن **Odoo** كنظام ERP مفتوح المصدر تجاري رائد، لاستخلاص الأنماط المعمارية و UX/UI التي يمكن تبنّيها في مشروع **ERP-SYSTEM**. هذا الملف موجز — التفاصيل الكاملة موجودة في `daftra-features.md` و `erpnext-features.md`.

- **تاريخ الإعداد:** 23 يونيو 2026
- **المُعدّ:** General Worker (وكيل بحث منتجات)
- **المصادر:** التوثيق الرسمي لـ Odoo 19 (Developer + User Docs) عبر `web_search` و `webfetch`.
- **الإصدار المرجعي:** Odoo 19.0 (الأحدث وقت البحث).

---

## 1. لماذا Odoo كمرجع؟

**Odoo** هو أكبر **open-source ERP تجاري** في العالم — **+12 مليون مستخدم**، **+44,000 تطبيق** على Odoo App Store، نموذج **Open Core** (Community Edition مجاني + Enterprise Edition مدفوع). يُعدّ **مرجع الصناعة (industry-standard)** لأنه يجمع بين مرونة المصدر المفتوح، نضج الهندسة المعمارية، وتغطية كاملة لدورة أعمال SMB و Enterprise في منصة واحدة قابلة للتوسعة بالـ modules.

---

## 2. أهم 10 أنماط معمارية تبنّاها Odoo

### 2.1 Modular Apps (تطبيقات معيارية)
كل وظيفة في Odoo هي **module** مستقل داخل مجلد `addons/` (`__manifest__.py` + Python + XML + static assets). يمكن تركيب/إزالة أي module دون لمس الباقي — تفعيل أو تعطيل ميزات كاملة من واجهة الإعدادات.

- **الفائدة لـ ERP-SYSTEM:** نمذجة كل وحدة (Identity, Finance, Inventory, Projects, Reports, Notifications) كـ module منفصل بمسار واضح.
- **مثال مرجعي:** [Building a Module — Odoo 19.0](https://www.odoo.com/documentation/19.0/developer/tutorials/backend.html).

### 2.2 Inheriting & Extending (لا تُعدّل الـ core أبدًا)
آليتان رسميتان للـ inheritance:
- **`_inherit`** على الـ Python class لإضافة/تعديل حقول، methods، constraints على model موجود دون لمس كوده الأصلي.
- **View inheritance** عبر `<record model="ir.ui.view"><field name="inherit_id" ref="..."/><xpath expr="..." position="after">` لتعديل أي view بـ XPath.

> "Odoo's inheritance features provide an effective extensibility mechanism. They allow you to extend existing third-party apps without changing them directly." — Odoo 19 Docs, Ch.12 Inheritance.

- **الفائدة لـ ERP-SYSTEM:** قابلية الترقية (upgrades) بلا كسر، فصل الـ customizations في طبقة خاصة بها.

### 2.3 Cron Jobs & Scheduled Actions (`ir.cron`)
مهام دورية تُعرَّف في `ir.cron` model — اسم، interval (`numbercall`, `interval_number`, `interval_type`: minutes/hours/days/...), كود Python يٌنفَّذ، retry logic، وتسجيل executions.

- **أمثلة حقيقية:** إرسال تذكيرات الفواتير المتأخرة، إعادة حساب المخزون، مزامنة البنك، إعادة محاولة الـ webhooks.
- **الفائدة لـ ERP-SYSTEM:** خلفية jobs لإصدار التقارير، التسويات، الإشعارات الدورية، الأرشفة.

### 2.4 Wizards (معالجات للعمليات المعقدة)
**`TransientModel`** = model مؤقت يُحذف تلقائيًا بعد العملية. يُستخدم في نماذج متعددة الخطوات (Multi-step wizards) — مثل تأكيد أمر بيع، استيراد CSV، دمج جهات اتصال، إقفال سنة مالية.

- **الفائدة لـ ERP-SYSTEM:** العمليات التي تتطلب جمع مدخلات عبر شاشات قبل تنفيذ إجراء فعلي (Setup Wizard، Import Wizard، Period Close Wizard).

### 2.5 Server Actions (`ir.actions.server`)
إجراءات قابلة لإعادة الاستخدام تُحفَّز من زر في الـ Form View، أو من قائمة "Action" في الـ List View، أو من Cron. أنواعها: Execute Python Code، Create Record، Write Record، Multi (سلسلة إجراءات)، Send Email، Webhook.

- **الفائدة لـ ERP-SYSTEM:** أتمتة بدون كود لإجراءات شائعة (Approve, Reject, Archive, Duplicate)، تسهيل إعداد العميل.

### 2.6 Email Templates (`mail.template`)
قوالب بريد بنطاق QWeb + placeholders (`${object.partner_id.name}`) تُرسَل تلقائيًا عند أحداث (تأكيد طلب، فاتورة مستحقة، عرض سعر). تدعم لغات متعددة و layouts قابلة للتخصيص.

- **الفائدة لـ ERP-SYSTEM:** تنبيهات وإشعارات احترافية بدون إعادة كتابة في كل مكان — واحد template، عدة triggers.

### 2.7 QWeb Templates (للطباعة و التقارير و الـ Web)
محرك templating XML مدمج في Odoo — يُستخدم لـ:
- **PDF Reports** (فواتير، عروض أسعار، كشوف حساب) عبر `wkhtmltopdf`.
- **Web pages** (موقع التجارة الإلكترونية، بوابة العميل).
- **Kanban cards** (بطاقات بصريّة ديناميكية).
- **Email body**.

- **الفائدة لـ ERP-SYSTEM:** طباعة احترافية للفواتير و Reports مع قابلية تخصيص كاملة من واجهة الـ Studio.

### 2.8 Chatter (نشاط/محادثات على الـ record)
كل model يرث من **`mail.thread`** يحصل تلقائيًا على: شريط رسائل (`message_ids`)، Followers (`message_follower_ids`)، Activities المجدولة (`activity_ids`)، Tracking للحقول المهمة. يظهر في أسفل Form View كـ widget موحد.

```python
class SaleOrder(models.Model):
    _name = 'sale.order'
    _inherit = ['mail.thread', 'mail.activity.mixin']
```

- **الفائدة لـ ERP-SYSTEM:** سجل تدقيق (Audit Log) مدمج لكل entity، تواصل سياقي بين أعضاء الفريق دون مغادرة الـ record.

### 2.9 Multi-Company بـ Allowed Companies
المستخدم يمكنه تفعيل عدة شركات في نفس الجلسة (`allowed_companies`) — يرى ويعدّل سجلات كل الشركات دفعةً واحدة. يطبَّق ذلك عبر:
- **Field `company_id`** (مع `check_company=True` على الـ Many2one لِمنع خلط السجلات).
- **`company_dependent=True`** على الحقول التي تختلف قيمتها حسب الشركة.
- **Default company** عبر `_default_company_id` أو group `base.group_multi_company`.

- **الفائدة لـ ERP-SYSTEM:** جاهز لتوسعة Multi-Company — فصل كامل للبيانات مع تجربة مستخدم موحدة.

### 2.10 Record Rules (`ir.rule`) للـ Multi-Tenancy و Security
قواعد على مستوى الـ record (Domain filters) مرتبطة بـ Groups — تتحكم في **من يرى/يعدّل أي صف**. تُستخدم لـ:
- **Row-Level Security** (موظف يرى أوامر البيع الخاصة بقسمه فقط).
- **Multi-Company isolation** (شركة A لا ترى سجلات شركة B حتى لو كانت في نفس قاعدة البيانات).
- **Field-Level Access** عبر Groups.

```xml
<record id="sale_order_rule" model="ir.rule">
    <field name="name">Sales: own company</field>
    <field name="model_id" ref="model_sale_order"/>
    <field name="domain_force">[('company_id','in',company_ids)]</field>
    <field name="groups" eval="[(4, ref('sales_team.group_sale_salesman'))]"/>
</record>
```

- **الفائدة لـ ERP-SYSTEM:** بديل مرن لـ Tenant-based isolation، يسمح بعزل الـ company داخل نفس الـ tenant مع قواعد دقيقة.

---

## 3. أهم 3 أنماط UX/UI

### 3.1 View Types متعددة لنفس الـ Model
نفس الـ model يمكن تقديمه بأكثر من view type يُبدّلها المستخدم من شريط الـ View Switcher:
- **`Form`** — صفحة تفاصيل السجل (مع tabs، status bar، buttons في الـ header).
- **`List`** (كان اسمه Tree) — جدول قابل للترتيب والتحرير inline.
- **`Kanban`** — بطاقات قابلة للسحب والإفلات بين الأعمدة (Stages).
- **`Calendar`** — عرض تقويم حسب التاريخ.
- **`Graph` / `Pivot`** — تحليلات بصرية.
- **`Gantt`** — جدول زمني للمهام و المشاريع.
- **`Activity`** — كل الـ activities المجدولة عبر الـ models.

> **القيمة:** المستخدم **يختار** الواجهة الأنسب لمهمته — دون قفز بين شاشات.

### 3.2 Search View: Filters / Group By / Search
شريط بحث موحد في كل List/Kanban view يحوي:
- **Search box** مع بحث ذكي يشمل جميع الحقول الـ indexed.
- **Filters** مُعرَّفة مسبقًا (My Drafts, Late Invoices, This Month) قابلة للحفظ كـ **Custom Filter**.
- **Group By** لتجميع السجلات (مثلًا حسب الحالة، العميل، الفترة).
- **Comparison** لمقارنة فترتين (هذا الشهر vs الشهر الماضي).
- **Favorites** لحفظ تركيبات بحث متكررة كزر في الشريط.

> **القيمة:** تجربة مستخدم **موحدة** عبر كل الـ models، لا تحتاج بناء شاشات بحث خاصة لكل وحدة.

### 3.3 Dashboard بـ KPI Tiles + Cohort Charts
الـ Dashboards (مثل Accounting Dashboard, Inventory Dashboard, Purchase Dashboard) تجمع:
- **KPI Tiles** بأرقام كبيرة + نسبة التغيير + سهم لأعلى/أسفل.
- **Charts** تفاعلية (line, bar, pie, cohort).
- **Quick Action Buttons** لأداء إجراءات شائعة.
- **Drill-down** من الـ tile إلى الـ List View مع filter مُطبَّق.

> **القيمة:** لوحة تحكم Executive بدون كتابة تقارير مخصصة لكل عميل — قوالب جاهزة قابلة للتخصيص.

---

## 4. دروس مستفادة لـ ERP-SYSTEM

| # | النمط | التطبيق المقترح في ERP-SYSTEM |
|---|------|------------------------------|
| 1 | **Modular Apps** | كل module (Identity, Finance, Inventory...) في مجلد منفصل، مع manifest يوضح الـ dependencies. |
| 2 | **Inheritance** | ابدأ من اليوم بقاعدة **"لا تعدّل كود core module من module آخر"** — استخدم extension hooks واضحة. |
| 3 | **Cron / Scheduled Jobs** | HostedService بسيط في الـ Backend لاستدعاء handlers دورية (تقارير، تسويات، notifications). |
| 4 | **Wizards** | عند أي عملية متعددة الخطوات (Import، Period Close، Setup) استخدم شاشة wizard بدلاً من شاشة واحدة ثقيلة. |
| 5 | **Server Actions** | مكتبة إجراءات قابلة لإعادة الاستخدام، تُربط بأزرار في الـ Form View. |
| 6 | **Email Templates** | Templates موحدة بالعربية + الإنجليزية، placeholders واضحة، preview قبل الإرسال. |
| 7 | **QWeb للطباعة** | محرر قوالب تقارير/PDF مع variables و conditionals. |
| 8 | **Chatter** | كل entity جوهري (Invoice, PO, Project) يحصل على Activity Log + Comments داخلية + Audit Trail. |
| 9 | **Multi-Company (future)** | إضافة `company_id` على entities المالية الأساسية + Rules للعزل. |
| 10 | **Record Rules** | استكمال TenantContext في الـ Backend عبر Rule-based filter بدل hardcoded WHERE. |

**UX/UI Quick Wins:**
- اعرض نفس الـ List Page بأوضاع: Table (افتراضي) / Cards / Timeline — يُبدّلها المستخدم.
- شريط Search موحد في كل صفحة قائمة، يحوي: Search Box + Filters + Group By + Saved Searches.
- Dashboard بسيط في الـ Home Page بكروت KPI + رسم بياني واحد، مع Drill-down.

---

## 5. روابط مرجعية رسمية (3 روابط)

1. **Architecture Overview (Server Framework 101) — Odoo 19.0**
   [https://www.odoo.com/documentation/19.0/developer/tutorials/server_framework_101/01_architecture.html](https://www.odoo.com/documentation/19.0/developer/tutorials/server_framework_101/01_architecture.html)
2. **Actions Reference (Server Actions, Scheduled Actions, Wizards) — Odoo 19.0**
   [https://www.odoo.com/documentation/19.0/developer/reference/backend/actions.html](https://www.odoo.com/documentation/19.0/developer/reference/backend/actions.html)
3. **Multi-company Guidelines — Odoo Developer Docs**
   [https://www.odoo.com/documentation/19.0/developer/howtos/company.html](https://www.odoo.com/documentation/19.0/developer/howtos/company.html)

---

## 6. ملاحظات ختامية

- **حدود البحث:** هذا ملف موجز (موجز تنفيذي)، وليس توثيقًا تقنيًا كاملاً. الـ patterns العشرة المختارة هي الأكثر نضجًا وانتشارًا في Odoo، وقابلة للتطبيق مباشرةً في ERP-SYSTEM.
- **الإصدار:** كل الأمثلة والروابط تشير إلى **Odoo 19.0** (صدر 2025). الأنماط الأساسية لم تتغيّر جوهريًا منذ Odoo 12، لكن الـ syntax تطوّر (انتقال من `Tree` إلى `List` كاسم رسمي، اعتماد `QWeb 2.0` تدريجيًا).
- **مقارنة سريعة:** Daftra عربي/سحابي، ERPNext مفتوح بالكامل (MIT)، Odoo Open Core — كل واحد منهم يجيب دروس مختلفة. Odoo **الأقوى** في: Modular Apps + Inheritance + Record Rules + Views المتعددة.

---

## 7. أولوية التطبيق في ERP-SYSTEM

| المرحلة | الأنماط الواجب تبنيها فورًا | السبب |
|--------|----------------------------|------|
| **Phase 2.5+ (الحالي)** | Modular Apps (2.1) + Search View (3.2) | البنية الحالية بالفعل modular — تعزيزها بقواعد واضحة + استكمال شريط البحث الموحد في كل صفحات القوائم. |
| **Phase 3** | Inheritance (2.2) + Server Actions (2.5) + Email Templates (2.6) | تمهيد لـ Open API للمطورين و Studio-like UI لإدارة workflows بدون كود. |
| **Phase 4** | Chatter (2.8) + Wizard (2.4) + Cron (2.3) | تجربة Collaboration حقيقية + أتمتة التقارير الدورية. |
| **Phase 5+** | QWeb Reports (2.7) + Multi-Company (2.9) + Record Rules (2.10) | ميزات Enterprise-level + جاهزية Multi-Company/Multi-Tenant. |

---

## 8. ملخص تنفيذي (سطر واحد لكل نمط)

1. **Modular Apps** → كل ميزة = module مستقل قابل للتفعيل/الإلغاء.
2. **Inheritance** → أعد تعريف/وسّع أي شيء بدون تعديل الأصل.
3. **Cron** → مهام دورية موصوفة في data، تُنفَّذ تلقائيًا.
4. **Wizards** → model مؤقت لجمع مدخلات عملية متعددة الخطوات.
5. **Server Actions** → أتمتة بدون كود، تُربط بأزرار في الـ views.
6. **Email Templates** → قوالب بريد موحدة بـ placeholders تُرسَل على events.
7. **QWeb** → templating واحد للـ PDF/Web/Email/Kanban.
8. **Chatter** → سجل تدقيق + محادثات + activities مدمجة في كل entity.
9. **Multi-Company** → عدة شركات في جلسة واحدة، عزل بيانات بـ company_id.
10. **Record Rules** → Row-Level Security مرنة عبر Groups و Domain.

**UX:**
- **Multi-View** → 7 أنماط عرض لنفس الـ model، المستخدم يختار.
- **Search موحد** → Filters + Group By + Favorites في كل List View.
- **Dashboard** → KPI tiles + charts + drill-down.

---

> **التوصية:** اقرأ القسمين 2 و 3 فقط — تحوي كل ما يلزم لاتخاذ قرارات معمارية سريعة في ERP-SYSTEM.