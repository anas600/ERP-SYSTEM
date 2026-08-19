'use client';

// Sprint 56 (DEC-149 + DEC-150) — Path C.1: Top Customers + Top Items Reports
// Sprint 56: 3 tabs — أكبر العملاء / أكبر الأصناف / ملخص
// All data from sales_invoices + sales_invoice_lines (real transactional tables)

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Trophy, Calendar, RefreshCw, AlertCircle, ArrowLeft, Award, TrendingUp, Users, Package, BarChart3 } from 'lucide-react';
import { PageHeader, Card, Button } from '@/components/ui';
import { getErrorMessage } from '@/lib/api';
import { formatNumber } from '@/lib/format';

interface TopCustomerRow {
  customerId: string;
  customerCode: string;
  customerName: string;
  invoiceCount: number;
  totalSales: number;
  totalPaid: number;
  outstanding: number;
  percentOfTotal: number;
}
interface TopCustomersReport {
  from: string;
  to: string;
  top: number;
  rows: TopCustomerRow[];
  grandTotalSales: number;
  grandTotalPaid: number;
  grandOutstanding: number;
}
interface TopItemRow {
  itemId: string;
  itemSku: string;
  itemName: string;
  totalQuantity: number;
  totalSales: number;
  lineCount: number;
  percentOfTotal: number;
}
interface TopItemsReport {
  from: string;
  to: string;
  top: number;
  rows: TopItemRow[];
  grandTotalSales: number;
  grandTotalQuantity: number;
}

const API_BASE = process.env.NEXT_PUBLIC_API_BASE || '';

function firstOfYearIso(): string {
  return `${new Date().getFullYear() - 1}-01-01`;
}
function todayIso(): string { return new Date().toISOString().slice(0, 10); }

