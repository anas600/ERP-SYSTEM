'use client';

// Sprint 63 (DEC-218) — SmartSidebar.
//
// Role-aware sidebar that replaces the static Sidebar in AppShell. It hides
// any module the current user cannot see (driven by useVisibleModules()).
//
// L19 / DEC-095: no userId is sent from the FE — the BE reads it from the
// JWT (the api.ts interceptor already attaches the Bearer token).
//
// Visual parity with the legacy sidebar:
//   - dark/light Tailwind classes, RTL (Arabic)
//   - Lucide icons
//   - grouped navigation (المالية / المشاريع / etc.)
//   - active route highlight
//   - loading state while the BE fetch is in flight

import { useMemo } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import {
  LayoutDashboard,
  TrendingUp,
  Briefcase,
  Hammer,
  Wallet,
  Layers,
  FolderTree,
  GitBranch,
  ArrowRightLeft,
  UserPlus,
  ShoppingCart,
  HandCoins,
  Scale,
  FileText,
  FileBarChart,
  Hourglass,
  Trophy,
  Boxes,
  Activity,
  Lock,
  Truck,
  FileSpreadsheet,
  PackageCheck,
  Receipt,
  Building2,
  UserCog,
  Clock,
  CalendarOff,
  Banknote,
  Users,
  Tag,
  Settings,
  Shield,
  Heart,
  Droplet,
  X,
  type LucideIcon,
} from 'lucide-react';
import { useVisibleModules } from '@/hooks/useVisibleModules';
import type { ModuleCode } from '@/lib/api-types';
import { cn } from '@/lib/utils';

// ============ Types ============

export interface NavItem {
  label: string;
  href: string;
  icon: LucideIcon;
  /** Which module this item belongs to (for visibility filtering). */
  module: ModuleCode;
}

export interface NavGroup {
  label?: string;
  items: NavItem[];
}

interface SmartSidebarProps {
  /** Open state on mobile (drawer). */
  open: boolean;
  /** Close handler for mobile (clicking backdrop or X). */
  onClose: () => void;
}

// ============ Navigation map ============
//
// Mirrors the legacy `NAV_GROUPS` in `AppShell.tsx` plus a `module` field
// so the SmartSidebar can filter by `useVisibleModules()`.
//
// Module map (matches the visibility matrix in
// `src/backend/Modules/Identity/AGENTS.md`):
//   Dashboard    → Dashboard
//   Projects     → Projects
//   Finance + AR → Finance (Finance module covers AR sub-screens too)
//   Inventory    → Inventory
//   Procurement  → Procurement
//   HR + Payroll → HR (HR module covers Payroll sub-screens too)
//   Admin        → Companies + Identity (admin / company switcher)

