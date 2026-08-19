'use client';

// Sprint 57 (DEC-152) — Path C.2: Executive Dashboard
// Holding-level overview: KPIs + Revenue trend + Top customers + Expense breakdown + AR/AP aging charts
// Uses Recharts (already in package.json)

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { TrendingUp, TrendingDown, DollarSign, Wallet, Users, Package, AlertCircle, BarChart3, Calendar, RefreshCw, ArrowLeft, Receipt, FileBarChart } from 'lucide-react';
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, LineChart, Line, CartesianGrid, Legend, PieChart, Pie, Cell } from 'recharts';
import { PageHeader, Card } from '@/components/ui';
import { getErrorMessage } from '@/lib/api';
import { formatNumber } from '@/lib/format';

const API_BASE = process.env.NEXT_PUBLIC_API_BASE || '';

interface DashboardData {
  asOfDate: string;
  companyId: string;
  companyName: string;
  kpis: {
    revenueYtd: number;
    expensesYtd: number;
    netIncomeYtd: number;
    cashPosition: number;
    arTotal: number;
    apTotal: number;
    openSalesInvoices: number;
    openVendorBills: number;
  };
  revenueTrend12Months: { month: string; monthLabel: string; revenue: number; expenses: number; netIncome: number }[];
  topCustomers: { label: string; value: number }[];
  expenseBreakdown: { label: string; value: number }[];
  arAgingBuckets: { current: number; days31To60: number; days61To90: number; days91Plus: number };
  apAgingBuckets: { current: number; days31To60: number; days61To90: number; days91Plus: number };
}

const CHART_COLORS = ['#f59e0b', '#3b82f6', '#10b981', '#8b5cf6', '#ef4444', '#ec4899', '#14b8a6'];
const AGING_COLORS = ['#10b981', '#f59e0b', '#f97316', '#ef4444']; // green→amber→orange→red

