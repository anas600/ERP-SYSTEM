'use client';

// Sprint 1: Holding view — Holding header + sub-companies grid.
// Shows the holding the active user belongs to, lists its sub-companies, and
// lets the user switch the active company via the <CompanySwitcher /> already
// present in the Topbar (we re-render it inline for explicit placement).
//
// Contract: GET /api/holdings/{slug}
// For the demo, the slug is "mfa-holding" (MFA Holding). The page is
// slug-driven: a future holding-switcher could read the slug from a
// `?slug=` query param or from the user's holding. For now we hard-code
// it as a constant so the demo link is stable.

import { useEffect, useMemo, useState } from 'react';
import {
  Building2,
  MapPin,
  Coins,
  Hash,
  AlertCircle,
  RefreshCw,
  CheckCircle2,
  XCircle,
  Calendar,
  ChevronLeft,
  Briefcase,
} from 'lucide-react';
import { Card, PageHeader, Button, Badge, EmptyState } from '@/components/ui';
import { CompanySwitcher } from '@/components/layout/CompanySwitcher';
import { holdingsApi, getErrorMessage, HoldingDetail, HoldingCompany } from '@/lib/api';
import { formatDate } from '@/lib/utils';

// الـ slug الـ demo (MFA Holding). قابل للاستبدال لاحقاً بمصدر ديناميكي.
const DEMO_HOLDING_SLUG = 'mfa-holding';

// Gradient palette — كل holding ياخذ gradient حسب الـ hash لـ name (deterministic).
// يضمن إن الـ logo placeholder يتغير بين الـ holdings بدون backend extra call.
const GRADIENT_PALETTE: { from: string; to: string }[] = [
  { from: 'from-blue-500', to: 'to-indigo-600' },
  { from: 'from-emerald-500', to: 'to-teal-600' },
  { from: 'from-amber-500', to: 'to-orange-600' },
  { from: 'from-purple-500', to: 'to-pink-600' },
  { from: 'from-rose-500', to: 'to-red-600' },
  { from: 'from-cyan-500', to: 'to-blue-600' },
];

function pickGradient(name: string): { from: string; to: string } {
  let h = 0;
  for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) | 0;
  const idx = Math.abs(h) % GRADIENT_PALETTE.length;
  return GRADIENT_PALETTE[idx];
}

function firstLetter(name: string): string {
  return (name ?? '').trim().charAt(0).toUpperCase() || '?';
}

// Sub-company card — name, code, status, currency
function CompanyCard({ company }: { company: HoldingCompany }) {
  const gradient = pickGradient(company.name);
  const active = company.isActive;
  return (
    <Card className="hover:shadow-md transition-shadow">
      <div className="flex items-start gap-3">
        <div
          className={`h-12 w-12 rounded-lg bg-gradient-to-br ${gradient.from} ${gradient.to} text-white flex items-center justify-center font-bold text-lg flex-shrink-0`}
          aria-hidden="true"
        >
          {firstLetter(company.name)}
        </div>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <h3 className="font-bold text-gray-800 truncate">{company.name}</h3>
            {active ? (
              <Badge variant="success">
                <CheckCircle2 className="h-3 w-3" />
                نشطة
              </Badge>
            ) : (
              <Badge variant="neutral">
                <XCircle className="h-3 w-3" />
                معطّلة
              </Badge>
            )}
          </div>
          <div className="mt-1 flex items-center gap-1 text-xs text-gray-500">
            <Hash className="h-3 w-3" />
            <span className="font-mono">{company.code}</span>
          </div>
          <div className="mt-2 flex flex-wrap items-center gap-3 text-xs text-gray-600">
            <span className="flex items-center gap-1">
              <Coins className="h-3 w-3" />
              {company.currency}
            </span>
            {company.country && (
              <span className="flex items-center gap-1">
                <MapPin className="h-3 w-3" />
                {company.country}
              </span>
            )}
            <span className="flex items-center gap-1">
              <Calendar className="h-3 w-3" />
              {formatDate(company.createdAt)}
            </span>
          </div>
        </div>
      </div>
    </Card>
  );
}

// Sub-company skeleton — يُعرض أثناء تحميل التفاصيل
function CompanyCardSkeleton() {
  return (
    <Card>
      <div className="flex items-start gap-3">
        <div className="h-12 w-12 rounded-lg bg-gray-100 animate-pulse flex-shrink-0" />
        <div className="flex-1 space-y-2">
          <div className="h-4 w-2/3 bg-gray-100 rounded animate-pulse" />
          <div className="h-3 w-1/3 bg-gray-100 rounded animate-pulse" />
          <div className="h-3 w-1/2 bg-gray-100 rounded animate-pulse" />
        </div>
      </div>
    </Card>
  );
}

