# Sprint 59: Construction Core (DEC-179..183)

**الهدف:** بناء طبقة Construction كاملة للنظام — لائحة أسعار + BOQ + Variation Orders + WIP/Retention/Advance accounting.

**Branch:** `feature/sprint-59-construction-core` (from develop @ f4b5d38)

**Background:** لائحة 355 لسنة 2026 + دفتر حصر امجاد + مستخلص + مقايسة معدلة + تقرير فني (5 ملفات). See `docs/plans/libyan-construction-analysis.html`.

---

## DEC-179 — UoM إضافيات (م.ط, مقطوعية, عدد)

**هدف:** إضافة 3 وحدات ناقصة من اللائحة 355 + Arabic aliases.

**UoM الحالية:** pcs, g, kg, ton, m, cm, mm, km, m², m³, l, ml, h, d, set, box, pkg (17)

**UoM المطلوب إضافتها:** 
- م.ط (متر طولي / linear meter) — code: `mlt` (متر طولي)
- مقطوعية (lump sum) — code: `lump` 
- عدد (count/ea) — code: `ea` (already have `pcs`? add as alias)

**حقيقة:** "عدد" بالعربية تعني "unit count" — pcs يخدم نفس الدور. نضيف `ea` كـ alias تقنياً.

**خطوات:**
1. إدراج 3 UoM جديدة في `DefaultInventorySeed.DefaultUoMs` (لكن للـ holding-company reference data، ليس للـ tenants)
2. بديل: إضافة الـ UoM في الـ seeder من Sprint 60 (الـ libyan holding seeder) — أكثر مرونة

**الحل المُختار:** تعديل `HoldingReferenceDataSeeder` (أو ما يعادله) لإدراج الوحدات الـ 3 الجديدة.

**ملفات:** 
- `src/backend/Shared/SeedData/DefaultInventorySeed.cs` (add to array)
- OR a new seeder specifically for `Holding Enterprise`

**الـ `units_of_measure` table** — لا تغيير schema (code/name/symbol موجودين).

---

## DEC-180 — Price Lists (لائحة الأسعار)

**هدف:** جداول لائحة الأسعار + seeder يستورد لائحة 355 لسنة 2026 (5,000+ بند).

**Schema:**

```jsonc
// src/backend/Host/data-types/price_lists.json
{
  "name": "PriceList", "table": "price_lists", "version": "1.0.0", "module": "Projects",
  "fields": [
    { "name": "id", "type": "uuid", "primary_key": true },
    { "name": "company_id", "type": "uuid", "nullable": false, "foreign_key": { "table": "companies" } },
    { "name": "code", "type": "varchar(50)", "nullable": false, "description": "355-2026" },
    { "name": "name", "type": "varchar(200)", "nullable": false },
    { "name": "description", "type": "text", "nullable": true },
    { "name": "issued_by", "type": "varchar(200)", "nullable": true },
    { "name": "issued_at", "type": "date", "nullable": true },
    { "name": "effective_from", "type": "date", "nullable": true },
    { "name": "effective_to", "type": "date", "nullable": true },
    { "name": "is_active", "type": "boolean", "nullable": false, "default": "true" },
    { "name": "created_at", "type": "timestamptz", "nullable": false, "default": "now()" },
    { "name": "created_by", "type": "uuid", "nullable": false, "foreign_key": { "table": "users" } },
    { "name": "updated_at", "type": "timestamptz", "nullable": false, "default": "now()" }
  ],
  "indexes": [
    { "name": "ix_price_lists_company_code", "columns": ["company_id", "code"], "unique": true }
  ]
}
```

```jsonc
// src/backend/Host/data-types/price_list_items.json
{
  "name": "PriceListItem", "table": "price_list_items", "version": "1.0.0", "module": "Projects",
  "fields": [
    { "name": "id", "type": "uuid", "primary_key": true },
    { "name": "price_list_id", "type": "uuid", "nullable": false, "foreign_key": { "table": "price_lists" } },
    { "name": "code", "type": "varchar(50)", "nullable": false, "description": "1.1.1.1 (hierarchical)" },
    { "name": "parent_code", "type": "varchar(50)", "nullable": true },
    { "name": "description", "type": "text", "nullable": false },
    { "name": "unit_id", "type": "uuid", "nullable": false, "foreign_key": { "table": "units_of_measure" } },
    { "name": "unit_price", "type": "numeric(18,4)", "nullable": false, "default": "0" },
    { "name": "section", "type": "varchar(50)", "nullable": true, "description": "Buildings | Roads | Water | etc." },
    { "name": "category", "type": "varchar(50)", "nullable": true, "description": "Material | Labor | Equipment | Subcontract | Other" },
    { "name": "level", "type": "int", "nullable": false, "default": "4", "description": "1=Chapter, 2=Section, 3=Sub, 4=Line" },
    { "name": "is_active", "type": "boolean", "nullable": false, "default": "true" },
    { "name": "created_at", "type": "timestamptz", "nullable": false, "default": "now()" }
  ],
  "indexes": [
    { "name": "ix_price_list_items_list_code", "columns": ["price_list_id", "code"], "unique": true },
    { "name": "ix_price_list_items_list_parent", "columns": ["price_list_id", "parent_code"] },
    { "name": "ix_price_list_items_section", "columns": ["section"] }
  ]
}
```

