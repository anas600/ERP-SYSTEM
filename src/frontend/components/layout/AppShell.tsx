'use client';

// مكوّن AppShell — الـ Layout الموحد لكل الصفحات المحمية
// يحوي: Topbar + Sidebar + Main content area
// Responsive: sidebar يصبح drawer على الشاشات الصغيرة

import { ReactNode, useState, useEffect } from 'react';
import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import {
  LayoutDashboard,
  Users,
  Truck,
  FileText,
  PackageCheck,
  Receipt,
  UserCog,
  Clock,
  CalendarOff,
  Boxes,
  Wallet,
  Briefcase,
  Hammer,
  Banknote,
  Hourglass,
  UserPlus,
  ShoppingCart,
  HandCoins,
  Menu,
  X,
  LogOut,
  ChevronLeft,
  // Phase 6.2 additions
  FolderTree,
  GitBranch,
  Activity,
  Lock,
  Layers,
  Bell,
  Building2,
  Tag,
  Settings,
  Shield,
  Heart,
  BarChart3,
  LineChart,
  FileBarChart,
  ShoppingBag,
  Package,
  FileSpreadsheet,
  ArrowRightLeft,
} from 'lucide-react';
import { authApi } from '@/lib/api';
import { CompanySwitcher } from '@/components/layout/CompanySwitcher';
import { cn } from '@/lib/utils';

// ============ Navigation structure ============
// يدعم مجموعات (groups) لتنظيم القائمة

export interface NavItem {
  label: string;
  href: string;
  icon: React.ComponentType<{ className?: string }>;
  /** قائمة فرعية (اختياري) */
  children?: NavItem[];
}

export interface NavGroup {
  label?: string;
  items: NavItem[];
}

const NAV_GROUPS: NavGroup[] = [
  {
    items: [
      { label: 'لوحة التحكم', href: '/dashboard', icon: LayoutDashboard },
    ],
  },
  {
    label: 'المالية',
    items: [
      // Sprint 30 (DEC-100): removed the duplicate /accounts page. The new
      // /finance/accounts page (Sprint 11 T1) is the single source of truth.
      { label: 'دليل الحسابات', href: '/finance/accounts', icon: Wallet },
      { label: 'مراكز التكلفة', href: '/finance/cost-centers', icon: FolderTree },
      { label: 'قيود اليومية', href: '/finance/journal-entries', icon: GitBranch },
      // Sprint 11 T1: top-level Transactions hub (recent journal feed).
      { label: 'المعاملات الأخيرة', href: '/transactions', icon: ArrowRightLeft },
      { label: 'العملاء', href: '/finance/customers', icon: UserPlus },
      { label: 'فواتير المبيعات', href: '/finance/sales-invoices', icon: ShoppingCart },
      { label: 'سندات القبض', href: '/finance/receipts', icon: HandCoins },
      { label: 'أعمار الذمم AR', href: '/finance/aging-ar', icon: Hourglass },
    ],
  },
  {
    label: 'المخزون',
    items: [
      { label: 'الأصناف', href: '/inventory/items', icon: Boxes },
      { label: 'حركات المخزون', href: '/inventory/movements', icon: Activity },
      { label: 'الحجوزات', href: '/inventory/reservations', icon: Lock },
      { label: 'مستويات المخزون', href: '/inventory/stock-levels', icon: Layers },
    ],
  },
  {
    label: 'المشاريع',
    items: [
      { label: 'المشاريع', href: '/projects', icon: Briefcase },
      { label: 'الموارد', href: '/resources', icon: Hammer },
    ],
  },
  {
    label: 'المشتريات',
    items: [
      { label: 'الموردين', href: '/procurement/vendors', icon: Truck },
      { label: 'أوامر الشراء', href: '/procurement/purchase-orders', icon: FileText },
      { label: 'استلامات البضاعة', href: '/procurement/goods-receipts', icon: PackageCheck },
      { label: 'فواتير الموردين', href: '/procurement/bills', icon: Receipt },
    ],
  },
  {
    label: 'الموارد البشرية',
    items: [
      { label: 'الأقسام', href: '/hr/departments', icon: Building2 },
      { label: 'الموظفين', href: '/hr/employees', icon: UserCog },
      { label: 'الحضور', href: '/hr/attendance', icon: Clock },
      { label: 'الإجازات', href: '/hr/leaves', icon: CalendarOff },
      { label: 'Payroll', href: '/hr/payroll', icon: Banknote },
    ],
  },
  // Sprint 22: Reports section removed (each module has its own reports).
  // Reports section deleted — each module has its own reports now.
  {
    label: 'الإدارة',
    items: [
      { label: 'المستخدمين', href: '/admin/users', icon: Users },
      { label: 'الشركات', href: '/admin/companies', icon: Building2 },
      { label: 'فئات الأصناف', href: '/admin/item-categories', icon: Tag },
      { label: 'قواعد الترحيل', href: '/admin/posting-rules', icon: Settings },
      { label: 'سجل التدقيق', href: '/admin/audit', icon: Shield },
      { label: 'صحة النظام', href: '/admin/health', icon: Heart },
    ],
  },
];

