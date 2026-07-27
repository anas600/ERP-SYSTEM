'use client';

// مكوّن Breadcrumbs — مسار تنقل تلقائي بناءً على الـ URL الحالي
// يقرأ الـ pathname من next/navigation، يقسّمه إلى segments،
// يتجاهل الـ route groups مثل (authenticated)،
// ويعرض كل segment مع تسميته العربية (مع fallback للـ slug).
//
// أمثلة:
//   /dashboard                  → الرئيسية / لوحة التحكم
//   /admin/users                → الرئيسية / الإدارة / المستخدمون
//   /finance/customers          → الرئيسية / المالية / العملاء
//   /finance/customers/new      → الرئيسية / المالية / العملاء / جديد
//   /inventory/items/abc/edit   → الرئيسية / المخزون / الأصناف / تعديل

import { Fragment } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { ChevronRight, Home } from 'lucide-react';
import { cn } from '@/lib/utils';

// ============ Segment label map ============
// مفاتيح الـ map هي الـ segments الفعلية في الـ URL.
// الـ value هي التسمية العربية المعروضة.

const SEGMENT_LABELS: Record<string, string> = {
  // Top-level
  dashboard: 'لوحة التحكم',
  reports: 'التقارير',
  notifications: 'الإشعارات',
  profile: 'الملف الشخصي',
  login: 'تسجيل الدخول',
  register: 'تسجيل',

  // Finance
  finance: 'المالية',
  accounts: 'دليل الحسابات',
  'cost-centers': 'مراكز التكلفة',
  customers: 'العملاء',
  'sales-invoices': 'فواتير المبيعات',
  receipts: 'سندات القبض',
  'journal-entries': 'القيود المحاسبية',
  'aging-ar': 'أعمار الذمم',

  // Inventory
  inventory: 'المخزون',
  items: 'الأصناف',
  movements: 'حركات المخزون',
  'stock-levels': 'مستويات المخزون',
  reservations: 'الحجوزات',

  // Projects
  projects: 'المشاريع',

  // Procurement
  procurement: 'المشتريات',
  vendors: 'الموردين',
  'purchase-orders': 'أوامر الشراء',
  'goods-receipts': 'استلامات البضاعة',
  bills: 'فواتير الموردين',

  // HR
  hr: 'الموارد البشرية',
  employees: 'الموظفين',
  attendance: 'الحضور',
  leaves: 'الإجازات',
  payroll: 'Payroll',
  'change-password': 'تغيير كلمة المرور',

  // Admin
  admin: 'الإدارة',
  users: 'المستخدمون',
  companies: 'الشركات',
  'item-categories': 'فئات الأصناف',
  'posting-rules': 'قواعد الترحيل',
  audit: 'سجل التدقيق',
  health: 'صحة النظام',

  // Reports sub
  financial: 'التقارير المالية',
  inventory_report: 'تقارير المخزون',
  sales: 'المبيعات',
  procurement_report: 'تقارير المشتريات',
  'account-activity': 'حركة حساب',
  'ap-aging': 'أعمار الذمم الدائنة',
  'balance-sheet': 'الميزانية العمومية',
  'cash-flow': 'التدفقات النقدية',
  collections: 'التحصيلات',
  'cost-center-performance': 'أداء مراكز التكلفة',
  'general-ledger': 'دفتر الأستاذ العام',
  'income-statement': 'قائمة الدخل',
  'trial-balance': 'ميزان المراجعة',
  vat: 'ضريبة القيمة المضافة',
  valuation: 'تقييم المخزون',
  'purchases-by-vendor': 'مشتريات حسب المورد',
  'top-vendors': 'أكبر الموردين',
  'budget-vs-actual': 'الميزانية مقابل الفعلي',
  'sales-by-customer': 'مبيعات حسب العميل',
  'sales-by-item': 'مبيعات حسب الصنف',
  'top-customers': 'أكبر العملاء',
};