**Seeder:** `LibyanPriceListSeeder` — يستورد الـ 5,000+ بند من اللائحة.

**سياسة:** اللائحة مرجع عام. كل holding يقدر ينسخها لـ company_id الخاص به. (global reference model = كل company عنده نسخته.)

**ملاحظة:** لتجنب بطء الـ seeder، نعمل **lazy import**:
- SEED يحوي فقط الـ line items الأكثر استخداماً (TOP 200) 
- الباقي يجلب من API عند الطلب (مستقبلاً: endpoint يستدعي اللائحة الرسمية)

**الحل العملي:** نُنشئ SEED يحوي **~50 بند لكل قسم** (9 sectors × 50 = 450 بند) — يكفي للـ demo. باقي الـ 5,000 يقدر المهندس يدخلهم يدوياً عبر UI.

**ملفات:**
- `src/backend/Host/data-types/price_lists.json` (new)
- `src/backend/Host/data-types/price_list_items.json` (new)
- `src/backend/Modules/Projects/Application/Services/PriceListService.cs` (new)
- `src/backend/Modules/Projects/Application/Dtos/PriceListDtos.cs` (new)
- `src/backend/Host/Controllers/PriceListsController.cs` (new)
- `src/backend/Shared/SeedData/LibyanPriceListSeederHostedService.cs` (new)

---

## DEC-181 — BOQ (Bill of Quantities)

**هدف:** إنشاء BOQ كامل لمشروع مع sub-items (L×W×H calculations).

**Schema:**

```jsonc
// src/backend/Host/data-types/boq_sections.json
{
  "name": "BoqSection", "table": "boq_sections", "version": "1.0.0", "module": "Projects",
  "fields": [
    { "name": "id", "type": "uuid", "primary_key": true },
    { "name": "company_id", "type": "uuid", "nullable": false, "foreign_key": { "table": "companies" } },
    { "name": "project_id", "type": "uuid", "nullable": false, "foreign_key": { "table": "projects" } },
    { "name": "code", "type": "varchar(50)", "nullable": false, "description": "1, 2, 3 (top-level chapter)" },
    { "name": "name", "type": "varchar(200)", "nullable": false, "description": "أعمال الإزالة" },
    { "name": "sort_order", "type": "int", "nullable": false, "default": "0" },
    { "name": "is_active", "type": "boolean", "nullable": false, "default": "true" },
    { "name": "created_at", "type": "timestamptz", "nullable": false, "default": "now()" }
  ],
  "indexes": [
    { "name": "ix_boq_sections_project", "columns": ["project_id", "code"], "unique": true }
  ]
}

// src/backend/Host/data-types/boq_lines.json
{
  "name": "BoqLine", "table": "boq_lines", "version": "1.0.0", "module": "Projects",
  "fields": [
    { "name": "id", "type": "uuid", "primary_key": true },
    { "name": "company_id", "type": "uuid", "nullable": false },
    { "name": "project_id", "type": "uuid", "nullable": false, "foreign_key": { "table": "projects" } },
    { "name": "section_id", "type": "uuid", "nullable": false, "foreign_key": { "table": "boq_sections" } },
    { "name": "price_list_item_id", "type": "uuid", "nullable": true, "foreign_key": { "table": "price_list_items" } },
    { "name": "code", "type": "varchar(50)", "nullable": false, "description": "1.1.1.1 (from price list)" },
    { "name": "description", "type": "text", "nullable": false },
    { "name": "unit_id", "type": "uuid", "nullable": false, "foreign_key": { "table": "units_of_measure" } },
    { "name": "contract_qty", "type": "numeric(18,4)", "nullable": false, "default": "0" },
    { "name": "executed_qty", "type": "numeric(18,4)", "nullable": false, "default": "0" },
    { "name": "unit_price", "type": "numeric(18,4)", "nullable": false, "default": "0" },
    { "name": "regional_premium_pct", "type": "numeric(5,2)", "nullable": false, "default": "0" },
    { "name": "final_unit_price", "type": "numeric(18,4)", "nullable": false, "default": "0" },
    { "name": "total_amount", "type": "numeric(18,4)", "nullable": false, "default": "0" },
    { "name": "is_measurable", "type": "boolean", "nullable": false, "default": "true" },
    { "name": "is_active", "type": "boolean", "nullable": false, "default": "true" },
    { "name": "sort_order", "type": "int", "nullable": false, "default": "0" },
    { "name": "created_at", "type": "timestamptz", "nullable": false, "default": "now()" }
  ],
  "indexes": [
    { "name": "ix_boq_lines_project_code", "columns": ["project_id", "code"], "unique": true },
    { "name": "ix_boq_lines_section", "columns": ["section_id"] }
  ]
}

// src/backend/Host/data-types/boq_subitems.json
{
  "name": "BoqSubitem", "table": "boq_subitems", "version": "1.0.0", "module": "Projects",
  "fields": [
    { "name": "id", "type": "uuid", "primary_key": true },
    { "name": "company_id", "type": "uuid", "nullable": false },
    { "name": "boq_line_id", "type": "uuid", "nullable": false, "foreign_key": { "table": "boq_lines" } },
    { "name": "description", "type": "varchar(200)", "nullable": false },
    { "name": "count", "type": "int", "nullable": false, "default": "1" },
    { "name": "length_m", "type": "numeric(10,3)", "nullable": false, "default": "0" },
    { "name": "width_m", "type": "numeric(10,3)", "nullable": false, "default": "0" },
    { "name": "height_m", "type": "numeric(10,3)", "nullable": false, "default": "0" },
    { "name": "initial_qty", "type": "numeric(18,4)", "nullable": false, "default": "0" },
    { "name": "deductions", "type": "numeric(18,4)", "nullable": false, "default": "0" },
    { "name": "final_qty", "type": "numeric(18,4)", "nullable": false, "default": "0" },
    { "name": "sort_order", "type": "int", "nullable": false, "default": "0" },
    { "name": "created_at", "type": "timestamptz", "nullable": false, "default": "now()" }
  ],
  "indexes": [
    { "name": "ix_boq_subitems_line", "columns": ["boq_line_id", "sort_order"] }
  ]
}
```

