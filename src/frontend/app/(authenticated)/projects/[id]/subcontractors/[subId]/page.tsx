'use client';

// Sprint 64 / DEC-226 — Subcontractor detail page.
//
// Shows the subcontractor master data + a list of their sub-contracts on
// this project + the cross-contract summary (via fetchSubcontractorProjectSummary).
//
// L19 / DEC-095: projectId + subId come from the URL — never from request bodies.

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import {
  ArrowRight, Building2, FileText, Phone, Mail, User2,
  Plus, Wallet, TrendingUp, HandCoins, Briefcase,
} from 'lucide-react';
import {
  PageHero, SectionCard, Button, EmptyState, SkeletonTable, StatCard, StatusPill,
} from '@/components/ui';
import { subcontractorsApi, fetchSubcontractorProjectSummary } from '@/lib/api/subcontractors';
import { getErrorMessage } from '@/lib/api';
import { useAuth } from '@/lib/useAuth';
import { formatCurrency, formatDate } from '@/lib/utils';
import type {
  SubContract, Subcontractor,
} from '@/lib/api/subcontractors';
import type { SubStatementSummary } from '@/lib/api-types';

const CONTRACT_STATUS_TONE: Record<number, { label: string; tone: 'green' | 'blue' | 'red' | 'slate' }> = {
  1: { label: 'نشط', tone: 'green' },
  2: { label: 'مكتمل', tone: 'blue' },
  3: { label: 'ملغى', tone: 'red' },
};

