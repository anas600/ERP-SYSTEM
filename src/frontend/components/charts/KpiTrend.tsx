'use client';

// Sprint 5 (Phase 4.1) — KPI trend arrow + percent delta.
//
// A tiny presentational component that shows ↑ or ↓ next to a percent number.
// Used inside the existing KPI tiles on the dashboard to indicate change vs
// the previous period (the BE may or may not include the trend field — when
// missing the parent just doesn't render <KpiTrend>).
//
// Colour: green for positive, red for negative. 0% renders a neutral dash.

import { ArrowUp, ArrowDown, Minus } from 'lucide-react';
import { cn } from '@/lib/utils';

export interface KpiTrendProps {
  /** Percent change vs the previous period. Can be negative. */
  value: number | null | undefined;
  /** When the parent has no value yet (loading) this hides the row. */
  loading?: boolean;
}

export function KpiTrend({ value, loading = false }: KpiTrendProps) {
  if (loading) {
    return <div className="h-3.5 w-14 mt-1 rounded bg-gray-100 animate-pulse" />;
  }
  if (value == null || Number.isNaN(value)) {
    return null;
  }
  const isUp = value > 0;
  const isDown = value < 0;
  const isFlat = !isUp && !isDown;
  const Icon = isUp ? ArrowUp : isDown ? ArrowDown : Minus;
  // Format with sign + 1 decimal; locale-independent, English numerals per
  // project convention.
  const abs = Math.abs(value);
  const formatted = `${isUp ? '+' : isDown ? '-' : ''}${abs.toFixed(1)}%`;

  return (
    <span
      className={cn(
        'inline-flex items-center gap-0.5 mt-1 text-xs font-semibold',
        isUp && 'text-green-600',
        isDown && 'text-red-600',
        isFlat && 'text-gray-500'
      )}
      title={`التغير عن الفترة السابقة: ${formatted}`}
    >
      <Icon className="h-3 w-3" />
      <span>{formatted}</span>
    </span>
  );
}
