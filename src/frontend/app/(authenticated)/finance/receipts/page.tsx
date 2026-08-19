'use client';

// صفحة سندات القبض (Receipts) — جدول
// Sprint 39 (DEC-125): ConfirmDialog + Toast + design tokens (no native alert/confirm)

import { useEffect, useState, useMemo } from 'react';
import Link from 'next/link';
import { Plus, CheckCircle2, RotateCcw, Receipt as ReceiptIcon } from 'lucide-react';
import { Button, Input, Table, Badge, PageHeader, Card, ConfirmDialog, EmptyState, useToast, SkeletonTable } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { arApi, Receipt, PAYMENT_METHODS, getErrorMessage } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { formatNumber } from '@/lib/format';

export default function ReceiptsPage() {
  const { loading: authLoading } = useAuth();
  const toast = useToast();
  const [receipts, setReceipts] = useState<Receipt[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');

  // ConfirmDialog state — replaces native confirm()
  const [confirm, setConfirm] = useState<{
    open: boolean;
    id: string;
    action: 'post' | 'reverse';
    loading: boolean;
  }>({ open: false, id: '', action: 'post', loading: false });

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await arApi.listReceipts();
      setReceipts(data);
    } catch (e: unknown) {
      const msg = getErrorMessage(e, 'تعذّر تحميل سندات القبض.');
      setError(msg);
      toast.error(msg);
    } finally {
      setLoading(false);
    }
  };

  const filtered = useMemo(() => {
    if (!search) return receipts;
    const q = search.toLowerCase();
    return receipts.filter(
      (r) => r.receiptNumber.toLowerCase().includes(q) || (r.customerName || '').toLowerCase().includes(q)
    );
  }, [receipts, search]);

  // Open ConfirmDialog instead of native confirm()
  const openConfirmPost = (id: string) =>
    setConfirm({ open: true, id, action: 'post', loading: false });
  const openConfirmReverse = (id: string) =>
    setConfirm({ open: true, id, action: 'reverse', loading: false });

  // Actual API call after user confirms
  const doConfirm = async () => {
    setConfirm((c) => ({ ...c, loading: true }));
    const { id, action } = confirm;
    try {
      if (action === 'post') {
        await arApi.postReceipt(id);
        toast.success('تم ترحيل السند بنجاح');
      } else {
        await arApi.reverseReceipt(id);
        toast.success('تم عكس السند بنجاح');
      }
      setConfirm({ open: false, id: '', action: 'post', loading: false });
      await load();
    } catch (e: unknown) {
      const msg = getErrorMessage(
        e,
        action === 'post' ? 'فشل ترحيل السند.' : 'فشل عكس السند.'
      );
      toast.error(msg);
      setConfirm((c) => ({ ...c, loading: false }));
    }
  };

  return (
    <div>
      <PageHeader
        title="💰 سندات القبض"
        description="سندات القبض على العملاء"
        actions={
          <Link href="/finance/receipts/new">
            <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>سند قبض جديد</Button>
          </Link>
        }
      />

      <Card className="mb-4">
        <Input
          placeholder="🔍 بحث (رقم/عميل)..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </Card>

      {loading ? (
        <SkeletonTable rows={6} cols={6} />
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={<ReceiptIcon className="h-12 w-12" />}
          title="لا توجد سندات قبض"
          description="ابدأ بإنشاء سند قبض جديد لتسجيل مدفوعات العملاء."
          action={
            <Link href="/finance/receipts/new">
              <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
                إنشاء سند قبض
              </Button>
            </Link>
          }
        />
      ) : (
        <Table
          columns={[
            {
              key: 'receiptNumber',
              header: 'رقم السند',
              render: (r) => <span className="font-mono font-semibold text-brand-600">{r.receiptNumber}</span>,
            },
            {
              key: 'customer',
              header: 'العميل',
              render: (r) => <span className="font-semibold text-ink-800">{r.customerName || '—'}</span>,
            },
            {
              key: 'receiptDate',
              header: 'التاريخ',
              render: (r) => <span className="text-sm text-ink-600">{formatDate(r.receiptDate)}</span>,
            },
            {
              key: 'amount',
              header: 'المبلغ',
              align: 'end',
              render: (r) => <span className="font-mono font-bold">{formatNumber(r.amount)} {r.currencyCode}</span>,
            },
            {
              key: 'paymentMethod',
              header: 'الطريقة',
              render: (r) => r.paymentMethod ? <Badge variant="info">{PAYMENT_METHODS[r.paymentMethod] || r.paymentMethod}</Badge> : <span className="text-xs text-ink-400">—</span>,
            },
            {
              key: 'allocations',
              header: 'التخصيصات',
              align: 'center',
              render: (r) => <span className="text-sm text-ink-600">{r.allocations.length} فاتورة</span>,
            },
            {
              key: 'status',
              header: 'الحالة',
              align: 'center',
              render: (r) => r.postedAt ? <Badge variant="success">مُرحّل</Badge> : <Badge variant="warning">مسودة</Badge>,
            },
            {
              key: 'actions',
              header: 'إجراءات',
              align: 'center',
              render: (r) => (
                <div className="flex items-center gap-1 justify-center">
                  {!r.postedAt && (
                    <button
                      onClick={() => openConfirmPost(r.id)}
                      className="text-success-600 hover:text-success-700 p-1.5 rounded-md hover:bg-success-50 transition-colors"
                      title="ترحيل"
                    >
                      <CheckCircle2 className="h-4 w-4" />
                    </button>
                  )}
                  {r.postedAt && (
                    <button
                      onClick={() => openConfirmReverse(r.id)}
                      className="text-warning-600 hover:text-warning-700 p-1.5 rounded-md hover:bg-warning-50 transition-colors"
                      title="عكس"
                    >
                      <RotateCcw className="h-4 w-4" />
                    </button>
                  )}
                </div>
              ),
            },
          ]}
          data={filtered}
          rowKey={(r) => r.id}
        />
      )}

      {!loading && filtered.length > 0 && (
        <p className="mt-3 text-xs text-ink-500 text-start">
          {filtered.length} سند • إجمالي: <span className="font-mono font-semibold">{formatNumber(filtered.reduce((s, r) => s + r.amount, 0))}</span>
        </p>
      )}

      {/* Sprint 39 (DEC-125): ConfirmDialog replaces native confirm() */}
      <ConfirmDialog
        open={confirm.open}
        title={confirm.action === 'post' ? 'تأكيد ترحيل السند' : 'تأكيد عكس السند'}
        message={
          confirm.action === 'post'
            ? 'سيتم ترحيل سند القبض وإنشاء قيد محاسبي (Dr 1210 / Cr 1230). لا يمكن التراجع.'
            : 'سيتم عكس السند وإنشاء قيد عكسي. تأكد من صحة العملية قبل المتابعة.'
        }
        confirmLabel={confirm.action === 'post' ? 'ترحيل' : 'عكس'}
        cancelLabel="إلغاء"
        variant={confirm.action === 'post' ? 'primary' : 'warning'}
        loading={confirm.loading}
        onConfirm={doConfirm}
        onCancel={() => setConfirm({ open: false, id: '', action: 'post', loading: false })}
      />
    </div>
  );
}
