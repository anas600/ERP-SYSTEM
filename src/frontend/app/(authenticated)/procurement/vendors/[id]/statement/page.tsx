'use client';

// Sprint 52b (DEC-138) — Vendor Statement مع Tabs
// التابات الثلاثة:
//   1. كشف حساب — كل الحركات (Opening + Bills + Payments) بترتيب زمني + رصيد جاري
//   2. الفواتير — كل فواتير المورد + الإجمالي (من الكروت السابقة)
//   3. المدفوعات — كل المدفوعات + الإجمالي
// الرصيد الافتتاحي + الختامي يظهر في الـ header (chips) — ليس كروت منفصلة.
// طلب أنس: إزالة الكروت لأنها "غير مريحة إطلاقاً".

import { useEffect, useState, useMemo } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { ArrowLeft, ArrowRight, FileText, RefreshCw, Calendar, AlertCircle, List, FilePlus, HandCoins } from 'lucide-react';
import { PageHeader, Card, Button } from '@/components/ui';
import { procurementApi, VendorStatement, getErrorMessage } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { formatNumber } from '@/lib/format';

function todayIso(): string { return new Date().toISOString().slice(0, 10); }
function daysAgoIso(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() - days);
  return d.toISOString().slice(0, 10);
}

type Tab = 'statement' | 'bills' | 'payments';