**Logic:**
- `boq_lines.contract_qty` = SUM(`boq_subitems.final_qty`) (computed in trigger or service)
- `boq_lines.total_amount` = `contract_qty * final_unit_price`
- `final_unit_price` = `unit_price * (1 + regional_premium_pct/100)`

**ملفات:**
- `src/backend/Host/data-types/boq_sections.json` (new)
- `src/backend/Host/data-types/boq_lines.json` (new)
- `src/backend/Host/data-types/boq_subitems.json` (new)
- `src/backend/Modules/Projects/Application/Services/BoqService.cs` (new)
- `src/backend/Modules/Projects/Application/Dtos/BoqDtos.cs` (new)
- `src/backend/Host/Controllers/BoqController.cs` (new)
- `src/frontend/app/(authenticated)/projects/[id]/boq/page.tsx` (new)

---

## DEC-182 — Variation Orders

**هدف:** إدارة الأوامر التعديلية (أوامر تغيير العقد).

**Schema:**

```jsonc
// src/backend/Host/data-types/variation_orders.json
{
  "name": "VariationOrder", "table": "variation_orders", "version": "1.0.0", "module": "Projects",
  "fields": [
    { "name": "id", "type": "uuid", "primary_key": true },
    { "name": "company_id", "type": "uuid", "nullable": false },
    { "name": "project_id", "type": "uuid", "nullable": false, "foreign_key": { "table": "projects" } },
    { "name": "contract_id", "type": "uuid", "nullable": true, "foreign_key": { "table": "contracts" } },
    { "name": "order_number", "type": "varchar(50)", "nullable": false },
    { "name": "issued_at", "type": "date", "nullable": false },
    { "name": "reason", "type": "text", "nullable": true },
    { "name": "status", "type": "varchar(20)", "nullable": false, "default": "'DRAFT'", "description": "DRAFT | PENDING | APPROVED | REJECTED" },
    { "name": "original_contract_value", "type": "numeric(18,4)", "nullable": false, "default": "0" },
    { "name": "variation_amount", "type": "numeric(18,4)", "nullable": false, "default": "0" },
    { "name": "new_contract_value", "type": "numeric(18,4)", "nullable": false, "default": "0" },
    { "name": "approved_at", "type": "timestamptz", "nullable": true },
    { "name": "approved_by", "type": "uuid", "nullable": true },
    { "name": "notes", "type": "text", "nullable": true },
    { "name": "created_at", "type": "timestamptz", "nullable": false, "default": "now()" },
    { "name": "created_by", "type": "uuid", "nullable": false },
    { "name": "updated_at", "type": "timestamptz", "nullable": false, "default": "now()" }
  ],
  "indexes": [
    { "name": "ix_vo_project_number", "columns": ["project_id", "order_number"], "unique": true }
  ]
}

// src/backend/Host/data-types/variation_order_lines.json
{
  "name": "VariationOrderLine", "table": "variation_order_lines", "version": "1.0.0", "module": "Projects",
  "fields": [
    { "name": "id", "type": "uuid", "primary_key": true },
    { "name": "company_id", "type": "uuid", "nullable": false },
    { "name": "variation_order_id", "type": "uuid", "nullable": false, "foreign_key": { "table": "variation_orders" } },
    { "name": "boq_line_id", "type": "uuid", "nullable": true, "foreign_key": { "table": "boq_lines" } },
    { "name": "line_type", "type": "varchar(20)", "nullable": false, "description": "ADD | MODIFY | DELETE" },
    { "name": "description", "type": "text", "nullable": false },
    { "name": "qty_change", "type": "numeric(18,4)", "nullable": false, "default": "0" },
    { "name": "price_change", "type": "numeric(18,4)", "nullable": false, "default": "0" },
    { "name": "net_change", "type": "numeric(18,4)", "nullable": false, "default": "0" },
    { "name": "sort_order", "type": "int", "nullable": false, "default": "0" },
    { "name": "created_at", "type": "timestamptz", "nullable": false, "default": "now()" }
  ],
  "indexes": [
    { "name": "ix_vo_lines_vo", "columns": ["variation_order_id", "sort_order"] }
  ]
}
```

