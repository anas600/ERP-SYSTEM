'use client';

// Sprint 64 / DEC-226 — Subcontractor list page for a project.
//
// Lists every subcontractor with at least one sub-contract on this project.
// Tapping a card → /projects/[id]/subcontractors/[subId].
//
// L19 / DEC-095: projectId comes from the route param, not the request body.

import { useEffect, useMemo, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { Plus, Search, HardHat, ArrowRight } from 'lucide-react';
import Link from 'next/link';
import {
  PageHero, SectionCard, Button, EmptyState, SkeletonTable,
} from '@/components/ui';
import { SubcontractorCard } from '@/components/subcontractor/SubcontractorCard';
import { subcontractorsApi } from '@/lib/api/subcontractors';
import { getErrorMessage } from '@/lib/api';
import { useAuth } from '@/lib/useAuth';
import type { SubContract, Subcontractor } from '@/lib/api/subcontractors';

export default function ProjectSubcontractorsPage() {
  const params = useParams();
  const router = useRouter();
  const projectId = String(params?.id ?? '');
  const { loading: authLoading } = useAuth();

  const [subcontractors, setSubcontractors] = useState<Subcontractor[]>([]);
  const [subContracts, setSubContracts] = useState<SubContract[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');

  useEffect(() => {
    if (authLoading || !projectId) return;
    let cancelled = false;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const [allSubs, allContracts] = await Promise.all([
          subcontractorsApi.listSubcontractors(),
          subcontractorsApi.listSubContractsByProject(projectId),
        ]);
        if (cancelled) return;
        setSubcontractors(allSubs);
        setSubContracts(allContracts);
      } catch (e) {
        if (!cancelled) setError(getErrorMessage(e, 'فشل التحميل'));
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => { cancelled = true; };
  }, [authLoading, projectId]);

  // Subcontractors that have at least one sub-contract on this project.
  const visibleSubcontractors = useMemo(() => {
    const linkedIds = new Set(subContracts.map((sc) => sc.subcontractorId));
    const filtered = subcontractors.filter((s) => linkedIds.has(s.id));
    const q = search.trim().toLowerCase();
    if (!q) return filtered;
    return filtered.filter((s) =>
      `${s.code} ${s.name} ${s.nameAr ?? ''} ${s.tradeSpecialty ?? ''}`
        .toLowerCase()
        .includes(q)
    );
  }, [subcontractors, subContracts, search]);

  return (
    <div className="space-y-6" dir="rtl">
      <PageHero
        eyebrow="إدارة المقاولين"
        title="مقاولو الباطن"
        subtitle="كل المقاولين الفرعيين المرتبطين بهذا المشروع مع عقودهم ومستخلصاتهم ومدفوعاتهم"
        tone="amber"
        actions={
          <>
            <Link href={`/projects/${projectId}`}>
              <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />}>
                عودة للمشروع
              </Button>
            </Link>
            <Button
              variant="primary"
              iconLeft={<Plus className="h-4 w-4" />}
              onClick={() => router.push(`/projects/${projectId}/subcontractors/contracts/new`)}
            >
              عقد باطن جديد
            </Button>
          </>
        }
        toolbar={
          <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
            <div className="relative flex-1 max-w-md">
              <Search className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-white/60" />
              <input
                type="search"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="ابحث بكود، اسم، أو تخصص..."
                className="w-full rounded-lg border-0 bg-white/95 px-3 py-2 pe-9 text-sm text-gray-900 placeholder:text-gray-400 shadow-sm focus:outline-none focus:ring-2 focus:ring-white/40"
              />
            </div>
            <p className="text-xs text-white/70">
              {visibleSubcontractors.length} من {subcontractors.length} مقاول
            </p>
          </div>
        }
      />

      {error && !loading && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-rose-700" role="alert">
          <p className="font-semibold">تعذّر التحميل</p>
          <p className="mt-1 text-sm">{error}</p>
        </div>
      )}

      <SectionCard title="المقاولون الفرعيون على هذا المشروع">
        {loading ? (
          <SkeletonTable rows={4} cols={3} />
        ) : visibleSubcontractors.length === 0 ? (
          <EmptyState
            icon={<HardHat className="h-12 w-12" />}
            title={subcontractors.length === 0 ? 'لا يوجد مقاولون بعد' : 'لا توجد نتائج'}
            description={
              subcontractors.length === 0
                ? 'ابدأ بإنشاء عقد باطن جديد على هذا المشروع. ستحتاج لإضافة المقاول في خطوة لاحقة.'
                : 'جرّب تغيير كلمة البحث.'
            }
            action={
              <Button
                variant="primary"
                iconLeft={<Plus className="h-4 w-4" />}
                onClick={() => router.push(`/projects/${projectId}/subcontractors/contracts/new`)}
              >
                عقد باطن جديد
              </Button>
            }
          />
        ) : (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {visibleSubcontractors.map((s) => (
              <SubcontractorCard key={s.id} {...s} projectId={projectId} />
            ))}
          </div>
        )}
      </SectionCard>
    </div>
  );
}