export default function VendorStatementPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const id = params.id;

  const [data, setData] = useState<VendorStatement | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [from, setFrom] = useState<string>(daysAgoIso(90));
  const [to, setTo] = useState<string>(todayIso());
  const [tab, setTab] = useState<Tab>('statement');

  const load = async (noFilter = false) => {
    setLoading(true);
    setError(null);
    try {
      const r = noFilter
        ? await procurementApi.getVendorStatement(id)
        : await procurementApi.getVendorStatement(id, from, to);
      setData(r);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل كشف الحساب.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); /* eslint-disable-next-line */ }, [id]);

  // Filter lines by type
  const filtered = useMemo(() => {
    if (!data) return { statement: [], bills: [], payments: [], opening: null as any };
    const statement = data.lines;
    const bills = data.lines.filter((l) => l.type === 'فاتورة مورّد');
    const payments = data.lines.filter((l) => l.type === 'دفعة');
    const opening = data.lines.find((l) => l.type === 'Opening') ?? null;
    return { statement, bills, payments, opening };
  }, [data]);

  const totals = useMemo(() => {
    if (!data) return null;
    return {
      billed: data.totalBilled,
      paid: data.totalPaid,
      closing: data.closingBalance,
      opening: data.openingBalance,
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
        title={data ? `كشف حساب: ${data.vendorName}` : 'كشف حساب مورّد'}
        description={
          data
            ? `${data.vendorCode} • ${from || 'البداية'} → ${to || 'النهاية'}`
            : 'الرصيد الافتتاحي + الفواتير + المدفوعات + الرصيد الختامي'
        }
        actions={
          <div className="flex items-center gap-2">
            <Link href={`/procurement/vendors/${id}`}>
              <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />}>
                بطاقة المورّد
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
          <Button variant="primary" onClick={() => load()} iconLeft={<Calendar className="h-4 w-4" />}>
            تطبيق
          </Button>
          <Button variant="secondary" onClick={() => { setFrom(''); setTo(''); load(true); }}>
            كل الفترات
          </Button>
          <Button variant="ghost" onClick={() => load()} iconLeft={<RefreshCw className="h-4 w-4" />}>
            إعادة تحميل
          </Button>
        </div>
      </Card>

      {error && (
        <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg mb-4 text-sm flex items-start gap-2">
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
          {/* ============ Tabs ============ */}
          <div className="flex items-center gap-1 border-b border-gray-200 mb-4">
            <TabButton
              active={tab === 'statement'}
              onClick={() => setTab('statement')}
              icon={<List className="h-4 w-4" />}
              label="كشف حساب"
              count={filtered.statement.length}
            />
            <TabButton
              active={tab === 'bills'}
              onClick={() => setTab('bills')}
              icon={<FilePlus className="h-4 w-4" />}
              label="الفواتير"
              count={filtered.bills.length}
              color="orange"
            />
            <TabButton
              active={tab === 'payments'}
              onClick={() => setTab('payments')}
              icon={<HandCoins className="h-4 w-4" />}
              label="المدفوعات"
              count={filtered.payments.length}
              color="green"
            />
          </div>

          {/* ============ Tab: Statement (all lines, running balance) ============ */}
          {tab === 'statement' && (
            <Card className="overflow-x-auto p-0">
              {filtered.statement.length === 0 ? (
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
                    {filtered.statement.map((l, i) => (
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
                              l.type === 'فاتورة مورّد'
                                ? 'bg-orange-100 text-orange-700'
                                : l.type === 'دفعة'
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
                        <td className="px-4 py-2.5 text-end font-mono text-orange-700">
                          {l.debit > 0 ? formatNumber(l.debit) : '—'}
                        </td>
                        <td className="px-4 py-2.5 text-end font-mono text-green-700">
                          {l.credit > 0 ? formatNumber(l.credit) : '—'}
                        </td>
                        <td
                          className={`px-4 py-2.5 text-end font-mono font-bold ${
                            l.runningBalance > 0
                              ? 'text-orange-700'
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
                  <tfoot className="bg-gray-50 border-t-2 border-gray-300">
                    <tr>
                      <td colSpan={4} className="px-4 py-2.5 text-start text-xs font-bold">الإجمالي</td>
                      <td className="px-4 py-2.5 text-end font-mono font-bold text-orange-700">{formatNumber(totals?.billed ?? 0)}</td>
                      <td className="px-4 py-2.5 text-end font-mono font-bold text-green-700">{formatNumber(totals?.paid ?? 0)}</td>
                      <td className="px-4 py-2.5 text-end font-mono font-bold">{formatNumber(totals?.closing ?? 0)}</td>
                    </tr>
                  </tfoot>
                </table>
              )}
            </Card>
          )}

          {/* ============ Tab: Bills ============ */}
          {tab === 'bills' && (
            <Card className="overflow-x-auto p-0">
              {filtered.bills.length === 0 ? (
                <div className="p-12 text-center text-gray-500">
                  <FilePlus className="h-12 w-12 mx-auto mb-3 text-gray-300" />
                  لا توجد فواتير في هذه الفترة.
                </div>
              ) : (
                <table className="w-full text-sm" dir="rtl">
                  <thead className="bg-orange-50 border-b border-orange-200">
                    <tr>
                      <th className="text-start px-4 py-3 font-semibold text-orange-900">التاريخ</th>
                      <th className="text-start px-4 py-3 font-semibold text-orange-900">رقم الفاتورة</th>
                      <th className="text-start px-4 py-3 font-semibold text-orange-900">الوصف</th>
                      <th className="text-end px-4 py-3 font-semibold text-orange-900">المبلغ</th>
                      <th className="text-end px-4 py-3 font-semibold text-orange-900">الرصيد بعد الفاتورة</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filtered.bills.map((b, i) => (
                      <tr key={i} className="border-b border-gray-100 hover:bg-orange-50/30">
                        <td className="px-4 py-2.5 text-gray-700 font-mono text-xs">{formatDate(b.date)}</td>
                        <td className="px-4 py-2.5 font-mono text-xs text-blue-600">{b.reference || '—'}</td>
                        <td className="px-4 py-2.5 text-gray-600 text-xs" title={b.description}>{b.description || '—'}</td>
                        <td className="px-4 py-2.5 text-end font-mono font-bold text-orange-700">{formatNumber(b.debit)}</td>
                        <td className="px-4 py-2.5 text-end font-mono text-gray-700">{formatNumber(b.runningBalance)}</td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot className="bg-orange-50 border-t-2 border-orange-300">
                    <tr>
                      <td colSpan={3} className="px-4 py-2.5 text-start text-sm font-bold text-orange-900">
                        إجمالي الفواتير ({filtered.bills.length} فاتورة)
                      </td>
                      <td className="px-4 py-2.5 text-end font-mono font-bold text-orange-700 text-base">
                        {formatNumber(totals?.billed ?? 0)}
                      </td>
                      <td></td>
                    </tr>
                  </tfoot>
                </table>
              )}
            </Card>
          )}

          {/* ============ Tab: Payments ============ */}
          {tab === 'payments' && (
            <Card className="overflow-x-auto p-0">
              {filtered.payments.length === 0 ? (
                <div className="p-12 text-center text-gray-500">
                  <HandCoins className="h-12 w-12 mx-auto mb-3 text-gray-300" />
                  لا توجد مدفوعات في هذه الفترة.
                </div>
              ) : (
                <table className="w-full text-sm" dir="rtl">
                  <thead className="bg-green-50 border-b border-green-200">
                    <tr>
                      <th className="text-start px-4 py-3 font-semibold text-green-900">التاريخ</th>
                      <th className="text-start px-4 py-3 font-semibold text-green-900">رقم السند</th>
                      <th className="text-start px-4 py-3 font-semibold text-green-900">الوصف</th>
                      <th className="text-end px-4 py-3 font-semibold text-green-900">المبلغ</th>
                      <th className="text-end px-4 py-3 font-semibold text-green-900">الرصيد بعد الدفعة</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filtered.payments.map((p, i) => (
                      <tr key={i} className="border-b border-gray-100 hover:bg-green-50/30">
                        <td className="px-4 py-2.5 text-gray-700 font-mono text-xs">{formatDate(p.date)}</td>
                        <td className="px-4 py-2.5 font-mono text-xs text-blue-600">{p.reference || '—'}</td>
                        <td className="px-4 py-2.5 text-gray-600 text-xs" title={p.description}>{p.description || '—'}</td>
                        <td className="px-4 py-2.5 text-end font-mono font-bold text-green-700">{formatNumber(p.credit)}</td>
                        <td className="px-4 py-2.5 text-end font-mono text-gray-700">{formatNumber(p.runningBalance)}</td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot className="bg-green-50 border-t-2 border-green-300">
                    <tr>
                      <td colSpan={3} className="px-4 py-2.5 text-start text-sm font-bold text-green-900">
                        إجمالي المدفوعات ({filtered.payments.length} دفعة)
                      </td>
                      <td className="px-4 py-2.5 text-end font-mono font-bold text-green-700 text-base">
                        {formatNumber(totals?.paid ?? 0)}
                      </td>
                      <td></td>
                    </tr>
                  </tfoot>
                </table>
              )}
            </Card>
          )}

          <p className="mt-3 text-xs text-gray-500">
            رصيد افتتاحي: <span className="font-mono font-semibold">{formatNumber(totals?.opening ?? 0)}</span>
            {' • '}
            رصيد ختامي: <span className="font-mono font-semibold">{formatNumber(totals?.closing ?? 0)}</span>
            {' • '}
            موجب = مستحق للمورّد (علينا دين) • سالب = لنا رصيد دائن
          </p>
        </>
      )}
    </div>
  );
}

function TabButton({ active, onClick, icon, label, count, color }: { active: boolean; onClick: () => void; icon: React.ReactNode; label: string; count?: number; color?: 'orange' | 'green' }) {
  const colorClasses = color === 'orange'
    ? 'border-orange-500 text-orange-700'
    : color === 'green'
    ? 'border-green-500 text-green-700'
    : 'border-blue-500 text-blue-700';
  return (
    <button
      onClick={onClick}
      className={`flex items-center gap-2 px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
        active
          ? colorClasses
          : 'border-transparent text-gray-500 hover:text-gray-700'
      }`}
    >
      {icon}
      <span>{label}</span>
      {count !== undefined && (
        <span className={`text-[10px] px-1.5 py-0.5 rounded-full ${active ? 'bg-gray-200' : 'bg-gray-100'}`}>
          {count}
        </span>
      )}
    </button>
  );
}