const NAV_GROUPS: NavGroup[] = [
  {
    items: [
      { label: 'لوحة التحكم', href: '/dashboard', icon: LayoutDashboard, module: 'Dashboard' },
      { label: 'اللوحة التنفيذية', href: '/dashboard/executive', icon: TrendingUp, module: 'Dashboard' },
    ],
  },
  {
    label: 'المالية',
    items: [
      { label: 'دليل الحسابات', href: '/finance/accounts', icon: Wallet, module: 'Finance' },
      { label: 'شجرة الحسابات (4 مستويات)', href: '/finance/accounts-tree', icon: Layers, module: 'Finance' },
      { label: 'مراكز التكلفة', href: '/finance/cost-centers', icon: FolderTree, module: 'Finance' },
      { label: 'قيود اليومية', href: '/finance/journal-entries', icon: GitBranch, module: 'Finance' },
      { label: 'المعاملات الأخيرة', href: '/transactions', icon: ArrowRightLeft, module: 'Finance' },
      { label: 'العملاء', href: '/finance/customers', icon: UserPlus, module: 'AR' },
      { label: 'فواتير المبيعات', href: '/finance/sales-invoices', icon: ShoppingCart, module: 'AR' },
      { label: 'سندات القبض', href: '/finance/receipts', icon: HandCoins, module: 'AR' },
    ],
  },
  {
    label: 'التقارير المالية',
    items: [
      { label: 'ميزان المراجعة', href: '/finance/reports/trial-balance', icon: Scale, module: 'Finance' },
      { label: 'ميزان المراجعة الهرمي', href: '/finance/trial-balance-v2', icon: Layers, module: 'Finance' },
      { label: 'دفتر الأستاذ', href: '/finance/reports/general-ledger', icon: FileText, module: 'Finance' },
      { label: 'الميزانية العمومية', href: '/finance/reports/balance-sheet', icon: FileBarChart, module: 'Finance' },
      { label: 'قائمة الدخل', href: '/finance/reports/income-statement', icon: TrendingUp, module: 'Finance' },
      { label: 'التدفقات النقدية', href: '/finance/reports/cash-flow', icon: Droplet, module: 'Finance' },
      { label: 'أعمار الذمم (AR + AP)', href: '/finance/reports/aging-summary', icon: Hourglass, module: 'Finance' },
      { label: 'أعمار الذمم AR', href: '/finance/aging-ar', icon: Activity, module: 'AR' },
      { label: 'أكبر العملاء والأصناف', href: '/finance/reports/top-customers', icon: Trophy, module: 'Finance' },
    ],
  },
  {
    label: 'المخزون',
    items: [
      { label: 'الأصناف', href: '/inventory/items', icon: Boxes, module: 'Inventory' },
      { label: 'حركات المخزون', href: '/inventory/movements', icon: Activity, module: 'Inventory' },
      { label: 'الحجوزات', href: '/inventory/reservations', icon: Lock, module: 'Inventory' },
      { label: 'مستويات المخزون', href: '/inventory/stock-levels', icon: Layers, module: 'Inventory' },
    ],
  },
  {
    label: 'المشاريع',
    items: [
      { label: 'المشاريع', href: '/projects', icon: Briefcase, module: 'Projects' },
      { label: 'الموارد', href: '/resources', icon: Hammer, module: 'Projects' },
    ],
  },
  {
    label: 'المشتريات',
    items: [
      { label: 'الموردين', href: '/procurement/vendors', icon: Truck, module: 'Procurement' },
      { label: 'أوامر الشراء', href: '/procurement/purchase-orders', icon: FileSpreadsheet, module: 'Procurement' },
      { label: 'استلامات البضاعة', href: '/procurement/goods-receipts', icon: PackageCheck, module: 'Procurement' },
      { label: 'فواتير الموردين', href: '/procurement/bills', icon: Receipt, module: 'Procurement' },
    ],
  },
  {
    label: 'الموارد البشرية',
    items: [
      { label: 'الأقسام', href: '/hr/departments', icon: Building2, module: 'HR' },
      { label: 'الموظفين', href: '/hr/employees', icon: UserCog, module: 'HR' },
      { label: 'الحضور', href: '/hr/attendance', icon: Clock, module: 'HR' },
      { label: 'الإجازات', href: '/hr/leaves', icon: CalendarOff, module: 'HR' },
      { label: 'Payroll', href: '/hr/payroll', icon: Banknote, module: 'HR' },
    ],
  },
  {
    label: 'الإدارة',
    items: [
      { label: 'المستخدمين', href: '/admin/users', icon: Users, module: 'Identity' },
      { label: 'الشركات', href: '/admin/companies', icon: Building2, module: 'Companies' },
      { label: 'فئات الأصناف', href: '/admin/item-categories', icon: Tag, module: 'Identity' },
      { label: 'قواعد الترحيل', href: '/admin/posting-rules', icon: Settings, module: 'Identity' },
      { label: 'سجل التدقيق', href: '/admin/audit', icon: Shield, module: 'Identity' },
      { label: 'صحة النظام', href: '/admin/health', icon: Heart, module: 'Identity' },
    ],
  },
];

// ============ Component ============

/**
 * Module-aware sidebar. Renders only the items whose module is in
 * `useVisibleModules()`. While the BE fetch is in flight, the sidebar shows
 * a skeleton and zero items (conservative — never shows a wrong link).
 */
