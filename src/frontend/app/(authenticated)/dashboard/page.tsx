'use client';

// Sprint 1 + Sprint 5 (Phase 4) + Sprint 59 — Holding-level Dashboard.
//
// Sprint 1: 4 KPI tiles (companies / users / activities_today / transactions).
// Sprint 5: 3 Recharts visualisations (revenue line, expense pie,
// top-customers bar) and ↑ / ↓ trend arrows on each KPI tile.
// Sprint 59: Added an "Inventory at a glance" quick-access section so
// accountants can jump to the inventory pages (items, stock levels,
// movements, reservations) and see their headline numbers without leaving
// the dashboard. Each card is a real <Link> for fast nav.

import { useEffect, useState } from 'react';
import Link from 'next/link';
import {
  Building2, Users, Activity, ArrowLeftRight, AlertCircle, RefreshCw,
  Package, Layers, ArrowRightLeft, Lock, ArrowUpLeft, ChevronLeft,
} from 'lucide-react';
import {
  Card, PageHeader, Button, SkeletonCard, SectionCard, StatCard, StatusPill,
} from '@/components/ui';
import { CompanySwitcher } from '@/components/layout/CompanySwitcher';
import { useAuth } from '@/lib/useAuth';
import {
  authedFetch,
  dashboardApi,
  getErrorMessage,
  RevenueVsExpensePoint,
  ExpenseCategorySlice,
  TopCustomerChartRow,
  DashboardSummary,
} from '@/lib/api';
import { RevenueChart } from '@/components/charts/RevenueChart';
import { ExpenseByCategoryChart } from '@/components/charts/ExpenseByCategoryChart';
import { TopCustomersChart } from '@/components/charts/TopCustomersChart';
import { KpiTrend } from '@/components/charts/KpiTrend';

interface KpiTile {
  key: keyof Pick<DashboardSummary, 'companies' | 'users' | 'activities_today' | 'transactions'>;
  trendKey: keyof Pick<
    DashboardSummary,
    'companiesTrendPct' | 'usersTrendPct' | 'activitiesTrendPct' | 'transactionsTrendPct'
  >;
  label: string;
  hint: string;
  icon: React.ComponentType<{ className?: string }>;
  accent: 'blue' | 'green' | 'purple' | 'yellow' | 'red';
  iconBg: string;
  iconColor: string;
}

const KPI_TILES: KpiTile[] = [
  {
    key: 'companies', trendKey: 'companiesTrendPct',
    label: 'الشركات', hint: 'تحت القابضة',
    icon: Building2, accent: 'blue', iconBg: 'bg-blue-50', iconColor: 'text-blue-600',
  },
  {
    key: 'users', trendKey: 'usersTrendPct',
    label: 'المستخدمون', hint: 'فعّالون',
    icon: Users, accent: 'green', iconBg: 'bg-green-50', iconColor: 'text-green-600',
  },
  {
    key: 'activities_today', trendKey: 'activitiesTrendPct',
    label: 'نشاطات اليوم', hint: 'آخر 24 ساعة',
    icon: Activity, accent: 'purple', iconBg: 'bg-purple-50', iconColor: 'text-purple-600',
  },
  {
    key: 'transactions', trendKey: 'transactionsTrendPct',
    label: 'المعاملات', hint: 'آخر 30 يوم',
    icon: ArrowLeftRight, accent: 'yellow', iconBg: 'bg-yellow-50', iconColor: 'text-yellow-600',
  },
];

interface InventoryKpi {
  items: number;
  activeItems: number;
  totalValue: number;
  lowStock: number;
  outOfStock: number;
  movements: number;
  reservations: number;
}

const EMPTY_INV: InventoryKpi = {
  items: 0, activeItems: 0, totalValue: 0,
  lowStock: 0, outOfStock: 0, movements: 0, reservations: 0,
};

const formatMoney = (n: number) => n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const formatNumber = (n: number) => n.toLocaleString('en-US');

