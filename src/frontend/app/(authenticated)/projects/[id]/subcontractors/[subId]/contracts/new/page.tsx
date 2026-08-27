'use client';

// Sprint 64 / DEC-222 + DEC-226 — Create a new sub-contract.
//
// Two flavors depending on whether the route carries a [subId] or not:
//   /projects/[id]/subcontractors/contracts/new
//       → choose the subcontractor from a dropdown (no [subId])
//   /projects/[id]/subcontractors/[subId]/contracts/new
//       → subcontractor is pre-selected from the URL (with [subId])
//
// We handle both via the optional [subId] route param.

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight } from 'lucide-react';
import { PageHero, SectionCard, Button, SkeletonTable } from '@/components/ui';
import { SubContractForm } from '@/components/subcontractor/SubContractForm';
import { subcontractorsApi } from '@/lib/api/subcontractors';
import { getErrorMessage } from '@/lib/api';
import { useAuth } from '@/lib/useAuth';
import type { Subcontractor } from '@/lib/api/subcontractors';

export default function NewSubContractPage() {
  const params = useParams();
  const router = useRouter();
  const projectId = String(params?.id ?? '');
  const subIdFromUrl = params?.subId ? String(params.subId) : null;
  const { loading: authLoading } = useAuth();

  const [subcontractors, setSubcontractors] = useState<Subcontractor[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (authLoading || !projectId) return;
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const subs = await subcontractorsApi.listSubcontractors();
        if (cancelled) return;
        setSubcontractors(subs);
      } catch (e) {
        if (!cancelled) setError(getErrorMessage(e, 'فشل التحميل'));
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => { cancelled = true; };
  }, [authLoading, projectId]);

  // If [subId] is in the URL but we don't have a subcontractor record for it,
  // fail fast.
  const initialSub = subIdFromUrl
    ? subcontractors.find((s) => s.id === subIdFromUrl) ?? null
    : null;
  if (subIdFromUrl && !loading && !initialSub) {
    return (
      <div className="space-y-6" dir="rtl">
        <PageHero
          eyebrow="إنشاء عقد"
          title="المقاول غير موجود"
          tone="rose"
        />
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-rose-700">
          المقاول الباطن المحدد غير موجود في هذه الشركة.
        </div>
        <Link href={`/projects/${projectId}/subcontractors`}>
          <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />}>
            العودة للقائمة
          </Button>
        </Link>
      </div>
    );
  }

  // If [subId] was in the URL, prepopulate the subcontractorId.
  const preloadedSubs: Subcontractor[] = subIdFromUrl && initialSub
    ? [initialSub]
    : subcontractors;

  return (
    <div className="space-y-6" dir="rtl">
      <PageHero
        eyebrow="إنشاء عقد باطن"
        title="عقد جديد"
        subtitle="حدد المقاول، رقم العقد، نطاق العمل، والقيمة + شروط الاحتجاز"
        tone="emerald"
        actions={
          <Button
            variant="secondary"
            iconLeft={<ArrowRight className="h-4 w-4" />}
            onClick={() => router.back()}
          >
            إلغاء
          </Button>
        }
      />

      {error && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-rose-700" role="alert">
          <p className="font-semibold">تعذّر التحميل</p>
          <p className="mt-1 text-sm">{error}</p>
        </div>
      )}

      <SectionCard>
        {loading ? (
          <SkeletonTable rows={4} cols={2} />
        ) : subcontractors.length === 0 ? (
          <div className="rounded-2xl border border-amber-200 bg-amber-50 p-4 text-amber-700">
            لا يوجد مقاولون باطن مسجلون. أضف مقاولاً أولاً من شاشة إدارة المقاولين.
          </div>
        ) : (
          <SubContractForm
            subcontractors={preloadedSubs}
            submitting={submitting}
            onCancel={() => router.back()}
            onSubmit={async (data) => {
              setSubmitting(true);
              try {
                const created = await subcontractorsApi.createSubContract(projectId, data);
                router.push(
                  `/projects/${projectId}/subcontractors/${created.subcontractorId}/contracts/${created.id}`
                );
              } finally {
                setSubmitting(false);
              }
            }}
          />
        )}
      </SectionCard>
    </div>
  );
}
