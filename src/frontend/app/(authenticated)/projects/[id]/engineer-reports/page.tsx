'use client';

// صفحة قائمة تقارير المهندس للمشروع (Sprint 61, DEC-192)
//
// - Filter by date range + status (All / Draft / Submitted / Approved / Rejected)
// - "New Report" button (top-right) navigates to /projects/[id]/engineer-reports/new
// - Each row links to /engineer-reports/[id]
//
// Bilingual (AR + EN). Uses the existing Modern design system.

import { useEffect, useMemo, useState } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import {
  ArrowRight, ClipboardList, Plus, Calendar, Filter, FileText,
} from 'lucide-react';
import {
  PageHero, SectionCard, EmptyState, SkeletonTable, Button, Input,
  ModernTable, StatusPill, FilterChips,
  type ModernTableColumn,
} from '@/components/ui';
import {
  projectsApi, getErrorMessage,
  ENGINEER_REPORT_STATUS_LABELS,
  type EngineerReportDto, type EngineerReportStatus, type Project,
} from '@/lib/api';
import { formatDate } from '@/lib/utils';

type StatusFilter = 'all' | EngineerReportStatus;

const STATUS_FILTERS: { key: StatusFilter; label: string; tone: 'blue' | 'amber' | 'green' | 'red' | 'slate' }[] = [
  { key: 'all', label: 'الكل / All', tone: 'blue' },
  { key: 'Draft', label: 'مسودة / Draft', tone: 'slate' },
  { key: 'Submitted', label: 'مُقدَّم / Submitted', tone: 'amber' },
  { key: 'Approved', label: 'معتمد / Approved', tone: 'green' },
  { key: 'Rejected', label: 'مرفوض / Rejected', tone: 'red' },
];

