'use client';

// 📋 ReconciliationQueue — Sprint 65 / Wave 3A (DEC-235 + DEC-237)
//
// Sister of the cross-module dashboard widgets (OutstandingArCard / OutstandingApCard)
// but for the bank reconciliation queue: a list of posted receipts that have not
// yet been matched to a sub-payment. The accountant clicks "البحث عن مطابقات" on
// a row, and the parent page fetches the suggested matches.
//
// The list is prop-driven: the parent page owns the data fetch + loading state
// and just passes the rows down. The component handles its own rendering and
// click handlers.

import { Search, HandCoins, Calendar, Hash } from 'lucide-react';
import { Card, Button, EmptyState } from '@/components/ui';
import { formatMoney, formatNumber } from '@/lib/format';
import type { UnmatchedReceipt } from '@/lib/api-types';

export interface ReconciliationQueueProps {
  /** The queue of unmatched receipts. */
  receipts: UnmatchedReceipt[];
  /** Optional click handler — called with the receiptId when the user clicks "Find matches". */
  onFindMatches?: (receiptId: string) => void;
  /** The id of the receipt currently being processed (disables its button). */
  findingFor?: string | null;
  /** Loading state. */
  loading?: boolean;
}

export function ReconciliationQueue({
  receipts,
  onFindMatches,
  findingFor = null,
  loading = false,
}: ReconciliationQueueProps) {
  if (loading) {
    return (
      <Card title="طابور التسوية" description="جارٍ التحميل..." accent="blue">
        <div className="h-32 flex items-center justify-center text-ink-400 text-sm">
          ...
        </div>
      </Card>
    );
  }

  if (!receipts || receipts.length === 0) {
    return (
      <Card
        title="طابور التسوية"
        description="سندات القبض التي تنتظر المطابقة مع دفعات المقاولين"
        accent="green"
      >
        <EmptyState
          title="لا توجد سندات قبض في الانتظار"
          description="كل سندات القبض المرحلة تمّت مطابقتها مع دفعات المقاولين."
        />
      </Card>
    );
  }

  return (
    <Card
      title="طابور التسوية"
      description={`${receipts.length} سند قبض بانتظار المطابقة`}
      accent="blue"
      data-testid="reconciliation-queue"
    >
      <div className="divide-y divide-ink-100">
        {receipts.map((r) => {
          const isFinding = findingFor === r.receiptId;
          return (
            <div
              key={r.receiptId}
              className="py-3 first:pt-0 last:pb-0 flex items-center gap-3"
              data-testid="queue-row"
              data-receipt-id={r.receiptId}
            >
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2 mb-1">
                  <Hash className="h-3.5 w-3.5 text-ink-400 flex-shrink-0" />
                  <p
                    className="font-mono font-bold text-ink-800 text-sm truncate"
                    data-testid="queue-receipt-number"
                  >
                    {r.receiptNumber}
                  </p>
                </div>
                <div className="flex items-center gap-3 text-xs text-ink-500">
                  <span className="flex items-center gap-1">
                    <HandCoins className="h-3 w-3" />
                    <span className="font-bold text-ink-700 tabular-nums">
                      {formatMoney(r.amount, 'LYD', 2)}
                    </span>
                  </span>
                  <span className="flex items-center gap-1">
                    <Calendar className="h-3 w-3" />
                    <span className="tabular-nums">
                      {formatDate(r.receiptDate)}
                    </span>
                  </span>
                  <span className="text-ink-400">
                    منذ {formatNumber(r.daysSinceReceipt, 0)} يوم
                  </span>
                </div>
              </div>
              {onFindMatches && (
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => onFindMatches(r.receiptId)}
                  disabled={isFinding}
                  iconLeft={<Search className="h-4 w-4" />}
                  data-testid="queue-find-button"
                >
                  {isFinding ? 'جارٍ البحث...' : 'البحث عن مطابقات'}
                </Button>
              )}
            </div>
          );
        })}
      </div>
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

export default ReconciliationQueue;
