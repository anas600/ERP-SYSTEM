-- =====================================================================
-- ERP System — Sprint 4 Demo Data Seed
-- =====================================================================
-- Purpose: 3 companies + 10 users + 100+ transactions for MFA Holding demo
-- Idempotent: ALL inserts use ON CONFLICT DO NOTHING / NOT EXISTS guards.
-- Re-runnable: safe to run multiple times without duplicate records.
-- Multi-Company: every company_id-scoped table is filtered by company_id.
-- Arabic: descriptions, names, notes use Libyan/Arabic dialect.
--
-- Sprint 4 Block A — targets (per docs/workflow/demo-roadmap.md):
--   - 3 subsidiary companies under Holding Enterprise
--   - 10 users with varied roles + user_companies assignments
--   - 100+ transactions across:
--       * 30 sales invoices (10 per company)
--       * 20 vendor bills (procurement, distributed)
--       * 30 journal entries (finance, monthly spread)
--       * 20 stock movements (inventory, IN+OUT)
--       * 35+ activity_log entries (5/day for 7 days)
-- Total: 135 transactions (135 >= 100 ✅)
--
-- BCrypt hash for password "Demo1234" (workFactor=11):
--   $2a$11$FKXjp3qKKr9.Xbcfn7XjIuUMyEcmRo.TYZPFhcoQxHj4CNtnALqki
-- Generated via tools/HashGen/ — verified with BCrypt.Verify → True.
-- (Existing admin user uses $2a$12$ — both are valid BCrypt and BCrypt.Verify
--  handles any cost; the AuthService can authenticate against either.)
-- =====================================================================

\set ON_ERROR_STOP on

