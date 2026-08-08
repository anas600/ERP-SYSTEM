'use client';

// Sprint 59 — PageHero (modern page header).
//
// Replaces the old PageHeader with a richer layout: large title, subtitle,
// a left accent bar, and a right-side actions row that can hold buttons,
// filters and a search input. Designed to give each list page a strong
// "command center" feel without overwhelming the content.

import { ReactNode } from 'react';
import { cn } from '@/lib/utils';

export interface PageHeroProps {
  /** Small uppercase eyebrow above the title (e.g. "المخزون"). */
  eyebrow?: string;
  title: string;
  subtitle?: string;
  /** Big number shown on the right (e.g. total value of stock). */
  highlight?: { label: string; value: string };
  /** Right-side action row. */
  actions?: ReactNode;
  /** Bottom strip — usually a FilterBar with search + select. */
  toolbar?: ReactNode;
  /** Optional accent gradient (default: slate). */
  tone?: 'slate' | 'blue' | 'emerald' | 'amber' | 'violet' | 'rose';
  className?: string;
}

const TONES = {
  slate:   { bg: 'from-slate-900 via-slate-800 to-slate-900',  ring: 'ring-white/10',  accent: 'bg-sky-400',    text: 'text-slate-300' },
  blue:    { bg: 'from-indigo-600 via-blue-600 to-indigo-700',  ring: 'ring-white/20',  accent: 'bg-cyan-300',   text: 'text-indigo-100' },
  emerald: { bg: 'from-emerald-600 via-teal-600 to-cyan-700',   ring: 'ring-white/20',  accent: 'bg-amber-300',  text: 'text-emerald-100' },
  amber:   { bg: 'from-amber-500 via-orange-500 to-rose-500',    ring: 'ring-white/20',  accent: 'bg-yellow-200', text: 'text-amber-100' },
  violet:  { bg: 'from-violet-600 via-fuchsia-600 to-pink-600', ring: 'ring-white/20',  accent: 'bg-pink-300',   text: 'text-violet-100' },
  rose:    { bg: 'from-rose-600 via-pink-600 to-fuchsia-600',   ring: 'ring-white/20',  accent: 'bg-amber-200',  text: 'text-rose-100' },
} as const;

export function PageHero({
  eyebrow,
  title,
  subtitle,
  highlight,
  actions,
  toolbar,
  tone = 'slate',
  className,
}: PageHeroProps) {
  const t = TONES[tone];
  return (
    <div
      className={cn(
        'relative overflow-hidden rounded-2xl bg-gradient-to-br text-white shadow-lg ring-1',
        t.bg,
        t.ring,
        className,
      )}
    >
      {/* Decorative grid pattern */}
      <div
        className="pointer-events-none absolute inset-0 opacity-[0.07]"
        aria-hidden="true"
        style={{
          backgroundImage:
            'linear-gradient(rgba(255,255,255,.6) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,.6) 1px, transparent 1px)',
          backgroundSize: '32px 32px',
        }}
      />
      {/* Accent bar */}
      <div className={cn('absolute right-0 top-0 h-full w-1.5', t.accent)} aria-hidden="true" />

      <div className="relative px-5 sm:px-7 pt-6 pb-2">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
          <div className="min-w-0 flex-1">
            {eyebrow && (
              <p className={cn('text-[11px] font-bold uppercase tracking-[0.2em]', t.text)}>
                {eyebrow}
              </p>
            )}
            <h1 className="mt-1 text-2xl sm:text-3xl font-extrabold leading-tight tracking-tight">
              {title}
            </h1>
            {subtitle && (
              <p className={cn('mt-1.5 text-sm sm:text-base max-w-2xl', t.text)}>
                {subtitle}
              </p>
            )}
          </div>

          {(highlight || actions) && (
            <div className="flex flex-col items-stretch gap-3 lg:items-end">
              {highlight && (
                <div className="rounded-xl bg-white/10 px-4 py-2.5 ring-1 ring-white/20 backdrop-blur">
                  <p className="text-[11px] font-medium uppercase tracking-wider text-white/70">
                    {highlight.label}
                  </p>
                  <p className="text-2xl font-bold tabular-nums">{highlight.value}</p>
                </div>
              )}
              {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
            </div>
          )}
        </div>

        {toolbar && (
          <div className="mt-5 -mx-5 sm:-mx-7 -mb-2 border-t border-white/10 bg-white/5 px-5 sm:px-7 py-3 backdrop-blur">
            {toolbar}
          </div>
        )}
      </div>
    </div>
  );
}
