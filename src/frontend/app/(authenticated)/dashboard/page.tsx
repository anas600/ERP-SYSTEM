'use client';

// Sprint 1: Holding-level Dashboard — 4 KPI tiles (companies / users /
// activities_today / transactions). Replaces the per-company dashboard from
// Phase 3, which showed per-company counts (vendors / POs / employees /
// low-stock). The new design reflects the Multi-Company / Holding model of
// Phase 6: the active company (X-Company-Id) drives the scope, and when the
// active company is the Holding the KPIs aggregate across all sub-companies.
//
// Note: the underlying endpoint (`GET /api/dashboard/summary`) is being wired
// by the Backend Jimi in parallel. The page shows a friendly Arabic error
// message until that endpoint is live — no mock data is used.

import { useEffect, useState } from 'react';
import {
  Building2,
  Users,
  Activity,
  ArrowLeftRight,
  AlertCircle,
  RefreshCw,
} from 'lucide-react';
import { Card, PageHeader, Button } from '@/components/ui';
import { CompanySwitcher } from '@/components/layout/CompanySwitcher';
import { useAuth } from '@/lib/useAuth';
import { dashboardApi, getErrorMessage } from '@/lib/api';

interface KpiTile {
  key: 'companies' | 'users' | 'activities_today' | 'transactions';
  label: string;
  /** توضيح قصير يظهر تحت الرقم */
  hint: string;
  icon: React.ComponentType<{ className?: string }>;
  /** لون الـ accent — يطابق الـ palette الموجود في Card */
  accent: 'blue' | 'green' | 'purple' | 'yellow' | 'red';
  /** color of the icon background ring */
  iconBg: string;
  iconColor: string;
}

const KPI_TILES: KpiTile[] = [
  {
    key: 'companies',
    label: 'الشركات',
    hint: 'تحت القابضة',
    icon: Building2,
    accent: 'blue',
    iconBg: 'bg-blue-50',
    iconColor: 'text-blue-600',
  },
  {
    key: 'users',
    label: 'المستخدمون',
    hint: 'فعّالون',
    icon: Users,
    accent: 'green',
    iconBg: 'bg-green-50',
    iconColor: 'text-green-600',
  },
  {
    key: 'activities_today',
    label: 'نشاطات اليوم',
    hint: 'آخر 24 ساعة',
    icon: Activity,
    accent: 'purple',
    iconBg: 'bg-purple-50',
    iconColor: 'text-purple-600',
  },
  {
    key: 'transactions',
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
  const [summary, setSummary] = useState<Awaited<
    ReturnType<typeof dashboardApi.getSummary>
  > | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
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

  useEffect(() => {
    load();
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

  return (
    <div>
      {/* Page header — مع CompanySwitcher في الـ actions عشان الـ user يقدر
          يبدّل بين الشركات من هنا أيضاً (إضافة لـ Topbar) */}
      <PageHeader
        title={`مرحباً، ${firstName} 👋`}
        description="نظرة عامة على نشاط القابضة والشركات التابعة"
        actions={
          <div className="flex items-center gap-2">
            <CompanySwitcher />
            <Button
              variant="secondary"
              onClick={load}
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
          <Button variant="secondary" onClick={load} disabled={loading}>
            إعادة المحاولة
          </Button>
        </div>
      )}

      {/* KPI tiles — 4 بطاقات responsive (1 col mobile, 2 tablet, 4 desktop) */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        {KPI_TILES.map((tile) => {
          const Icon = tile.icon;
          const value = loading
            ? null
            : error
              ? null
              : (summary?.[tile.key] ?? null);

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

      {/* ملخص إضافي — يعرض "as of" timestamp لو متاح من الـ backend */}
      {summary?.asOf && !error && (
        <div className="text-xs text-gray-400 text-center mt-2">
          آخر تحديث: {new Date(summary.asOf).toLocaleString('en-GB')}
        </div>
      )}
    </div>
  );
}