export function SmartSidebar({ open, onClose }: SmartSidebarProps) {
  const pathname = usePathname();
  const { modules: visible, loading } = useVisibleModules();

  const visibleSet = useMemo(() => new Set<ModuleCode>(visible), [visible]);

  const filteredGroups = useMemo(() => {
    return NAV_GROUPS
      .map((group) => ({
        ...group,
        items: group.items.filter((item) => visibleSet.has(item.module)),
      }))
      .filter((group) => group.items.length > 0);
  }, [visibleSet]);

  return (
    <>
      {/* Backdrop on mobile */}
      {open && (
        <div
          className="fixed inset-0 z-40 bg-black/40 md:hidden"
          onClick={onClose}
          aria-hidden="true"
        />
      )}

      <aside
        className={cn(
          'fixed md:sticky md:top-0 inset-y-0 right-0 z-50 md:z-30',
          'w-64 bg-white border-l border-ink-200 flex-shrink-0',
          'transform transition-transform duration-200 ease-in-out',
          'md:translate-x-0 md:h-screen',
          open ? 'translate-x-0' : 'translate-x-full md:translate-x-0',
        )}
        dir="rtl"
        aria-label="القائمة الجانبية"
      >
        {/* Logo + close (mobile) */}
        <div className="h-16 flex items-center justify-between px-5 border-b border-ink-100 bg-gradient-to-l from-brand-50/30 to-transparent">
          <Link href="/dashboard" className="flex items-center gap-2.5 group" onClick={onClose}>
            <div className="h-9 w-9 rounded-xl bg-gradient-to-br from-brand-500 to-brand-700 text-white flex items-center justify-center font-bold text-sm shadow-soft-sm group-hover:shadow-soft transition-shadow">
              ERP
            </div>
            <div>
              <p className="font-bold text-ink-800 leading-tight text-sm">ERP-SYSTEM</p>
              <p className="text-[10px] text-ink-500">v1.0.15 · Sprint 63</p>
            </div>
          </Link>
          <button
            onClick={onClose}
            className="md:hidden text-ink-400 hover:text-ink-700 p-1 transition-colors"
            aria-label="إغلاق القائمة"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Navigation */}
        <nav className="overflow-y-auto h-[calc(100vh-4rem)] py-4 px-3">
          {loading ? (
            <div className="space-y-2 px-3 py-2" aria-busy="true">
              <div className="h-4 w-32 rounded bg-ink-100 animate-pulse" />
              <div className="h-4 w-40 rounded bg-ink-100 animate-pulse" />
              <div className="h-4 w-28 rounded bg-ink-100 animate-pulse" />
              <div className="h-4 w-36 rounded bg-ink-100 animate-pulse" />
              <p className="text-[11px] text-ink-400 pt-2">جاري التحميل...</p>
            </div>
          ) : filteredGroups.length === 0 ? (
            <p className="px-3 py-6 text-xs text-ink-500 text-center">
              لا توجد وحدات متاحة لك. تواصل مع المسؤول.
            </p>
          ) : (
            filteredGroups.map((group, gi) => (
              <div key={gi} className={cn(gi > 0 && 'mt-5')}>
                {group.label && (
                  <p className="px-3 mb-2 text-[10px] font-semibold text-ink-400 uppercase tracking-wider">
                    {group.label}
                  </p>
                )}
                <ul className="space-y-0.5">
                  {group.items.map((item) => {
                    const Icon = item.icon;
                    const active = pathname === item.href || pathname?.startsWith(item.href + '/');
                    return (
                      <li key={item.href}>
                        <Link
                          href={item.href}
                          onClick={onClose}
                          className={cn(
                            'flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-all duration-150',
                            active
                              ? 'bg-brand-50 text-brand-700 shadow-soft-sm font-bold'
                              : 'text-ink-700 hover:bg-ink-50 hover:text-ink-800',
                          )}
                        >
                          <Icon className={cn('h-4 w-4 flex-shrink-0', active ? 'text-brand-600' : 'text-ink-400')} />
                          <span>{item.label}</span>
                        </Link>
                      </li>
                    );
                  })}
                </ul>
              </div>
            ))
          )}
        </nav>
      </aside>
    </>
  );
}
