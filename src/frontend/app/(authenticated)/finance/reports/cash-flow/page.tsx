'use client';

// Sprint 49 — Sprint 48 (DEC-132): صفحة التدفقات النقدية
// Cash Flow (Indirect Method) — قائمة التدفقات النقدية لفترة محددة
// Operating + Investing + Financing = Net Change in Cash

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { Droplet, Calendar, RefreshCw, AlertCircle, Printer, ArrowUp, ArrowDown, ArrowLeft } from 'lucide-react';
import { PageHeader, Card, Button } from '@/components/ui';
import { financeApi, CashFlowReport, getErrorMessage } from '@/lib/api';
import { formatNumber } from '@/lib/format';

function firstOfYearIso(): string {
  const d = new Date();
  return `${d.getFullYear()}-01-01`;
}
function todayIso(): string { return new Date().toISOString().slice(0, 10); }

export default function CashFlowPage() {
  const [report, setReport] = useState<CashFlowReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [from, setFrom] = useState<string>(firstOfYearIso());
  const [to, setTo] = useState<string>(todayIso());

  const load = async (f?: string, t?: string) => {
    setLoading(true); setError(null);
    try {
      const r = await financeApi.getCashFlow(f, t);
      setReport(r);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل التدفقات النقدية.'));
    } finally { setLoading(false); }
  };

  useEffect(() => { load(from, to); }, []);

  return (
    <div>
      <Link href="/dashboard" className="inline-flex items-center gap-1 text-sm text-ink-500 hover:text-brand-600 mb-3 transition-colors">
        <ArrowLeft className="h-4 w-4" />
        العودة للوحة التحكم
      </Link>
      <PageHeader
        title="التدفقات النقدية"
        description="قائمة التدفقات النقدية (الطريقة غير المباشرة) لفترة محددة — تشغيلي + استثماري + تمويلي = صافي التغير في النقد"
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
          <Card className={`p-4 mb-4 border-r-4 ${report.netChangeInCash >= 0 ? 'border-emerald-500 bg-emerald-50/40' : 'border-danger-500 bg-red-50/40'}`}>
            <div className="flex flex-wrap items-center justify-between gap-4">
              <div className="flex items-center gap-3">
                {report.netChangeInCash >= 0 ? <ArrowUp className="h-7 w-7 text-emerald-600" /> : <ArrowDown className="h-7 w-7 text-danger-600" />}
                <div>
                  <p className={`text-lg font-bold ${report.netChangeInCash >= 0 ? 'text-emerald-800' : 'text-red-800'}`}>
                    صافي التغير في النقد: {formatNumber(report.netChangeInCash)} LYD
                  </p>
                  <p className="text-xs text-gray-500" dir="ltr">الفترة من {new Date(report.from).toLocaleDateString('en-GB')} إلى {new Date(report.to).toLocaleDateString('en-GB')}</p>
                </div>
              </div>
              <div className="flex items-center gap-6 text-sm">
                <div>
                  <p className="text-xs text-gray-500">تشغيلي</p>
                  <p className={`font-mono font-bold ${report.netOperatingCash >= 0 ? 'text-emerald-700' : 'text-red-700'}`}>{formatNumber(report.netOperatingCash)}</p>
                </div>
                <div>
                  <p className="text-xs text-gray-500">استثماري</p>
                  <p className={`font-mono font-bold ${report.netInvestingCash >= 0 ? 'text-emerald-700' : 'text-red-700'}`}>{formatNumber(report.netInvestingCash)}</p>
                </div>
                <div>
                  <p className="text-xs text-gray-500">تمويلي</p>
                  <p className={`font-mono font-bold ${report.netFinancingCash >= 0 ? 'text-emerald-700' : 'text-red-700'}`}>{formatNumber(report.netFinancingCash)}</p>
                </div>
              </div>
            </div>
          </Card>

          <div className="space-y-4">
            <CFSection title="الأنشطة التشغيلية (Operating)" lines={report.operating.lines} subtotal={report.netOperatingCash} color="blue" />
            <CFSection title="الأنشطة الاستثمارية (Investing)" lines={report.investing.lines} subtotal={report.netInvestingCash} color="amber" />
            <CFSection title="أنشطة التمويل (Financing)" lines={report.financing.lines} subtotal={report.netFinancingCash} color="purple" />
          </div>
        </>
      )}
    </div>
  );
}

function CFSection({ title, lines, subtotal, color }: { title: string; lines: { description: string; amount: number; accountCode?: string; accountName?: string }[]; subtotal: number; color: 'blue' | 'amber' | 'purple' }) {
  const colorMap = { blue: 'bg-blue-50 border-blue-200', amber: 'bg-amber-50 border-amber-200', purple: 'bg-purple-50 border-purple-200' };
  const textMap = { blue: 'text-blue-800', amber: 'text-amber-800', purple: 'text-purple-800' };
  return (
    <Card className="p-0 overflow-hidden">
      <div className={`px-4 py-3 border-b ${colorMap[color]} flex items-center justify-between`}>
        <h3 className={`text-sm font-bold ${textMap[color]}`}>{title} <span className="text-xs text-gray-500 font-normal">({lines.length} بند)</span></h3>
        <p className={`text-sm font-mono font-bold ${textMap[color]}`}>{formatNumber(subtotal)} LYD</p>
      </div>
      {lines.length === 0 ? (
        <div className="px-4 py-6 text-center text-gray-400 text-sm">لا توجد حركات</div>
      ) : (
        <table className="w-full text-sm" dir="rtl">
          <thead className="bg-white border-b border-gray-100">
            <tr>
              <th className="text-start px-4 py-2 font-semibold text-gray-600 text-xs">البيان</th>
              <th className="text-start px-4 py-2 font-semibold text-gray-600 text-xs">Sprint 54: حساب L3</th>
              <th className="text-end px-4 py-2 font-semibold text-gray-600 text-xs">المبلغ</th>
            </tr>
          </thead>
          <tbody>
            {lines.map((l, i) => (
              <tr key={i} className="border-b border-gray-100 hover:bg-gray-50">
                <td className="px-4 py-2 text-gray-800">{l.description}</td>
                <td className="px-4 py-2 text-xs">
                  {l.accountCode ? (
                    <span className="inline-flex items-center gap-1">
                      <span className="px-1.5 py-0.5 text-[9px] font-mono font-bold rounded bg-amber-100 text-amber-800">L3</span>
                      <span className="font-mono text-gray-600">{l.accountCode}</span>
                      <span className="text-gray-500">{l.accountName}</span>
                    </span>
                  ) : (
                    <span className="text-gray-400 text-xs">—</span>
                  )}
                </td>
                <td className={`px-4 py-2 text-end font-mono font-bold ${l.amount >= 0 ? 'text-emerald-700' : 'text-red-700'}`}>{formatNumber(l.amount)}</td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr className="bg-gray-50">
              <td colSpan={2} className="px-4 py-2 text-start text-xs font-bold text-gray-700">صافي {title}</td>
              <td className={`px-4 py-2 text-end font-mono font-bold ${textMap[color]}`}>{formatNumber(subtotal)}</td>
            </tr>
          </tfoot>
        </table>
      )}
    </Card>
  );
}
