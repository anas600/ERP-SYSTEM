// Generate detail pages using generic api.get (no specific methods needed)
const fs = require('fs');
const path = require('path');

const BASE = 'F:/minimaxDescktop2/ERP-SYstem/src/frontend/app/(authenticated)';

const pages = [
  {
    dir: 'finance/customers/[id]',
    title: 'بطاقة العميل',
    desc: 'بيانات العميل + الفواتير + الرصيد',
    apiUrl: '/api/ar/customers/{id}',
    listUrl: '/finance/customers',
    listLabel: 'العملاء',
    fields: ['code', 'name', 'nameEn', 'email', 'phone', 'address', 'creditLimit', 'paymentTermsDays', 'isActive', 'createdAt'],
  },
  {
    dir: 'finance/receipts/[id]',
    title: 'سند قبض',
    desc: 'بيانات سند القبض + التوزيعات',
    apiUrl: '/api/ar/receipts/{id}',
    listUrl: '/finance/receipts',
    listLabel: 'سندات القبض',
    fields: ['receiptNumber', 'receiptDate', 'customerName', 'amount', 'currency', 'status', 'method', 'reference', 'createdAt'],
  },
  {
    dir: 'procurement/purchase-orders/[id]',
    title: 'أمر شراء',
    desc: 'بيانات أمر الشراء + البنود + الاعتماد',
    apiUrl: '/api/procurement/pos/{id}',
    listUrl: '/procurement/purchase-orders',
    listLabel: 'أوامر الشراء',
    fields: ['poNumber', 'orderDate', 'vendorName', 'vendorCode', 'status', 'totalAmount', 'currency', 'expectedDate', 'createdAt'],
  },
  {
    dir: 'procurement/goods-receipts/[id]',
    title: 'استلام بضاعة',
    desc: 'بيانات استلام البضاعة + البنود',
    apiUrl: '/api/procurement/grs/{id}',
    listUrl: '/procurement/goods-receipts',
    listLabel: 'استلامات البضاعة',
    fields: ['grNumber', 'receivedDate', 'poNumber', 'vendorName', 'warehouseName', 'status', 'createdAt'],
  },
  {
    dir: 'procurement/bills/[id]',
    title: 'فاتورة مورد',
    desc: 'بيانات فاتورة المورد + البنود + الدفعات',
    apiUrl: '/api/procurement/bills/{id}',
    listUrl: '/procurement/bills',
    listLabel: 'فواتير الموردين',
    fields: ['billNumber', 'billDate', 'dueDate', 'vendorName', 'subtotal', 'vatAmount', 'total', 'currency', 'status', 'createdAt'],
  },
  {
    dir: 'hr/employees/[id]',
    title: 'بطاقة موظف',
    desc: 'بيانات الموظف + الراتب + الحضور',
    apiUrl: '/api/hr/employees/{id}',
    listUrl: '/hr/employees',
    listLabel: 'الموظفين',
    fields: ['employeeNumber', 'fullName', 'email', 'phone', 'nationalId', 'jobTitle', 'hireDate', 'baseSalary', 'isActive'],
  },
  {
    dir: 'hr/payroll/[id]',
    title: 'تشغيل رواتب',
    desc: 'بيانات Payroll Run + البنود + المعالجة',
    apiUrl: '/api/hr/payroll/runs/{id}',
    listUrl: '/hr/payroll',
    listLabel: 'Payroll',
    fields: ['runNumber', 'periodStart', 'periodEnd', 'status', 'totalGross', 'totalNet', 'totalTax', 'employeeCount', 'createdAt'],
  },
  {
    dir: 'inventory/items/[id]',
    title: 'بطاقة صنف',
    desc: 'بيانات الصنف + المخزون + الحركات',
    apiUrl: '/api/inventory/items/{id}',
    listUrl: '/inventory/items',
    listLabel: 'الأصناف',
    fields: ['sku', 'barcode', 'name', 'nameEn', 'categoryName', 'unitOfMeasure', 'costPrice', 'sellingPrice', 'reorderLevel', 'isActive'],
  },
  {
    dir: 'inventory/movements/[id]',
    title: 'حركة مخزون',
    desc: 'بيانات حركة المخزون',
    apiUrl: '/api/inventory/movements/{id}',
    listUrl: '/inventory/movements',
    listLabel: 'حركات المخزون',
    fields: ['movementNumber', 'movementDate', 'itemName', 'warehouseName', 'movementType', 'quantity', 'unitCost', 'totalCost', 'reference', 'createdAt'],
  },
  {
    dir: 'projects/[id]',
    title: 'مشروع',
    desc: 'بيانات المشروع + المهام + الميزانية',
    apiUrl: '/api/projects/{id}',
    listUrl: '/projects',
    listLabel: 'المشاريع',
    fields: ['code', 'name', 'customerName', 'startDate', 'endDate', 'status', 'budget', 'actualCost', 'progress', 'createdAt'],
  },
];

