'use client';

// 🏗️ /dashboard/cross-module — Sprint 65 / Wave 2A (DEC-234 + DEC-236)
//
// Cross-module KPI page that ties together AR, AP, and project profitability
// in one screen. Built for construction companies that need to see "what is
// owed to us (AR) vs what we owe (AP) vs how each project is performing" in
// a single glance.
//
// The page is intentionally read-only — no mutations. The data flow is:
//   - 2 parallel GETs on mount: /api/dashboard/cross-module + /api/dashboard/project-profitability
//   - The data is passed down to the 3 components as props
//   - Components render stat cards (OutstandingArCard, OutstandingApCard) + a profitability list
//   - Errors render an error.tsx boundary; empty state renders zeroed cards

import { useEffect, useState } from 'react';
import { AlertCircle, RefreshCw } from 'lucide-react';
import { PageHeader, Card, SkeletonPage, EmptyState } from '@/components/ui';
import { OutstandingArCard } from '@/components/dashboard/OutstandingArCard';
import { OutstandingApCard } from '@/components/dashboard/OutstandingApCard';
import { ProjectProfitabilityCard } from '@/components/dashboard/ProjectProfitabilityCard';
import {
  fetchDashboardCrossModule,
  fetchProjectProfitability,
} from '@/lib/api/dashboard';
import { getErrorMessage } from '@/lib/api';
import type {
  DashboardCrossModuleResponse,
  ProjectProfitabilityResponse,
} from '@/lib/api-types';

function emptyKpi(): DashboardCrossModuleResponse {
  return {
    outstandingAR: 0,
    outstandingAP: 0,
    netPosition: 0,
    projectCount: 0,
    totalContractValue: 0,
    totalRevenue: 0,
    totalSubcontractorCost: 0,
    unprofitableProjects: 0,
  };
}

export default function CrossModuleDashboardPage() {
  const [kpi, setKpi] = useState<DashboardCrossModuleResponse | null>(null);
  const [projects, setProjects] = useState<ProjectProfitabilityResponse[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const [kpiData, projectsData] = await Promise.all([
        fetchDashboardCrossModule(),
        fetchProjectProfitability(),
      ]);
      setKpi(kpiData);
      setProjects(projectsData);
    } catch (e) {
      setError(getErrorMessage(e));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  if (loading && !kpi) {
    return (
      <>
        <PageHeader
          title="مؤشرات الموديولات المشتركة"
          description="الذمم المدينة + الذمم الدائنة + ربحية المشاريع"
        />
        <SkeletonPage />
      </>
    );
  }

  if (error) {
    return (
      <>
        <PageHeader
          title="مؤشرات الموديولات المشتركة"
          description="الذمم المدينة + الذمم الدائنة + ربحية المشاريع"
        />
        <Card accent="red">
          <div className="flex items-start gap-3">
            <AlertCircle className="h-5 w-5 text-rose-600 mt-0.5" />
            <div className="min-w-0 flex-1">
              <p className="font-semibold text-rose-700">فشل تحميل البيانات</p>
              <p className="text-sm text-ink-600 mt-1 break-words">{error}</p>
              <button
                type="button"
                onClick={load}
                className="mt-3 inline-flex items-center gap-1.5 text-sm font-semibold text-brand-600 hover:text-brand-700"
              >
                <RefreshCw className="h-4 w-4" />
                إعادة المحاولة
              </button>
            </div>
          </div>
        </Card>
      </>
    );
  }

  const data = kpi ?? emptyKpi();

  return (
    <>
      <PageHeader
        title="مؤشرات الموديولات المشتركة"
        description="نظرة موحّدة على الذمم المدينة، الذمم الدائنة، وربحية المشاريع"
        actions={
          <button
            type="button"
            onClick={load}
            className="inline-flex items-center gap-1.5 text-sm font-semibold text-brand-600 hover:text-brand-700"
            aria-label="إعادة تحميل"
          >
            <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
            تحديث
          </button>
        }
      />

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
        <OutstandingArCard value={data.outstandingAR} loading={loading} />
        <OutstandingApCard value={data.outstandingAP} loading={loading} />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 mb-4">
        <Card title="المركز المالي" description="صافي المركز = الذمم المدينة − الذمم الدائنة" accent={data.netPosition >= 0 ? 'green' : 'red'}>
          <p
            className={
              data.netPosition >= 0
                ? 'text-3xl font-extrabold text-success-700 tabular-nums'
                : 'text-3xl font-extrabold text-rose-600 tabular-nums'
            }
            data-testid="net-position"
          >
            {data.netPosition.toLocaleString('en-GB', { maximumFractionDigits: 0 })} LYD
          </p>
          <p className="text-xs text-ink-500 mt-2">
            عدد المشاريع غير المربحة حالياً: <strong>{data.unprofitableProjects}</strong>
          </p>
        </Card>

        <Card title="العقود النشطة" description="قيمة العقود الجارية" accent="blue">
          <p className="text-3xl font-extrabold text-blue-700 tabular-nums">
            {data.totalContractValue.toLocaleString('en-GB', { maximumFractionDigits: 0 })} LYD
          </p>
          <p className="text-xs text-ink-500 mt-2">
            عدد المشاريع النشطة: <strong>{data.projectCount}</strong>
          </p>
        </Card>

        <Card title="إجمالي الإيرادات" description="الفواتير المرحلة (Posted)" accent="purple">
          <p className="text-3xl font-extrabold text-purple-700 tabular-nums">
            {data.totalRevenue.toLocaleString('en-GB', { maximumFractionDigits: 0 })} LYD
          </p>
          <p className="text-xs text-ink-500 mt-2">
            تكاليف المقاولين: <strong>{data.totalSubcontractorCost.toLocaleString('en-GB', { maximumFractionDigits: 0 })} LYD</strong>
          </p>
        </Card>
      </div>

      <div className="grid grid-cols-1">
        <ProjectProfitabilityCard projects={projects} limit={10} loading={loading} />
      </div>

      {projects.length === 0 && !loading && (
        <EmptyState
          title="لا توجد بيانات لعرضها"
          description="لم يتم العثور على أي مشاريع لهذه الشركة بعد."
        />
      )}
    </>
  );
}
