'use client';

// Sprint 5 (Phase 4.1 / T2) — Expenses-by-Category pie chart for the dashboard.
//
// Renders a donut/pie chart with one slice per Expense-type account, top
// categories first. If the BE returns a `color` hex per slice, we honour it;
// otherwise we fall back to a small Tailwind-style palette (kept inline to
// avoid pulling in a dep).
//
// Data shape: GET /api/dashboard/charts/expenses-by-category?months=3
// → ExpenseCategorySlice[] in lib/api.ts.

import { useMemo } from 'react';
import {
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Tooltip,
  Legend,
} from 'recharts';
import { Card } from '@/components/ui';
import { ExpenseCategorySlice } from '@/lib/api';
import { formatNumber } from '@/lib/format';

// Fallback palette (used when the BE doesn't ship a `color` field, or ships
// an empty string). Recharts auto-picks a default but it tends to be muddy;
// this 8-color set matches the chart line colors for a coherent look.
const FALLBACK_PALETTE = [
  '#2563eb', // blue
  '#16a34a', // green
  '#dc2626', // red
  '#f59e0b', // amber
  '#7c3aed', // violet
  '#0891b2', // cyan
  '#db2777', // pink
  '#65a30d', // lime
];

export interface ExpenseByCategoryChartProps {
  data: ExpenseCategorySlice[] | null | undefined;
  loading?: boolean;
  title?: string;
  description?: string;
}

export function ExpenseByCategoryChart({
  data,
  loading = false,
  title = 'المصروفات حسب الفئة',
  description = 'آخر 3 أشهر',
}: ExpenseByCategoryChartProps) {
  // Aggregate the top 5 + "أخرى" bucket. This matches the hand-off: "Top 5
  // categories + Other". We keep all categories when there are ≤ 5 to avoid
  // the redundant "أخرى" group of size 0.
  const rows = useMemo(() => {
    const safe = (data ?? []).map((s) => ({
      name: s.category || '—',
      value: Number(s.amount) || 0,
      color: s.color || '',
    }));
    safe.sort((a, b) => b.value - a.value);
    if (safe.length <= 5) return safe;
    const top5 = safe.slice(0, 5);
    const otherSum = safe.slice(5).reduce((acc, s) => acc + s.value, 0);
    return [
      ...top5,
      { name: 'أخرى', value: otherSum, color: '' },
    ];
  }, [data]);

  // Resolved colors per row (BE color wins, else fallback palette cycles).
  const colors = useMemo(
    () => rows.map((r, i) => r.color || FALLBACK_PALETTE[i % FALLBACK_PALETTE.length]),
    [rows]
  );

  return (
    <Card title={title} description={description}>
      <div className="h-64" dir="ltr">
        {loading ? (
          <div className="h-full flex items-center justify-center">
            <div className="h-8 w-8 rounded-full border-2 border-purple-200 border-t-purple-600 animate-spin" />
          </div>
        ) : rows.length === 0 ? (
          <div className="h-full flex items-center justify-center text-sm text-gray-400">
            لا توجد مصروفات في الفترة المحددة
          </div>
        ) : (
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie
                data={rows}
                dataKey="value"
                nameKey="name"
                cx="50%"
                cy="50%"
                innerRadius={50}
                outerRadius={90}
                paddingAngle={2}
              >
                {rows.map((_, i) => (
                  <Cell key={`cell-${i}`} fill={colors[i]} />
                ))}
              </Pie>
              <Tooltip
                contentStyle={{
                  fontSize: 12,
                  borderRadius: 8,
                  border: '1px solid #e5e7eb',
                }}
                formatter={(v: number | string, n: string | number) => [
                  typeof v === 'number' ? formatNumber(v, 2) : v,
                  String(n),
                ]}
              />
              <Legend
                wrapperStyle={{ fontSize: 11, paddingTop: 4 }}
                iconType="circle"
              />
            </PieChart>
          </ResponsiveContainer>
        )}
      </div>
    </Card>
  );
}
