'use client';

// مكوّنات Skeleton — حالات تحميل موحدة (placeholder رمادي ينبض)
// Sprint 39 (DEC-125): uses ink-200 + the shimmer class for the modern effect
// Variants:
//   - Skeleton:       شريط/سطر واحد قابل للتخصيص
//   - SkeletonCard:   هيكل بطاقة كامل
//   - SkeletonTable:  صفوف × أعمدة
//   - SkeletonPage:   هيكل صفحة كاملة (header + table)

import { HTMLAttributes } from 'react';
import { cn } from '@/lib/utils';

// ============ Skeleton (سطر واحد) ============

export interface SkeletonProps extends HTMLAttributes<HTMLDivElement> {
  /** عرض الـ skeleton (Tailwind class) — افتراضي w-full */
  width?: string;
  /** ارتفاع الـ skeleton (Tailwind class) — افتراضي h-4 */
  height?: string;
  /** هل دائري (مثلاً للأفاتار)؟ */
  rounded?: boolean;
}

export function Skeleton({
  width = 'w-full',
  height = 'h-4',
  rounded = false,
  className,
  ...props
}: SkeletonProps) {
  return (
    <div
      role="status"
      aria-label="جاري التحميل"
      className={cn(
        // Sprint 39 (DEC-125): use shimmer class for the modern sweeping effect
        'shimmer',
        rounded ? 'rounded-full' : 'rounded',
        width,
        height,
        className
      )}
      {...props}
    />
  );
}

// ============ SkeletonCard (بطاقة) ============

export interface SkeletonCardProps {
  /** هل تعرض header داخل البطاقة؟ */
  hasHeader?: boolean;
  /** عدد أسطر المحتوى داخل البطاقة */
  lines?: number;
  className?: string;
}

export function SkeletonCard({ hasHeader = true, lines = 3, className }: SkeletonCardProps) {
  return (
    <div
      role="status"
      aria-label="جاري تحميل البطاقة"
      className={cn('bg-white rounded-xl shadow-soft border border-ink-200 p-5', className)}
    >
      {hasHeader && (
        <div className="mb-4 space-y-2">
          <Skeleton width="w-1/3" height="h-5" />
          <Skeleton width="w-1/2" height="h-3" />
        </div>
      )}
      <div className="space-y-2">
        {Array.from({ length: lines }).map((_, i) => (
          // تباين عرض الأسطر لمظهر طبيعي
          <Skeleton key={i} width={i === lines - 1 ? 'w-2/3' : 'w-full'} height="h-3" />
        ))}
      </div>
    </div>
  );
}

// ============ SkeletonTable (صفوف × أعمدة) ============

export interface SkeletonTableProps {
  /** عدد الصفوف */
  rows?: number;
  /** عدد الأعمدة */
  cols?: number;
  /** عرض الـ container — يترك للـ parent */
  className?: string;
}

export function SkeletonTable({ rows = 5, cols = 4, className }: SkeletonTableProps) {
  return (
    <div
      role="status"
      aria-label="جاري تحميل الجدول"
      dir="rtl"
      className={cn('bg-white rounded-xl shadow-soft border border-ink-200 overflow-hidden', className)}
    >
      {/* Header row */}
      <div className="bg-ink-50 border-b border-ink-200 px-4 py-3">
        <div
          className="grid gap-4"
          style={{ gridTemplateColumns: `repeat(${cols}, minmax(0, 1fr))` }}
        >
          {Array.from({ length: cols }).map((_, i) => (
            <Skeleton key={`h-${i}`} width="w-3/4" height="h-3" />
          ))}
        </div>
      </div>
      {/* Body rows */}
      <div className="divide-y divide-ink-100">
        {Array.from({ length: rows }).map((_, r) => (
          <div key={`r-${r}`} className="px-4 py-4">
            <div
              className="grid gap-4 items-center"
              style={{ gridTemplateColumns: `repeat(${cols}, minmax(0, 1fr))` }}
            >
              {Array.from({ length: cols }).map((_, c) => (
                <Skeleton
                  key={`c-${r}-${c}`}
                  width={c === 0 ? 'w-1/2' : c === cols - 1 ? 'w-3/4' : 'w-2/3'}
                  height="h-4"
                />
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

// ============ SkeletonPage (هيكل صفحة كاملة) ============

export interface SkeletonPageProps {
  /** هل يحتوي على PageHeader skeleton؟ */
  hasHeader?: boolean;
  /** عدد صفوف الجدول */
  rows?: number;
  className?: string;
}

export function SkeletonPage({ hasHeader = true, rows = 6, className }: SkeletonPageProps) {
  return (
    <div dir="rtl" className={cn('space-y-4', className)}>
      {hasHeader && (
        <div className="bg-white rounded-xl shadow-soft border border-ink-200 p-6">
          <Skeleton width="w-48" height="h-7" className="mb-2" />
          <Skeleton width="w-64" height="h-4" />
        </div>
      )}
      <SkeletonTable rows={rows} cols={4} />
    </div>
  );
}
