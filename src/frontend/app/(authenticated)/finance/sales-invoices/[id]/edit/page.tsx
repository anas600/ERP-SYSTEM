'use client';

// صفحة تعديل فاتورة مبيعات (Draft فقط) — customer + line items + totals auto-calc
//
// الفواتير المُرحَّلة (status !== Draft) لا يمكن تعديلها — تظهر رسالة "لا يمكن التعديل".
// PUT /api/ar/sales-invoices/{id} يقبل body كامل بنفس بنية create (بدون postImmediately).

import { useEffect, useMemo, useState } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight, Save, Plus, Trash2, Lock, FileText } from 'lucide-react';
import { Button, Input, Select, Card, PageHeader, Badge } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { arApi, Customer, SalesInvoice, SALES_INVOICE_STATUSES, SALES_INVOICE_STATUS_VARIANTS, getErrorMessage } from '@/lib/api';
import { projectsApi, Project } from '@/lib/api';
import { inventoryApi, Item } from '@/lib/api';
import { formatNumber } from '@/lib/format';

interface LineDraft {
  // id محلي للـ React key فقط — id الفعلي للبند يأتي من الـ backend (عند التعديل)
  localId: string;
  id?: string;
  description: string;
  quantity: string;
  unitPrice: string;
  taxRate: string;
  itemId?: string;
}

const emptyLine = (): LineDraft => ({
  localId: crypto.randomUUID(),
  description: '',
  quantity: '1',
  unitPrice: '0',
  taxRate: '0',
});