// ============ Sidebar ============

interface SidebarProps {
  open: boolean;
  onClose: () => void;
}

function Sidebar({ open, onClose }: SidebarProps) {
  const pathname = usePathname();

  return (
    <>
      {/* Backdrop على الموبايل */}
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
          'w-64 bg-white border-l border-gray-200 flex-shrink-0',
          'transform transition-transform duration-200 ease-in-out',
          'md:translate-x-0 md:h-screen',
          open ? 'translate-x-0' : 'translate-x-full md:translate-x-0'
        )}
        dir="rtl"
      >
        {/* Logo */}
        <div className="h-16 flex items-center justify-between px-5 border-b border-gray-100">
          <Link href="/dashboard" className="flex items-center gap-2" onClick={onClose}>
            <div className="h-9 w-9 rounded-lg bg-blue-600 text-white flex items-center justify-center font-bold">
              ERP
            </div>
            <div>
              <p className="font-bold text-gray-800 leading-tight">ERP-SYSTEM</p>
              <p className="text-[10px] text-gray-500">v1.0.34-hotfix2 · Phase 6.3</p>
            </div>
          </Link>
          <button
            onClick={onClose}
            className="md:hidden text-gray-400 hover:text-gray-600 p-1"
            aria-label="إغلاق القائمة"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Navigation */}
        <nav className="overflow-y-auto h-[calc(100vh-4rem)] py-4 px-3">
          {NAV_GROUPS.map((group, gi) => (
            <div key={gi} className={cn(gi > 0 && 'mt-5')}>
              {group.label && (
                <p className="px-3 mb-2 text-[10px] font-semibold text-gray-400 uppercase tracking-wider">
                  {group.label}
                </p>
              )}
              <ul className="space-y-1">
                {group.items.map((item) => {
                  const Icon = item.icon;
                  // active إذا الـ pathname يطابق الـ href أو يبدأ به
                  const active = pathname === item.href || pathname?.startsWith(item.href + '/');
                  return (
                    <li key={item.href}>
                      <Link
                        href={item.href}
                        onClick={onClose}
                        className={cn(
                          'flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors',
                          active
                            ? 'bg-blue-50 text-blue-700'
                            : 'text-gray-600 hover:bg-gray-50 hover:text-gray-800'
                        )}
                      >
                        <Icon className={cn('h-4 w-4 flex-shrink-0', active ? 'text-blue-600' : 'text-gray-400')} />
                        <span>{item.label}</span>
                      </Link>
                    </li>
                  );
                })}
              </ul>
            </div>
          ))}
        </nav>
      </aside>
    </>
  );
}

// ============ Topbar ============

interface TopbarProps {
  onMenuClick: () => void;
  userName: string;
  userEmail: string;
  onLogout: () => void;
}

