'use client';

// Sprint 5 (Phase 4.1 / T1) — Revenue vs Expense line chart for the dashboard.
//
// Renders a 6-month trend line with three series:
//   - revenue (green)
//   - expense (red)
//   - net     (blue, profit = positive)
//
// The data shape comes from GET /api/dashboard/charts/revenue?months=6 and
// matches RevenueVsExpensePoint in lib/api.ts (which mirrors the C# DTO).
//
// We render the X axis with short Arabic month names (يناير، فبراير، ...) and
// the Y axis with English numerals per the project convention. The chart is
// wrapped in ResponsiveContainer so it shrinks to fit its parent (the Card).

import { useMemo } from 'react';
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
} from 'recharts';
import { Card } from '@/components/ui';
import { RevenueVsExpensePoint } from '@/lib/api';
import { formatNumber } from '@/lib/format';

// ============ Month label helpers ============
// ISO yyyy-MM → Arabic short name (يناير, فبراير, ...). Stable across renders.

const AR_MONTHS = [
  'يناير',
  'فبراير',
  'مارس',
  'أبريل',
  'مايو',
  'يونيو',
  'يوليو',
  'أغسطس',
  'سبتمبر',
  'أكتوبر',
  'نوفمبر',
  'ديسمبر',
];

function formatMonthLabel(yyyyMm: string): string {
  // yyyyMm is "2026-02" — UTC sortable.
  const parts = yyyyMm.split('-');
  if (parts.length !== 2) return yyyyMm;
  const monthIdx = Number(parts[1]) - 1;
  if (Number.isNaN(monthIdx) || monthIdx < 0 || monthIdx > 11) return yyyyMm;
  return AR_MONTHS[monthIdx];
}

// ============ Component ============

export interface RevenueChartProps {
  data: RevenueVsExpensePoint[] | null | undefined;
  loading?: boolean;
  /** Custom title shown in the Card header. */
  title?: string;
  /** Custom description shown under the title. */
  description?: string;
}

export function RevenueChart({
  data,
  loading = false,
  title = 'الإيرادات مقابل المصروفات',
  description = 'آخر 6 أشهر',
}: RevenueChartProps) {
  // Recharts wants a flat array of objects; our DTO is already that shape.
  // We map `month` → `name` (X axis label) here so the X axis is human-readable.
  const rows = useMemo(
    () =>
      (data ?? []).map((p) => ({
        name: formatMonthLabel(p.month),
        revenue: Number(p.revenue) || 0,
        expense: Number(p.expense) || 0,
        net: Number(p.net) || 0,
      })),
    [data]
  );

  return (
    <Card title={title} description={description}>
      <div className="h-64" dir="ltr">
        {loading ? (
          <div className="h-full flex items-center justify-center">
            <div className="h-8 w-8 rounded-full border-2 border-blue-200 border-t-blue-600 animate-spin" />
          </div>
        ) : rows.length === 0 ? (
          <div className="h-full flex items-center justify-center text-sm text-gray-400">
            لا توجد بيانات للعرض
          </div>
        ) : (
          <ResponsiveContainer width="100%" height="100%">
            <LineChart
              data={rows}
              margin={{ top: 8, right: 12, left: 0, bottom: 0 }}
            >
              <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
              <XAxis
                dataKey="name"
                tick={{ fontSize: 12, fill: '#6b7280' }}
                stroke="#d1d5db"
              />
              <YAxis
                tick={{ fontSize: 12, fill: '#6b7280' }}
                stroke="#d1d5db"
                tickFormatter={(v) =>
                  typeof v === 'number' ? formatNumber(v, 0) : String(v)
                }
                width={60}
              />
              <Tooltip
                contentStyle={{
                  fontSize: 12,
                  borderRadius: 8,
                  border: '1px solid #e5e7eb',
                }}
                // Numbers inside the tooltip are LYD; show with thousands sep.
                formatter={(v: number | string) =>
                  typeof v === 'number' ? formatNumber(v, 2) : v
                }
              />
              <Legend
                wrapperStyle={{ fontSize: 12, paddingTop: 4 }}
                iconType="circle"
              />
              <Line
                type="monotone"
                dataKey="revenue"
                name="الإيرادات"
                stroke="#16a34a"
                strokeWidth={2.5}
                dot={{ r: 3 }}
                activeDot={{ r: 5 }}
              />
              <Line
                type="monotone"
                dataKey="expense"
                name="المصروفات"
                stroke="#dc2626"
                strokeWidth={2.5}
                dot={{ r: 3 }}
                activeDot={{ r: 5 }}
              />
              <Line
                type="monotone"
                dataKey="net"
                name="الصافي"
                stroke="#2563eb"
                strokeWidth={2.5}
                dot={{ r: 3 }}
                activeDot={{ r: 5 }}
              />
            </LineChart>
          </ResponsiveContainer>
        )}
      </div>
    </Card>
  );
}
