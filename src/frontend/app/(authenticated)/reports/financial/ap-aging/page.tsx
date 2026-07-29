'use client';

import { useEffect, useCallback, useState } from 'react';
import { ArrowLeft, FileText } from 'lucide-react';
import Link from 'next/link';
import { PageHeader, Card, Button } from '@/components/ui';
import { reportsApi, getErrorMessage } from '@/lib/api';
import { formatDate, formatCurrency } from '@/lib/utils';

export default function ApAgingPage() {
  const [asOf, setAsOf] = useState('2026-07-26');
  const [report, setReport] = useState<any>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);


  const load = useCallback(async () => {
    setLoading(true); setError(null);
    try {
      const data = await reportsApi.apAging(asOf);
      setReport(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل التقرير.'));
    } finally { setLoading(false); }
  }, [asOf]);
  useEffect(() => { load(); }, [load]);

  return (
    <div>
      <PageHeader
        title="أعمار الذمم الدائنة"
        description="AP Aging — فواتير الموردين المستحقة"
        actions={
          <Link href="/reports/financial">
            <Button variant="secondary" iconLeft={<ArrowLeft className="h-4 w-4" />}>العودة</Button>
          </Link>
        }
      />

      <Card className="p-4 mb-4">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
          <div>
            <label className="text-xs text-gray-500 block mb-1">كما في تاريخ</label>
            <input type="date" value={asOf} onChange={(e) => setAsOf(e.target.value)} className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm" />
          </div>
          <div className="flex items-end">
            <Button onClick={load} variant="primary" disabled={loading}>
              {loading ? 'جاري التحميل...' : 'تحديث'}
            </Button>
          </div>
        </div>
      </Card>

      {error && <div role="alert" className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">{error}</div>}

      {loading ? (
        <Card className="p-12 text-center text-gray-500">جاري التحميل...</Card>
      ) : !report ? (
        <Card className="p-12 text-center text-gray-500">
          <FileText className="h-12 w-12 mx-auto mb-3 text-gray-300" />
          لا توجد بيانات في الفترة المحددة.
        </Card>
      ) : (
        <Card className="p-4">
          <pre className="text-xs bg-gray-50 p-3 rounded overflow-auto" dir="ltr">
            {JSON.stringify(report, null, 2)}
          </pre>
        </Card>
      )}
    </div>
  );
}
