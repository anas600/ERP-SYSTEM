'use client';

import { useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import {
  BarChart3, TrendingUp, Package, Briefcase, FileText, Calendar, Download, Filter, RefreshCw, AlertCircle, Clock
} from 'lucide-react';
import { PageHeader, Card, Badge, Button } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { getReports, getErrorMessage } from '@/lib/api';
import type { ReportDto } from '@/lib/api-types';
import { formatDateTime } from '@/lib/utils';

type ReportTab = 'overview' | 'financial' | 'sales' | 'inventory' | 'projects';

const TABS: { id: ReportTab; label: string; icon: any; color: string }[] = [
  { id: 'overview',   label: 'Overview',     icon: BarChart3, color: 'blue' },
  { id: 'financial',  label: 'Financial',    icon: TrendingUp, color: 'green' },
  { id: 'sales',      label: 'Sales',        icon: FileText, color: 'purple' },
  { id: 'inventory',  label: 'Inventory',    icon: Package, color: 'orange' },
  { id: 'projects',   label: 'Projects',     icon: Briefcase, color: 'red' },
];

const REPORTS = {
  financial: [
    { id: 'pl', name: 'Profit & Loss', description: 'Income statement summary', href: '/reports/financial', badge: 'Most used' },
    { id: 'bs', name: 'Balance Sheet', description: 'Assets, liabilities, equity', href: '/reports/financial', badge: null },
    { id: 'tb', name: 'Trial Balance', description: 'All account balances', href: '/reports/financial', badge: null },
    { id: 'cf', name: 'Cash Flow', description: 'Cash movement over period', href: '/reports/financial', badge: 'New' },
  ],
  sales: [
    { id: 'sp', name: 'Sales by Period', description: 'Revenue trend over time', href: '/reports/sales', badge: null },
    { id: 'tc', name: 'Top Customers', description: 'Highest-revenue customers', href: '/reports/sales', badge: 'Top 10' },
    { id: 'ar', name: 'Aging Report (AR)', description: 'Outstanding invoices by age', href: '/reports/sales', badge: null },
  ],
  inventory: [
    { id: 'val', name: 'Inventory Valuation', description: 'Current stock value', href: '/reports/inventory', badge: null },
    { id: 'low', name: 'Low Stock Alerts', description: 'Items below reorder point', href: '/reports/inventory', badge: 'Urgent' },
    { id: 'mov', name: 'Stock Movements', description: 'Recent in/out movements', href: '/reports/inventory', badge: null },
    { id: 'age', name: 'Stock Aging', description: 'Items by age in stock', href: '/reports/inventory', badge: null },
  ],
  projects: [
    { id: 'pnl', name: 'Project P&L', description: 'Revenue vs cost per project', href: '/reports/projects', badge: null },
    { id: 'bva', name: 'Budget vs Actual', description: 'Project budget tracking', href: '/reports/projects', badge: null },
    { id: 'sum', name: 'Project Summary', description: 'All projects overview', href: '/reports/projects', badge: null },
  ],
};

export default function ReportsPage() {
  const { loading: authLoading } = useAuth();
  const [activeTab, setActiveTab] = useState<ReportTab>('overview');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');

  // Sprint 11 T1: saved reports list (separate state so the page works
  // even when the BE endpoint isn't wired yet on the parallel branch).
  const [reports, setReports] = useState<ReportDto[]>([]);
  const [reportsLoading, setReportsLoading] = useState(true);
  const [reportsError, setReportsError] = useState<string | null>(null);

  const loadReports = async () => {
    setReportsLoading(true);
    setReportsError(null);
    try {
      const data = await getReports();
      setReports(Array.isArray(data) ? data : []);
    } catch (e: unknown) {
      setReportsError(getErrorMessage(e, 'تعذّر تحميل التقارير المحفوظة.'));
      setReports([]);
    } finally {
      setReportsLoading(false);
    }
  };

  useEffect(() => {
    if (!authLoading) {
      void loadReports();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [authLoading]);

  if (authLoading) {
    return <div className="text-center py-12 text-gray-500">جاري التحميل...</div>;
  }

  return (
    <div>
      <PageHeader
        title="📊 التقارير"
        description="Reports — كل التقارير المالية والمبيعات والمخزون والمشاريع"
        actions={
          <Button
            variant="secondary"
            onClick={loadReports}
            disabled={reportsLoading}
            iconLeft={<RefreshCw className={`h-4 w-4 ${reportsLoading ? 'animate-spin' : ''}`} />}
          >
            تحديث
          </Button>
        }
      />

      {/* Sprint 11 T1: Saved reports list (above the tabs).
          Surfaces the most recently generated reports. */}
      <SavedReportsPanel
        reports={reports}
        loading={reportsLoading}
        error={reportsError}
        onRefresh={loadReports}
      />

      {/* Date Range Filter */}
      <div className="bg-white rounded-xl shadow-sm p-4 mb-4">
        <div className="flex items-center gap-2 mb-3">
          <Filter className="h-4 w-4 text-gray-500" />
          <span className="text-sm font-medium text-gray-700">الفترة الزمنية</span>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
          <div>
            <label className="text-xs text-gray-500 block mb-1">من تاريخ</label>
            <input
              type="date"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
            />
          </div>
          <div>
            <label className="text-xs text-gray-500 block mb-1">إلى تاريخ</label>
            <input
              type="date"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-lg text-sm"
            />
          </div>
          <div className="flex items-end gap-2">
            <Button variant="primary" size="sm">تطبيق</Button>
            <Button variant="secondary" size="sm" onClick={() => { setFromDate(''); setToDate(''); }}>
              مسح
            </Button>
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="bg-white rounded-xl shadow-sm p-2 mb-4 flex gap-1 overflow-x-auto">
        {TABS.map((tab) => {
          const Icon = tab.icon;
          return (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`px-4 py-2 rounded-lg text-sm font-medium flex items-center gap-2 whitespace-nowrap ${
                activeTab === tab.id
                  ? `bg-${tab.color}-500 text-white`
                  : 'text-gray-600 hover:bg-gray-100'
              }`}
            >
              <Icon className="h-4 w-4" />
              {tab.label}
            </button>
          );
        })}
      </div>

      {/* Tab Content */}
      {activeTab === 'overview' && <OverviewTab />}
      {activeTab === 'financial' && <ReportGrid title="التقارير المالية" reports={REPORTS.financial} color="green" />}
      {activeTab === 'sales' && <ReportGrid title="تقارير المبيعات" reports={REPORTS.sales} color="purple" />}
      {activeTab === 'inventory' && <ReportGrid title="تقارير المخزون" reports={REPORTS.inventory} color="orange" />}
      {activeTab === 'projects' && <ReportGrid title="تقارير المشاريع" reports={REPORTS.projects} color="red" />}
    </div>
  );
}

function OverviewTab() {
  const cards = [
    { title: 'إجمالي التقارير', value: '14', sub: '4 فئات', icon: BarChart3, color: 'blue' },
    { title: 'تقارير مالية', value: '4', sub: 'P&L, BS, TB, CF', icon: TrendingUp, color: 'green' },
    { title: 'تقارير مبيعات', value: '3', sub: 'SP, TC, AR', icon: FileText, color: 'purple' },
    { title: 'تقارير مخزون', value: '4', sub: 'VAL, LOW, MOV, AGE', icon: Package, color: 'orange' },
    { title: 'تقارير مشاريع', value: '3', sub: 'PNL, BVA, SUM', icon: Briefcase, color: 'red' },
  ];
  return (
    <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-3">
      {cards.map((c) => {
        const Icon = c.icon;
        return (
          <Card key={c.title} accent={c.color as any}>
            <div className="flex items-start justify-between mb-2">
              <Icon className={`h-5 w-5 text-${c.color}-600`} />
            </div>
            <div className="text-2xl font-bold text-gray-800">{c.value}</div>
            <div className="text-sm text-gray-600 mt-1">{c.title}</div>
            <div className="text-xs text-gray-400 mt-1">{c.sub}</div>
          </Card>
        );
      })}
    </div>
  );
}

function ReportGrid({ title, reports, color }: { title: string; reports: any[]; color: string }) {
  return (
    <div>
      <h2 className="text-lg font-semibold text-gray-800 mb-3">{title}</h2>
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
        {reports.map((r) => (
          <Link key={r.id} href={r.href}>
            <Card accent={color as any} className="hover:shadow-md transition-shadow cursor-pointer">
              <div className="flex items-start justify-between mb-2">
                <BarChart3 className={`h-5 w-5 text-${color}-600`} />
                {r.badge && <Badge variant="info">{r.badge}</Badge>}
              </div>
              <h3 className="font-bold text-gray-800">{r.name}</h3>
              <p className="text-sm text-gray-500 mt-1">{r.description}</p>
              <div className="mt-3 flex items-center gap-2 text-xs text-blue-600">
                <Download className="h-3 w-3" />
                <span>PDF / CSV</span>
              </div>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  );
}

// ============ SavedReportsPanel (Sprint 11 T1) ============
//
// Shows the list of recently generated/saved reports. Soft-fails if the BE
// endpoint isn't wired yet on the parallel branch.

interface SavedReportsPanelProps {
  reports: ReportDto[];
  loading: boolean;
  error: string | null;
  onRefresh: () => void;
}

function SavedReportsPanel({ reports, loading, error, onRefresh }: SavedReportsPanelProps) {
  return (
    <Card className="mb-4">
      <div className="flex items-center justify-between mb-3">
        <h3 className="text-base font-bold text-gray-800 flex items-center gap-2">
          <FileText className="h-4 w-4 text-blue-600" />
          التقارير المحفوظة
          {reports.length > 0 && (
            <span className="text-xs text-gray-500 font-normal">
              ({reports.length})
            </span>
          )}
        </h3>
        {error && <Badge variant="neutral">غير متاح بعد</Badge>}
      </div>
      {error ? (
        <div className="bg-amber-50 border border-amber-100 text-amber-800 px-3 py-2 rounded-lg text-sm flex items-start gap-2">
          <AlertCircle className="h-4 w-4 flex-shrink-0 mt-0.5" />
          <div>
            <p className="text-xs">{error}</p>
            <p className="text-xs mt-0.5 text-amber-700">
              ملاحظة: قد يكون الـ endpoint <code>/api/reports</code> غير مُفعَّل بعد على الفرع المتوازي.
            </p>
          </div>
        </div>
      ) : loading && reports.length === 0 ? (
        <div className="space-y-2">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-9 bg-gray-100 rounded animate-pulse" />
          ))}
        </div>
      ) : reports.length === 0 ? (
        <div className="text-center py-4 text-sm text-gray-500">
          لا توجد تقارير محفوظة بعد. أنشئ تقريراً من الفئات أدناه.
        </div>
      ) : (
        <ul className="divide-y divide-gray-100">
          {reports.slice(0, 10).map((r) => (
            <li key={r.id} className="py-2 flex items-center gap-3 text-sm">
              <FileText className="h-4 w-4 text-blue-500 flex-shrink-0" />
              <div className="flex-1 min-w-0">
                <div className="font-medium text-gray-800 truncate">{r.title}</div>
                <div className="text-xs text-gray-500 flex items-center gap-2 mt-0.5">
                  <Badge variant="info">{r.type}</Badge>
                  <span className="flex items-center gap-1">
                    <Clock className="h-3 w-3" />
                    {formatDateTime(r.generatedAt)}
                  </span>
                </div>
              </div>
              {r.downloadUrl && (
                <a
                  href={r.downloadUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-xs text-blue-600 hover:text-blue-800 flex items-center gap-1"
                >
                  <Download className="h-3 w-3" />
                  تنزيل
                </a>
              )}
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}
