'use client';

// Sprint 54 (DEC-142) — ميزان المراجعة الهرمي
// Trial Balance (v2) — يعرض L4 (Detail) مع L3 (Control) parent، يدعم الـ drill-down
// كل صف = حساب L4 postable. الـ L3 يظهر كـ section header.

import { useEffect, useState, useMemo } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Scale, RefreshCw, Calendar, AlertCircle, CheckCircle2, XCircle, ArrowLeft, Layers, Wallet } from 'lucide-react';
import { PageHeader, Card, Button } from '@/components/ui';
import { financeApi, projectsApi, TrialBalanceV2Report, TrialBalanceV2Row, getErrorMessage } from '@/lib/api';
import { formatNumber } from '@/lib/format';

function todayIso(): string { return new Date().toISOString().slice(0, 10); }

export default function TrialBalanceV2Page() {
  const router = useRouter();
  const [report, setReport] = useState<TrialBalanceV2Report | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [asOf, setAsOf] = useState<string>(todayIso());
  // Sprint 60 (DEC-191): فلاتر cost_center + project.
  const [costCenterId, setCostCenterId] = useState<string>('');
  const [projectId, setProjectId] = useState<string>('');
  const [costCenters, setCostCenters] = useState<{ id: string; code: string; name: string }[]>([]);
  const [projects, setProjects] = useState<{ id: string; code: string; name: string }[]>([]);

  // Sprint 60 (DEC-191): تحميل قوائم cost centers + projects.
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
    setLoading(true);
    setError(null);
    try {
      const r = await financeApi.getTrialBalanceV2(
        date,
        ccId && ccId.length > 0 ? ccId : undefined,
        pId && pId.length > 0 ? pId : undefined,
      );
      setReport(r);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل ميزان المراجعة.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load(asOf, costCenterId, projectId);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Group rows by L3 parent (parentCode)
  // null parentCode = row IS L3 (or L2 standalone)
  const grouped = useMemo(() => {
    const groups = new Map<string, { l3: { code: string; name: string; id: string } | null; rows: TrialBalanceV2Row[]; subtotalDr: number; subtotalCr: number }>();
    if (!report) return groups;
    for (const r of report.rows) {
      const key = r.parentCode || `L${r.level}-${r.accountCode}`;
      if (!groups.has(key)) {
        groups.set(key, {
          l3: r.parentCode ? { code: r.parentCode, name: r.parentName || '', id: r.parentAccountId || '' } : null,
          rows: [],
          subtotalDr: 0,
          subtotalCr: 0,
        });
      }
      const g = groups.get(key)!;
      g.rows.push(r);
      g.subtotalDr += r.debit;
      g.subtotalCr += r.credit;
    }
    return groups;
  }, [report]);

  return (
    <div>
      <Link href="/dashboard" className="inline-flex items-center gap-1 text-sm text-ink-500 hover:text-brand-600 mb-3 transition-colors">
        <ArrowLeft className="h-4 w-4" />
        العودة للوحة التحكم
      </Link>
      <PageHeader
        title="ميزان المراجعة الهرمي"
        description="Sprint 54 (DEC-142): الحسابات الـ L4 (Detail) مع L3 (Control) parent — يدعم الـ drill-down إلى الأستاذ العام"
        actions={
          <div className="flex items-center gap-2">
            <Link href="/finance/trial-balance">
              <Button variant="ghost" size="sm" iconLeft={<Scale className="h-4 w-4" />}>
                النسخة القديمة
              </Button>
            </Link>
            <Link href="/finance/accounts">
              <Button variant="secondary" iconLeft={<Wallet className="h-4 w-4" />}>
                دليل الحسابات
              </Button>
            </Link>
          </div>
        }
      />

      <Card className="mb-4 p-4">
        <div className="flex flex-wrap items-end gap-3">
          <div className="flex flex-col">
            <label className="text-xs text-gray-500 mb-1">في تاريخ</label>
            <input
              type="date"
              value={asOf}
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
                <option key={cc.id} value={cc.id}>{cc.code} — {cc.name}</option>
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
                <option key={p.id} value={p.id}>{p.code} — {p.name}</option>
              ))}
            </select>
          </div>
          <Button variant="primary" onClick={() => load(asOf, costCenterId, projectId)} iconLeft={<Calendar className="h-4 w-4" />}>تطبيق</Button>
          <Button variant="ghost" onClick={() => { setAsOf(todayIso()); setCostCenterId(''); setProjectId(''); load(todayIso(), '', ''); }} iconLeft={<RefreshCw className="h-4 w-4" />}>اليوم</Button>
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
          {/* Sprint 54: ملخص — إجماليات + حالة التوازن */}
          <Card className={`p-4 mb-4 border-r-4 ${report.isBalanced ? 'border-green-500 bg-green-50/40' : 'border-danger-500 bg-red-50/40'}`}>
            <div className="flex flex-wrap items-center justify-between gap-4">
              <div className="flex items-center gap-3">
                {report.isBalanced ? <CheckCircle2 className="h-7 w-7 text-green-600" /> : <XCircle className="h-7 w-7 text-danger-600" />}
                <div>
                  <p className={`text-lg font-bold ${report.isBalanced ? 'text-green-800' : 'text-red-800'}`}>
                    {report.isBalanced ? 'ميزان مراجَع ومتوازن' : 'الميزان غير متوازن!'}
                  </p>
                  <p className="text-xs text-gray-500">في تاريخ {report.asOfDate?.slice(0, 10)}</p>
                </div>
              </div>
              <div className="flex items-center gap-6 text-sm">
                <div>
                  <p className="text-xs text-gray-500">إجمالي المدين</p>
                  <p className="font-mono font-bold text-blue-700">{formatNumber(report.totalDebit)}</p>
                </div>
                <div>
                  <p className="text-xs text-gray-500">إجمالي الدائن</p>
                  <p className="font-mono font-bold text-orange-700">{formatNumber(report.totalCredit)}</p>
                </div>
                <div>
                  <p className="text-xs text-gray-500">الفرق</p>
                  <p className={`font-mono font-bold ${Math.abs(report.variance) < 0.01 ? 'text-green-700' : 'text-red-700'}`}>{formatNumber(report.variance)}</p>
                </div>
                <div>
                  <p className="text-xs text-gray-500">عدد الحسابات</p>
                  <p className="font-mono font-bold text-gray-700">{report.rows.length}</p>
                </div>
              </div>
            </div>
          </Card>

          {/* L3 Grouped View — كل L3 control يظهر كـ section header مع L4 details تحته */}
          <Card className="p-0 overflow-hidden">
            <div className="px-4 py-3 border-b border-gray-200 bg-gradient-to-l from-amber-50 to-white flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Layers className="h-4 w-4 text-amber-700" />
                <h3 className="text-sm font-bold text-amber-900">الحسابات مجمّعة حسب L3 (Control Account)</h3>
              </div>
              <span className="text-xs text-gray-500">{grouped.size} مجموعة</span>
            </div>
            {Array.from(grouped.entries()).map(([key, group], idx) => (
              <div key={key} className={idx > 0 ? 'border-t border-gray-200' : ''}>
                {/* L3 section header */}
                <div className="px-4 py-2 bg-amber-50/40 border-b border-amber-100 flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    {group.l3 ? (
                      <>
                        <span className="inline-block px-1.5 py-0.5 text-[9px] font-mono font-bold rounded bg-amber-600 text-white">L3</span>
                        <span className="font-mono text-xs text-gray-700">{group.l3.code}</span>
                        <span className="text-sm font-bold text-amber-900">{group.l3.name}</span>
                      </>
                    ) : (
                      <>
                        <span className={`inline-block px-1.5 py-0.5 text-[9px] font-mono font-bold rounded ${group.rows[0]?.level === 2 ? 'bg-blue-600 text-white' : 'bg-gray-600 text-white'}`}>L{group.rows[0]?.level || '?'}</span>
                        <span className="font-mono text-xs text-gray-700">{group.rows[0]?.accountCode}</span>
                        <span className="text-sm font-bold text-gray-700">{group.rows[0]?.accountName}</span>
                        <span className="text-xs text-gray-500">(حساب بدون L3 parent — مستوى {group.rows[0]?.level})</span>
                      </>
                    )}
                  </div>
                  <div className="flex items-center gap-3 text-xs">
                    <span className="text-gray-500">مدين: <span className="font-mono font-bold text-blue-700">{formatNumber(group.subtotalDr)}</span></span>
                    <span className="text-gray-500">دائن: <span className="font-mono font-bold text-orange-700">{formatNumber(group.subtotalCr)}</span></span>
                  </div>
                </div>
                <table className="w-full text-sm" dir="rtl">
                  <tbody>
                    {group.rows.map((r) => (
                      <tr
                        key={r.accountId}
                        className="border-b border-gray-100 last:border-b-0 hover:bg-blue-50/40 cursor-pointer transition-colors"
                        onClick={() => router.push(`/finance/reports/general-ledger?accountId=${r.accountId}`)}
                      >
                        <td className="px-4 py-2 w-32">
                          <div className="flex items-center gap-1.5">
                            <span className="inline-block w-3 h-px bg-gray-300"></span>
                            <span className="inline-block px-1.5 py-0.5 text-[9px] font-mono font-bold rounded bg-gray-200 text-gray-600">L{r.level}</span>
                            {/* Sprint 60 (DEC-191): نُفضّل الـ new_code (canonical). */}
                            <span className="font-mono text-xs text-emerald-700 font-semibold">{r.newCode ?? r.accountCode}</span>
                          </div>
                        </td>
                        <td className="px-4 py-2 text-gray-800">
                          {r.accountName}
                          {r.section && <span className="ms-2 text-[10px] text-gray-400">({r.section})</span>}
                        </td>
                        <td className="px-4 py-2 text-end font-mono text-blue-700">{r.debit > 0 ? formatNumber(r.debit) : '—'}</td>
                        <td className="px-4 py-2 text-end font-mono text-orange-700">{r.credit > 0 ? formatNumber(r.credit) : '—'}</td>
                        <td className="px-4 py-2 text-end font-mono font-bold">{formatNumber(r.net)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ))}
          </Card>
        </>
      )}
    </div>
  );
}