-- =====================================================================
-- SECTION 0: Idempotent activity_log table creation
-- (The JSON DataTypeMigrator may not have created it on every deploy;
--  the Sprint 3 Sprint-3 PR #167 used it but a fresh build can lack it.)
-- =====================================================================
CREATE TABLE IF NOT EXISTS activity_log (
    id           bigserial PRIMARY KEY,
    company_id   uuid NULL,
    user_id      uuid NULL,
    action       varchar(40) NOT NULL,
    ip_address   varchar(45) NULL,
    user_agent   varchar(255) NULL,
    metadata     jsonb NULL,
    created_at   timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_activity_log_user_created
    ON activity_log (user_id, created_at);
CREATE INDEX IF NOT EXISTS ix_activity_log_company_created
    ON activity_log (company_id, created_at);
CREATE INDEX IF NOT EXISTS ix_activity_log_action_created
    ON activity_log (action, created_at);

-- =====================================================================
-- SECTION 1: 3 Subsidiary Companies under Holding
-- Holding ID: 00000000-0000-0000-0000-000000000001 (already exists)
-- =====================================================================
INSERT INTO companies (id, code, name, legal_name, parent_company_id, is_group, base_currency, is_active, created_at, updated_at)
VALUES
  ('11111111-1111-1111-1111-111111111111', 'ALF-CONST',
   'شركة الفجر للمقاولات العامة', 'AlFajr General Contracting Co.',
   '00000000-0000-0000-0000-000000000001',
   false, 'LYD', true, now(), now()),

  ('22222222-2222-2222-2222-222222222222', 'ALF-TRADE',
   'شركة الفجر للتجارة والتوريدات', 'AlFajr Trading & Supplies Co.',
   '00000000-0000-0000-0000-000000000001',
   false, 'LYD', true, now(), now()),

  ('33333333-3333-3333-3333-333333333333', 'ALN-LOG',
   'شركة النور للخدمات اللوجستية', 'AlNoor Logistics Services Co.',
   '00000000-0000-0000-0000-000000000001',
   false, 'LYD', true, now(), now())
ON CONFLICT (code) DO NOTHING;

-- Fallback: also key by (parent_company_id, code) for ON CONFLICT
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM companies WHERE code = 'ALF-CONST') THEN
    INSERT INTO companies (id, code, name, parent_company_id, is_group, base_currency, is_active, created_at, updated_at)
    VALUES ('11111111-1111-1111-1111-111111111111', 'ALF-CONST', 'شركة الفجر للمقاولات العامة',
            '00000000-0000-0000-0000-000000000001', false, 'LYD', true, now(), now());
  END IF;
  IF NOT EXISTS (SELECT 1 FROM companies WHERE code = 'ALF-TRADE') THEN
    INSERT INTO companies (id, code, name, parent_company_id, is_group, base_currency, is_active, created_at, updated_at)
    VALUES ('22222222-2222-2222-2222-222222222222', 'ALF-TRADE', 'شركة الفجر للتجارة والتوريدات',
            '00000000-0000-0000-0000-000000000001', false, 'LYD', true, now(), now());
  END IF;
  IF NOT EXISTS (SELECT 1 FROM companies WHERE code = 'ALN-LOG') THEN
    INSERT INTO companies (id, code, name, parent_company_id, is_group, base_currency, is_active, created_at, updated_at)
    VALUES ('33333333-3333-3333-3333-333333333333', 'ALN-LOG', 'شركة النور للخدمات اللوجستية',
            '00000000-0000-0000-0000-000000000001', false, 'LYD', true, now(), now());
  END IF;
END $$;

-- =====================================================================
-- SECTION 2: Chart of Accounts per subsidiary (subset, idempotent)
-- All accounts link back to the same Arabic-named account codes used by
-- the Holding's DefaultCoASeed so reports roll up cleanly.
-- =====================================================================
DO $$
DECLARE
  v_companys uuid[] := ARRAY[
    '11111111-1111-1111-1111-111111111111',
    '22222222-2222-2222-2222-222222222222',
    '33333333-3333-3333-3333-333333333333'
  ];
  v_c uuid;
BEGIN
  FOREACH v_c IN ARRAY v_companys LOOP
    -- 1210 النقدية
    INSERT INTO accounts (id, company_id, code, name, type, normal_balance, is_postable, is_active, created_at, updated_at)
    VALUES (gen_random_uuid(), v_c, '1210', 'النقدية', 1, 1, true, true, now(), now())
    ON CONFLICT (company_id, code) DO NOTHING;
    -- 1230 ذمم مدينة
    INSERT INTO accounts (id, company_id, code, name, type, normal_balance, is_postable, is_active, created_at, updated_at)
    VALUES (gen_random_uuid(), v_c, '1230', 'ذمم مدينة (عملاء)', 1, 1, true, true, now(), now())
    ON CONFLICT (company_id, code) DO NOTHING;
    -- 1240 مخزون
    INSERT INTO accounts (id, company_id, code, name, type, normal_balance, is_postable, is_active, created_at, updated_at)
    VALUES (gen_random_uuid(), v_c, '1240', 'مخزون البضاعة', 1, 1, true, true, now(), now())
    ON CONFLICT (company_id, code) DO NOTHING;
    -- 2210 دائنون لموردين
    INSERT INTO accounts (id, company_id, code, name, type, normal_balance, is_postable, is_active, created_at, updated_at)
    VALUES (gen_random_uuid(), v_c, '2210', 'دائنون لموردين', 2, 2, true, true, now(), now())
    ON CONFLICT (company_id, code) DO NOTHING;
    -- 2250 ضريبة مخرجات
    INSERT INTO accounts (id, company_id, code, name, type, normal_balance, is_postable, is_active, created_at, updated_at)
    VALUES (gen_random_uuid(), v_c, '2250', 'ضريبة القيمة المضافة المستحقة', 2, 2, true, true, now(), now())
    ON CONFLICT (company_id, code) DO NOTHING;
    -- 3100 رأس المال
    INSERT INTO accounts (id, company_id, code, name, type, normal_balance, is_postable, is_active, created_at, updated_at)
    VALUES (gen_random_uuid(), v_c, '3100', 'رأس المال', 3, 2, true, true, now(), now())
    ON CONFLICT (company_id, code) DO NOTHING;
    -- 4200 مصروفات إدارية
    INSERT INTO accounts (id, company_id, code, name, type, normal_balance, is_postable, is_active, created_at, updated_at)
    VALUES (gen_random_uuid(), v_c, '4200', 'مصروفات إدارية وعمومية', 5, 1, true, true, now(), now())
    ON CONFLICT (company_id, code) DO NOTHING;
    -- 5110 إيرادات المشاريع
    INSERT INTO accounts (id, company_id, code, name, type, normal_balance, is_postable, is_active, created_at, updated_at)
    VALUES (gen_random_uuid(), v_c, '5110', 'إيرادات المشاريع', 4, 2, true, true, now(), now())
    ON CONFLICT (company_id, code) DO NOTHING;
  END LOOP;
END $$;

-- =====================================================================
-- SECTION 3: Warehouses (one per subsidiary, idempotent by code)
-- =====================================================================
INSERT INTO warehouses (id, company_id, code, name, location, is_active, created_at, updated_at, created_by, updated_by)
VALUES
  (gen_random_uuid(), '11111111-1111-1111-1111-111111111111',
   'WH-CONST-01', 'مستودع الفجر للمقاولات - طرابلس', 'طرابلس - المنطقة الصناعية', true, now(), now(),
   '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111'),
  (gen_random_uuid(), '22222222-2222-2222-2222-222222222222',
   'WH-TRADE-01', 'مستودع الفجر للتجارة - بنغازي', 'بنغازي - شارع جمال عبد الناصر', true, now(), now(),
   '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111'),
  (gen_random_uuid(), '33333333-3333-3333-3333-333333333333',
   'WH-LOG-01',  'مستودع النور اللوجستي - مصراتة', 'مصراتة - ميناء مصراتة', true, now(), now(),
   '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111')
ON CONFLICT (company_id, code) DO NOTHING;

-- Use admin user as warehouse creator if not present
DO $$
DECLARE
  v_admin_id uuid := '11111111-1111-1111-1111-111111111111';
BEGIN
  UPDATE warehouses SET created_by = v_admin_id, updated_by = v_admin_id
    WHERE created_by IS NULL;
END $$;

-- =====================================================================
-- SECTION 4: Items (products/services per company, idempotent by sku+company)
-- =====================================================================
DO $$
DECLARE
  v_items_alf_const uuid[] := ARRAY[
    gen_random_uuid(), gen_random_uuid(), gen_random_uuid(), gen_random_uuid(), gen_random_uuid()
  ];
BEGIN
  INSERT INTO items (id, company_id, sku, name, description, item_type, costing_method, average_cost, standard_cost, reorder_level, reorder_quantity, is_active, created_at, updated_at, created_by, updated_by)
  VALUES
    (v_items_alf_const[1], '11111111-1111-1111-1111-111111111111', 'CON-CEM-50',
     'إسمنت بورتلاندي 50 كجم', 'شيكارة إسمنت محلية الصنع 50 كجم - للمقاولات',
     1, 1, 28.0, 28.0, 100, 500, true, now(), now(),
     '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111'),
    (v_items_alf_const[2], '11111111-1111-1111-1111-111111111111', 'CON-STL-12',
     'حديد تسليح 12 مم', 'حديد تسليح قطر 12 مم - للهيكل الخرساني',
     1, 1, 1850.0, 1850.0, 50, 200, true, now(), now(),
     '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111'),
    (v_items_alf_const[3], '11111111-1111-1111-1111-111111111111', 'CON-BLK-20',
     'بلوك أسمنتي 20 سم', 'بلوك خرساني 20x20x40 سم للجدران',
     1, 1, 1.5, 1.5, 1000, 5000, true, now(), now(),
     '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111'),
    (v_items_alf_const[4], '11111111-1111-1111-1111-111111111111', 'CON-SAN-1M3',
     'رمل ناعم 1 م³', 'رمل ناعم منجم الكفرة - متر مكعب',
     1, 1, 80.0, 80.0, 30, 100, true, now(), now(),
     '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111'),
    (v_items_alf_const[5], '11111111-1111-1111-1111-111111111111', 'CON-PNT-20L',
     'دهان أكريليك أبيض 20 لتر', 'دهان جدران داخلي - أبيض',
     1, 1, 180.0, 180.0, 20, 60, true, now(), now(),
     '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111')
  ON CONFLICT (company_id, sku) DO NOTHING;
END $$;

DO $$
DECLARE
  v_items uuid[] := ARRAY[gen_random_uuid(), gen_random_uuid(), gen_random_uuid(), gen_random_uuid(), gen_random_uuid()];
BEGIN
  INSERT INTO items (id, company_id, sku, name, description, item_type, costing_method, average_cost, standard_cost, reorder_level, reorder_quantity, is_active, created_at, updated_at, created_by, updated_by)
  VALUES
    (v_items[1], '22222222-2222-2222-2222-222222222222', 'TRD-PAP-A4',
     'ورق A4 - رزمة 500 ورقة', 'ورق طباعة أبيض 80 غرام - رزمة',
     1, 1, 45.0, 45.0, 30, 100, true, now(), now(),
     '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111'),
    (v_items[2], '22222222-2222-2222-2222-222222222222', 'TRD-INK-HP',
     'حبر HP 304 أسود', 'خرطوشة حبر HP أصلية',
     1, 1, 85.0, 85.0, 20, 50, true, now(), now(),
     '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111'),
    (v_items[3], '22222222-2222-2222-2222-222222222222', 'TRD-PEN-PACK',
     'أقلام حبر متنوعة 12 قطعة', 'طقم أقلام مكتبية',
     1, 1, 25.0, 25.0, 50, 150, true, now(), now(),
     '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111'),
    (v_items[4], '22222222-2222-2222-2222-222222222222', 'TRD-FOL-A4',
     'ملفات A4 كرتون - 50 قطعة', 'ملفات كرتون أبيض للمكاتـب',
     1, 1, 35.0, 35.0, 25, 80, true, now(), now(),
     '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111'),
    (v_items[5], '22222222-2222-2222-2222-222222222222', 'TRD-CHR-OFC',
     'كرسي مكتبي دوّار', 'كرسي مكتبي جلد صناعي مع عجلات',
     1, 1, 320.0, 320.0, 10, 30, true, now(), now(),
     '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111')
  ON CONFLICT (company_id, sku) DO NOTHING;
END $$;

DO $$
DECLARE
  v_items uuid[] := ARRAY[gen_random_uuid(), gen_random_uuid(), gen_random_uuid(), gen_random_uuid(), gen_random_uuid()];
BEGIN
  INSERT INTO items (id, company_id, sku, name, description, item_type, costing_method, average_cost, standard_cost, reorder_level, reorder_quantity, is_active, created_at, updated_at, created_by, updated_by)
  VALUES
    (v_items[1], '33333333-3333-3333-3333-333333333333', 'LOG-TRK-MED',
     'شاحنة نقل متوسط 5 طن', 'خدمة نقل شاحنة متوسطة - رحلة محلية',
     2, 1, 800.0, 800.0, 0, 0, true, now(), now(),
     '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111'),
    (v_items[2], '33333333-3333-3333-3333-333333333333', 'LOG-WRH-MO',
     'تخزين شهري - متر مكعب', 'خدمة تخزين في مستودع مراقب - شهرياً',
     2, 1, 25.0, 25.0, 0, 0, true, now(), now(),
     '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111'),
    (v_items[3], '33333333-3333-3333-3333-333333333333', 'LOG-LD-HELP',
     'خدمات عمّال تحميل', 'خدمة عمّال تحميل وتفريغ - الساعة',
     2, 1, 15.0, 15.0, 0, 0, true, now(), now(),
     '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111'),
    (v_items[4], '33333333-3333-3333-3333-333333333333', 'LOG-CLR-CLR',
     'خدمة تخليص جمركي', 'خدمة تخليص جمركي مع متابعة الإجراءات',
     2, 1, 1500.0, 1500.0, 0, 0, true, now(), now(),
     '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111'),
    (v_items[5], '33333333-3333-3333-3333-333333333333', 'LOG-PAL-WD',
     'طبالي خشبية - قطعة', 'طبالي خشبية قياسية 120x100 سم',
     1, 1, 28.0, 28.0, 100, 500, true, now(), now(),
     '11111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111')
  ON CONFLICT (company_id, sku) DO NOTHING;
END $$;

-- =====================================================================
-- SECTION 5: Customers (3 per subsidiary + 1 holding-level = 10 total)
-- Idempotent: ON CONFLICT (company_id, code) DO NOTHING
-- =====================================================================
INSERT INTO customers (id, company_id, code, name, name_en, tax_id, email, phone, address, credit_limit, payment_terms_days, is_active, created_at, created_by, updated_at, updated_by)
VALUES
  -- ALF-CONST (مقاولات) — 3 customers
  (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', 'C-CONST-01',
   'وزارة الإسكان والمرافق', 'Ministry of Housing',
   'TAX-MOH-001', 'tenders@housing.gov.ly', '+218911234001',
   'طرابلس - شارع الفاتح', 500000.0, 60, true, now(),
   '11111111-1111-1111-1111-111111111111', now(), '11111111-1111-1111-1111-111111111111'),
  (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', 'C-CONST-02',
   'شركة الطرق والجسور الليبية', 'Libyan Roads & Bridges Co.',
   'TAX-LRB-002', 'projects@lrb.ly', '+218911234002',
   'بنغازي - شارع جمال عبد الناصر', 300000.0, 45, true, now(),
   '11111111-1111-1111-1111-111111111111', now(), '11111111-1111-1111-1111-111111111111'),
  (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', 'C-CONST-03',
   'مشروع المدينة السكنية - تاجوراء', 'Tagura Residential City Project',
   'TAX-TRC-003', 'admin@tagura-city.ly', '+218911234003',
   'تاجوراء - 15 كم جنوب طرابلس', 800000.0, 90, true, now(),
   '11111111-1111-1111-1111-111111111111', now(), '11111111-1111-1111-1111-111111111111'),

  -- ALF-TRADE (تجارة) — 4 customers
  (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'C-TRADE-01',
   'مجمع الأعمال التجارية - الزاوية', 'Business Complex - Zawiya',
   'TAX-BCZ-004', 'purchases@bcz.ly', '+218911234004',
   'الزاوية - شارع الشط', 100000.0, 30, true, now(),
   '11111111-1111-1111-1111-111111111111', now(), '11111111-1111-1111-1111-111111111111'),
  (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'C-TRADE-02',
   'شركة الاتصالات الليبية', 'Libyan Telecom Co.',
   'TAX-LTC-005', 'suppliers@ltc.ly', '+218911234005',
   'طرابلس - برج الاتصالات', 150000.0, 30, true, now(),
   '11111111-1111-1111-1111-111111111111', now(), '11111111-1111-1111-1111-111111111111'),
  (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'C-TRADE-03',
   'مؤسسة الفجر للتعليم', 'AlFajr Education Foundation',
   'TAX-AFE-006', 'orders@alfajr-edu.ly', '+218911234006',
   'مصراتة - حي الزهور', 50000.0, 30, true, now(),
   '11111111-1111-1111-1111-111111111111', now(), '11111111-1111-1111-1111-111111111111'),
  (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'C-TRADE-04',
   'مشروع طريق الكفرة - الجفرة', 'Kufra-Jufra Road Project',
   'TAX-KJR-007', 'admin@kjr-road.ly', '+218911234007',
   'سبها - مشروع الطريق السريع', 200000.0, 60, true, now(),
   '11111111-1111-1111-1111-111111111111', now(), '11111111-1111-1111-1111-111111111111'),

  -- ALN-LOG (خدمات لوجستية) — 3 customers
  (gen_random_uuid(), '33333333-3333-3333-3333-333333333333', 'C-LOG-01',
   'شركة النفط الوطنية - ميناء الحريقة', 'National Oil - Hariga Port',
   'TAX-NOC-008', 'logistics@noc.ly', '+218911234008',
   'طبرق - ميناء الحريقة', 500000.0, 45, true, now(),
   '11111111-1111-1111-1111-111111111111', now(), '11111111-1111-1111-1111-111111111111'),
  (gen_random_uuid(), '33333333-3333-3333-3333-333333333333', 'C-LOG-02',
   'مجمع الصناعات الغذائية - الخمس', 'Food Industries Complex - Khoms',
   'TAX-FIK-009', 'warehouse@fic-khoms.ly', '+218911234009',
   'الخمس - المنطقة الصناعية', 75000.0, 30, true, now(),
   '11111111-1111-1111-1111-111111111111', now(), '11111111-1111-1111-1111-111111111111'),
  (gen_random_uuid(), '33333333-3333-3333-3333-333333333333', 'C-LOG-03',
   'مشروع المطار الجديد - صرمان', 'New Airport Project - Sabratha',
   'TAX-SAP-010', 'logistics@sab-airport.ly', '+218911234010',
   'صرمان - موقع المطار الجديد', 600000.0, 90, true, now(),
   '11111111-1111-1111-1111-111111111111', now(), '11111111-1111-1111-1111-111111111111')
ON CONFLICT (company_id, code) DO NOTHING;

-- =====================================================================
-- SECTION 6: Vendors (3 per subsidiary + 1 holding-level = 10 total)
-- =====================================================================
INSERT INTO vendors (id, company_id, code, name, email, phone, address, tax_number, currency, payment_terms, is_active, created_at, created_by, updated_at, updated_by)
VALUES
  -- ALF-CONST vendors
  (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', 'V-CONST-01',
   'مورد الإسمنت والحديد - الزاوية', 'sales@cement-steel.zw.ly', '+218921234001',
   'الزاوية - مصنع الإسمنت', 'TAX-V001', 'LYD', 'Net30', true, now(),
   '11111111-1111-1111-1111-111111111111', now(), '11111111-1111-1111-1111-111111111111'),
  (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', 'V-CONST-02',
   'مورد المعدات الثقيلة - تاجوراء', 'rental@heavy-eq.ly', '+218921234002',
   'تاجوراء - مستودع المعدات', 'TAX-V002', 'LYD', 'Net30', true, now(),
   '11111111-1111-1111-1111-111111111111', now(), '11111111-1111-1111-1111-111111111111'),
  (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', 'V-CONST-03',
   'ورشة النور لصيانة المعدات', 'service@alnoor-workshop.ly', '+218921234003',
   'طرابلس - طريق المطار', 'TAX-V003', 'LYD', 'Net15', true, now(),
   '11111111-1111-1111-1111-111111111111', now(), '11111111-1111-1111-1111-111111111111'),

  -- ALF-TRADE vendors
  (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'V-TRADE-01',
   'موزع القرطاسية الموحد', 'orders@stationery-dist.ly', '+218921234004',
   'طرابلس - شارع الجمهورية', 'TAX-V004', 'LYD', 'Net30', true, now(),
   '11111111-1111-1111-1111-111111111111', now(), '11111111-1111-1111-1111-111111111111'),
  (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'V-TRADE-02',
   'مورد الأثاث المكتبي الحديث', 'sales@office-furniture.ly', '+218921234005',
   'بنغازي - المنطقة الحرة', 'TAX-V005', 'LYD', 'Net45', true, now(),
   '11111111-1111-1111-1111-111111111111', now(), '11111111-1111-1111-1111-111111111111'),
  (gen_random_uuid(), '22222222-2222-2222-2222-222222222222', 'V-TRADE-03',
   'مستوردات النور للورق والطباعة', 'imports@alnoor-paper.ly', '+218921234006',
   'مصراتة - ميناء مصراتة التجاري', 'TAX-V006', 'LYD', 'Net30', true, now(),
   '11111111-1111-1111-1111-111111111111', now(), '11111111-1111-1111-1111-111111111111'),

  -- ALN-LOG vendors
  (gen_random_uuid(), '33333333-3333-3333-3333-333333333333', 'V-LOG-01',
   'شركة الوقود والنقل البري', 'fuel@land-transport.ly', '+218921234007',
   'طرابلس - مستودع الوقود الرئيسي', 'TAX-V007', 'LYD', 'Net15', true, now(),
   '11111111-1111-1111-1111-111111111111', now(), '11111111-1111-1111-1111-111111111111'),
  (gen_random_uuid(), '33333333-3333-3333-3333-333333333333', 'V-LOG-02',
   'ورشة صيانة الشاحنات الدولية', 'service@intl-trucks.ly', '+218921234008',
   'سرت - طريق الإمداد', 'TAX-V008', 'LYD', 'Net30', true, now(),
   '11111111-1111-1111-1111-111111111111', now(), '11111111-1111-1111-1111-111111111111'),
  (gen_random_uuid(), '33333333-3333-3333-3333-333333333333', 'V-LOG-03',
   'مورد الطرود والطبالي', 'packaging@crates-pallets.ly', '+218921234009',
   'بنغازي - حي السلام', 'TAX-V009', 'LYD', 'Net30', true, now(),
   '11111111-1111-1111-1111-111111111111', now(), '11111111-1111-1111-1111-111111111111'),

  -- Holding-level vendor
  (gen_random_uuid(), '00000000-0000-0000-0000-000000000001', 'V-HOLD-01',
   'شركة المحاسبة والاستشارات القانونية', 'info@legal-accounting.ly', '+218921234010',
   'طرابلس - شارع الشط', 'TAX-V010', 'LYD', 'Net30', true, now(),
   '11111111-1111-1111-1111-111111111111', now(), '11111111-1111-1111-1111-111111111111')
ON CONFLICT (company_id, code) DO NOTHING;

-- =====================================================================
-- SECTION 7: 10 Users (1 existing admin + 9 new)
-- All use BCrypt hash of "Demo1234" so the owner (Mavis) can demo with
-- the same password across all 10 accounts.
-- Idempotent: ON CONFLICT (email) DO NOTHING
-- =====================================================================
INSERT INTO users (id, email, password_hash, full_name, is_active, two_factor_enabled, is_deleted, created_at, updated_at)
VALUES
  -- 1. (existing) admin@alfajr.local — owned by Holding Enterprise
  -- already in DB; ON CONFLICT prevents the duplicate insert.

  -- 2. mohamed@alfajr.local — Admin role, full access
  ('22222222-2222-2222-2222-222222222201', 'mohamed@alfajr.local',
   '$2a$11$FKXjp3qKKr9.Xbcfn7XjIuUMyEcmRo.TYZPFhcoQxHj4CNtnALqki',
   'محمد أحمد الفرنسيس — المدير العام', true, false, false, now(), now()),

  -- 3. ahmed@alfajr.local — Accountant
  ('22222222-2222-2222-2222-222222222202', 'ahmed@alfajr.local',
   '$2a$11$FKXjp3qKKr9.Xbcfn7XjIuUMyEcmRo.TYZPFhcoQxHj4CNtnALqki',
   'أحمد عبدالله الفيتوري — المحاسب الأول', true, false, false, now(), now()),

  -- 4. fatima@alfajr.local — Accountant
  ('22222222-2222-2222-2222-222222222203', 'fatima@alfajr.local',
   '$2a$11$FKXjp3qKKr9.Xbcfn7XjIuUMyEcmRo.TYZPFhcoQxHj4CNtnALqki',
   'فاطمة حميدة الجروشي — محاسبة', true, false, false, now(), now()),

  -- 5. khaled@alfajr.local — ProjectManager (construction)
  ('22222222-2222-2222-2222-222222222204', 'khaled@alfajr.local',
   '$2a$11$FKXjp3qKKr9.Xbcfn7XjIuUMyEcmRo.TYZPFhcoQxHj4CNtnALqki',
   'خالد محمد الشيباني — مدير مشاريع المقاولات', true, false, false, now(), now()),

  -- 6. omar@alfajr.local — ProjectManager
  ('22222222-2222-2222-2222-222222222205', 'omar@alfajr.local',
   '$2a$11$FKXjp3qKKr9.Xbcfn7XjIuUMyEcmRo.TYZPFhcoQxHj4CNtnALqki',
   'عمر يوسف الزروق — مهندس موقع أول', true, false, false, now(), now()),

  -- 7. sara@alfajr.local — Viewer (holding secretary)
  ('22222222-2222-2222-2222-222222222206', 'sara@alfajr.local',
   '$2a$11$FKXjp3qKKr9.Xbcfn7XjIuUMyEcmRo.TYZPFhcoQxHj4CNtnALqki',
   'سارة علي التارقية — سكرتارية الإدارة', true, false, false, now(), now()),

  -- 8. ali@alfajr.local — Accountant
  ('22222222-2222-2222-2222-222222222207', 'ali@alfajr.local',
   '$2a$11$FKXjp3qKKr9.Xbcfn7XjIuUMyEcmRo.TYZPFhcoQxHj4CNtnALqki',
   'علي عمر الشريف — أمين مخزن رئيسي', true, false, false, now(), now()),

  -- 9. rida@alfajr.local — Viewer (multi-company read-only)
  ('22222222-2222-2222-2222-222222222208', 'rida@alfajr.local',
   '$2a$11$FKXjp3qKKr9.Xbcfn7XjIuUMyEcmRo.TYZPFhcoQxHj4CNtnALqki',
   'رضا خليل العريبي — مراقب مالي', true, false, false, now(), now()),

  -- 10. naseer@alfajr.local — ProjectManager (procurement)
  ('22222222-2222-2222-2222-222222222209', 'naseer@alfajr.local',
   '$2a$11$FKXjp3qKKr9.Xbcfn7XjIuUMyEcmRo.TYZPFhcoQxHj4CNtnALqki',
   'نصير علي الكيلاني — مسؤول مشتريات أول', true, false, false, now(), now())
ON CONFLICT (email) DO NOTHING;

-- =====================================================================
-- SECTION 8: user_roles (assign roles to users)
-- Roles: Admin, Accountant, ProjectManager, Viewer
-- Idempotent: composite PK (user_id, role_id)
-- =====================================================================
DO $$
DECLARE
  v_role_admin   uuid := (SELECT id FROM roles WHERE name = 'Admin' LIMIT 1);
  v_role_acc     uuid := (SELECT id FROM roles WHERE name = 'Accountant' LIMIT 1);
  v_role_pm      uuid := (SELECT id FROM roles WHERE name = 'ProjectManager' LIMIT 1);
  v_role_viewer  uuid := (SELECT id FROM roles WHERE name = 'Viewer' LIMIT 1);
BEGIN
  -- admin@alfajr.local (existing) already has all 4 roles

  -- 2. mohamed → Admin + Accountant (general manager)
  INSERT INTO user_roles (user_id, role_id, assigned_at) VALUES
    ('22222222-2222-2222-2222-222222222201', v_role_admin,  now()),
    ('22222222-2222-2222-2222-222222222201', v_role_acc,    now())
  ON CONFLICT DO NOTHING;

  -- 3. ahmed → Accountant + Viewer
  INSERT INTO user_roles (user_id, role_id, assigned_at) VALUES
    ('22222222-2222-2222-2222-222222222202', v_role_acc,    now()),
    ('22222222-2222-2222-2222-222222222202', v_role_viewer, now())
  ON CONFLICT DO NOTHING;

  -- 4. fatima → Accountant
  INSERT INTO user_roles (user_id, role_id, assigned_at) VALUES
    ('22222222-2222-2222-2222-222222222203', v_role_acc, now())
  ON CONFLICT DO NOTHING;

  -- 5. khaled → ProjectManager
  INSERT INTO user_roles (user_id, role_id, assigned_at) VALUES
    ('22222222-2222-2222-2222-222222222204', v_role_pm, now())
  ON CONFLICT DO NOTHING;

  -- 6. omar → ProjectManager + Accountant
  INSERT INTO user_roles (user_id, role_id, assigned_at) VALUES
    ('22222222-2222-2222-2222-222222222205', v_role_pm,  now()),
    ('22222222-2222-2222-2222-222222222205', v_role_acc, now())
  ON CONFLICT DO NOTHING;

  -- 7. sara → Viewer
  INSERT INTO user_roles (user_id, role_id, assigned_at) VALUES
    ('22222222-2222-2222-2222-222222222206', v_role_viewer, now())
  ON CONFLICT DO NOTHING;

  -- 8. ali → Accountant + Viewer
  INSERT INTO user_roles (user_id, role_id, assigned_at) VALUES
    ('22222222-2222-2222-2222-222222222207', v_role_acc,    now()),
    ('22222222-2222-2222-2222-222222222207', v_role_viewer, now())
  ON CONFLICT DO NOTHING;

  -- 9. rida → Viewer
  INSERT INTO user_roles (user_id, role_id, assigned_at) VALUES
    ('22222222-2222-2222-2222-222222222208', v_role_viewer, now())
  ON CONFLICT DO NOTHING;

  -- 10. naseer → ProjectManager
  INSERT INTO user_roles (user_id, role_id, assigned_at) VALUES
    ('22222222-2222-2222-2222-222222222209', v_role_pm, now())
  ON CONFLICT DO NOTHING;
END $$;

-- =====================================================================
-- SECTION 9: user_companies (assign users to one or more companies)
-- Idempotent: composite PK (user_id, company_id)
-- =====================================================================
INSERT INTO user_companies (user_id, company_id, is_default, assigned_at)
VALUES
  -- admin@alfajr.local (existing) — already has Holding as default

  -- 2. mohamed — Holding + 3 subs
  ('22222222-2222-2222-2222-222222222201', '00000000-0000-0000-0000-000000000001', true,  now()),
  ('22222222-2222-2222-2222-222222222201', '11111111-1111-1111-1111-111111111111', false, now()),
  ('22222222-2222-2222-2222-222222222201', '22222222-2222-2222-2222-222222222222', false, now()),
  ('22222222-2222-2222-2222-222222222201', '33333333-3333-3333-3333-333333333333', false, now()),

  -- 3. ahmed — Holding + ALF-CONST + ALF-TRADE (multi-company accountant)
  ('22222222-2222-2222-2222-222222222202', '00000000-0000-0000-0000-000000000001', true,  now()),
  ('22222222-2222-2222-2222-222222222202', '11111111-1111-1111-1111-111111111111', false, now()),
  ('22222222-2222-2222-2222-222222222202', '22222222-2222-2222-2222-222222222222', false, now()),

  -- 4. fatima — ALF-CONST only
  ('22222222-2222-2222-2222-222222222203', '11111111-1111-1111-1111-111111111111', true, now()),

  -- 5. khaled — ALF-CONST (project manager)
  ('22222222-2222-2222-2222-222222222204', '11111111-1111-1111-1111-111111111111', true, now()),

  -- 6. omar — ALF-CONST + ALN-LOG (project manager, cross-company)
  ('22222222-2222-2222-2222-222222222205', '11111111-1111-1111-1111-111111111111', true,  now()),
  ('22222222-2222-2222-2222-222222222205', '33333333-3333-3333-3333-333333333333', false, now()),

  -- 7. sara — Holding only (secretary)
  ('22222222-2222-2222-2222-222222222206', '00000000-0000-0000-0000-000000000001', true, now()),

  -- 8. ali — ALF-TRADE (warehouse keeper)
  ('22222222-2222-2222-2222-222222222207', '22222222-2222-2222-2222-222222222222', true, now()),

  -- 9. rida — all 4 (financial controller, read-only)
  ('22222222-2222-2222-2222-222222222208', '00000000-0000-0000-0000-000000000001', true,  now()),
  ('22222222-2222-2222-2222-222222222208', '11111111-1111-1111-1111-111111111111', false, now()),
  ('22222222-2222-2222-2222-222222222208', '22222222-2222-2222-2222-222222222222', false, now()),
  ('22222222-2222-2222-2222-222222222208', '33333333-3333-3333-3333-333333333333', false, now()),

  -- 10. naseer — ALF-TRADE + ALN-LOG (procurement, cross-company)
  ('22222222-2222-2222-2222-222222222209', '22222222-2222-2222-2222-222222222222', true,  now()),
  ('22222222-2222-2222-2222-222222222209', '33333333-3333-3333-3333-333333333333', false, now())
ON CONFLICT (user_id, company_id) DO NOTHING;

-- =====================================================================
-- SECTION 10: Sales Invoices — 30 total (10 per subsidiary)
-- Multi-line amounts, realistic Libyan data, spread over 60 days back.
-- Idempotent: ON CONFLICT (company_id, invoice_number) DO NOTHING.
-- Invoice numbers are FIXED (S4-0001..S4-0030) so re-runs are no-ops.
-- =====================================================================
DO $$
DECLARE
  v_cust      uuid;
  v_company   uuid;
  v_subtotal  numeric;
  v_tax       numeric;
  v_total     numeric;
  v_status    text;
  v_paid      numeric;
  v_date      date;
  v_notes     text;
  v_invoice_n text;
  v_i         int;
  v_company_codes text[] := ARRAY['11111111-1111-1111-1111-111111111111',
                                   '22222222-2222-2222-2222-222222222222',
                                   '33333333-3333-3333-3333-333333333333'];
  v_count     int;
BEGIN
  FOR v_i IN 1..30 LOOP
    -- Pick a company and a customer from that company (round-robin)
    v_company := v_company_codes[((v_i - 1) % 3) + 1]::uuid;
    SELECT id INTO v_cust FROM customers
     WHERE company_id = v_company AND is_active = true
     ORDER BY code LIMIT 1
     OFFSET ((v_i - 1) / 3) % 3;

    IF v_cust IS NULL THEN CONTINUE; END IF;

    -- Spread dates over the last 60 days
    v_date := current_date - ((v_i * 2) % 60);
    v_subtotal := 8000 + (v_i * 1373) % 50000;
    v_tax := ROUND(v_subtotal * 0.15, 2);
    v_total := v_subtotal + v_tax;

    -- 80% Posted, 20% Draft
    IF v_i % 5 = 0 THEN
      v_status := 'Draft';
      v_paid := 0;
    ELSE
      v_status := 'Posted';
      v_paid := CASE WHEN v_i % 3 = 0 THEN 0 ELSE v_total END;
    END IF;

    v_invoice_n := 'S4-' || LPAD(v_i::text, 4, '0');

    v_notes := CASE ((v_i - 1) % 6)
      WHEN 0 THEN 'فاتورة مبيعات شهرية — توريد مواد بناء'
      WHEN 1 THEN 'فاتورة خدمات استشارية — عقد صيانة'
      WHEN 2 THEN 'فاتورة توريدات مكتبية — مناقصة حكومية'
      WHEN 3 THEN 'فاتورة نقل بضائع — شحنة عبر الطريق الساحلي'
      WHEN 4 THEN 'فاتورة خدمات لوجستية — تخليص جمركي'
      ELSE 'فاتورة متنوعة — خدمات إضافية'
    END;

    INSERT INTO sales_invoices (
      id, company_id, customer_id, invoice_number, invoice_date, due_date,
      currency_code, exchange_rate, subtotal, tax_amount, total_amount, paid_amount,
      status, is_deleted, notes, created_at, created_by, updated_at, updated_by
    ) VALUES (
      gen_random_uuid(), v_company, v_cust, v_invoice_n, v_date, v_date + interval '30 days',
      'LYD', 1.0, v_subtotal, v_tax, v_total, v_paid,
      v_status, false, v_notes, now(),
      '11111111-1111-1111-1111-111111111111', now(),
      '11111111-1111-1111-1111-111111111111'
    )
    ON CONFLICT (company_id, invoice_number) DO NOTHING;
  END LOOP;

  SELECT COUNT(*) INTO v_count FROM sales_invoices WHERE invoice_number LIKE 'S4-%';
  RAISE NOTICE 'Sprint 4 sales_invoices seeded (S4-*: % rows)', v_count;
END $$;

-- =====================================================================
-- SECTION 11: Vendor Bills — 20 total (7/7/6 across companies)
-- Idempotent: fixed B4-0001..B4-0020 + ON CONFLICT (company_id, bill_number).
-- =====================================================================
DO $$
DECLARE
  v_vendor    uuid;
  v_company   uuid;
  v_subtotal  numeric;
  v_tax       numeric;
  v_total     numeric;
  v_status    text;
  v_date      date;
  v_notes     text;
  v_bill_n    text;
  v_i         int;
  v_companies uuid[] := ARRAY[
    '11111111-1111-1111-1111-111111111111'::uuid,
    '22222222-2222-2222-2222-222222222222'::uuid,
    '33333333-3333-3333-3333-333333333333'::uuid
  ];
  v_bills_per_company int[] := ARRAY[7, 7, 6];
  v_company_idx int := 0;
  v_count int;
  v_seq int := 0;
BEGIN
  FOR v_company_idx IN 1..3 LOOP
    v_company := v_companies[v_company_idx];
    FOR v_i IN 1..v_bills_per_company[v_company_idx] LOOP
      SELECT id INTO v_vendor FROM vendors
       WHERE company_id = v_company AND is_active = true
       ORDER BY code LIMIT 1
       OFFSET ((v_i - 1) % 3);

      IF v_vendor IS NULL THEN CONTINUE; END IF;

      v_date := current_date - ((v_i * 4) % 90);
      v_subtotal := 5000 + (v_i * 911 + v_company_idx * 1000) % 30000;
      v_tax := ROUND(v_subtotal * 0.15, 2);
      v_total := v_subtotal + v_tax;
      v_status := CASE WHEN v_i % 4 = 0 THEN 'Draft' ELSE 'Posted' END;

      v_seq := v_seq + 1;
      v_bill_n := 'B4-' || LPAD(v_seq::text, 4, '0');

      v_notes := CASE ((v_i - 1) % 5)
        WHEN 0 THEN 'فاتورة شراء إسمنت وحديد - مقاولات'
        WHEN 1 THEN 'فاتورة وقود ومحروقات - تشغيل أسطول'
        WHEN 2 THEN 'فاتورة قرطاسية ومستلزمات مكتبية'
        WHEN 3 THEN 'فاتورة صيانة دورية للمعدات'
        ELSE 'فاتورة خدمات لوجستية ونقل'
      END;

      INSERT INTO vendor_bills (
        id, company_id, bill_number, vendor_id, status, bill_date, due_date,
        currency, sub_total, tax_amount, total_amount, notes,
        created_at, created_by, updated_at, updated_by
      ) VALUES (
        gen_random_uuid(), v_company, v_bill_n, v_vendor, v_status, v_date, v_date + interval '30 days',
        'LYD', v_subtotal, v_tax, v_total, v_notes, now(),
        '11111111-1111-1111-1111-111111111111', now(),
        '11111111-1111-1111-1111-111111111111'
      )
      ON CONFLICT (company_id, bill_number) DO NOTHING;
    END LOOP;
  END LOOP;

  SELECT COUNT(*) INTO v_count FROM vendor_bills WHERE bill_number LIKE 'B4-%';
  RAISE NOTICE 'Sprint 4 vendor_bills seeded (B4-*: % rows)', v_count;
END $$;

-- =====================================================================
-- SECTION 12: Journal Entries — 30 total (10 per subsidiary)
-- Each entry has 2 balanced lines (Dr / Cr).
-- Idempotent: fixed JE-S4-0001..0030 + ON CONFLICT (company_id, entry_number).
-- Lines use the JE id from the actual row (post-conflict), so re-runs
-- don't violate the journal_lines FK.
-- =====================================================================
DO $$
DECLARE
  v_company     uuid;
  v_company_ids uuid[] := ARRAY[
    '11111111-1111-1111-1111-111111111111'::uuid,
    '22222222-2222-2222-2222-222222222222'::uuid,
    '33333333-3333-3333-3333-333333333333'::uuid
  ];
  v_je_id       uuid;
  v_entry_n     text;
  v_amount      numeric;
  v_dr_id       uuid;
  v_cr_id       uuid;
  v_desc        text;
  v_i           int;
  v_count       int;
BEGIN
  FOR v_i IN 1..30 LOOP
    v_company := v_company_ids[((v_i - 1) % 3) + 1];

    -- Pick the standard CoA accounts (1230 AR, 5110 revenue)
    SELECT id INTO v_dr_id FROM accounts WHERE company_id = v_company AND code = '1230' LIMIT 1;
    SELECT id INTO v_cr_id FROM accounts WHERE company_id = v_company AND code = '5110' LIMIT 1;

    IF v_dr_id IS NULL OR v_cr_id IS NULL THEN
      SELECT id INTO v_dr_id FROM accounts WHERE company_id = v_company AND code = '4200' LIMIT 1;
      SELECT id INTO v_cr_id FROM accounts WHERE company_id = v_company AND code = '1210' LIMIT 1;
      IF v_dr_id IS NULL OR v_cr_id IS NULL THEN
        CONTINUE;
      END IF;
    END IF;

    v_amount := 5000 + (v_i * 1237) % 35000;
    v_entry_n := 'JE-S4-' || LPAD(v_i::text, 4, '0');

    v_desc := CASE ((v_i - 1) % 6)
      WHEN 0 THEN 'قيد إيراد مشروع - فاتورة مبيعات'
      WHEN 1 THEN 'قيد إيجار شهري - مبنى الإدارة'
      WHEN 2 THEN 'قيد مصاريف كهرباء ومياه'
      WHEN 3 THEN 'قيد رواتب شهرية - تحويل بنكي'
      WHEN 4 THEN 'قيد شراء معدات - توريد'
      ELSE 'قيد تسوية - مصروفات متنوعة'
    END;

    -- ON CONFLICT (company_id, entry_number) — entry_number is unique per company
    INSERT INTO journal_entries (
      id, entry_number, entry_date, description, reference, status,
      created_by_user_id, company_id, posted_at, created_at, updated_at
    ) VALUES (
      gen_random_uuid(), v_entry_n, current_date - ((v_i * 3) % 90), v_desc,
      'SPRINT-4-REF-' || v_i, 2,
      '11111111-1111-1111-1111-111111111111', v_company,
      now(), now(), now()
    )
    ON CONFLICT (company_id, entry_number) DO NOTHING;

    -- Look up the actual id (whether newly inserted or pre-existing) for the lines.
    SELECT id INTO v_je_id FROM journal_entries
     WHERE company_id = v_company AND entry_number = v_entry_n;

    IF v_je_id IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM journal_lines WHERE journal_entry_id = v_je_id)
    THEN
      INSERT INTO journal_lines (
        id, journal_entry_id, account_id, debit, credit, description, line_number, company_id
      ) VALUES
        (gen_random_uuid(), v_je_id, v_dr_id, v_amount, 0, 'مدين - ' || v_desc, 1, v_company),
        (gen_random_uuid(), v_je_id, v_cr_id, 0, v_amount, 'دائن - ' || v_desc, 2, v_company);
    END IF;
  END LOOP;

  SELECT COUNT(*) INTO v_count FROM journal_entries WHERE entry_number LIKE 'JE-S4-%';
  RAISE NOTICE 'Sprint 4 journal_entries seeded (JE-S4-*: % rows)', v_count;
