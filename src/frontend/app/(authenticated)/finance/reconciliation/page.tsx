'use client';

// 🏦 /finance/reconciliation — Sprint 65 / Wave 3A (DEC-235 + DEC-237)
//
// Bank reconciliation page: lists posted receipts that have not been matched to
// a sub-payment, lets the accountant find suggested matches, and confirms the
// match with a single click. Built for construction companies where the bank
// credit from a subcontractor's work is the natural signal that a sub-payment
// obligation is satisfied.
//
// The data flow:
//   - On mount: load the queue of unmatched receipts.
//   - For each row, the accountant clicks "البحث عن مطابقات" to fetch the
//     suggested matches (top 5 by score) for that single receipt.
//   - The matches are rendered as a stack of ReceiptMatchCards.
//   - Each card has a "تأكيد المطابقة" button that calls the confirm-match
//     endpoint and removes the receipt from the queue on success.
//   - Errors render an error card; empty state renders the EmptyState
//     component; loading renders skeletons.

import { useEffect, useState, useCallback } from 'react';
import { AlertCircle, RefreshCw, ArrowRightLeft, X } from 'lucide-react';
import { PageHeader, Card, SkeletonPage, EmptyState, Button, useToast } from '@/components/ui';
import { ReconciliationQueue } from '@/components/finance/ReconciliationQueue';
import { ReceiptMatchCard } from '@/components/finance/ReceiptMatchCard';
import {
  fetchReconciliationQueue,
  suggestMatches,
  confirmMatch,
} from '@/lib/api/reconciliation';
import { getErrorMessage } from '@/lib/api';
import type { UnmatchedReceipt, SubPaymentMatch } from '@/lib/api-types';

export default function BankReconciliationPage() {
  const [queue, setQueue] = useState<UnmatchedReceipt[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedReceiptId, setSelectedReceiptId] = useState<string | null>(null);
  const [matches, setMatches] = useState<SubPaymentMatch[]>([]);
  const [finding, setFinding] = useState(false);
  const [confirming, setConfirming] = useState<string | null>(null); // subPaymentId being confirmed
  const { show } = useToast();

  const loadQueue = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const rows = await fetchReconciliationQueue(0, 50);
      setQueue(rows);
    } catch (e) {
      setError(getErrorMessage(e));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadQueue();
  }, [loadQueue]);

  const handleFind = async (receiptId: string) => {
    setSelectedReceiptId(receiptId);
    setFinding(true);
    setMatches([]);
    setError(null);
    try {
      const m = await suggestMatches(receiptId, 5);
      setMatches(m);
    } catch (e) {
      setError(getErrorMessage(e));
      setMatches([]);
    } finally {
      setFinding(false);
    }
  };

  const handleClearMatches = () => {
    setSelectedReceiptId(null);
    setMatches([]);
  };

  const handleConfirm = async (match: SubPaymentMatch) => {
    if (!selectedReceiptId) return;
    setConfirming(match.subPaymentId);
    try {
      const confirmed = await confirmMatch(selectedReceiptId, match.subPaymentId);
      show(
        `تم تأكيد مطابقة ${match.subcontractorName} بنجاح (نقاط: ${confirmed.score})`,
        'success',
      );
      // Remove the confirmed receipt from the queue and clear the matches panel.
      setQueue((q) => q.filter((r) => r.receiptId !== selectedReceiptId));
      setSelectedReceiptId(null);
      setMatches([]);
    } catch (e) {
      show(`فشل تأكيد المطابقة: ${getErrorMessage(e)}`, 'error');
    } finally {
      setConfirming(null);
    }
  };

  if (loading && queue.length === 0) {
    return (
      <>
        <PageHeader
          title="تسوية البنك"
          description="مطابقة سندات القبض مع دفعات المقاولين"
        />
        <SkeletonPage />
      </>
    );
  }

  if (error && queue.length === 0) {
    return (
      <>
        <PageHeader
          title="تسوية البنك"
          description="مطابقة سندات القبض مع دفعات المقاولين"
        />
        <Card accent="red">
          <div className="flex items-start gap-3">
            <AlertCircle className="h-5 w-5 text-rose-600 mt-0.5" />
            <div className="min-w-0 flex-1">
              <p className="font-semibold text-rose-700">فشل تحميل البيانات</p>
              <p className="text-sm text-ink-600 mt-1 break-words">{error}</p>
              <button
                type="button"
                onClick={loadQueue}
                className="mt-3 inline-flex items-center gap-1.5 text-sm font-semibold text-brand-600 hover:text-brand-700"
              >
                <RefreshCw className="h-4 w-4" />
                إعادة المحاولة
              </button>
            </div>
          </div>
        </Card>
      </>
    );
  }

  return (
    <>
      <PageHeader
        title="تسوية البنك"
        description="مطابقة سندات القبض مع دفعات المقاولين"
        actions={
          <Button
            variant="ghost"
            size="sm"
            onClick={loadQueue}
            iconLeft={<RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />}
          >
            تحديث
          </Button>
        }
      />

      {error && (
        <Card accent="red" className="mb-4">
          <div className="flex items-start gap-3">
            <AlertCircle className="h-5 w-5 text-rose-600 mt-0.5" />
            <p className="text-sm text-ink-700 break-words">{error}</p>
          </div>
        </Card>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <ReconciliationQueue
          receipts={queue}
          onFindMatches={handleFind}
          findingFor={selectedReceiptId && finding ? selectedReceiptId : null}
          loading={loading}
        />

        <Card
          title={
            <div className="flex items-center justify-between gap-2">
              <span className="flex items-center gap-2">
                <ArrowRightLeft className="h-4 w-4 text-brand-500" />
                المطابقات المقترحة
              </span>
              {selectedReceiptId && (
                <Button
                  variant="ghost"
                  size="xs"
                  onClick={handleClearMatches}
                  iconLeft={<X className="h-3.5 w-3.5" />}
                >
                  إغلاق
                </Button>
              )}
            </div>
          }
          description={
            selectedReceiptId
              ? 'اختر أفضل مطابقة ثم اضغط تأكيد'
              : 'اختر سنداً من الطابور لعرض المطابقات'
          }
          accent="purple"
        >
          {!selectedReceiptId && !finding && (
            <EmptyState
              title="لم يتم اختيار سند بعد"
              description="اضغط 'البحث عن مطابقات' على أي سند من الطابور."
            />
          )}
          {finding && (
            <div className="h-32 flex items-center justify-center text-ink-400 text-sm">
              جارٍ البحث عن مطابقات...
            </div>
          )}
          {!finding && matches.length === 0 && selectedReceiptId && (
            <EmptyState
              title="لا توجد مطابقات"
              description="لا توجد دفعات فرعية متطابقة في حدود ±5% و±30 يوم."
            />
          )}
          {!finding && matches.length > 0 && (
            <div className="space-y-3" data-testid="matches-list">
              {matches.map((m) => (
                <ReceiptMatchCard
                  key={m.subPaymentId}
                  match={m}
                  onConfirm={handleConfirm}
                  confirming={confirming === m.subPaymentId}
                />
              ))}
            </div>
          )}
        </Card>
      </div>
    </>
  );
}
