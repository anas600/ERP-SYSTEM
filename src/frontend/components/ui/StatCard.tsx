'use client';

// Sprint 59 — StatCard (modern KPI card with gradient + icon + trend).
//
// Used as the top-of-page metric for /inventory/* pages. Each card has a
// subtle gradient background, a colored icon disc, a big number (with
// tabular-nums for stable alignment), a label, a hint line and an optional
// delta indicator.

import { ReactNode } from 'react';
import { ArrowDownRight, ArrowUpRight, Minus } from 'lucide-react';
import { cn } from '@/lib/utils';

export type StatCardTone = 'blue' | 'green' | 'emerald' | 'amber' | 'red' | 'purple' | 'violet' | 'slate' | 'indigo';

const TONES: Record<StatCardTone, { bg: string; ring: string; text: string; soft: string; icon: string }> = {
  blue:    { bg: 'from-blue-50 to-white',     ring: 'ring-blue-100',    text: 'text-blue-700',    soft: 'bg-blue-100/70',    icon: 'text-blue-600' },
  green:   { bg: 'from-green-50 to-white',    ring: 'ring-green-100',   text: 'text-green-700',   soft: 'bg-green-100/70',   icon: 'text-green-600' },
  emerald: { bg: 'from-emerald-50 to-white',  ring: 'ring-emerald-100', text: 'text-emerald-700', soft: 'bg-emerald-100/70', icon: 'text-emerald-600' },
  amber:   { bg: 'from-amber-50 to-white',    ring: 'ring-amber-100',   text: 'text-amber-700',   soft: 'bg-amber-100/70',   icon: 'text-amber-600' },
  red:     { bg: 'from-rose-50 to-white',     ring: 'ring-rose-100',    text: 'text-rose-700',    soft: 'bg-rose-100/70',    icon: 'text-rose-600' },
  purple:  { bg: 'from-purple-50 to-white',   ring: 'ring-purple-100',  text: 'text-purple-700',  soft: 'bg-purple-100/70',  icon: 'text-purple-600' },
  violet:  { bg: 'from-violet-50 to-white',   ring: 'ring-violet-100',  text: 'text-violet-700',  soft: 'bg-violet-100/70',  icon: 'text-violet-600' },
  slate:   { bg: 'from-slate-50 to-white',    ring: 'ring-slate-100',   text: 'text-slate-700',   soft: 'bg-slate-100/70',   icon: 'text-slate-600' },
  indigo:  { bg: 'from-indigo-50 to-white',   ring: 'ring-indigo-100',  text: 'text-indigo-700',  soft: 'bg-indigo-100/70',  icon: 'text-indigo-600' },
};

export interface StatCardProps {
  label: string;
  value: string | number;
  hint?: string;
  icon?: React.ComponentType<{ className?: string }>;
  tone?: StatCardTone;
  /** Percent change vs previous period. Null hides the indicator. */
  delta?: number | null;
  /** Optional small slot below the hint (e.g. a small inline link). */
  footer?: ReactNode;
  /** Optional currency code shown as a prefix. */
  currency?: string;
  /** Loading state — shows a shimmer. */
  loading?: boolean;
  className?: string;
}

export function StatCard({
  label,
  value,
  hint,
  icon: Icon,
  tone = 'slate',
  delta,
  footer,
  currency,
  loading,
  className,
}: StatCardProps) {
  const t = TONES[tone];
  return (
    <div
      className={cn(
        'relative overflow-hidden rounded-2xl bg-gradient-to-br p-5 shadow-sm ring-1 transition-all hover:shadow-md',
        t.bg,
        t.ring,
        className,
      )}
    >
      {/* Decorative blob in the top-right */}
      <div
        className={cn(
          'pointer-events-none absolute -right-10 -top-10 h-32 w-32 rounded-full opacity-40 blur-2xl',
          t.soft,
        )}
        aria-hidden="true"
      />

      <div className="relative flex items-start justify-between gap-4">
        <div className="min-w-0 flex-1">
          <p className={cn('text-xs font-semibold uppercase tracking-wider', t.text)}>
            {label}
          </p>
          {loading ? (
            <div className="mt-2 h-9 w-24 rounded-md bg-white/60 animate-pulse" />
          ) : (
            <p className="mt-1.5 text-3xl font-bold text-gray-900 tabular-nums leading-tight">
              {currency && <span className="text-base font-semibold text-gray-500 ms-1">{currency}</span>}
              {value}
            </p>
          )}
          {hint && !loading && (
            <p className="mt-1 text-xs text-gray-500">{hint}</p>
          )}
          {delta != null && !loading && (
            <DeltaPill delta={delta} />
          )}
          {footer && <div className="mt-2">{footer}</div>}
        </div>
        {Icon && (
          <div
            className={cn(
              'flex h-11 w-11 flex-shrink-0 items-center justify-center rounded-xl shadow-sm',
              t.soft,
            )}
          >
            <Icon className={cn('h-5 w-5', t.icon)} />
          </div>
        )}
      </div>
    </div>
  );
}

function DeltaPill({ delta }: { delta: number }) {
  const positive = delta > 0.5;
  const negative = delta < -0.5;
  const Icon = positive ? ArrowUpRight : negative ? ArrowDownRight : Minus;
  const tone = positive
    ? 'bg-emerald-50 text-emerald-700 ring-emerald-100'
    : negative
      ? 'bg-rose-50 text-rose-700 ring-rose-100'
      : 'bg-gray-50 text-gray-600 ring-gray-200';
  const sign = positive ? '+' : '';
  return (
    <span
      className={cn(
        'mt-2 inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-semibold ring-1',
        tone,
      )}
    >
      <Icon className="h-3 w-3" />
      {sign}
      {delta.toFixed(1)}%
    </span>
  );
}
