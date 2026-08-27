'use client';

// 🧾 ReceiptMatchCard — Sprint 65 / Wave 3A (DEC-235 + DEC-237)
//
// One suggested match between an AR Receipt and an AP Sub-Payment. The card shows
// the subcontractor, the amount, the date, and a coloured score badge. The
// accountant clicks "تأكيد" to confirm the match and the parent page calls the
// `/api/receipts/{id}/confirm-match/{subPaymentId}` endpoint.
//
// The card is prop-driven: the parent page passes the match in, and the parent
// owns the API call + loading state. We only render.

import { CheckCircle2, Hammer, Calendar, Coins, Trophy } from 'lucide-react';
import { Card, Badge, Button } from '@/components/ui';
import { formatMoney, formatNumber } from '@/lib/format';
import type { SubPaymentMatch, MatchQuality } from '@/lib/api-types';

export interface ReceiptMatchCardProps {
  /** The match row from the BE. */
  match: SubPaymentMatch;
  /** Click handler — typically triggers a confirm-match API call. */
  onConfirm?: (match: SubPaymentMatch) => void;
  /** Whether the confirm button is disabled (e.g. another request in flight). */
  confirming?: boolean;
}

const QUALITY_TONE: Record<MatchQuality, 'success' | 'info' | 'warning' | 'danger'> = {
  EXCELLENT: 'success',
  GOOD: 'info',
  FAIR: 'warning',
  POOR: 'danger',
};

export function ReceiptMatchCard({
  match,
  onConfirm,
  confirming = false,
}: ReceiptMatchCardProps) {
  return (
    <Card
      accent={
        match.matchQuality === 'EXCELLENT' || match.matchQuality === 'GOOD'
          ? 'green'
          : match.matchQuality === 'FAIR'
            ? 'yellow'
            : 'red'
      }
      className="overflow-hidden"
      data-testid="match-card"
      data-quality={match.matchQuality}
    >
      <div className="flex items-start justify-between gap-3 mb-3">
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2 mb-1">
            <Hammer className="h-4 w-4 text-ink-400 flex-shrink-0" />
            <p
              className="font-bold text-ink-800 truncate"
              data-testid="match-subcontractor"
            >
              {match.subcontractorName || 'مقاول غير مسمى'}
            </p>
          </div>
          <p className="text-xs text-ink-500">
            رقم الدفعة:{' '}
            <span className="font-mono font-semibold text-ink-700">
              {match.paymentNumber || '—'}
            </span>
          </p>
        </div>
        <Badge variant={QUALITY_TONE[match.matchQuality]} size="md" data-testid="match-quality">
          <Trophy className="h-3 w-3 me-1" />
          {match.matchQualityName} · {formatNumber(match.score, 0)}
        </Badge>
      </div>

      <div className="grid grid-cols-2 gap-3 mb-3">
        <div className="flex items-center gap-2">
          <Coins className="h-4 w-4 text-emerald-600 flex-shrink-0" />
          <div>
            <p className="text-[10px] text-ink-500 uppercase tracking-wider">المبلغ</p>
            <p className="text-sm font-bold text-ink-800 tabular-nums">
              {formatMoney(match.amount, 'LYD', 2)}
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Calendar className="h-4 w-4 text-blue-600 flex-shrink-0" />
          <div>
            <p className="text-[10px] text-ink-500 uppercase tracking-wider">
              تاريخ الدفعة
            </p>
            <p className="text-sm font-bold text-ink-800 tabular-nums">
              {formatDate(match.paymentDate)}
            </p>
          </div>
        </div>
      </div>

      {onConfirm && (
        <Button
          variant="primary"
          size="sm"
          onClick={() => onConfirm(match)}
          disabled={confirming}
          iconLeft={<CheckCircle2 className="h-4 w-4" />}
          className="w-full"
          data-testid="match-confirm-button"
        >
          {confirming ? 'جارٍ التأكيد...' : 'تأكيد المطابقة'}
        </Button>
      )}
    </Card>
  );
}

function formatDate(iso: string): string {
  try {
    const d = new Date(iso);
    return d.toLocaleDateString('en-GB', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
    });
  } catch {
    return iso;
  }
}

export default ReceiptMatchCard;
