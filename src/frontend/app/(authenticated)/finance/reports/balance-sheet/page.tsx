'use client';

// Sprint 49 — Sprint 48 (DEC-130): صفحة الميزانية العمومية
// Balance Sheet — الميزانية العمومية في تاريخ محدد
// Σ Assets = Σ Liab + Σ Equity

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { FileBarChart, Calendar, RefreshCw, AlertCircle, CheckCircle2, XCircle, Printer, ArrowLeft } from 'lucide-react';
import { PageHeader, Card, Button } from '@/components/ui';
import { financeApi, projectsApi, BalanceSheetReport, getErrorMessage } from '@/lib/api';
import { formatNumber } from '@/lib/format';

function todayIso(): string { return new Date().toISOString().slice(0, 10); }

export default function BalanceSheetPage() {
  const router = useRouter();
  const [report, setReport] = useState<BalanceSheetReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [asOf, setAsOf] = useState<string>(todayIso());
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
      } catch { /* الفلاتر اختيارية */ }
    })();
    return () => { cancelled = true; };
  }, []);

  const load = async (date?: string, ccId?: string, pId?: string) => {
    setLoading(true); setError(null);
    try {
      const r = await financeApi.getBalanceSheet(
        date,
        ccId && ccId.length > 0 ? ccId : undefined,
        pId && pId.length > 0 ? pId : undefined,
      );
      setReport(r);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل الميزانية العمومية.'));
    } finally { setLoading(false); }
  };

  useEffect(() => { load(asOf, costCenterId, projectId); /* eslint-disable-next-line react-hooks/exhaustive-deps */ }, []);

  const onPrint = () => window.print();

  return (
    <div>
      <Link href="/dashboard" className="inline-flex items-center gap-1 text-sm text-ink-500 hover:text-brand-600 mb-3 transition-colors">
        <ArrowLeft className="h-4 w-4" />
        العودة للوحة التحكم
      </Link>
      <PageHeader
        title="الميزانية العمومية"
        description="قائمة المركز المالي للشركة في تاريخ محدد — مجموع الأصول = مجموع الالتزامات + حقوق الملكية"
      />

      <Card className="mb-4 p-4">
        <div className="flex flex-wrap items-end gap-3">
          <div className="flex flex-col">
            <label className="text-xs text-gray-500 mb-1">كما في تاريخ</label>
            <input
              type="date" value={asOf}
              onChange={(e) => setAsOf(e.target.value)}
              className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
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
          <Button variant="primary" onClick={() => load(asOf, costCenterId, projectId)} iconLeft={<Calendar className="h-4 w-4" />}>تطبيق</Button>
          <Button variant="ghost" onClick={() => { setAsOf(todayIso()); setCostCenterId(''); setProjectId(''); load(todayIso(), '', ''); }} iconLeft={<RefreshCw className="h-4 w-4" />}>إعادة</Button>
          {report && (
            <Button variant="secondary" onClick={onPrint} iconLeft={<Printer className="h-4 w-4" />}>طباعة</Button>
          )}
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
          {/* Balance check banner */}
          <Card className={`p-4 mb-4 ${report.isBalanced ? 'border-r-4 border-green-500 bg-green-50/40' : 'border-r-4 border-danger-500 bg-red-50/40'}`}>
            <div className="flex flex-wrap items-center justify-between gap-4">
              <div className="flex items-center gap-3">
                {report.isBalanced ? <CheckCircle2 className="h-7 w-7 text-green-600" /> : <XCircle className="h-7 w-7 text-danger-600" />}
                <div>
                  <p className={`text-lg font-bold ${report.isBalanced ? 'text-green-800' : 'text-red-800'}`}>
                    {report.isBalanced ? 'الميزانية متوازنة ✓' : 'الميزانية غير متوازنة ✗'}
                  </p>
                  <p className="text-xs text-gray-500">
                    Σ الأصول = Σ الالتزامات + Σ حقوق الملكية
                    {!report.isBalanced && ` — فرق: ${formatNumber(Math.abs(report.variance))} LYD`}
                  </p>
                </div>
              </div>
              <div className="flex items-center gap-6 text-sm">
                <div>
                  <p className="text-xs text-gray-500">إجمالي الأصول</p>
                  <p className="font-mono font-bold text-blue-700">{formatNumber(report.totalAssets)}</p>
                </div>
                <div>
                  <p className="text-xs text-gray-500">إجمالي الالتزامات</p>
                  <p className="font-mono font-bold text-red-700">{formatNumber(report.totalLiabilities)}</p>
                </div>
                <div>
                  <p className="text-xs text-gray-500">إجمالي حقوق الملكية</p>
                  <p className="font-mono font-bold text-emerald-700">{formatNumber(report.totalEquity)}</p>
                </div>
              </div>
            </div>
          </Card>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
            {/* Assets */}
            <SectionCard title="الأصول (Assets)" rows={report.assets.rows} subtotal={report.totalAssets} color="blue" onRowClick={(id) => router.push(`/finance/reports/general-ledger?accountId=${id}`)} />
            {/* Liabilities */}
            <SectionCard title="الالتزامات (Liabilities)" rows={report.liabilities.rows} subtotal={report.totalLiabilities} color="red" onRowClick={(id) => router.push(`/finance/reports/general-ledger?accountId=${id}`)} />
          </div>
          <div className="mt-4">
            <SectionCard title="حقوق الملكية (Equity)" rows={report.equity.rows} subtotal={report.totalEquity} color="emerald" onRowClick={(id) => router.push(`/finance/reports/general-ledger?accountId=${id}`)} />
          </div>

          <p className="mt-3 text-xs text-gray-500">
            كما في تاريخ <span className="font-mono font-semibold">{asOf}</span> • الأصول = الالتزامات + حقوق الملكية ({formatNumber(report.totalLiabilitiesAndEquity)} LYD)
          </p>
        </>
      )}
    </div>
  );
}