**ملفات:**
- `src/backend/Host/data-types/variation_orders.json` (new)
- `src/backend/Host/data-types/variation_order_lines.json` (new)
- `src/backend/Modules/Projects/Application/Services/VariationOrderService.cs` (new)
- `src/backend/Modules/Projects/Application/Dtos/VariationOrderDtos.cs` (new)
- `src/backend/Host/Controllers/VariationOrdersController.cs` (new)
- `src/frontend/app/(authenticated)/projects/[id]/variations/page.tsx` (new)

---

## DEC-183 — CoA + Posting Rules (محاسبة آلية)

**هدف:** إضافة 4 حسابات + 4 posting rules للمستخلصات والأوامر التعديلية.

**4 CoA جديدة:**

```
L1 = 1102 (أصول متداولة)
  L2 = 1102 (موجود — نقدية وبنوك) — نضيف:
  L2 = 1103 (أعمال تحت التنفيذ — WIP)
    L3 = 1103-001 WIP عام
    L3 = 1103-002 WIP — مشاريع قيد التنفيذ
    L3 = 1103-003 التزامات عقود (Off-balance)

L1 = 1201 (مدينون) — نضيف:
  L2 = 1201 (موجود) — نضيف:
  L3 = 1201-101 ذمم عقود — مشاريع حكومية (NDA)
  L3 = 1201-102 ذمم عقود — جهات حكومية أخرى
  L3 = 1201-103 ذمم عقود — عملاء خاصون

L1 = 2100 (خصوم متداولة) — نضيف:
  L2 = 2106 دفعة مقدمة مستلمة من العملاء
  L2 = 2107 احتجاز ضمان مستحق الدفع
```

**ملفات:** تعديل `src/backend/Shared/SeedData/DefaultCoASeed.cs` لإضافة الحسابات الجديدة.

**4 Posting Rules جديدة:**

| Code | Event | Dr | Cr |
|------|-------|----|----|
| PR-CONTRACT-OPEN | Contract signed | 1103-002 | 1103-003 |
| PR-BILLING-POST | Billing approved | 1201-101 / 5101 | 4301 / 1103-002 |
| PR-BILLING-ADVANCE-DEDUCT | Advance deduction | 2106 | 1201-101 |
| PR-BILLING-RETENTION-DEDUCT | Retention deduction | (net) | 2107 |
| PR-VO-OPEN | Variation order approved | 1103-002 | 1103-003 |
| PR-FINAL-DELIVERY | Project delivered | 1103-002 | 5101 / 3202 |
| PR-RETENTION-RELEASE | Retention released | 2107 | 1101 |

**ملفات:**
- `src/backend/Shared/SeedData/DefaultCoASeed.cs` (add 4 accounts)
- `src/backend/Shared/SeedData/ConstructionPostingRulesSeeder.cs` (new — 7 posting rules)
- `src/backend/Modules/Finance/Application/Services/BillingAccountingService.cs` (new — auto-GL)

---

## Sprint 59 Done Criteria

- [ ] 9 جداول جديدة schema في `data-types/`
- [ ] BE services: PriceList, Boq, VariationOrder
- [ ] BE controllers + DTOs
- [ ] Seeder يستورد 450+ بند (50 من كل قطاع)
- [ ] FE page `/projects/[id]/boq`
- [ ] FE page `/projects/[id]/variations`
- [ ] 4 CoA جديدة + 7 Posting Rules
- [ ] tsc 0 errors
- [ ] build success
- [ ] Playwright smoke test: navigate + load pages

**Time budget: 4-5 hours (DEC-179..183).**
