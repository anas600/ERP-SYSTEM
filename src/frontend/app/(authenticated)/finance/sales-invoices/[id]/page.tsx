'use client';

// صفحة تفاصيل فاتورة مبيعات — customer info + lines + payments
// Sprint 5 (Phase 5.3): adds a "Print / Download PDF" button that uses
// window.print() with a dedicated @media print block. The print stylesheet
// hides the sidebar + topbar + action buttons, so the printed page is a
// clean invoice document with header, line items, totals, and a footer
// signature block.

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight, ArrowLeft, Send, XCircle, FileText, Printer } from 'lucide-react';
import { Button, Badge, Card, PageHeader } from '@/components/ui';
import { arApi, SalesInvoice, SALES_INVOICE_STATUSES, SALES_INVOICE_STATUS_VARIANTS, getErrorMessage } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { formatNumber } from '@/lib/format';
import { useToast } from '@/lib/useToast';

export default function SalesInvoiceDetailPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const toast = useToast();
  const id = params.id;
  const [invoice, setInvoice] = useState<SalesInvoice | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionLoading, setActionLoading] = useState(false);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await arApi.getInvoice(id);
      setInvoice(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'تعذّر تحميل الفاتورة.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [id]);

  const onPost = async () => {
    if (!invoice) return;
    if (!confirm('سيتم ترحيل الفاتورة وإنشاء قيد محاسبي. هل أنت متأكد؟')) return;
    setActionLoading(true);
    try {
      const updated = await arApi.postInvoice(invoice.id);
      setInvoice(updated);
      toast.success('تم ترحيل الفاتورة بنجاح.');
    } catch (e: unknown) {
      const msg = getErrorMessage(e, 'فشل ترحيل الفاتورة.');
      setError(msg);
      toast.error(msg);
    } finally {
      setActionLoading(false);
    }
  };

  const onCancel = async () => {
    if (!invoice) return;
    if (!confirm('هل تريد إلغاء هذه الفاتورة؟')) return;
    setActionLoading(true);
    try {
      const updated = await arApi.cancelInvoice(invoice.id);
      setInvoice(updated);
      toast.success('تم إلغاء الفاتورة.');
    } catch (e: unknown) {
      const msg = getErrorMessage(e, 'فشل إلغاء الفاتورة.');
      setError(msg);
      toast.error(msg);
    } finally {
      setActionLoading(false);
    }
  };

  // Sprint 5: print handler — uses the browser's native print dialog. The
  // @media print block in globals.css hides everything except the .printable
  // area (sidebar, topbar, action buttons, back link) so the printed page is
  // a clean invoice document. The user picks "Save as PDF" in the print
  // dialog to actually generate a PDF.
  const onPrint = () => {
    if (typeof window === 'undefined') return;
    // Give the browser a tick to settle any pending UI changes before
    // opening the print dialog (otherwise the page may flash with old state).
    setTimeout(() => window.print(), 50);
  };

  if (loading) {
    return (
      <div>
        <PageHeader title="فاتورة مبيعات" description="جاري التحميل..." />
      </div>
    );
  }
  if (error) {
    return (
      <div>
        <PageHeader title="فاتورة مبيعات" description="خطأ" />
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">{error}</div>
      </div>
    );
  }
  if (!invoice) return null;

  const statusLabel = SALES_INVOICE_STATUSES[invoice.status] || '—';
  const statusVariant = SALES_INVOICE_STATUS_VARIANTS[invoice.status] || 'neutral';
  const isDraft = invoice.status === 1;
  const isCancellable = isDraft || (invoice.paidAmount === 0 && invoice.status !== 6);

  return (
    <div>
      <div className="mb-3 no-print">
        <button
          onClick={() => router.back()}
          className="inline-flex items-center gap-1 text-sm text-gray-500 hover:text-blue-600 transition-colors"
        >
          <ArrowLeft className="h-4 w-4" />
          العودة للقائمة
        </button>
      </div>

      <PageHeader
        title={`📄 فاتورة ${invoice.invoiceNumber}`}
        description={invoice.customerName ? `العميل: ${invoice.customerName}` : 'تفاصيل الفاتورة'}
        breadcrumb={[
          { label: 'الرئيسية', href: '/dashboard' },
          { label: 'المالية', href: '/finance/sales-invoices' },
          { label: 'فواتير المبيعات', href: '/finance/sales-invoices' },
          { label: invoice.invoiceNumber },
        ]}
        actions={
          <div className="flex items-center gap-2 no-print">
            <Link href="/finance/sales-invoices">
              <Button variant="ghost" iconLeft={<ArrowRight className="h-4 w-4" />}>رجوع</Button>
            </Link>
            {/* Sprint 5 (Phase 5.3): Print / Download PDF. Hidden in print
                output via the .no-print class — only the invoice body prints. */}
            <Button
              variant="secondary"
              onClick={onPrint}
              iconLeft={<Printer className="h-4 w-4" />}
              title="طباعة أو حفظ كـ PDF (Ctrl+P)"
            >
              <span className="hidden sm:inline">طباعة / PDF</span>
              <span className="sm:hidden">طباعة</span>
            </Button>
            {isDraft && (
              <Button variant="primary" loading={actionLoading} onClick={onPost} iconLeft={<Send className="h-4 w-4" />}>
                ترحيل الفاتورة
              </Button>
            )}
            {isCancellable && (
              <Button variant="danger" loading={actionLoading} onClick={onCancel} iconLeft={<XCircle className="h-4 w-4" />}>
                إلغاء
              </Button>
            )}
          </div>
        }
      />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <Card className="lg:col-span-2">
          <div className="flex items-center justify-between mb-3">
            <h3 className="font-bold text-gray-800">معلومات الفاتورة</h3>
            <Badge variant={statusVariant}>{statusLabel}</Badge>
          </div>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-sm">
            <div>
              <p className="text-gray-500">التاريخ</p>
              <p className="font-semibold">{formatDate(invoice.invoiceDate)}</p>
            </div>
            <div>
              <p className="text-gray-500">الاستحقاق</p>
              <p className="font-semibold">{formatDate(invoice.dueDate)}</p>
            </div>
            <div>
              <p className="text-gray-500">العملة</p>
              <p className="font-mono font-semibold">{invoice.currencyCode}</p>
            </div>
            <div>
              <p className="text-gray-500">سعر الصرف</p>
              <p className="font-mono font-semibold">{formatNumber(invoice.exchangeRate, 8)}</p>
            </div>
            <div>
              <p className="text-gray-500">تاريخ الترحيل</p>
              <p className="font-semibold">{formatDate(invoice.postedAt)}</p>
            </div>
            <div>
              <p className="text-gray-500">القيد</p>
              <p className="font-mono text-xs">{invoice.journalEntryId ? invoice.journalEntryId.slice(0, 8) + '...' : '—'}</p>
            </div>
          </div>

          <h3 className="font-bold text-gray-800 pt-4 mt-4 border-t">البنود</h3>
          <div className="overflow-x-auto mt-2">
            <table className="w-full text-sm">
              <thead>
                <tr className="text-right text-xs text-gray-500 border-b">
                  <th className="py-2 pr-2">#</th>
                  <th className="py-2 pr-2">الوصف</th>
                  <th className="py-2 pr-2 text-left">الكمية</th>
                  <th className="py-2 pr-2 text-left">السعر</th>
                  <th className="py-2 pr-2 text-left">الضريبة</th>
                  <th className="py-2 pr-2 text-left">المجموع</th>
                </tr>
              </thead>
              <tbody>
                {invoice.lines.map((l) => (
                  <tr key={l.id} className="border-b">
                    <td className="py-2 pr-2 text-gray-500">{l.lineNumber}</td>
                    <td className="py-2 pr-2">{l.description}</td>
                    <td className="py-2 pr-2 text-left font-mono">{formatNumber(l.quantity)}</td>
                    <td className="py-2 pr-2 text-left font-mono">{formatNumber(l.unitPrice)}</td>
                    <td className="py-2 pr-2 text-left font-mono">{(l.taxRate * 100).toFixed(2)}%</td>
                    <td className="py-2 pr-2 text-left font-mono font-semibold">{formatNumber(l.lineTotal)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {invoice.notes && (
            <div className="mt-4 p-3 bg-gray-50 rounded-lg text-sm">
              <p className="text-gray-500 mb-1">ملاحظات:</p>
              <p className="text-gray-800">{invoice.notes}</p>
            </div>
          )}
        </Card>

        <div className="space-y-4">
          <Card>
            <h3 className="font-bold text-gray-800 mb-3 flex items-center gap-2">
              <FileText className="h-4 w-4" /> الملخص المالي
            </h3>
            <div className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-gray-600">الإجمالي قبل الضريبة:</span>
                <span className="font-mono font-semibold">{formatNumber(invoice.subtotal)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-600">الضريبة:</span>
                <span className="font-mono font-semibold">{formatNumber(invoice.taxAmount)}</span>
              </div>
              <div className="flex justify-between border-t pt-2">
                <span className="font-bold">الإجمالي:</span>
                <span className="font-mono font-bold">{formatNumber(invoice.totalAmount)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-green-700">المدفوع:</span>
                <span className="font-mono font-semibold text-green-700">{formatNumber(invoice.paidAmount)}</span>
              </div>
              <div className="flex justify-between border-t pt-2 bg-red-50 -mx-4 px-4 py-2 mt-2">
                <span className="font-bold text-red-700">المتبقي:</span>
                <span className="font-mono font-bold text-red-700 text-lg">{formatNumber(invoice.outstanding)}</span>
              </div>
            </div>
          </Card>

          <Card>
            <h3 className="font-bold text-gray-800 mb-3">المدفوعات ({invoice.allocations.length})</h3>
            {invoice.allocations.length === 0 ? (
              <p className="text-sm text-gray-500">لا توجد مدفوعات. أنشئ سند قبض من <Link href="/finance/receipts/new" className="text-blue-600 hover:underline">هنا</Link>.</p>
            ) : (
              <div className="space-y-2">
                {invoice.allocations.map((a) => (
                  <div key={a.id} className="flex justify-between text-sm border-b pb-2">
                    <span className="font-mono text-xs">{a.salesInvoiceId.slice(0, 8)}...</span>
                    <span className="font-mono font-semibold">{formatNumber(a.amountApplied)}</span>
                  </div>
                ))}
              </div>
            )}
          </Card>
        </div>
      </div>
    </div>
  );
}
