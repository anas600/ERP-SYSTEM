'use client';

// صفحة مستويات المخزون (Stock Levels) — Sprint 59 redesign
//
// Critical for daily inventory work. Pulls items + levels in parallel and
// joins them so we can show item names + SKUs alongside the warehouse
// quantities. Progress bars visualize on-hand vs reorder level for fast
// low-stock scanning.

import { useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import {
  Layers, Search, AlertTriangle, CheckCircle2, TrendingUp,
  Warehouse, Package, Barcode, DollarSign, BarChart3, Activity,
} from 'lucide-react';
import {
  PageHero, StatCard, StatusPill, SectionCard, ProgressBar,
  Button, EmptyState, SkeletonTable,
} from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { authedFetch, getErrorMessage } from '@/lib/api';

interface StockLevel {
  id: string;
  itemId: string;
  warehouseId: string;
  quantityOnHand: number;
  quantityReserved: number;
  quantityAvailable: number;
  averageCost: number;
  reorderLevel?: number;
}

interface ItemLite {
  id: string;
  sku: string;
  name: string;
  itemType: string;
}

type Filter = 'all' | 'low' | 'out' | 'healthy';

function formatMoney(n: number): string {
  return n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
function formatNumber(n: number): string {
  return n.toLocaleString('en-US');
}

export default function StockLevelsPage() {
  const { loading: authLoading } = useAuth();
  const [levels, setLevels] = useState<StockLevel[]>([]);
  const [items, setItems] = useState<ItemLite[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState<Filter>('all');
  const [search, setSearch] = useState('');

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      // Load items in parallel — used to enrich the level rows.
      const [lvlRes, itemRes] = await Promise.all([
        authedFetch('/api/inventory/levels', { cache: 'no-store' }),
        authedFetch('/api/inventory/items', { cache: 'no-store' }),
      ]);
      const lvlJson = lvlRes.ok ? await lvlRes.json() : [];
      const itemJson = itemRes.ok ? await itemRes.json() : [];
      setLevels(Array.isArray(lvlJson) ? lvlJson : []);
      setItems(Array.isArray(itemJson) ? itemJson : []);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل التحميل'));
    } finally {
      setLoading(false);
    }
  };

  const itemMap = useMemo(() => {
    const m = new Map<string, ItemLite>();
    for (const it of items) m.set(it.id, it);
    return m;
  }, [items]);

  const classified = useMemo(() => {
    return levels.map((l) => {
      const reorder = l.reorderLevel ?? 0;
      const available = l.quantityAvailable ?? 0;
      const onHand = l.quantityOnHand ?? 0;
      const state: 'out' | 'low' | 'healthy' =
        onHand <= 0 ? 'out' : reorder > 0 && available <= reorder ? 'low' : 'healthy';
      const lineValue = onHand * Number(l.averageCost || 0);
      return { ...l, _state: state, _lineValue: lineValue, _reorder: reorder };
    });
  }, [levels]);

  const counts = useMemo(() => {
    const c: Record<Filter, number> = { all: levels.length, low: 0, out: 0, healthy: 0 };
    for (const r of classified) c[r._state]++;
    return c;
  }, [classified, levels]);

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    return classified.filter((l) => {
      if (filter !== 'all' && l._state !== filter) return false;
      if (q) {
        const it = itemMap.get(l.itemId);
        const hay = `${it?.sku ?? ''} ${it?.name ?? ''} ${l.warehouseId} ${l.id}`.toLowerCase();
        if (!hay.includes(q)) return false;
      }
      return true;
    });
  }, [classified, filter, search, itemMap]);

  // KPIs
  const totalOnHand = levels.reduce((s, l) => s + Number(l.quantityOnHand || 0), 0);
  const totalReserved = levels.reduce((s, l) => s + Number(l.quantityReserved || 0), 0);
  const totalAvailable = levels.reduce((s, l) => s + Number(l.quantityAvailable || 0), 0);
  const totalValue = levels.reduce((s, l) => s + Number(l.quantityOnHand || 0) * Number(l.averageCost || 0), 0);

  const filterDefs: { key: Filter; label: string; tone: 'slate' | 'green' | 'amber' | 'red' }[] = [
    { key: 'all',     label: 'الكل',           tone: 'slate' },
    { key: 'healthy', label: 'متوفر',          tone: 'green' },
    { key: 'low',     label: 'مخزون منخفض',   tone: 'amber' },
    { key: 'out',     label: 'نفد المخزون',     tone: 'red' },
  ];

  return (
    <div className="space-y-6">
      <PageHero
        eyebrow="إدارة المخزون"
        title="مستويات المخزون"
        subtitle="كميات الأصناف في كل مستودع مع متابعة حدود إعادة الطلب وحجز الكمية"
        tone="blue"
        highlight={
          loading
            ? undefined
            : { label: 'قيمة المخزون الإجمالية', value: `${formatMoney(totalValue)} ل.د` }
        }
        actions={
          <>
            <Button variant="secondary" onClick={load} disabled={loading}>
              تحديث
            </Button>
          </>
        }
        toolbar={
          <div className="relative flex-1 max-w-md">
            <Search className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-white/60" />
            <input
              type="search"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="ابحث بـ SKU، اسم الصنف، المستودع..."
              className="w-full rounded-lg border-0 bg-white/95 px-3 py-2 pe-9 text-sm text-gray-900 placeholder:text-gray-400 shadow-sm focus:outline-none focus:ring-2 focus:ring-white/40"
            />
          </div>
        }
      />

      {error && !loading && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-rose-700" role="alert">
          <p className="font-semibold">تعذّر تحميل مستويات المخزون</p>
          <p className="text-sm mt-1">{error}</p>
        </div>
      )}

      {/* KPI strip */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <StatCard
          label="إجمالي باليد"
          value={loading ? '…' : formatNumber(totalOnHand)}
          icon={Layers}
          tone="blue"
          hint="كل المستودعات"
        />
        <StatCard
          label="محجوز"
          value={loading ? '…' : formatNumber(totalReserved)}
          icon={Activity}
          tone="amber"
          hint={`متاح: ${formatNumber(totalAvailable)}`}
        />
        <StatCard
          label="تحت حد الطلب"
          value={loading ? '…' : formatNumber(counts.low)}
          icon={AlertTriangle}
          tone={counts.low > 0 ? 'amber' : 'slate'}
          hint="يحتاج إعادة طلب"
        />
        <StatCard
          label="نفد المخزون"
          value={loading ? '…' : formatNumber(counts.out)}
          icon={TrendingUp}
          tone={counts.out > 0 ? 'red' : 'slate'}
          hint="كمية صفرية"
        />
      </div>

      {/* Filter chips */}
      <div className="flex flex-wrap items-center gap-2 rounded-2xl bg-white p-2 shadow-sm ring-1 ring-gray-200/70">
        {filterDefs.map((f) => {
          const active = filter === f.key;
          const activeBg = active
            ? f.tone === 'slate'
              ? 'bg-slate-900 text-white'
              : f.tone === 'green'
                ? 'bg-emerald-600 text-white'
                : f.tone === 'amber'
                  ? 'bg-amber-500 text-white'
                  : 'bg-rose-600 text-white'
            : 'bg-gray-50 text-gray-600 hover:bg-gray-100';
          return (
            <button
              key={f.key}
              onClick={() => setFilter(f.key)}
              className={`flex items-center gap-2 rounded-xl px-4 py-2 text-sm font-bold transition ${activeBg}`}
            >
              <span>{f.label}</span>
              <span
                className={
                  'rounded-full px-2 py-0.5 text-[11px] font-bold tabular-nums ' +
                  (active ? 'bg-white/20 text-white' : 'bg-white text-gray-500 ring-1 ring-gray-200')
                }
              >
                {counts[f.key]}
              </span>
            </button>
          );
        })}
      </div>

      {/* List */}
      {loading ? (
        <SkeletonTable rows={6} cols={6} />
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={<Layers className="h-12 w-12" />}
          title={levels.length === 0 ? 'لا توجد مستويات مخزون' : 'لا توجد نتائج'}
          description={
            levels.length === 0
              ? 'لا توجد كميات في المستودعات بعد. أضف حركة استلام (Receive) لتسجيل أول مستوى.'
              : 'لا توجد مستويات تطابق الفلاتر الحالية.'
          }
          action={
            levels.length === 0 ? (
              <Link href="/inventory/movements/new">
                <Button variant="primary" iconLeft={<BarChart3 className="h-4 w-4" />}>
                  حركة استلام
                </Button>
              </Link>
            ) : (
              <Button
                variant="secondary"
                onClick={() => {
                  setFilter('all');
                  setSearch('');
                }}
              >
                مسح الفلاتر
              </Button>
            )
          }
        />
      ) : (
        <SectionCard flush title={`مستويات المخزون (${filtered.length.toLocaleString('en-US')})`}>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 text-[11px] uppercase tracking-wider text-slate-500">
                <tr>
                  <th className="px-4 py-3 text-start font-semibold">الصنف</th>
                  <th className="px-4 py-3 text-start font-semibold">المستودع</th>
                  <th className="px-4 py-3 text-end font-semibold">باليد</th>
                  <th className="px-4 py-3 text-end font-semibold">محجوز</th>
                  <th className="px-4 py-3 text-end font-semibold">متاح</th>
                  <th className="px-4 py-3 text-start font-semibold w-72">حد الطلب</th>
                  <th className="px-4 py-3 text-end font-semibold">قيمة</th>
                  <th className="px-4 py-3 text-center font-semibold">الحالة</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {filtered.map((l) => {
                  const it = itemMap.get(l.itemId);
                  // Visual scale: max(reorder * 1.5, onHand, 1) so the bar makes sense.
                  const max = Math.max(l._reorder * 1.5, l.quantityOnHand, l._reorder, 1);
                  return (
                    <tr key={l.id} className="transition-colors hover:bg-slate-50/60">
                      <td className="px-4 py-3 min-w-[200px]">
                        {it ? (
                          <>
                            <div className="flex items-center gap-2">
                              <span className="rounded bg-slate-100 px-1.5 py-0.5 font-mono text-[10px] font-semibold text-slate-700">
                                {it.sku}
                              </span>
                              <span className="font-semibold text-gray-900 truncate max-w-[220px]" title={it.name}>
                                {it.name}
                              </span>
                            </div>
                            <p className="mt-0.5 text-[11px] text-gray-500">{it.itemType}</p>
                          </>
                        ) : (
                          <span className="font-mono text-[11px] text-gray-400">{l.itemId.substring(0, 8)}…</span>
                        )}
                      </td>
                      <td className="px-4 py-3">
                        <span className="inline-flex items-center gap-1 rounded bg-slate-100 px-1.5 py-0.5 font-mono text-[11px] text-slate-700">
                          <Warehouse className="h-3 w-3" />
                          {l.warehouseId.substring(0, 8)}…
                        </span>
                      </td>
                      <td className="px-4 py-3 text-end font-bold tabular-nums text-gray-900">
                        {formatNumber(l.quantityOnHand)}
                      </td>
                      <td className="px-4 py-3 text-end tabular-nums text-amber-700">
                        {formatNumber(l.quantityReserved)}
                      </td>
                      <td className="px-4 py-3 text-end font-bold tabular-nums text-emerald-700">
                        {formatNumber(l.quantityAvailable)}
                      </td>
                      <td className="px-4 py-3">
                        <ProgressBar
                          value={l.quantityAvailable}
                          max={max}
                          threshold={l._reorder}
                          showValue={false}
                          label={`حد الطلب: ${l._reorder > 0 ? formatNumber(l._reorder) : 'غير محدد'}`}
                        />
                      </td>
                      <td className="px-4 py-3 text-end font-bold tabular-nums">
                        {formatMoney(l._lineValue)}
                        <span className="block text-[10px] font-normal text-gray-400">
                          @ {formatMoney(Number(l.averageCost))}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-center">
                        {l._state === 'out' ? (
                          <StatusPill tone="red" label="نفد" />
                        ) : l._state === 'low' ? (
                          <StatusPill tone="amber" label="منخفض" />
                        ) : (
                          <StatusPill tone="green" label="متوفر" />
                        )}
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
