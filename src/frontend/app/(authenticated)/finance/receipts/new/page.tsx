'use client';

// صفحة إنشاء سند قبض — pick customer + pick open invoices + allocate amounts

import { useEffect, useState, useMemo } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight, Save, Send, Plus, Trash2 } from 'lucide-react';
import { Button, Input, Select, Card, PageHeader } from '@/components/ui';
import { arApi, Customer, SalesInvoice, PAYMENT_METHODS, getErrorMessage } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { formatNumber } from '@/lib/format';

interface AllocDraft {
  id: string;
  salesInvoiceId: string;
  amountApplied: string;
}

const emptyAlloc = (salesInvoiceId = ''): AllocDraft => ({
  id: crypto.randomUUID(),
  salesInvoiceId,
  amountApplied: '0',
});

export default function NewReceiptPage() {
  const router = useRouter();
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [customerId, setCustomerId] = useState<string>('');
  const [openInvoices, setOpenInvoices] = useState<SalesInvoice[]>([]);
  const [receiptDate, setReceiptDate] = useState<string>(new Date().toISOString().slice(0, 10));
  const [amount, setAmount] = useState<string>('0');
  const [currencyCode, setCurrencyCode] = useState('LYD');
  const [paymentMethod, setPaymentMethod] = useState('');
  const [notes, setNotes] = useState('');
  const [allocations, setAllocations] = useState<AllocDraft[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    arApi.listCustomers()
      .then(setCustomers)
      .catch((e) => setError(getErrorMessage(e, 'تعذّر تحميل العملاء.')));
  }, []);

  // عند تغيير العميل: جلب فواتيره المفتوحة
  useEffect(() => {
    if (!customerId) { setOpenInvoices([]); setAllocations([]); return; }
    // للـ MVP: نُجلب كل الفواتير ونفلتر يدوياً (الـ list endpoint لا يدعم customerId filter حالياً)
    arApi.listInvoices()
      .then((all) => {
        const opens = all.filter((i) => i.customerId === customerId && i.outstanding > 0 && i.status !== 6);
        setOpenInvoices(opens);
        if (opens.length > 0) {
          setAllocations([emptyAlloc(opens[0].id)]);
        } else {
          setAllocations([]);
        }
      })
      .catch(() => setOpenInvoices([]));
  }, [customerId]);

  const customerOptions = useMemo(
    () => [{ value: '', label: 'اختر العميل' }, ...customers.filter((c) => c.isActive).map((c) => ({ value: c.id, label: `${c.code} — ${c.name}` }))],
    [customers]
  );

  const invoiceOptions = useMemo(() => {
    return openInvoices.map((inv) => ({
      value: inv.id,
      label: `${inv.invoiceNumber} — ${formatDate(inv.invoiceDate)} — متبقي: ${formatNumber(inv.outstanding)}`,
    }));
  }, [openInvoices]);

  const paymentMethodOptions = useMemo(() => [
    { value: '', label: 'اختر طريقة الدفع' },
    ...Object.entries(PAYMENT_METHODS).map(([k, v]) => ({ value: k, label: v })),
  ], []);

  const totalAllocated = useMemo(
    () => allocations.reduce((s, a) => s + (Number(a.amountApplied) || 0), 0),
    [allocations]
  );

  const addAlloc = () => setAllocations((a) => [...a, emptyAlloc(openInvoices[0]?.id || '')]);
  const removeAlloc = (id: string) => setAllocations((a) => a.length > 1 ? a.filter((x) => x.id !== id) : a);
  const updateAlloc = (id: string, patch: Partial<AllocDraft>) =>
    setAllocations((a) => a.map((x) => (x.id === id ? { ...x, ...patch } : x)));

  const submit = async (postImmediately: boolean) => {
    setError(null);
    if (!customerId) { setError('اختر العميل.'); return; }
    if (Number(amount) <= 0) { setError('مبلغ السند يجب أن يكون أكبر من صفر.'); return; }
    if (allocations.length === 0) { setError('أضف تخصيصاً واحداً على الأقل.'); return; }
    if (Math.abs(totalAllocated - Number(amount)) > 0.0001) {
      setError(`مجموع التخصيصات (${formatNumber(totalAllocated)}) يجب أن يساوي المبلغ (${formatNumber(Number(amount))}).`);
      return;
    }
    setSubmitting(true);
    try {
      const payload = {
        customerId,
        receiptDate: new Date(receiptDate).toISOString(),
        amount: Number(amount),
        currencyCode,
        paymentMethod: paymentMethod || undefined,
        notes: notes || undefined,
        allocations: allocations
          .filter((a) => a.salesInvoiceId && Number(a.amountApplied) > 0)
          .map((a) => ({ salesInvoiceId: a.salesInvoiceId, amountApplied: Number(a.amountApplied) })),
        postImmediately,
      };
      const r = await arApi.createReceipt(payload);
      router.push(`/finance/receipts`);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل إنشاء سند القبض.'));
      setSubmitting(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="➕ سند قبض جديد"
        description="إنشاء سند قبض مع تخصيص على فواتير العميل"
        breadcrumb={[
          { label: 'الرئيسية', href: '/dashboard' },
          { label: 'المالية', href: '/finance/receipts' },
          { label: 'سندات القبض', href: '/finance/receipts' },
          { label: 'جديد' },
        ]}
        actions={
          <Link href="/finance/receipts">
            <Button variant="ghost" iconLeft={<ArrowRight className="h-4 w-4" />}>رجوع</Button>
          </Link>
        }
      />

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm">{error}</div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <Card className="lg:col-span-2 space-y-4">
          <h3 className="font-bold text-gray-800">معلومات السند</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <Select
              label="العميل *"
              value={customerId}
              onChange={(e) => setCustomerId(e.target.value)}
              options={customerOptions}
            />
            <Input label="تاريخ السند *" type="date" value={receiptDate} onChange={(e) => setReceiptDate(e.target.value)} />
            <Input label="المبلغ *" type="number" value={amount} onChange={(e) => setAmount(e.target.value)} min={0} step="0.0001" />
            <Input label="العملة" value={currencyCode} onChange={(e) => setCurrencyCode(e.target.value)} maxLength={3} />
            <Select
              label="طريقة الدفع"
              value={paymentMethod}
              onChange={(e) => setPaymentMethod(e.target.value)}
              options={paymentMethodOptions}
            />
          </div>

          <h3 className="font-bold text-gray-800 pt-2 border-t">التخصيص على الفواتير</h3>
          {!customerId ? (
            <p className="text-sm text-gray-500">اختر العميل أولاً لعرض فواتيره المفتوحة.</p>
          ) : openInvoices.length === 0 ? (
            <p className="text-sm text-gray-500">لا توجد فواتير مفتوحة لهذا العميل.</p>
          ) : (
            <>
              <div className="space-y-2">
                {allocations.map((a) => (
                  <div key={a.id} className="flex items-end gap-2">
                    <div className="flex-1">
                      <Select
                        label="الفاتورة"
                        value={a.salesInvoiceId}
                        onChange={(e) => updateAlloc(a.id, { salesInvoiceId: e.target.value })}
                        options={invoiceOptions}
                      />
                    </div>
                    <div className="w-40">
                      <Input
                        label="المبلغ"
                        type="number"
                        value={a.amountApplied}
                        onChange={(e) => updateAlloc(a.id, { amountApplied: e.target.value })}
                        min={0}
                        step="0.0001"
                      />
                    </div>
                    <button type="button" onClick={() => removeAlloc(a.id)} className="text-red-500 hover:text-red-700 p-2 mb-1" disabled={allocations.length === 1}>
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                ))}
              </div>
              <Button type="button" variant="secondary" onClick={addAlloc} iconLeft={<Plus className="h-4 w-4" />}>
                إضافة تخصيص
              </Button>
            </>
          )}

          <h3 className="font-bold text-gray-800 pt-2 border-t">ملاحظات</h3>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={2}
            className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-200"
            placeholder="ملاحظات اختيارية..."
          />
        </Card>

        <Card>
          <h3 className="font-bold text-gray-800 mb-3">الملخص</h3>
          <div className="space-y-2 text-sm">
            <div className="flex justify-between">
              <span className="text-gray-600">المبلغ:</span>
              <span className="font-mono font-semibold">{formatNumber(Number(amount))} {currencyCode}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600">مجموع التخصيصات:</span>
              <span className={`font-mono font-semibold ${Math.abs(totalAllocated - Number(amount)) > 0.0001 ? 'text-red-600' : 'text-green-600'}`}>
                {formatNumber(totalAllocated)}
              </span>
            </div>
            <div className="flex justify-between border-t pt-2">
              <span className="font-bold text-gray-800">الفرق:</span>
              <span className={`font-mono font-bold ${Math.abs(totalAllocated - Number(amount)) > 0.0001 ? 'text-red-600' : 'text-green-600'}`}>
                {formatNumber(Number(amount) - totalAllocated)}
              </span>
            </div>
          </div>

          <div className="mt-6 space-y-2">
            <Button
              type="button"
              variant="secondary"
              loading={submitting}
              onClick={() => submit(false)}
              iconLeft={<Save className="h-4 w-4" />}
              className="w-full"
            >
              حفظ كمسودة
            </Button>
            <Button
              type="button"
              variant="primary"
              loading={submitting}
              onClick={() => submit(true)}
              iconLeft={<Send className="h-4 w-4" />}
              className="w-full"
            >
              حفظ وترحيل (Dr 1210 / Cr 1230)
            </Button>
          </div>
        </Card>
      </div>
    </div>
  );
}