export default function HoldingPage() {
  const [holding, setHolding] = useState<HoldingDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await holdingsApi.getBySlug(DEMO_HOLDING_SLUG);
      setHolding(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'تعذّر تحميل بيانات القابضة.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  // الـ gradient مستقر للـ holding (لا يتغير بين renders) — يُحسب مرة واحدة
  const heroGradient = useMemo(
    () => pickGradient(holding?.name ?? 'holding'),
    [holding?.name]
  );

  // Sub-companies sort: active أولاً، ثم بالاسم
  const sortedCompanies = useMemo(() => {
    if (!holding) return [];
    return [...holding.companies].sort((a, b) => {
      if (a.isActive !== b.isActive) return a.isActive ? -1 : 1;
      return a.name.localeCompare(b.name, 'ar');
    });
  }, [holding]);

  return (
    <div>
      <PageHeader
        title="🏢 القابضة"
        description="عرض القابضة والشركات التابعة"
        actions={
          <div className="flex items-center gap-2">
            <CompanySwitcher />
            <Button
              variant="secondary"
              onClick={load}
              disabled={loading}
              aria-label="تحديث"
            >
              <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
              <span className="hidden sm:inline">تحديث</span>
            </Button>
          </div>
        }
      />

      {/* Error banner — لو الـ endpoint غير متاح بعد */}
      {error && !loading && (
        <div
          className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 flex items-start gap-3"
          role="alert"
        >
          <AlertCircle className="h-5 w-5 flex-shrink-0 mt-0.5" />
          <div className="flex-1">
            <p className="font-semibold">تعذّر تحميل بيانات القابضة</p>
            <p className="text-sm mt-0.5">
              {error} — Endpoint:{' '}
              <code className="text-xs">/api/holdings/{DEMO_HOLDING_SLUG}</code>
            </p>
            <p className="text-xs mt-1 text-red-600">
              ملاحظة: في وضع التطوير قد يكون الـ endpoint غير مُفعَّل بعد.
            </p>
          </div>
          <Button variant="secondary" onClick={load} disabled={loading}>
            إعادة المحاولة
          </Button>
        </div>
      )}

      {/* Hero section — Holding name + gradient logo + meta */}
      <Card className="mb-6 overflow-hidden">
        <div className="flex flex-col md:flex-row md:items-center gap-4">
          <div
            className={`h-20 w-20 rounded-2xl bg-gradient-to-br ${heroGradient.from} ${heroGradient.to} text-white flex items-center justify-center font-bold text-4xl flex-shrink-0 shadow-md`}
            aria-hidden="true"
          >
            {firstLetter(holding?.name ?? '')}
          </div>
          <div className="flex-1 min-w-0">
            {loading && !holding ? (
              <>
                <div className="h-7 w-48 bg-gray-100 rounded animate-pulse mb-2" />
                <div className="h-4 w-64 bg-gray-100 rounded animate-pulse" />
              </>
            ) : (
              <>
                <div className="flex items-center gap-2 flex-wrap">
                  <h2 className="text-2xl font-bold text-gray-800">
                    {holding?.name ?? '—'}
                  </h2>
                  <Badge variant="info">القابضة</Badge>
                </div>
                {holding?.legalName && holding.legalName !== holding.name && (
                  <p className="text-sm text-gray-500 mt-1">{holding.legalName}</p>
                )}
                <div className="mt-3 flex flex-wrap items-center gap-3 text-sm text-gray-600">
                  {holding?.taxNumber && (
                    <span className="flex items-center gap-1">
                      <Hash className="h-3.5 w-3.5 text-gray-400" />
                      <span className="font-mono text-xs">{holding.taxNumber}</span>
                    </span>
                  )}
                  <span className="flex items-center gap-1">
                    <Coins className="h-3.5 w-3.5 text-gray-400" />
                    العملة الأساسية:{' '}
                    <span className="font-semibold">{holding?.baseCurrency ?? '—'}</span>
                  </span>
                  {holding?.country && (
                    <span className="flex items-center gap-1">
                      <MapPin className="h-3.5 w-3.5 text-gray-400" />
                      {holding.country}
                    </span>
                  )}
                </div>
              </>
            )}
          </div>
          <div className="text-right md:text-left md:border-r md:pr-6 md:mr-2 border-gray-200">
            <p className="text-xs text-gray-500">عدد الشركات</p>
            <p className="text-3xl font-bold text-blue-600 mt-0.5 tabular-nums">
              {loading && !holding ? (
                <span className="inline-block h-8 w-10 bg-gray-100 rounded animate-pulse" />
              ) : (
                holding?.companies.length ?? 0
              )}
            </p>
            {holding && holding.companies.length > 0 && (
              <p className="text-xs text-gray-400 mt-0.5">
                {holding.companies.filter((c) => c.isActive).length} نشطة
              </p>
            )}
          </div>
        </div>
      </Card>

      {/* Sub-companies section */}
      <div className="mb-3 flex items-center justify-between">
        <h3 className="text-lg font-bold text-gray-800 flex items-center gap-2">
          <Briefcase className="h-5 w-5 text-gray-500" />
          الشركات التابعة
        </h3>
        {!loading && holding && (
          <span className="text-xs text-gray-500">
            إجمالي: {sortedCompanies.length}
          </span>
        )}
      </div>

      {loading && !holding ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {[1, 2, 3].map((i) => (
            <CompanyCardSkeleton key={i} />
          ))}
        </div>
      ) : error && !holding ? (
        // الـ error banner فوق يعرض التفاصيل — هنا نعرض empty state بسيط
        <Card>
          <div className="text-center py-8 text-sm text-gray-500">
            لا توجد بيانات لعرضها.
          </div>
        </Card>
      ) : sortedCompanies.length === 0 ? (
        <Card>
          <EmptyState
            icon={<Building2 className="h-12 w-12 text-gray-300" />}
            title="لا توجد شركات تابعة"
            description="لم يتم إضافة أي شركة لهذه القابضة بعد."
          />
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {sortedCompanies.map((c) => (
            <CompanyCard key={c.id} company={c} />
          ))}
        </div>
      )}

      {/* Footer hint — يوضّح إن الـ active company ممكن يتغيّر من الـ CompanySwitcher */}
      {!loading && holding && holding.companies.length > 0 && (
        <div className="mt-6 text-center text-xs text-gray-400 flex items-center justify-center gap-1">
          <ChevronLeft className="h-3 w-3" />
          <span>
            لتغيير الشركة النشطة، استخدم زر الشركة في أعلى الصفحة.
          </span>
        </div>
      )}
    </div>
  );
}
