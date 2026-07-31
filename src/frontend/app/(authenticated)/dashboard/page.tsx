'use client';

// Sprint 1 + Sprint 5 (Phase 4) — Holding-level Dashboard.
//
// Sprint 1: 4 KPI tiles (companies / users / activities_today / transactions).
// Sprint 5: adds 3 Recharts visualisations (revenue line, expense pie,
// top-customers bar) and ↑ / ↓ trend arrows on each KPI tile.
//
// Each chart has its own loading + empty state — we tolerate the BE being
// down (404) by swallowing the chart errors silently (we just render the
// empty state for that chart, not a full-page error). The main KPI summary
// keeps the original Sprint-1 error path because it's the page's primary
// signal.

import { useEffect, useState } from 'react';
import {
  Building2,
  Users,
  Activity,
  ArrowLeftRight,
  AlertCircle,
  RefreshCw,
} from 'lucide-react';
import { Card, PageHeader, Button, SkeletonCard } from '@/components/ui';
import { CompanySwitcher } from '@/components/layout/CompanySwitcher';
import { useAuth } from '@/lib/useAuth';
import {
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
  key: keyof Pick<
    DashboardSummary,
    'companies' | 'users' | 'activities_today' | 'transactions'
  >;
  /** Field on DashboardSummary that carries the matching trend pct. */
  trendKey: keyof Pick<
    DashboardSummary,
    | 'companiesTrendPct'
    | 'usersTrendPct'
    | 'activitiesTrendPct'
    | 'transactionsTrendPct'
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
    key: 'companies',
    trendKey: 'companiesTrendPct',
    label: 'الشركات',
    hint: 'تحت القابضة',
    icon: Building2,
    accent: 'blue',
    iconBg: 'bg-blue-50',
    iconColor: 'text-blue-600',
  },
  {
    key: 'users',
    trendKey: 'usersTrendPct',
    label: 'المستخدمون',
    hint: 'فعّالون',
    icon: Users,
    accent: 'green',
    iconBg: 'bg-green-50',
    iconColor: 'text-green-600',
  },
  {
    key: 'activities_today',
    trendKey: 'activitiesTrendPct',
    label: 'نشاطات اليوم',
    hint: 'آخر 24 ساعة',
    icon: Activity,
    accent: 'purple',
    iconBg: 'bg-purple-50',
    iconColor: 'text-purple-600',
  },
  {
    key: 'transactions',
    trendKey: 'transactionsTrendPct',
    label: 'المعاملات',
    hint: 'آخر 30 يوم',
    icon: ArrowLeftRight,
    accent: 'yellow',
    iconBg: 'bg-yellow-50',
    iconColor: 'text-yellow-600',
  },
];

export default function DashboardPage() {
  const { user } = useAuth();
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Sprint 5: chart state. Each chart has its own loading flag so a single
  // 404 on one endpoint doesn't block the others.
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

  const loadCharts = async () => {
    // Fire all 3 in parallel; each sets its own state so a failure on one
    // doesn't sink the others. We swallow 404s by leaving state as-is (the
    // chart component renders its own empty state).
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
    void loadCharts();
  }, []);

  // Format numbers with Arabic locale (so 1,234 becomes ١٬٢٣٤ visually)
  // Falls back to en-US if Intl is unavailable.
  const formatNumber = (n: number | undefined | null): string => {
    if (n == null) return '—';
    try {
      return n.toLocaleString('ar-EG');
    } catch {
      return n.toLocaleString('en-US');
    }
  };

  const firstName = (user?.fullName ?? '').split(' ')[0] || 'مستخدم';

  // Single refresh handler that re-fetches summary + charts in one click.
  const refresh = () => {
    void loadSummary();
    void loadCharts();
  };

  return (
    <div>
      <PageHeader
        title={`مرحباً، ${firstName} 👋`}
        description="نظرة عامة على نشاط القابضة والشركات التابعة"
        actions={
          <div className="flex items-center gap-2">
            <CompanySwitcher />
            <Button
              variant="secondary"
              onClick={refresh}
              disabled={loading}
              aria-label="تحديث"
            >
              <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
              <span className="hidden sm:inline">تحديث</span>
            </Button>
          </div>
        }
      />

      {/* Error state — رسالة ودودة بالعربي لو الـ endpoint غير متاح */}
      {error && !loading && (
        <div
          className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 flex items-start gap-3"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0 mt-0.5" />
          <div className="flex-1">
            <p className="font-semibold">تعذّر تحميل لوحة التحكم</p>
            <p className="text-sm mt-0.5">
              {error} — Endpoint: <code className="text-xs">/api/dashboard/summary</code>
            </p>
            <p className="text-xs mt-1 text-red-600">
              ملاحظة: في وضع التطوير قد يكون الـ endpoint غير مُفعَّل بعد.
            </p>
          </div>
          <Button variant="secondary" onClick={loadSummary} disabled={loading}>
            إعادة المحاولة
          </Button>
        </div>
      )}

      {/* Sprint 5: trend-loaded guard. The summary object always has the 4
          number fields, but the *TrendPct fields are optional (BE may omit
          them). We still render <KpiTrend> unconditionally — it hides itself
          when the value is null. */}
      {summary && !error && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
          {KPI_TILES.map((tile) => {
            const Icon = tile.icon;
            const value = loading
              ? null
              : (summary?.[tile.key] ?? null);
            const trendValue = summary?.[tile.trendKey];

            return (
              <Card key={tile.key} accent={tile.accent}>
                <div className="flex items-start justify-between">
                  <div className="min-w-0 flex-1">
                    <p className="text-sm text-gray-500">{tile.label}</p>
                    {value == null ? (
                      <div
                        className="mt-2 h-9 w-20 rounded bg-gray-100 animate-pulse"
                        role="status"
                        aria-label="جاري التحميل"
                      />
                    ) : (
                      <p className="text-3xl font-bold text-gray-800 mt-1 tabular-nums">
                        {formatNumber(value)}
                      </p>
                    )}
                    <KpiTrend value={trendValue} loading={loading} />
                    <p className="text-xs text-gray-400 mt-1">{tile.hint}</p>
                  </div>
                  <div
                    className={`h-12 w-12 rounded-lg ${tile.iconBg} flex items-center justify-center flex-shrink-0`}
                  >
                    <Icon className={`h-6 w-6 ${tile.iconColor}`} />
                  </div>
                </div>
              </Card>
            );
          })}
        </div>
      )}

      {/* Initial loading state for the KPI grid (replaces the empty grid) */}
      {loading && !summary && !error && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
          {KPI_TILES.map((tile) => (
            <SkeletonCard key={tile.key} hasHeader={false} lines={2} />
          ))}
        </div>
      )}

      {/* Sprint 5 (Phase 4.1) — Charts row 1: revenue line + expenses pie */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 mb-4">
        <RevenueChart data={revenue} loading={revenueLoading} />
        <ExpenseByCategoryChart data={expenses} loading={expensesLoading} />
      </div>

      {/* Sprint 5 (Phase 4.1) — Charts row 2: top customers bar (full width) */}
      <div className="mb-6">
        <TopCustomersChart data={topCustomers} loading={topCustomersLoading} />
      </div>

      {/* Sprint 1: "as of" timestamp when present */}
      {summary?.asOf && !error && (
        <div className="text-xs text-gray-400 text-center mt-2">
          آخر تحديث: {new Date(summary.asOf).toLocaleString('en-GB')}
        </div>
      )}
    </div>
  );
}
