'use client';

// صفحة حركات المخزون (Stock Movements) — Sprint 59 redesign
//
// Modern dashboard with:
//   1. PageHero (emerald tone) + total movement value
//   2. 4 StatCards: total movements, total value, posted, drafts
//   3. Type tab bar (All / Receive / Issue / Transfer / Adjust) — colored
//   4. Search + view toggle
//   5. Rich movement cards with type icon, qty, total, source

import { useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import {
  Plus, Search, LayoutGrid, Table2, ArrowDownToLine, ArrowUpFromLine,
  ArrowRightLeft, Settings2, Filter, Eye, Calendar, Hash, Package, DollarSign,
  CheckCircle2, FileEdit,
} from 'lucide-react';
import {
  PageHero, StatCard, StatusPill, SectionCard,
  Button, EmptyState, SkeletonCard, SkeletonTable,
} from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { authedFetch, getErrorMessage } from '@/lib/api';

interface StockMovement {
  id: string;
  reference: string;
  type: string; // BE serializes as string (Receive, Issue, Transfer, Adjust, Return)
  status: string; // Draft, Posted, Reversed
  movementDate: string;
  itemId: string;
  warehouseId: string;
  destinationWarehouseId?: string;
  quantity: number;
  unitCost: number;
  totalCost: number;
  projectId?: string;
  costCenterId?: string;
  notes?: string;
  createdAt: string;
  postedAt?: string;
}

const TYPE_META: Record<
  string,
  { label: string; icon: React.ComponentType<{ className?: string }>; tone: 'green' | 'red' | 'blue' | 'amber' | 'purple' }
> = {
  Receive:  { label: 'استلام',   icon: ArrowDownToLine,  tone: 'green' },
  Issue:    { label: 'صرف',      icon: ArrowUpFromLine,  tone: 'red' },
  Transfer: { label: 'تحويل',    icon: ArrowRightLeft,    tone: 'blue' },
  Adjust:   { label: 'تسوية',    icon: Settings2,         tone: 'amber' },
  Return:   { label: 'إرجاع',    icon: ArrowDownToLine,  tone: 'purple' },
};

const STATUS_META: Record<string, { label: string; tone: 'green' | 'amber' | 'slate' }> = {
  Posted:   { label: 'مُرحَّل', tone: 'green' },
  Draft:    { label: 'مسودة',   tone: 'amber' },
  Reversed: { label: 'معكوس',   tone: 'slate' },
};

type TabKey = 'all' | 'Receive' | 'Issue' | 'Transfer' | 'Adjust';
type ViewMode = 'grid' | 'table';

function formatMoney(n: number): string {
  return n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
function formatNumber(n: number): string {
  return n.toLocaleString('en-US');
}

export default function StockMovementsPage() {
  const { loading: authLoading } = useAuth();
  const [items, setItems] = useState<StockMovement[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tab, setTab] = useState<TabKey>('all');
  const [search, setSearch] = useState('');
  const [view, setView] = useState<ViewMode>('grid');

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await authedFetch('/api/inventory/movements', { cache: 'no-store' });
      if (!res.ok) throw new Error('فشل التحميل');
      const data = await res.json();
      setItems(Array.isArray(data) ? data : []);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل التحميل'));
    } finally {
      setLoading(false);
    }
  };

  // Counts per type — used to populate the tab labels
  const typeCounts = useMemo(() => {
    const c: Record<string, number> = { all: items.length };
    for (const m of items) c[m.type] = (c[m.type] ?? 0) + 1;
    return c;
  }, [items]);

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    return items.filter((m) => {
      if (tab !== 'all' && m.type !== tab) return false;
      if (q) {
        const hay = `${m.reference} ${m.notes ?? ''} ${m.itemId} ${m.warehouseId} ${m.type}`.toLowerCase();
        if (!hay.includes(q)) return false;
      }
      return true;
    });
  }, [items, tab, search]);

  const totalCount = items.length;
  const postedCount = items.filter((m) => m.status === 'Posted').length;
  const draftCount = items.filter((m) => m.status === 'Draft').length;
  // Net value of all movements (use absolute value so opposite signs cancel visually)
  const totalValue = items.reduce((s, m) => s + Math.abs(Number(m.totalCost || 0)), 0);

  const tabDefs: { key: TabKey; label: string; tone: 'slate' | 'green' | 'red' | 'blue' | 'amber' }[] = [
    { key: 'all',      label: 'الكل',     tone: 'slate' },
    { key: 'Receive',  label: 'استلام',   tone: 'green' },
    { key: 'Issue',    label: 'صرف',      tone: 'red' },
    { key: 'Transfer', label: 'تحويل',    tone: 'blue' },
    { key: 'Adjust',   label: 'تسوية',    tone: 'amber' },
  ];

  return (
    <div className="space-y-6">
      <PageHero
        eyebrow="إدارة المخزون"
        title="حركات المخزون"
        subtitle="سجل استلام وصرف وتحويل وتسوية الأصناف مع مصدر الحركة وقيمتها"
        tone="slate"
        highlight={
          loading
            ? undefined
            : { label: 'قيمة الحركات الإجمالية', value: `${formatMoney(totalValue)} ل.د` }
        }
        actions={
          <>
            <Link href="/inventory/movements/new">
              <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
                حركة جديدة
              </Button>
            </Link>
            <Button variant="secondary" onClick={load} disabled={loading}>
              تحديث
            </Button>
          </>
        }
        toolbar={
          <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
            <div className="flex flex-1 items-center gap-3">
              <div className="relative flex-1 max-w-md">
                <Search className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-white/60" />
                <input
                  type="search"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  placeholder="ابحث برقم الحركة، الكود، الملاحظات..."
                  className="w-full rounded-lg border-0 bg-white/95 px-3 py-2 pe-9 text-sm text-gray-900 placeholder:text-gray-400 shadow-sm focus:outline-none focus:ring-2 focus:ring-white/40"
                />
              </div>
            </div>
            <div className="flex items-center gap-1 rounded-lg bg-white/10 p-0.5 backdrop-blur">
              <button
                onClick={() => setView('grid')}
                className={
                  'flex items-center gap-1.5 rounded-md px-3 py-1.5 text-xs font-semibold transition ' +
                  (view === 'grid' ? 'bg-white text-slate-900 shadow' : 'text-white/80 hover:text-white')
                }
              >
                <LayoutGrid className="h-3.5 w-3.5" />
                بطاقات
              </button>
              <button
                onClick={() => setView('table')}
                className={
                  'flex items-center gap-1.5 rounded-md px-3 py-1.5 text-xs font-semibold transition ' +
                  (view === 'table' ? 'bg-white text-slate-900 shadow' : 'text-white/80 hover:text-white')
                }
              >
                <Table2 className="h-3.5 w-3.5" />
                جدول
              </button>
            </div>
          </div>
        }
      />

      {error && !loading && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-rose-700" role="alert">
          <p className="font-semibold">تعذّر تحميل الحركات</p>
          <p className="text-sm mt-1">{error}</p>
        </div>
      )}

      {/* Type tab bar — wider, with counts + colored icons */}
      <div className="flex flex-wrap items-center gap-2 rounded-2xl bg-white p-2 shadow-sm ring-1 ring-gray-200/70">
        {tabDefs.map((t) => {
          const meta = t.key === 'all' ? null : TYPE_META[t.key];
          const Icon = meta?.icon ?? Filter;
          const active = tab === t.key;
          const activeBg = active
            ? t.tone === 'slate'
              ? 'bg-slate-900 text-white'
              : t.tone === 'green'
                ? 'bg-emerald-600 text-white'
                : t.tone === 'red'
                  ? 'bg-rose-600 text-white'
                  : t.tone === 'blue'
                    ? 'bg-blue-600 text-white'
                    : 'bg-amber-600 text-white'
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
                {typeCounts[t.key] ?? 0}
              </span>
            </button>
          );
        })}
      </div>

      {/* KPI strip */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <StatCard
          label="إجمالي الحركات"
          value={loading ? '…' : formatNumber(totalCount)}
          icon={Hash}
          tone="slate"
          hint="في الفترة الحالية"
        />
        <StatCard
          label="مُرحَّلة"
          value={loading ? '…' : formatNumber(postedCount)}
          icon={CheckCircle2}
          tone="green"
          hint="أثّرت على GL"
        />
        <StatCard
          label="مسودات"
          value={loading ? '…' : formatNumber(draftCount)}
          icon={FileEdit}
          tone="amber"
          hint="تحت الإعداد"
        />
        <StatCard
          label="قيمة الحركات"
          value={loading ? '…' : formatMoney(totalValue)}
          currency="ل.د"
          icon={DollarSign}
          tone="violet"
          hint="مجموع |مدين| + |دائن|"
        />
      </div>

      {/* List */}
      {loading ? (
        view === 'grid' ? (
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <SkeletonCard key={i} hasHeader={false} lines={4} />
            ))}
          </div>
        ) : (
          <SkeletonTable rows={6} cols={6} />
        )
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={<Hash className="h-12 w-12" />}
          title={items.length === 0 ? 'لا توجد حركات بعد' : 'لا توجد نتائج'}
          description={
            items.length === 0
              ? 'ابدأ بتسجيل أول حركة مخزون (استلام، صرف، تحويل، تسوية).'
              : 'لا توجد حركات تطابق الفلاتر الحالية.'
          }
          action={
            items.length === 0 ? (
              <Link href="/inventory/movements/new">
                <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
                  أول حركة
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
      ) : view === 'grid' ? (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
          {filtered.map((m) => {
            const meta = TYPE_META[m.type] ?? { label: m.type, icon: Settings2, tone: 'amber' as const };
            const sMeta = STATUS_META[m.status] ?? { label: m.status, tone: 'slate' as const };
            const Icon = meta.icon;
            return (
              <div
                key={m.id}
                className="group relative overflow-hidden rounded-2xl bg-white shadow-sm ring-1 ring-gray-200/70 transition-all hover:-translate-y-0.5 hover:shadow-lg"
              >
                <div
                  className={
                    'h-1 w-full ' +
                    (meta.tone === 'green'
                      ? 'bg-emerald-400'
                      : meta.tone === 'red'
                        ? 'bg-rose-400'
                        : meta.tone === 'blue'
                          ? 'bg-blue-400'
                          : meta.tone === 'amber'
                            ? 'bg-amber-400'
                            : 'bg-violet-400')
                  }
                />

                <div className="p-5">
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex items-center gap-3 min-w-0">
                      <div
                        className={
                          'flex h-11 w-11 flex-shrink-0 items-center justify-center rounded-xl ' +
                          (meta.tone === 'green'
                            ? 'bg-emerald-100 text-emerald-600'
                            : meta.tone === 'red'
                              ? 'bg-rose-100 text-rose-600'
                              : meta.tone === 'blue'
                                ? 'bg-blue-100 text-blue-600'
                                : meta.tone === 'amber'
                                  ? 'bg-amber-100 text-amber-600'
                                  : 'bg-violet-100 text-violet-600')
                        }
                      >
                        <Icon className="h-5 w-5" />
                      </div>
                      <div className="min-w-0">
                        <div className="flex items-center gap-2">
                          <span className="rounded-md bg-slate-100 px-1.5 py-0.5 font-mono text-[10px] font-semibold text-slate-700">
                            {m.reference}
                          </span>
                          <StatusPill tone={sMeta.tone} label={sMeta.label} showDot={false} />
                        </div>
                        <p className="mt-1 text-sm font-bold text-gray-900">{meta.label}</p>
                      </div>
                    </div>
                    <Link
                      href={`/inventory/movements/${m.id}`}
                      className="rounded-md p-1.5 text-gray-400 opacity-0 transition-opacity group-hover:opacity-100 hover:bg-gray-100 hover:text-gray-700"
                      title="عرض التفاصيل"
                    >
                      <Eye className="h-3.5 w-3.5" />
                    </Link>
                  </div>

                  <div className="mt-4 grid grid-cols-2 gap-3 text-xs">
                    <div className="rounded-lg bg-slate-50 p-2.5">
                      <p className="text-[10px] font-semibold uppercase tracking-wider text-slate-500">
                        الكمية
                      </p>
                      <p className="mt-1 text-base font-extrabold text-gray-900 tabular-nums">
                        {Number(m.quantity) > 0 ? '+' : ''}
                        {formatNumber(Number(m.quantity))}
                      </p>
                    </div>
                    <div className="rounded-lg bg-slate-50 p-2.5">
                      <p className="text-[10px] font-semibold uppercase tracking-wider text-slate-500">
                        القيمة
                      </p>
                      <p className="mt-1 text-base font-extrabold text-gray-900 tabular-nums">
                        {formatMoney(Number(m.totalCost))} <span className="text-[10px] text-gray-500">ل.د</span>
                      </p>
                    </div>
                  </div>

                  <div className="mt-3 space-y-1.5 text-[11px] text-gray-500">
                    <div className="flex items-center gap-1.5">
                      <Calendar className="h-3 w-3" />
                      <span className="tabular-nums">{new Date(m.movementDate).toLocaleDateString('en-GB')}</span>
                    </div>
                    <div className="flex items-center gap-1.5">
                      <Package className="h-3 w-3" />
                      <span className="font-mono">صنف {m.itemId.substring(0, 8)}…</span>
                    </div>
                  </div>

                  {m.notes && (
                    <p className="mt-3 line-clamp-2 text-[11px] italic text-gray-500">“{m.notes}”</p>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      ) : (
        <SectionCard flush title={`الحركات (${filtered.length.toLocaleString('en-US')})`}>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 text-[11px] uppercase tracking-wider text-slate-500">
                <tr>
                  <th className="px-4 py-3 text-start font-semibold">المرجع</th>
                  <th className="px-4 py-3 text-start font-semibold">النوع</th>
                  <th className="px-4 py-3 text-start font-semibold">التاريخ</th>
                  <th className="px-4 py-3 text-end font-semibold">الكمية</th>
                  <th className="px-4 py-3 text-end font-semibold">سعر الوحدة</th>
                  <th className="px-4 py-3 text-end font-semibold">القيمة</th>
                  <th className="px-4 py-3 text-center font-semibold">الحالة</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {filtered.map((m) => {
                  const meta = TYPE_META[m.type] ?? { label: m.type, icon: Settings2, tone: 'amber' as const };
                  const sMeta = STATUS_META[m.status] ?? { label: m.status, tone: 'slate' as const };
                  const Icon = meta.icon;
                  return (
                    <tr key={m.id} className="transition-colors hover:bg-slate-50/60">
                      <td className="px-4 py-3 font-mono text-xs font-semibold text-slate-700">
                        {m.reference}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-2">
                          <div
                            className={
                              'flex h-7 w-7 items-center justify-center rounded-lg ' +
                              (meta.tone === 'green'
                                ? 'bg-emerald-100 text-emerald-600'
                                : meta.tone === 'red'
                                  ? 'bg-rose-100 text-rose-600'
                                  : meta.tone === 'blue'
                                    ? 'bg-blue-100 text-blue-600'
                                    : meta.tone === 'amber'
                                      ? 'bg-amber-100 text-amber-600'
                                      : 'bg-violet-100 text-violet-600')
                            }
                          >
                            <Icon className="h-3.5 w-3.5" />
                          </div>
                          <span className="font-semibold text-gray-900">{meta.label}</span>
                        </div>
                      </td>
                      <td className="px-4 py-3 text-xs text-gray-600 tabular-nums">
                        {new Date(m.movementDate).toLocaleDateString('en-GB')}
                      </td>
                      <td className="px-4 py-3 text-end font-bold tabular-nums">
                        {Number(m.quantity) > 0 ? '+' : ''}
                        {formatNumber(Number(m.quantity))}
                      </td>
                      <td className="px-4 py-3 text-end tabular-nums">
                        {formatMoney(Number(m.unitCost))}
                      </td>
                      <td className="px-4 py-3 text-end font-bold tabular-nums">
                        {formatMoney(Number(m.totalCost))}
                      </td>
                      <td className="px-4 py-3 text-center">
                        <StatusPill tone={sMeta.tone} label={sMeta.label} showDot={false} />
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </SectionCard>
      )}
    </div>
  );
}
