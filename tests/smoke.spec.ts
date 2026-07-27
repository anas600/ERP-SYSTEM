/**
 * Smoke test — verifies all 41 critical backend endpoints respond 200.
 * Run: npx playwright test smoke.spec.ts
 */
import { test, expect, describe } from '@playwright/test';
import { apiGet, HOLDING_ID } from './helpers/api';

const FROM = '2026-01-01';
const TO = '2026-12-31';

describe('Smoke: backend endpoints return 200 + JSON shape', () => {
  // ============ Identity & Access ============
  test('GET /api/users', async () => {
    const { status, data } = await apiGet('/api/users');
    expect(status).toBe(200);
    expect(Array.isArray(data?.items) || Array.isArray(data)).toBe(true);
  });

  test('GET /api/identity/roles', async () => {
    const { status, data } = await apiGet('/api/identity/roles');
    expect(status).toBe(200);
    expect(Array.isArray(data)).toBe(true);
  });

  // ============ Companies (Multi-Company core) ============
  test('GET /api/companies', async () => {
    const { status, data } = await apiGet('/api/companies');
    expect(status).toBe(200);
    expect(Array.isArray(data)).toBe(true);
    expect(data.length).toBeGreaterThan(0);
  });

  test('GET /api/companies/tree', async () => {
    const { status, data } = await apiGet('/api/companies/tree');
    expect(status).toBe(200);
    // tree can be array or object — just ensure non-null
    expect(data).not.toBeNull();
  });

  // ============ HR ============
  test('GET /api/hr/departments', async () => {
    const { status, data } = await apiGet('/api/hr/departments');
    expect(status).toBe(200);
    expect(Array.isArray(data)).toBe(true);
  });

  test('GET /api/hr/employees', async () => {
    const { status, data } = await apiGet('/api/hr/employees');
    expect(status).toBe(200);
    expect(Array.isArray(data)).toBe(true);
  });

  test('GET /api/hr/payroll/runs', async () => {
    const { status, data } = await apiGet('/api/hr/payroll/runs');
    expect(status).toBe(200);
    expect(Array.isArray(data?.items) || Array.isArray(data)).toBe(true);
  });

  test('GET /api/hr/attendance', async () => {
    const { status, data } = await apiGet('/api/hr/attendance');
    expect(status).toBe(200);
    expect(Array.isArray(data?.items) || Array.isArray(data)).toBe(true);
  });

  test('GET /api/hr/leaves', async () => {
    const { status, data } = await apiGet('/api/hr/leaves');
    expect(status).toBe(200);
    expect(Array.isArray(data?.items) || Array.isArray(data)).toBe(true);
  });

  // ============ Inventory ============
  test('GET /api/inventory/items', async () => {
    const { status, data } = await apiGet('/api/inventory/items');
    expect(status).toBe(200);
    expect(Array.isArray(data)).toBe(true);
  });

  test('GET /api/inventory/warehouses', async () => {
    const { status, data } = await apiGet('/api/inventory/warehouses');
    expect(status).toBe(200);
    expect(Array.isArray(data)).toBe(true);
  });

  test('GET /api/inventory/categories', async () => {
    const { status, data } = await apiGet('/api/inventory/categories');
    expect(status).toBe(200);
    expect(Array.isArray(data)).toBe(true);
  });

  test('GET /api/inventory/uom', async () => {
    const { status, data } = await apiGet('/api/inventory/uom');
    expect(status).toBe(200);
    expect(Array.isArray(data)).toBe(true);
  });

  // ============ Finance / Accounting ============
  test('GET /api/finance/accounts', async () => {
    const { status, data } = await apiGet('/api/finance/accounts');
    expect(status).toBe(200);
    expect(Array.isArray(data)).toBe(true);
    expect(data.length).toBeGreaterThan(40); // 47 from seed
  });

  test('GET /api/finance/journal-entries', async () => {
    const { status, data } = await apiGet('/api/finance/journal-entries', { from: FROM, to: TO });
    expect(status).toBe(200);
    expect(Array.isArray(data?.items) || Array.isArray(data)).toBe(true);
  });

  // ============ AR (Sales / Customers) ============
  test('GET /api/ar/customers', async () => {
    const { status, data } = await apiGet('/api/ar/customers');
    expect(status).toBe(200);
    expect(Array.isArray(data)).toBe(true);
    expect(data.length).toBeGreaterThanOrEqual(15);
  });

  test('GET /api/ar/sales-invoices', async () => {
    const { status, data } = await apiGet('/api/ar/sales-invoices');
    expect(status).toBe(200);
    expect(Array.isArray(data?.items) || Array.isArray(data)).toBe(true);
  });

  test('GET /api/ar/aging', async () => {
    const { status, data } = await apiGet('/api/ar/aging');
    expect(status).toBe(200);
    expect(data).not.toBeNull();
  });

  // ============ Procurement ============
  test('GET /api/procurement/vendors', async () => {
    const { status, data } = await apiGet('/api/procurement/vendors');
    expect(status).toBe(200);
    expect(Array.isArray(data)).toBe(true);
    expect(data.length).toBeGreaterThanOrEqual(10);
  });

  test('GET /api/procurement/bills', async () => {
    const { status, data } = await apiGet('/api/procurement/bills');
    expect(status).toBe(200);
    expect(Array.isArray(data?.items) || Array.isArray(data)).toBe(true);
  });

  test('GET /api/procurement/pos', async () => {
    const { status, data } = await apiGet('/api/procurement/pos');
    expect(status).toBe(200);
    expect(Array.isArray(data?.items) || Array.isArray(data)).toBe(true);
  });

  test('GET /api/procurement/grs', async () => {
    const { status, data } = await apiGet('/api/procurement/grs');
    expect(status).toBe(200);
    expect(Array.isArray(data?.items) || Array.isArray(data)).toBe(true);
  });

  // ============ Projects ============
  test('GET /api/projects', async () => {
    const { status, data } = await apiGet('/api/projects');
    expect(status).toBe(200);
    expect(Array.isArray(data?.items) || Array.isArray(data)).toBe(true);
  });

  // ============ Reports — Financial ============
  test('GET /api/finance/reports/trial-balance', async () => {
    const { status, data } = await apiGet('/api/finance/reports/trial-balance', { from: FROM, to: TO });
    expect(status).toBe(200);
    expect(Array.isArray(data?.rows)).toBe(true);
  });

  test('GET /api/finance/reports/balance-sheet', async () => {
    const { status, data } = await apiGet('/api/finance/reports/balance-sheet', { asOf: '2026-12-31' });
    expect(status).toBe(200);
    expect(data?.assets).toBeDefined();
    expect(data?.liabilities).toBeDefined();
  });

  test('GET /api/finance/reports/income-statement', async () => {
    const { status, data } = await apiGet('/api/finance/reports/income-statement', { from: FROM, to: TO });
    expect(status).toBe(200);
    expect(data).not.toBeNull();
  });

  test('GET /api/finance/reports/cash-flow', async () => {
    const { status, data } = await apiGet('/api/finance/reports/cash-flow', { from: FROM, to: TO });
    expect(status).toBe(200);
    expect(data).not.toBeNull();
  });

  test('GET /api/finance/reports/vat', async () => {
    const { status, data } = await apiGet('/api/finance/reports/vat', { from: FROM, to: TO });
    expect(status).toBe(200);
    expect(data).not.toBeNull();
  });

  test('GET /api/finance/reports/ap-aging', async () => {
    const { status, data } = await apiGet('/api/finance/reports/ap-aging', { asOf: '2026-12-31' });
    expect(status).toBe(200);
    expect(data).not.toBeNull();
  });

  test('GET /api/finance/reports/cost-center-performance', async () => {
    const { status, data } = await apiGet('/api/finance/reports/cost-center-performance', { from: FROM, to: TO });
    expect(status).toBe(200);
    expect(data).not.toBeNull();
  });

  test('GET /api/finance/reports/journal-entries', async () => {
    const { status, data } = await apiGet('/api/finance/reports/journal-entries', { from: FROM, to: TO });
    expect(status).toBe(200);
    expect(Array.isArray(data?.lines) || Array.isArray(data?.items) || Array.isArray(data)).toBe(true);
    // Data integrity: totalDebit must equal totalCredit (accounting equation)
    if (data?.totalDebit !== undefined) {
      expect(Math.abs(data.totalDebit - data.totalCredit)).toBeLessThan(0.01);
    }
  });

  // ============ Reports — Sales / Procurement / Inventory / Projects ============
  test('GET /api/ar/reports/top-customers', async () => {
    const { status, data } = await apiGet('/api/ar/reports/top-customers', { from: FROM, to: TO, limit: 10 });
    expect(status).toBe(200);
    expect(data?.rows).toBeDefined();
  });

  test('GET /api/ar/reports/sales-by-customer', async () => {
    const { status, data } = await apiGet('/api/ar/reports/sales-by-customer', { from: FROM, to: TO });
    expect(status).toBe(200);
    expect(data?.rows).toBeDefined();
  });

  test('GET /api/ar/reports/sales-by-item', async () => {
    const { status, data } = await apiGet('/api/ar/reports/sales-by-item', { from: FROM, to: TO });
    expect(status).toBe(200);
    expect(data?.rows).toBeDefined();
  });

  test('GET /api/finance/reports/collections', async () => {
    const { status, data } = await apiGet('/api/finance/reports/collections', { from: FROM, to: TO });
    expect(status).toBe(200);
    expect(data).not.toBeNull();
  });

  test('GET /api/procurement/reports/purchases-by-vendor', async () => {
    const { status, data } = await apiGet('/api/procurement/reports/purchases-by-vendor', { from: FROM, to: TO });
    expect(status).toBe(200);
    expect(data).not.toBeNull();
  });

  test('GET /api/procurement/reports/top-vendors', async () => {
    const { status, data } = await apiGet('/api/procurement/reports/top-vendors', { from: FROM, to: TO, limit: 10 });
    expect(status).toBe(200);
    expect(data?.rows).toBeDefined();
  });

  test('GET /api/reports/projects/budget-vs-actual', async () => {
    const { status, data } = await apiGet('/api/reports/projects/budget-vs-actual', { from: FROM, to: TO });
    expect(status).toBe(200);
    expect(data).not.toBeNull();
  });

  test('GET /api/reports/inventory/valuation', async () => {
    const { status, data } = await apiGet('/api/reports/inventory/valuation');
    expect(status).toBe(200);
    expect(data?.count).toBeGreaterThan(0);
  });
});
