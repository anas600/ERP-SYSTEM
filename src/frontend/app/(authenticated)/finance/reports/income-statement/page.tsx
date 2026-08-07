'use client';

// Sprint 49 — Sprint 48 (DEC-131): صفحة قائمة الدخل
// Income Statement — قائمة الدخل لفترة محددة
// Revenue − Expenses = Net Income

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { TrendingUp, Calendar, RefreshCw, AlertCircle, TrendingDown, Printer } from 'lucide-react';
import { PageHeader, Card, Button } from '@/components/ui';
import { financeApi, IncomeStatementReport, getErrorMessage } from '@/lib/api';
import { formatNumber } from '@/lib/format';

function firstOfYearIso(): string {
  const d = new Date();
  return `${d.getFullYear()}-01-01`;
}
function todayIso(): string { return new Date().toISOString().slice(0, 10); }

export default function IncomeStatementPage() {
  const router = useRouter();
  const [report, setReport] = useState<IncomeStatementReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [from, setFrom] = useState<string>(firstOfYearIso());
  const [to, setTo] = useState<string>(todayIso());

  const load = async (f?: string, t?: string) => {
    setLoading(true); setError(null);
    try {
      const r = await financeApi.getIncomeStatement(f, t);
      setReport(r);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل قائمة الدخل.'));
    } finally { setLoading(false); }
  };

  useEffect(() => { load(from, to); }, []);

  return (
    <div>
      <PageHeader
        title="قائمة الدخل"
        description="قائمة الأرباح والخسائر لفترة محددة — الإيرادات − المصروفات = صافي الربح"
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
          <Button variant="primary" onClick={() => load(from, to)} iconLeft={<Calendar className="h-4 w-4" />}>تطبيق</Button>
          <Button variant="ghost" onClick={() => { setFrom(firstOfYearIso()); setTo(todayIso()); load(firstOfYearIso(), todayIso()); }} iconLeft={<RefreshCw className="h-4 w-4" />}>السنة الحالية</Button>
          {report && <Button variant="secondary" onClick={() => window.print()} iconLeft={<Printer className="h-4 w-4" />}>طباعة</Button>}
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
      ) : !report ? null : (
        <>
          <Card className={`p-4 mb-4 border-r-4 ${report.isProfitable ? 'border-green-500 bg-green-50/40' : 'border-danger-500 bg-red-50/40'}`}>
            <div className="flex flex-wrap items-center justify-between gap-4">
              <div className="flex items-center gap-3">
                {report.isProfitable ? <TrendingUp className="h-7 w-7 text-green-600" /> : <TrendingDown className="h-7 w-7 text-danger-600" />}
                <div>
                  <p className={`text-lg font-bold ${report.isProfitable ? 'text-green-800' : 'text-red-800'}`}>
                    {report.isProfitable ? 'ربح' : 'خسارة'} — صافي {formatNumber(Math.abs(report.netIncome))} LYD
                  </p>
                  <p className="text-xs text-gray-500">الفترة من {report.from} إلى {report.to}</p>
                </div>
              </div>
              <div className="flex items-center gap-6 text-sm">
                <div>
                  <p className="text-xs text-gray-500">إجمالي الإيرادات</p>
                  <p className="font-mono font-bold text-emerald-700">{formatNumber(report.totalRevenue)}</p>
                </div>
                <div>
                  <p className="text-xs text-gray-500">إجمالي المصروفات</p>
                  <p className="font-mono font-bold text-red-700">{formatNumber(report.totalExpenses)}</p>
                </div>
                <div>
                  <p className="text-xs text-gray-500">صافي الدخل</p>
                  <p className={`font-mono font-bold ${report.isProfitable ? 'text-green-700' : 'text-red-700'}`}>{formatNumber(report.netIncome)}</p>
                </div>
              </div>
            </div>
          </Card>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
            <SectionCard title="الإيرادات (Revenue)" rows={report.revenue.rows} subtotal={report.totalRevenue} color="emerald" onRowClick={(id) => router.push(`/finance/reports/general-ledger?accountId=${id}`)} />
            <SectionCard title="المصروفات (Expenses)" rows={report.expenses.rows} subtotal={report.totalExpenses} color="red" onRowClick={(id) => router.push(`/finance/reports/general-ledger?accountId=${id}`)} />
          </div>
        </>
      )}
    </div>
  );
}

function SectionCard({ title, rows, subtotal, color, onRowClick }: { title: string; rows: { accountId: string; accountCode: string; accountName: string; amount: number }[]; subtotal: number; color: 'emerald' | 'red'; onRowClick: (accountId: string) => void }) {
  const colorMap = { emerald: 'bg-emerald-50 border-emerald-200', red: 'bg-red-50 border-red-200' };
  const textMap = { emerald: 'text-emerald-800', red: 'text-red-800' };
  return (
    <Card className="p-0 overflow-hidden">
      <div className={`px-4 py-3 border-b ${colorMap[color]} flex items-center justify-between`}>
        <h3 className={`text-sm font-bold ${textMap[color]}`}>{title} <span className="text-xs text-gray-500 font-normal">({rows.length} حساب)</span></h3>
        <p className={`text-sm font-mono font-bold ${textMap[color]}`}>{formatNumber(subtotal)} LYD</p>
      </div>
      {rows.length === 0 ? (
        <div className="px-4 py-6 text-center text-gray-400 text-sm">لا توجد حسابات</div>
      ) : (
        <table className="w-full text-sm" dir="rtl">
          <thead className="bg-white border-b border-gray-100">
            <tr>
              <th className="text-start px-4 py-2 font-semibold text-gray-600 text-xs">الكود</th>
              <th className="text-start px-4 py-2 font-semibold text-gray-600 text-xs">اسم الحساب</th>
              <th className="text-end px-4 py-2 font-semibold text-gray-600 text-xs">المبلغ</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.accountId} className="border-b border-gray-100 hover:bg-blue-50/40 cursor-pointer transition-colors" onClick={() => onRowClick(r.accountId)}>
                <td className="px-4 py-2 font-mono text-xs text-blue-600">{r.accountCode}</td>
                <td className="px-4 py-2 text-gray-800">{r.accountName}</td>
                <td className="px-4 py-2 text-end font-mono font-bold">{formatNumber(r.amount)}</td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr className="bg-gray-50">
              <td colSpan={2} className="px-4 py-2 text-start text-xs font-bold text-gray-700">إجمالي {title.split(' ')[0]}</td>
              <td className={`px-4 py-2 text-end font-mono font-bold ${textMap[color]}`}>{formatNumber(subtotal)}</td>
            </tr>
          </tfoot>
        </table>
      )}
    </Card>
  );
}
