-- ============================================================
-- ERP System — Phases 2-8 (HR, Procurement, Sales, Inventory,
-- Payroll, Projects, Journal Entries)
-- Idempotent at the level of each phase
-- ============================================================

\set ON_ERROR_STOP on

DO $$
DECLARE
  v_company_id  uuid := 'ec6b98ee-221c-410e-a690-192245314a68';
  v_admin_id   uuid := 'f61842d7-195b-4823-855f-ca4adb80f7ac';

  v_acc_cash    uuid := '7f8124fd-8d20-404c-89c9-c05b0653cff5';
  v_acc_bank    uuid := '9d1a8e44-2829-4006-8488-f14720d92301';
  v_acc_ar      uuid := '69056f40-928c-4137-a5e1-12be806de6fd';
  v_acc_inv     uuid := '621bfd2d-2935-4a88-91a9-e42e187edcf3';
  v_acc_ap      uuid := '916ba930-6ca3-4693-ab86-8a9a1dff8a52';
  v_acc_eq      uuid := 'd6cf88c9-8e7e-44b4-9ed3-1ebf84e794a4';
  v_acc_admin   uuid := '5b1de03d-f5f3-454b-b8f2-d55b6aae6753';
  v_acc_salary  uuid := 'c8ee59e0-d530-4e7b-aa05-bbba4d40ff49';
  v_acc_rev_proj uuid := 'b769959f-67bf-4830-8c74-139ef93cfc08';
  v_acc_vat_out uuid;
  v_acc_vat_in  uuid;

  v_count       int;
  v_year_start  date := '2025-08-01';
  v_now         timestamptz := now();