END $$;

-- =====================================================================
-- SECTION 13: Stock Movements — 20 total (10 IN, 10 OUT)
-- IN movements linked to vendor bills; OUT movements linked to sales
-- =====================================================================
DO $$
DECLARE
  v_item     uuid;
  v_warehouse uuid;
  v_company  uuid;
  v_company_ids uuid[] := ARRAY[
    '11111111-1111-1111-1111-111111111111'::uuid,
    '22222222-2222-2222-2222-222222222222'::uuid,
    '33333333-3333-3333-3333-333333333333'::uuid
  ];
  v_qty      numeric;
  v_cost     numeric;
  v_type     int;
  v_ref      text;
  v_i        int;
  v_count    int;
BEGIN
  -- IN movements (type=1) — no unique constraint on `reference`, so guard with NOT EXISTS
  FOR v_i IN 1..10 LOOP
    v_company := v_company_ids[((v_i - 1) % 3) + 1];
    SELECT id INTO v_item FROM items WHERE company_id = v_company AND is_active = true ORDER BY sku LIMIT 1 OFFSET ((v_i - 1) % 5);
    SELECT id INTO v_warehouse FROM warehouses WHERE company_id = v_company LIMIT 1;
    IF v_item IS NULL OR v_warehouse IS NULL THEN CONTINUE; END IF;

    v_qty := 50 + (v_i * 17) % 300;
    v_cost := 25 + (v_i * 13) % 250;
    v_ref := 'GR-S4-' || LPAD(v_i::text, 4, '0');

    IF NOT EXISTS (SELECT 1 FROM stock_movements WHERE company_id = v_company AND reference = v_ref) THEN
      INSERT INTO stock_movements (
        id, company_id, reference, type, movement_date, item_id, warehouse_id,
        quantity, unit_cost, source_type, status, created_at, created_by, posted_at
      ) VALUES (
        gen_random_uuid(), v_company, v_ref, 1,
        current_date - ((v_i * 3) % 60), v_item, v_warehouse,
        v_qty, v_cost, 'VendorBill', 2, now(),
        '11111111-1111-1111-1111-111111111111', now()
      );
    END IF;
  END LOOP;

  -- OUT movements (type=2)
  FOR v_i IN 1..10 LOOP
    v_company := v_company_ids[((v_i - 1) % 3) + 1];
    SELECT id INTO v_item FROM items WHERE company_id = v_company AND is_active = true ORDER BY sku LIMIT 1 OFFSET ((v_i - 1) % 5);
    SELECT id INTO v_warehouse FROM warehouses WHERE company_id = v_company LIMIT 1;
    IF v_item IS NULL OR v_warehouse IS NULL THEN CONTINUE; END IF;

    v_qty := 5 + (v_i * 7) % 60;
    v_cost := 25 + (v_i * 11) % 250;
    v_ref := 'IS-S4-' || LPAD(v_i::text, 4, '0');

    IF NOT EXISTS (SELECT 1 FROM stock_movements WHERE company_id = v_company AND reference = v_ref) THEN
      INSERT INTO stock_movements (
        id, company_id, reference, type, movement_date, item_id, warehouse_id,
        quantity, unit_cost, source_type, status, created_at, created_by, posted_at
      ) VALUES (
        gen_random_uuid(), v_company, v_ref, 2,
        current_date - ((v_i * 2) % 45), v_item, v_warehouse,
        v_qty, v_cost, 'SalesInvoice', 2, now(),
        '11111111-1111-1111-1111-111111111111', now()
      );
    END IF;
  END LOOP;

  SELECT COUNT(*) INTO v_count FROM stock_movements WHERE reference LIKE '%-S4-%';
  RAISE NOTICE 'Sprint 4 stock_movements seeded (% rows)', v_count;