/**
 * ترجمة الـ segment لاسمه العربي مع fallback للـ slug نفسه.
 * الـ segments الديناميكية مثل [id] / new / edit يتم تمييزها محلياً.
 */
function labelFor(segment: string, isLast: boolean, prevIsDynamic: boolean): string {
  // مسارات ديناميكية شائعة
  if (segment === 'new') return 'جديد';
  if (segment === 'edit') return 'تعديل';

  // لو الـ segment السابق كان dynamic id (UUID-like / رقم)
  // والـ segment الحالي edit/new — تم التعامل معه أعلاه
  if (prevIsDynamic) {
    // لو الـ segment الحالي يبدو أنه id (مثل abc-123 أو 5)
    if (/^[a-z0-9-]{6,}$/i.test(segment) || /^\d+$/.test(segment)) {
      // نُظهر #المعرّف بشكل مختصر
      return `#${segment.slice(0, 6)}`;
    }
  }

  return SEGMENT_LABELS[segment] ?? segment;
}

function isDynamicSegment(seg: string): boolean {
  return /^[a-z0-9-]{6,}$/i.test(seg) || /^\d+$/.test(seg);
}

// ============ Component ============

export interface BreadcrumbsProps {
  /** تعطيل عرض الرئيسية كأول segment */
  hideHome?: boolean;
  /** كلاس إضافي للـ nav */
  className?: string;
}

export function Breadcrumbs({ hideHome = false, className }: BreadcrumbsProps) {
  const pathname = usePathname() ?? '/';

  // قسّم الـ pathname وتجاهل الـ segments الفارغة
  const rawSegments = pathname.split('/').filter(Boolean);

  // ابنِ مصفوفة الـ segments مع عناوينها العربية
  // نتجاهل الـ route groups (الـ segments اللي بين قوسين) مثل (authenticated)
  const filtered = rawSegments.filter((s) => !(s.startsWith('(') && s.endsWith(')')));

  if (filtered.length === 0) {
    // على الـ root لا نعرض شيء
    return null;
  }

  // ابنِ الـ cumulative hrefs
  const crumbs: { label: string; href: string; isLast: boolean }[] = [];
  let acc = '';
  for (let i = 0; i < filtered.length; i++) {
    acc += `/${filtered[i]}`;
    const isLast = i === filtered.length - 1;
    const prevSeg = i > 0 ? filtered[i - 1] : '';
    const prevIsDynamic = i > 0 && isDynamicSegment(prevSeg);
    crumbs.push({
      label: labelFor(filtered[i], isLast, prevIsDynamic),
      href: acc,
      isLast,
    });
  }

  return (
    <nav
      dir="rtl"
      aria-label="مسار التنقل"
      className={cn('text-sm text-gray-500 mb-3 flex items-center flex-wrap gap-y-1', className)}
    >
      {!hideHome && (
        <>
          <Link
            href="/dashboard"
            className="flex items-center gap-1 hover:text-blue-600 transition-colors"
            aria-label="الرئيسية"
          >
            <Home className="h-3.5 w-3.5" />
            <span>الرئيسية</span>
          </Link>
          {crumbs.length > 0 && (
            <ChevronRight className="h-3.5 w-3.5 mx-1.5 text-gray-300" />
          )}
        </>
      )}
      {crumbs.map((c, i) => (
        <Fragment key={c.href}>
          {c.isLast ? (
            <span className="text-gray-800 font-medium" aria-current="page">
              {c.label}
            </span>
          ) : (
            <Link
              href={c.href}
              className="hover:text-blue-600 transition-colors"
            >
              {c.label}
            </Link>
          )}
          {i < crumbs.length - 1 && (
            <ChevronRight className="h-3.5 w-3.5 mx-1.5 text-gray-300" />
          )}
        </Fragment>
      ))}
    </nav>
  );
}

export default Breadcrumbs;
