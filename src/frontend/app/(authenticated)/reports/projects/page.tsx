'use client';

import { useEffect, useState } from 'react';
import { Briefcase, TrendingUp, BarChart3, ArrowRight } from 'lucide-react';
import Link from 'next/link';
import { PageHeader, Card, Badge, Button } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { api, getErrorMessage } from '@/lib/api';

type ReportType = 'summary' | 'pnl' | 'bva';

interface ProjectRow {
  projectId?: string;
  projectName?: string;
  budget?: number;
  actual?: number;
  variance?: number;
  revenue?: number;
  cost?: number;
  profit?: number;
  status?: string;
}

export default function ProjectsReportsPage() {
  const { loading: authLoading } = useAuth();
  const [reportType, setReportType] = useState<ReportType>('summary');
  const [rows, setRows] = useState<ProjectRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (authLoading) return;
    load();
  }, [authLoading, reportType]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const endpoint = reportType === 'summary' ? 'projects/summary' : reportType;
      const data = await api.get<ProjectRow[] | { items: ProjectRow[] }>(`/api/reports/${endpoint}`);
      const items = Array.isArray(data.data) ? data.data : (data.data as any).items || [];
      setRows(items);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل التقرير.'));
    } finally {
      setLoading(false);
    }
  };

  const reportMeta: Record<ReportType, { title: string; en: string; icon: any; color: string }> = {
    summary: { title: 'ملخص المشاريع', en: 'Project Summary', icon: Briefcase, color: 'blue' },
    pnl: { title: 'الأرباح والخسائر', en: 'Project P&L', icon: TrendingUp, color: 'green' },
    'bva': { title: 'الميزانية مقابل الفعلي', en: 'Budget vs Actual', icon: BarChart3, color: 'purple' },
  };
  const current = reportMeta[reportType];

  return (
    <div>
      <PageHeader
        title={`📁 ${current.title}`}
        description={current.en}
        actions={
          <Link href="/reports">
            <Button variant="secondary" iconLeft={<ArrowRight className="h-4 w-4" />}>
              العودة للتقارير
            </Button>
          </Link>
        }
      />

      {/* Tabs */}
      <div className="bg-white rounded-xl shadow-sm p-2 mb-4 flex gap-1 overflow-x-auto">
        {(Object.keys(reportMeta) as ReportType[]).map((key) => {
          const r = reportMeta[key];
          const Icon = r.icon;
          return (
            <button
              key={key}
              onClick={() => setReportType(key)}
              className={`px-4 py-2 rounded-lg text-sm font-medium flex items-center gap-2 whitespace-nowrap ${
                reportType === key ? `bg-${r.color}-500 text-white` : 'text-gray-600 hover:bg-gray-100'
              }`}
            >
              <Icon className="h-4 w-4" />
              {r.title}
            </button>
          );
        })}
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">
          {error}
        </div>
      )}

      {loading ? (
        <div className="bg-white rounded-xl shadow-sm p-12 text-center text-gray-500">
          <div className="inline-block h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-r-transparent" />
          <p className="mt-3 text-sm">جاري التحميل...</p>
        </div>
      ) : rows.length === 0 ? (
        <div className="bg-white rounded-xl shadow-sm p-12 text-center text-gray-500">
          لا توجد مشاريع.
        </div>
      ) : (
        <div className="bg-white rounded-xl shadow-sm p-4">
          <table className="w-full text-sm">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">المشروع</th>
                {reportType === 'summary' && (
                  <>
                    <th className="px-3 py-2 text-start text-xs font-semibold text-gray-600">الحالة</th>
                    <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">الإيراد</th>
                    <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">التكلفة</th>
                  </>
                )}
                {reportType === 'pnl' && (
                  <>
                    <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">الإيراد</th>
                    <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">التكلفة</th>
                    <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">الربح</th>
                  </>
                )}
                {reportType === 'bva' && (
                  <>
                    <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">الميزانية</th>
                    <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">الفعلي</th>
                    <th className="px-3 py-2 text-end text-xs font-semibold text-gray-600">الفرق</th>
                  </>
                )}
              </tr>
            </thead>
            <tbody>
              {rows.map((r, i) => (
                <tr key={i} className="border-b border-gray-100 last:border-0">
                  <td className="px-3 py-2 font-semibold">{r.projectName ?? r.projectId ?? '—'}</td>
                  {reportType === 'summary' && (
                    <>
                      <td className="px-3 py-2"><Badge variant="info">{r.status ?? '—'}</Badge></td>
                      <td className="px-3 py-2 text-end font-mono">
                        {r.revenue?.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) ?? '—'}
                      </td>
                      <td className="px-3 py-2 text-end font-mono">
                        {r.cost?.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) ?? '—'}
                      </td>
                    </>
                  )}
                  {reportType === 'pnl' && (
                    <>
                      <td className="px-3 py-2 text-end font-mono">
                        {r.revenue?.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) ?? '—'}
                      </td>
                      <td className="px-3 py-2 text-end font-mono">
                        {r.cost?.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) ?? '—'}
                      </td>
                      <td className={`px-3 py-2 text-end font-mono font-bold ${(r.profit ?? 0) >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                        {r.profit?.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) ?? '—'}
                      </td>
                    </>
                  )}
                  {reportType === 'bva' && (
                    <>
                      <td className="px-3 py-2 text-end font-mono">
                        {r.budget?.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) ?? '—'}
                      </td>
                      <td className="px-3 py-2 text-end font-mono">
                        {r.actual?.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) ?? '—'}
                      </td>
                      <td className={`px-3 py-2 text-end font-mono font-bold ${(r.variance ?? 0) >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                        {r.variance?.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) ?? '—'}
                      </td>
                    </>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
