'use client';

// Sprint 49 — Sprint 48 (DEC-131): صفحة قائمة الدخل
// Income Statement — قائمة الدخل لفترة محددة
// Revenue − Expenses = Net Income

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { TrendingUp, Calendar, RefreshCw, AlertCircle, TrendingDown, Printer, ArrowLeft } from 'lucide-react';
import { PageHeader, Card, Button } from '@/components/ui';
import { financeApi, projectsApi, IncomeStatementReport, getErrorMessage } from '@/lib/api';
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
  // Sprint 60 (DEC-191): فلاتر cost_center + project.
  const [costCenterId, setCostCenterId] = useState<string>('');
  const [projectId, setProjectId] = useState<string>('');
  const [costCenters, setCostCenters] = useState<{ id: string; code: string; name: string }[]>([]);
  const [projects, setProjects] = useState<{ id: string; code: string; name: string }[]>([]);

  // Sprint 60 (DEC-191): تحميل قوائم cost centers + projects للـ dropdowns.
  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [ccs, projs] = await Promise.all([
          financeApi.listCostCenters() as Promise<{ id: string; code: string; name: string }[]>,
          projectsApi.listProjects() as Promise<{ id: string; code: string; name: string }[]>,
        ]);
        if (!cancelled) {
          setCostCenters(Array.isArray(ccs) ? ccs : []);
          setProjects(Array.isArray(projs) ? projs : []);
        }
      } catch { /* الفلاتر اختيارية — نتجاهل الفشل */ }
    })();
    return () => { cancelled = true; };
  }, []);

  const load = async (f?: string, t?: string, ccId?: string, pId?: string) => {
    setLoading(true); setError(null);
    try {
      const r = await financeApi.getIncomeStatement(
        f,
        t,
        ccId && ccId.length > 0 ? ccId : undefined,
        pId && pId.length > 0 ? pId : undefined,
      );
      setReport(r);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل قائمة الدخل.'));
    } finally { setLoading(false); }
  };

  useEffect(() => { load(from, to, costCenterId, projectId); /* eslint-disable-next-line react-hooks/exhaustive-deps */ }, []);

  const onApply = () => load(from, to, costCenterId, projectId);
  const onReset = () => {
    setFrom(firstOfYearIso());
    setTo(todayIso());
    setCostCenterId('');
    setProjectId('');
    load(firstOfYearIso(), todayIso(), '', '');
  };

  return (
    <div>
      <Link href="/dashboard" className="inline-flex items-center gap-1 text-sm text-ink-500 hover:text-brand-600 mb-3 transition-colors">
        <ArrowLeft className="h-4 w-4" />
        العودة للوحة التحكم
      </Link>
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
          {/* Sprint 60 (DEC-191): فلتر cost center */}
          <div className="flex flex-col">
            <label className="text-xs text-gray-500 mb-1">مركز التكلفة</label>
            <select
              value={costCenterId}
              onChange={(e) => setCostCenterId(e.target.value)}
              className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 min-w-[160px]"
            >
              <option value="">كل المراكز</option>
              {costCenters.map((cc) => (
                <option key={cc.id} value={cc.id}>
                  {cc.code} — {cc.name}
                </option>
              ))}
            </select>
          </div>
          {/* Sprint 60 (DEC-191): فلتر project */}
          <div className="flex flex-col">
            <label className="text-xs text-gray-500 mb-1">المشروع</label>
            <select
              value={projectId}
              onChange={(e) => setProjectId(e.target.value)}
              className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 min-w-[160px]"
            >
              <option value="">كل المشاريع</option>
              {projects.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.code} — {p.name}
                </option>
              ))}
            </select>
          </div>
          <Button variant="primary" onClick={onApply} iconLeft={<Calendar className="h-4 w-4" />}>تطبيق</Button>
          <Button variant="ghost" onClick={onReset} iconLeft={<RefreshCw className="h-4 w-4" />}>السنة الحالية</Button>
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
                  <p className="text-xs text-gray-500" dir="ltr">الفترة من {new Date(report.from).toLocaleDateString('en-GB')} إلى {new Date(report.to).toLocaleDateString('en-GB')}</p>
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

          {/* Sprint 54 (DEC-143): L2 sections view — تجميع حسب Sub-class (L2) مع L4 details تحتها */}
          {(report.revenueSections.length > 0 || report.expenseSections.length > 0) && (
            <Card className="mt-4 p-0 overflow-hidden">
              <div className="px-4 py-3 border-b border-gray-200 bg-gradient-to-l from-blue-50 to-white flex items-center justify-between">
                <div>
                  <h3 className="text-sm font-bold text-blue-900">قائمة الدخل حسب التسلسل الهرمي (L2 Sub-class)</h3>
                  <p className="text-xs text-gray-500 mt-0.5">تجميع الحسابات الـ L4 (Detail) تحت آبائها L2 (Sub-class) — يوضح أي قسم من النشاط يولّد الإيرادات/أين تذهب المصروفات</p>
                </div>
              </div>
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 p-4">
                {report.revenueSections.length > 0 && (
                  <div>
                    <h4 className="text-xs font-bold text-emerald-700 mb-2 flex items-center gap-1">
                      <TrendingUp className="h-3.5 w-3.5" /> الإيرادات ({report.revenueSections.length} قسم L2)
                    </h4>
                    {report.revenueSections.map((sec) => (
                      <L2SectionCard key={sec.l2AccountId} section={sec} color="emerald" onRowClick={(id) => router.push(`/finance/reports/general-ledger?accountId=${id}`)} />
                    ))}
                  </div>
                )}
                {report.expenseSections.length > 0 && (
                  <div>
                    <h4 className="text-xs font-bold text-red-700 mb-2 flex items-center gap-1">
                      <TrendingDown className="h-3.5 w-3.5" /> المصروفات ({report.expenseSections.length} قسم L2)
                    </h4>
                    {report.expenseSections.map((sec) => (
                      <L2SectionCard key={sec.l2AccountId} section={sec} color="red" onRowClick={(id) => router.push(`/finance/reports/general-ledger?accountId=${id}`)} />
                    ))}
                  </div>
                )}
              </div>
            </Card>
          )}
        </>
      )}
    </div>
  );
}

