'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight, FileText, RefreshCw } from 'lucide-react';
import { PageHeader, Card, Button } from '@/components/ui';
import { api, getErrorMessage } from '@/lib/api';
import { formatDate, formatCurrency } from '@/lib/utils';

export default function HrPayrollIdPage() {
  const params = useParams<{ id: string }>();
  const id = params.id;

  const [item, setItem] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => { load(); }, [id]);

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const url = "/api/hr/payroll/runs/{id}".replace('{id}', encodeURIComponent(id || ''));
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
        title="تشغيل رواتب"
        description="بيانات Payroll Run + البنود + المعالجة"
        actions={
          <Link href="/hr/payroll">
            <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />}>العودة إلى Payroll</Button>
          </Link>
        }
      />

      {error && (
        <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg mb-4">
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
              <div className="flex justify-between text-sm gap-2">
                <dt className="text-gray-500 flex-shrink-0">runNumber</dt>
                <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
                  {String(item.runNumber ?? item['runNumberName'] ?? '—')}
                </dd>
              </div>
              <div className="flex justify-between text-sm gap-2">
                <dt className="text-gray-500 flex-shrink-0">periodStart</dt>
                <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
                  {String(item.periodStart ?? item['periodStartName'] ?? '—')}
                </dd>
              </div>
              <div className="flex justify-between text-sm gap-2">
                <dt className="text-gray-500 flex-shrink-0">periodEnd</dt>
                <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
                  {String(item.periodEnd ?? item['periodEndName'] ?? '—')}
                </dd>
              </div>
              <div className="flex justify-between text-sm gap-2">
                <dt className="text-gray-500 flex-shrink-0">status</dt>
                <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
                  {String(item.status ?? item['statusName'] ?? '—')}
                </dd>
              </div>
              <div className="flex justify-between text-sm gap-2">
                <dt className="text-gray-500 flex-shrink-0">totalGross</dt>
                <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
                  {String(item.totalGross ?? item['totalGrossName'] ?? '—')}
                </dd>
              </div>
              <div className="flex justify-between text-sm gap-2">
                <dt className="text-gray-500 flex-shrink-0">totalNet</dt>
                <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
                  {String(item.totalNet ?? item['totalNetName'] ?? '—')}
                </dd>
              </div>
              <div className="flex justify-between text-sm gap-2">
                <dt className="text-gray-500 flex-shrink-0">totalTax</dt>
                <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
                  {String(item.totalTax ?? item['totalTaxName'] ?? '—')}
                </dd>
              </div>
              <div className="flex justify-between text-sm gap-2">
                <dt className="text-gray-500 flex-shrink-0">employeeCount</dt>
                <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
                  {String(item.employeeCount ?? item['employeeCountName'] ?? '—')}
                </dd>
              </div>
              <div className="flex justify-between text-sm gap-2">
                <dt className="text-gray-500 flex-shrink-0">createdAt</dt>
                <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
                  {String(item.createdAt ?? item['createdAtName'] ?? '—')}
                </dd>
              </div>
            </dl>
          </Card>

          <Card className="p-6">
            <h3 className="text-lg font-bold text-gray-800 mb-4">الإجراءات</h3>
            <div className="space-y-2">
              <Button variant="primary" onClick={load} iconLeft={<RefreshCw className="h-4 w-4" />} className="w-full">
                إعادة تحميل
              </Button>
              <Link href="/hr/payroll">
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
