// Take comprehensive screenshots for user manual
import { chromium } from 'playwright';
import fs from 'fs';
import path from 'path';

const SHOTS_DIR = 'C:\\Users\\Anas\\AppData\\Local\\Temp\\playwright-shots';
if (!fs.existsSync(SHOTS_DIR)) fs.mkdirSync(SHOTS_DIR, { recursive: true });

const MANUAL_DIR = 'C:\\Users\\Anas\\.minimax-agent\\projects\\user-manual-assets';
if (!fs.existsSync(MANUAL_DIR)) fs.mkdirSync(MANUAL_DIR, { recursive: true });

const BASE = 'http://localhost:3000';

const PAGES = [
  // Login
  { url: '/login', name: 'login', desc: 'صفحة تسجيل الدخول' },
  // Dashboard
  { url: '/dashboard', name: 'dashboard', desc: 'لوحة التحكم' },
  // Finance
  { url: '/finance/accounts', name: 'accounts', desc: 'دليل الحسابات' },
  { url: '/finance/accounts/new', name: 'accounts-new', desc: 'حساب جديد' },
  { url: '/finance/cost-centers', name: 'cost-centers', desc: 'مراكز التكلفة' },
  { url: '/finance/cost-centers/new', name: 'cost-centers-new', desc: 'مركز تكلفة جديد' },
  { url: '/finance/journal-entries', name: 'journal-entries', desc: 'قيود اليومية' },
  { url: '/finance/journal-entries/new', name: 'journal-entries-new', desc: 'قيد يومية جديد' },
  { url: '/finance/trial-balance', name: 'trial-balance', desc: 'ميزان المراجعة' },
  { url: '/finance/customers', name: 'customers', desc: 'العملاء' },
  { url: '/finance/customers/new', name: 'customers-new', desc: 'عميل جديد' },
  { url: '/finance/sales-invoices', name: 'sales-invoices', desc: 'فواتير المبيعات' },
  { url: '/finance/sales-invoices/new', name: 'sales-invoices-new', desc: 'فاتورة مبيعات جديدة' },
  { url: '/finance/receipts', name: 'receipts', desc: 'سندات القبض' },
  { url: '/finance/receipts/new', name: 'receipts-new', desc: 'سند قبض جديد' },
  { url: '/finance/aging-ar', name: 'aging-ar', desc: 'أعمار الذمم المدينة' },
  // Inventory
  { url: '/inventory/items', name: 'items', desc: 'الأصناف' },
  { url: '/inventory/items/new', name: 'items-new', desc: 'صنف جديد' },
  { url: '/inventory/movements', name: 'movements', desc: 'حركات المخزون' },
  { url: '/inventory/movements/new', name: 'movements-new', desc: 'حركة مخزون جديدة' },
  { url: '/inventory/reservations', name: 'reservations', desc: 'الحجوزات' },
  { url: '/inventory/stock-levels', name: 'stock-levels', desc: 'مستويات المخزون' },
  // Procurement
  { url: '/procurement/vendors', name: 'vendors', desc: 'الموردين' },
  { url: '/procurement/vendors/new', name: 'vendors-new', desc: 'مورد جديد' },
  { url: '/procurement/purchase-orders', name: 'purchase-orders', desc: 'أوامر الشراء' },
  { url: '/procurement/purchase-orders/new', name: 'purchase-orders-new', desc: 'أمر شراء جديد' },
  { url: '/procurement/goods-receipts', name: 'goods-receipts', desc: 'استلامات البضاعة' },
  { url: '/procurement/goods-receipts/new', name: 'goods-receipts-new', desc: 'استلام بضاعة جديد' },
  { url: '/procurement/bills', name: 'bills', desc: 'فواتير الموردين' },
  { url: '/procurement/bills/new', name: 'bills-new', desc: 'فاتورة مورد جديدة' },
  // HR
  { url: '/hr/departments', name: 'hr-departments', desc: 'الأقسام' },
  { url: '/hr/employees', name: 'hr-employees', desc: 'الموظفين' },
  { url: '/hr/employees/new', name: 'hr-employees-new', desc: 'موظف جديد' },
  { url: '/hr/attendance', name: 'hr-attendance', desc: 'الحضور' },
  { url: '/hr/leaves', name: 'hr-leaves', desc: 'الإجازات' },
  { url: '/hr/leaves/new', name: 'hr-leaves-new', desc: 'طلب إجازة' },
  { url: '/hr/payroll', name: 'hr-payroll', desc: 'الرواتب' },
  // Projects
  { url: '/projects', name: 'projects', desc: 'المشاريع' },
  { url: '/projects/new', name: 'projects-new', desc: 'مشروع جديد' },
  { url: '/resources', name: 'resources', desc: 'الموارد' },
  // Admin
  { url: '/admin/users', name: 'admin-users', desc: 'المستخدمين' },
  { url: '/admin/users/new', name: 'admin-users-new', desc: 'مستخدم جديد' },
  { url: '/admin/companies', name: 'admin-companies', desc: 'الشركات' },
  { url: '/admin/audit', name: 'admin-audit', desc: 'سجل التدقيق' },
  { url: '/admin/health', name: 'admin-health', desc: 'صحة النظام' },
  { url: '/admin/item-categories', name: 'admin-item-categories', desc: 'فئات الأصناف' },
  { url: '/admin/posting-rules', name: 'admin-posting-rules', desc: 'قواعد الترحيل' },
  // Other
  { url: '/profile', name: 'profile', desc: 'الملف الشخصي' },
  { url: '/holding', name: 'holding', desc: 'القابضة' },
  { url: '/transactions', name: 'transactions', desc: 'المعاملات الأخيرة' },
];

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await context.newPage();

  // Login
  console.log('1) Login...');
  await page.goto(`${BASE}/login`, { waitUntil: 'domcontentloaded' });
  await page.waitForSelector('input[type=email]');
  await page.fill('input[type=email]', 'admin@erp.local');
  await page.fill('input[type=password]', 'ChangeMe1234!');
  await page.click('button[type=submit]');
  await page.waitForURL(/.*\/dashboard.*/);
  console.log('  ✓ Logged in');

  // Visit all pages
  console.log(`\n2) Capturing ${PAGES.length} pages...`);
  for (const target of PAGES) {
    try {
      await page.goto(`${BASE}${target.url}`, { waitUntil: 'domcontentloaded' });
      await page.waitForLoadState('networkidle', { timeout: 10000 }).catch(() => {});
      await page.waitForTimeout(1500);
      // Save to manual assets dir
      const filename = `manual-${target.name}.png`;
      const localPath = path.join(MANUAL_DIR, filename);
      const tempPath = path.join(SHOTS_DIR, filename);
      await page.screenshot({ path: tempPath, fullPage: true });
      fs.copyFileSync(tempPath, localPath);
      console.log(`  📸 ${filename} (${target.desc})`);
    } catch (e) {
      console.log(`  ✗ ${target.name}: ${e.message}`);
    }
  }

  await browser.close();
  console.log(`\n✓ Done. Saved ${PAGES.length} screenshots to ${MANUAL_DIR}`);
})().catch((e) => {
  console.error(`FATAL: ${e.message}`);
  process.exit(1);
});
