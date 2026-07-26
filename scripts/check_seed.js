const { Client } = require('pg');
(async () => {
  const db = new Client({ host: 'localhost', port: 5432, database: 'erp_system_demo', user: 'erp_user', password: 'Demo1234' });
  await db.connect();
  const r = await db.query(`SELECT
    (SELECT COUNT(*) FROM customers WHERE company_id='00000000-0000-0000-0000-000000000001') AS cust,
    (SELECT COUNT(*) FROM vendors WHERE company_id='00000000-0000-0000-0000-000000000001') AS vend,
    (SELECT COUNT(*) FROM items WHERE company_id='00000000-0000-0000-0000-000000000001') AS items,
    (SELECT COUNT(*) FROM sales_invoices WHERE company_id='00000000-0000-0000-0000-000000000001') AS si,
    (SELECT COUNT(*) FROM vendor_bills WHERE company_id='00000000-0000-0000-0000-000000000001') AS vb,
    (SELECT COUNT(*) FROM payroll_runs WHERE company_id='00000000-0000-0000-0000-000000000001') AS pr,
    (SELECT COUNT(*) FROM journal_entries WHERE company_id='00000000-0000-0000-0000-000000000001') AS je,
    (SELECT COUNT(*) FROM journal_lines jl JOIN journal_entries j ON j.id=jl.journal_entry_id WHERE j.company_id='00000000-0000-0000-0000-000000000001') AS jl`);
  const x = r.rows[0];
  console.log('Customers:', x.cust, '| Vendors:', x.vend, '| Items:', x.items);
  console.log('SalesInv:', x.si, '| Bills:', x.vb, '| Payroll:', x.pr);
  console.log('JEs:', x.je, '| JLines:', x.jl);
  await db.end();
})();