export default function ExecutiveDashboard() {
  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const token = typeof window !== 'undefined' ? localStorage.getItem('accessToken') : null;
      const companyId = typeof window !== 'undefined' ? localStorage.getItem('currentCompanyId') : null;
      const headers: HeadersInit = { 'Content-Type': 'application/json' };
      if (token) (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`;
      if (companyId) (headers as Record<string, string>)['X-Company-Id'] = companyId;
      const r = await fetch(`${API_BASE}/api/finance/ledger/dashboard/executive`, { headers, credentials: 'include' });
      if (!r.ok) throw new Error(`HTTP ${r.status}`);
      const j = await r.json();
      setData(j);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل لوحة المعلومات التنفيذية.'));
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, []);

  return (
    <div>
      <Link href="/dashboard" className="inline-flex items-center gap-1 text-sm text-ink-500 hover:text-brand-600 mb-3 transition-colors">
        <ArrowLeft className="h-4 w-4" />
        العودة للوحة التحكم
      </Link>
      <PageHeader
        title="اللوحة التنفيذية"
        description={`Sprint 57 (DEC-152): نظرة عامة على الشركة — KPIs + رسوم بيانية لـ Revenue/Expense Trend + Top Customers + AR/AP Aging`}
        actions={
          <button
            onClick={load}
            className="inline-flex items-center gap-1 px-3 py-2 text-sm text-ink-700 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
          >
            <RefreshCw className="h-4 w-4" /> تحديث
          </button>
        }
      />

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
      ) : !data ? null : (
        <>
          {/* ==== KPI Cards ==== */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
            <KpiCard
              icon={<DollarSign className="h-6 w-6" />}
              label="الإيرادات YTD"
              value={data.kpis.revenueYtd}
              color="emerald"
              suffix="LYD"
            />
            <KpiCard
              icon={<TrendingDown className="h-6 w-6" />}
              label="المصروفات YTD"
              value={data.kpis.expensesYtd}
              color="red"
              suffix="LYD"
            />
            <KpiCard
              icon={data.kpis.netIncomeYtd >= 0 ? <TrendingUp className="h-6 w-6" /> : <TrendingDown className="h-6 w-6" />}
              label="صافي الدخل YTD"
              value={data.kpis.netIncomeYtd}
              color={data.kpis.netIncomeYtd >= 0 ? 'emerald' : 'red'}
              suffix="LYD"
            />
            <KpiCard
              icon={<Wallet className="h-6 w-6" />}
              label="السيولة النقدية"
              value={data.kpis.cashPosition}
              color="blue"
              suffix="LYD"
            />
            <KpiCard
              icon={<Users className="h-6 w-6" />}
              label="إجمالي ذمم العملاء (AR)"
              value={data.kpis.arTotal}
              color="amber"
              suffix="LYD"
              sublabel={`${data.kpis.openSalesInvoices} فاتورة مفتوحة`}
            />
            <KpiCard
              icon={<Package className="h-6 w-6" />}
              label="إجمالي ذمم الموردين (AP)"
              value={data.kpis.apTotal}
              color="purple"
              suffix="LYD"
              sublabel={`${data.kpis.openVendorBills} فاتورة موردين`}
            />
            <KpiCard
              icon={<Receipt className="h-6 w-6" />}
              label="صافي AR-AP (Working Capital)"
              value={data.kpis.arTotal - data.kpis.apTotal}
              color={(data.kpis.arTotal - data.kpis.apTotal) >= 0 ? 'emerald' : 'red'}
              suffix="LYD"
              sublabel="(AR) − (AP)"
            />
            <KpiCard
              icon={<FileBarChart className="h-6 w-6" />}
              label="الفترة"
              value={null}
              color="gray"
              customText={new Date(data.asOfDate).toLocaleDateString('ar-LY', { day: 'numeric', month: 'long', year: 'numeric' })}
            />
          </div>

          {/* ==== Revenue/Expense Trend (12 months) ==== */}
          <Card className="p-4 mb-6">
            <div className="flex items-center gap-2 mb-4">
              <BarChart3 className="h-5 w-5 text-blue-700" />
              <h3 className="text-base font-bold text-blue-900">اتجاه الإيرادات والمصروفات (آخر 12 شهر)</h3>
            </div>
            {data.revenueTrend12Months.length === 0 ? (
              <div className="text-gray-400 text-sm py-8 text-center">لا توجد بيانات</div>
            ) : (
              <ResponsiveContainer width="100%" height={280}>
                <LineChart data={data.revenueTrend12Months} margin={{ top: 10, right: 30, left: 0, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                  <XAxis dataKey="monthLabel" tick={{ fontSize: 11 }} />
                  <YAxis tick={{ fontSize: 11 }} tickFormatter={(v) => v >= 1000 ? `${(v/1000).toFixed(0)}K` : v} />
                  <Tooltip contentStyle={{ direction: 'rtl', fontSize: 12 }} formatter={(v: number) => formatNumber(v) + ' LYD'} />
                  <Legend />
                  <Line type="monotone" dataKey="revenue" name="الإيرادات" stroke="#10b981" strokeWidth={2} dot={{ r: 3 }} />
                  <Line type="monotone" dataKey="expenses" name="المصروفات" stroke="#ef4444" strokeWidth={2} dot={{ r: 3 }} />
                  <Line type="monotone" dataKey="netIncome" name="صافي الدخل" stroke="#3b82f6" strokeWidth={2} dot={{ r: 3 }} />
                </LineChart>
              </ResponsiveContainer>
            )}
          </Card>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 mb-6">
            {/* ==== Top Customers ==== */}
            <Card className="p-4">
              <div className="flex items-center gap-2 mb-4">
                <Users className="h-5 w-5 text-amber-700" />
                <h3 className="text-base font-bold text-amber-900">أكبر 5 عملاء (السنة الحالية)</h3>
              </div>
              {data.topCustomers.length === 0 ? (
                <div className="text-gray-400 text-sm py-8 text-center">لا توجد مبيعات</div>
              ) : (
                <ResponsiveContainer width="100%" height={250}>
                  <BarChart data={data.topCustomers} layout="vertical" margin={{ top: 5, right: 30, left: 80, bottom: 5 }}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                    <XAxis type="number" tick={{ fontSize: 11 }} tickFormatter={(v) => v >= 1000 ? `${(v/1000).toFixed(0)}K` : v} />
                    <YAxis type="category" dataKey="label" tick={{ fontSize: 10 }} width={75} />
                    <Tooltip contentStyle={{ direction: 'rtl', fontSize: 12 }} formatter={(v: number) => formatNumber(v) + ' LYD'} />
                    <Bar dataKey="value" name="المبيعات" fill="#f59e0b" radius={[0, 4, 4, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              )}
            </Card>

            {/* ==== Expense Breakdown ==== */}
            <Card className="p-4">
              <div className="flex items-center gap-2 mb-4">
                <Package className="h-5 w-5 text-red-700" />
                <h3 className="text-base font-bold text-red-900">أكبر 5 حسابات مصروفات (السنة الحالية)</h3>
              </div>
              {data.expenseBreakdown.length === 0 ? (
                <div className="text-gray-400 text-sm py-8 text-center">لا توجد مصروفات</div>
              ) : (
                <ResponsiveContainer width="100%" height={250}>
                  <BarChart data={data.expenseBreakdown} layout="vertical" margin={{ top: 5, right: 30, left: 80, bottom: 5 }}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                    <XAxis type="number" tick={{ fontSize: 11 }} tickFormatter={(v) => v >= 1000 ? `${(v/1000).toFixed(0)}K` : v} />
                    <YAxis type="category" dataKey="label" tick={{ fontSize: 10 }} width={75} />
                    <Tooltip contentStyle={{ direction: 'rtl', fontSize: 12 }} formatter={(v: number) => formatNumber(v) + ' LYD'} />
                    <Bar dataKey="value" name="المصروفات" fill="#ef4444" radius={[0, 4, 4, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              )}
            </Card>
          </div>

          {/* ==== AR vs AP Aging (charts) ==== */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
            <Card className="p-4">
              <div className="flex items-center gap-2 mb-4">
                <Users className="h-5 w-5 text-amber-700" />
                <h3 className="text-base font-bold text-amber-900">أعمار الذمم المدينة (AR Aging)</h3>
                <span className="text-xs text-gray-500 mr-auto">إجمالي: {formatNumber(data.arAgingBuckets.current + data.arAgingBuckets.days31To60 + data.arAgingBuckets.days61To90 + data.arAgingBuckets.days91Plus)} LYD</span>
              </div>
              <AgingPieChart data={data.arAgingBuckets} />
            </Card>

            <Card className="p-4">
              <div className="flex items-center gap-2 mb-4">
                <Package className="h-5 w-5 text-purple-700" />
                <h3 className="text-base font-bold text-purple-900">أعمار الذمم الدائنة (AP Aging)</h3>
                <span className="text-xs text-gray-500 mr-auto">إجمالي: {formatNumber(data.apAgingBuckets.current + data.apAgingBuckets.days31To60 + data.apAgingBuckets.days61To90 + data.apAgingBuckets.days91Plus)} LYD</span>
              </div>
              <AgingPieChart data={data.apAgingBuckets} />
            </Card>
          </div>
        </>
      )}
    </div>
  );
}

function KpiCard({ icon, label, value, color, suffix, sublabel, customText }: {
  icon: React.ReactNode; label: string; value: number | null; color: string; suffix?: string; sublabel?: string; customText?: string;
}) {
  const colorMap: Record<string, string> = {
    emerald: 'bg-emerald-50 border-emerald-200 text-emerald-700',
    red: 'bg-red-50 border-red-200 text-red-700',
    amber: 'bg-amber-50 border-amber-200 text-amber-700',
    blue: 'bg-blue-50 border-blue-200 text-blue-700',
    purple: 'bg-purple-50 border-purple-200 text-purple-700',
    gray: 'bg-gray-50 border-gray-200 text-gray-700',
  };
  const c = colorMap[color] || colorMap.blue;
  return (
    <div className={`p-4 rounded-xl border-2 ${c.split(' ').slice(0, 2).join(' ')}`}>
      <div className="flex items-center justify-between mb-2">
        <span className="text-xs font-bold uppercase tracking-wide opacity-75">{label}</span>
        <div className={`p-1.5 rounded-lg ${c.split(' ')[2]}`}>{icon}</div>
      </div>
      {customText ? (
        <p className="text-2xl font-bold">{customText}</p>
      ) : (
        <>
          <p className="text-2xl font-bold">{value != null ? formatNumber(value) : '—'}</p>
          {suffix && <p className="text-xs opacity-75 mt-0.5">{suffix}</p>}
          {sublabel && <p className="text-xs opacity-75 mt-1">{sublabel}</p>}
        </>
      )}
    </div>
  );
}

function AgingPieChart({ data }: { data: { current: number; days31To60: number; days61To90: number; days91Plus: number } }) {
  const chartData = [
    { name: '0-30 يوم (Current)', value: data.current, color: AGING_COLORS[0] },
    { name: '31-60 يوم', value: data.days31To60, color: AGING_COLORS[1] },
    { name: '61-90 يوم', value: data.days61To90, color: AGING_COLORS[2] },
    { name: '91+ يوم', value: data.days91Plus, color: AGING_COLORS[3] },
  ];
  const total = chartData.reduce((s, c) => s + c.value, 0);
  if (total === 0) {
    return <div className="text-gray-400 text-sm py-8 text-center">لا توجد ذمم مستحقة</div>;
  }
  return (
    <div>
      <ResponsiveContainer width="100%" height={220}>
        <PieChart>
          <Pie
            data={chartData}
            cx="50%"
            cy="50%"
            innerRadius={50}
            outerRadius={85}
            paddingAngle={2}
            dataKey="value"
            label={({ name, value, percent }) => `${(percent * 100).toFixed(0)}%`}
            labelLine={false}
          >
            {chartData.map((entry, i) => (
              <Cell key={i} fill={entry.color} />
            ))}
          </Pie>
          <Tooltip contentStyle={{ direction: 'rtl', fontSize: 12 }} formatter={(v: number) => formatNumber(v) + ' LYD'} />
          <Legend layout="horizontal" verticalAlign="bottom" align="center" wrapperStyle={{ fontSize: 11 }} />
        </PieChart>
      </ResponsiveContainer>
      <div className="grid grid-cols-2 gap-2 mt-3 text-xs">
        {chartData.map((c) => (
          <div key={c.name} className="flex items-center gap-2">
            <span className="w-2.5 h-2.5 rounded-full" style={{ background: c.color }}></span>
            <span className="text-gray-600">{c.name}:</span>
            <span className="font-mono font-bold">{formatNumber(c.value)} LYD</span>
          </div>
        ))}
      </div>
    </div>
  );
}
