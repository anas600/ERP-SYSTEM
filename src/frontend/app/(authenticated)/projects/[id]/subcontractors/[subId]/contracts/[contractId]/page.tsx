'use client';

// Sprint 64 / DEC-226 — Sub-Contract detail page (main visual surface).
//
// Shows:
//   - Sub-contract header (number, scope, value, retention, status)
//   - The SubStatement (the main visual element — DEC-225)
//   - Billings list (uses existing /api/sub-contracts/{id}/billings)
//   - Payments list (uses existing /api/sub-contracts/{id}/payments)
//
// L19 / DEC-095: ids come from the URL.

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import {
  ArrowRight, FileText, Wallet, Plus, Receipt, AlertCircle,
} from 'lucide-react';
import {
  PageHero, SectionCard, Button, EmptyState, SkeletonTable, StatusPill,
} from '@/components/ui';
import { SubStatement } from '@/components/subcontractor/SubStatement';
import { subcontractorsApi, fetchSubStatement } from '@/lib/api/subcontractors';
import { getErrorMessage } from '@/lib/api';
import { useAuth } from '@/lib/useAuth';
import { formatCurrency, formatDate } from '@/lib/utils';
import type {
  SubContract, Subcontractor, SubProgressBilling, SubPayment,
} from '@/lib/api/subcontractors';
import type { SubStatement as SubStatementModel } from '@/lib/api-types';

const STATUS_META: Record<number, { label: string; tone: 'green' | 'blue' | 'red' | 'slate' }> = {
  1: { label: 'نشط', tone: 'green' },
  2: { label: 'مكتمل', tone: 'blue' },
  3: { label: 'ملغى', tone: 'red' },
};

const BILLING_STATUS_META: Record<number, { label: string; tone: 'green' | 'amber' | 'red' | 'slate' | 'blue' }> = {
  1: { label: 'مسودة', tone: 'slate' },
  2: { label: 'معتمد', tone: 'green' },
  3: { label: 'مدفوع', tone: 'blue' },
  4: { label: 'ملغى', tone: 'red' },
};