export default function DashboardPage() {
  const { user } = useAuth();
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Inventory at-a-glance (Sprint 59)
  const [inv, setInv] = useState<InventoryKpi>(EMPTY_INV);
  const [invLoading, setInvLoading] = useState(true);

  const [revenue, setRevenue] = useState<RevenueVsExpensePoint[] | null>(null);
  const [revenueLoading, setRevenueLoading] = useState(true);
  const [expenses, setExpenses] = useState<ExpenseCategorySlice[] | null>(null);
  const [expensesLoading, setExpensesLoading] = useState(true);
  const [topCustomers, setTopCustomers] = useState<TopCustomerChartRow[] | null>(null);
  const [topCustomersLoading, setTopCustomersLoading] = useState(true);

  const loadSummary = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await dashboardApi.getSummary();
      setSummary(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'تعذّر تحميل لوحة التحكم.'));
    } finally {
      setLoading(false);
    }
  };

  const loadInventory = async () => {
    setInvLoading(true);
    try {
      const [itemsRes, levelsRes, movementsRes, reservationsRes] = await Promise.all([
        authedFetch('/api/inventory/items', { cache: 'no-store' }),
        authedFetch('/api/inventory/levels', { cache: 'no-store' }),
        authedFetch('/api/inventory/movements', { cache: 'no-store' }),
        authedFetch('/api/inventory/reservations', { cache: 'no-store' }),
      ]);
      const items = itemsRes.ok ? await itemsRes.json() : [];
      const levels = levelsRes.ok ? await levelsRes.json() : [];
      const movements = movementsRes.ok ? await movementsRes.json() : [];
      const reservations = reservationsRes.ok ? await reservationsRes.json() : [];

      const itemList = Array.isArray(items) ? items : [];
      const levelList = Array.isArray(levels) ? levels : [];
      const movementList = Array.isArray(movements) ? movements : [];
      const reservationList = Array.isArray(reservations) ? reservations : [];

      const activeItems = itemList.filter((i: { isActive?: boolean }) => i.isActive).length;
      const stockByItem = new Map<string, { onHand: number; available: number; reorder: number }>();
      for (const l of levelList) {
        const cur = stockByItem.get(l.itemId) ?? { onHand: 0, available: 0, reorder: l.reorderLevel ?? 0 };
        cur.onHand += Number(l.quantityOnHand || 0);
        cur.available += Number(l.quantityAvailable || 0);
        cur.reorder = Math.max(cur.reorder, l.reorderLevel ?? 0);
        stockByItem.set(l.itemId, cur);
      }
      let lowStock = 0;
      let outOfStock = 0;
      for (const i of itemList) {
        const s = stockByItem.get(i.id);
        if (!s || s.onHand <= 0) {
          outOfStock++;
        } else if (s.reorder > 0 && s.available <= s.reorder) {
          lowStock++;
        }
      }
      const totalValue = levelList.reduce(
        (sum: number, l: { quantityOnHand?: number; averageCost?: number }) =>
          sum + Number(l.quantityOnHand || 0) * Number(l.averageCost || 0),
        0,
      );

      setInv({
        items: itemList.length,
        activeItems,
        totalValue,
        lowStock,
        outOfStock,
        movements: movementList.length,
        reservations: reservationList.length,
      });
    } catch {
      setInv(EMPTY_INV);
    } finally {
      setInvLoading(false);
    }
  };

  const loadCharts = async () => {
    setRevenueLoading(true);
    setExpensesLoading(true);
    setTopCustomersLoading(true);

    void dashboardApi
      .getRevenueChart(6)
      .then((r) => setRevenue(r))
      .catch(() => setRevenue([]))
      .finally(() => setRevenueLoading(false));

    void dashboardApi
      .getExpenseByCategory(3)
      .then((r) => setExpenses(r))
      .catch(() => setExpenses([]))
      .finally(() => setExpensesLoading(false));

    void dashboardApi
      .getTopCustomers(5)
      .then((r) => setTopCustomers(r))
      .catch(() => setTopCustomers([]))
      .finally(() => setTopCustomersLoading(false));
  };

  useEffect(() => {
    void loadSummary();
    void loadInventory();
    void loadCharts();
  }, []);

  const firstName = (user?.fullName ?? '').split(' ')[0] || 'مستخدم';
  const refresh = () => {
    void loadSummary();
    void loadInventory();
    void loadCharts();
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={`مرحباً، ${firstName} 👋`}
        description="نظرة عامة على نشاط القابضة والشركات التابعة"
        actions={
          <div className="flex items-center gap-2">
            <CompanySwitcher />
            <Button variant="secondary" onClick={refresh} disabled={loading} aria-label="تحديث">
              <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
              <span className="hidden sm:inline">تحديث</span>
            </Button>
          </div>
        }
      />

      {error && !loading && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg flex items-start gap-3" role="alert">
          <AlertCircle className="h-5 w-5 flex-shrink-0 mt-0.5" />
          <div className="flex-1">
            <p className="font-semibold">تعذّر تحميل لوحة التحكم</p>
            <p className="text-sm mt-0.5">
              {error} — Endpoint: <code className="text-xs">/api/dashboard/summary</code>
            </p>
          </div>
          <Button variant="secondary" onClick={loadSummary} disabled={loading}>
            إعادة المحاولة
          </Button>
        </div>
      )}

      {/* Top-level KPIs */}
      {summary && !error && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {KPI_TILES.map((tile) => {
            const Icon = tile.icon;
            const value = loading ? null : (summary?.[tile.key] ?? null);
            const trendValue = summary?.[tile.trendKey];
            return (
              <Card key={tile.key} accent={tile.accent}>
                <div className="flex items-start justify-between">
                  <div className="min-w-0 flex-1">
                    <p className="text-sm text-gray-500">{tile.label}</p>
                    {value == null ? (
                      <div className="mt-2 h-9 w-20 rounded bg-gray-100 animate-pulse" />
                    ) : (
                      <p className="text-3xl font-bold text-gray-800 mt-1 tabular-nums">
                        {formatNumber(value)}
                      </p>
                    )}
                    <KpiTrend value={trendValue} loading={loading} />
                    <p className="text-xs text-gray-400 mt-1">{tile.hint}</p>
                  </div>
                  <div className={`h-12 w-12 rounded-lg ${tile.iconBg} flex items-center justify-center flex-shrink-0`}>
                    <Icon className={`h-6 w-6 ${tile.iconColor}`} />
                  </div>
                </div>
              </Card>
            );
          })}
        </div>
      )}

      {loading && !summary && !error && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {KPI_TILES.map((tile) => (
            <SkeletonCard key={tile.key} hasHeader={false} lines={2} />
          ))}
        </div>
      )}

      {/* Sprint 59 — Inventory at a glance (quick nav) */}
      <SectionCard
        title="نظرة سريعة على المخزون"
        description="أرقام رئيسية + اختصارات للشاشات الأكثر استخداماً للمحاسبين"
      >
        <div className="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-4">
          <InventoryQuickCard
            href="/inventory/items"
            title="الأصناف"
            description="كتالوج المنتجات والتسعير"
            icon={Package}
            tone="emerald"
            primaryValue={invLoading ? '…' : formatNumber(inv.items)}
            primaryLabel="إجمالي الأصناف"
            secondary={invLoading ? '…' : `${formatNumber(inv.activeItems)} نشط`}
            secondaryTone="green"
          />
          <InventoryQuickCard
            href="/inventory/stock-levels"
            title="مستويات المخزون"
            description="الكميات في المستودعات"
            icon={Layers}
            tone="blue"
            primaryValue={invLoading ? '…' : `${formatMoney(inv.totalValue)} ل.د`}
            primaryLabel="قيمة المخزون"
            secondary={
              invLoading
                ? '…'
                : inv.lowStock + inv.outOfStock > 0
                  ? `${inv.lowStock + inv.outOfStock} يحتاج متابعة`
                  : 'الحالة ممتازة'
            }
            secondaryTone={inv.lowStock + inv.outOfStock > 0 ? 'amber' : 'green'}
            warning={inv.lowStock + inv.outOfStock > 0}
          />
          <InventoryQuickCard
            href="/inventory/movements"
            title="حركات المخزون"
            description="استلام / صرف / تحويل / تسوية"
            icon={ArrowRightLeft}
            tone="slate"
            primaryValue={invLoading ? '…' : formatNumber(inv.movements)}
            primaryLabel="إجمالي الحركات"
            secondary="آخر 30 يوم"
            secondaryTone="slate"
          />
          <InventoryQuickCard
            href="/inventory/reservations"
            title="الحجوزات"
            description="حجوزات قصيرة المدى على المخزون"
            icon={Lock}
            tone="violet"
            primaryValue={invLoading ? '…' : formatNumber(inv.reservations)}
            primaryLabel="حجوزات نشطة"
            secondary="تلقائياً تُحرر عند الانتهاء"
            secondaryTone="violet"
          />
        </div>
      </SectionCard>

      {/* Charts row 1 */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <RevenueChart data={revenue} loading={revenueLoading} />
        <ExpenseByCategoryChart data={expenses} loading={expensesLoading} />
      </div>

      {/* Charts row 2 */}
      <div>
        <TopCustomersChart data={topCustomers} loading={topCustomersLoading} />
      </div>

      {summary?.asOf && !error && (
        <div className="text-xs text-gray-400 text-center mt-2">
          آخر تحديث: {new Date(summary.asOf).toLocaleString('en-GB')}
        </div>
      )}
    </div>
  );
}