export default function ProjectEngineerReportsPage() {
  const params = useParams<{ id: string }>();
  const projectId = params.id;

  const [project, setProject] = useState<Project | null>(null);
  const [items, setItems] = useState<EngineerReportDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [from, setFrom] = useState<string>('');
  const [to, setTo] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const [proj, list] = await Promise.all([
        projectsApi.getProject(projectId).catch(() => null),
        projectsApi.listEngineerReports(projectId),
      ]);
      setProject(proj);
      setItems(Array.isArray(list) ? list : []);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل التقارير.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); /* eslint-disable-next-line react-hooks/exhaustive-deps */ }, [projectId]);

  const counts = useMemo(() => {
    const c: Record<StatusFilter, number> = {
      all: items.length,
      Draft: 0, Submitted: 0, Approved: 0, Rejected: 0,
    };
    for (const it of items) c[it.status] = (c[it.status] ?? 0) + 1;
    return c;
  }, [items]);

  const filtered = useMemo(() => {
    return items.filter((r) => {
      if (statusFilter !== 'all' && r.status !== statusFilter) return false;
      if (from && r.reportDate < from) return false;
      if (to && r.reportDate > to) return false;
      return true;
    });
  }, [items, from, to, statusFilter]);

  const columns: ModernTableColumn<EngineerReportDto>[] = [
    {
      key: 'date',
      header: 'التاريخ / Date',
      widthClass: 'w-32',
      render: (r) => (
        <span className="text-xs tabular-nums text-gray-700">
          {formatDate(r.reportDate)}
        </span>
      ),
    },
    {
      key: 'status',
      header: 'الحالة / Status',
      widthClass: 'w-32',
      align: 'center',
      render: (r) => {
        const meta = ENGINEER_REPORT_STATUS_LABELS[r.status] ?? ENGINEER_REPORT_STATUS_LABELS.Draft;
        return <StatusPill tone={meta.tone} label={`${meta.ar} / ${meta.en}`} showDot={false} />;
      },
    },
    {
      key: 'weather',
      header: 'الطقس / Weather',
      widthClass: 'w-40',
      render: (r) => (
        <span className="text-xs text-gray-600">{r.weather || '—'}</span>
      ),
    },
    {
      key: 'work',
      header: 'ما تم إنجازه / Work Done',
      render: (r) => {
        const preview = (r.workDone ?? '').slice(0, 80);
        return (
          <span className="line-clamp-1 text-sm text-gray-800" title={r.workDone}>
            {preview}
            {r.workDone && r.workDone.length > 80 ? '…' : ''}
          </span>
        );
      },
    },
    {
      key: 'engineer',
      header: 'المهندس / Engineer',
      widthClass: 'w-40',
      render: (r) => (
        <span className="text-xs text-gray-600">{r.engineerName ?? r.engineerId.slice(0, 8)}</span>
      ),
    },
    {
      key: 'actions',
      header: '',
      align: 'end',
      widthClass: 'w-24',
      render: (r) => (
        <Link href={`/engineer-reports/${r.id}`}>
          <Button variant="ghost" size="sm" iconLeft={<FileText className="h-3.5 w-3.5" />}>
            عرض / View
          </Button>
        </Link>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <PageHero
        eyebrow="إدارة المشاريع / Project Management"
        title="تقارير المهندس / Engineer Reports"
        subtitle={
          project
            ? `${project.code} — ${project.name}`
            : 'تقارير المهندس اليومية للمشروع'
        }
        tone="violet"
        actions={
          <>
            <Link href={`/projects/${projectId}/engineer-reports/new`}>
              <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
                تقرير جديد / New Report
              </Button>
            </Link>
            <Link href={`/projects/${projectId}`}>
              <Button variant="ghost" iconLeft={<ArrowRight className="h-4 w-4" />}>
                العودة / Back
              </Button>
            </Link>
          </>
        }
      />

      {error && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-rose-700" role="alert">
          <p className="font-semibold">تعذّر التحميل</p>
          <p className="mt-1 text-sm">{error}</p>
        </div>
      )}

      {/* Filter bar */}
      <SectionCard title="فلاتر / Filters" description="تاريخ + حالة التقرير">
        <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
          <div>
            <label className="mb-1 flex items-center gap-1 text-xs text-gray-600">
              <Calendar className="h-3 w-3" /> من تاريخ / From
            </label>
            <Input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
          </div>
          <div>
            <label className="mb-1 flex items-center gap-1 text-xs text-gray-600">
              <Calendar className="h-3 w-3" /> إلى تاريخ / To
            </label>
            <Input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
          </div>
          <div>
            <label className="mb-1 flex items-center gap-1 text-xs text-gray-600">
              <Filter className="h-3 w-3" /> الحالة / Status
            </label>
            <FilterChips
              chips={STATUS_FILTERS.map((s) => ({
                key: s.key,
                label: s.label,
                count: counts[s.key] ?? 0,
                tone: s.tone,
              }))}
              active={statusFilter}
              onChange={(k) => setStatusFilter(k as StatusFilter)}
            />
          </div>
        </div>
      </SectionCard>

      {/* Table */}
      {loading ? (
        <SkeletonTable rows={4} cols={5} />
      ) : filtered.length === 0 ? (
        <SectionCard>
          <EmptyState
            icon={<ClipboardList className="h-12 w-12" />}
            title="لا توجد تقارير"
            description="لا توجد تقارير تطابق الفلاتر. أنشئ تقريراً جديداً للبدء."
            action={
              <Link href={`/projects/${projectId}/engineer-reports/new`}>
                <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
                  تقرير جديد / New Report
                </Button>
              </Link>
            }
          />
        </SectionCard>
      ) : (
        <SectionCard
          flush
          title={`تقارير المشروع (${filtered.length})`}
          description={`${counts.Draft} مسودة · ${counts.Submitted} مُقدَّم · ${counts.Approved} معتمد · ${counts.Rejected} مرفوض`}
        >
          <ModernTable columns={columns} rows={filtered} rowKey={(r) => r.id} />
        </SectionCard>
      )}
    </div>
  );
}