export default function EditSalesInvoicePage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  useAuth();

  const [invoice, setInvoice] = useState<SalesInvoice | null>(null);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [projects, setProjects] = useState<Project[]>([]);
  const [items, setItems] = useState<Item[]>([]);
  const [customerId, setCustomerId] = useState<string>('');
  const [invoiceDate, setInvoiceDate] = useState<string>('');
  const [dueDate, setDueDate] = useState<string>('');
  const [notes, setNotes] = useState<string>('');
  const [projectId, setProjectId] = useState<string>('');
  const [lines, setLines] = useState<LineDraft[]>([emptyLine()]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        const inv = await arApi.getInvoice(params.id);
        setInvoice(inv);
        if (inv.status !== 1) {
          // ليست مسودة — لا نُحمِّل الـ form (الـ render سيعرض شاشة "لا يمكن التعديل")
          return;
        }
        setCustomerId(inv.customerId || '');
        setInvoiceDate(inv.invoiceDate ? inv.invoiceDate.split('T')[0] : '');
        setDueDate(inv.dueDate ? inv.dueDate.split('T')[0] : '');
        setNotes(inv.notes || '');
        setProjectId(inv.projectId || '');
        setLines(
          inv.lines.length > 0
            ? inv.lines.map((l) => ({
                localId: crypto.randomUUID(),
                id: l.id,
                description: l.description || '',
                quantity: String(l.quantity),
                unitPrice: String(l.unitPrice),
                taxRate: String(l.taxRate),
                itemId: l.itemId,
              }))
            : [emptyLine()]
        );
      } catch (e: unknown) {
        setError(getErrorMessage(e, 'تعذّر تحميل الفاتورة.'));
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [params.id]);

  // تحميل القوائم المرجعية بعد تحميل الفاتورة (لا نعتمد على نجاحها)
  useEffect(() => {
    arApi.listCustomers().then(setCustomers).catch(() => undefined);
    projectsApi.listProjects().then(setProjects).catch(() => undefined);
    inventoryApi.listItems().then(setItems).catch(() => undefined);
  }, []);

  const customerOptions = useMemo(
    () => [
      { value: '', label: 'اختر العميل' },
      ...customers.filter((c) => c.isActive).map((c) => ({ value: c.id, label: `${c.code} — ${c.name}` })),
    ],
    [customers]
  );

  const projectOptions = useMemo(
    () => [
      { value: '', label: '— بدون مشروع —' },
      ...projects.filter((p) => p.isActive).map((p) => ({ value: p.id, label: `${p.code} — ${p.name}` })),
    ],
    [projects]
  );

  const itemOptions = useMemo(
    () => [
      { value: '', label: '— بدون منتج —' },
      ...items.filter((i) => i.isActive).map((i) => ({ value: i.id, label: `${i.sku} — ${i.name}` })),
    ],
    [items]
  );

  const updateLine = (localId: string, patch: Partial<LineDraft>) => {
    setLines((ls) => ls.map((l) => (l.localId === localId ? { ...l, ...patch } : l)));
  };
  const removeLine = (localId: string) => {
    setLines((ls) => (ls.length > 1 ? ls.filter((l) => l.localId !== localId) : ls));
  };
  const addLine = () => setLines((ls) => [...ls, emptyLine()]);

  const totals = useMemo(() => {
    let subtotal = 0;
    let taxAmount = 0;
    for (const l of lines) {
      const qty = Number(l.quantity) || 0;
      const price = Number(l.unitPrice) || 0;
      const tax = Number(l.taxRate) || 0;
      const lineSub = qty * price;
      const lineTax = lineSub * tax;
      subtotal += lineSub;
      taxAmount += lineTax;
    }
    return { subtotal, taxAmount, total: subtotal + taxAmount };
  }, [lines]);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!invoice) return;
    setError(null);
    if (!customerId) {
      setError('الرجاء اختيار العميل.');
      return;
    }
    if (lines.every((l) => !l.description.trim() || Number(l.quantity) <= 0)) {
      setError('الرجاء إضافة بند واحد على الأقل بوصف وكمية صحيحة.');
      return;
    }
    setSubmitting(true);
    try {
      await arApi.updateInvoice(params.id, {
        customerId,
        invoiceDate: new Date(invoiceDate).toISOString(),
        dueDate: dueDate ? new Date(dueDate).toISOString() : undefined,
        currencyCode: invoice.currencyCode,
        exchangeRate: invoice.exchangeRate,
        notes: notes || undefined,
        projectId: projectId || undefined,
        lines: lines
          .filter((l) => l.description.trim() && Number(l.quantity) > 0)
          .map((l) => ({
            description: l.description.trim(),
            quantity: Number(l.quantity),
            unitPrice: Number(l.unitPrice),
            taxRate: Number(l.taxRate) || 0,
            itemId: l.itemId || undefined,
          })),
      });
      router.push(`/finance/sales-invoices/${params.id}`);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحديث الفاتورة.'));
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div>
        <PageHeader title="✏️ تعديل فاتورة مبيعات" />
        <Card>
          <div className="text-center py-12 text-gray-500">جاري التحميل...</div>
        </Card>
      </div>
    );
  }

  if (error && !invoice) {
    return (
      <div>
        <PageHeader title="✏️ تعديل فاتورة مبيعات" />
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
          {error}
        </div>
        <div className="mt-4">
          <Link href="/finance/sales-invoices">
            <Button variant="ghost">رجوع للقائمة</Button>
          </Link>
        </div>
      </div>
    );
  }

  if (!invoice) return null;

  // غير مسودة → لا يمكن التعديل
  if (invoice.status !== 1) {
    const statusLabel = SALES_INVOICE_STATUSES[invoice.status] || '—';
    const variant = SALES_INVOICE_STATUS_VARIANTS[invoice.status] || 'neutral';
    return (
      <div>
        <PageHeader
          title={`✏️ تعديل فاتورة ${invoice.invoiceNumber}`}
          breadcrumb={[
            { label: 'الرئيسية', href: '/dashboard' },
            { label: 'فواتير المبيعات', href: '/finance/sales-invoices' },
            { label: invoice.invoiceNumber, href: `/finance/sales-invoices/${params.id}` },
            { label: 'تعديل' },
          ]}
          actions={
            <Link href={`/finance/sales-invoices/${params.id}`}>
              <Button variant="ghost" iconLeft={<ArrowRight className="h-4 w-4" />}>
                العودة للتفاصيل
              </Button>
            </Link>
          }
        />

        <Card className="max-w-xl">
          <div className="flex items-center gap-3 mb-3">
            <Lock className="h-5 w-5 text-gray-500" />
            <h3 className="font-bold text-gray-800">لا يمكن تعديل هذه الفاتورة</h3>
            <Badge variant={variant}>{statusLabel}</Badge>
          </div>
          <p className="text-sm text-gray-600">
            الفواتير المُرحَّلة (Sent, Partially Paid, Paid, Overdue) أو المُلغاة لا يمكن تعديلها.
            لإنشاء فاتورة بديلة، ألغِ هذه وأنشئ فاتورة جديدة.
          </p>
          <div className="mt-4 flex items-center gap-2">
            <Link href={`/finance/sales-invoices/${params.id}`}>
              <Button variant="primary" iconLeft={<FileText className="h-4 w-4" />}>
                العودة لبطاقة الفاتورة
              </Button>
            </Link>
            <Link href="/finance/sales-invoices/new">
              <Button variant="secondary" iconLeft={<Plus className="h-4 w-4" />}>
                فاتورة جديدة
              </Button>
            </Link>
          </div>
        </Card>
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title={`✏️ تعديل فاتورة ${invoice.invoiceNumber}`}
        description={invoice.customerName ? `العميل: ${invoice.customerName}` : 'مسودة قابلة للتعديل'}
        breadcrumb={[
          { label: 'الرئيسية', href: '/dashboard' },
          { label: 'فواتير المبيعات', href: '/finance/sales-invoices' },
          { label: invoice.invoiceNumber, href: `/finance/sales-invoices/${params.id}` },
          { label: 'تعديل' },
        ]}
        actions={
          <Link href={`/finance/sales-invoices/${params.id}`}>
            <Button variant="ghost" iconLeft={<ArrowRight className="h-4 w-4" />}>
              إلغاء والعودة
            </Button>
          </Link>
        }
      />

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm">{error}</div>
      )}

      <form onSubmit={onSubmit}>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <Card className="lg:col-span-2 space-y-4">
            <h3 className="font-bold text-gray-800">معلومات أساسية</h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <Select
                label="العميل *"
                value={customerId}
                onChange={(e) => setCustomerId(e.target.value)}
                options={customerOptions}
                required
              />
              <Input
                label="تاريخ الفاتورة *"
                type="date"
                value={invoiceDate}
                onChange={(e) => setInvoiceDate(e.target.value)}
                required
              />
              <Input
                label="تاريخ الاستحقاق"
                type="date"
                value={dueDate}
                onChange={(e) => setDueDate(e.target.value)}
              />
              <Select
                label="المشروع (اختياري)"
                value={projectId}
                onChange={(e) => setProjectId(e.target.value)}
                options={projectOptions}
              />
            </div>

            <h3 className="font-bold text-gray-800 pt-2">البنود</h3>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-right text-xs text-gray-500 border-b">
                    <th className="py-2 pr-2">#</th>
                    <th className="py-2 pr-2">المنتج</th>
                    <th className="py-2 pr-2">الوصف</th>
                    <th className="py-2 pr-2">الكمية</th>
                    <th className="py-2 pr-2">السعر</th>
                    <th className="py-2 pr-2">الضريبة</th>
                    <th className="py-2 pr-2 text-left">المجموع</th>
                    <th className="py-2"></th>
                  </tr>
                </thead>
                <tbody>
                  {lines.map((l, i) => {
                    const qty = Number(l.quantity) || 0;
                    const price = Number(l.unitPrice) || 0;
                    const tax = Number(l.taxRate) || 0;
                    const lineTotal = qty * price;
                    return (
                      <tr key={l.localId} className="border-b align-top">
                        <td className="py-2 pr-2 text-gray-500">{i + 1}</td>
                        <td className="py-2 pr-2 min-w-[140px]">
                          <Select
                            value={l.itemId || ''}
                            onChange={(e) => updateLine(l.localId, { itemId: e.target.value || undefined })}
                            options={itemOptions}
                          />
                        </td>
                        <td className="py-2 pr-2 min-w-[200px]">
                          <Input
                            value={l.description}
                            onChange={(e) => updateLine(l.localId, { description: e.target.value })}
                            placeholder="وصف البند"
                          />
                        </td>
                        <td className="py-2 pr-2">
                          <Input
                            type="number"
                            value={l.quantity}
                            onChange={(e) => updateLine(l.localId, { quantity: e.target.value })}
                            min={0}
                            step="0.0001"
                          />
                        </td>
                        <td className="py-2 pr-2">
                          <Input
                            type="number"
                            value={l.unitPrice}
                            onChange={(e) => updateLine(l.localId, { unitPrice: e.target.value })}
                            min={0}
                            step="0.0001"
                          />
                        </td>
                        <td className="py-2 pr-2">
                          <Input
                            type="number"
                            value={l.taxRate}
                            onChange={(e) => updateLine(l.localId, { taxRate: e.target.value })}
                            min={0}
                            max={1}
                            step="0.0001"
                          />
                        </td>
                        <td className="py-2 pr-2 text-left font-mono font-semibold whitespace-nowrap">
                          {formatNumber(lineTotal)}
                        </td>
                        <td className="py-2 text-center">
                          <button
                            type="button"
                            onClick={() => removeLine(l.localId)}
                            className="text-red-500 hover:text-red-700 p-1"
                            disabled={lines.length === 1}
                            title="حذف البند"
                          >
                            <Trash2 className="h-4 w-4" />
                          </button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            <Button type="button" variant="secondary" onClick={addLine} iconLeft={<Plus className="h-4 w-4" />}>
              إضافة بند
            </Button>

            <h3 className="font-bold text-gray-800 pt-2">ملاحظات</h3>
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
                <span className="text-gray-600">الإجمالي قبل الضريبة:</span>
                <span className="font-mono font-semibold">{formatNumber(totals.subtotal)}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-600">الضريبة:</span>
                <span className="font-mono font-semibold">{formatNumber(totals.taxAmount)}</span>
              </div>
              <div className="flex justify-between border-t pt-2">
                <span className="font-bold text-gray-800">الإجمالي:</span>
                <span className="font-mono font-bold text-blue-600 text-lg">{formatNumber(totals.total)}</span>
              </div>
            </div>

            <div className="mt-6 space-y-2">
              <Button
                type="submit"
                variant="primary"
                loading={submitting}
                iconLeft={<Save className="h-4 w-4" />}
                className="w-full"
              >
                حفظ التعديلات
              </Button>
              <Link href={`/finance/sales-invoices/${params.id}`}>
                <Button type="button" variant="ghost" className="w-full">
                  إلغاء
                </Button>
              </Link>
            </div>
          </Card>
        </div>
      </form>
    </div>
  );
}
