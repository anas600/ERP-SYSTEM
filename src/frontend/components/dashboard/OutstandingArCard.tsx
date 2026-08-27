'use client';

// 📊 OutstandingArCard — Sprint 65 / Wave 2A (DEC-234 + DEC-236)
//
// One of the 3 cross-module KPI cards on the new /dashboard/cross-module page.
// Shows the company's outstanding accounts receivable (AR) — i.e. the unpaid
// portion of posted sales_invoices. Renders a green tone when AR is 0 (clean)
// and a red tone when AR is positive (attention needed).
//
// The card intentionally does NOT fetch its own data — the parent page passes
// the value in as a prop so we keep the loading/error states in one place.

import { ArrowDownRight, HandCoins } from 'lucide-react';
import { Card, StatCard } from '@/components/ui';
import { formatMoney } from '@/lib/format';

export interface OutstandingArCardProps {
  /** Outstanding AR (LYD). 0 = nothing to collect. */
  value: number;
  /** Optional currency code, defaults to LYD. */
  currency?: string;
  /** Optional click handler — e.g. navigate to the AR aging report. */
  onClick?: () => void;
  /** Loading state. */
  loading?: boolean;
}

export function OutstandingArCard({
  value,
  currency = 'LYD',
  onClick,
  loading = false,
}: OutstandingArCardProps) {
  const isClean = value <= 0;
  const tone = isClean ? 'green' : 'red';
  const subtitle = isClean
    ? 'لا يوجد ذمم مدينة مستحقة'
    : 'مستحقات لم تُحصّل بعد';
  return (
    <StatCard
      label="الذمم المدينة المستحقة (AR)"
      value={formatMoney(value, currency, 0)}
      hint={subtitle}
      icon={HandCoins}
      tone={tone}
      loading={loading}
      delta={null}
      footer={
        isClean ? null : (
          <span className="inline-flex items-center gap-1 text-xs text-rose-600 font-semibold">
            <ArrowDownRight className="h-3 w-3" />
            يتطلب متابعة التحصيل
          </span>
        )
      }
    />
  );
}

export default OutstandingArCard;
