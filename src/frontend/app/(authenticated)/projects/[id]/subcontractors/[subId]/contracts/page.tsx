'use client';

// Sprint 64 / DEC-226 — Sub-Contract list page (filtered to a single subcontractor).
//
// Shows every sub-contract for a (project, subcontractor) pair.
// Tapping a row → /projects/[id]/subcontractors/[subId]/contracts/[contractId].

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight, FileText, Plus } from 'lucide-react';
import {
  PageHero, SectionCard, Button, EmptyState, SkeletonTable, StatusPill, ModernTable, type ModernTableColumn,
} from '@/components/ui';
import { subcontractorsApi } from '@/lib/api/subcontractors';
import { getErrorMessage } from '@/lib/api';
import { useAuth } from '@/lib/useAuth';
import { formatCurrency, formatDate } from '@/lib/utils';
import type { SubContract } from '@/lib/api/subcontractors';

const STATUS_META: Record<number, { label: string; tone: 'green' | 'blue' | 'red' | 'slate' }> = {
  1: { label: 'نشط', tone: 'green' },
  2: { label: 'مكتمل', tone: 'blue' },
  3: { label: 'ملغى', tone: 'red' },
};

export default function SubContractsListPage() {
  const params = useParams();
  const router = useRouter();
  const projectId = String(params?.id ?? '');
  const subId = String(params?.subId ?? '');
  const { loading: authLoading } = useAuth();

  const [items, setItems] = useState<SubContract[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (authLoading || !projectId) return;
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const all = await subcontractorsApi.listSubContractsByProject(projectId);
        if (cancelled) return;
        setItems(all.filter((c) => c.subcontractorId === subId));
      } catch (e) {
        if (!cancelled) setError(getErrorMessage(e, 'فشل التحميل'));
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => { cancelled = true; };
  }, [authLoading, projectId, subId]);

  const columns: ModernTableColumn<SubContract>[] = [
    {
      key: 'number',
      header: 'رقم العقد',
      widthClass: 'w-32',
      render: (c) => (
        <span className="font-mono text-[12px] font-bold text-gray-900">{c.contractNumber}</span>
      ),
    },
    {
      key: 'scope',
      header: 'نطاق العمل',
      render: (c) => <span className="line-clamp-1 text-sm text-gray-700">{c.scopeOfWork}</span>,
    },
    {
      key: 'value',
      header: 'القيمة',
      align: 'end',
      widthClass: 'w-32',
      render: (c) => (
        <span className="font-mono text-sm font-bold tabular-nums text-gray-900">
          {formatCurrency(c.contractValue)}
        </span>
      ),
    },
    {
      key: 'retention',
      header: 'الاحتجاز',
      align: 'end',
      widthClass: 'w-24',
      render: (c) => (
        <span className="font-mono text-xs tabular-nums text-gray-700">{c.retentionPercent}%</span>
      ),
    },
    {
      key: 'status',
      header: 'الحالة',
      widthClass: 'w-28',
      render: (c) => {
        const m = STATUS_META[c.status] ?? { label: '—', tone: 'slate' as const };
        return <StatusPill tone={m.tone} label={m.label} showDot={false} />;
      },
    },
    {
      key: 'date',
      header: 'تاريخ البدء',
      widthClass: 'w-28',
      render: (c) => (
        <span className="text-xs tabular-nums text-gray-600">{formatDate(c.startDate)}</span>
      ),
    },
  ];

  return (
    <div className="space-y-6" dir="rtl">
      <PageHero
        eyebrow="عقود الباطن"
        title="قائمة العقود"
        subtitle="كل عقود الباطن لهذا المقاول على هذا المشروع"
        tone="emerald"
        actions={
          <>
            <Link href={`/projects/${projectId}/subcontractors/${subId}`}>
              <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />}>
                عودة للمقاول
              </Button>
            </Link>
            <Button
              variant="primary"
              iconLeft={<Plus className="h-4 w-4" />}
              onClick={() => router.push(`/projects/${projectId}/subcontractors/${subId}/contracts/new`)}
            >
              عقد جديد
            </Button>
          </>
        }
      />

      {error && !loading && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-rose-700" role="alert">
          <p className="font-semibold">تعذّر التحميل</p>
          <p className="mt-1 text-sm">{error}</p>
        </div>
      )}

      <SectionCard>
        {loading ? (
          <SkeletonTable rows={3} cols={6} />
        ) : items.length === 0 ? (
          <EmptyState
            icon={<FileText className="h-12 w-12" />}
            title="لا توجد عقود باطن بعد"
            description="أنشئ أول عقد لهذا المقاول على هذا المشروع."
            action={
              <Button
                variant="primary"
                iconLeft={<Plus className="h-4 w-4" />}
                onClick={() => router.push(`/projects/${projectId}/subcontractors/${subId}/contracts/new`)}
              >
                إنشاء عقد
              </Button>
            }
          />
        ) : (
          <ModernTable
            columns={columns}
            rows={items}
            rowKey={(c) => c.id}
            onRowClick={(c) => {
              router.push(`/projects/${projectId}/subcontractors/${subId}/contracts/${c.id}`);
            }}
          />
        )}
      </SectionCard>
    </div>
  );
}