export default function SubContractDetailPage() {
  const params = useParams();
  const router = useRouter();
  const projectId = String(params?.id ?? '');
  const subId = String(params?.subId ?? '');
  const contractId = String(params?.contractId ?? '');
  const { loading: authLoading } = useAuth();

  const [contract, setContract] = useState<SubContract | null>(null);
  const [sub, setSub] = useState<Subcontractor | null>(null);
  const [statement, setStatement] = useState<SubStatementModel | null>(null);
  const [billings, setBillings] = useState<SubProgressBilling[]>([]);
  const [payments, setPayments] = useState<SubPayment[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (authLoading || !contractId) return;
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const [c, st, bs, ps] = await Promise.all([
          subcontractorsApi.getSubContract(contractId),
          fetchSubStatement(contractId).catch(() => null),
          subcontractorsApi.listBillingsBySubContract(contractId).catch(() => []),
          subcontractorsApi.listPaymentsBySubContract(contractId).catch(() => []),
        ]);
        if (cancelled) return;
        setContract(c);
        setStatement(st);
        setBillings(bs);
        setPayments(ps);

        // Load subcontractor after we have the contract.
        const subData = await subcontractorsApi.getSubcontractor(c.subcontractorId);
        if (cancelled) return;
        setSub(subData);
      } catch (e) {
        if (!cancelled) setError(getErrorMessage(e, 'فشل التحميل'));
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => { cancelled = true; };
  }, [authLoading, contractId]);

  if (loading) {
    return (
      <div className="space-y-6" dir="rtl">
        <PageHero eyebrow="عقد باطن" title="جارٍ التحميل…" tone="emerald" />
        <SkeletonTable rows={5} cols={4} />
      </div>
    );
  }

  if (error || !contract) {
    return (
      <div className="space-y-6" dir="rtl">
        <PageHero eyebrow="عقد باطن" title="تعذّر التحميل" tone="rose" />
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-rose-700">
          <p className="font-semibold">خطأ</p>
          <p className="mt-1 text-sm">{error ?? 'العقد غير موجود.'}</p>
        </div>
        <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />} onClick={() => router.back()}>
          العودة
        </Button>
      </div>
    );
  }

  const meta = STATUS_META[contract.status] ?? { label: '—', tone: 'slate' as const };

  return (
    <div className="space-y-6" dir="rtl">
      <PageHero
        eyebrow="عقد باطن"
        title={contract.contractNumber}
        subtitle={`${sub?.name ?? '—'} — ${contract.scopeOfWork}`}
        tone="emerald"
        actions={
          <Link href={`/projects/${projectId}/subcontractors/${subId}/contracts`}>
            <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />}>
              العودة للقائمة
            </Button>
          </Link>
        }
        highlight={!loading ? { label: 'قيمة العقد', value: formatCurrency(contract.contractValue) } : undefined}
      />

      {/* Contract meta */}
      <div className="grid grid-cols-2 gap-3 md:grid-cols-4">
        <div className="rounded-xl border border-gray-100 bg-white p-4">
          <p className="text-[11px] font-bold uppercase tracking-wider text-gray-500">الاحتجاز</p>
          <p className="mt-1 text-base font-bold text-gray-900 tabular-nums">{contract.retentionPercent}%</p>
        </div>
        <div className="rounded-xl border border-gray-100 bg-white p-4">
          <p className="text-[11px] font-bold uppercase tracking-wider text-gray-500">تحرير بعد</p>
          <p className="mt-1 text-base font-bold text-gray-900 tabular-nums">{contract.retentionReleaseBilling} مستخلص</p>
        </div>
        <div className="rounded-xl border border-gray-100 bg-white p-4">
          <p className="text-[11px] font-bold uppercase tracking-wider text-gray-500">تاريخ البدء</p>
          <p className="mt-1 text-sm font-bold text-gray-900 tabular-nums">{formatDate(contract.startDate)}</p>
        </div>
        <div className="rounded-xl border border-gray-100 bg-white p-4">
          <p className="text-[11px] font-bold uppercase tracking-wider text-gray-500">الحالة</p>
          <p className="mt-1">
            <StatusPill tone={meta.tone} label={meta.label} showDot={false} />
          </p>
        </div>
      </div>

      {/* SubStatement — main visual element */}
      {statement && <SubStatement statement={statement} />}

      {/* Billings */}
      <SectionCard
        title="المستخلصات"
        actions={
          <Button
            variant="secondary"
            size="sm"
            iconLeft={<Plus className="h-3.5 w-3.5" />}
            onClick={() => router.push(
              `/projects/${projectId}/subcontractors/${subId}/contracts/${contractId}/billings/new`
            )}
          >
            مستخلص جديد
          </Button>
        }
      >
        {billings.length === 0 ? (
          <EmptyState
            icon={<FileText className="h-12 w-12" />}
            title="لا توجد مستخلصات بعد"
            description="أنشئ أول مستخلص لتتبع نسبة الإنجاز والمستحقات."
            action={
              <Button
                variant="primary"
                iconLeft={<Plus className="h-4 w-4" />}
                onClick={() => router.push(
                  `/projects/${projectId}/subcontractors/${subId}/contracts/${contractId}/billings/new`
                )}
              >
                مستخلص جديد
              </Button>
            }
          />
        ) : (
          <ul className="divide-y divide-gray-100">
            {billings.map((b) => {
              const m = BILLING_STATUS_META[b.status] ?? { label: '—', tone: 'slate' as const };
              return (
                <li key={b.id} className="flex items-center gap-3 rounded-lg px-3 py-3 hover:bg-emerald-50/30">
                  <Wallet className="h-5 w-5 text-emerald-500" />
                  <div className="min-w-0 flex-1">
                    <p className="text-sm font-bold text-gray-900">{b.billingNumber}</p>
                    <p className="text-[11px] text-gray-500 tabular-nums">{formatDate(b.billingDate)} · {b.workCompletedPercent}% إنجاز</p>
                  </div>
                  <div className="text-end">
                    <p className="text-sm font-bold tabular-nums text-gray-900">{formatCurrency(b.netPayable)}</p>
                    <p className="text-[11px] text-gray-500 tabular-nums">صافي</p>
                  </div>
                  <StatusPill tone={m.tone} label={m.label} showDot={false} />
                </li>
              );
            })}
          </ul>
        )}
      </SectionCard>

      {/* Payments */}
      <SectionCard
        title="المدفوعات"
        actions={
          <Button
            variant="secondary"
            size="sm"
            iconLeft={<Plus className="h-3.5 w-3.5" />}
            onClick={() => router.push(
              `/projects/${projectId}/subcontractors/${subId}/contracts/${contractId}/payments/new`
            )}
          >
            دفعة جديدة
          </Button>
        }
      >
        {payments.length === 0 ? (
          <EmptyState
            icon={<Receipt className="h-12 w-12" />}
            title="لا توجد مدفوعات بعد"
            description="سجّل أول دفعة (أو حرّر احتجاز) بعد اعتماد المستخلصات."
            action={
              <Button
                variant="primary"
                iconLeft={<Plus className="h-4 w-4" />}
                onClick={() => router.push(
                  `/projects/${projectId}/subcontractors/${subId}/contracts/${contractId}/payments/new`
                )}
              >
                دفعة جديدة
              </Button>
            }
          />
        ) : (
          <ul className="divide-y divide-gray-100">
            {payments.map((p) => (
              <li key={p.id} className="flex items-center gap-3 rounded-lg px-3 py-3 hover:bg-blue-50/30">
                <Receipt className="h-5 w-5 text-blue-500" />
                <div className="min-w-0 flex-1">
                  <p className="text-sm font-bold text-gray-900">{p.paymentNumber}</p>
                  <p className="text-[11px] text-gray-500 tabular-nums">
                    {formatDate(p.paymentDate)}
                    {p.paymentMethod && ` · ${p.paymentMethod}`}
                    {p.retentionReleased > 0 && (
                      <span className="ms-1 inline-flex items-center rounded-full bg-amber-50 px-1.5 py-0.5 text-[10px] font-bold text-amber-700 ring-1 ring-amber-200">
                        تحرير احتجاز
                      </span>
                    )}
                  </p>
                </div>
                <div className="text-end">
                  <p className="text-sm font-bold tabular-nums text-gray-900">
                    {formatCurrency(p.amount + p.retentionReleased)}
                  </p>
                  {p.retentionReleased > 0 && (
                    <p className="text-[11px] text-amber-700 tabular-nums">
                      احتجاز: {formatCurrency(p.retentionReleased)}
                    </p>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </SectionCard>

      {!statement && (
        <div className="flex items-start gap-3 rounded-2xl border border-amber-200 bg-amber-50 p-4 text-amber-700">
          <AlertCircle className="h-5 w-5 flex-shrink-0" />
          <p className="text-sm">
            كشف الحساب غير متاح لهذه العقد (لا توجد مستخلصات أو مدفوعات بعد).
          </p>
        </div>
      )}
    </div>
  );
}
