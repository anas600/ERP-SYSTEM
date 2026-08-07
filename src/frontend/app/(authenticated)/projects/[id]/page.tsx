'use client';

import { useEffect, useState, useMemo } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight, FileText, RefreshCw, TrendingUp, TrendingDown, Calendar, BarChart3 } from 'lucide-react';
import { PageHeader, Card, Button, Input } from '@/components/ui';
import { api, getErrorMessage, projectsApi, Project, ProjectPnL } from '@/lib/api';
import { formatDate, formatCurrency } from '@/lib/utils';

type Tab = 'details' | 'pnl';

export default function ProjectsIdPage() {
  const params = useParams<{ id: string }>();
  const id = params.id;

  const [item, setItem] = useState<Project | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tab, setTab] = useState<Tab>('details');

  useEffect(() => { load(); }, [id]);

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const data = await projectsApi.getProject(id);
      setItem(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل البيانات.'));
    } finally { setLoading(false); }
  };

  if (loading) {
    return (
      <div className="text-center py-12 text-gray-500">
        <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
        <p className="mt-3 text-sm">جاري التحميل...</p>
      </div>
    );
  }

  return (
    <div>
      <PageHeader
        title={item ? `${item.code} — ${item.name}` : 'مشروع'}
        description={item?.description || 'بيانات المشروع + الأرباح والخسائر'}
        actions={
          <Link href="/projects">
            <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />}>العودة إلى المشاريع</Button>
          </Link>
        }
      />

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
          {error}
        </div>
      )}

      {!item ? (
        <Card className="p-12 text-center text-gray-500">
          <FileText className="h-12 w-12 mx-auto mb-3 text-gray-300" />
          لم يتم العثور على السجل.
        </Card>
      ) : (
        <>
          {/* Tab nav (Sprint 57) */}
          <div className="flex gap-2 mb-4 border-b border-gray-200">
            <button
              type="button"
              onClick={() => setTab('details')}
              className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px ${
                tab === 'details'
                  ? 'border-blue-600 text-blue-700'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              التفاصيل
            </button>
            <button
              type="button"
              onClick={() => setTab('pnl')}
              className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px flex items-center gap-1 ${
                tab === 'pnl'
                  ? 'border-blue-600 text-blue-700'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              <BarChart3 className="h-4 w-4" />
              الأرباح والخسائر
            </button>
          </div>

          {tab === 'details' ? (
            <DetailsTab item={item} onReload={load} />
          ) : (
            <PnLTab projectId={id} />
          )}
        </>
      )}
    </div>
  );
}

function DetailsTab({ item, onReload }: { item: Project; onReload: () => void }) {
  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
      <Card className="p-6">
        <h3 className="text-lg font-bold text-gray-800 mb-4">المعلومات الأساسية</h3>
        <dl className="space-y-3">
          <Row label="code" value={item.code} />
          <Row label="name" value={item.name} />
          <Row label="status" value={String(item.status)} />
          <Row label="budget" value={String(item.budget)} />
          <Row label="startDate" value={item.startDate} />
          <Row label="endDate" value={item.endDate} />
          <Row label="createdAt" value={item.createdAt} />
          <Row label="isActive" value={String(item.isActive)} />
        </dl>
      </Card>

      <Card className="p-6">
        <h3 className="text-lg font-bold text-gray-800 mb-4">الإجراءات</h3>
        <div className="space-y-2">
          <Button variant="primary" onClick={onReload} iconLeft={<RefreshCw className="h-4 w-4" />} className="w-full">
            إعادة تحميل
          </Button>
          <Link href="/projects">
            <Button variant="secondary" className="w-full">العودة للقائمة</Button>
          </Link>
        </div>
      </Card>
    </div>
  );
}

function Row({ label, value }: { label: string; value?: string }) {
  return (
    <div className="flex justify-between text-sm gap-2">
      <dt className="text-gray-500 flex-shrink-0">{label}</dt>
      <dd className="font-medium text-gray-800 font-mono text-xs text-end break-all">
        {value && value.length > 0 ? value : '—'}
      </dd>
    </div>
  );
}

// ===== Sprint 57 / DEC-161: P&L Tab =====