function toPascal(s) {
  return s.replace(/[^a-zA-Z0-9]/g, ' ').split(' ').filter(Boolean).map(x => x[0].toUpperCase() + x.slice(1)).join('');
}

const template = (p) => `'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight, FileText, RefreshCw } from 'lucide-react';
import { PageHeader, Card, Button } from '@/components/ui';
import { api, getErrorMessage } from '@/lib/api';
import { formatDate, formatCurrency } from '@/lib/utils';

export default function ${toPascal(p.dir)}Page() {
  const params = useParams<{ id: string }>();
  const id = params.id;

  const [item, setItem] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => { load(); }, [id]);

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const url = "${p.apiUrl}".replace('{id}', encodeURIComponent(id || ''));
      const r = await api.get(url);
      setItem(r.data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل البيانات.'));
    } finally { setLoading(false); }
  };

  if (loading) {
    return (
      <div className="text-center py-12 text-gray-500">
        <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
        <p className="mt-3 text-sm">جاري التحميل...</p>
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title="${p.title}"
        description="${p.desc}"
        actions={
          <Link href="${p.listUrl}">
            <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />}>العودة إلى ${p.listLabel}</Button>
          </Link>
        }
      />

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
          {error}
        </div>
      )}

      {!item ? (
        <Card className="p-12 text-center text-gray-500">
          <FileText className="h-12 w-12 mx-auto mb-3 text-gray-300" />
          لم يتم العثور على السجل.
        </Card>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          <Card className="p-6">
            <h3 className="text-lg font-bold text-gray-800 mb-4">المعلومات الأساسية</h3>
            <dl className="space-y-3">
${p.fields.map(f => `              <div className="flex justify-between text-sm gap-2">
                <dt className="text-gray-500 flex-shrink-0">${f}</dt>
                <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
                  {String(item.${f} ?? item['${f}Name'] ?? '—')}
                </dd>
              </div>`).join('\n')}
            </dl>
          </Card>

          <Card className="p-6">
            <h3 className="text-lg font-bold text-gray-800 mb-4">الإجراءات</h3>
            <div className="space-y-2">
              <Button variant="primary" onClick={load} iconLeft={<RefreshCw className="h-4 w-4" />} className="w-full">
                إعادة تحميل
              </Button>
              <Link href="${p.listUrl}">
                <Button variant="secondary" className="w-full">العودة للقائمة</Button>
              </Link>
            </div>
          </Card>

          <Card className="p-4 lg:col-span-2">
            <h3 className="text-sm font-semibold text-gray-700 mb-2">البيانات الخام (JSON)</h3>
            <pre className="text-xs bg-gray-50 p-3 rounded overflow-auto max-h-96" dir="ltr">
              {JSON.stringify(item, null, 2)}
            </pre>
          </Card>
        </div>
      )}
    </div>
  );
}
`;

for (const p of pages) {
  const dir = path.join(BASE, p.dir);
  fs.mkdirSync(dir, { recursive: true });
  const file = path.join(dir, 'page.tsx');
  fs.writeFileSync(file, template(p), 'utf8');
  console.log('Wrote', file);
}

console.log('\\nAll detail pages regenerated!');