// ============ Sub-component ============

function InventoryQuickCard({
  href,
  title,
  description,
  icon: Icon,
  tone,
  primaryValue,
  primaryLabel,
  secondary,
  secondaryTone,
  warning,
}: {
  href: string;
  title: string;
  description: string;
  icon: React.ComponentType<{ className?: string }>;
  tone: 'blue' | 'green' | 'amber' | 'red' | 'purple' | 'slate' | 'emerald' | 'indigo' | 'violet';
  primaryValue: string;
  primaryLabel: string;
  secondary?: string;
  secondaryTone?: 'green' | 'red' | 'amber' | 'slate' | 'violet';
  warning?: boolean;
}) {
  const ring = {
    blue: 'hover:ring-blue-200',
    green: 'hover:ring-emerald-200',
    emerald: 'hover:ring-emerald-200',
    amber: 'hover:ring-amber-200',
    red: 'hover:ring-rose-200',
    purple: 'hover:ring-violet-200',
    violet: 'hover:ring-violet-200',
    slate: 'hover:ring-slate-200',
    indigo: 'hover:ring-indigo-200',
  }[tone];
  const iconBg = {
    blue: 'bg-blue-100 text-blue-600',
    green: 'bg-emerald-100 text-emerald-600',
    emerald: 'bg-emerald-100 text-emerald-600',
    amber: 'bg-amber-100 text-amber-600',
    red: 'bg-rose-100 text-rose-600',
    purple: 'bg-violet-100 text-violet-600',
    violet: 'bg-violet-100 text-violet-600',
    slate: 'bg-slate-100 text-slate-600',
    indigo: 'bg-indigo-100 text-indigo-600',
  }[tone];

  return (
    <Link
      href={href}
      className={`group relative block overflow-hidden rounded-2xl bg-white p-4 shadow-sm ring-1 ring-gray-200/70 transition-all hover:-translate-y-0.5 hover:shadow-md ${ring}`}
    >
      {warning && (
        <div className="absolute right-0 top-0 h-full w-1 bg-amber-400" aria-hidden="true" />
      )}
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0 flex-1">
          <p className="text-[10px] font-semibold uppercase tracking-wider text-gray-400">المخزون</p>
          <h4 className="mt-0.5 text-sm font-bold text-gray-900">{title}</h4>
          <p className="mt-1 line-clamp-1 text-[11px] text-gray-500">{description}</p>
        </div>
        <div className={`flex h-9 w-9 flex-shrink-0 items-center justify-center rounded-lg ${iconBg}`}>
          <Icon className="h-4 w-4" />
        </div>
      </div>
      <div className="mt-3">
        <p className="text-[10px] font-semibold uppercase tracking-wider text-gray-500">{primaryLabel}</p>
        <p className="mt-0.5 text-xl font-extrabold text-gray-900 tabular-nums">{primaryValue}</p>
      </div>
      {secondary && (
        <div className="mt-2 flex items-center gap-1.5 text-[11px]">
          {warning && <AlertCircle className="h-3 w-3 text-amber-500" />}
          {secondaryTone && (
            <StatusPill tone={secondaryTone as 'green' | 'red' | 'amber' | 'slate' | 'violet'} label={secondary} showDot={false} />
          )}
        </div>
      )}
      <div className="mt-3 flex items-center justify-end text-[11px] font-semibold text-gray-400 transition-colors group-hover:text-blue-600">
        <span>فتح</span>
        <ChevronLeft className="ms-1 h-3 w-3 transition-transform group-hover:-translate-x-0.5" />
      </div>
    </Link>
  );
}
