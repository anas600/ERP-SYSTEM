'use client';

// Sprint 64 / DEC-222 + DEC-226 — Create a new sub-contract (project-level entry).
//
// Spec path: /projects/[id]/subcontractors/contracts/new
//   - Lets the user pick the subcontractor from a dropdown.
//   - For a "preselected subcontractor" shortcut, see also:
//     /projects/[id]/subcontractors/[subId]/contracts/new
//
// Both paths submit to POST /api/projects/{projectId}/sub-contracts.

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
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

  return (
    <div className="space-y-6" dir="rtl">
      <PageHero
        eyebrow="إنشاء عقد باطن"
        title="عقد جديد"
        subtitle="اختر المقاول + نطاق العمل + القيمة + شروط الاحتجاز"
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
            subcontractors={subcontractors}
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
