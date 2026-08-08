'use client';

// صفحة المنتجات (Items) — Sprint 59 redesign
//
// Modern dashboard-style layout:
//   1. PageHero with eyebrow + title + KPI highlight (total stock value)
//   2. 4 StatCards: items count, active, total value, low-stock count
//   3. Filter bar (search + type filter + view toggle)
//   4. Grid OR table view of items, each enriched with live stock data
//
// Uses authedFetch (Sprint 58 hotfix) so all calls carry the JWT + X-Company-Id.

import { useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import {
  Package, Plus, Search, LayoutGrid, Table2, Pencil, Boxes,
  TrendingUp, AlertTriangle, Tag, Barcode, DollarSign,
} from 'lucide-react';
import {
  PageHero, StatCard, StatusPill, SectionCard, ProgressBar,
  Button, Input, EmptyState, SkeletonCard, SkeletonTable,
} from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { authedFetch, getErrorMessage } from '@/lib/api';

interface Item {
  id: string;
  sku: string;
  barcode?: string;
  name: string;
  description?: string;
  categoryId?: string;
  unitOfMeasureId: string;
  itemType: string;
  costingMethod: string;
  averageCost: number;
  reorderLevel: number;
  reorderQuantity: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

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

const ITEM_TYPE_META: Record<string, { label: string; tone: 'blue' | 'green' | 'amber' | 'purple' | 'slate' | 'sky' }> = {
  RawMaterial: { label: 'مادة خام', tone: 'amber' },
  FinishedGood: { label: 'منتج نهائي', tone: 'green' },
  Service: { label: 'خدمة', tone: 'sky' },
  Consumable: { label: 'مستهلكات', tone: 'purple' },
  Product: { label: 'منتج', tone: 'blue' },
  Merchandise: { label: 'بضاعة', tone: 'purple' },
};

const COSTING_META: Record<string, string> = {
  WeightedAverage: 'متوسط مرجح',
  FIFO: 'أول وارد أول صادر',
  LIFO: 'آخر وارد أول صادر',
  Standard: 'معيار',
};

type ViewMode = 'grid' | 'table';
type TypeFilter = 'all' | string;
type StatusFilter = 'all' | 'active' | 'inactive' | 'low';

function formatMoney(n: number): string {
  return n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
function formatNumber(n: number): string {
  return n.toLocaleString('en-US');
}

export default function ItemsPage() {
  const { loading: authLoading } = useAuth();
  const [items, setItems] = useState<Item[]>([]);
  const [levels, setLevels] = useState<StockLevel[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState<TypeFilter>('all');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');
  const [view, setView] = useState<ViewMode>('grid');

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const [itemsRes, levelsRes] = await Promise.all([
        authedFetch('/api/inventory/items', { cache: 'no-store' }),
        authedFetch('/api/inventory/levels', { cache: 'no-store' }),
      ]);
      const itemsJson = itemsRes.ok ? await itemsRes.json() : [];
      const levelsJson = levelsRes.ok ? await levelsRes.json() : [];
      setItems(Array.isArray(itemsJson) ? itemsJson : []);
      setLevels(Array.isArray(levelsJson) ? levelsJson : []);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل التحميل'));
    } finally {
      setLoading(false);
    }
  };

  // Build a fast lookup: itemId -> total available across warehouses
  const stockByItem = useMemo(() => {
    const map = new Map<string, { onHand: number; reserved: number; available: number; reorder: number }>();
    for (const l of levels) {
      const cur = map.get(l.itemId) ?? { onHand: 0, reserved: 0, available: 0, reorder: l.reorderLevel ?? 0 };
      cur.onHand += Number(l.quantityOnHand || 0);
      cur.reserved += Number(l.quantityReserved || 0);
      cur.available += Number(l.quantityAvailable || 0);
      cur.reorder = Math.max(cur.reorder, l.reorderLevel ?? 0);
      map.set(l.itemId, cur);
    }
    return map;
  }, [levels]);

  const types = useMemo(() => {
    const set = new Set<string>();
    items.forEach((i) => i.itemType && set.add(i.itemType));
    return Array.from(set).sort();
  }, [items]);

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    return items.filter((i) => {
      if (typeFilter !== 'all' && i.itemType !== typeFilter) return false;
      if (statusFilter === 'active' && !i.isActive) return false;
      if (statusFilter === 'inactive' && i.isActive) return false;
      if (statusFilter === 'low') {
        const s = stockByItem.get(i.id);
        const reorder = s?.reorder ?? i.reorderLevel ?? 0;
        const available = s?.available ?? 0;
        if (reorder > 0 && available > reorder) return false;
      }
      if (q) {
        const hay = `${i.sku} ${i.name} ${i.description ?? ''} ${i.itemType}`.toLowerCase();
        if (!hay.includes(q)) return false;
      }
      return true;
    });
  }, [items, search, typeFilter, statusFilter, stockByItem]);

  // KPIs (computed from full item set, not the filtered view, to keep the
  // "global at-a-glance" nature of the metrics).
  const totalItems = items.length;
  const activeItems = items.filter((i) => i.isActive).length;
  const totalValue = items.reduce((sum, i) => {
    const stock = stockByItem.get(i.id);
    const qty = stock?.onHand ?? 0;
    return sum + qty * Number(i.averageCost || 0);
  }, 0);
  const lowStockCount = items.filter((i) => {
    const s = stockByItem.get(i.id);
    const reorder = s?.reorder ?? i.reorderLevel ?? 0;
    const available = s?.available ?? 0;
    return reorder > 0 && available > 0 && available <= reorder;
  }).length;

  return (
    <div className="space-y-6">
      <PageHero
        eyebrow="إدارة المخزون"
        title="الأصناف والمنتجات"
        subtitle="كتالوج الأصناف، متوسط التكلفة، ومستويات إعادة الطلب عبر المستودعات"
        tone="emerald"
        highlight={
          loading
            ? undefined
            : { label: 'قيمة المخزون الإجمالية', value: `${formatMoney(totalValue)} ل.د` }
        }
        actions={
          <>
            <Link href="/inventory/items/new">
              <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
                صنف جديد
              </Button>
            </Link>
            <Button variant="secondary" onClick={load} disabled={loading}>
              تحديث
            </Button>
          </>
        }
        toolbar={
          <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
            <div className="flex flex-1 flex-col gap-2 sm:flex-row sm:items-center">
              <div className="relative flex-1 max-w-md">
                <Search className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-white/60" />
                <input
                  type="search"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  placeholder="ابحث بـ SKU، الاسم، الباركود..."
                  className="w-full rounded-lg border-0 bg-white/95 px-3 py-2 pe-9 text-sm text-gray-900 placeholder:text-gray-400 shadow-sm focus:outline-none focus:ring-2 focus:ring-white/40"
                />
              </div>
              <div className="flex items-center gap-2 text-xs font-semibold text-white/80">
                <span>النوع:</span>
                <select
                  value={typeFilter}
                  onChange={(e) => setTypeFilter(e.target.value as TypeFilter)}
                  className="rounded-lg border-0 bg-white/95 px-3 py-2 text-sm text-gray-900 shadow-sm focus:outline-none focus:ring-2 focus:ring-white/40"
                >
                  <option value="all">الكل ({types.length})</option>
                  {types.map((t) => (
                    <option key={t} value={t}>
                      {ITEM_TYPE_META[t]?.label ?? t}
                    </option>
                  ))}
                </select>
              </div>
              <div className="flex items-center gap-1 rounded-lg bg-white/10 p-0.5 backdrop-blur">
                {[
                  { v: 'all' as StatusFilter, label: 'الكل' },
                  { v: 'active' as StatusFilter, label: 'نشط' },
                  { v: 'inactive' as StatusFilter, label: 'موقوف' },
                  { v: 'low' as StatusFilter, label: 'مخزون منخفض' },
                ].map((opt) => (
                  <button
                    key={opt.v}
                    onClick={() => setStatusFilter(opt.v)}
                    className={
                      'rounded-md px-3 py-1.5 text-xs font-semibold transition ' +
                      (statusFilter === opt.v
                        ? 'bg-white text-slate-900 shadow'
                        : 'text-white/80 hover:text-white')
                    }
                  >
                    {opt.label}
                  </button>
                ))}
              </div>
            </div>
            <div className="flex items-center gap-1 rounded-lg bg-white/10 p-0.5 backdrop-blur">
              <button
                onClick={() => setView('grid')}
                className={
                  'flex items-center gap-1.5 rounded-md px-3 py-1.5 text-xs font-semibold transition ' +
                  (view === 'grid' ? 'bg-white text-slate-900 shadow' : 'text-white/80 hover:text-white')
                }
                aria-label="عرض بطاقات"
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
                aria-label="عرض جدول"
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
          <p className="font-semibold">تعذّر تحميل الأصناف</p>
          <p className="text-sm mt-1">{error}</p>
        </div>
      )}

      {/* KPI strip */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <StatCard
          label="إجمالي الأصناف"
          value={loading ? '…' : formatNumber(totalItems)}
          icon={Boxes}
          tone="emerald"
          hint="في الكتالوج"
        />
        <StatCard
          label="أصناف نشطة"
          value={loading ? '…' : formatNumber(activeItems)}
          icon={TrendingUp}
          tone="blue"
          hint={`${totalItems ? Math.round((activeItems / totalItems) * 100) : 0}% من الإجمالي`}
        />
        <StatCard
          label="قيمة المخزون"
          value={loading ? '…' : formatMoney(totalValue)}
          currency="ل.د"
          icon={DollarSign}
          tone="violet"
          hint="بمتوسط التكلفة المرجح"
        />
        <StatCard
          label="مخزون منخفض"
          value={loading ? '…' : formatNumber(lowStockCount)}
          icon={AlertTriangle}
          tone={lowStockCount > 0 ? 'amber' : 'slate'}
          hint="تحت حد إعادة الطلب"
        />
      </div>

      {/* List */}
      {loading ? (
        view === 'grid' ? (
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <SkeletonCard key={i} hasHeader={false} lines={3} />
            ))}
          </div>
        ) : (
          <SkeletonTable rows={6} cols={5} />
        )
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={<Package className="h-12 w-12" />}
          title={items.length === 0 ? 'لا توجد أصناف بعد' : 'لا توجد نتائج'}
          description={
            items.length === 0
              ? 'ابدأ بإضافة أول صنف في المخزون. كل صنف له SKU فريد ومتوسط تكلفة.'
              : 'جرّب توسيع الفلاتر أو البحث بكلمات مختلفة.'
          }
          action={
            items.length === 0 ? (
              <Link href="/inventory/items/new">
                <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
                  إضافة صنف
                </Button>
              </Link>
            ) : (
              <Button
                variant="secondary"
                onClick={() => {
                  setSearch('');
                  setTypeFilter('all');
                  setStatusFilter('all');
                }}
              >
                مسح الفلاتر
              </Button>
            )
          }
        />
      ) : view === 'grid' ? (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
          {filtered.map((item) => {
            const stock = stockByItem.get(item.id);
            const onHand = stock?.onHand ?? 0;
            const reorder = stock?.reorder ?? item.reorderLevel ?? 0;
            const available = stock?.available ?? 0;
            const lineValue = onHand * Number(item.averageCost || 0);
            const low = reorder > 0 && available > 0 && available <= reorder;
            const out = onHand <= 0;
            const typeMeta = ITEM_TYPE_META[item.itemType] ?? { label: item.itemType, tone: 'slate' as const };
            return (
              <div
                key={item.id}
                className="group relative overflow-hidden rounded-2xl bg-white shadow-sm ring-1 ring-gray-200/70 transition-all hover:-translate-y-0.5 hover:shadow-lg"
              >
                {/* Top accent strip */}
                <div className={
                  'h-1 w-full ' +
                  (out ? 'bg-rose-400' : low ? 'bg-amber-400' : item.isActive ? 'bg-emerald-400' : 'bg-gray-300')
                } />

                <div className="p-5">
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <span className="inline-flex items-center gap-1 rounded-md bg-slate-100 px-2 py-0.5 font-mono text-[11px] font-semibold text-slate-700">
                          <Barcode className="h-3 w-3" />
                          {item.sku}
                        </span>
                        <StatusPill tone={typeMeta.tone} label={typeMeta.label} showDot={false} />
                      </div>
                      <h3 className="mt-2 truncate text-base font-bold text-gray-900" title={item.name}>
                        {item.name}
                      </h3>
                      {item.description && (
                        <p className="mt-0.5 line-clamp-2 text-xs text-gray-500">{item.description}</p>
                      )}
                    </div>
                    <Link
                      href={`/inventory/items/${item.id}/edit`}
                      className="rounded-md p-1.5 text-gray-400 opacity-0 transition-opacity group-hover:opacity-100 hover:bg-gray-100 hover:text-gray-700"
                      title="تعديل"
                    >
                      <Pencil className="h-3.5 w-3.5" />
                    </Link>
                  </div>

                  <div className="mt-4 grid grid-cols-2 gap-3 text-xs">
                    <div className="rounded-lg bg-slate-50 p-2.5">
                      <p className="text-[10px] font-semibold uppercase tracking-wider text-slate-500">
                        متوسط التكلفة
                      </p>
                      <p className="mt-1 text-sm font-bold text-gray-900 tabular-nums">
                        {formatMoney(Number(item.averageCost || 0))} <span className="text-[10px] text-gray-500">ل.د</span>
                      </p>
                    </div>
                    <div className="rounded-lg bg-slate-50 p-2.5">
                      <p className="text-[10px] font-semibold uppercase tracking-wider text-slate-500">
                        تكلفة الاستهلاك
                      </p>
                      <p className="mt-1 text-sm font-bold text-gray-900 tabular-nums">
                        {COSTING_META[item.costingMethod] ?? item.costingMethod}
                      </p>
                    </div>
                  </div>

                  {/* Stock indicator */}
                  <div className="mt-4">
                    <ProgressBar
                      value={available}
                      max={Math.max(reorder * 2, available, 1)}
                      threshold={reorder}
                      label="الكمية المتاحة"
                      showValue
                      formatValue={(v) => formatNumber(v)}
                    />
                  </div>

                  <div className="mt-3 flex items-center justify-between border-t border-gray-100 pt-3">
                    <div>
                      <p className="text-[10px] font-semibold uppercase tracking-wider text-gray-500">
                        قيمة المخزون
                      </p>
                      <p className="text-sm font-bold text-gray-900 tabular-nums">
                        {formatMoney(lineValue)} <span className="text-[10px] text-gray-500">ل.د</span>
                      </p>
                    </div>
                    <div className="flex items-center gap-1.5">
                      {out ? (
                        <StatusPill tone="red" label="نفد المخزون" />
                      ) : low ? (
                        <StatusPill tone="amber" label="منخفض" />
                      ) : item.isActive ? (
                        <StatusPill tone="green" label="متوفر" />
                      ) : (
                        <StatusPill tone="slate" label="موقوف" />
                      )}
                    </div>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      ) : (
        <SectionCard
          flush
          title={`الأصناف (${filtered.length.toLocaleString('en-US')})`}
          description="عرض جدولي — قابل للفرز اليدوي بمحاذاة الأعمدة"
        >
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-slate-50 text-[11px] uppercase tracking-wider text-slate-500">
                <tr>
                  <th className="px-4 py-3 text-start font-semibold">SKU</th>
                  <th className="px-4 py-3 text-start font-semibold">الصنف</th>
                  <th className="px-4 py-3 text-start font-semibold">النوع</th>
                  <th className="px-4 py-3 text-end font-semibold">متوسط التكلفة</th>
                  <th className="px-4 py-3 text-end font-semibold">متاح</th>
                  <th className="px-4 py-3 text-end font-semibold">قيمة المخزون</th>
                  <th className="px-4 py-3 text-center font-semibold">الحالة</th>
                  <th className="px-4 py-3 text-end font-semibold">إجراء</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {filtered.map((item) => {
                  const stock = stockByItem.get(item.id);
                  const onHand = stock?.onHand ?? 0;
                  const available = stock?.available ?? 0;
                  const reorder = stock?.reorder ?? item.reorderLevel ?? 0;
                  const lineValue = onHand * Number(item.averageCost || 0);
                  const low = reorder > 0 && available > 0 && available <= reorder;
                  const out = onHand <= 0;
                  const typeMeta = ITEM_TYPE_META[item.itemType] ?? { label: item.itemType, tone: 'slate' as const };
                  return (
                    <tr key={item.id} className="transition-colors hover:bg-slate-50/60">
                      <td className="px-4 py-3 font-mono text-xs text-slate-700">{item.sku}</td>
                      <td className="px-4 py-3">
                        <p className="font-semibold text-gray-900">{item.name}</p>
                        {item.description && (
                          <p className="text-[11px] text-gray-500 line-clamp-1">{item.description}</p>
                        )}
                      </td>
                      <td className="px-4 py-3">
                        <StatusPill tone={typeMeta.tone} label={typeMeta.label} showDot={false} />
                      </td>
                      <td className="px-4 py-3 text-end tabular-nums">
                        {formatMoney(Number(item.averageCost || 0))}
                      </td>
                      <td className="px-4 py-3 text-end tabular-nums font-semibold">
                        {formatNumber(available)}
                        {reorder > 0 && (
                          <span className="block text-[10px] font-normal text-gray-400">
                            إعادة الطلب: {formatNumber(reorder)}
                          </span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-end tabular-nums font-semibold">
                        {formatMoney(lineValue)}
                      </td>
                      <td className="px-4 py-3 text-center">
                        {out ? (
                          <StatusPill tone="red" label="نفد" />
                        ) : low ? (
                          <StatusPill tone="amber" label="منخفض" />
                        ) : item.isActive ? (
                          <StatusPill tone="green" label="متوفر" />
                        ) : (
                          <StatusPill tone="slate" label="موقوف" />
                        )}
                      </td>
                      <td className="px-4 py-3 text-end">
                        <Link
                          href={`/inventory/items/${item.id}/edit`}
                          className="inline-flex items-center gap-1 rounded-md bg-slate-100 px-2 py-1 text-xs font-semibold text-slate-700 hover:bg-slate-200"
                        >
                          <Pencil className="h-3 w-3" />
                          تعديل
                        </Link>
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