function SectionCard({ title, rows, subtotal, color, onRowClick }: { title: string; rows: { accountId: string; accountCode: string; newCode?: string | null; accountName: string; section?: string | null; balance: number }[]; subtotal: number; color: 'blue' | 'red' | 'emerald'; onRowClick: (accountId: string) => void }) {
  const colorMap = { blue: 'bg-blue-50 border-blue-200', red: 'bg-red-50 border-red-200', emerald: 'bg-emerald-50 border-emerald-200' };
  const textMap = { blue: 'text-blue-800', red: 'text-red-800', emerald: 'text-emerald-800' };
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
              <th className="text-end px-4 py-2 font-semibold text-gray-600 text-xs">الرصيد</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => {
              // Sprint 52a: synthetic NET row (AccountId=00000000-...) is not a real account,
              // so it can't be drilled down to. Render it as a non-clickable summary row.
              const isSynthetic = !r.accountId || r.accountId === '00000000-0000-0000-0000-000000000000';
              return (
                <tr
                  key={r.accountId || r.accountCode}
                  className={`border-b border-gray-100 ${isSynthetic ? 'bg-amber-50/50 font-semibold' : 'hover:bg-blue-50/40 cursor-pointer'} transition-colors`}
                  onClick={isSynthetic ? undefined : () => onRowClick(r.accountId)}
                >
                  <td className={`px-4 py-2 font-mono text-xs ${isSynthetic ? 'text-amber-700' : 'text-emerald-700 font-semibold'}`}>
                    {/* Sprint 60 (DEC-191): نُفضّل الـ new_code (canonical). */}
                    {isSynthetic ? r.accountCode : (r.newCode ?? r.accountCode)}
                  </td>
                  <td className="px-4 py-2 text-gray-800">
                    {r.accountName}
                    {isSynthetic && <span className="text-[10px] text-amber-600 mr-2">(محسوب تلقائيًا — لم يُرحَّل)</span>}
                    {!isSynthetic && r.section && <span className="text-[10px] text-gray-400 ms-2">({r.section})</span>}
                  </td>
                  <td className="px-4 py-2 text-end font-mono font-bold">{formatNumber(r.balance)}</td>
                </tr>
              );
            })}
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