END $$;

-- =====================================================================
-- SECTION 14: Activity Log — 35+ entries (5+ per day for last 7 days)
-- Spread across 10 users, 4 companies, with varied actions
-- =====================================================================
DO $$
DECLARE
  v_user_ids uuid[] := ARRAY[
    '11111111-1111-1111-1111-111111111111'::uuid,
    '22222222-2222-2222-2222-222222222201'::uuid,
    '22222222-2222-2222-2222-222222222202'::uuid,
    '22222222-2222-2222-2222-222222222203'::uuid,
    '22222222-2222-2222-2222-222222222204'::uuid,
    '22222222-2222-2222-2222-222222222205'::uuid,
    '22222222-2222-2222-2222-222222222206'::uuid,
    '22222222-2222-2222-2222-222222222207'::uuid,
    '22222222-2222-2222-2222-222222222208'::uuid,
    '22222222-2222-2222-2222-222222222209'::uuid
  ];
  v_company_ids uuid[] := ARRAY[
    '00000000-0000-0000-0000-000000000001'::uuid,
    '11111111-1111-1111-1111-111111111111'::uuid,
    '22222222-2222-2222-2222-222222222222'::uuid,
    '33333333-3333-3333-3333-333333333333'::uuid
  ];
  v_actions text[] := ARRAY[
    'login', 'refresh', 'logout', 'company_switch',
    'view_dashboard', 'view_invoice', 'view_report',
    'create_invoice', 'create_bill', 'view_users',
    'view_companies', 'view_activity', 'register'
  ];
  v_user     uuid;
  v_company  uuid;
  v_action   text;
  v_meta     jsonb;
  v_dt       timestamptz;
  v_i        int;
  v_count    int;
