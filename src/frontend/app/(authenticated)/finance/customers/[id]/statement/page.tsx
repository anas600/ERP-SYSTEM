'use client';

// Sprint 36 (DEC-122): Customer Statement (كشف حساب عميل)
// Opening balance + chronological invoices/receipts + closing balance
// AR convention: positive balance = customer owes us

import { useEffect, useState, useMemo } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { ArrowLeft, ArrowRight, FileText, RefreshCw, Calendar, AlertCircle } from 'lucide-react';
import { PageHeader, Card, Button } from '@/components/ui';
import { arApi, CustomerStatement, getErrorMessage } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { formatNumber } from '@/lib/format';

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}
function daysAgoIso(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() - days);
  return d.toISOString().slice(0, 10);
}

export default function CustomerStatementPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const id = params.id;

  const [data, setData] = useState<CustomerStatement | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [from, setFrom] = useState<string>(daysAgoIso(90));
  const [to, setTo] = useState<string>(todayIso());

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const r = await arApi.getCustomerStatement(id, from, to);
      setData(r);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل كشف الحساب.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const onApplyFilter = () => {
    load();
  };

  const onClearFilter = () => {
    setFrom('');
    setTo('');
    // Load all-time statement (no filter)
    setLoading(true);
    setError(null);
    arApi
      .getCustomerStatement(id)
      .then((r) => setData(r))
      .catch((e: unknown) => setError(getErrorMessage(e, 'فشل تحميل كشف الحساب.')))
      .finally(() => setLoading(false));
  };

  const totals = useMemo(() => {
    if (!data) return null;
    return {
      invoiced: data.totalInvoiced,
      received: data.totalReceived,
      closing: data.closingBalance,
      count: data.lines.length,
    };
  }, [data]);

  return (
    <div>
      <div className="mb-3">
        <button
          onClick={() => router.back()}
          className="inline-flex items-center gap-1 text-sm text-gray-500 hover:text-blue-600 transition-colors"
        >
          <ArrowLeft className="h-4 w-4" />
          العودة
        </button>
      </div>

      <PageHeader
        title={data ? `كشف حساب: ${data.customerName}` : 'كشف حساب عميل'}
        description={
          data
            ? `${data.customerCode} • ${from || 'البداية'} → ${to || 'النهاية'}`
            : 'الرصيد الافتتاحي + الفواتير + المقبوضات + الرصيد الختامي'
        }
        actions={
          <div className="flex items-center gap-2">
            <Link href={`/finance/customers/${id}`}>
              <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />}>
                بطاقة العميل
              </Button>
            </Link>
          </div>
        }
      />

      {/* Date Range Filter */}
      <Card className="mb-4 p-4">
        <div className="flex flex-wrap items-end gap-3">
          <div className="flex flex-col">
            <label className="text-xs text-gray-500 mb-1">من تاريخ</label>
            <input
              type="date"
              value={from}
              onChange={(e) => setFrom(e.target.value)}
              className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div className="flex flex-col">
            <label className="text-xs text-gray-500 mb-1">إلى تاريخ</label>
            <input
              type="date"
              value={to}
              onChange={(e) => setTo(e.target.value)}
              className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <Button variant="primary" onClick={onApplyFilter} iconLeft={<Calendar className="h-4 w-4" />}>
            تطبيق
          </Button>
          <Button variant="secondary" onClick={onClearFilter}>
            كل الفترات
          </Button>
          <Button variant="ghost" onClick={load} iconLeft={<RefreshCw className="h-4 w-4" />}>
            إعادة تحميل
          </Button>
        </div>
      </Card>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm flex items-start gap-2">
          <AlertCircle className="h-5 w-5 flex-shrink-0 mt-0.5" />
          <span>{error}</span>
        </div>
      )}

      {loading ? (
        <div className="text-center py-12 text-gray-500">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
          <p className="mt-3 text-sm">جاري التحميل...</p>
        </div>
      ) : !data ? (
        <Card className="p-12 text-center text-gray-500">
          <FileText className="h-12 w-12 mx-auto mb-3 text-gray-300" />
          لا توجد بيانات.
        </Card>
      ) : (
        <>
          {/* Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-4">
            <Card className="p-4">
              <p className="text-xs text-gray-500 mb-1">الرصيد الافتتاحي</p>
              <p className="text-xl font-bold font-mono text-gray-800">
                {formatNumber(data.openingBalance)} <span className="text-sm text-gray-500">LYD</span>
              </p>
            </Card>
            <Card className="p-4">
              <p className="text-xs text-gray-500 mb-1">إجمالي الفواتير</p>
              <p className="text-xl font-bold font-mono text-blue-700">
                {formatNumber(totals?.invoiced ?? 0)} <span className="text-sm text-gray-500">LYD</span>
              </p>
            </Card>
            <Card className="p-4">
              <p className="text-xs text-gray-500 mb-1">إجمالي المقبوضات</p>
              <p className="text-xl font-bold font-mono text-green-700">
                {formatNumber(totals?.received ?? 0)} <span className="text-sm text-gray-500">LYD</span>
              </p>
            </Card>
            <Card
              className={`p-4 ${
                (totals?.closing ?? 0) > 0
                  ? 'border-r-4 border-red-400 bg-red-50/30'
                  : (totals?.closing ?? 0) < 0
                  ? 'border-r-4 border-blue-400 bg-blue-50/30'
                  : ''
              }`}
            >
              <p className="text-xs text-gray-500 mb-1">الرصيد الختامي</p>
              <p
                className={`text-xl font-bold font-mono ${
                  (totals?.closing ?? 0) > 0
                    ? 'text-red-700'
                    : (totals?.closing ?? 0) < 0
                    ? 'text-blue-700'
                    : 'text-gray-800'
                }`}
              >
                {formatNumber(totals?.closing ?? 0)} <span className="text-sm text-gray-500">LYD</span>
              </p>
              <p className="text-[10px] text-gray-400 mt-1">
                {(totals?.closing ?? 0) > 0
                  ? '← على العميل (مديون)'
                  : (totals?.closing ?? 0) < 0
                  ? '← لنا رصيد دائن'
                  : 'مسوّى'}
              </p>
            </Card>
          </div>

          {/* Lines Table */}
          <Card className="overflow-x-auto p-0">
            {data.lines.length === 0 ? (
              <div className="p-12 text-center text-gray-500">
                <FileText className="h-12 w-12 mx-auto mb-3 text-gray-300" />
                لا توجد حركات في هذه الفترة.
              </div>
            ) : (
              <table className="w-full text-sm" dir="rtl">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>
                    <th className="text-start px-4 py-3 font-semibold text-gray-700">التاريخ</th>
                    <th className="text-start px-4 py-3 font-semibold text-gray-700">النوع</th>
                    <th className="text-start px-4 py-3 font-semibold text-gray-700">المرجع</th>
                    <th className="text-start px-4 py-3 font-semibold text-gray-700">الوصف</th>
                    <th className="text-end px-4 py-3 font-semibold text-gray-700">مدين</th>
                    <th className="text-end px-4 py-3 font-semibold text-gray-700">دائن</th>
                    <th className="text-end px-4 py-3 font-semibold text-gray-700">الرصيد</th>
                  </tr>
                </thead>
                <tbody>
                  {data.lines.map((l, i) => (
                    <tr
                      key={i}
                      className={`border-b border-gray-100 hover:bg-gray-50 transition-colors ${
                        l.type === 'Opening' ? 'bg-amber-50/40 font-semibold' : ''
                      }`}
                    >
                      <td className="px-4 py-2.5 text-gray-700 font-mono text-xs">
                        {formatDate(l.date)}
                      </td>
                      <td className="px-4 py-2.5">
                        <span
                          className={`text-xs px-2 py-0.5 rounded ${
                            l.type === 'فاتورة'
                              ? 'bg-blue-100 text-blue-700'
                              : l.type === 'سند قبض'
                              ? 'bg-green-100 text-green-700'
                              : 'bg-amber-100 text-amber-700'
                          }`}
                        >
                          {l.type}
                        </span>
                      </td>
                      <td className="px-4 py-2.5 font-mono text-xs text-blue-600">{l.reference || '—'}</td>
                      <td className="px-4 py-2.5 text-gray-600 text-xs max-w-xs truncate" title={l.description}>
                        {l.description || '—'}
                      </td>
                      <td className="px-4 py-2.5 text-end font-mono text-blue-700">
                        {l.debit > 0 ? formatNumber(l.debit) : '—'}
                      </td>
                      <td className="px-4 py-2.5 text-end font-mono text-green-700">
                        {l.credit > 0 ? formatNumber(l.credit) : '—'}
                      </td>
                      <td
                        className={`px-4 py-2.5 text-end font-mono font-bold ${
                          l.runningBalance > 0
                            ? 'text-red-700'
                            : l.runningBalance < 0
                            ? 'text-blue-700'
                            : 'text-gray-500'
                        }`}
                      >
                        {formatNumber(l.runningBalance)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </Card>

          <p className="mt-3 text-xs text-gray-500">
            إجمالي الحركات: <span className="font-semibold">{data.lines.length}</span> • موجب = على العميل
            (مديون لنا) • سالب = لنا رصيد دائن
          </p>
        </>
      )}
    </div>
  );
}
