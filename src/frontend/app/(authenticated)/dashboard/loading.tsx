// Sprint 5 (Phase 5.2) — Dashboard loading skeleton.
//
// Matches the new chart-heavy layout (4 KPI tiles + 2 line/pie charts + 1
// bar chart). Kept purely declarative — no spinners. The existing
// TableSkeleton doesn't fit a dashboard with charts so we render a custom
// grid here.

import { Skeleton, SkeletonCard } from '@/components/ui';

export default function Loading() {
  return (
    <div dir="rtl" className="space-y-4">
      {/* PageHeader skeleton */}
      <div className="bg-white rounded-xl shadow-sm p-6">
        <Skeleton width="w-56" height="h-7" className="mb-2" />
        <Skeleton width="w-72" height="h-4" />
      </div>

      {/* KPI tiles (4 cards) */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {Array.from({ length: 4 }).map((_, i) => (
          <SkeletonCard key={i} hasHeader={false} lines={2} />
        ))}
      </div>

      {/* Row 1: revenue line + expense pie */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
          <Skeleton width="w-40" height="h-5" className="mb-2" />
          <Skeleton width="w-56" height="h-3" className="mb-4" />
          <Skeleton width="w-full" height="h-56" />
        </div>
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
          <Skeleton width="w-40" height="h-5" className="mb-2" />
          <Skeleton width="w-56" height="h-3" className="mb-4" />
          <Skeleton width="w-full" height="h-56" rounded />
        </div>
      </div>

      {/* Row 2: top customers bar (full width) */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
        <Skeleton width="w-40" height="h-5" className="mb-2" />
        <Skeleton width="w-64" height="h-3" className="mb-4" />
        <Skeleton width="w-full" height="h-56" />
      </div>
    </div>
  );
}
