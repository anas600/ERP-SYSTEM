// Route-level loading for /activity.
// Next.js renders this immediately on navigation while the page's
// data fetch is in flight. Keeps the layout from flashing empty.

import { Skeleton } from '@/components/ui';

export default function Loading() {
  return (
    <div dir="rtl">
      <div className="mb-6 space-y-2">
        <div className="h-7 w-48 bg-gray-200 rounded animate-pulse" />
        <div className="h-4 w-72 bg-gray-200 rounded animate-pulse" />
      </div>
      <div className="bg-white rounded-xl shadow-sm border border-gray-100 p-4 sm:p-6">
        <ol>
          {Array.from({ length: 5 }).map((_, i) => {
            const isLast = i === 4;
            return (
              <div
                key={i}
                className={`relative flex gap-3 sm:gap-4 ${isLast ? '' : 'pb-5'}`}
              >
                {!isLast && (
                  <span
                    className="absolute right-[15px] top-8 bottom-0 w-px bg-gray-100"
                    aria-hidden="true"
                  />
                )}
                <Skeleton rounded width="w-8" height="h-8" />
                <div className="flex-1 min-w-0 space-y-2 pt-1">
                  <Skeleton width="w-1/3" height="h-4" />
                  <Skeleton width="w-2/3" height="h-3" />
                  <Skeleton width="w-1/4" height="h-3" />
                </div>
              </div>
            );
          })}
        </ol>
      </div>
    </div>
  );
}