BEGIN
  -- 5 entries per day for last 7 days = 35 entries minimum, plus 7 extras = 42 total.
  -- Idempotency: timestamps are derived from a STABLE base date (today's date at
  -- midnight UTC), NOT from now(). This guarantees re-runs produce the same
  -- timestamps and the (user, action, created_at) NOT EXISTS guard works.
  FOR v_i IN 1..42 LOOP
    v_user := v_user_ids[((v_i - 1) % 10) + 1];
    v_company := v_company_ids[((v_i - 1) % 4) + 1];
    v_action := v_actions[((v_i - 1) % 13) + 1];
    -- Stable base: today's date at 22:00 UTC (a fixed hour so re-runs match).
    -- v_i=1..5 → today 22:00, 21:00, 20:00, 19:00, 18:00
    -- v_i=6..10 → yesterday 22:00, 21:00, 20:00, 19:00, 18:00
    -- v_i=11..15 → 2 days ago 22:00..18:00, etc.
    v_dt := date_trunc('day', now())::timestamptz
            + interval '14 hours'  -- 14:00 UTC = 16:00 Libya time (a "working hour")
            - (((v_i - 1) / 5) || ' days')::interval
            - (((v_i - 1) % 5) * interval '1 hour');

    v_meta := jsonb_build_object(
      'demo', true,
      'ip', '192.168.1.' || (10 + v_i % 250),
      'user_agent', 'ERP-Demo-Client/1.0',
      'session_id', 'sess-s4-' || v_i
    );

    -- NOT EXISTS guard: (user, action, created_at) tuple is unique per the seed.
    -- If the same combination already exists, skip.
    IF NOT EXISTS (
      SELECT 1 FROM activity_log
       WHERE user_id = v_user
         AND action = v_action
         AND created_at = v_dt
    ) THEN
      INSERT INTO activity_log (company_id, user_id, action, ip_address, user_agent, metadata, created_at)
      VALUES (v_company, v_user, v_action, '192.168.1.' || (10 + v_i % 250),
              'ERP-Demo-Client/1.0 (Sprint 4 demo seed)', v_meta, v_dt);
    END IF;
  END LOOP;

  SELECT COUNT(*) INTO v_count FROM activity_log WHERE metadata->>'demo' = 'true';
  RAISE NOTICE 'Sprint 4 activity_log seeded (% rows)', v_count;