export default function TopCustomersPage() {
  const router = useRouter();
  const [tab, setTab] = useState<'customers' | 'items'>('customers');
  const [customers, setCustomers] = useState<TopCustomersReport | null>(null);
  const [items, setItems] = useState<TopItemsReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [from, setFrom] = useState<string>(firstOfYearIso());
  const [to, setTo] = useState<string>(todayIso());
  const [top, setTop] = useState<number>(10);

  const load = async (f?: string, t?: string, tp?: number) => {
    setLoading(true); setError(null);
    const fromQ = f || from, toQ = t || to, topQ = tp || top;
    try {
      // Sprint 56: نفس مفاتيح localStorage التي يستخدمها api.ts
      const token = typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null;
      const companyId = typeof window !== 'undefined' ? localStorage.getItem('currentCompanyId') : null;
      const headers: HeadersInit = { 'Content-Type': 'application/json' };
      if (token) (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`;
      if (companyId) (headers as Record<string, string>)['X-Company-Id'] = companyId;
      const [c, i] = await Promise.all([
        fetch(`${API_BASE}/api/ar/reports/top-customers?from=${fromQ}&to=${toQ}&top=${topQ}`, { headers, credentials: 'include' }).then(r => r.ok ? r.json() : null),
        fetch(`${API_BASE}/api/ar/reports/top-items?from=${fromQ}&to=${toQ}&top=${topQ}`, { headers, credentials: 'include' }).then(r => r.ok ? r.json() : null),
      ]);
      if (c) setCustomers(c);
      if (i) setItems(i);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل التقارير.'));
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  return (
    <div>
      <Link href="/dashboard" className="inline-flex items-center gap-1 text-sm text-ink-500 hover:text-brand-600 mb-3 transition-colors">
        <ArrowLeft className="h-4 w-4" />
        العودة للوحة التحكم
      </Link>
      <PageHeader
        title="أكبر العملاء والأصناف"
        description="Sprint 56: تقارير قائمة على sales_invoices + sales_invoice_lines (الوثائق الحقيقية)"
        actions={
          <div className="flex items-center gap-2">
            <Trophy className="h-5 w-5 text-amber-500" />
            <span className="text-xs text-gray-500">Period: {from} → {to}</span>
          </div>
        }
      />

      <Card className="mb-4 p-4">
        <div className="flex flex-wrap items-end gap-3">
          <div className="flex flex-col">
            <label className="text-xs text-gray-500 mb-1">من تاريخ</label>
            <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div className="flex flex-col">
            <label className="text-xs text-gray-500 mb-1">إلى تاريخ</label>
            <input type="date" value={to} onChange={(e) => setTo(e.target.value)} className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div className="flex flex-col">
            <label className="text-xs text-gray-500 mb-1">عدد النتائج</label>
            <input type="number" min={1} max={100} value={top} onChange={(e) => setTop(parseInt(e.target.value) || 10)} className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 w-20" />
          </div>
          <Button variant="primary" onClick={() => load()} iconLeft={<Calendar className="h-4 w-4" />}>تطبيق</Button>
          <Button variant="ghost" onClick={() => { const f = firstOfYearIso(), t = todayIso(); setFrom(f); setTo(t); load(f, t); }} iconLeft={<RefreshCw className="h-4 w-4" />}>آخر 12 شهر</Button>
        </div>
      </Card>

      {error && (
        <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg mb-4 text-sm flex items-start gap-2">
          <AlertCircle className="h-5 w-5 flex-shrink-0 mt-0.5" /><span>{error}</span>
        </div>
      )}

      {loading ? (
        <div className="text-center py-12 text-gray-500">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
          <p className="mt-3 text-sm">جاري التحميل...</p>
        </div>
      ) : (
        <>
          {/* Tabs */}
          <div className="flex items-center gap-2 mb-4 border-b border-gray-200">
            <button
              onClick={() => setTab('customers')}
              className={`px-4 py-2 text-sm font-bold flex items-center gap-1.5 border-b-2 transition-colors ${
                tab === 'customers' ? 'border-amber-500 text-amber-700' : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              <Users className="h-4 w-4" /> أكبر العملاء
            </button>
            <button
              onClick={() => setTab('items')}
              className={`px-4 py-2 text-sm font-bold flex items-center gap-1.5 border-b-2 transition-colors ${
                tab === 'items' ? 'border-amber-500 text-amber-700' : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              <Package className="h-4 w-4" /> أكبر الأصناف
            </button>
          </div>

          {tab === 'customers' && customers && (
            <CustomersTab report={customers} />
          )}
          {tab === 'items' && items && (
            <ItemsTab report={items} />
          )}
        </>
      )}
    </div>
  );
}

function CustomersTab({ report }: { report: TopCustomersReport }) {
  return (
    <>
      <Card className={`p-4 mb-4 border-r-4 border-amber-500 bg-amber-50/30`}>
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <Trophy className="h-7 w-7 text-amber-600" />
            <div>
              <p className="text-lg font-bold text-amber-900">أكبر {report.rows.length} عملاء</p>
              <p className="text-xs text-gray-500">من {report.from?.slice(0, 10)} إلى {report.to?.slice(0, 10)}</p>
            </div>
          </div>
          <div className="flex items-center gap-6 text-sm">
            <div>
              <p className="text-xs text-gray-500">إجمالي المبيعات</p>
              <p className="font-mono font-bold text-amber-700">{formatNumber(report.grandTotalSales)} LYD</p>
            </div>
            <div>
              <p className="text-xs text-gray-500">المحصل</p>
              <p className="font-mono font-bold text-emerald-700">{formatNumber(report.grandTotalPaid)} LYD</p>
            </div>
            <div>
              <p className="text-xs text-gray-500">المستحق</p>
              <p className="font-mono font-bold text-red-700">{formatNumber(report.grandOutstanding)} LYD</p>
            </div>
          </div>
        </div>
      </Card>

      <Card className="p-0 overflow-hidden">
        <div className="px-4 py-3 border-b border-gray-200 bg-gradient-to-l from-amber-50 to-white flex items-center gap-2">
          <Award className="h-4 w-4 text-amber-700" />
          <h3 className="text-sm font-bold text-amber-900">ترتيب العملاء حسب المبيعات</h3>
        </div>
        {report.rows.length === 0 ? (
          <div className="px-4 py-6 text-center text-gray-400 text-sm">لا توجد فواتير في الفترة</div>
        ) : (
          <table className="w-full text-sm" dir="rtl">
            <thead className="bg-white border-b border-gray-100">
              <tr>
                <th className="text-start px-4 py-2 font-semibold text-gray-600 text-xs w-12">#</th>
                <th className="text-start px-4 py-2 font-semibold text-gray-600 text-xs">العميل</th>
                <th className="text-end px-4 py-2 font-semibold text-gray-600 text-xs">الفواتير</th>
                <th className="text-end px-4 py-2 font-semibold text-gray-600 text-xs">إجمالي المبيعات</th>
                <th className="text-end px-4 py-2 font-semibold text-gray-600 text-xs">المحصل</th>
                <th className="text-end px-4 py-2 font-semibold text-gray-600 text-xs">المستحق</th>
                <th className="text-end px-4 py-2 font-semibold text-gray-600 text-xs w-32">النسبة</th>
              </tr>
            </thead>
            <tbody>
              {report.rows.map((r, i) => (
                <tr key={r.customerId} className="border-b border-gray-100 hover:bg-amber-50/30 transition-colors">
                  <td className="px-4 py-2 text-center">
                    {i < 3 ? (
                      <span className={`inline-flex items-center justify-center w-6 h-6 rounded-full text-[10px] font-bold text-white ${
                        i === 0 ? 'bg-amber-500' : i === 1 ? 'bg-gray-400' : 'bg-orange-700'
                      }`}>
                        {i + 1}
                      </span>
                    ) : (
                      <span className="text-gray-500 text-xs">{i + 1}</span>
                    )}
                  </td>
                  <td className="px-4 py-2">
                    <div className="flex items-center gap-2">
                      <span className="font-mono text-xs text-blue-600">{r.customerCode}</span>
                      <span className="font-bold text-gray-800">{r.customerName}</span>
                    </div>
                  </td>
                  <td className="px-4 py-2 text-end font-mono text-gray-700">{r.invoiceCount}</td>
                  <td className="px-4 py-2 text-end font-mono font-bold text-amber-700">{formatNumber(r.totalSales)}</td>
                  <td className="px-4 py-2 text-end font-mono text-emerald-700">{formatNumber(r.totalPaid)}</td>
                  <td className="px-4 py-2 text-end font-mono text-red-700">{formatNumber(r.outstanding)}</td>
                  <td className="px-4 py-2 text-end">
                    <div className="flex items-center gap-2">
                      <div className="flex-1 h-1.5 bg-gray-100 rounded-full overflow-hidden">
                        <div className="h-full bg-gradient-to-r from-amber-400 to-amber-600" style={{ width: `${Math.min(100, r.percentOfTotal)}%` }} />
                      </div>
                      <span className="font-mono text-xs font-bold text-amber-700 w-12 text-end">{r.percentOfTotal}%</span>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="bg-amber-50 font-bold">
                <td colSpan={3} className="px-4 py-2 text-start text-xs text-amber-900">الإجمالي</td>
                <td className="px-4 py-2 text-end font-mono text-amber-900">{formatNumber(report.grandTotalSales)}</td>
                <td className="px-4 py-2 text-end font-mono text-emerald-900">{formatNumber(report.grandTotalPaid)}</td>
                <td className="px-4 py-2 text-end font-mono text-red-900">{formatNumber(report.grandOutstanding)}</td>
                <td className="px-4 py-2 text-end font-mono text-amber-900">100%</td>
              </tr>
            </tfoot>
          </table>
        )}
      </Card>
    </>
  );
}

function ItemsTab({ report }: { report: TopItemsReport }) {
  return (
    <>
      <Card className={`p-4 mb-4 border-r-4 border-blue-500 bg-blue-50/30`}>
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <Package className="h-7 w-7 text-blue-600" />
            <div>
              <p className="text-lg font-bold text-blue-900">أكبر {report.rows.length} أصناف</p>
              <p className="text-xs text-gray-500">من {report.from?.slice(0, 10)} إلى {report.to?.slice(0, 10)}</p>
            </div>
          </div>
          <div className="flex items-center gap-6 text-sm">
            <div>
              <p className="text-xs text-gray-500">إجمالي المبيعات</p>
              <p className="font-mono font-bold text-blue-700">{formatNumber(report.grandTotalSales)} LYD</p>
            </div>
            <div>
              <p className="text-xs text-gray-500">الكمية المباعة</p>
              <p className="font-mono font-bold text-amber-700">{formatNumber(report.grandTotalQuantity)}</p>
            </div>
          </div>
        </div>
      </Card>

      <Card className="p-0 overflow-hidden">
        <div className="px-4 py-3 border-b border-gray-200 bg-gradient-to-l from-blue-50 to-white flex items-center gap-2">
          <BarChart3 className="h-4 w-4 text-blue-700" />
          <h3 className="text-sm font-bold text-blue-900">ترتيب الأصناف حسب المبيعات</h3>
        </div>
        {report.rows.length === 0 ? (
          <div className="px-4 py-6 text-center text-gray-400 text-sm">لا توجد أصناف مباعة في الفترة</div>
        ) : (
          <table className="w-full text-sm" dir="rtl">
            <thead className="bg-white border-b border-gray-100">
              <tr>
                <th className="text-start px-4 py-2 font-semibold text-gray-600 text-xs w-12">#</th>
                <th className="text-start px-4 py-2 font-semibold text-gray-600 text-xs">الصنف</th>
                <th className="text-end px-4 py-2 font-semibold text-gray-600 text-xs">الكمية</th>
                <th className="text-end px-4 py-2 font-semibold text-gray-600 text-xs">عدد السطور</th>
                <th className="text-end px-4 py-2 font-semibold text-gray-600 text-xs">إجمالي المبيعات</th>
                <th className="text-end px-4 py-2 font-semibold text-gray-600 text-xs w-32">النسبة</th>
              </tr>
            </thead>
            <tbody>
              {report.rows.map((r, i) => (
                <tr key={r.itemId} className="border-b border-gray-100 hover:bg-blue-50/30 transition-colors">
                  <td className="px-4 py-2 text-center">
                    {i < 3 ? (
                      <span className={`inline-flex items-center justify-center w-6 h-6 rounded-full text-[10px] font-bold text-white ${
                        i === 0 ? 'bg-blue-500' : i === 1 ? 'bg-gray-400' : 'bg-orange-700'
                      }`}>
                        {i + 1}
                      </span>
                    ) : (
                      <span className="text-gray-500 text-xs">{i + 1}</span>
                    )}
                  </td>
                  <td className="px-4 py-2">
                    <div className="flex items-center gap-2">
                      <span className="font-mono text-xs text-blue-600">{r.itemSku}</span>
                      <span className="font-bold text-gray-800">{r.itemName}</span>
                    </div>
                  </td>
                  <td className="px-4 py-2 text-end font-mono text-gray-700">{formatNumber(r.totalQuantity)}</td>
                  <td className="px-4 py-2 text-end font-mono text-gray-700">{r.lineCount}</td>
                  <td className="px-4 py-2 text-end font-mono font-bold text-blue-700">{formatNumber(r.totalSales)}</td>
                  <td className="px-4 py-2 text-end">
                    <div className="flex items-center gap-2">
                      <div className="flex-1 h-1.5 bg-gray-100 rounded-full overflow-hidden">
                        <div className="h-full bg-gradient-to-r from-blue-400 to-blue-600" style={{ width: `${Math.min(100, r.percentOfTotal)}%` }} />
                      </div>
                      <span className="font-mono text-xs font-bold text-blue-700 w-12 text-end">{r.percentOfTotal}%</span>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="bg-blue-50 font-bold">
                <td colSpan={2} className="px-4 py-2 text-start text-xs text-blue-900">الإجمالي</td>
                <td className="px-4 py-2 text-end font-mono text-blue-900">{formatNumber(report.grandTotalQuantity)}</td>
                <td className="px-4 py-2 text-end"></td>
                <td className="px-4 py-2 text-end font-mono text-blue-900">{formatNumber(report.grandTotalSales)}</td>
                <td className="px-4 py-2 text-end font-mono text-blue-900">100%</td>
              </tr>
            </tfoot>
          </table>
        )}
      </Card>
    </>
  );
}