function L2SectionCard({ section, color, onRowClick }: { section: { l2Code: string; l2Name: string; rows: { accountId: string; accountCode: string; accountName: string; newCode?: string | null; section?: string | null; amount: number }[]; subtotal: number }; color: 'emerald' | 'red'; onRowClick: (accountId: string) => void }) {
  const headerMap = { emerald: 'bg-emerald-50/60 border-emerald-200 text-emerald-900', red: 'bg-red-50/60 border-red-200 text-red-900' };
  const badgeMap = { emerald: 'bg-emerald-600 text-white', red: 'bg-red-600 text-white' };
  return (
    <div className="mb-2 border border-gray-200 rounded-lg overflow-hidden">
      <div className={`px-3 py-2 border-b ${headerMap[color]} flex items-center justify-between`}>
        <div className="flex items-center gap-2">
          <span className={`inline-block px-2 py-0.5 text-[10px] font-mono font-bold rounded ${badgeMap[color]}`}>L2</span>
          <span className="font-mono text-xs text-gray-500">{section.l2Code}</span>
          <span className="text-sm font-bold">{section.l2Name}</span>
          <span className="text-xs text-gray-500">({section.rows.length} حساب)</span>
        </div>
        <span className="font-mono text-sm font-bold">{formatNumber(section.subtotal)} LYD</span>
      </div>
      <table className="w-full text-sm" dir="rtl">
        <tbody>
          {section.rows.map((r) => (
            <tr key={r.accountId} className="border-b border-gray-100 hover:bg-blue-50/40 cursor-pointer transition-colors" onClick={() => onRowClick(r.accountId)}>
              <td className="px-3 py-1.5 w-32">
                <div className="flex items-center gap-1.5">
                  <span className="inline-block w-3 h-px bg-gray-300"></span>
                  <span className="inline-block px-1.5 py-0.5 text-[9px] font-mono font-bold rounded bg-gray-200 text-gray-600">L4</span>
                  {/* Sprint 60 (DEC-191): نُفضّل الـ new_code (canonical) للعرض. */}
                  <span className="font-mono text-xs text-emerald-700 font-semibold">{r.newCode ?? r.accountCode}</span>
                </div>
              </td>
              <td className="px-3 py-1.5 text-gray-800">
                {r.accountName}
                {r.section && (
                  <span className="ms-2 text-[10px] text-gray-400">({r.section})</span>
                )}
              </td>
              <td className="px-3 py-1.5 text-end font-mono">{formatNumber(r.amount)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function SectionCard({ title, rows, subtotal, color, onRowClick }: { title: string; rows: { accountId: string; accountCode: string; accountName: string; newCode?: string | null; section?: string | null; amount: number }[]; subtotal: number; color: 'emerald' | 'red'; onRowClick: (accountId: string) => void }) {
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
                <td className="px-4 py-2 font-mono text-xs">
                  {/* Sprint 60 (DEC-191): نُفضّل الـ new_code (canonical). */}
                  <span className="text-emerald-700 font-semibold">{r.newCode ?? r.accountCode}</span>
                </td>
                <td className="px-4 py-2 text-gray-800">
                  {r.accountName}
                  {r.section && (
                    <span className="ms-2 text-[10px] text-gray-400">({r.section})</span>
                  )}
                </td>
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