function PnLTab({ projectId }: { projectId: string }) {
  const today = new Date().toISOString().slice(0, 10);
  const yearStart = `${new Date().getFullYear() - 1}-01-01`;

  const [from, setFrom] = useState<string>(yearStart);
  const [to, setTo] = useState<string>(today);
  const [pnl, setPnl] = useState<ProjectPnL | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await projectsApi.getProjectPnL(projectId, from || undefined, to || undefined);
      setPnl(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل الأرباح والخسائر.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [projectId]);

  const isProfit = useMemo(() => (pnl?.grossProfit ?? 0) >= 0, [pnl]);

  return (
    <div>
      {/* Date range filter */}
      <Card className="p-4 mb-4">
        <div className="flex flex-col sm:flex-row sm:items-end gap-3">
          <div className="flex-1">
            <label className="block text-xs text-gray-600 mb-1 flex items-center gap-1">
              <Calendar className="h-3 w-3" /> من تاريخ
            </label>
            <Input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
          </div>
          <div className="flex-1">
            <label className="block text-xs text-gray-600 mb-1 flex items-center gap-1">
              <Calendar className="h-3 w-3" /> إلى تاريخ
            </label>
            <Input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
          </div>
          <Button variant="primary" onClick={load} disabled={loading}>
            {loading ? 'جاري التحميل…' : 'تحديث'}
          </Button>
        </div>
      </Card>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
          {error}
        </div>
      )}

      {loading && !pnl ? (
        <Card className="p-12 text-center text-gray-500">جاري التحميل…</Card>
      ) : !pnl ? null : (
        <>
          {/* Summary cards */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-4">
            <Card className="p-4">
              <div className="flex items-center gap-2 text-xs text-gray-500 mb-1">
                <TrendingUp className="h-4 w-4" /> الإيرادات
              </div>
              <div className="text-2xl font-bold text-green-700">{formatCurrency(pnl.totalRevenue)}</div>
              <div className="text-xs text-gray-500 mt-1">{pnl.invoiceCount} فاتورة</div>
            </Card>

            <Card className="p-4">
              <div className="flex items-center gap-2 text-xs text-gray-500 mb-1">
                <TrendingDown className="h-4 w-4" /> التكاليف
              </div>
              <div className="text-2xl font-bold text-red-700">{formatCurrency(pnl.totalCosts)}</div>
              <div className="text-xs text-gray-500 mt-1">{pnl.costEntryCount} قيد</div>
            </Card>

            <Card className="p-4">
              <div className="text-xs text-gray-500 mb-1">صافي الربح</div>
              <div className={`text-2xl font-bold ${isProfit ? 'text-green-700' : 'text-red-700'}`}>
                {formatCurrency(pnl.grossProfit)}
              </div>
              <div className="text-xs text-gray-500 mt-1">Revenue − Costs</div>
            </Card>

            <Card className="p-4">
              <div className="text-xs text-gray-500 mb-1">هامش الربح</div>
              <div className={`text-2xl font-bold ${isProfit ? 'text-green-700' : 'text-red-700'}`}>
                {pnl.profitMarginPercent.toFixed(2)}%
              </div>
              <div className="text-xs text-gray-500 mt-1">
                {pnl.totalRevenue === 0 ? 'لا إيرادات' : 'من الإيرادات'}
              </div>
            </Card>
          </div>

          {/* Cost breakdown */}
          <Card className="p-4">
            <h3 className="text-sm font-bold text-gray-800 mb-3">تفصيل التكاليف حسب الحساب</h3>
            {pnl.costsByAccount.length === 0 ? (
              <div className="text-center text-gray-500 py-8 text-sm">
                لا توجد تكاليف على هذا المشروع في النطاق الزمني المحدد.
                <br />
                <span className="text-xs text-gray-400 mt-2 block">
                  أضف قيود محاسبية مع project_id لتظهر هنا.
                </span>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b text-gray-600 text-xs">
                      <th className="text-start py-2 px-2">الحساب</th>
                      <th className="text-start py-2 px-2">الكود</th>
                      <th className="text-end py-2 px-2">المبلغ</th>
                      <th className="text-end py-2 px-2">% من التكاليف</th>
                    </tr>
                  </thead>
                  <tbody>
                    {pnl.costsByAccount.map((c) => (
                      <tr key={c.accountCode} className="border-b last:border-0">
                        <td className="py-2 px-2 text-gray-800">{c.accountName}</td>
                        <td className="py-2 px-2 text-gray-500 font-mono text-xs">{c.accountCode}</td>
                        <td className="py-2 px-2 text-end font-mono text-red-700">
                          {formatCurrency(c.amount)}
                        </td>
                        <td className="py-2 px-2 text-end text-gray-600 text-xs">
                          {pnl.totalCosts > 0
                            ? ((c.amount / pnl.totalCosts) * 100).toFixed(1)
                            : '0.0'}%
                        </td>
                      </tr>
                    ))}
                    <tr className="font-bold bg-gray-50">
                      <td className="py-2 px-2">الإجمالي</td>
                      <td className="py-2 px-2"></td>
                      <td className="py-2 px-2 text-end font-mono text-red-700">
                        {formatCurrency(pnl.totalCosts)}
                      </td>
                      <td className="py-2 px-2 text-end text-xs">100%</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            )}
          </Card>

          <p className="text-xs text-gray-500 mt-3 text-end">
            فترة: {pnl.from ? formatDate(pnl.from) : 'البداية'} ← {pnl.to ? formatDate(pnl.to) : 'اليوم'}
          </p>
        </>
      )}
    </div>
  );
}
