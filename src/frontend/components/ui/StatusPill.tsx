'use client';

// Sprint 59 — StatusPill + ProgressBar + SectionCard.
//
// Reusable atoms for the inventory redesign.
//   <StatusPill>  — colored chip with optional dot prefix
//   <ProgressBar> — for stock level vs reorder threshold
//   <SectionCard> — content card with header row

import { ReactNode } from 'react';
import { cn } from '@/lib/utils';

// ============== StatusPill ==============

export type StatusPillTone =
  | 'green' | 'red' | 'amber' | 'blue' | 'slate' | 'purple' | 'violet' | 'sky' | 'rose';

const PILL_TONES: Record<StatusPillTone, { bg: string; text: string; dot: string; ring: string }> = {
  green:  { bg: 'bg-emerald-50', text: 'text-emerald-700', dot: 'bg-emerald-500', ring: 'ring-emerald-200' },
  red:    { bg: 'bg-rose-50',    text: 'text-rose-700',    dot: 'bg-rose-500',    ring: 'ring-rose-200' },
  amber:  { bg: 'bg-amber-50',   text: 'text-amber-700',   dot: 'bg-amber-500',   ring: 'ring-amber-200' },
  blue:   { bg: 'bg-blue-50',    text: 'text-blue-700',    dot: 'bg-blue-500',    ring: 'ring-blue-200' },
  slate:  { bg: 'bg-slate-50',   text: 'text-slate-700',   dot: 'bg-slate-500',   ring: 'ring-slate-200' },
  purple: { bg: 'bg-purple-50',  text: 'text-purple-700',  dot: 'bg-purple-500',  ring: 'ring-purple-200' },
  violet: { bg: 'bg-violet-50',  text: 'text-violet-700',  dot: 'bg-violet-500',  ring: 'ring-violet-200' },
  sky:    { bg: 'bg-sky-50',     text: 'text-sky-700',     dot: 'bg-sky-500',     ring: 'ring-sky-200' },
  rose:   { bg: 'bg-rose-50',    text: 'text-rose-700',    dot: 'bg-rose-500',    ring: 'ring-rose-200' },
};

export interface StatusPillProps {
  tone: StatusPillTone;
  label: string;
  showDot?: boolean;
  className?: string;
}

export function StatusPill({ tone, label, showDot = true, className }: StatusPillProps) {
  const t = PILL_TONES[tone];
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[11px] font-bold uppercase tracking-wider ring-1',
        t.bg,
        t.text,
        t.ring,
        className,
      )}
    >
      {showDot && <span className={cn('h-1.5 w-1.5 rounded-full', t.dot)} aria-hidden="true" />}
      {label}
    </span>
  );
}

// ============== ProgressBar ==============

export interface ProgressBarProps {
  /** Current value (0..max) */
  value: number;
  /** Maximum value. Defaults to 100. */
  max?: number;
  /** Optional threshold — values below this are highlighted as "low". */
  threshold?: number;
  /** Show the value as text on the right. */
  showValue?: boolean;
  /** Optional label rendered to the left. */
  label?: string;
  /** Tone override (otherwise inferred from value vs threshold). */
  tone?: 'green' | 'amber' | 'red' | 'blue';
  className?: string;
  /** Optional formatter for the value text. */
  formatValue?: (v: number) => string;
}

export function ProgressBar({
  value,
  max = 100,
  threshold,
  showValue = true,
  label,
  tone,
  className,
  formatValue,
}: ProgressBarProps) {
  const safeMax = max > 0 ? max : 1;
  const pct = Math.min(100, Math.max(0, (value / safeMax) * 100));
  const inferredTone: 'green' | 'amber' | 'red' | 'blue' =
    threshold != null
      ? value <= 0
        ? 'red'
        : value <= threshold
          ? 'amber'
          : 'green'
      : tone ?? 'blue';
  const finalTone = tone ?? inferredTone;

  const barClass = {
    green: 'bg-gradient-to-l from-emerald-400 to-emerald-500',
    amber: 'bg-gradient-to-l from-amber-400 to-amber-500',
    red:   'bg-gradient-to-l from-rose-400 to-rose-500',
    blue:  'bg-gradient-to-l from-blue-400 to-blue-500',
  }[finalTone];

  const labelClass = {
    green: 'text-emerald-700',
    amber: 'text-amber-700',
    red:   'text-rose-700',
    blue:  'text-blue-700',
  }[finalTone];

  return (
    <div className={cn('w-full', className)}>
      {(label || showValue) && (
        <div className="mb-1 flex items-center justify-between text-[11px] font-semibold text-gray-600">
          {label && <span className={cn(labelClass)}>{label}</span>}
          {showValue && (
            <span className="tabular-nums text-gray-700">
              {formatValue ? formatValue(value) : value.toLocaleString('en-US')}
              {max !== 100 && <span className="text-gray-400"> / {max.toLocaleString('en-US')}</span>}
            </span>
          )}
        </div>
      )}
      <div
        className="h-2 w-full overflow-hidden rounded-full bg-gray-100"
        role="progressbar"
        aria-valuenow={Math.round(pct)}
        aria-valuemin={0}
        aria-valuemax={100}
      >
        <div
          className={cn('h-full rounded-full transition-all duration-500 ease-out', barClass)}
          style={{ width: `${pct}%` }}
        />
      </div>
    </div>
  );
}

// ============== SectionCard ==============

export interface SectionCardProps {
  title?: string;
  description?: string;
  actions?: ReactNode;
  className?: string;
  bodyClassName?: string;
  children: ReactNode;
  /** Removes the inner padding when used as a flush table wrapper. */
  flush?: boolean;
}

export function SectionCard({
  title,
  description,
  actions,
  className,
  bodyClassName,
  children,
  flush,
}: SectionCardProps) {
  return (
    <div
      className={cn(
        'overflow-hidden rounded-2xl bg-white shadow-sm ring-1 ring-gray-200/70',
        className,
      )}
    >
      {(title || actions) && (
        <div className="flex flex-col gap-1 border-b border-gray-100 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            {title && (
              <h3 className="text-base font-bold text-gray-900">{title}</h3>
            )}
            {description && (
              <p className="mt-0.5 text-xs text-gray-500">{description}</p>
            )}
          </div>
          {actions && <div className="flex items-center gap-2">{actions}</div>}
        </div>
      )}
      <div className={cn(flush ? '' : 'p-5', bodyClassName)}>{children}</div>
    </div>
  );
}