BEGIN
  -- Phase 2: Employees
  SELECT COUNT(*) INTO v_count FROM employees WHERE company_id = v_company_id;
  IF v_count >= 15 THEN
    RAISE NOTICE 'Phase 2 already done (% employees). Skipping.', v_count;
  ELSE
    RAISE NOTICE 'Phase 2: Employees (15 more)';

    INSERT INTO employees (id, company_id, employee_number, full_name, email, phone, national_id,
      department_id, job_title, hire_date, base_salary, is_active, created_at, created_by, updated_at)
    SELECT gen_random_uuid(), v_company_id,
      'EMP-2026-' || LPAD((e.i+3)::text, 4, '0'),
      e.name_ar,
      LOWER(REPLACE(SPLIT_PART(e.name_en, ' ', 1), ' ', '.')) || e.i::text || '@alfajr.local',
      '+21891' || LPAD((2000000 + e.i*137)::text, 7, '0'),
      'NAT-' || LPAD((100+e.i)::text, 8, '0'),
      (SELECT id FROM departments WHERE company_id = v_company_id AND code = e.dept LIMIT 1),
      e.job_title,
      v_year_start - (e.i * interval '7 days'),
      e.salary, true, v_now, v_admin_id, v_now
    FROM (VALUES
      (0,  'محمد الفيتوري',  'Mohamed Fitouri',   'OPS',  'مهندس مدني',     3500.0),
      (1,  'علي الزوام',      'Ali Zwam',          'OPS',  'مشرف موقع',      4200.0),
      (2,  'خالد بن عمر',     'Khaled Omar',       'OPS',  'عامل بناء',      1800.0),
      (3,  'يوسف الشريف',     'Youssef Shariff',   'FIN',  'محاسب',          3200.0),
      (4,  'أحمد التارقية',   'Ahmed Targia',      'FIN',  'مدير مالي',      6500.0),
      (5,  'سالم البريكي',    'Salem Bereki',      'SALES','مندوب مبيعات',   2800.0),
      (6,  'عمر الكاسح',      'Omar Kaseh',        'OPS',  'سائق معدات',     2200.0),
      (7,  'فاطمة العريبي',   'Fatima Orabi',      'FIN',  'محاسبة رواتب',   2900.0),
      (8,  'نورا الفيتوري',   'Nora Fitouri',      'IT',   'مطور نظم',       4500.0),
      (9,  'حسن المصراتي',    'Hassan Misrati',    'OPS',  'مهندس كهرباء',   3800.0),
      (10, 'سعاد بن سعيد',    'Souad Said',        'IT',   'مدير مشاريع',    7000.0),
      (11, 'كريم العريبي',    'Karim Orabi',       'OPS',  'نجار',           2000.0),
      (12, 'منى البريكي',     'Mona Bereki',       'FIN',  'محاسب أول',      4200.0),
      (13, 'عبدالله الزروق',  'Abdullah Zarrouk',  'OPS',  'حداد',           2100.0),
      (14, 'ليلى الفيتوري',   'Layla Fitouri',     'SALES','مدير مبيعات',    5800.0)
    ) AS e(i, name_ar, name_en, dept, job_title, salary);
  END IF;

  -- Phase 3: Salary Structures
  SELECT COUNT(*) INTO v_count FROM salary_structures WHERE company_id = v_company_id;
  IF v_count >= 3 THEN
    RAISE NOTICE 'Phase 3 already done (% structures). Skipping.', v_count;
  ELSE
    RAISE NOTICE 'Phase 3: Salary Structures (3 templates)';

    INSERT INTO salary_structures (id, company_id, code, name, currency, is_active, created_at, created_by, updated_at)
    VALUES
      (gen_random_uuid(), v_company_id, 'SS-MGR', 'هيكل المديرين',    'LYD', true, v_now, v_admin_id, v_now),
      (gen_random_uuid(), v_company_id, 'SS-ENG', 'هيكل المهندسين',   'LYD', true, v_now, v_admin_id, v_now),
      (gen_random_uuid(), v_company_id, 'SS-WKR', 'هيكل العمال',      'LYD', true, v_now, v_admin_id, v_now);

    -- Structure lines for managers
    INSERT INTO salary_structure_lines (id, company_id, salary_structure_id, type, name, amount, sort_order)
    SELECT gen_random_uuid(), v_company_id, ss.id, sl.t, sl.n, sl.a, sl.o
    FROM salary_structures ss
    CROSS JOIN (VALUES
      ('earning',    'الراتب الأساسي', 0.0,  1),
      ('earning',    'بدل سكن',        300.0, 2),
      ('earning',    'بدل مواصلات',    150.0, 3),
      ('deduction',  'تأمينات (موظف)', 0.0,   4),
      ('deduction',  'ضريبة دخل',      0.0,   5)
    ) AS sl(t, n, a, o)
    WHERE ss.code = 'SS-MGR';
  END IF;

  -- Phase 4: Attendance (12 months, weekdays only)
  SELECT COUNT(*) INTO v_count FROM attendance WHERE company_id = v_company_id;
  IF v_count > 100 THEN
    RAISE NOTICE 'Phase 4 already done (% attendance). Skipping.', v_count;
  ELSE
    RAISE NOTICE 'Phase 4: Attendance (12 months)';
    INSERT INTO attendance (id, company_id, employee_id, type, "timestamp", ip_address, created_at)
    SELECT gen_random_uuid(), v_company_id,
      e.id,
      CASE (i % 2) WHEN 0 THEN 'check_in' ELSE 'check_out' END,
      (v_year_start + (m * interval '1 month') + (d * interval '1 day') + interval '8 hours' + ((i % 30) * interval '1 minute')),
      '192.168.1.' || (10 + (i % 100))::text,
      v_now
    FROM employees e
    CROSS JOIN generate_series(0, 11) AS m(month_idx)
    CROSS JOIN generate_series(1, 22) AS d(day_idx)
    CROSS JOIN generate_series(0, 1) AS i(seq)
    WHERE e.company_id = v_company_id
      AND EXTRACT(DOW FROM (v_year_start + (m * interval '1 month') + (d * interval '1 day'))) NOT IN (5, 6);
  END IF;

  -- Phase 5: Leave Requests
  SELECT COUNT(*) INTO v_count FROM leave_requests WHERE company_id = v_company_id;
  IF v_count > 20 THEN
    RAISE NOTICE 'Phase 5 already done (% leaves). Skipping.', v_count;
  ELSE
    RAISE NOTICE 'Phase 5: Leave Requests (~50)';
    INSERT INTO leave_requests (id, company_id, employee_id, leave_type, start_date, end_date,
      total_days, status, reason, created_at, created_by, updated_at)
    SELECT gen_random_uuid(), v_company_id,
      e.id,
      CASE (i % 4) WHEN 0 THEN 'Annual' WHEN 1 THEN 'Sick' WHEN 2 THEN 'Emergency' ELSE 'Unpaid' END,
      (v_year_start + (m * interval '1 month') + (5 * interval '1 day')),
      (v_year_start + (m * interval '1 month') + (5 * interval '1 day') + (((i % 5) + 1) || ' days')::interval),
      (i % 5) + 1,
      CASE (i % 10) WHEN 0 THEN 'Pending' WHEN 1 THEN 'Rejected' ELSE 'Approved' END,
      'طلب إجازة ' || (i+1)::text,
      v_now, v_admin_id, v_now
    FROM employees e
    CROSS JOIN generate_series(0, 11) AS m(month_idx)
    CROSS JOIN generate_series(0, 2) AS i(seq)
    WHERE e.company_id = v_company_id;
  END IF;

  RAISE NOTICE 'Phase 2-5 done';
END $$;
