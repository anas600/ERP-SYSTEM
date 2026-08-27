'use client';

// 📊 ProjectProfitabilityCard — Sprint 65 / Wave 2A (DEC-234 + DEC-236)
//
// Shows the top-N most-profitable projects (revenue - total cost, including
// subcontractor cost) for the current company. Defaults to 5 projects, but the
// caller can pass any limit. The health-status pill (OK / AT_RISK / OVER_BUDGET)
// is colour-coded for quick scanning.
//
// The card is prop-driven: the parent page passes the projects array in. The
// component handles its own sort + slice + render.

import Link from 'next/link';
import { TrendingUp, ChevronLeft } from 'lucide-react';
import { Card, Badge, SectionCard, StatusPill } from '@/components/ui';
import type { ProjectProfitabilityResponse, ProjectHealthStatus } from '@/lib/api-types';
import { formatMoney, formatNumber } from '@/lib/format';

export interface ProjectProfitabilityCardProps {
  /** All profitability rows from /api/dashboard/project-profitability. */
  projects: ProjectProfitabilityResponse[];
  /** Max rows to show. Defaults to 5. */
  limit?: number;
  /** Optional currency code. */
  currency?: string;
  /** Loading state — renders a skeleton-ish message instead of the table. */
  loading?: boolean;
  /** Href of the "view all" link in the card footer. */
  viewAllHref?: string;
}

function toneForHealth(status: ProjectHealthStatus): 'green' | 'amber' | 'red' {
  switch (status) {
    case 'OK':
      return 'green';
    case 'AT_RISK':
      return 'amber';
    case 'OVER_BUDGET':
      return 'red';
  }
}

function labelForHealth(status: ProjectHealthStatus): string {
  switch (status) {
    case 'OK':
      return 'سليم';
    case 'AT_RISK':
      return 'في خطر';
    case 'OVER_BUDGET':
      return 'تجاوز الميزانية';
  }
}

export function ProjectProfitabilityCard({
  projects,
  limit = 5,
  currency = 'LYD',
  loading = false,
  viewAllHref = '/projects',
}: ProjectProfitabilityCardProps) {
  if (loading) {
    return (
      <Card title="ربحية المشاريع" description="جاري التحميل..." accent="purple">
        <div className="h-32 flex items-center justify-center text-ink-400 text-sm">
          ...
        </div>
      </Card>
    );
  }

  const top = (projects ?? [])
    .slice()
    .sort((a, b) => b.grossProfit - a.grossProfit)
    .slice(0, limit);

  return (
    <Card
      title="ربحية المشاريع"
      description={`أعلى ${Math.min(limit, top.length)} مشاريع من حيث الربح الإجمالي`}
      accent="purple"
      actions={
        <Badge variant="brand">
          <TrendingUp className="h-3 w-3 ml-1" />
          Top {top.length}
        </Badge>
      }
      footer={
        <Link
          href={viewAllHref}
          className="inline-flex items-center gap-1 text-sm font-semibold text-brand-600 hover:text-brand-700"
        >
          عرض كل المشاريع
          <ChevronLeft className="h-4 w-4" />
        </Link>
      }
    >
      {top.length === 0 ? (
        <div className="py-8 text-center text-sm text-ink-500">
          لا توجد مشاريع لعرضها.
        </div>
      ) : (
        <div className="divide-y divide-ink-100">
          {top.map((p) => (
            <div
              key={p.projectId}
              className="flex items-center gap-3 py-3 first:pt-0 last:pb-0"
              data-testid="profitability-row"
              data-health={p.healthStatus}
            >
              <div className="min-w-0 flex-1">
                <p className="text-sm font-semibold text-ink-800 truncate">
                  {p.projectName}
                </p>
                <p className="text-xs text-ink-500 font-mono">{p.projectCode}</p>
              </div>
              <div className="text-right flex-shrink-0">
                <p
                  className={
                    p.grossProfit >= 0
                      ? 'text-sm font-bold text-success-700 tabular-nums'
                      : 'text-sm font-bold text-rose-600 tabular-nums'
                  }
                >
                  {formatMoney(p.grossProfit, currency, 0)}
                </p>
                <p className="text-[11px] text-ink-500 tabular-nums">
                  هامش {formatNumber(p.profitMarginPercent, 1)}%
                </p>
              </div>
              <StatusPill tone={toneForHealth(p.healthStatus)} label={labelForHealth(p.healthStatus)} />
            </div>
          ))}
        </div>
      )}
    </Card>
  );
}

export default ProjectProfitabilityCard;
