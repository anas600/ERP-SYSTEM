'use client';

// صفحة المشاريع (Projects) — Sprint 59 redesign (DEC-170)
//
// Modern dashboard-style layout:
//   1. PageHero with eyebrow + title + total-budget highlight
//   2. 4 StatCards: total projects, active, total budget, total revenue (PnL sum)
//   3. FilterChips for status tabs (الكل / تخطيط / نشط / معلق / مكتمل / ملغي)
//   4. ModernTable of projects, clickable rows to /projects/[id]
//
// Backend: GET /api/projects returns Project[] with numeric status (1..5).

import { useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import {
  Briefcase, Plus, Pencil, Calendar, Wallet,
  TrendingUp, CheckCircle2, PauseCircle, XCircle, ClipboardList,
} from 'lucide-react';
import {
  PageHero, StatCard, StatusPill, SectionCard,
  ModernTable, FilterChips,
  Button, EmptyState, SkeletonTable, type ModernTableColumn,
} from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { projectsApi, getErrorMessage, type Project, type ProjectStatusName } from '@/lib/api';
import { formatDate, formatCurrency } from '@/lib/utils';

// L120: BE returns status as string. Map Planning|Active|OnHold|Completed|Cancelled.
const STATUS_META: Record<ProjectStatusName, { label: string; tone: 'green' | 'amber' | 'red' | 'blue' | 'slate' }> = {
  Planning: { label: 'تخطيط', tone: 'slate' },
  Active: { label: 'نشط', tone: 'green' },
  OnHold: { label: 'معلق', tone: 'amber' },
  Completed: { label: 'مكتمل', tone: 'blue' },
  Cancelled: { label: 'ملغي', tone: 'red' },
};

const STATUS_KEYS: ProjectStatusName[] = ['Planning', 'Active', 'OnHold', 'Completed', 'Cancelled'];

type StatusFilter = 'all' | ProjectStatusName;

export default function ProjectsPage() {
  const { loading: authLoading } = useAuth();
  const [items, setItems] = useState<Project[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [tab, setTab] = useState<StatusFilter>('all');

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await projectsApi.listProjects();
      setItems(Array.isArray(data) ? data : []);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل التحميل'));
    } finally {
      setLoading(false);
    }
  };

  const counts = useMemo(() => {
    const c: Record<StatusFilter, number> = { all: items.length, Planning: 0, Active: 0, OnHold: 0, Completed: 0, Cancelled: 0 };
    for (const p of items) c[p.status] = (c[p.status] ?? 0) + 1;
    return c;
  }, [items]);

  const totalBudget = useMemo(
    () => items.reduce((s, p) => s + Number(p.budget || 0), 0),
    [items],
  );

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    return items.filter((p) => {
      if (tab !== 'all' && p.status !== tab) return false;
      if (q) {
        const hay = `${p.code} ${p.name} ${p.description ?? ''}`.toLowerCase();
        if (!hay.includes(q)) return false;
      }
      return true;
    });
  }, [items, search, tab]);

  const columns: ModernTableColumn<Project>[] = [
    {
      key: 'code',
      header: 'الكود',
      widthClass: 'w-32',
      render: (p) => (
        <span className="inline-flex items-center rounded-md bg-slate-100 px-2 py-0.5 font-mono text-[11px] font-bold text-slate-700">
          {p.code}
        </span>
      ),
    },
    {
      key: 'name',
      header: 'المشروع',
      render: (p) => (
        <div className="min-w-0">
          <p className="truncate font-bold text-gray-900" title={p.name}>{p.name}</p>
          {p.description && (
            <p className="mt-0.5 line-clamp-1 text-xs text-gray-500">{p.description}</p>
          )}
        </div>
      ),
    },
    {
      key: 'status',
      header: 'الحالة',
      widthClass: 'w-32',
      render: (p) => {
        const meta = STATUS_META[p.status] ?? { label: p.status, tone: 'slate' as const };
        return <StatusPill tone={meta.tone} label={meta.label} showDot={false} />;
      },
    },
    {
      key: 'budget',
      header: 'الميزانية',
      align: 'end',
      widthClass: 'w-36',
      render: (p) => (
        <span className="font-mono font-semibold tabular-nums text-gray-900">
          {Number(p.budget || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
          <span className="ms-1 text-[10px] text-gray-500">ل.د</span>
        </span>
      ),
    },
    {
      key: 'start',
      header: 'تاريخ البدء',
      align: 'start',
      widthClass: 'w-36',
      render: (p) => (
        <span className="text-xs text-gray-600 tabular-nums">
          {formatDate(p.startDate)}
        </span>
      ),
    },
    {
      key: 'actions',
      header: '',
      align: 'end',
      widthClass: 'w-20',
      render: (p) => (
        <div className="flex items-center justify-end gap-1" onClick={(e) => e.stopPropagation()}>
          <Link
            href={`/projects/${p.id}/edit`}
            className="rounded-md p-1.5 text-gray-400 hover:bg-gray-100 hover:text-gray-700"
            title="تعديل"
          >
            <Pencil className="h-3.5 w-3.5" />
          </Link>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <PageHero
        eyebrow="إدارة المشاريع"
        title="المشاريع"
        subtitle="قائمة المشاريع النشطة والمكتملة — مع متابعة الميزانية، الأرباح والخسائر، العقود والمستخلصات"
        tone="violet"
        highlight={
          loading
            ? undefined
            : { label: 'إجمالي الميزانيات', value: `${formatCurrency(totalBudget)}` }
        }
        actions={
          <>
            <Link href="/projects/new">
              <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
                مشروع جديد
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
              <svg
                className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-white/60"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              <input
                type="search"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="ابحث بكود، اسم، أو وصف المشروع..."
                className="w-full rounded-lg border-0 bg-white/95 px-3 py-2 pe-9 text-sm text-gray-900 placeholder:text-gray-400 shadow-sm focus:outline-none focus:ring-2 focus:ring-white/40"
              />
            </div>
            <p className="text-xs text-white/70">
              {filtered.length} من {items.length} مشروع
            </p>
          </div>
        }
      />

      {error && !loading && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-rose-700" role="alert">
          <p className="font-semibold">تعذّر تحميل المشاريع</p>
          <p className="mt-1 text-sm">{error}</p>
        </div>
      )}

      {/* KPI strip */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <StatCard
          label="إجمالي المشاريع"
          value={loading ? '…' : items.length.toLocaleString('en-US')}
          icon={Briefcase}
          tone="violet"
          hint="في النظام"
        />
        <StatCard
          label="مشاريع نشطة"
          value={loading ? '…' : (counts.Active ?? 0).toLocaleString('en-US')}
          icon={TrendingUp}
          tone="green"
          hint={`${counts.all ? Math.round(((counts.Active ?? 0) / counts.all) * 100) : 0}% من الإجمالي`}
        />
        <StatCard
          label="إجمالي الميزانيات"
          value={loading ? '…' : formatCurrency(totalBudget)}
          icon={Wallet}
          tone="blue"
          hint="بالدينار الليبي"
        />
        <StatCard
          label="مكتمل / ملغي"
          value={loading ? '…' : `${(counts.Completed ?? 0) + (counts.Cancelled ?? 0)}`}
          icon={CheckCircle2}
          tone="slate"
          hint={`${counts.Completed ?? 0} مكتمل · ${counts.Cancelled ?? 0} ملغي`}
        />
      </div>

      {/* Status filter chips */}
      <FilterChips
        chips={[
          { key: 'all', label: 'الكل', count: counts.all, tone: 'blue' },
          { key: 'Planning', label: 'تخطيط', count: counts.Planning ?? 0, tone: 'slate' },
          { key: 'Active', label: 'نشط', count: counts.Active ?? 0, tone: 'green' },
          { key: 'OnHold', label: 'معلق', count: counts.OnHold ?? 0, tone: 'amber' },
          { key: 'Completed', label: 'مكتمل', count: counts.Completed ?? 0, tone: 'blue' },
          { key: 'Cancelled', label: 'ملغي', count: counts.Cancelled ?? 0, tone: 'red' },
        ]}
        active={tab}
        onChange={(k) => setTab(k === 'all' ? 'all' : (k as ProjectStatusName))}
      />

      {/* List */}
      {loading ? (
        <SkeletonTable rows={6} cols={5} />
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={<Briefcase className="h-12 w-12" />}
          title={items.length === 0 ? 'لا توجد مشاريع بعد' : 'لا توجد نتائج'}
          description={
            items.length === 0
              ? 'ابدأ بإنشاء أول مشروع لتنظيم أعمال الشركة. كل مشروع له ميزانية ومركز تكلفة تلقائي.'
              : 'جرّب تغيير الفلاتر أو البحث بكلمات مختلفة.'
          }
          action={
            items.length === 0 ? (
              <Link href="/projects/new">
                <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
                  إنشاء مشروع جديد
                </Button>
              </Link>
            ) : (
              <Button
                variant="secondary"
                onClick={() => {
                  setSearch('');
                  setTab('all');
                }}
              >
                مسح الفلاتر
              </Button>
            )
          }
        />
      ) : (
        <ModernTable
          columns={columns}
          rows={filtered}
          rowKey={(p) => p.id}
          onRowClick={(p) => {
            window.location.href = `/projects/${p.id}`;
          }}
        />
      )}
    </div>
  );
}
