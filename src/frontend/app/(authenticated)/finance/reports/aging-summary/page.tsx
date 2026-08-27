'use client';

// Sprint 49 — صفحة أعمار الذمم الموحدة (AR + AP)
// AR Aging (موجود) + AP Aging (Sprint 48 DEC-133)

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Hourglass, Calendar, AlertCircle, RefreshCw, TrendingUp, TrendingDown, ArrowLeft } from 'lucide-react';
import { PageHeader, Card, Button } from '@/components/ui';
import { arApi, financeApi, projectsApi, getErrorMessage } from '@/lib/api';
import { formatNumber } from '@/lib/format';

function todayIso(): string { return new Date().toISOString().slice(0, 10); }

export default function AgingSummaryPage() {
  const router = useRouter();
  const [ar, setAr] = useState<any | null>(null);
  const [ap, setAp] = useState<any | null>(null);
  const [asOf, setAsOf] = useState<string>(todayIso());
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // Sprint 60 (DEC-191): فلاتر cost_center + project (تنطبق على AP فقط).
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

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const [arR, apR] = await Promise.all([
        arApi.aging?.(asOf).catch(() => null),
        financeApi.getAPAging(
          asOf,
          costCenterId && costCenterId.length > 0 ? costCenterId : undefined,
          projectId && projectId.length > 0 ? projectId : undefined,
        ),
      ]);
      setAr(arR);
      setAp(apR);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل أعمار الذمم.'));
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); /* eslint-disable-next-line react-hooks/exhaustive-deps */ }, []);

  return (
    <div>
      <Link href="/dashboard" className="inline-flex items-center gap-1 text-sm text-ink-500 hover:text-brand-600 mb-3 transition-colors">
        <ArrowLeft className="h-4 w-4" />
        العودة للوحة التحكم
      </Link>
      <PageHeader
        title="أعمار الذمم (AR + AP)"
        description="ملخص موحد لأعمار الذمم المدينة (AR) والمستحقة للموردين (AP) — Current / 31-60 / 61-90 / 91+ يوم"
      />

      <Card className="mb-4 p-4">
        <div className="flex flex-wrap items-end gap-3">
          <div className="flex flex-col">
            <label className="text-xs text-gray-500 mb-1">كما في تاريخ</label>
            <input type="date" value={asOf} onChange={(e) => setAsOf(e.target.value)} className="border border-gray-300 rounded-lg px-3 py-2 text-sm" />
          </div>
          {/* Sprint 60 (DEC-191): فلتر cost center (ينطبق على AP Aging فقط) */}
          <div className="flex flex-col">
            <label className="text-xs text-gray-500 mb-1">مركز التكلفة (AP)</label>
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
          {/* Sprint 60 (DEC-191): فلتر project (ينطبق على AP Aging فقط) */}
          <div className="flex flex-col">
            <label className="text-xs text-gray-500 mb-1">المشروع (AP)</label>
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
          <Button variant="primary" onClick={load} iconLeft={<Calendar className="h-4 w-4" />}>تطبيق</Button>
          <Button variant="ghost" onClick={() => { setCostCenterId(''); setProjectId(''); load(); }} iconLeft={<RefreshCw className="h-4 w-4" />}>إعادة</Button>
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
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          {/* AR — Customers (we owe us) */}
          <Card className="p-0 overflow-hidden">
            <div className="px-4 py-3 bg-emerald-50 border-b border-emerald-200">
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-bold text-emerald-800 flex items-center gap-2">
                  <TrendingUp className="h-4 w-4" /> أعمار الذمم المدينة (AR) — لنا عند العملاء
                </h3>
                {ar && (
                  <p className="text-sm font-mono font-bold text-emerald-800">
                    {formatNumber(ar.grandTotal?.total ?? ar.totalOutstanding ?? 0)} LYD
                  </p>
                )}
              </div>
            </div>
            {!ar ? (
              <div className="px-4 py-6 text-center text-gray-400 text-sm">لا توجد بيانات</div>
            ) : ar.rows?.length === 0 ? (
              <div className="px-4 py-6 text-center text-gray-400 text-sm">لا يوجد عملاء مستحقون</div>
            ) : (
              <table className="w-full text-sm" dir="rtl">
                <thead className="bg-white border-b border-gray-100">
                  <tr>
                    <th className="text-start px-3 py-2 text-xs font-semibold text-gray-600">العميل</th>
                    <th className="text-end px-3 py-2 text-xs font-semibold text-gray-600">0-30</th>
                    <th className="text-end px-3 py-2 text-xs font-semibold text-gray-600">31-60</th>
                    <th className="text-end px-3 py-2 text-xs font-semibold text-gray-600">61-90</th>
                    <th className="text-end px-3 py-2 text-xs font-semibold text-gray-600">91+</th>
                    <th className="text-end px-3 py-2 text-xs font-semibold text-gray-600">الإجمالي</th>
                  </tr>
                </thead>
                <tbody>
                  {ar.rows.slice(0, 10).map((r: any, i: number) => (
                    <tr key={r.customerId || i} className="border-b border-gray-100 hover:bg-emerald-50/30 cursor-pointer transition-colors" onClick={() => router.push(`/finance/customers/${r.customerId}/statement`)}>
                      <td className="px-3 py-2 text-xs">
                        <div className="font-mono text-blue-600">{r.customerCode}</div>
                        <div className="text-gray-700">{r.customerName}</div>
                      </td>
                      <td className="px-3 py-2 text-end font-mono text-xs">{formatNumber(r.buckets?.bucket0To30 ?? 0)}</td>
                      <td className="px-3 py-2 text-end font-mono text-xs">{formatNumber(r.buckets?.bucket31To60 ?? 0)}</td>
                      <td className="px-3 py-2 text-end font-mono text-xs">{formatNumber(r.buckets?.bucket61To90 ?? 0)}</td>
                      <td className="px-3 py-2 text-end font-mono text-xs">{formatNumber(r.buckets?.bucket120Plus ?? 0)}</td>
                      <td className="px-3 py-2 text-end font-mono font-bold text-emerald-700">{formatNumber(r.buckets?.total ?? 0)}</td>
                    </tr>
                  ))}
                </tbody>
                <tfoot className="bg-gray-50">
                  <tr>
                    <td className="px-3 py-2 text-start text-xs font-bold">الإجمالي ({ar.rows.length} عميل)</td>
                    <td className="px-3 py-2 text-end font-mono font-bold text-xs">{formatNumber(ar.grandTotal?.bucket0To30 ?? 0)}</td>
                    <td className="px-3 py-2 text-end font-mono font-bold text-xs">{formatNumber(ar.grandTotal?.bucket31To60 ?? 0)}</td>
                    <td className="px-3 py-2 text-end font-mono font-bold text-xs">{formatNumber(ar.grandTotal?.bucket61To90 ?? 0)}</td>
                    <td className="px-3 py-2 text-end font-mono font-bold text-xs">{formatNumber(ar.grandTotal?.bucket120Plus ?? 0)}</td>
                    <td className="px-3 py-2 text-end font-mono font-bold text-emerald-700">{formatNumber(ar.grandTotal?.total ?? 0)}</td>
                  </tr>
                </tfoot>
              </table>
            )}
          </Card>

          {/* AP — Vendors (we owe them) */}
          <Card className="p-0 overflow-hidden">
            <div className="px-4 py-3 bg-red-50 border-b border-red-200">
              <div className="flex items-center justify-between">
                <h3 className="text-sm font-bold text-red-800 flex items-center gap-2">
                  <TrendingDown className="h-4 w-4" /> أعمار الذمم الدائنة (AP) — علينا للموردين
                </h3>
                {ap && (
                  <p className="text-sm font-mono font-bold text-red-800">
                    {formatNumber(ap.grandTotal)} LYD
                  </p>
                )}
              </div>
            </div>
            {!ap ? (
              <div className="px-4 py-6 text-center text-gray-400 text-sm">لا توجد بيانات</div>
            ) : ap.vendors.length === 0 ? (
              <div className="px-4 py-6 text-center text-gray-400 text-sm">لا يوجد موردون مستحقون</div>
            ) : (
              <table className="w-full text-sm" dir="rtl">
                <thead className="bg-white border-b border-gray-100">
                  <tr>
                    <th className="text-start px-3 py-2 text-xs font-semibold text-gray-600">المورد</th>
                    <th className="text-end px-3 py-2 text-xs font-semibold text-gray-600">0-30</th>
                    <th className="text-end px-3 py-2 text-xs font-semibold text-gray-600">31-60</th>
                    <th className="text-end px-3 py-2 text-xs font-semibold text-gray-600">61-90</th>
                    <th className="text-end px-3 py-2 text-xs font-semibold text-gray-600">91+</th>
                    <th className="text-end px-3 py-2 text-xs font-semibold text-gray-600">الإجمالي</th>
                  </tr>
                </thead>
                <tbody>
                  {ap.vendors.slice(0, 10).map((v: any, i: number) => (
                    <tr key={v.vendorId || i} className="border-b border-gray-100 hover:bg-red-50/30 cursor-pointer transition-colors" onClick={() => router.push(`/procurement/vendors/${v.vendorId}/statement`)}>
                      <td className="px-3 py-2 text-xs">
                        <div className="font-mono text-blue-600">{v.vendorCode}</div>
                        <div className="text-gray-700">{v.vendorName}</div>
                      </td>
                      <td className="px-3 py-2 text-end font-mono text-xs">{formatNumber(v.current)}</td>
                      <td className="px-3 py-2 text-end font-mono text-xs">{formatNumber(v.days31To60)}</td>
                      <td className="px-3 py-2 text-end font-mono text-xs">{formatNumber(v.days61To90)}</td>
                      <td className="px-3 py-2 text-end font-mono text-xs">{formatNumber(v.days91Plus)}</td>
                      <td className="px-3 py-2 text-end font-mono font-bold text-red-700">{formatNumber(v.total)}</td>
                    </tr>
                  ))}
                </tbody>
                <tfoot className="bg-gray-50">
                  <tr>
                    <td className="px-3 py-2 text-start text-xs font-bold">الإجمالي ({ap.vendors.length} مورد)</td>
                    <td className="px-3 py-2 text-end font-mono font-bold text-xs">{formatNumber(ap.totalCurrent)}</td>
                    <td className="px-3 py-2 text-end font-mono font-bold text-xs">{formatNumber(ap.total31To60)}</td>
                    <td className="px-3 py-2 text-end font-mono font-bold text-xs">{formatNumber(ap.total61To90)}</td>
                    <td className="px-3 py-2 text-end font-mono font-bold text-xs">{formatNumber(ap.total91Plus)}</td>
                    <td className="px-3 py-2 text-end font-mono font-bold text-red-700">{formatNumber(ap.grandTotal)}</td>
                  </tr>
                </tfoot>
              </table>
            )}
          </Card>
        </div>
      )}
    </div>
  );
}