export default function SubcontractorDetailPage() {
  const params = useParams();
  const router = useRouter();
  const projectId = String(params?.id ?? '');
  const subId = String(params?.subId ?? '');
  const { loading: authLoading } = useAuth();

  const [sub, setSub] = useState<Subcontractor | null>(null);
  const [contracts, setContracts] = useState<SubContract[]>([]);
  const [summary, setSummary] = useState<SubStatementSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (authLoading || !projectId || !subId) return;
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const [subData, allContracts, summaryData] = await Promise.all([
          subcontractorsApi.getSubcontractor(subId),
          subcontractorsApi.listSubContractsByProject(projectId),
          fetchSubcontractorProjectSummary(subId, projectId).catch(() => null),
        ]);
        if (cancelled) return;
        setSub(subData);
        setContracts(allContracts.filter((c) => c.subcontractorId === subId));
        setSummary(summaryData);
      } catch (e) {
        if (!cancelled) setError(getErrorMessage(e, 'فشل التحميل'));
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => { cancelled = true; };
  }, [authLoading, projectId, subId]);

  return (
    <div className="space-y-6" dir="rtl">
      <PageHero
        eyebrow="تفاصيل المقاول الباطن"
        title={sub ? `${sub.code} — ${sub.name}` : 'مقاول باطن'}
        subtitle="البيانات الأساسية + عقود الباطن على هذا المشروع + ملخص مالي"
        tone="amber"
        actions={
          <Link href={`/projects/${projectId}/subcontractors`}>
            <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />}>
              عودة للقائمة
            </Button>
          </Link>
        }
      />

      {error && !loading && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-rose-700" role="alert">
          <p className="font-semibold">تعذّر التحميل</p>
          <p className="mt-1 text-sm">{error}</p>
        </div>
      )}

      {/* Master data card */}
      {sub && (
        <SectionCard title="البيانات الأساسية">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2 lg:grid-cols-4">
            <div className="rounded-xl border border-gray-100 bg-white p-4">
              <p className="text-[11px] font-bold uppercase tracking-wider text-gray-500">الكود</p>
              <p className="mt-1 font-mono text-base font-bold text-gray-900">{sub.code}</p>
            </div>
            <div className="rounded-xl border border-gray-100 bg-white p-4">
              <p className="text-[11px] font-bold uppercase tracking-wider text-gray-500">التخصص</p>
              <p className="mt-1 text-base font-bold text-gray-900">{sub.tradeSpecialty || '—'}</p>
            </div>
            <div className="rounded-xl border border-gray-100 bg-white p-4">
              <p className="text-[11px] font-bold uppercase tracking-wider text-gray-500">المسؤول</p>
              <p className="mt-1 flex items-center gap-1.5 text-sm font-bold text-gray-900">
                <User2 className="h-3.5 w-3.5 text-gray-400" />
                {sub.contactPerson || '—'}
              </p>
            </div>
            <div className="rounded-xl border border-gray-100 bg-white p-4">
              <p className="text-[11px] font-bold uppercase tracking-wider text-gray-500">الحالة</p>
              <p className="mt-1">
                <StatusPill
                  tone={sub.isActive ? 'green' : 'red'}
                  label={sub.isActive ? 'نشط' : 'معطّل'}
                />
              </p>
            </div>
            <div className="rounded-xl border border-gray-100 bg-white p-4 md:col-span-2">
              <p className="text-[11px] font-bold uppercase tracking-wider text-gray-500">الهاتف</p>
              <p className="mt-1 flex items-center gap-1.5 text-sm font-bold text-gray-900 tabular-nums">
                <Phone className="h-3.5 w-3.5 text-gray-400" />
                {sub.phone || '—'}
              </p>
            </div>
            <div className="rounded-xl border border-gray-100 bg-white p-4 md:col-span-2">
              <p className="text-[11px] font-bold uppercase tracking-wider text-gray-500">البريد الإلكتروني</p>
              <p className="mt-1 flex items-center gap-1.5 text-sm font-bold text-gray-900">
                <Mail className="h-3.5 w-3.5 text-gray-400" />
                {sub.email || '—'}
              </p>
            </div>
          </div>
        </SectionCard>
      )}

      {/* Cross-contract summary */}
      {summary && (
        <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
          <StatCard
            label="عدد العقود"
            value={summary.subContractCount.toString()}
            icon={Briefcase}
            tone="violet"
            hint="على هذا المشروع"
          />
          <StatCard
            label="إجمالي قيمة العقود"
            value={formatCurrency(summary.totalContractValue)}
            icon={Wallet}
            tone="blue"
            hint="ل.د"
          />
          <StatCard
            label="إجمالي المستخلصات"
            value={formatCurrency(summary.totalBilled)}
            icon={TrendingUp}
            tone="blue"
            hint="إجمالي مفوتر"
          />
          <StatCard
            label="الرصيد المستحق"
            value={formatCurrency(summary.totalOutstanding)}
            icon={HandCoins}
            tone={summary.totalOutstanding > 0 ? 'amber' : 'green'}
            hint={summary.totalOutstanding > 0 ? 'يستحق السداد' : 'مسوّى'}
          />
        </div>
      )}

      {/* Contracts list */}
      <SectionCard
        title="عقود الباطن على هذا المشروع"
        actions={
          <Button
            variant="primary"
            size="sm"
            iconLeft={<Plus className="h-3.5 w-3.5" />}
            onClick={() => router.push(`/projects/${projectId}/subcontractors/${subId}/contracts/new`)}
          >
            عقد جديد
          </Button>
        }
      >
        {loading ? (
          <SkeletonTable rows={3} cols={5} />
        ) : contracts.length === 0 ? (
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
          <ul className="divide-y divide-gray-100">
            {contracts.map((c) => {
              const meta = CONTRACT_STATUS_TONE[c.status] ?? { label: '—', tone: 'slate' as const };
              return (
                <li key={c.id}>
                  <Link
                    href={`/projects/${projectId}/subcontractors/${subId}/contracts/${c.id}`}
                    className="flex items-center gap-3 rounded-lg px-3 py-3 transition hover:bg-amber-50/40"
                  >
                    <Building2 className="h-5 w-5 text-amber-500" />
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-bold text-gray-900">{c.contractNumber}</p>
                      <p className="line-clamp-1 text-xs text-gray-500">{c.scopeOfWork}</p>
                    </div>
                    <div className="text-end">
                      <p className="text-sm font-bold tabular-nums text-gray-900">
                        {formatCurrency(c.contractValue)}
                      </p>
                      <p className="text-[11px] text-gray-500 tabular-nums">
                        {c.startDate ? formatDate(c.startDate) : '—'}
                      </p>
                    </div>
                    <StatusPill tone={meta.tone} label={meta.label} showDot={false} />
                  </Link>
                </li>
              );
            })}
          </ul>
        )}
      </SectionCard>
    </div>
  );
}
