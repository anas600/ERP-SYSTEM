// Sprint 5 (Phase 5.2) — CoA loading skeleton.
//
// The new CoA page is a hierarchical tree (rows with indentation per depth).
// The skeleton hints at that layout with rows of varying widths — narrower
// rows look like deep tree leaves, wider ones like root nodes.

import { Skeleton } from '@/components/ui';

export default function Loading() {
  // We render 8 rows with a decreasing width pattern to mimic the indentation
  // visual of a tree. Kept as a single card so the layout shift is minimal.
  const widths = ['w-full', 'w-11/12', 'w-10/12', 'w-9/12', 'w-8/12', 'w-7/12', 'w-6/12', 'w-5/12'];
  return (
    <div dir="rtl" className="space-y-4">
      {/* PageHeader skeleton */}
      <div className="bg-white rounded-xl shadow-sm p-6">
        <Skeleton width="w-48" height="h-7" className="mb-2" />
        <Skeleton width="w-64" height="h-4" />
      </div>
      {/* Filter bar skeleton */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-4">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
          <Skeleton height="h-10" />
          <Skeleton height="h-10" />
          <Skeleton height="h-10" />
        </div>
      </div>
      {/* Tree rows skeleton */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        {Array.from({ length: 8 }).map((_, i) => (
          <div
            key={i}
            className="flex items-center gap-3 px-4 py-3 border-b border-gray-100 last:border-0"
          >
            <Skeleton width="w-5" height="h-5" />
            <Skeleton width="w-5" height="h-5" />
            <Skeleton width={widths[i % widths.length]} height="h-4" />
            <div className="flex-1" />
            <Skeleton width="w-16" height="h-3" />
            <Skeleton width="w-20" height="h-5" rounded />
          </div>
        ))}
      </div>
    </div>
  );
}