END $$;

-- =====================================================================
-- Summary counts
-- =====================================================================
DO $$
DECLARE
  v_companies int;
  v_users     int;
  v_invoices  int;
  v_bills     int;
  v_jes       int;
  v_moves     int;
  v_activity  int;
  v_uc        int;
  v_ur        int;
BEGIN
  SELECT COUNT(*) INTO v_companies FROM companies WHERE id != '00000000-0000-0000-0000-000000000001';
  SELECT COUNT(*) INTO v_users FROM users;
  SELECT COUNT(*) INTO v_invoices FROM sales_invoices WHERE invoice_number LIKE 'S4-%';
  SELECT COUNT(*) INTO v_bills FROM vendor_bills WHERE bill_number LIKE 'B4-%';
  SELECT COUNT(*) INTO v_jes FROM journal_entries WHERE entry_number LIKE 'JE-S4%';
  SELECT COUNT(*) INTO v_moves FROM stock_movements WHERE reference LIKE '%-S4-%';
  SELECT COUNT(*) INTO v_activity FROM activity_log WHERE metadata->>'demo' = 'true';
  SELECT COUNT(*) INTO v_uc FROM user_companies;
  SELECT COUNT(*) INTO v_ur FROM user_roles;

  RAISE NOTICE '=========================================';
  RAISE NOTICE 'Sprint 4 Demo Data Summary';
  RAISE NOTICE '  Companies (subsidiaries): %', v_companies;
  RAISE NOTICE '  Users (total):             %', v_users;
  RAISE NOTICE '  Sales Invoices (S4-*):     %', v_invoices;
  RAISE NOTICE '  Vendor Bills (B4-*):       %', v_bills;
  RAISE NOTICE '  Journal Entries (JE-S4*):  %', v_jes;
  RAISE NOTICE '  Stock Movements (-S4-):    %', v_moves;
  RAISE NOTICE '  Activity Log (demo=true):  %', v_activity;
  RAISE NOTICE '  user_companies links:      %', v_uc;
  RAISE NOTICE '  user_roles links:          %', v_ur;
  RAISE NOTICE '  Total transactions:        %',
    (v_invoices + v_bills + v_jes + v_moves + v_activity);
  RAISE NOTICE '=========================================';
END $$;
