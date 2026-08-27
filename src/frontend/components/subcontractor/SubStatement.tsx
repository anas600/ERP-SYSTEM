'use client';

// Sprint 64 / DEC-225 — SubStatement display component.
//
// Visualises the per-sub-contract P&L:
//   - Health badge (OK / OVERDUE / SETTLED) — green / orange / blue
//   - 4 stat cards: Contract Value, Billed, Paid, Outstanding
//   - Retention summary row (withheld / released / net)
//   - Progress bar (work completed %)
//
// All Arabic. L19: the component is purely presentational; CompanyId is
// never rendered (the caller already knows it from the JWT context).

import { ReactNode } from 'react';
import { cn } from '@/lib/utils';
import type { SubStatement as SubStatementModel } from '@/lib/api-types';
import { formatDate, formatCurrency } from '@/lib/utils';

export interface SubStatementProps {
  statement: SubStatementModel;
  className?: string;
  /** Optional element to render in the top-right action slot. */
  actions?: ReactNode;
}

// Health status → tailwind classes (mirrors the StatusPill palette).
function healthToneClasses(status: string): { ring: string; bg: string; text: string; dot: string } {
  switch (status) {
    case 'OK':
      return { ring: 'ring-emerald-200', bg: 'bg-emerald-50', text: 'text-emerald-700', dot: 'bg-emerald-500' };
    case 'OVERDUE':
      return { ring: 'ring-amber-200', bg: 'bg-amber-50', text: 'text-amber-700', dot: 'bg-amber-500' };
    case 'SETTLED':
      return { ring: 'ring-blue-200', bg: 'bg-blue-50', text: 'text-blue-700', dot: 'bg-blue-500' };
    default:
      return { ring: 'ring-slate-200', bg: 'bg-slate-50', text: 'text-slate-700', dot: 'bg-slate-500' };
  }
}

interface StatCellProps {
  label: string;
  value: string;
  hint?: string;
  tone?: 'default' | 'success' | 'warning' | 'info';
}

function StatCell({ label, value, hint, tone = 'default' }: StatCellProps) {
  const valueClass = {
    default: 'text-gray-900',
    success: 'text-emerald-700',
    warning: 'text-amber-700',
    info: 'text-blue-700',
  }[tone];

  return (
    <div className="rounded-xl border border-gray-100 bg-white p-4 shadow-sm">
      <p className="text-[11px] font-bold uppercase tracking-wider text-gray-500">{label}</p>
      <p className={cn('mt-1 text-lg font-bold tabular-nums', valueClass)}>{value}</p>
      {hint && <p className="mt-0.5 text-[11px] text-gray-400">{hint}</p>}
    </div>
  );
}

export function SubStatement({ statement, className, actions }: SubStatementProps) {
  const tone = healthToneClasses(statement.healthStatus);

  return (
    <div
      data-testid="sub-statement"
      data-health={statement.healthStatus}
      className={cn('overflow-hidden rounded-2xl bg-white shadow-sm ring-1 ring-gray-200/70', className)}
    >
      {/* Header */}
      <div className="flex flex-col gap-3 border-b border-gray-100 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="min-w-0">
          <p className="text-[11px] font-bold uppercase tracking-wider text-gray-500">كشف حساب العقد الباطن</p>
          <h3 className="mt-0.5 truncate text-lg font-bold text-gray-900" title={statement.contractNumber}>
            {statement.contractNumber}{' '}
            <span className="text-sm font-normal text-gray-500">— {statement.subcontractorName}</span>
          </h3>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <span
            data-testid="health-badge"
            className={cn(
              'inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-bold uppercase tracking-wider ring-1',
              tone.bg, tone.text, tone.ring,
            )}
          >
            <span className={cn('h-1.5 w-1.5 rounded-full', tone.dot)} aria-hidden="true" />
            {statement.healthStatusName}
          </span>
          <span
            data-testid="status-pill"
            className="inline-flex items-center rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-bold text-slate-700 ring-1 ring-slate-200"
          >
            {statement.statusName}
          </span>
          {actions}
        </div>
      </div>

      {/* Stat cells */}
      <div className="grid grid-cols-2 gap-3 p-5 lg:grid-cols-4">
        <StatCell
          label="قيمة العقد"
          value={formatCurrency(statement.contractValue)}
          hint="إجمالي قيمة العقد الباطن"
        />
        <StatCell
          label="إجمالي المستخلصات"
          value={formatCurrency(statement.totalBilledGross)}
          hint={`${statement.billingCount} مستخلص`}
          tone="info"
        />
        <StatCell
          label="إجمالي المدفوع"
          value={formatCurrency(statement.totalPaid)}
          hint={statement.lastPaymentDate ? `آخر دفعة: ${formatDate(statement.lastPaymentDate)}` : 'لا توجد مدفوعات'}
          tone="success"
        />
        <StatCell
          label="الرصيد المستحق"
          value={formatCurrency(statement.outstandingBalance)}
          hint={statement.outstandingBalance === 0 ? 'لا يوجد رصيد مستحق' : 'يستحق السداد'}
          tone={statement.outstandingBalance === 0 ? 'success' : 'warning'}
        />
      </div>

      {/* Retention + progress */}
      <div className="grid grid-cols-1 gap-3 border-t border-gray-100 bg-gray-50/40 p-5 lg:grid-cols-3">
        <div>
          <p className="text-[11px] font-bold uppercase tracking-wider text-gray-500">الاحتجاز المحتجز</p>
          <p className="mt-1 text-base font-bold text-gray-900 tabular-nums">{formatCurrency(statement.totalRetentionWithheld)}</p>
        </div>
        <div>
          <p className="text-[11px] font-bold uppercase tracking-wider text-gray-500">الاحتجاز المُحرّر</p>
          <p className="mt-1 text-base font-bold text-gray-900 tabular-nums">{formatCurrency(statement.totalRetentionReleased)}</p>
        </div>
        <div>
          <p className="mb-1 text-[11px] font-bold uppercase tracking-wider text-gray-500">نسبة الإنجاز</p>
          <div className="flex items-center gap-2">
            <div className="h-2 flex-1 overflow-hidden rounded-full bg-gray-200">
              <div
                className="h-full rounded-full bg-gradient-to-l from-blue-400 to-blue-500"
                style={{ width: `${Math.min(100, Math.max(0, statement.workCompletedToDate))}%` }}
              />
            </div>
            <span className="text-sm font-bold tabular-nums text-gray-900">
              {statement.workCompletedToDate.toFixed(1)}%
            </span>
          </div>
        </div>
      </div>

      {/* Dates footer */}
      <div className="flex flex-wrap items-center gap-x-6 gap-y-1 border-t border-gray-100 px-5 py-3 text-[11px] text-gray-500">
        {statement.firstBillingDate && (
          <span>أول مستخلص: <span className="font-bold text-gray-700">{formatDate(statement.firstBillingDate)}</span></span>
        )}
        {statement.lastBillingDate && (
          <span>آخر مستخلص: <span className="font-bold text-gray-700">{formatDate(statement.lastBillingDate)}</span></span>
        )}
        {statement.lastPaymentDate && (
          <span>آخر دفعة: <span className="font-bold text-gray-700">{formatDate(statement.lastPaymentDate)}</span></span>
        )}
      </div>
    </div>
  );
}

export default SubStatement;
