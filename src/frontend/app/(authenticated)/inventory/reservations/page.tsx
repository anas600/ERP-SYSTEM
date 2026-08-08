'use client';

// صفحة حجوزات المخزون (Reservations) — Sprint 59 redesign
//
// Reservations are short-lived holds on stock tied to a reference (PO line,
// project material, etc.). The page emphasises the expiry state because
// expired reservations should be released promptly to free up available stock.

import { useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import {
  Plus, Search, Lock, AlertTriangle, CheckCircle2, Clock,
  Package, Warehouse, Hash, Timer, CalendarClock, FileText,
} from 'lucide-react';
import {
  PageHero, StatCard, StatusPill, SectionCard,
  Button, EmptyState, SkeletonCard, SkeletonTable,
} from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { authedFetch, getErrorMessage } from '@/lib/api';

interface StockReservation {
  id: string;
  itemId: string;
  warehouseId: string;
  quantity: number;
  referenceType: string;
  referenceId: string;
  expiresAt: string;
  createdAt: string;
}

type TabKey = 'all' | 'active' | 'expiring' | 'expired';

function formatNumber(n: number): string {
  return n.toLocaleString('en-US');
}

function timeUntil(iso: string): { ms: number; label: string; tone: 'green' | 'amber' | 'red' | 'slate' } {
  const ms = new Date(iso).getTime() - Date.now();
  if (ms <= 0) {
    const sinceMs = -ms;
    const days = Math.floor(sinceMs / (1000 * 60 * 60 * 24));
    return { ms, label: days > 0 ? `منتهية منذ ${days} يوم` : 'منتهية الآن', tone: 'red' };
  }
  const hours = Math.floor(ms / (1000 * 60 * 60));
  if (hours < 24) {
    return { ms, label: hours > 0 ? `متبقي ${hours} ساعة` : 'أقل من ساعة', tone: 'amber' };
  }
  const days = Math.floor(hours / 24);
  return { ms, label: `متبقي ${days} يوم`, tone: 'green' };
}

export default function ReservationsPage() {
  const { loading: authLoading } = useAuth();
  const [items, setItems] = useState<StockReservation[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tab, setTab] = useState<TabKey>('all');
  const [search, setSearch] = useState('');

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await authedFetch('/api/inventory/reservations', { cache: 'no-store' });
      if (!res.ok) throw new Error('فشل التحميل');
      const data = await res.json();
      setItems(Array.isArray(data) ? data : []);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل التحميل'));
    } finally {
      setLoading(false);
    }
  };

  const classified = useMemo(() => {
    const now = Date.now();
    return items.map((r) => {
      const t = timeUntil(r.expiresAt);
      const state: 'expired' | 'expiring' | 'active' =
        t.ms <= 0 ? 'expired' : t.ms < 1000 * 60 * 60 * 24 ? 'expiring' : 'active';
      return { ...r, _state: state, _time: t };
    });
  }, [items]);

  const counts = useMemo(() => {
    const c: Record<TabKey, number> = {
      all: items.length,
      active: classified.filter((r) => r._state === 'active').length,
      expiring: classified.filter((r) => r._state === 'expiring').length,
      expired: classified.filter((r) => r._state === 'expired').length,
    };
    return c;
  }, [items, classified]);

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    return classified.filter((r) => {
      if (tab !== 'all' && r._state !== tab) return false;
      if (q) {
        const hay = `${r.id} ${r.itemId} ${r.warehouseId} ${r.referenceType} ${r.referenceId}`.toLowerCase();
        if (!hay.includes(q)) return false;
      }
      return true;
    });
  }, [classified, tab, search]);

  const totalQty = items.reduce((s, r) => s + Number(r.quantity || 0), 0);
  const activeQty = classified.filter((r) => r._state === 'active').reduce((s, r) => s + Number(r.quantity), 0);
  const expiredQty = classified.filter((r) => r._state === 'expired').reduce((s, r) => s + Number(r.quantity), 0);

  const tabDefs: { key: TabKey; label: string; tone: 'slate' | 'green' | 'amber' | 'red'; icon: React.ComponentType<{ className?: string }> }[] = [
    { key: 'all',      label: 'الكل',          tone: 'slate', icon: Hash },
    { key: 'active',   label: 'فعّالة',         tone: 'green', icon: CheckCircle2 },
    { key: 'expiring', label: 'تنتهي قريباً',   tone: 'amber', icon: Timer },
    { key: 'expired',  label: 'منتهية',         tone: 'red',   icon: AlertTriangle },
  ];

  return (
    <div className="space-y-6">
      <PageHero
        eyebrow="إدارة المخزون"
        title="حجوزات المخزون"
        subtitle="حجوزات قصيرة المدى مرتبطة بأوامر صرف أو مواد مشروع — راقِب الانتهاء لتحرير الكمية فوراً"
        tone="violet"
        highlight={
          loading
            ? undefined
            : { label: 'إجمالي الكمية المحجوزة', value: formatNumber(totalQty) }
        }
        actions={
          <>
            <Link href="/inventory/reservations/new">
              <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
                حجز جديد
              </Button>
            </Link>
            <Button variant="secondary" onClick={load} disabled={loading}>
              تحديث
            </Button>
          </>
        }
        toolbar={
          <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
            <div className="relative flex-1 max-w-md">
              <Search className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-white/60" />
              <input
                type="search"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="ابحث بالمعرّف، الصنف، المستودع، المرجع..."
                className="w-full rounded-lg border-0 bg-white/95 px-3 py-2 pe-9 text-sm text-gray-900 placeholder:text-gray-400 shadow-sm focus:outline-none focus:ring-2 focus:ring-white/40"
              />
            </div>
            <p className="text-xs text-white/70">
              الحجوزات تحرر الكمية المتاحة تلقائياً عند انتهاء صلاحيتها.
            </p>
          </div>
        }
      />

      {error && !loading && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-rose-700" role="alert">
          <p className="font-semibold">تعذّر تحميل الحجوزات</p>
          <p className="text-sm mt-1">{error}</p>
        </div>
      )}

      {/* Tab bar with counts */}
      <div className="flex flex-wrap items-center gap-2 rounded-2xl bg-white p-2 shadow-sm ring-1 ring-gray-200/70">
        {tabDefs.map((t) => {
          const Icon = t.icon;
          const active = tab === t.key;
          const activeBg = active
            ? t.tone === 'slate'
              ? 'bg-slate-900 text-white'
              : t.tone === 'green'
                ? 'bg-emerald-600 text-white'
                : t.tone === 'amber'
                  ? 'bg-amber-500 text-white'
                  : 'bg-rose-600 text-white'
            : 'bg-gray-50 text-gray-600 hover:bg-gray-100';
          return (
            <button
              key={t.key}
              onClick={() => setTab(t.key)}
              className={`flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-bold transition ${activeBg}`}
            >
              <Icon className="h-4 w-4" />
              <span>{t.label}</span>
              <span
                className={
                  'rounded-full px-2 py-0.5 text-[11px] font-bold tabular-nums ' +
                  (active ? 'bg-white/20 text-white' : 'bg-white text-gray-500 ring-1 ring-gray-200')
                }
              >
                {counts[t.key]}
              </span>
            </button>
          );
        })}
      </div>

      {/* KPI strip */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <StatCard
          label="إجمالي الحجوزات"
          value={loading ? '…' : formatNumber(items.length)}
          icon={Lock}
          tone="slate"
          hint="في النظام"
        />
        <StatCard
          label="فعّالة"
          value={loading ? '…' : formatNumber(counts.active)}
          icon={CheckCircle2}
          tone="green"
          hint={`كمية: ${formatNumber(activeQty)}`}
        />
        <StatCard
          label="تنتهي خلال 24 ساعة"
          value={loading ? '…' : formatNumber(counts.expiring)}
          icon={Timer}
          tone="amber"
          hint="تحتاج متابعة"
        />
        <StatCard
          label="منتهية"
          value={loading ? '…' : formatNumber(counts.expired)}
          icon={AlertTriangle}
          tone={counts.expired > 0 ? 'red' : 'slate'}
          hint={`كمية محجوزة: ${formatNumber(expiredQty)}`}
        />
      </div>

      {/* List */}
      {loading ? (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <SkeletonCard key={i} hasHeader={false} lines={4} />
          ))}
        </div>
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={<Lock className="h-12 w-12" />}
          title={items.length === 0 ? 'لا توجد حجوزات' : 'لا توجد نتائج'}
          description={
            items.length === 0
              ? 'لم يتم تسجيل أي حجز على المخزون بعد. الحجوزات تُنشأ عادةً مع أوامر الصرف.'
              : 'جرّب توسيع الفلاتر أو البحث بكلمات مختلفة.'
          }
          action={
            items.length === 0 ? (
              <Link href="/inventory/reservations/new">
                <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
                  أول حجز
                </Button>
              </Link>
            ) : (
              <Button
                variant="secondary"
                onClick={() => {
                  setTab('all');
                  setSearch('');
                }}
              >
                مسح الفلاتر
              </Button>
            )
          }
        />
      ) : (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
          {filtered.map((r) => {
            const accent =
              r._time.tone === 'red'
                ? 'from-rose-500 to-rose-600'
                : r._time.tone === 'amber'
                  ? 'from-amber-500 to-amber-600'
                  : r._time.tone === 'green'
                    ? 'from-emerald-500 to-emerald-600'
                    : 'from-slate-500 to-slate-600';
            return (
              <div
                key={r.id}
                className="group relative overflow-hidden rounded-2xl bg-white shadow-sm ring-1 ring-gray-200/70 transition-all hover:-translate-y-0.5 hover:shadow-lg"
              >
                <div className={`h-1 w-full bg-gradient-to-l ${accent}`} />

                <div className="p-5">
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex items-center gap-3 min-w-0">
                      <div className="flex h-11 w-11 flex-shrink-0 items-center justify-center rounded-xl bg-slate-100 text-slate-600">
                        <Lock className="h-5 w-5" />
                      </div>
                      <div className="min-w-0">
                        <p className="text-[10px] font-semibold uppercase tracking-wider text-slate-500">
                          {r.referenceType}
                        </p>
                        <p className="font-mono text-xs font-semibold text-slate-700 truncate" title={r.referenceId}>
                          {r.referenceId.substring(0, 16)}…
                        </p>
                      </div>
                    </div>
                    <StatusPill tone={r._time.tone} label={r._time.label} />
                  </div>

                  <div className="mt-4 grid grid-cols-2 gap-3 text-xs">
                    <div className="rounded-lg bg-slate-50 p-2.5">
                      <p className="text-[10px] font-semibold uppercase tracking-wider text-slate-500">
                        الكمية المحجوزة
                      </p>
                      <p className="mt-1 text-lg font-extrabold text-gray-900 tabular-nums">
                        {formatNumber(Number(r.quantity))}
                      </p>
                    </div>
                    <div className="rounded-lg bg-slate-50 p-2.5">
                      <p className="text-[10px] font-semibold uppercase tracking-wider text-slate-500">
                        ينتهي في
                      </p>
                      <p className="mt-1 text-xs font-bold text-gray-900 tabular-nums">
                        {new Date(r.expiresAt).toLocaleDateString('en-GB')}
                      </p>
                    </div>
                  </div>

                  <div className="mt-3 space-y-1.5 text-[11px] text-gray-500">
                    <div className="flex items-center gap-1.5">
                      <Package className="h-3 w-3" />
                      <span className="font-mono">صنف {r.itemId.substring(0, 8)}…</span>
                    </div>
                    <div className="flex items-center gap-1.5">
                      <Warehouse className="h-3 w-3" />
                      <span className="font-mono">مستودع {r.warehouseId.substring(0, 8)}…</span>
                    </div>
                    <div className="flex items-center gap-1.5">
                      <CalendarClock className="h-3 w-3" />
                      <span>أُنشئ في {new Date(r.createdAt).toLocaleDateString('en-GB')}</span>
                    </div>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
