'use client';

// 📊 OutstandingApCard — Sprint 65 / Wave 2A (DEC-234 + DEC-236)
//
// Sister of OutstandingArCard but for accounts payable (AP) — i.e. the unmatched
// sub_payments that have not yet been matched to vendor bills (DEC-232 / Sprint 64).
// Renders a green tone when AP is 0 (nothing to pay) and an amber tone when
// positive (review the queue).
//
// The card is prop-driven: the parent page owns the data fetch + loading state.

import { ArrowUpRight, Receipt } from 'lucide-react';
import { StatCard } from '@/components/ui';
import { formatMoney } from '@/lib/format';

export interface OutstandingApCardProps {
  /** Outstanding AP (LYD). 0 = nothing to pay. */
  value: number;
  /** Optional currency code, defaults to LYD. */
  currency?: string;
  /** Optional click handler. */
  onClick?: () => void;
  /** Loading state. */
  loading?: boolean;
}

export function OutstandingApCard({
  value,
  currency = 'LYD',
  onClick,
  loading = false,
}: OutstandingApCardProps) {
  const isClean = value <= 0;
  const tone = isClean ? 'green' : 'amber';
  const subtitle = isClean
    ? 'لا يوجد ذمم دائنة معلّقة'
    : 'دفعات مقاولين بانتظار المطابقة';
  return (
    <StatCard
      label="الذمم الدائنة المستحقة (AP)"
      value={formatMoney(value, currency, 0)}
      hint={subtitle}
      icon={Receipt}
      tone={tone}
      loading={loading}
      delta={null}
      footer={
        isClean ? null : (
          <span className="inline-flex items-center gap-1 text-xs text-amber-700 font-semibold">
            <ArrowUpRight className="h-3 w-3" />
            يتطلب مراجعة فريق المالية
          </span>
        )
      }
    />
  );
}

export default OutstandingApCard;
