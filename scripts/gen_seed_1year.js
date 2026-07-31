// 1-year seed data generator — uses ACTUAL table schemas from the DB.
const { Client } = require('pg');
const C = '00000000-0000-0000-0000-000000000001';
const U = '11111111-1111-1111-1111-111111111111';
const VAT = 0.15;
const YS = new Date('2026-01-01');
let _s = 20260101;
function r() { _s = (_s * 9301 + 49297) % 233280; return _s / 233280; }
function ri(a, b) { return Math.floor(r() * (b - a + 1)) + a; }
function pick(a) { return a[Math.floor(r() * a.length)]; }
function pickN(a, n) { const c = [...a], o = []; for (let i = 0; i < n && c.length; i++) { const i2 = Math.floor(r() * c.length); o.push(c[i2]); c.splice(i2, 1); } return o; }

async function main() {
  const db = new Client({ host: 'localhost', port: 5432, database: 'erp_system_demo', user: 'erp_user', password: 'Demo1234' });
  await db.connect();
  console.log('Connected.');
  const rc = await db.query('SELECT COUNT(*) FROM customers WHERE company_id=$1', [C]);
  if (parseInt(rc.rows[0].count) > 5) { console.log('Already seeded.'); await db.end(); return; }

  // Accounts
  const ar = await db.query('SELECT code, id FROM accounts WHERE company_id=$1', [C]);
  const Acc = {}; for (const r of ar.rows) Acc[r.code] = r.id;
  console.log(`Accounts: ${Object.keys(Acc).length}`);

  // Add missing accounts (id, company_id, code, name, type, normal_balance, is_postable, is_active, created_at, updated_at)
  const need = {
    '1110': { t: 1, n: 1, name: 'البنك' }, '1210': { t: 1, n: 1, name: 'النقدية' },
    '1230': { t: 1, n: 1, name: 'ذمم مدينة' }, '2210': { t: 2, n: 2, name: 'دائنون' },
    '2250': { t: 2, n: 2, name: 'ضريبة قيمة مضافة مستحقة' },
    '4100': { t: 4, n: 1, name: 'إيرادات المبيعات' },
    '4200': { t: 4, n: 1, name: 'مصروفات إدارية' },
    '5500': { t: 4, n: 1, name: 'مصروف الرواتب' },
    '1255': { t: 1, n: 1, name: 'ضريبة مدفوعة مقدماً' },
  };
  const miss = Object.keys(need).filter(c => !Acc[c]);
  if (miss.length) {
    for (const code of miss) {
      const x = need[code];
      const r = await db.query(
        `INSERT INTO accounts (id, company_id, code, name, type, normal_balance, is_postable, is_active, created_at, updated_at)
         VALUES (gen_random_uuid(), $1, $2, $3, $4, $5, true, true, now(), now()) RETURNING id`,
        [C, code, x.name, x.t, x.n]);
      Acc[code] = r.rows[0].id;
    }
    console.log(`Added ${miss.length} accounts`);
  }
  const acc = c => Acc[c];

  // Warehouses (already seeded — load existing)
  console.log('Warehouses...');
  const whRes = await db.query(`SELECT id FROM warehouses WHERE company_id=$1 ORDER BY code`, [C]);
  const WH = whRes.rows.map(r => r.id);
  if (WH.length === 0) throw new Error('No warehouses found');

  // UoMs (already seeded — load existing, use lowercase codes per DB)
  console.log('UoMs...');
  const uomRes = await db.query(`SELECT id, code FROM units_of_measure WHERE company_id=$1`, [C]);
  const UOM = {};
  for (const r of uomRes.rows) UOM[r.code] = r.id;

  // Categories (already seeded — load existing)
  console.log('Categories...');
  const catRes = await db.query(`SELECT id, code FROM item_categories WHERE company_id=$1`, [C]);
  const CAT = {};
  for (const r of catRes.rows) CAT[r.code] = r.id;

  // Departments (might or might not exist — create only if not)
  console.log('Departments...');
  const deptRes = await db.query(`SELECT id, code FROM departments WHERE company_id=$1`, [C]);
  const DEPT = { IT: null, ACC: null, SALES: null, OPS: null };
  for (const r of deptRes.rows) DEPT[r.code] = r.id;
  // Create any missing
  for (const [code, name] of [['IT', 'تقنية المعلومات'], ['ACC', 'المحاسبة'], ['SALES', 'المبيعات'], ['OPS', 'العمليات']]) {
    if (!DEPT[code]) {
      const r = await db.query(
        `INSERT INTO departments (id, company_id, code, name, is_active, created_at, updated_at)
         VALUES (gen_random_uuid(), $1, $2, $3, true, now(), now()) RETURNING id`,
        [C, code, name]);
      DEPT[code] = r.rows[0].id;
    }
  }

  // Customers (id, company_id, code, name, name_en, tax_id, email, phone, address, credit_limit, payment_terms_days, is_active, created_at, created_by, updated_at, updated_by)
  console.log('Customers...');
  const CD = [
    ['CUST-001', 'شركة النور للتقنية', 'LIGHT-TECH', '21845102', 'info@light-tech.ly', '0911234567'],
    ['CUST-002', 'مؤسسة الفجر للتجارة', 'ALFJR-TRADE', '21845103', 'trade@alfajr.ly', '0911234568'],
    ['CUST-003', 'شركة بنغازي للمقاولات', 'BENG-CONST', '21845104', 'projects@beng-const.ly', '0921234567'],
    ['CUST-004', 'متجر العائلة', 'FAMILY-MART', '21845105', 'sales@family-mart.ly', '0919876543'],
    ['CUST-005', 'شركة الأمل للاستيراد', 'AMAL-IMPORT', '21845106', 'import@amal.ly', '0918765432'],
    ['CUST-006', 'مزرعة الساحل', 'SAHEL-FARM', '21845107', 'farm@sahel.ly', '0927654321'],
    ['CUST-007', 'فندق المدينة', 'CITY-HOTEL', '21845108', 'hotel@city.ly', '0912345678'],
    ['CUST-008', 'شركة النفط الليبية', 'LIBYA-OIL', '21845109', 'supply@libyaoil.ly', '0913456789'],
    ['CUST-009', 'مدرسة المستقبل', 'FUTURE-SCHOOL', '21845110', 'admin@future-school.ly', '0924567890'],
    ['CUST-010', 'مستشفى الأمل', 'AMAL-HOSP', '21845111', 'admin@amal-hospital.ly', '0915678901'],
    ['CUST-011', 'شركة الاتصالات', 'TELCO', '21845112', 'info@telco.ly', '0916789012'],
    ['CUST-012', 'مكتب المحاماة', 'LAW-FIRM', '21845113', 'office@law.ly', '0927890123'],
    ['CUST-013', 'مخبز المدينة', 'CITY-BAKERY', '21845114', 'bakery@city.ly', '0918901234'],
    ['CUST-014', 'شركة التأمين', 'INSURANCE', '21845115', 'claims@insco.ly', '0919012345'],
    ['CUST-015', 'متجر الإلكترونيات', 'ELECTRO-MART', '21845116', 'sales@electro.ly', '0920123456'],
  ];
  const CUST = [];
  for (const [code, name, en, tax, email, phone] of CD) {
    const r = await db.query(
      `INSERT INTO customers (id, company_id, code, name, name_en, tax_id, email, phone, credit_limit, payment_terms_days, is_active, created_at, created_by, updated_at, updated_by)
       VALUES (gen_random_uuid(), $1, $2, $3, $4, $5, $6, $7, 50000, 30, true, now(), $8, now(), $8) RETURNING id`,
      [C, code, name, en, tax, email, phone, U]);
    CUST.push(r.rows[0].id);
  }
  console.log(`  ${CUST.length}`);

  // Vendors (id, company_id, code, name, email, phone, tax_number, currency, payment_terms, is_active, created_at, created_by, updated_at, updated_by)
  console.log('Vendors...');
  const VD = [
    ['VEN-001', 'شركة الإمداد العام', 'GEN-SUPPLY', '21899001', 'supply@gensupply.ly', '0911000001'],
    ['VEN-002', 'موردي الجملة', 'WHOLESALE', '21899002', 'sales@wholesale.ly', '0911000002'],
    ['VEN-003', 'شركة النقل والشحن', 'TRANSPORT', '21899003', 'logistics@transport.ly', '0911000003'],
    ['VEN-004', 'مزود الطاقة', 'POWER', '21899004', 'billing@power.ly', '0911000004'],
    ['VEN-005', 'مورد الأجهزة', 'EQUIPMENT', '21899005', 'sales@equipment.ly', '0911000005'],
    ['VEN-006', 'شركة المواد الخام', 'RAW-MAT', '21899006', 'sales@rawmat.ly', '0911000006'],
    ['VEN-007', 'مزود الإنترنت', 'ISP', '21899007', 'billing@isp.ly', '0911000007'],
    ['VEN-008', 'مورد المكاتب', 'OFFICE-SUP', '21899008', 'sales@office-supply.ly', '0911000008'],
    ['VEN-009', 'شركة الصيانة', 'MAINT', '21899009', 'service@maint.ly', '0911000009'],
    ['VEN-010', 'مورد التغليف', 'PACKAGING', '21899010', 'sales@packaging.ly', '0911000010'],
  ];
  const VEN = [];
  for (const [code, name, _, tax, email, phone] of VD) {
    const r = await db.query(
      `INSERT INTO vendors (id, company_id, code, name, email, phone, tax_number, currency, payment_terms, is_active, created_at, created_by, updated_at, updated_by)
       VALUES (gen_random_uuid(), $1, $2, $3, $4, $5, $6, 'LYD', 30, true, now(), $7, now(), $7) RETURNING id`,
      [C, code, name, email, phone, tax, U]);
    VEN.push(r.rows[0].id);
  }
  console.log(`  ${VEN.length}`);

  // Items (id, company_id, sku, name, name_en, description, category_id, unit_of_measure_id, barcode, average_cost, sale_price, tax_rate, reorder_level, is_active, created_at, created_by, updated_at, updated_by)
  // Use existing categories (FG, RM, CON, SVC, OFF) and UoMs (pcs, kg, m, m2, m3, l)
  console.log('Items...');
  // Map our UoM codes to existing ones
  const uomMap = { PCS: 'pcs', KG: 'kg', M: 'm', L: 'l', BOX: 'pcs' };
  // Map categories: use existing RM for raw, FG for finished, OFF for office
  const catMap = { ELEC: 'FG', FOOD: 'RM', CONS: 'RM' };
  // If we don't have ELEC/FOOD/CONS mapped to anything we have, skip them or use a fallback
  const fallbackCat = Object.values(CAT)[0]; // use first available
  const ID = [
    ['ITM-001', 'لابتوب ديل', 'Dell-Laptop', 'FG', uomMap.PCS, '5000123456001', 2500, 3500],
    ['ITM-002', 'هاتف سامسونج', 'Samsung-Phone', 'FG', uomMap.PCS, '5000123456002', 800, 1200],
    ['ITM-003', 'شاشة 24 بوصة', 'Monitor-24', 'FG', uomMap.PCS, '5000123456003', 400, 600],
    ['ITM-004', 'كيبل شبكة', 'Network-Cable', 'FG', uomMap.M, '5000123456004', 1.5, 3],
    ['ITM-005', 'أرز بسمتي 5كغ', 'Basmati-Rice', 'RM', uomMap.KG, '5000123456005', 20, 30],
    ['ITM-006', 'زيت زيتون 1لتر', 'Olive-Oil', 'RM', uomMap.L, '5000123456006', 25, 40],
    ['ITM-007', 'سكر 1كغ', 'Sugar', 'RM', uomMap.KG, '5000123456007', 3, 5],
    ['ITM-008', 'شاي أخضر 500غ', 'Green-Tea', 'RM', uomMap.BOX, '5000123456008', 15, 25],
    ['ITM-009', 'أسمنت 50كغ', 'Cement', 'RM', uomMap.KG, '5000123456009', 15, 22],
    ['ITM-010', 'حديد تسليح 12مم', 'Rebar-12', 'RM', uomMap.M, '5000123456010', 8, 12],
    ['ITM-011', 'رمل بناء', 'Sand', 'RM', uomMap.KG, '5000123456011', 0.5, 1],
    ['ITM-012', 'بلاط سيراميك', 'Tiles', 'RM', uomMap.BOX, '5000123456012', 30, 50],
    ['ITM-013', 'طابعة HP', 'HP-Printer', 'OFF', uomMap.PCS, '5000123456013', 500, 800],
    ['ITM-014', 'حبر طابعة', 'Printer-Ink', 'OFF', uomMap.PCS, '5000123456014', 50, 80],
    ['ITM-015', 'ورق A4', 'A4-Paper', 'OFF', uomMap.BOX, '5000123456015', 20, 35],
    ['ITM-016', 'دقيق 1كغ', 'Flour', 'RM', uomMap.KG, '5000123456016', 2, 4],
    ['ITM-017', 'حليب طازج 1لتر', 'Fresh-Milk', 'RM', uomMap.L, '5000123456017', 3, 5],
    ['ITM-018', 'جبنة بيضاء', 'Cheese', 'RM', uomMap.KG, '5000123456018', 15, 25],
    ['ITM-019', 'دهان جدران', 'Wall-Paint', 'RM', uomMap.L, '5000123456019', 12, 20],
    ['ITM-020', 'مسامير متنوعة', 'Nails', 'RM', uomMap.BOX, '5000123456020', 8, 15],
  ];
  // Build the actual array with valid cat/uom IDs
  const ID_RES = ID.map(it => {
    const [code, name, en, catCode, uomCode, bc, cost, price] = it;
    return [code, name, en, CAT[catCode] || fallbackCat, UOM[uomCode] || Object.values(UOM)[0], bc, cost, price];
  });
  const ITM = [];
  for (const it of ID_RES) {
    const [code, name, en, cat, uom, bc, cost, price] = it;
    // items table: id, company_id, sku, barcode, name, description, category_id, unit_of_measure_id, item_type, costing_method, average_cost, standard_cost, inventory_account_id, cogs_account_id, sales_account_id, reorder_level, reorder_quantity, is_active, created_at, created_by, updated_at, updated_by
    // Note: NO name_en, NO sale_price, NO tax_rate. Use inventory_account_id from stock account.
    const r = await db.query(
      `INSERT INTO items (id, company_id, sku, barcode, name, description, category_id, unit_of_measure_id, item_type, costing_method, average_cost, standard_cost, reorder_level, reorder_quantity, is_active, created_at, created_by, updated_at, updated_by)
       VALUES (gen_random_uuid(), $1, $2, $3, $4, $5, $6, $7, 1, 3, $8, $8, 10, 50, true, now(), $9, now(), $9) RETURNING id`,
      [C, code, bc, name, en, cat, uom, cost, U]);
    ITM.push(r.rows[0].id);
  }
  console.log(`  ${ITM.length}`);

  // Employees (id, company_id, employee_number, full_name, email, phone, national_id, department_id, job_title, hire_date, base_salary, is_active, created_at, created_by, updated_at, updated_by)
  console.log('Employees...');
  const ED = [
    ['EMP-001', 'أحمد الفيتوري', '1234567890', 'ahmed@alfajr.ly', '0911111111', DEPT.IT, 3500],
    ['EMP-002', 'فاطمة الزهراني', '1234567891', 'fatima@alfajr.ly', '0922222222', DEPT.ACC, 3200],
    ['EMP-003', 'محمد المنفي', '1234567892', 'mohamed@alfajr.ly', '0933333333', DEPT.SALES, 3000],
    ['EMP-004', 'سارة التارقية', '1234567893', 'sara@alfajr.ly', '0944444444', DEPT.OPS, 2800],
    ['EMP-005', 'علي المقرحي', '1234567894', 'ali@alfajr.ly', '0955555555', DEPT.OPS, 2900],
    ['EMP-006', 'مريم الزروق', '1234567895', 'mariam@alfajr.ly', '0966666666', DEPT.IT, 3800],
    ['EMP-007', 'خالد الفرجاني', '1234567896', 'khaled@alfajr.ly', '0977777777', DEPT.SALES, 3100],
    ['EMP-008', 'نورا العريبي', '1234567897', 'nora@alfajr.ly', '0988888888', DEPT.ACC, 3300],
  ];
  const EMP = [];
  for (const e of ED) {
    const [code, name, nat, email, phone, dept, sal] = e;
    const r = await db.query(
      `INSERT INTO employees (id, company_id, employee_number, full_name, email, phone, national_id, department_id, job_title, hire_date, base_salary, is_active, created_at, created_by, updated_at, updated_by)
       VALUES (gen_random_uuid(), $1, $2, $3, $4, $5, $6, $7, 'موظف', '2024-01-01', $8, true, now(), $9, now(), $9) RETURNING id`,
      [C, code, name, email, phone, nat, dept, sal, U]);
    EMP.push(r.rows[0].id);
  }
  console.log(`  ${EMP.length}`);

  // Cost Centers (id, company_id, code, name, type) — type is integer (1=Admin, 2=Production, 3=Sales, 4=Project, 5=Service)
  console.log('Cost centers...');
  const CC = {};
  for (const [code, name, type] of [['CC-HQ', 'الإدارة العامة', 1], ['CC-PROJ', 'المشاريع', 4], ['CC-SALES', 'المبيعات', 3], ['CC-WH', 'المخازن', 1]]) {
    const existing = await db.query(`SELECT id FROM cost_centers WHERE company_id=$1 AND code=$2`, [C, code]);
    if (existing.rows.length > 0) {
      CC[code] = existing.rows[0].id;
    } else {
      const r = await db.query(
        `INSERT INTO cost_centers (id, company_id, code, name, type) VALUES (gen_random_uuid(), $1, $2, $3, $4) RETURNING id`,
        [C, code, name, type]);
      CC[code] = r.rows[0].id;
    }
  }

  // Projects (id, company_id, cost_center_id, code, name, description, status, budget, start_date, end_date, is_active, created_at, created_by, updated_at, updated_by)
  console.log('Projects...');
  const PD = [
    ['PRJ-001', 'مشروع بناء مدرسة بنغازي', 'Benghazi-School', 1, 500000, '2026-03-01', '2026-12-31', 'CC-PROJ'],
    ['PRJ-002', 'مشروع تجديد فندق المدينة', 'Hotel-Reno', 1, 250000, '2026-01-15', '2026-08-31', 'CC-PROJ'],
    ['PRJ-003', 'مشروع توريد أجهزة كمبيوتر', 'PC-Supply', 1, 150000, '2026-02-01', '2026-06-30', 'CC-SALES'],
  ];
  const PROJ = [];
  for (const p of PD) {
    const [code, name, desc, st, bud, s, e, cc] = p;
    const r = await db.query(
      `INSERT INTO projects (id, company_id, cost_center_id, code, name, description, status, budget, start_date, end_date, is_active, created_at, created_by, updated_at, updated_by)
       VALUES (gen_random_uuid(), $1, $2, $3, $4, $5, $6, $7, $8, $9, true, now(), $10, now(), $10) RETURNING id`,
      [C, CC[cc], code, name, desc, st, bud, s, e, U]);
    PROJ.push(r.rows[0].id);
  }
  console.log(`  ${PROJ.length}`);

  // Stock movements (id, company_id, reference, type, movement_date, item_id, warehouse_id, quantity, unit_cost, status, created_at, created_by, posted_at)
  // type and status are integers
  console.log('Initial stock...');
  for (let i = 0; i < ITM.length; i++) {
    const cost = ID_RES[i][6];
    const qty = ri(50, 200);
    await db.query(
      `INSERT INTO stock_movements (id, company_id, reference, type, movement_date, item_id, warehouse_id, quantity, unit_cost, status, created_at, created_by, posted_at)
       VALUES (gen_random_uuid(), $1, 'OB-2026', 1, '2026-01-01', $2, $3, $4, $5, 2, now(), $6, now())`,
      [C, ITM[i], WH[0], qty, cost, U]);
    await db.query(
      `INSERT INTO stock_levels (id, company_id, item_id, warehouse_id, quantity_on_hand, quantity_reserved, average_cost, last_movement_at, version)
       VALUES (gen_random_uuid(), $1, $2, $3, $4, 0, $5, now(), 0)
       ON CONFLICT (item_id, warehouse_id) DO UPDATE SET quantity_on_hand=EXCLUDED.quantity_on_hand, average_cost=EXCLUDED.average_cost, last_movement_at=now(), version=stock_levels.version+1`,
      [C, ITM[i], WH[0], qty, cost]);
  }

  // Sales invoices
  console.log('Sales invoices (12 months)...');
  let ic = 1, totI = 0;
  for (let mo = 0; mo < 12; mo++) {
    const ms = new Date(YS.getTime() + mo * 30 * 86400000);
    const nI = ri(30, 50);
    for (let n = 0; n < nI; n++) {
      const idate = new Date(ms); idate.setDate(ri(1, 28));
      const ddate = new Date(idate); ddate.setDate(ddate.getDate() + 30);
      const cust = pick(CUST);
      const nL = ri(1, 3);
      const lines = pickN(ITM.map((id, i) => ({ id, d: ID_RES[i] })), nL);
      let sub = 0;
      const lc = lines.map(({ id, d }, ln) => { const q = ri(1, 20); const p = d[7]; const lt = +(q * p).toFixed(4); sub += lt; return { id, desc: d[1], ln: ln + 1, q, p, lt }; });
      const tx = +(sub * VAT).toFixed(4); const tot = +(sub + tx).toFixed(4);
      const invn = `INV-2026-${String(ic).padStart(5, '0')}`;
      ic++;
      const r = await db.query(
        `INSERT INTO sales_invoices (id, company_id, customer_id, invoice_number, invoice_date, due_date, currency_code, exchange_rate, subtotal, tax_amount, total_amount, paid_amount, status, is_deleted, created_at, created_by, updated_at, updated_by)
         VALUES (gen_random_uuid(), $1, $2, $3, $4, $5, 'LYD', 1, $6, $7, $8, 0, 'Paid', false, now(), $9, now(), $9) RETURNING id`,
        [C, cust, invn, idate.toISOString().split('T')[0], ddate.toISOString().split('T')[0], sub.toFixed(4), tx.toFixed(4), tot.toFixed(4), U]);
      const iid = r.rows[0].id;
      for (const l of lc) {
        await db.query(
          `INSERT INTO sales_invoice_lines (id, sales_invoice_id, item_id, description, line_number, quantity, unit_price, tax_rate, line_total) VALUES (gen_random_uuid(), $1, $2, $3, $4, $5, $6, $7, $8)`,
          [iid, l.id, l.desc, l.ln, l.q, l.p, VAT, l.lt.toFixed(4)]);
      }
      // Journal (status is integer; 1=Posted)
      const jen = `JE-SI-${String(ic).padStart(6, '0')}`;
      const rje = await db.query(
        `INSERT INTO journal_entries (id, entry_number, entry_date, description, reference, status, company_id, created_by_user_id, posted_at, created_at, updated_at)
         VALUES (gen_random_uuid(), $1, $2, $3, $4, 1, $5, $6, now(), now(), now()) RETURNING id`,
        [jen, idate.toISOString().split('T')[0], `Sales ${invn}`, invn, C, U]);
      const jeid = rje.rows[0].id;
      await db.query(`INSERT INTO journal_lines (id, journal_entry_id, account_id, debit, credit, description, line_number, company_id) VALUES (gen_random_uuid(), $1, $2, $3, 0, $4, 1, $5)`, [jeid, acc('1230'), tot.toFixed(4), invn, C]);
      await db.query(`INSERT INTO journal_lines (id, journal_entry_id, account_id, debit, credit, description, line_number, company_id) VALUES (gen_random_uuid(), $1, $2, 0, $3, $4, 2, $5)`, [jeid, acc('4100'), sub.toFixed(4), invn, C]);
      await db.query(`INSERT INTO journal_lines (id, journal_entry_id, account_id, debit, credit, description, line_number, company_id) VALUES (gen_random_uuid(), $1, $2, 0, $3, $4, 3, $5)`, [jeid, acc('2250'), tx.toFixed(4), invn, C]);
      totI++;
    }
  }
  console.log(`  Total: ${totI}`);

  // Vendor bills
  console.log('Bills (12 months)...');
  let bc = 1, totB = 0;
  for (let mo = 0; mo < 12; mo++) {
    const ms = new Date(YS.getTime() + mo * 30 * 86400000);
    const nB = ri(15, 30);
    for (let n = 0; n < nB; n++) {
      const bdate = new Date(ms); bdate.setDate(ri(1, 28));
      const ddate = new Date(bdate); ddate.setDate(ddate.getDate() + 30);
      const ven = pick(VEN);
      const nL = ri(1, 3);
      const lines = pickN(ITM.map((id, i) => ({ id, d: ID_RES[i] })), nL);
      let sub = 0;
      const lc = lines.map(({ id, d }, ln) => { const q = ri(5, 30); const c = d[6]; const lt = +(q * c).toFixed(4); sub += lt; return { id, desc: d[1], ln: ln + 1, q, c, lt }; });
      const tx = +(sub * VAT).toFixed(4); const tot = +(sub + tx).toFixed(4);
      const bn = `BILL-2026-${String(bc).padStart(5, '0')}`;
      bc++;
      const r = await db.query(
        `INSERT INTO vendor_bills (id, company_id, bill_number, vendor_id, status, bill_date, due_date, currency, sub_total, tax_amount, total_amount, created_at, created_by, updated_at, updated_by)
         VALUES (gen_random_uuid(), $1, $2, $3, 'Posted', $4, $5, 'LYD', $6, $7, $8, now(), $9, now(), $9) RETURNING id`,
        [C, bn, ven, bdate.toISOString().split('T')[0], ddate.toISOString().split('T')[0], sub.toFixed(4), tx.toFixed(4), tot.toFixed(4), U]);
      const bid = r.rows[0].id;
      for (const l of lc) {
        await db.query(
          `INSERT INTO vendor_bill_lines (id, company_id, vendor_id, vendor_bill_id, item_id, quantity, unit_price, tax_rate, sub_total, line_order) VALUES (gen_random_uuid(), $1, $2, $3, $4, $5, $6, $7, $8, $9)`,
          [C, ven, bid, l.id, l.q, l.c, VAT, l.lt.toFixed(4), l.ln]);
      }
      const jen = `JE-VB-${String(bc).padStart(6, '0')}`;
      const rje = await db.query(
        `INSERT INTO journal_entries (id, entry_number, entry_date, description, reference, status, company_id, created_by_user_id, posted_at, created_at, updated_at)
         VALUES (gen_random_uuid(), $1, $2, $3, $4, 1, $5, $6, now(), now(), now()) RETURNING id`,
        [jen, bdate.toISOString().split('T')[0], `Bill ${bn}`, bn, C, U]);
      const jeid = rje.rows[0].id;
      await db.query(`INSERT INTO journal_lines (id, journal_entry_id, account_id, debit, credit, description, line_number, company_id) VALUES (gen_random_uuid(), $1, $2, $3, 0, $4, 1, $5)`, [jeid, acc('4200'), sub.toFixed(4), bn, C]);
      await db.query(`INSERT INTO journal_lines (id, journal_entry_id, account_id, debit, credit, description, line_number, company_id) VALUES (gen_random_uuid(), $1, $2, $3, 0, $4, 2, $5)`, [jeid, acc('1255'), tx.toFixed(4), bn, C]);
      await db.query(`INSERT INTO journal_lines (id, journal_entry_id, account_id, debit, credit, description, line_number, company_id) VALUES (gen_random_uuid(), $1, $2, 0, $3, $4, 3, $5)`, [jeid, acc('2210'), tot.toFixed(4), bn, C]);
      totB++;
    }
  }
  console.log(`  Total: ${totB}`);

  // Payroll
  console.log('Payroll (12 months)...');
  for (let mo = 0; mo < 12; mo++) {
    const ms = new Date(YS.getTime() + mo * 30 * 86400000);
    const ps = new Date(ms); ps.setDate(1);
    const pe = new Date(ms); pe.setDate(28);
    let totG = 0; for (let i = 0; i < ED.length; i++) totG += ED[i][6];
    const r = await db.query(
      `INSERT INTO payroll_runs (id, company_id, period_start, period_end, status, total_gross, total_net, posted_at, created_at, created_by, updated_at, updated_by)
       VALUES (gen_random_uuid(), $1, $2, $3, 'Posted', $4, $4, now(), now(), $5, now(), $5) RETURNING id`,
      [C, ps.toISOString().split('T')[0], pe.toISOString().split('T')[0], totG.toFixed(4), U]);
    const rid = r.rows[0].id;
    for (let i = 0; i < EMP.length; i++) {
      const g = ED[i][6]; const tx = +(g * 0.05).toFixed(4); const net = +(g - tx).toFixed(4);
      await db.query(
        `INSERT INTO payroll_items (id, company_id, payroll_run_id, employee_id, base_salary, gross_salary, tax_amount, social_insurance_employee, net_salary, status, payment_days, created_at, created_by, updated_at)
         VALUES (gen_random_uuid(), $1, $2, $3, $4, $4, $5, 0, $6, 2, 30, now(), $7, now())`,
        [C, rid, EMP[i], g, tx, net, U]);
    }
    const jen = `JE-PR-${String(mo + 1).padStart(3, '0')}`;
    const rje = await db.query(
      `INSERT INTO journal_entries (id, entry_number, entry_date, description, reference, status, company_id, created_by_user_id, posted_at, created_at, updated_at)
       VALUES (gen_random_uuid(), $1, $2, $3, $4, 1, $5, $6, now(), now(), now()) RETURNING id`,
      [jen, pe.toISOString().split('T')[0], `Payroll ${pe.toISOString().split('T')[0]}`, jen, C, U]);
    const jeid = rje.rows[0].id;
    await db.query(`INSERT INTO journal_lines (id, journal_entry_id, account_id, debit, credit, description, line_number, company_id) VALUES (gen_random_uuid(), $1, $2, $3, 0, $4, 1, $5)`, [jeid, acc('5500'), totG.toFixed(4), jen, C]);
    await db.query(`INSERT INTO journal_lines (id, journal_entry_id, account_id, debit, credit, description, line_number, company_id) VALUES (gen_random_uuid(), $1, $2, 0, $3, $4, 2, $5)`, [jeid, acc('1210'), totG.toFixed(4), jen, C]);
  }
  console.log(`  Done`);

  // INTEGRITY
  console.log('\n========== INTEGRITY ==========');
  const r1 = await db.query(`
    SELECT je.id, je.entry_number, SUM(jl.debit) AS d, SUM(jl.credit) AS c
    FROM journal_entries je
    JOIN journal_lines jl ON jl.journal_entry_id = je.id
    WHERE je.company_id = $1
    GROUP BY je.id, je.entry_number
    HAVING ABS(SUM(jl.debit) - SUM(jl.credit)) > 0.01`, [C]);
  console.log(r1.rows.length === 0 ? '✅ JEs balance (D=C)' : `❌ ${r1.rows.length} unbalanced`);

  const r2 = await db.query(`
    SELECT
      (SELECT COALESCE(SUM(jl.debit)-SUM(jl.credit), 0) FROM journal_lines jl JOIN accounts a ON a.id=jl.account_id JOIN journal_entries je ON je.id=jl.journal_entry_id WHERE a.company_id=$1 AND a.type=1) AS A,
      (SELECT COALESCE(SUM(jl.credit)-SUM(jl.debit), 0) FROM journal_lines jl JOIN accounts a ON a.id=jl.account_id JOIN journal_entries je ON je.id=jl.journal_entry_id WHERE a.company_id=$1 AND a.type=2) AS L,
      (SELECT COALESCE(SUM(jl.credit)-SUM(jl.debit), 0) FROM journal_lines jl JOIN accounts a ON a.id=jl.account_id JOIN journal_entries je ON je.id=jl.journal_entry_id WHERE a.company_id=$1 AND a.type IN (3,5)) AS E,
      (SELECT COALESCE(SUM(jl.debit)-SUM(jl.credit), 0) FROM journal_lines jl JOIN accounts a ON a.id=jl.account_id JOIN journal_entries je ON je.id=jl.journal_entry_id WHERE a.company_id=$1 AND a.type=4) AS X`, [C]);
  const AAssets = parseFloat(r2.rows[0].a), LL = parseFloat(r2.rows[0].l), EE = parseFloat(r2.rows[0].e), XX = parseFloat(r2.rows[0].x);
  console.log(`Assets=${AAssets.toFixed(2)} | Liab=${LL.toFixed(2)} | Eq+Rev=${EE.toFixed(2)} | Exp=${XX.toFixed(2)}`);
  console.log(`A = L + E - X ?  ${Math.abs(AAssets - (LL + EE - XX)) < 0.01 ? '✅' : '❌'}  (diff=${(AAssets - (LL + EE - XX)).toFixed(4)})`);

  const r3 = await db.query('SELECT COUNT(*) FROM stock_levels WHERE company_id=$1 AND quantity_on_hand < 0', [C]);
  console.log(`Negative stock: ${r3.rows[0].count}  ${r3.rows[0].count === '0' || r3.rows[0].count === 0 ? '✅' : '❌'}`);

  const r4 = await db.query(`
    SELECT
      (SELECT COUNT(*) FROM customers WHERE company_id=$1) AS cust,
      (SELECT COUNT(*) FROM vendors WHERE company_id=$1) AS vend,
      (SELECT COUNT(*) FROM items WHERE company_id=$1) AS items,
      (SELECT COUNT(*) FROM sales_invoices WHERE company_id=$1) AS si,
      (SELECT COUNT(*) FROM vendor_bills WHERE company_id=$1) AS vb,
      (SELECT COUNT(*) FROM payroll_runs WHERE company_id=$1) AS pr,
      (SELECT COUNT(*) FROM journal_entries WHERE company_id=$1) AS je,
      (SELECT COUNT(*) FROM journal_lines jl JOIN journal_entries j ON j.id=jl.journal_entry_id WHERE j.company_id=$1) AS jl`, [C]);
  const x = r4.rows[0];
  console.log(`\nCust: ${x.cust} | Vend: ${x.vend} | Items: ${x.items}`);
  console.log(`SalesInv: ${x.si} | Bills: ${x.vb} | Payroll: ${x.pr}`);
  console.log(`JEs: ${x.je} | JLines: ${x.jl}`);

  await db.end();
  console.log('\n✅ DONE');
}

main().catch(e => { console.error('ERROR:', e.message); console.error(e.stack); process.exit(1); });
