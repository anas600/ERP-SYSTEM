-- ============================================================
-- ERP System — Full Year Seed Data Generator
-- Idempotent: skips if data already exists
-- Company: AlFajr Holding Company (multi-company refactor)
-- ============================================================

\set ON_ERROR_STOP on

-- Get tenant and admin IDs
DO $$
DECLARE
  v_company_id  uuid := 'ec6b98ee-221c-410e-a690-192245314a68';
  v_admin_id   uuid := 'f61842d7-195b-4823-855f-ca4adb80f7ac';

  -- Account IDs
  v_acc_cash    uuid := '7f8124fd-8d20-404c-89c9-c05b0653cff5'; -- 1210 النقدية
  v_acc_bank    uuid := '9d1a8e44-2829-4006-8488-f14720d92301'; -- 1110 البنك
  v_acc_ar      uuid := '69056f40-928c-4137-a5e1-12be806de6fd'; -- 1230 ذمم مدينة
  v_acc_inv     uuid := '621bfd2d-2935-4a88-91a9-e42e187edcf3'; -- 1240 مخزون
  v_acc_ap      uuid := '916ba930-6ca3-4693-ab86-8a9a1dff8a52'; -- 2210 دائنون
  v_acc_eq      uuid := 'd6cf88c9-8e7e-44b4-9ed3-1ebf84e794a4'; -- 3100 رأس المال
  v_acc_admin   uuid := '5b1de03d-f5f3-454b-b8f2-d55b6aae6753'; -- 4200 مصروفات إدارية
  v_acc_salary  uuid := 'c8ee59e0-d530-4e7b-aa05-bbba4d40ff49'; -- 5500 مصروف الرواتب
  v_acc_rev_proj uuid := 'b769959f-67bf-4830-8c74-139ef93cfc08'; -- 5110 إيرادات المشاريع
  v_acc_vat_out uuid;
  v_acc_vat_in  uuid;

  v_count       int;
  v_year_start  date := '2025-08-01';
  v_now         timestamptz := now();
