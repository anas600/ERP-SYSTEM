-- ============================================================
-- ERP System — Phases 6-8: Procurement, Sales, Inventory,
-- Payroll, Projects, Journal Entries
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
  v_i           int;
  v_dt          date;
  v_amount      numeric;
  v_qty         numeric;
  v_price       numeric;
  v_je_id       uuid;
  v_po_id       uuid;
  v_rec         record;
  v_gr_id       uuid;
  v_cust_id     uuid;
  v_vendor_id   uuid;
  v_item_id     uuid;
  v_wh_id       uuid;
  v_po_num      int := 1000;
  v_inv_num     int := 5000;
  v_je_counter  int := 0;
  v_status      text;
  v_run_id      uuid;
BEGIN
  -- Get VAT account IDs
  SELECT id INTO v_acc_vat_out FROM accounts WHERE company_id = v_company_id AND code = '2250' LIMIT 1;
  SELECT id INTO v_acc_vat_in  FROM accounts WHERE company_id = v_company_id AND code = '1255' LIMIT 1;

  -- ============================================================
  -- Phase 6: Purchase Orders (40)
  -- ============================================================
  SELECT COUNT(*) INTO v_count FROM purchase_orders WHERE company_id = v_company_id;
  IF v_count >= 30 THEN
    RAISE NOTICE 'Phase 6: POs already exist (%)', v_count;
  ELSE
    RAISE NOTICE 'Phase 6: Purchase Orders (40)';
    FOR v_i IN 1..40 LOOP
      v_dt := v_year_start + ((v_i * 8) || ' days')::interval;
      SELECT id INTO v_vendor_id FROM vendors WHERE company_id = v_company_id ORDER BY random() LIMIT 1;
      v_amount := (5000 + (v_i * 1373) % 80000)::numeric;
      v_po_num := v_po_num + 1;

      INSERT INTO purchase_orders (id, company_id, po_number, vendor_id, status, order_date, expected_date,
        currency, sub_total, tax_amount, total_amount, notes, created_at, created_by, updated_at)
      VALUES
        (gen_random_uuid(), v_company_id, 'PO-' || v_po_num::text, v_vendor_id,
         CASE (v_i % 4) WHEN 0 THEN 'Draft' WHEN 1 THEN 'Approved' WHEN 2 THEN 'Sent' ELSE 'Received' END,
         v_dt, v_dt + interval '14 days', 'LYD',
         ROUND(v_amount / 1.15, 2), ROUND(v_amount - (v_amount / 1.15), 2), v_amount,
         'أمر شراء رقم ' || v_po_num, v_now, v_admin_id, v_now);
    END LOOP;
  END IF;

  -- ============================================================
  -- Phase 6b: Goods Receipts + Vendor Bills + Payments
  -- ============================================================
  SELECT COUNT(*) INTO v_count FROM goods_receipts WHERE company_id = v_company_id;
  IF v_count < 20 THEN
    RAISE NOTICE 'Phase 6b: Goods Receipts + Bills + Payments';
    FOR v_rec IN
      SELECT id, vendor_id, total_amount, order_date FROM purchase_orders
      WHERE company_id = v_company_id AND status IN ('Approved','Sent','Received')
      ORDER BY order_date LIMIT 30
    LOOP
      -- Goods receipt
      INSERT INTO goods_receipts (id, company_id, gr_number, purchase_order_id, status, received_date,
        warehouse_id, notes, created_at, created_by, updated_at)
      VALUES
        (gen_random_uuid(), v_company_id, 'GR-' || substring(v_rec.id::text, 1, 8), v_rec.id, 'Received',
          v_rec.order_date + interval '10 days',
          (SELECT id FROM warehouses WHERE company_id = v_company_id ORDER BY random() LIMIT 1),
          'استلام البضاعة', v_now, v_admin_id, v_now)
      RETURNING id INTO v_gr_id;

      -- Vendor bill (posted)
      INSERT INTO vendor_bills (id, company_id, bill_number, goods_receipt_id, vendor_id, status,
        bill_date, due_date, currency, sub_total, tax_amount, total_amount, notes,
        posted_at, created_at, created_by, updated_at)
      VALUES
        (gen_random_uuid(), v_company_id, 'BILL-' || extract(epoch from v_rec.order_date)::bigint || '-' || (random()*1000)::int,
          v_gr_id, v_rec.vendor_id, 'Posted',
          v_rec.order_date + interval '10 days', v_rec.order_date + interval '40 days',
          'LYD', ROUND(v_rec.total_amount / 1.15, 2),
          ROUND(v_rec.total_amount - v_rec.total_amount/1.15, 2),
          v_rec.total_amount,
          'فاتورة مورد', v_rec.order_date + interval '10 days', v_now, v_admin_id, v_now);
    END LOOP;
  END IF;

  -- Payments (60% full, 30% partial, 10% none)
  SELECT COUNT(*) INTO v_count FROM payments WHERE company_id = v_company_id;
  IF v_count < 10 THEN
    RAISE NOTICE 'Phase 6c: Payments to vendors';
    FOR v_rec IN
      SELECT vb.id, vb.vendor_id, vb.total_amount, vb.bill_date FROM vendor_bills vb
      WHERE vb.company_id = v_company_id AND vb.status = 'Posted'
    LOOP
      v_dt := (v_rec.bill_date + interval '40 days')::date;
      IF v_dt > CURRENT_DATE THEN CONTINUE; END IF;

      v_amount := CASE (abs(hashtext(v_rec.id::text)) % 10)
        WHEN 0 THEN 0
        WHEN 9 THEN v_rec.total_amount * 0.5
        ELSE v_rec.total_amount
      END;

      IF v_amount > 0 THEN
        INSERT INTO payments (id, company_id, party_type, party_id, payment_number,
          payment_date, amount, currency_code, payment_method, status, posted_at, created_at, created_by, updated_at)
        VALUES
          (gen_random_uuid(), v_company_id, v_company_id, 'Vendor', v_rec.vendor_id,
            'PAY-' || extract(epoch from v_dt)::bigint || '-' || (random()*1000)::int,
            v_dt, v_amount, 'LYD', 'BankTransfer', 2, v_dt, v_now, v_admin_id, v_now);
      END IF;
    END LOOP;
  END IF;

  -- ============================================================
  -- Phase 7: Sales Invoices (60) + Receipts
  -- ============================================================
  SELECT COUNT(*) INTO v_count FROM sales_invoices WHERE company_id = v_company_id;
  IF v_count >= 50 THEN
    RAISE NOTICE 'Phase 7: Invoices already exist (%)', v_count;
  ELSE
    RAISE NOTICE 'Phase 7: Sales Invoices (60) + Receipts';
    FOR v_i IN 1..60 LOOP
      v_dt := v_year_start + ((v_i * 5) || ' days')::interval;
      SELECT id INTO v_cust_id FROM customers WHERE company_id = v_company_id ORDER BY random() LIMIT 1;
      v_amount := (8000 + (v_i * 2347) % 120000)::numeric;
      v_inv_num := v_inv_num + 1;
      v_status := CASE (v_i % 10) WHEN 0 THEN 'Draft' WHEN 9 THEN 'Partial' ELSE 'Posted' END;

      INSERT INTO sales_invoices (id, company_id, customer_id, invoice_number, invoice_date,
        due_date, currency_code, exchange_rate, subtotal, tax_amount, total_amount, paid_amount, outstanding,
        status, notes, posted_at, created_at, created_by, updated_at)
      VALUES
        (gen_random_uuid(), v_company_id, v_company_id, v_cust_id, 'INV-' || v_inv_num::text,
          v_dt, v_dt + interval '30 days', 'LYD', 1.0,
          ROUND(v_amount / 1.15, 2), ROUND(v_amount - v_amount/1.15, 2), v_amount,
          CASE v_status WHEN 'Posted' THEN v_amount WHEN 'Partial' THEN v_amount * 0.5 ELSE 0 END,
          CASE v_status WHEN 'Posted' THEN 0 WHEN 'Partial' THEN v_amount * 0.5 ELSE v_amount END,
          v_status, 'فاتورة مبيعات', CASE WHEN v_status IN ('Posted','Partial') THEN v_dt END,
          v_now, v_admin_id, v_now);
    END LOOP;

    -- Receipts
    FOR v_rec IN
      SELECT id, customer_id, paid_amount, invoice_date FROM sales_invoices
      WHERE company_id = v_company_id AND paid_amount > 0
    LOOP
      v_dt := (v_rec.invoice_date + interval '15 days')::date;
      IF v_dt > CURRENT_DATE THEN CONTINUE; END IF;

      INSERT INTO receipts (id, company_id, customer_id, receipt_number, receipt_date,
        amount, currency_code, payment_method, notes, posted_at, created_at, created_by, updated_at)
      VALUES
        (gen_random_uuid(), v_company_id, v_company_id, v_rec.customer_id,
          'RCP-' || extract(epoch from v_dt)::bigint || '-' || (random()*1000)::int,
          v_dt, v_rec.paid_amount, 'LYD', 'BankTransfer', 'سند قبض', v_dt, v_now, v_admin_id, v_now);
    END LOOP;
  END IF;

  -- ============================================================
  -- Phase 7b: Stock Movements + Levels
  -- ============================================================
  SELECT COUNT(*) INTO v_count FROM stock_movements WHERE company_id = v_company_id;
  IF v_count < 30 THEN
    RAISE NOTICE 'Phase 7b: Stock Movements';
    -- IN movements (from goods receipts)
    FOR v_rec IN
      SELECT gr.id, gr.warehouse_id, gr.received_date FROM goods_receipts gr
      WHERE gr.company_id = v_company_id
    LOOP
      v_item_id := (SELECT id FROM items WHERE company_id = v_company_id ORDER BY random() LIMIT 1);
      v_qty := 50 + (random() * 200)::numeric;
      v_price := (SELECT average_cost FROM items WHERE id = v_item_id);

      INSERT INTO stock_movements (id, company_id, reference, type, movement_date, item_id, warehouse_id,
        quantity, unit_cost, source_type, source_id, status, created_at, created_by, posted_at)
      VALUES
        (gen_random_uuid(), v_company_id, v_company_id, 'GR-' || substring(v_rec.id::text, 1, 8), 1,
          v_rec.received_date, v_item_id, v_rec.warehouse_id,
          v_qty, v_price, 'GoodsReceipt', v_rec.id, 2, v_now, v_admin_id, v_rec.received_date);

      INSERT INTO stock_levels (id, company_id, item_id, warehouse_id, quantity_on_hand,
        average_cost, last_movement_at, version)
      VALUES (gen_random_uuid(), v_company_id, v_company_id, v_item_id, v_rec.warehouse_id,
        v_qty, v_price, v_rec.received_date, 1)
      ON CONFLICT DO NOTHING;
    END LOOP;

    -- OUT movements (from sales)
    FOR v_rec IN
      SELECT id, invoice_date FROM sales_invoices WHERE company_id = v_company_id AND status = 'Posted'
    LOOP
      v_item_id := (SELECT id FROM items WHERE company_id = v_company_id ORDER BY random() LIMIT 1);
      v_wh_id := (SELECT id FROM warehouses WHERE company_id = v_company_id ORDER BY random() LIMIT 1);
      v_qty := 5 + (random() * 30)::numeric;
      v_price := (SELECT average_cost FROM items WHERE id = v_item_id);

      INSERT INTO stock_movements (id, company_id, reference, type, movement_date, item_id, warehouse_id,
        quantity, unit_cost, source_type, source_id, status, created_at, created_by, posted_at)
      VALUES
        (gen_random_uuid(), v_company_id, v_company_id, 'INV-' || substring(v_rec.id::text, 1, 8), 2,
          v_rec.invoice_date, v_item_id, v_wh_id,
          v_qty, v_price, 'SalesInvoice', v_rec.id, 2, v_now, v_admin_id, v_rec.invoice_date);
    END LOOP;
  END IF;

  -- ============================================================
  -- Phase 8a: Payroll Runs (12 months)
  -- ============================================================
  SELECT COUNT(*) INTO v_count FROM payroll_runs WHERE company_id = v_company_id;
  IF v_count >= 12 THEN
    RAISE NOTICE 'Phase 8a: Payroll already exists (%)', v_count;
  ELSE
    RAISE NOTICE 'Phase 8a: Payroll Runs (12 months)';
    FOR v_i IN 0..11 LOOP
      v_dt := (v_year_start + (v_i || ' months')::interval)::date;

      INSERT INTO payroll_runs (id, company_id, period_start, period_end, status, total_gross,
        total_net, processed_at, posted_at, notes, created_at, created_by, updated_at)
      VALUES
        (gen_random_uuid(), v_company_id,
          v_dt, (v_dt + interval '1 month' - interval '1 day')::date,
          'Posted', 105000.0, 95000.0, v_dt, v_dt,
          'رواتب شهر ' || to_char(v_dt, 'YYYY-MM'),
          v_now, v_admin_id, v_now)
      RETURNING id INTO v_run_id;

      INSERT INTO payroll_items (id, company_id, payroll_run_id, employee_id, base_salary, gross_salary,
        tax_amount, social_insurance_employee, net_salary, status, payment_days, created_at, created_by, updated_at)
      SELECT gen_random_uuid(), v_company_id, v_run_id, e.id, e.base_salary, e.base_salary + 450,
        ROUND((e.base_salary * 0.05)::numeric, 2), ROUND((e.base_salary * 0.04)::numeric, 2),
        e.base_salary + 450 - ROUND((e.base_salary * 0.05)::numeric, 2) - ROUND((e.base_salary * 0.04)::numeric, 2),
        'Posted', 30, v_now, v_admin_id, v_now
      FROM employees e WHERE e.company_id = v_company_id AND e.is_active = true;
    END LOOP;
  END IF;

  -- ============================================================
  -- Phase 8b: Project Tasks + Resource Assignments + Budgets
  -- ============================================================
  SELECT COUNT(*) INTO v_count FROM project_tasks WHERE company_id = v_company_id;
  IF v_count < 10 THEN
    RAISE NOTICE 'Phase 8b: Project tasks + assignments';
    FOR v_rec IN
      SELECT id, name, start_date FROM projects WHERE company_id = v_company_id
    LOOP
      FOR v_i IN 1..6 LOOP
        INSERT INTO project_tasks (id, company_id, project_id, name, description, status,
          estimated_hours, actual_hours, start_date, end_date, progress_percent, created_at, updated_at)
        VALUES
          (gen_random_uuid(), v_company_id, v_rec.id,
            'مهمة ' || v_i || ' - ' || v_rec.name, 'وصف المهمة ' || v_i, (v_i % 4),
            40 + v_i*10, 30 + v_i*8,
            v_rec.start_date + ((v_i * 5) || ' days')::interval,
            v_rec.start_date + (((v_i+1) * 5) || ' days')::interval,
            v_i * 15, v_now, v_now);
      END LOOP;
    END LOOP;

    -- First insert resources (employees as resources)
    INSERT INTO resources (id, company_id, code, name, type, hourly_rate, is_active, created_at, updated_at)
    SELECT gen_random_uuid(), v_company_id, 'RES-' || employee_number, full_name, 1, 25.0, true, v_now, v_now
    FROM employees WHERE company_id = v_company_id;

    -- Resource assignments
    FOR v_i IN 1..30 LOOP
      INSERT INTO resource_assignments (id, company_id, project_id, task_id, resource_id, user_id,
        from_ts, to_ts, hourly_rate, created_at)
      SELECT gen_random_uuid(), v_company_id,
        (SELECT id FROM projects WHERE company_id = v_company_id ORDER BY random() LIMIT 1),
        (SELECT id FROM project_tasks WHERE company_id = v_company_id ORDER BY random() LIMIT 1),
        (SELECT id FROM resources WHERE company_id = v_company_id ORDER BY random() LIMIT 1),
        (SELECT id FROM users WHERE company_id = v_company_id ORDER BY random() LIMIT 1),
        v_year_start + ((v_i * 8) || ' days')::interval,
        v_year_start + ((v_i * 8 + 14) || ' days')::interval,
        25.0 + (v_i % 30), v_now;
    END LOOP;

    -- Project budgets (1 per project, use DISTINCT to avoid duplicates)
    INSERT INTO project_budgets (id, company_id, project_id, cost_center_id, account_id,
      budget_amount, spent_amount, committed_amount, last_recalculated_at)
    SELECT gen_random_uuid(), v_company_id, p.id, p.cost_center_id, v_acc_inv,
      ROUND((p.budget * 0.3)::numeric, 2), ROUND((p.budget * 0.18)::numeric, 2),
      ROUND((p.budget * 0.05)::numeric, 2), v_now
    FROM projects p
    WHERE p.company_id = v_company_id
      AND NOT EXISTS (SELECT 1 FROM project_budgets pb WHERE pb.project_id = p.id);
  END IF;

  -- ============================================================
  -- Phase 8c: Journal Entries (auto-post for all transactions)
  -- ============================================================
  SELECT COUNT(*) INTO v_count FROM journal_entries WHERE company_id = v_company_id;
  IF v_count < 50 THEN
    RAISE NOTICE 'Phase 8c: Journal Entries for all transactions';
    -- Sales invoices → AR + VAT + Revenue
    FOR v_rec IN
      SELECT id, customer_id, total_amount, subtotal, tax_amount, invoice_date
      FROM sales_invoices WHERE company_id = v_company_id AND status IN ('Posted','Partial')
    LOOP
      v_je_counter := v_je_counter + 1;
      v_je_id := gen_random_uuid();
      INSERT INTO journal_entries (id, company_id, entry_number, entry_date, description, reference,
        status, posted_at, created_at, created_by_user_id)
      VALUES
        (v_je_id, v_company_id, 'JE-' || (9000 + v_je_counter)::text,
          v_rec.invoice_date, 'قيد فاتورة مبيعات', 'INV-' || v_je_counter::text,
          2, v_rec.invoice_date, v_now, v_admin_id);
      INSERT INTO journal_lines (id, journal_entry_id, account_id, debit, credit, description, line_number)
      VALUES
        (gen_random_uuid(), v_je_id, v_acc_ar, v_rec.total_amount, 0, 'مدين - عميل', 1),
        (gen_random_uuid(), v_je_id, v_acc_vat_out, 0, v_rec.tax_amount, 'دائن - ضريبة', 2),
        (gen_random_uuid(), v_je_id, v_acc_rev_proj, 0, v_rec.subtotal, 'دائن - إيراد', 3);
      UPDATE sales_invoices SET journal_entry_id = v_je_id WHERE id = v_rec.id;
    END LOOP;

    -- Vendor bills → Inv + VAT Input + AP
    FOR v_rec IN
      SELECT vb.id, vb.vendor_id, vb.total_amount, vb.sub_total, vb.tax_amount, vb.bill_date
      FROM vendor_bills vb WHERE vb.company_id = v_company_id AND vb.status = 'Posted'
    LOOP
      v_je_counter := v_je_counter + 1;
      v_je_id := gen_random_uuid();
      INSERT INTO journal_entries (id, company_id, entry_number, entry_date, description, reference,
        status, posted_at, created_at, created_by_user_id)
      VALUES
        (v_je_id, v_company_id, 'JE-' || (9000 + v_je_counter)::text,
          v_rec.bill_date, 'قيد فاتورة مورد', 'BILL-' || v_je_counter::text,
          2, v_rec.bill_date, v_now, v_admin_id);
      INSERT INTO journal_lines (id, journal_entry_id, account_id, debit, credit, description, line_number)
      VALUES
        (gen_random_uuid(), v_je_id, v_acc_inv, v_rec.sub_total, 0, 'مدين - مخزون', 1),
        (gen_random_uuid(), v_je_id, v_acc_vat_in, v_rec.tax_amount, 0, 'مدين - ضريبة مدفوعة', 2),
        (gen_random_uuid(), v_je_id, v_acc_ap, 0, v_rec.total_amount, 'دائن - مورد', 3);
      UPDATE vendor_bills SET journal_entry_id = v_je_id WHERE id = v_rec.id;
    END LOOP;

    -- Receipts → Bank + AR
    FOR v_rec IN
      SELECT r.id, r.customer_id, r.amount, r.receipt_date FROM receipts r
      WHERE r.company_id = v_company_id
    LOOP
      v_je_counter := v_je_counter + 1;
      v_je_id := gen_random_uuid();
      INSERT INTO journal_entries (id, company_id, entry_number, entry_date, description, reference,
        status, posted_at, created_at, created_by_user_id)
      VALUES
        (v_je_id, v_company_id, 'JE-' || (9000 + v_je_counter)::text,
          v_rec.receipt_date, 'قيد سند قبض', 'RCP-' || v_je_counter::text,
          2, v_rec.receipt_date, v_now, v_admin_id);
      INSERT INTO journal_lines (id, journal_entry_id, account_id, debit, credit, description, line_number)
      VALUES
        (gen_random_uuid(), v_je_id, v_acc_bank, v_rec.amount, 0, 'مدين - بنك', 1),
        (gen_random_uuid(), v_je_id, v_acc_ar, 0, v_rec.amount, 'دائن - عميل', 2);
      UPDATE receipts SET journal_entry_id = v_je_id WHERE id = v_rec.id;
    END LOOP;

    -- Payments → AP + Bank
    FOR v_rec IN
      SELECT p.id, p.party_id, p.amount, p.payment_date FROM payments p
      WHERE p.company_id = v_company_id AND p.status = 2
    LOOP
      v_je_counter := v_je_counter + 1;
      v_je_id := gen_random_uuid();
      INSERT INTO journal_entries (id, company_id, entry_number, entry_date, description, reference,
        status, posted_at, created_at, created_by_user_id)
      VALUES
        (v_je_id, v_company_id, 'JE-' || (9000 + v_je_counter)::text,
          v_rec.payment_date, 'قيد سند دفع', 'PAY-' || v_je_counter::text,
          2, v_rec.payment_date, v_now, v_admin_id);
      INSERT INTO journal_lines (id, journal_entry_id, account_id, debit, credit, description, line_number)
      VALUES
        (gen_random_uuid(), v_je_id, v_acc_ap, v_rec.amount, 0, 'مدين - مورد', 1),
        (gen_random_uuid(), v_je_id, v_acc_bank, 0, v_rec.amount, 'دائن - بنك', 2);
      UPDATE payments SET journal_entry_id = v_je_id WHERE id = v_rec.id;
    END LOOP;

    -- Payroll → Salary + Cash + AP
    FOR v_rec IN
      SELECT pr.id, pr.period_end,
        SUM(pi.net_salary) AS net, SUM(pi.base_salary + 450 - pi.net_salary) AS deductions
      FROM payroll_runs pr
      JOIN payroll_items pi ON pi.payroll_run_id = pr.id
      WHERE pr.company_id = v_company_id AND pr.status = 'Posted'
      GROUP BY pr.id, pr.period_end
    LOOP
      v_je_counter := v_je_counter + 1;
      v_je_id := gen_random_uuid();
      INSERT INTO journal_entries (id, company_id, entry_number, entry_date, description, reference,
        status, posted_at, created_at, created_by_user_id)
      VALUES
        (v_je_id, v_company_id, 'JE-' || (9000 + v_je_counter)::text,
          v_rec.period_end, 'قيد رواتب', 'PAYROLL-' || v_je_counter::text,
          2, v_rec.period_end, v_now, v_admin_id);
      INSERT INTO journal_lines (id, journal_entry_id, account_id, debit, credit, description, line_number)
      VALUES
        (gen_random_uuid(), v_je_id, v_acc_salary, v_rec.net + v_rec.deductions, 0, 'مدين - مصروف رواتب', 1),
        (gen_random_uuid(), v_je_id, v_acc_cash, 0, v_rec.net, 'دائن - نقدية', 2),
        (gen_random_uuid(), v_je_id, v_acc_ap, 0, v_rec.deductions, 'دائن - مستحقات', 3);
    END LOOP;

    -- Monthly rent + utilities + depreciation
    FOR v_i IN 0..11 LOOP
      v_dt := (v_year_start + (v_i || ' months')::interval)::date;

      -- Rent 8000
      v_je_counter := v_je_counter + 1;
      v_je_id := gen_random_uuid();
      INSERT INTO journal_entries (id, company_id, entry_number, entry_date, description, reference,
        status, posted_at, created_at, created_by_user_id)
      VALUES
        (v_je_id, v_company_id, 'JE-' || (9000 + v_je_counter)::text,
          v_dt, 'إيجار شهري', 'RENT-' || (v_i+1), 2, v_dt, v_now, v_admin_id);
      INSERT INTO journal_lines (id, journal_entry_id, account_id, debit, credit, description, line_number)
      VALUES
        (gen_random_uuid(), v_je_id, v_acc_admin, 8000, 0, 'إيجار', 1),
        (gen_random_uuid(), v_je_id, v_acc_cash, 0, 8000, 'نقدية', 2);

      -- Utilities 2500
      v_je_counter := v_je_counter + 1;
      v_je_id := gen_random_uuid();
      INSERT INTO journal_entries (id, company_id, entry_number, entry_date, description, reference,
        status, posted_at, created_at, created_by_user_id)
      VALUES
        (v_je_id, v_company_id, 'JE-' || (9000 + v_je_counter)::text,
          v_dt + 5, 'كهرباء ومياه', 'UTIL-' || (v_i+1), 2, v_dt + 5, v_now, v_admin_id);
      INSERT INTO journal_lines (id, journal_entry_id, account_id, debit, credit, description, line_number)
      VALUES
        (gen_random_uuid(), v_je_id, v_acc_admin, 2500, 0, 'كهرباء ومياه', 1),
        (gen_random_uuid(), v_je_id, v_acc_cash, 0, 2500, 'نقدية', 2);

      -- Depreciation 3000
      v_je_counter := v_je_counter + 1;
      v_je_id := gen_random_uuid();
      INSERT INTO journal_entries (id, company_id, entry_number, entry_date, description, reference,
        status, posted_at, created_at, created_by_user_id)
      VALUES
        (v_je_id, v_company_id, 'JE-' || (9000 + v_je_counter)::text,
          v_dt + 10, 'إهلاك شهري', 'DEP-' || (v_i+1), 2, v_dt + 10, v_now, v_admin_id);
      INSERT INTO journal_lines (id, journal_entry_id, account_id, debit, credit, description, line_number)
      VALUES
        (gen_random_uuid(), v_je_id, v_acc_admin, 3000, 0, 'إهلاك', 1),
        (gen_random_uuid(), v_je_id, v_acc_eq, 0, 3000, 'مجمع الإهلاك', 2);
    END LOOP;

    RAISE NOTICE 'Phase 8c: % journal entries created', v_je_counter;
  END IF;

  RAISE NOTICE '=========================================';
  RAISE NOTICE 'ALL PHASES DONE';
  RAISE NOTICE '=========================================';
END $$;