function Topbar({ onMenuClick, userName, userEmail, onLogout }: TopbarProps) {
  const [userMenu, setUserMenu] = useState(false);
  const initials = userName
    .split(' ')
    .map((s) => s[0])
    .filter(Boolean)
    .slice(0, 2)
    .join('');

  return (
    <header className="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-4 md:px-6 sticky top-0 z-20">
      <div className="flex items-center gap-3 min-w-0 flex-1">
        <button
          onClick={onMenuClick}
          className="md:hidden text-gray-600 hover:text-gray-800 p-1.5 rounded-lg hover:bg-gray-100"
          aria-label="فتح القائمة"
        >
          <Menu className="h-5 w-5" />
        </button>
        <Link href="/dashboard" className="md:hidden text-lg font-bold text-gray-800">
          ERP
        </Link>
        {/* Phase 6.3: Company switcher (drives X-Company-Id on every request) */}
        <div className="hidden md:block flex-shrink-0">
          <CompanySwitcher />
        </div>
        {/* Sprint 5 (Phase 5.1): Global search — Cmd/Ctrl+K to focus. Hidden on
            phones (≤ sm) to keep the topbar readable. */}
        {/* Sprint 22: GlobalSearch + NotificationBell removed (dead modules). */}
      </div>

      <div className="flex items-center gap-2 flex-shrink-0">
        <div className="relative">
          <button
            onClick={() => setUserMenu((v) => !v)}
            className="flex items-center gap-2 p-1.5 rounded-lg hover:bg-gray-100"
          >
            <div className="h-8 w-8 rounded-full bg-blue-100 text-blue-700 flex items-center justify-center text-sm font-bold">
              {initials || '؟'}
            </div>
            <div className="hidden sm:block text-right">
              <p className="text-sm font-semibold text-gray-800 leading-tight">{userName}</p>
              <p className="text-[10px] text-gray-500 leading-tight">{userEmail}</p>
            </div>
            <ChevronLeft className={cn('h-4 w-4 text-gray-400 transition-transform', !userMenu && 'rotate-180')} />
          </button>

          {userMenu && (
            <>
              <div className="fixed inset-0 z-10" onClick={() => setUserMenu(false)} />
              <div className="absolute left-0 mt-2 w-56 bg-white rounded-lg shadow-lg border border-gray-100 py-1 z-20">
                <div className="px-4 py-2 border-b border-gray-100 sm:hidden">
                  <p className="text-sm font-semibold text-gray-800">{userName}</p>
                  <p className="text-xs text-gray-500">{userEmail}</p>
                </div>
                <Link
                  href="/admin/users"
                  onClick={() => setUserMenu(false)}
                  className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                >
                  <Users className="h-4 w-4" />
                  <span>إدارة المستخدمين</span>
                </Link>
                <Link
                  href="/admin/companies"
                  onClick={() => setUserMenu(false)}
                  className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                >
                  <Building2 className="h-4 w-4" />
                  <span>إدارة الشركات</span>
                </Link>
                <Link
                  href="/admin/audit"
                  onClick={() => setUserMenu(false)}
                  className="w-full flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50"
                >
                  <Shield className="h-4 w-4" />
                  <span>سجل التدقيق</span>
                </Link>
                <div className="border-t border-gray-100 my-1" />
                <button
                  onClick={() => {
                    setUserMenu(false);
                    onLogout();
                  }}
                  className="w-full text-right flex items-center gap-2 px-4 py-2 text-sm text-red-600 hover:bg-red-50"
                >
                  <LogOut className="h-4 w-4" />
                  <span>تسجيل الخروج</span>
                </button>
              </div>
            </>
          )}
        </div>
      </div>
    </header>
  );
}

// ============ AppShell ============

export interface AppShellProps {
  children: ReactNode;
}

export function AppShell({ children }: AppShellProps) {
  const router = useRouter();
  const [sidebarOpen, setSidebarOpen] = useState(false);
  // Fix hydration mismatch: read from localStorage only on client (after mount)
  const [user, setUser] = useState<{ fullName: string; email: string } | null>(null);
  useEffect(() => {
    setUser(authApi.getUser());
  }, []);
  const userName = user?.fullName || '';
  const userEmail = user?.email || '';

  const onLogout = () => {
    authApi.logout();
    router.push('/login');
  };

  return (
    <div className="min-h-screen bg-gray-50 flex" dir="rtl">
      <Sidebar open={sidebarOpen} onClose={() => setSidebarOpen(false)} />
      <div className="flex-1 flex flex-col min-w-0">
        <Topbar
          onMenuClick={() => setSidebarOpen(true)}
          userName={userName}
          userEmail={userEmail}
          onLogout={onLogout}
        />
        <main className="flex-1 p-4 md:p-6 max-w-7xl w-full mx-auto">{children}</main>
      </div>
    </div>
  );
}
