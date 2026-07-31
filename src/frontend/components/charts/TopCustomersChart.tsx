'use client';

// Sprint 5 (Phase 4.1 / T3) — Top 5 Customers horizontal bar chart.
//
// Renders a horizontal bar chart with the top N customers (default 5) sorted
// by total spent. The X axis is the spend amount, the Y axis is the customer
// name (truncated for long names). Each bar shows the total on hover via the
// built-in Recharts tooltip.
//
// Data shape: GET /api/dashboard/charts/top-customers?limit=5
// → TopCustomerChartRow[] in lib/api.ts.

import { useMemo } from 'react';
import {
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Cell,
} from 'recharts';
import { Card } from '@/components/ui';
import { TopCustomerChartRow } from '@/lib/api';
import { formatNumber } from '@/lib/format';

// One color per rank; the array size matches the default limit of 5 but
// Recharts cycles it for larger limits, which is fine.
const RANK_COLORS = ['#1d4ed8', '#2563eb', '#3b82f6', '#60a5fa', '#93c5fd'];

export interface TopCustomersChartProps {
  data: TopCustomerChartRow[] | null | undefined;
  loading?: boolean;
  title?: string;
  description?: string;
}

export function TopCustomersChart({
  data,
  loading = false,
  title = 'أفضل 5 عملاء',
  description = 'حسب إجمالي المشتريات',
}: TopCustomersChartProps) {
  // Sort descending by totalSpent; cap visual name width with a truncation.
  const rows = useMemo(() => {
    const safe = (data ?? []).map((c) => ({
      name: c.customerName || '—',
      totalSpent: Number(c.totalSpent) || 0,
      invoiceCount: Number(c.invoiceCount) || 0,
    }));
    safe.sort((a, b) => b.totalSpent - a.totalSpent);
    return safe;
  }, [data]);

  return (
    <Card title={title} description={description}>
      <div className="h-64" dir="ltr">
        {loading ? (
          <div className="h-full flex items-center justify-center">
            <div className="h-8 w-8 rounded-full border-2 border-green-200 border-t-green-600 animate-spin" />
          </div>
        ) : rows.length === 0 ? (
          <div className="h-full flex items-center justify-center text-sm text-gray-400">
            لا توجد بيانات مبيعات بعد
          </div>
        ) : (
          <ResponsiveContainer width="100%" height="100%">
            <BarChart
              data={rows}
              layout="vertical"
              margin={{ top: 4, right: 16, left: 0, bottom: 0 }}
              // Gap between bars — kept tight for a compact feel.
              barCategoryGap="20%"
            >
              <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" horizontal={false} />
              <XAxis
                type="number"
                tick={{ fontSize: 11, fill: '#6b7280' }}
                stroke="#d1d5db"
                tickFormatter={(v) =>
                  typeof v === 'number' ? formatNumber(v, 0) : String(v)
                }
              />
              <YAxis
                type="category"
                dataKey="name"
                tick={{ fontSize: 11, fill: '#374151' }}
                stroke="#d1d5db"
                width={110}
                // Truncate long customer names so the chart stays readable.
                tickFormatter={(v: string) =>
                  v.length > 18 ? v.slice(0, 17) + '…' : v
                }
              />
              <Tooltip
                contentStyle={{
                  fontSize: 12,
                  borderRadius: 8,
                  border: '1px solid #e5e7eb',
                }}
                formatter={(v: number | string, _n, p) => {
                  const row = (p as { payload?: { invoiceCount?: number } })?.payload;
                  const inv = row?.invoiceCount ?? 0;
                  return [
                    typeof v === 'number' ? formatNumber(v, 2) : v,
                    `(${inv} فاتورة)`,
                  ];
                }}
              />
              <Bar dataKey="totalSpent" name="إجمالي المشتريات" radius={[0, 4, 4, 0]}>
                {rows.map((_, i) => (
                  <Cell key={`bar-${i}`} fill={RANK_COLORS[i % RANK_COLORS.length]} />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        )}
      </div>
    </Card>
  );
}
