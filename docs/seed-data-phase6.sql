-- ============================================================
-- ERP System — Phase 6 (Multi-Company) Seed Data
-- Idempotent: skips if data already exists
-- Compatible with the new company_id architecture (no tenant_id)
-- ============================================================

\set ON_ERROR_STOP on

DO $$
DECLARE
  v_count int;
  v_holding_id uuid := 'ec6b98ee-221c-410e-a690-192245314a68';
  v_company_a  uuid := '11111111-1111-1111-1111-111111111111';
  v_company_b  uuid := '22222222-2222-2222-2222-222222222222';
  v_now timestamptz := now();
  v_year_start date := '2025-08-01';
  v_i int;
  v_j int;
  v_dt date;
  v_amount numeric;
  v_je_id uuid;
BEGIN
  -- Skip if data already exists (using company_id on accounts)
  SELECT COUNT(*) INTO v_count FROM accounts WHERE company_id = v_holding_id;
  IF v_count > 10 THEN
    RAISE NOTICE 'Data already seeded for company_id=%. Skipping.', v_holding_id;
    RETURN;
  END IF;

  -- ====================================================
  -- 1. Create additional Subsidiary companies
  -- ====================================================
  INSERT INTO companies (id, name, code, is_group, parent_company_id, is_active, created_at, updated_at)
  VALUES
    (v_company_a, 'شركة الفجر للمقاولات', 'ALF-CONST', false, v_holding_id, true, v_now, v_now),
    (v_company_b, 'شركة الفجر للتجارة', 'ALF-TRADE', false, v_holding_id, true, v_now, v_now)
  ON CONFLICT (id) DO NOTHING;

  -- ====================================================
  -- 2. Chart of Accounts (per company)
  -- ====================================================
  INSERT INTO accounts (id, company_id, code, name, type, normal_balance, is_postable, is_active, created_at, updated_at)
  SELECT gen_random_uuid(), v_holding_id,
    c.code, c.name, c.type, c.normal_balance, true, true, v_now, v_now
  FROM (VALUES
    ('1000', 'الأصول', 1, 1),
    ('1100', 'الأصول غير المتداولة', 1, 1),
    ('1110', 'البنك', 1, 1),
    ('1200', 'الأصول المتداولة', 1, 1),
    ('1210', 'النقدية', 1, 1),
    ('1230', 'ذمم مدينة (عملاء)', 1, 1),
    ('1240', 'مخزون', 1, 1),
    ('1255', 'ضريبة مدفوعة مقدماً', 1, 1),
    ('2000', 'الالتزامات', 2, 2),
    ('2210', 'دائنون لموردين', 2, 2),
    ('2250', 'ضريبة مخرجات', 2, 2),
    ('3000', 'حقوق الملكية', 3, 2),
    ('3100', 'رأس المال', 3, 2),
    ('3200', 'أرباح محتجزة', 3, 2),
    ('4000', 'المصروفات', 5, 1),
    ('4200', 'مصروفات إدارية', 5, 1),
    ('5110', 'إيرادات المشاريع', 4, 2),
    ('5500', 'مصروف الرواتب', 5, 1)
  ) AS c(code, name, type, normal_balance)
  ON CONFLICT (company_id, code) DO NOTHING;

  RAISE NOTICE 'Phase 6 seed: Accounts created';

  -- ====================================================
  -- 3. Warehouses
  -- ====================================================
  INSERT INTO warehouses (id, company_id, code, name, location, is_active, created_at, updated_at)
  SELECT gen_random_uuid(), v_holding_id, c.code, c.name, c.loc, true, v_now, v_now
  FROM (VALUES
    ('WH-01', 'المستودع الرئيسي', 'طرابلس'),
    ('WH-02', 'مستودع بنغازي', 'بنغازي'),
    ('WH-03', 'مستودع مصراتة', 'مصراتة')
  ) AS c(code, name, loc)
  WHERE NOT EXISTS (SELECT 1 FROM warehouses WHERE company_id = v_holding_id AND code = c.code);

  -- ====================================================
  -- 4. Customers & Vendors
  -- ====================================================
  INSERT INTO customers (id, company_id, code, name, name_en, email, phone, credit_limit, payment_terms_days, is_active, created_at, created_by, updated_at)
  SELECT gen_random_uuid(), v_holding_id, 'C' || LPAD((i)::text, 4, '0'), name_ar, name_en,
    LOWER(REPLACE(name_en, ' ', '.')) || '@example.com', '+21891' || LPAD((1000000 + i*111)::text, 7, '0'),
    50000, 30, true, v_now, NULL, v_now
  FROM generate_series(1, 15) AS i
  CROSS JOIN (VALUES
    ('شركة الإنشاءات المتحدة', 'United Construction Co'),
    ('مؤسسة الفجر للمقاولات', 'AlFajr Contracting Est'),
    ('شركة البنية التحتية', 'Infrastructure Co'),
    ('شركة الإسكان الوطني', 'National Housing Co'),
    ('مجمع الأعمال التجارية', 'Business Complex'),
    ('شركة الطرق والجسور', 'Roads and Bridges Co'),
    ('شركة الكهرباء الليبية', 'Libyan Electricity Co'),
    ('مؤسسة المياه والصرف', 'Water and Sanitation Est'),
    ('شركة الاتصالات', 'Telecom Co'),
    ('مجمع الجامعات', 'University Complex'),
    ('شركة النفط الوطنية', 'National Oil Co'),
    ('مشروع الطريق الساحلي', 'Coastal Road Project'),
    ('مشروع المطار الجديد', 'New Airport Project'),
    ('مكتب الهندسة الحديث', 'Modern Engineering Office'),
    ('مشروع المدينة السكنية', 'Residential City Project')
  ) AS c(i, name_ar, name_en) ON c.i = i
  ON CONFLICT (company_id, code) DO NOTHING;

  INSERT INTO vendors (id, company_id, code, name, email, phone, address, tax_number, payment_terms, is_active, created_at, created_by, updated_at)
  SELECT gen_random_uuid(), v_holding_id, 'V' || LPAD((i)::text, 4, '0'), name_ar,
    LOWER(REPLACE(name_en, ' ', '.')) || '@vendor.com', '+21892' || LPAD((1000000 + i*222)::text, 7, '0'),
    'بنغازي، ليبيا', 'TAX-' || LPAD((1000+i)::text, 6, '0'), 'Net 30', true, v_now, NULL, v_now
  FROM generate_series(1, 10) AS i
  CROSS JOIN (VALUES
    ('مورد الأسمنت والحديد', 'Cement Steel Supplier'),
    ('مورد المعدات الثقيلة', 'Heavy Equipment Supplier'),
    ('مورد المواد الكهربائية', 'Electrical Supplier'),
    ('مورد الأخشاب', 'Timber Supplier'),
    ('مورد الأدوات الصحية', 'Plumbing Supplier'),
    ('مورد الدهانات', 'Paint Supplier'),
    ('مورد البلاط والسيراميك', 'Tiles Supplier'),
    ('مورد الزجاج والألمنيوم', 'Glass Aluminum Supplier'),
    ('مورد المعدات المكتبية', 'Office Equipment Supplier'),
    ('مورد الخدمات اللوجستية', 'Logistics Provider')
  ) AS c(i, name_ar, name_en) ON c.i = i
  ON CONFLICT (company_id, code) DO NOTHING;

  -- ====================================================
  -- 5. Items (products)
  -- ====================================================
  INSERT INTO items (id, company_id, sku, name, description, item_type, costing_method, average_cost, standard_cost, is_active, created_at, created_by, updated_at)
  SELECT gen_random_uuid(), v_holding_id, 'SKU-' || LPAD((i)::text, 4, '0'),
    name_ar, 'صنف ' || name_en, 1, 1, cost, cost, true, v_now, NULL, v_now
  FROM generate_series(1, 20) AS i
  CROSS JOIN (VALUES
    ('أسمنت بورتلاندي 50 كجم', 'Portland Cement 50kg', 25.0),
    ('حديد تسليح 12 مم', 'Rebar 12mm', 1850.0),
    ('حديد تسليح 16 مم', 'Rebar 16mm', 1850.0),
    ('رمل بناء', 'Building Sand', 80.0),
    ('حصى (زلط)', 'Gravel', 90.0),
    ('طوب أحمر', 'Red Bricks', 0.85),
    ('بلاط سيراميك 30x30', 'Ceramic Tiles 30x30', 45.0),
    ('ماسورة PPR 25 مم', 'PPR Pipe 25mm', 12.0),
    ('كابل كهربائي 2.5 مم', 'Electric Cable 2.5mm', 8.5),
    ('كابل كهربائي 4 مم', 'Electric Cable 4mm', 13.0),
    ('لوحة كهربائية 12 خط', 'Electrical Panel 12-line', 350.0),
    ('دهان أكريليك أبيض 20 لتر', 'Acrylic Paint White 20L', 180.0),
    ('ورق جدران', 'Wallpaper', 35.0),
    ('مسامير وصواميل (كجم)', 'Nuts Bolts kg', 15.0),
    ('ألواح جبس بورد', 'Gypsum Board', 42.0),
    ('سلك ربط 3 مم', 'Binding Wire 3mm', 6.0),
    ('بخاخ رش دهان', 'Paint Spray Gun', 220.0),
    ('خرطوم مياه 1 إنش', 'Water Hose 1 inch', 18.0),
    ('مفتاح إضاءة', 'Light Switch', 4.5),
    ('بطارية ليثيوم 100Ah', 'Lithium Battery 100Ah', 1200.0)
  ) AS c(i, name_ar, name_en, cost) ON c.i = i
  ON CONFLICT (company_id, sku) DO NOTHING;

  RAISE NOTICE 'Phase 6 seed: Reference data created';
  RAISE NOTICE 'For a full year of transactions, see seed-data-full-year.sql';
END $$;