BEGIN
  -- Check if already seeded
  SELECT COUNT(*) INTO v_count FROM sales_invoices WHERE company_id = v_company_id;
  IF v_count > 5 THEN
    RAISE NOTICE 'Already seeded (% invoices). Skipping.', v_count;
    RETURN;
  END IF;

  -- VAT accounts
  INSERT INTO accounts (id, company_id, code, name, type, normal_balance, is_postable, is_active, created_at, updated_at)
  VALUES (gen_random_uuid(), v_company_id, '2250', 'ضريبة القيمة المضافة المستحقة (مبيعات)', 2, 2, true, true, v_now, v_now)
  RETURNING id INTO v_acc_vat_out;
  INSERT INTO accounts (id, company_id, code, name, type, normal_balance, is_postable, is_active, created_at, updated_at)
  VALUES (gen_random_uuid(), v_company_id, '1255', 'ضريبة مدفوعة مقدماً (مشتريات)', 1, 1, true, true, v_now, v_now)
  RETURNING id INTO v_acc_vat_in;

  RAISE NOTICE 'Seeding 12 months starting %', v_year_start;

  -- ====== DEPARTMENTS (additional 4) ======
  INSERT INTO departments (id, company_id, code, name, is_active, created_at, updated_at)
  VALUES
    (gen_random_uuid(), v_company_id, 'FIN', 'الإدارة المالية', true, v_now, v_now),
    (gen_random_uuid(), v_company_id, 'OPS', 'العمليات والمشاريع', true, v_now, v_now),
    (gen_random_uuid(), v_company_id, 'SALES', 'المبيعات', true, v_now, v_now),
    (gen_random_uuid(), v_company_id, 'TECH', 'تقنية المعلومات', true, v_now, v_now);

  -- ====== CUSTOMERS (15) ======
  INSERT INTO customers (id, company_id, code, name, name_en, email, phone, address, credit_limit, payment_terms_days, is_active, created_at, created_by, updated_at)
  SELECT gen_random_uuid(), v_company_id, v_company_id, c.code, c.name_ar, c.name_en,
    LOWER(REPLACE(c.name_en, ' ', '.')) || '@example.com',
    '+21891' || LPAD((1000000 + (c.i)*111)::text, 7, '0'),
    'طرابلس، ليبيا', (50000 + c.i*25000)::numeric, 30 + (c.i % 4) * 15,
    true, v_now, v_admin_id, v_now
  FROM (VALUES
    (0,  'C0001', 'شركة الإنشاءات المتحدة', 'United Construction Co'),
    (1,  'C0002', 'مؤسسة الفجر للمقاولات', 'AlFajr Contracting Est'),
    (2,  'C0003', 'شركة البنية التحتية', 'Infrastructure Co'),
    (3,  'C0004', 'مكتب الهندسة الحديث', 'Modern Engineering Office'),
    (4,  'C0005', 'شركة الإسكان الوطني', 'National Housing Co'),
    (5,  'C0006', 'مجمع الأعمال التجارية', 'Business Complex'),
    (6,  'C0007', 'شركة الطرق والجسور', 'Roads and Bridges Co'),
    (7,  'C0008', 'مشروع المطار الجديد', 'New Airport Project'),
    (8,  'C0009', 'شركة الكهرباء الليبية', 'Libyan Electricity Co'),
    (9,  'C0010', 'مؤسسة المياه والصرف', 'Water and Sanitation Est'),
    (10, 'C0011', 'شركة الاتصالات', 'Telecom Co'),
    (11, 'C0012', 'مشروع المدينة السكنية', 'Residential City Project'),
    (12, 'C0013', 'مجمع الجامعات', 'University Complex'),
    (13, 'C0014', 'شركة النفط الوطنية', 'National Oil Co'),
    (14, 'C0015', 'مشروع الطريق الساحلي', 'Coastal Road Project')
  ) AS c(i, code, name_ar, name_en);

  -- ====== VENDORS (additional 8) ======
  INSERT INTO vendors (id, company_id, code, name, email, phone, address, tax_number, payment_terms, is_active, created_at, created_by, updated_at)
  SELECT gen_random_uuid(), v_company_id, v.code, v.name_ar,
    LOWER(REPLACE(v.name_en, ' ', '.')) || '@vendor.com',
    '+21892' || LPAD((1000000 + v.i*222)::text, 7, '0'),
    'بنغازي، ليبيا', 'TAX-' || LPAD((1000+v.i)::text, 6, '0'),
    'Net 30', true, v_now, v_admin_id, v_now
  FROM (VALUES
    (0, 'V0010', 'مورد الأسمنت والحديد', 'Cement Steel Supplier'),
    (1, 'V0011', 'مورد المعدات الثقيلة', 'Heavy Equipment Supplier'),
    (2, 'V0012', 'مورد المواد الكهربائية', 'Electrical Supplier'),
    (3, 'V0013', 'مورد الأخشاب', 'Timber Supplier'),
    (4, 'V0014', 'مورد الأدوات الصحية', 'Plumbing Supplier'),
    (5, 'V0015', 'مورد الدهانات', 'Paint Supplier'),
    (6, 'V0016', 'مورد البلاط والسيراميك', 'Tiles Supplier'),
    (7, 'V0017', 'مورد الزجاج والألمنيوم', 'Glass Aluminum Supplier')
  ) AS v(i, code, name_ar, name_en);

  -- ====== ITEM CATEGORIES ======
  INSERT INTO item_categories (id, company_id, code, name, is_active, created_at, updated_at)
  VALUES
    (gen_random_uuid(), v_company_id, 'CAT-CON', 'مواد البناء', true, v_now, v_now),
    (gen_random_uuid(), v_company_id, 'CAT-ELE', 'المعدات الكهربائية', true, v_now, v_now),
    (gen_random_uuid(), v_company_id, 'CAT-PLB', 'الأدوات الصحية', true, v_now, v_now),
    (gen_random_uuid(), v_company_id, 'CAT-PNT', 'الدهانات', true, v_now, v_now),
    (gen_random_uuid(), v_company_id, 'CAT-HRD', 'الحديد والمعادن', true, v_now, v_now);

  -- ====== UNITS OF MEASURE ======
  INSERT INTO units_of_measure (id, company_id, code, name, symbol, is_active, created_at, updated_at)
  VALUES
    (gen_random_uuid(), v_company_id, 'EA', 'قطعة', 'EA', true, v_now, v_now),
    (gen_random_uuid(), v_company_id, 'KG', 'كيلوجرام', 'KG', true, v_now, v_now),
    (gen_random_uuid(), v_company_id, 'TON', 'طن', 'TON', true, v_now, v_now),
    (gen_random_uuid(), v_company_id, 'M', 'متر', 'M', true, v_now, v_now),
    (gen_random_uuid(), v_company_id, 'M2', 'متر مربع', 'M2', true, v_now, v_now),
    (gen_random_uuid(), v_company_id, 'M3', 'متر مكعب', 'M3', true, v_now, v_now);

  -- ====== WAREHOUSES (3 more) ======
  INSERT INTO warehouses (id, company_id, code, name, location, is_active, created_at, created_by, updated_at)
  SELECT gen_random_uuid(), v_company_id, v_company_id, w.code, w.name, w.loc, true, v_now, v_admin_id, v_now
  FROM (VALUES
    ('WH-A1', 'مستودع المشروع أ', 'طرابلس - المشروع أ'),
    ('WH-B1', 'مستودع المشروع ب', 'مصراتة'),
    ('WH-BNG', 'مستودع بنغازي', 'بنغازي')
  ) AS w(code, name, loc);

  -- ====== ITEMS (20) ======
  INSERT INTO items (id, company_id, sku, name, description, item_type, costing_method, average_cost, standard_cost, is_active, created_at, created_by, updated_at)
  SELECT gen_random_uuid(), v_company_id, v_company_id, 'SKU-' || LPAD((it.i+1)::text, 4, '0'),
    it.name_ar, 'صنف ' || it.name_en, 1, 1, it.cost, it.cost,
    true, v_now, v_admin_id, v_now
  FROM (VALUES
    (0,  'أسمنت بورتلاندي 50 كجم',  'Portland Cement 50kg',       25.0),
    (1,  'حديد تسليح 12 مم',         'Rebar 12mm',              1850.0),
    (2,  'حديد تسليح 16 مم',         'Rebar 16mm',              1850.0),
    (3,  'رمل بناء',                  'Building Sand',              80.0),
    (4,  'حصى (زلط)',                 'Gravel',                     90.0),
    (5,  'طوب أحمر',                  'Red Bricks',                  0.85),
    (6,  'بلاط سيراميك 30x30',        'Ceramic Tiles 30x30',        45.0),
    (7,  'ماسورة PPR 25 مم',          'PPR Pipe 25mm',              12.0),
    (8,  'كابل كهربائي 2.5 مم',       'Electric Cable 2.5mm',        8.5),
    (9,  'كابل كهربائي 4 مم',         'Electric Cable 4mm',         13.0),
    (10, 'لوحة كهربائية 12 خط',       'Electrical Panel 12-line',  350.0),
    (11, 'دهان أكريليك أبيض 20 لتر',  'Acrylic Paint White 20L',   180.0),
    (12, 'ورق جدران',                 'Wallpaper',                  35.0),
    (13, 'مسامير وصواميل (كجم)',      'Nuts Bolts kg',              15.0),
    (14, 'ألواح جبس بورد',            'Gypsum Board',               42.0),
    (15, 'سلك ربط 3 مم',              'Binding Wire 3mm',            6.0),
    (16, 'بخاخ رش دهان',              'Paint Spray Gun',           220.0),
    (17, 'خرطوم مياه 1 إنش',          'Water Hose 1 inch',          18.0),
    (18, 'مفتاح إضاءة',               'Light Switch',                4.5),
    (19, 'بطارية ليثيوم 100Ah',       'Lithium Battery 100Ah',    1200.0)
  ) AS it(i, name_ar, name_en, cost);

  -- ====== COST CENTERS (5 more) ======
  INSERT INTO cost_centers (id, company_id, code, name, type, budget_amount, start_date, end_date, is_active, created_at, updated_at)
  SELECT gen_random_uuid(), v_company_id, v_company_id, cc.code, cc.name, 1,
    (50000 + cc.i*30000)::numeric, v_year_start, v_year_start + interval '12 months',
    true, v_now, v_now
  FROM (VALUES
    (0, 'CC-VILLA-25', 'مشروع فيلا سكنية A'),
    (1, 'CC-MALL-25',  'مشروع مجمع تجاري B'),
    (2, 'CC-ROAD-25',  'مشروع طريق ساحلي C'),
    (3, 'CC-SCH-25',   'مشروع مدرسة D'),
    (4, 'CC-WH-25',    'مشروع مستودع E')
  ) AS cc(i, code, name);

  -- ====== PROJECTS (3 active) ======
  INSERT INTO projects (id, company_id, cost_center_id, code, name, description, status, budget, start_date, end_date, is_active, created_at, created_by, updated_at)
  SELECT gen_random_uuid(), v_company_id, v_company_id,
    (SELECT id FROM cost_centers WHERE company_id = v_company_id AND code = p.cc_code LIMIT 1),
    p.code, p.name, p.descr, 1, p.budget,
    v_year_start, v_year_start + interval '12 months', true, v_now, v_admin_id, v_now
  FROM (VALUES
    ('CC-VILLA-25', 'PRJ-2025-01', 'فيلا سكنية - حي الأندلس', 'بناء فيلا سكنية 3 طوابق', 850000.0),
    ('CC-MALL-25',  'PRJ-2025-02', 'مجمع تجاري - شارع الجمهورية', 'بناء 12 محل تجاري', 1200000.0),
    ('CC-ROAD-25',  'PRJ-2025-03', 'طريق ساحلي - km 50-65', 'تعبيد وتجهيز 15 كم', 2500000.0)
  ) AS p(cc_code, code, name, descr, budget);

  RAISE NOTICE 'Phase 1: Reference data DONE';
END $$;
