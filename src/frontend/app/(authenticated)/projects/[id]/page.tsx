'use client';

// صفحة تفاصيل المشروع (Project) — Sprint 59 redesign (DEC-173)
//
// Modern layout:
//   1. PageHero with code, name, status pill, and KPI highlight (net P&L or budget)
//   2. 4 KPI StatCards (Budget, Revenue, Costs, Profit) — loaded from PnL
//   3. Tab navigation as FilterChips: التفاصيل / P&L / العقد / المستخلصات
//   4. Each tab renders its own modern content (cards, tables, modals)
//
// Sprint 57 (DEC-160..162): P&L data
// Sprint 58 (DEC-163..165): Contract + Billings + WIP

import { useEffect, useState, useMemo, Fragment } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import {
  ArrowRight, FileText, RefreshCw, TrendingUp, TrendingDown,
  Calendar, BarChart3, FileSignature, Receipt, Plus, Trash2, CheckCircle, XCircle,
  AlertTriangle, Package, Pencil, Wallet, Coins, Calculator, ClipboardList, Ruler, Hammer, ListChecks,
} from 'lucide-react';
import {
  PageHero, StatCard, StatusPill, SectionCard, ProgressBar,
  ModernTable, FilterChips, Button, Input, Modal, EmptyState, SkeletonTable,
  type ModernTableColumn,
} from '@/components/ui';
import {
  api, getErrorMessage, projectsApi, type Project, type ProjectStatusName, type ProjectPnL, type Contract,
  type CreateContractRequest, type UpdateContractRequest, type ProgressBilling,
  type CreateBillingRequest, type BillingPreview, type ProjectWip,
  type BoqSectionDto, type BoqLineDto, type BoqSubitemDto, type VariationOrderDto,
} from '@/lib/api';
import { formatDate, formatCurrency } from '@/lib/utils';

// L120: BE returns ProjectStatus as string (Planning|Active|OnHold|Completed|Cancelled).
const STATUS_META: Record<ProjectStatusName, { label: string; tone: 'green' | 'amber' | 'red' | 'blue' | 'slate' }> = {
  Planning: { label: 'تخطيط', tone: 'slate' },
  Active: { label: 'نشط', tone: 'green' },
  OnHold: { label: 'معلق', tone: 'amber' },
  Completed: { label: 'مكتمل', tone: 'blue' },
  Cancelled: { label: 'ملغي', tone: 'red' },
};

type Tab = 'details' | 'pnl' | 'contract' | 'billings' | 'boq' | 'variations';

const TAB_CHIPS = [
  { key: 'details', label: 'التفاصيل', icon: <FileText className="h-3.5 w-3.5" /> },
  { key: 'pnl', label: 'P&L', icon: <BarChart3 className="h-3.5 w-3.5" /> },
  { key: 'contract', label: 'العقد', icon: <FileSignature className="h-3.5 w-3.5" /> },
  { key: 'billings', label: 'المستخلصات', icon: <Receipt className="h-3.5 w-3.5" /> },
  { key: 'boq', label: 'كراسة الحصر', icon: <ClipboardList className="h-3.5 w-3.5" /> },
  { key: 'variations', label: 'الأوامر التعديلية', icon: <AlertTriangle className="h-3.5 w-3.5" /> },
];

export default function ProjectsIdPage() {
  const params = useParams<{ id: string }>();
  const id = params.id;

  const [item, setItem] = useState<Project | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tab, setTab] = useState<Tab>('details');
  const [pnl, setPnl] = useState<ProjectPnL | null>(null);
  const [wip, setWip] = useState<ProjectWip | null>(null);

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const data = await projectsApi.getProject(id);
      setItem(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل البيانات.'));
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, [id]);

  if (loading) {
    return (
      <div className="space-y-6">
        <PageHero
          eyebrow="إدارة المشاريع"
          title="جاري التحميل…"
          tone="violet"
        />
        <SkeletonTable rows={4} cols={3} />
      </div>
    );
  }

  const status = item ? STATUS_META[item.status] : null;

  return (
    <div className="space-y-6">
      <PageHero
        eyebrow="إدارة المشاريع"
        title={item ? `${item.name}` : 'مشروع'}
        subtitle={
          item
            ? `${item.code}${item.description ? ` — ${item.description}` : ''}`
            : 'تفاصيل المشروع'
        }
        tone="violet"
        actions={
          <>
            {item && (
              <Link href={`/projects/${id}/edit`}>
                <Button variant="secondary" iconLeft={<Pencil className="h-4 w-4" />}>
                  تعديل
                </Button>
              </Link>
            )}
            <Link href="/projects">
              <Button variant="ghost" iconLeft={<ArrowRight className="h-4 w-4" />}>
                العودة
              </Button>
            </Link>
          </>
        }
        highlight={
          item && (pnl || wip)
            ? { label: 'صافي الربح', value: formatCurrency(pnl?.grossProfit ?? 0) }
            : item
              ? { label: 'الميزانية', value: formatCurrency(item.budget) }
              : undefined
        }
      />

      {error && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-rose-700" role="alert">
          <p className="font-semibold">تعذّر تحميل المشروع</p>
          <p className="mt-1 text-sm">{error}</p>
        </div>
      )}

      {!item ? (
        <SectionCard>
          <EmptyState
            icon={<FileText className="h-12 w-12" />}
            title="لم يتم العثور على المشروع"
            description="قد يكون المشروع محذوفاً أو ليس لديك صلاحية لعرضه."
            action={
              <Link href="/projects">
                <Button variant="primary">العودة إلى المشاريع</Button>
              </Link>
            }
          />
        </SectionCard>
      ) : (
        <>
          {/* Status + key info row */}
          <div className="grid grid-cols-1 gap-4 lg:grid-cols-4">
            <div className="lg:col-span-1">
              <SectionCard title="حالة المشروع">
                <div className="flex flex-col items-center gap-3 py-2">
                  <StatusPill
                    tone={status?.tone ?? 'slate'}
                    label={status?.label ?? String(item.status)}
                    showDot={false}
                  />
                  <p className="text-center text-xs text-gray-500">
                    {item.isActive ? 'المشروع فعّال' : 'المشروع موقوف'}
                  </p>
                </div>
              </SectionCard>
            </div>
            <div className="lg:col-span-3">
              <SectionCard title="معلومات سريعة">
                <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
                  <div>
                    <p className="text-[11px] font-bold uppercase tracking-wider text-gray-500">الكود</p>
                    <p className="mt-1 font-mono text-sm font-bold text-gray-900">{item.code}</p>
                  </div>
                  <div>
                    <p className="text-[11px] font-bold uppercase tracking-wider text-gray-500">الميزانية</p>
                    <p className="mt-1 font-mono text-sm font-bold text-gray-900">
                      {formatCurrency(item.budget)}
                    </p>
                  </div>
                  <div>
                    <p className="text-[11px] font-bold uppercase tracking-wider text-gray-500">تاريخ البدء</p>
                    <p className="mt-1 text-sm text-gray-900 tabular-nums">{formatDate(item.startDate)}</p>
                  </div>
                  <div>
                    <p className="text-[11px] font-bold uppercase tracking-wider text-gray-500">تاريخ النهاية</p>
                    <p className="mt-1 text-sm text-gray-900 tabular-nums">
                      {item.endDate ? formatDate(item.endDate) : '—'}
                    </p>
                  </div>
                </div>
              </SectionCard>
            </div>
          </div>

          {/* Tab nav as FilterChips */}
          <div className="flex flex-wrap items-center gap-2">
            {TAB_CHIPS.map((t) => (
              <button
                key={t.key}
                onClick={() => setTab(t.key as Tab)}
                className={
                  'flex items-center gap-2 rounded-xl px-4 py-2 text-sm font-bold transition ' +
                  (tab === t.key
                    ? 'bg-violet-600 text-white shadow-sm'
                    : 'bg-white text-gray-600 ring-1 ring-gray-200 hover:bg-gray-50')
                }
              >
                {t.icon}
                <span>{t.label}</span>
              </button>
            ))}
          </div>

          {tab === 'details' && <DetailsTab item={item} onReload={load} />}
          {tab === 'pnl' && <PnLTab projectId={id} onLoaded={(p, w) => { setPnl(p); setWip(w); }} />}
          {tab === 'contract' && <ContractTab projectId={id} onWipRefresh={() => undefined} />}
          {tab === 'billings' && <BillingsTab projectId={id} onChange={() => undefined} />}
          {tab === 'boq' && <BoqTab projectId={id} />}
          {tab === 'variations' && <VariationsTab projectId={id} />}
        </>
      )}
    </div>
  );
}

// =================== Tab: Details ===================

function DetailsTab({ item, onReload }: { item: Project; onReload: () => void }) {
  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <SectionCard title="المعلومات الأساسية" description="تفاصيل المشروع الكاملة">
        <dl className="space-y-3 text-sm">
          <Row label="كود المشروع" value={item.code} />
          <Row label="اسم المشروع" value={item.name} />
          <Row label="الحالة" value={STATUS_META[item.status]?.label ?? String(item.status)} />
          <Row label="الميزانية" value={formatCurrency(item.budget)} />
          <Row label="تاريخ البدء" value={formatDate(item.startDate)} />
          <Row label="تاريخ النهاية" value={item.endDate ? formatDate(item.endDate) : '—'} />
          <Row label="فعّال" value={item.isActive ? 'نعم' : 'لا'} />
          <Row label="تاريخ الإنشاء" value={formatDate(item.createdAt)} />
        </dl>
      </SectionCard>
      <SectionCard title="الوصف والإجراءات" description="ملاحظات + روابط سريعة">
        <div className="space-y-3">
          {item.description ? (
            <div className="rounded-lg bg-slate-50 p-3 text-sm text-gray-700">
              {item.description}
            </div>
          ) : (
            <p className="text-sm text-gray-500">لا يوجد وصف للمشروع.</p>
          )}
          <div className="space-y-2 border-t border-gray-100 pt-3">
            <Button variant="secondary" onClick={onReload} iconLeft={<RefreshCw className="h-4 w-4" />} className="w-full">
              إعادة تحميل
            </Button>
            <Link href={`/projects/${item.id}/edit`}>
              <Button variant="primary" iconLeft={<Pencil className="h-4 w-4" />} className="w-full">
                تعديل المشروع
              </Button>
            </Link>
          </div>
        </div>
      </SectionCard>
    </div>
  );
}

function Row({ label, value }: { label: string; value?: string }) {
  return (
    <div className="flex items-center justify-between gap-2 border-b border-gray-100 pb-2 last:border-0 last:pb-0">
      <dt className="text-xs text-gray-500">{label}</dt>
      <dd className="text-end font-mono text-xs font-medium text-gray-900 break-all">
        {value && value.length > 0 ? value : '—'}
      </dd>
    </div>
  );
}

// =================== Tab: P&L ===================

function PnLTab({ projectId, onLoaded }: { projectId: string; onLoaded: (p: ProjectPnL | null, w: ProjectWip | null) => void }) {
  const today = new Date().toISOString().slice(0, 10);
  const yearStart = `${new Date().getFullYear() - 1}-01-01`;

  const [from, setFrom] = useState<string>(yearStart);
  const [to, setTo] = useState<string>(today);
  const [pnl, setPnl] = useState<ProjectPnL | null>(null);
  const [wip, setWip] = useState<ProjectWip | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const [pnlData, wipData] = await Promise.all([
        projectsApi.getProjectPnL(projectId, from || undefined, to || undefined).catch(() => null),
        projectsApi.getProjectWip(projectId).catch(() => null),
      ]);
      setPnl(pnlData);
      setWip(wipData);
      onLoaded(pnlData, wipData);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل الأرباح والخسائر.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [projectId]);

  const isProfit = useMemo(() => (pnl?.grossProfit ?? 0) >= 0, [pnl]);

  return (
    <div className="space-y-4">
      {/* Date filter */}
      <SectionCard>
        <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
          <div className="flex-1">
            <label className="mb-1 flex items-center gap-1 text-xs text-gray-600">
              <Calendar className="h-3 w-3" /> من تاريخ
            </label>
            <Input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
          </div>
          <div className="flex-1">
            <label className="mb-1 flex items-center gap-1 text-xs text-gray-600">
              <Calendar className="h-3 w-3" /> إلى تاريخ
            </label>
            <Input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
          </div>
          <Button variant="primary" onClick={load} disabled={loading}>
            {loading ? 'جاري التحميل…' : 'تحديث'}
          </Button>
        </div>
      </SectionCard>

      {error && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-rose-700" role="alert">
          <p className="font-semibold">تعذّر تحميل P&L</p>
          <p className="mt-1 text-sm">{error}</p>
        </div>
      )}

      {loading && !pnl ? (
        <SkeletonTable rows={4} cols={3} />
      ) : !pnl ? null : (
        <>
          {/* KPI strip */}
          <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
            <StatCard
              label="الإيرادات"
              value={formatCurrency(pnl.totalRevenue)}
              icon={TrendingUp}
              tone="green"
              hint={`${pnl.invoiceCount} فاتورة`}
            />
            <StatCard
              label="التكاليف"
              value={formatCurrency(pnl.totalCosts)}
              icon={TrendingDown}
              tone="red"
              hint={`${pnl.costEntryCount} قيد محاسبي`}
            />
            <StatCard
              label="صافي الربح"
              value={formatCurrency(pnl.grossProfit)}
              icon={isProfit ? TrendingUp : TrendingDown}
              tone={isProfit ? 'green' : 'red'}
              hint="Revenue − Costs"
            />
            <StatCard
              label="هامش الربح"
              value={`${pnl.profitMarginPercent.toFixed(2)}%`}
              icon={Calculator}
              tone={isProfit ? 'violet' : 'red'}
              hint="Profit / Revenue"
            />
          </div>

          {/* WIP card (DEC-165) */}
          {wip && (
            <div className="overflow-hidden rounded-2xl bg-white shadow-sm ring-1 ring-amber-200">
              <div className="flex items-center gap-2 border-b border-amber-200 bg-amber-50 px-5 py-3">
                <Package className="h-5 w-5 text-amber-700" />
                <h3 className="text-sm font-bold text-amber-900">WIP — العمل قيد التنفيذ (DEC-165)</h3>
                <span className="ms-auto text-xs text-amber-700">{wip.statusName}</span>
              </div>
              <div className="grid grid-cols-2 gap-4 p-5 md:grid-cols-4">
                <WipItem label="إجمالي التكاليف" value={formatCurrency(wip.totalCosts)} />
                <WipItem label="إجمالي المفوتر (صافي)" value={formatCurrency(wip.totalBilledNet)} />
                <WipItem label="احتجاز محتجز" value={formatCurrency(wip.totalRetentionHeld)} />
                <div>
                  <p className="text-[11px] font-bold uppercase tracking-wider text-gray-500">WIP (تكاليف − مفوتر)</p>
                  <p className={`mt-1 font-mono text-base font-bold ${wip.wip > 0 ? 'text-amber-700' : wip.wip < 0 ? 'text-rose-700' : 'text-emerald-700'}`}>
                    {formatCurrency(wip.wip)}
                  </p>
                </div>
              </div>
              <div className="border-t border-amber-200 bg-amber-50/50 px-5 py-2.5 text-xs text-amber-800">
                {wip.wip > 0 && '↗ العمل جاري والفوترة متأخرة (طبيعي في المشاريع طويلة المدى)'}
                {wip.wip < 0 && '⚠ فوترنا أكثر مما أنفقنا — تحقق من المستخلصات'}
                {wip.wip === 0 && '✓ التكاليف = المفوتر بالضبط'}
              </div>
            </div>
          )}

          {/* Costs by account table */}
          <SectionCard
            title="تفصيل التكاليف حسب الحساب"
            description={`من ${pnl.from ? formatDate(pnl.from) : 'البداية'} إلى ${pnl.to ? formatDate(pnl.to) : 'اليوم'}`}
          >
            {pnl.costsByAccount.length === 0 ? (
              <EmptyState
                icon={<Coins className="h-10 w-10" />}
                title="لا توجد تكاليف"
                description="لا توجد تكاليف على هذا المشروع في النطاق الزمني المحدد."
              />
            ) : (
              <ModernTable
                columns={[
                  { key: 'name', header: 'الحساب', render: (c) => (
                    <div>
                      <p className="font-semibold text-gray-900">{c.accountName}</p>
                      <p className="font-mono text-[11px] text-gray-500">{c.accountCode}</p>
                    </div>
                  )},
                  { key: 'amount', header: 'المبلغ', align: 'end', render: (c) => (
                    <span className="font-mono font-bold text-rose-700 tabular-nums">
                      {formatCurrency(c.amount)}
                    </span>
                  )},
                  { key: 'pct', header: '%', align: 'end', widthClass: 'w-28', render: (c) => {
                    const pct = pnl.totalCosts > 0 ? (c.amount / pnl.totalCosts) * 100 : 0;
                    return (
                      <div className="flex items-center gap-2">
                        <ProgressBar value={pct} max={100} tone="red" showValue={false} />
                        <span className="font-mono text-xs tabular-nums text-gray-600">
                          {pct.toFixed(1)}%
                        </span>
                      </div>
                    );
                  }},
                ]}
                rows={pnl.costsByAccount}
                rowKey={(c) => c.accountCode}
              />
            )}
          </SectionCard>
        </>
      )}
    </div>
  );
}

function WipItem({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-[11px] font-bold uppercase tracking-wider text-gray-500">{label}</p>
      <p className="mt-1 font-mono text-base font-bold text-amber-900">{value}</p>
    </div>
  );
}

// =================== Tab: Contract (DEC-163) ===================

function ContractTab({ projectId, onWipRefresh }: { projectId: string; onWipRefresh: () => void }) {
  const [contract, setContract] = useState<Contract | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [editing, setEditing] = useState<Contract | null>(null);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await projectsApi.getContract(projectId);
      setContract(data);
    } catch (e: unknown) {
      const msg = getErrorMessage(e, '');
      if (msg.includes('404') || msg.includes('Not Found') || msg === '') {
        setContract(null);
      } else {
        setError(msg);
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [projectId]);

  const handleDelete = async () => {
    if (!contract) return;
    if (!confirm(`حذف العقد ${contract.contractNumber ?? contract.id}؟`)) return;
    try {
      await projectsApi.deleteContract(contract.id);
      setContract(null);
      onWipRefresh();
    } catch (e: unknown) {
      alert('فشل الحذف: ' + getErrorMessage(e, ''));
    }
  };

  return (
    <div className="space-y-4">
      {error && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-rose-700" role="alert">
          <p className="font-semibold">تعذّر تحميل العقد</p>
          <p className="mt-1 text-sm">{error}</p>
        </div>
      )}

      {loading ? (
        <SkeletonTable rows={4} cols={3} />
      ) : !contract ? (
        <SectionCard>
          <EmptyState
            icon={<FileSignature className="h-12 w-12" />}
            title="لا يوجد عقد على هذا المشروع"
            description="أضف عقداً لتفعيل المستخلصات وحساب WIP. العقد يحدد قيمة المشروع ونسبة الدفعة المقدمة والاحتجاز."
            action={
              <Button
                variant="primary"
                onClick={() => { setEditing(null); setShowModal(true); }}
                iconLeft={<Plus className="h-4 w-4" />}
              >
                إنشاء عقد
              </Button>
            }
          />
        </SectionCard>
      ) : (
        <>
          {/* Contract summary stats */}
          <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
            <StatCard label="قيمة العقد" value={formatCurrency(contract.contractValue)} icon={Wallet} tone="blue" />
            <StatCard label="دفعة مقدمة" value={`${contract.advancePercent}%`} icon={Coins} tone="amber" />
            <StatCard label="احتجاز" value={`${contract.retentionPercent}%`} icon={Calculator} tone="violet" hint={`يبدأ من مستخلص #${contract.retentionStartBilling}`} />
            <StatCard
              label="الحالة"
              value={contract.isActive ? 'فعّال' : 'موقوف'}
              icon={FileSignature}
              tone={contract.isActive ? 'green' : 'slate'}
            />
          </div>

          <SectionCard
            title="تفاصيل العقد"
            description={contract.contractNumber ?? `بدون رقم — ${contract.id}`}
            actions={
              <div className="flex gap-2">
                <Button variant="secondary" onClick={() => { setEditing(contract); setShowModal(true); }}>
                  تعديل
                </Button>
                <Button variant="ghost" onClick={handleDelete} iconLeft={<Trash2 className="h-4 w-4" />}>
                  حذف
                </Button>
              </div>
            }
          >
            <dl className="grid grid-cols-1 gap-4 md:grid-cols-2">
              <Row label="رقم العقد" value={contract.contractNumber ?? '—'} />
              <Row label="قيمة العقد" value={formatCurrency(contract.contractValue)} />
              <Row label="نسبة الدفعة المقدمة" value={`${contract.advancePercent}%`} />
              <Row label="نسبة الاحتجاز" value={`${contract.retentionPercent}%`} />
              <Row label="بداية الاحتجاز من مستخلص #" value={String(contract.retentionStartBilling)} />
              <Row label="تاريخ البداية" value={contract.startDate ? formatDate(contract.startDate) : '—'} />
              <Row label="تاريخ النهاية" value={contract.endDate ? formatDate(contract.endDate) : '—'} />
              <Row label="تاريخ الإنشاء" value={formatDate(contract.createdAt)} />
              {contract.notes && (
                <div className="md:col-span-2">
                  <p className="text-xs text-gray-500">ملاحظات</p>
                  <p className="mt-1 rounded-lg bg-slate-50 p-3 text-sm text-gray-700">
                    {contract.notes}
                  </p>
                </div>
              )}
            </dl>
          </SectionCard>
        </>
      )}

      {showModal && (
        <ContractModal
          projectId={projectId}
          existing={editing}
          onClose={() => setShowModal(false)}
          onSaved={async () => { setShowModal(false); await load(); }}
        />
      )}
    </div>
  );
}

function ContractModal({ projectId, existing, onClose, onSaved }: {
  projectId: string;
  existing: Contract | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [form, setForm] = useState<CreateContractRequest>(existing ? {
    contractNumber: existing.contractNumber ?? '',
    contractValue: existing.contractValue,
    advancePercent: existing.advancePercent,
    retentionPercent: existing.retentionPercent,
    retentionStartBilling: existing.retentionStartBilling,
    startDate: existing.startDate,
    endDate: existing.endDate,
    notes: existing.notes ?? '',
  } : {
    contractNumber: '',
    contractValue: 0,
    advancePercent: 10,
    retentionPercent: 5,
    retentionStartBilling: 1,
    startDate: new Date().toISOString().slice(0, 10),
    endDate: undefined,
    notes: '',
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true); setError(null);
    try {
      if (existing) {
        const data: UpdateContractRequest = form;
        await projectsApi.updateContract(existing.id, data);
      } else {
        await projectsApi.createContract(projectId, form);
      }
      onSaved();
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل الحفظ.'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal open onClose={onClose} title={existing ? 'تعديل العقد' : 'إنشاء عقد جديد'}>
      <form onSubmit={handleSubmit} className="space-y-3">
        {error && <div className="rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</div>}
        <div>
          <label className="mb-1 block text-xs text-gray-600">رقم العقد (اختياري)</label>
          <Input value={form.contractNumber ?? ''} onChange={(e) => setForm({ ...form, contractNumber: e.target.value })} />
        </div>
        <div>
          <label className="mb-1 block text-xs text-gray-600">قيمة العقد (LYD) *</label>
          <Input type="number" step="0.0001" required value={form.contractValue}
            onChange={(e) => setForm({ ...form, contractValue: parseFloat(e.target.value) || 0 })} />
        </div>
        <div className="grid grid-cols-3 gap-2">
          <div>
            <label className="mb-1 block text-xs text-gray-600">دفعة مقدمة %</label>
            <Input type="number" step="0.01" min="0" max="100" value={form.advancePercent}
              onChange={(e) => setForm({ ...form, advancePercent: parseFloat(e.target.value) || 0 })} />
          </div>
          <div>
            <label className="mb-1 block text-xs text-gray-600">احتجاز %</label>
            <Input type="number" step="0.01" min="0" max="100" value={form.retentionPercent}
              onChange={(e) => setForm({ ...form, retentionPercent: parseFloat(e.target.value) || 0 })} />
          </div>
          <div>
            <label className="mb-1 block text-xs text-gray-600">احتجاز من #</label>
            <Input type="number" min="1" value={form.retentionStartBilling ?? 1}
              onChange={(e) => setForm({ ...form, retentionStartBilling: parseInt(e.target.value) || 1 })} />
          </div>
        </div>
        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className="mb-1 block text-xs text-gray-600">تاريخ البداية</label>
            <Input type="date" value={form.startDate ?? ''} onChange={(e) => setForm({ ...form, startDate: e.target.value })} />
          </div>
          <div>
            <label className="mb-1 block text-xs text-gray-600">تاريخ النهاية</label>
            <Input type="date" value={form.endDate ?? ''} onChange={(e) => setForm({ ...form, endDate: e.target.value })} />
          </div>
        </div>
        <div>
          <label className="mb-1 block text-xs text-gray-600">ملاحظات</label>
          <Input value={form.notes ?? ''} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
        </div>
        <div className="flex justify-end gap-2 border-t border-gray-100 pt-3">
          <Button type="button" variant="secondary" onClick={onClose}>إلغاء</Button>
          <Button type="submit" variant="primary" disabled={saving}>{saving ? 'جاري الحفظ…' : 'حفظ'}</Button>
        </div>
      </form>
    </Modal>
  );
}

// =================== Tab: Billings (DEC-164) ===================

function BillingsTab({ projectId, onChange }: { projectId: string; onChange: () => void }) {
  const [contract, setContract] = useState<Contract | null>(null);
  const [billings, setBillings] = useState<ProgressBilling[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);
  const [tab, setTab] = useState<'all' | 1 | 2 | 3>('all');

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const [c, b] = await Promise.all([
        projectsApi.getContract(projectId).catch(() => null),
        projectsApi.getBillings(projectId),
      ]);
      setContract(c);
      setBillings(Array.isArray(b) ? b : []);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل التحميل.'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [projectId]);

  const handleApprove = async (id: string) => {
    if (!confirm('ترحيل المستخلص؟ سينشئ فاتورة وقيد محاسبي تلقائياً.')) return;
    try {
      await projectsApi.approveBilling(id);
      await load();
      onChange();
    } catch (e: unknown) {
      alert('فشل الترحيل: ' + getErrorMessage(e, ''));
    }
  };

  const handleCancel = async (id: string) => {
    if (!confirm('إلغاء هذا المستخلص (مسودة)؟')) return;
    try {
      await projectsApi.cancelBilling(id);
      await load();
      onChange();
    } catch (e: unknown) {
      alert('فشل الإلغاء: ' + getErrorMessage(e, ''));
    }
  };

  const counts = useMemo(() => {
    const c = { all: billings.length, 1: 0, 2: 0, 3: 0 };
    for (const b of billings) c[b.status as 1 | 2 | 3] = (c[b.status as 1 | 2 | 3] ?? 0) + 1;
    return c;
  }, [billings]);

  const filtered = useMemo(() => {
    if (tab === 'all') return billings;
    return billings.filter((b) => b.status === tab);
  }, [billings, tab]);

  const totalGross = useMemo(() => billings.reduce((s, b) => s + Number(b.grossAmount || 0), 0), [billings]);
  const totalNet = useMemo(() => billings.reduce((s, b) => s + Number(b.netAmount || 0), 0), [billings]);
  const totalAdvance = useMemo(() => billings.reduce((s, b) => s + Number(b.advanceDeducted || 0), 0), [billings]);
  const totalRetention = useMemo(() => billings.reduce((s, b) => s + Number(b.retentionDeducted || 0), 0), [billings]);

  const columns: ModernTableColumn<ProgressBilling>[] = [
    { key: 'num', header: 'رقم', widthClass: 'w-32', render: (b) => (
      <span className="font-mono text-xs font-bold text-gray-900">{b.billingNumber}</span>
    )},
    { key: 'date', header: 'تاريخ', widthClass: 'w-32', render: (b) => (
      <span className="text-xs tabular-nums text-gray-600">{formatDate(b.billingDate)}</span>
    )},
    { key: 'pct', header: '% إنجاز', align: 'end', widthClass: 'w-28', render: (b) => (
      <div className="flex items-center justify-end gap-2">
        <span className="font-mono text-xs tabular-nums">{b.workCompletedPercent.toFixed(2)}%</span>
      </div>
    )},
    { key: 'gross', header: 'إجمالي', align: 'end', render: (b) => (
      <span className="font-mono text-xs text-gray-900">{formatCurrency(b.grossAmount)}</span>
    )},
    { key: 'adv', header: 'دفعة مقدمة', align: 'end', render: (b) => (
      <span className="font-mono text-xs text-orange-600">{formatCurrency(b.advanceDeducted)}</span>
    )},
    { key: 'ret', header: 'احتجاز', align: 'end', render: (b) => (
      <span className="font-mono text-xs text-violet-600">{formatCurrency(b.retentionDeducted)}</span>
    )},
    { key: 'net', header: 'صافي', align: 'end', render: (b) => (
      <span className="font-mono text-xs font-bold text-emerald-700">{formatCurrency(b.netAmount)}</span>
    )},
    { key: 'status', header: 'الحالة', align: 'center', widthClass: 'w-32', render: (b) => <BillingStatusPill status={b.status} /> },
    { key: 'actions', header: '', align: 'end', widthClass: 'w-24', render: (b) => (
      <div className="flex items-center justify-end gap-1" onClick={(e) => e.stopPropagation()}>
        {b.status === 1 && (
          <>
            <button
              onClick={() => handleApprove(b.id)}
              title="ترحيل"
              className="rounded p-1 text-emerald-600 hover:bg-emerald-50"
            >
              <CheckCircle className="h-4 w-4" />
            </button>
            <button
              onClick={() => handleCancel(b.id)}
              title="إلغاء"
              className="rounded p-1 text-rose-600 hover:bg-rose-50"
            >
              <XCircle className="h-4 w-4" />
            </button>
          </>
        )}
      </div>
    )},
  ];

  return (
    <div className="space-y-4">
      {error && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 p-4 text-rose-700" role="alert">
          <p className="font-semibold">تعذّر تحميل المستخلصات</p>
          <p className="mt-1 text-sm">{error}</p>
        </div>
      )}

      {!contract ? (
        <SectionCard>
          <EmptyState
            icon={<AlertTriangle className="h-12 w-12 text-amber-400" />}
            title="لا يوجد عقد"
            description="أنشئ عقداً أولاً قبل إضافة المستخلصات."
            action={
              <Link href={`/projects/${projectId}`}>
                <Button variant="primary" onClick={() => undefined}>
                  العودة لتبويب العقد
                </Button>
              </Link>
            }
          />
        </SectionCard>
      ) : (
        <>
          {/* Billing summary KPIs */}
          <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
            <StatCard
              label="عدد المستخلصات"
              value={billings.length.toLocaleString('en-US')}
              icon={Receipt}
              tone="blue"
              hint={`${counts[2] ?? 0} مُرحّل · ${counts[1] ?? 0} مسودة`}
            />
            <StatCard
              label="إجمالي المفوتر (صافي)"
              value={formatCurrency(totalNet)}
              icon={TrendingUp}
              tone="green"
              hint={`إجمالي: ${formatCurrency(totalGross)}`}
            />
            <StatCard
              label="إجمالي الدفعة المقدمة"
              value={formatCurrency(totalAdvance)}
              icon={Coins}
              tone="amber"
              hint="يُخصم مرة واحدة"
            />
            <StatCard
              label="إجمالي الاحتجاز"
              value={formatCurrency(totalRetention)}
              icon={Calculator}
              tone="violet"
              hint="محتجز حتى التسليم النهائي"
            />
          </div>

          {/* Status filter + add button */}
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <FilterChips
              chips={[
                { key: 'all', label: 'الكل', count: counts.all, tone: 'blue' },
                { key: '1', label: 'مسودة', count: counts[1] ?? 0, tone: 'amber' },
                { key: '2', label: 'مُرحّل', count: counts[2] ?? 0, tone: 'green' },
                { key: '3', label: 'ملغى', count: counts[3] ?? 0, tone: 'red' },
              ]}
              active={String(tab)}
              onChange={(k) => setTab(k === 'all' ? 'all' : (Number(k) as 1 | 2 | 3))}
            />
            <Button variant="primary" onClick={() => setShowModal(true)} iconLeft={<Plus className="h-4 w-4" />}>
              مستخلص جديد
            </Button>
          </div>

          {/* Billings table */}
          {loading ? (
            <SkeletonTable rows={4} cols={7} />
          ) : billings.length === 0 ? (
            <SectionCard>
              <EmptyState
                icon={<Receipt className="h-12 w-12" />}
                title="لا توجد مستخلصات"
                description="ابدأ بإنشاء أول مستخلص لتتبع الفوترة على هذا العقد."
                action={
                  <Button
                    variant="primary"
                    onClick={() => setShowModal(true)}
                    iconLeft={<Plus className="h-4 w-4" />}
                  >
                    مستخلص جديد
                  </Button>
                }
              />
            </SectionCard>
          ) : (
            <SectionCard
              flush
              title={`مستخلصات العقد (${filtered.length})`}
              description={`قيمة العقد: ${formatCurrency(contract.contractValue)}`}
            >
              <ModernTable
                columns={columns}
                rows={filtered}
                rowKey={(b) => b.id}
              />
            </SectionCard>
          )}
        </>
      )}

      {showModal && contract && (
        <BillingModal
          projectId={projectId}
          contract={contract}
          onClose={() => setShowModal(false)}
          onSaved={async () => { setShowModal(false); await load(); onChange(); }}
        />
      )}
    </div>
  );
}

function BillingStatusPill({ status }: { status: number }) {
  if (status === 1) return <StatusPill tone="amber" label="مسودة" showDot={false} />;
  if (status === 2) return <StatusPill tone="green" label="مُرحّل" showDot={false} />;
  if (status === 3) return <StatusPill tone="red" label="ملغى" showDot={false} />;
  return <StatusPill tone="slate" label={String(status)} showDot={false} />;
}

function BillingModal({ projectId, contract, onClose, onSaved }: {
  projectId: string;
  contract: Contract;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [form, setForm] = useState<CreateBillingRequest>({
    billingNumber: `B-${new Date().getFullYear()}-${String(Math.floor(Math.random() * 999) + 1).padStart(3, '0')}`,
    billingDate: new Date().toISOString().slice(0, 10),
    workCompletedPercent: 0,
    notes: '',
  });
  const [preview, setPreview] = useState<BillingPreview | null>(null);
  const [previewing, setPreviewing] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Live preview (debounced) — L118
  useEffect(() => {
    if (!form.workCompletedPercent || form.workCompletedPercent <= 0) {
      setPreview(null);
      return;
    }
    const t = setTimeout(async () => {
      setPreviewing(true);
      try {
        const p = await projectsApi.previewBilling(contract.id, form.workCompletedPercent);
        setPreview(p);
      } catch {
        setPreview(null);
      } finally {
        setPreviewing(false);
      }
    }, 300);
    return () => clearTimeout(t);
  }, [form.workCompletedPercent, contract.id]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true); setError(null);
    try {
      await projectsApi.createBilling(projectId, form);
      onSaved();
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل الحفظ.'));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal open onClose={onClose} title="مستخلص جديد" size="lg">
      <form onSubmit={handleSubmit} className="space-y-3">
        {error && <div className="rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</div>}
        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className="mb-1 block text-xs text-gray-600">رقم المستخلص *</label>
            <Input required value={form.billingNumber} onChange={(e) => setForm({ ...form, billingNumber: e.target.value })} />
          </div>
          <div>
            <label className="mb-1 block text-xs text-gray-600">تاريخ المستخلص *</label>
            <Input type="date" required value={form.billingDate} onChange={(e) => setForm({ ...form, billingDate: e.target.value })} />
          </div>
        </div>
        <div>
          <label className="mb-1 block text-xs text-gray-600">نسبة الإنجاز التراكمية % (0-100) *</label>
          <Input type="number" step="0.01" min="0.01" max="100" required
            value={form.workCompletedPercent}
            onChange={(e) => setForm({ ...form, workCompletedPercent: parseFloat(e.target.value) || 0 })} />
          {preview && (
            <div className="mt-1 text-xs text-gray-500">
              أعلى نسبة سابقة: {preview.previousMaxPercent.toFixed(2)}% — رقم المستخلص الجديد: #{preview.nextBillingNumber}
            </div>
          )}
        </div>
        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className="mb-1 block text-xs text-gray-600">من تاريخ</label>
            <Input type="date" value={form.periodFrom ?? ''} onChange={(e) => setForm({ ...form, periodFrom: e.target.value })} />
          </div>
          <div>
            <label className="mb-1 block text-xs text-gray-600">إلى تاريخ</label>
            <Input type="date" value={form.periodTo ?? ''} onChange={(e) => setForm({ ...form, periodTo: e.target.value })} />
          </div>
        </div>

        {/* Live preview card */}
        <div className="rounded-lg border border-blue-200 bg-blue-50 p-3">
          <div className="mb-2 flex items-center gap-1 text-xs font-bold text-blue-800">
            <BarChart3 className="h-3 w-3" /> معاينة المستخلص (Live Preview)
            {previewing && <span className="text-xs text-gray-500">— يحسب…</span>}
          </div>
          {preview ? (
            <div className="grid grid-cols-2 gap-2 text-sm md:grid-cols-4">
              <div>
                <div className="text-xs text-gray-500">إجمالي</div>
                <div className="font-mono font-bold text-gray-800">{formatCurrency(preview.grossAmount)}</div>
              </div>
              <div>
                <div className="text-xs text-gray-500">دفعة مقدمة</div>
                <div className="font-mono text-orange-600">{formatCurrency(preview.advanceDeducted)}</div>
              </div>
              <div>
                <div className="text-xs text-gray-500">احتجاز</div>
                <div className="font-mono text-violet-600">{formatCurrency(preview.retentionDeducted)}</div>
              </div>
              <div>
                <div className="text-xs text-gray-500">صافي</div>
                <div className="font-mono font-bold text-emerald-700">{formatCurrency(preview.netAmount)}</div>
              </div>
            </div>
          ) : (
            <div className="text-xs text-gray-500">أدخل نسبة الإنجاز لمشاهدة الأرقام المتوقعة.</div>
          )}
        </div>

        <div>
          <label className="mb-1 block text-xs text-gray-600">ملاحظات</label>
          <Input value={form.notes ?? ''} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
        </div>
        <div className="flex justify-end gap-2 border-t border-gray-100 pt-2">
          <Button type="button" variant="secondary" onClick={onClose}>إلغاء</Button>
          <Button type="submit" variant="primary" disabled={saving || !preview}>
            {saving ? 'جاري الحفظ…' : 'حفظ كمسودة'}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

// =================== Sprint 59 (DEC-181): Tab: BOQ (كراسة الحصر) ===================
function BoqTab({ projectId }: { projectId: string }) {
  const [sections, setSections] = useState<BoqSectionDto[]>([]);
  const [lines, setLines] = useState<BoqLineDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showSectionForm, setShowSectionForm] = useState(false);
  const [showLineForm, setShowLineForm] = useState<string | null>(null); // sectionId for new line
  const [sectionCode, setSectionCode] = useState('');
  const [sectionName, setSectionName] = useState('');
  const [units, setUnits] = useState<Array<{ id: string; code: string; name: string }>>([]);
  const [lineForm, setLineForm] = useState({
    code: '',
    description: '',
    unitId: '',
    contractQty: 0,
    unitPrice: 0,
    regionalPremiumPct: 0,
    isMeasurable: true,
  });
  const [subitemsByLine, setSubitemsByLine] = useState<Record<string, BoqSubitemDto[]>>({});
  const [expandedLines, setExpandedLines] = useState<Set<string>>(new Set());
  const [showSubitemForm, setShowSubitemForm] = useState<string | null>(null);
  const [subitemForm, setSubitemForm] = useState({
    description: '',
    count: 1,
    lengthM: 0,
    widthM: 0,
    heightM: 0,
    deductions: 0,
  });

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const [secs, lns, uoms] = await Promise.all([
        projectsApi.listBoqSections(projectId).catch(() => []),
        projectsApi.listBoqLines(projectId).catch(() => []),
        api.get<Array<{ id: string; code: string; name: string }>>('/api/inventory/uom').then(r => r.data).catch(() => []),
      ]);
      setSections(secs);
      setLines(lns);
      setUnits(uoms);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل كراسة الحصر.'));
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, [projectId]);

  const createSection = async () => {
    if (!sectionCode.trim() || !sectionName.trim()) return;
    try {
      await projectsApi.createBoqSection(projectId, { code: sectionCode, name: sectionName });
      setSectionCode(''); setSectionName('');
      setShowSectionForm(false);
      await load();
    } catch (e: unknown) { setError(getErrorMessage(e, 'فشل إنشاء القسم.')); }
  };

  const createLine = async () => {
    if (!showLineForm || !lineForm.code.trim() || !lineForm.description.trim() || !lineForm.unitId) return;
    try {
      await projectsApi.createBoqLine(projectId, {
        sectionId: showLineForm,
        code: lineForm.code,
        description: lineForm.description,
        unitId: lineForm.unitId,
        contractQty: Number(lineForm.contractQty),
        unitPrice: Number(lineForm.unitPrice),
        regionalPremiumPct: Number(lineForm.regionalPremiumPct),
        isMeasurable: lineForm.isMeasurable,
      });
      setLineForm({ code: '', description: '', unitId: '', contractQty: 0, unitPrice: 0, regionalPremiumPct: 0, isMeasurable: true });
      setShowLineForm(null);
      await load();
    } catch (e: unknown) { setError(getErrorMessage(e, 'فشل إنشاء البند.')); }
  };

  const toggleSubitems = async (lineId: string) => {
    const next = new Set(expandedLines);
    if (next.has(lineId)) {
      next.delete(lineId);
    } else {
      next.add(lineId);
      if (!subitemsByLine[lineId]) {
        try {
          const subs = await projectsApi.listBoqSubitems(projectId, lineId);
          setSubitemsByLine(s => ({ ...s, [lineId]: subs }));
        } catch { /* ignore */ }
      }
    }
    setExpandedLines(next);
  };

  const createSubitem = async (lineId: string) => {
    try {
      await projectsApi.createBoqSubitem(projectId, {
        boqLineId: lineId,
        description: subitemForm.description,
        count: Number(subitemForm.count),
        lengthM: Number(subitemForm.lengthM),
        widthM: Number(subitemForm.widthM),
        heightM: Number(subitemForm.heightM),
        deductions: Number(subitemForm.deductions),
      });
      setSubitemForm({ description: '', count: 1, lengthM: 0, widthM: 0, heightM: 0, deductions: 0 });
      setShowSubitemForm(null);
      // Refresh this line's subitems
      const subs = await projectsApi.listBoqSubitems(projectId, lineId);
      setSubitemsByLine(s => ({ ...s, [lineId]: subs }));
    } catch (e: unknown) { setError(getErrorMessage(e, 'فشل إضافة المقاس.')); }
  };

  if (loading) return <SkeletonTable rows={4} cols={4} />;

  const linesFor = (sectionId: string) => lines.filter(l => l.sectionId === sectionId);

  return (
    <div className="space-y-4">
      <SectionCard
        title="كراسة الحصر (BOQ)"
        description="الأقسام ← البنود ← المقاسات الفرعية (L×W×H). الكمية الإجمالية تُحسب من المقاسات تلقائياً."
        actions={
          <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />} onClick={() => setShowSectionForm(true)}>
            قسم جديد
          </Button>
        }
      >
        {error && <div className="mb-3 rounded-lg border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">{error}</div>}
        {sections.length === 0 ? (
          <EmptyState
            icon={<ClipboardList className="h-12 w-12" />}
            title="لا توجد أقسام بعد"
            description="ابدأ بإنشاء قسم (مثال: أعمال الحفر، أعمال الخرسانة، أعمال التشطيبات) ثم أضف البنود داخله."
            action={
              <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />} onClick={() => setShowSectionForm(true)}>
                إنشاء أول قسم
              </Button>
            }
          />
        ) : (
          <div className="space-y-4">
            {sections.map(sec => {
              const secLines = linesFor(sec.id);
              const secTotal = secLines.reduce((s, l) => s + Number(l.totalAmount || 0), 0);
              return (
                <div key={sec.id} className="rounded-xl border border-gray-200 bg-white">
                  <div className="flex items-center justify-between border-b border-gray-100 bg-slate-50 px-4 py-3">
                    <div className="flex items-center gap-3">
                      <span className="inline-flex h-7 w-7 items-center justify-center rounded-md bg-violet-100 text-sm font-bold text-violet-700">{sec.code}</span>
                      <p className="font-bold text-gray-900">{sec.name}</p>
                      <span className="text-xs text-gray-500">{secLines.length} بند</span>
                    </div>
                    <div className="flex items-center gap-3">
                      <span className="font-mono text-sm font-semibold text-gray-900">
                        {secTotal.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} <span className="text-[10px] text-gray-500">ل.د</span>
                      </span>
                      <Button variant="secondary" size="sm" iconLeft={<Plus className="h-3.5 w-3.5" />} onClick={() => setShowLineForm(sec.id)}>
                        بند جديد
                      </Button>
                    </div>
                  </div>
                  {secLines.length === 0 ? (
                    <p className="px-4 py-6 text-center text-sm text-gray-500">لا توجد بنود في هذا القسم بعد.</p>
                  ) : (
                    <table className="w-full text-sm">
                      <thead className="bg-gray-50 text-xs uppercase text-gray-500">
                        <tr>
                          <th className="px-3 py-2 text-start">الكود</th>
                          <th className="px-3 py-2 text-start">الوصف</th>
                          <th className="px-3 py-2 text-end">الكمية</th>
                          <th className="px-3 py-2 text-start">الوحدة</th>
                          <th className="px-3 py-2 text-end">السعر</th>
                          <th className="px-3 py-2 text-end">السعر النهائي</th>
                          <th className="px-3 py-2 text-end">الإجمالي</th>
                          <th className="px-3 py-2 text-end"></th>
                        </tr>
                      </thead>
                      <tbody>
                        {secLines.map(l => {
                          const isOpen = expandedLines.has(l.id);
                          const subs = subitemsByLine[l.id] || [];
                          return (
                            <Fragment key={l.id}>
                              <tr className="border-t border-gray-100 hover:bg-gray-50">
                                <td className="px-3 py-2 font-mono text-xs font-bold text-slate-700">{l.code}</td>
                                <td className="px-3 py-2 text-gray-900">{l.description}</td>
                                <td className="px-3 py-2 text-end font-mono tabular-nums">{Number(l.contractQty).toLocaleString('en-US')}</td>
                                <td className="px-3 py-2 text-xs text-gray-600">{l.unitCode || '—'}</td>
                                <td className="px-3 py-2 text-end font-mono tabular-nums">{Number(l.unitPrice).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
                                <td className="px-3 py-2 text-end font-mono tabular-nums">{Number(l.finalUnitPrice).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
                                <td className="px-3 py-2 text-end font-mono font-semibold tabular-nums">{Number(l.totalAmount).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
                                <td className="px-3 py-2 text-end">
                                  <Button variant="ghost" size="sm" iconLeft={isOpen ? <XCircle className="h-3.5 w-3.5" /> : <ListChecks className="h-3.5 w-3.5" />} onClick={() => toggleSubitems(l.id)}>
                                    {isOpen ? 'إخفاء' : 'مقاسات'}
                                  </Button>
                                </td>
                              </tr>
                              {isOpen && (
                                <tr>
                                  <td colSpan={8} className="bg-amber-50/40 px-3 py-3">
                                    <div className="mb-2 flex items-center justify-between">
                                      <p className="text-xs font-bold text-gray-700">المقاسات الفرعية (L × W × H)</p>
                                      <Button variant="secondary" size="sm" iconLeft={<Plus className="h-3.5 w-3.5" />} onClick={() => setShowSubitemForm(l.id)}>
                                        إضافة مقاس
                                      </Button>
                                    </div>
                                    {subs.length === 0 ? (
                                      <p className="text-xs text-gray-500">لا توجد مقاسات فرعية — الكمية الحالية نهائية.</p>
                                    ) : (
                                      <table className="w-full text-xs">
                                        <thead className="text-gray-500">
                                          <tr>
                                            <th className="px-2 py-1 text-start">الوصف</th>
                                            <th className="px-2 py-1 text-end">العدد</th>
                                            <th className="px-2 py-1 text-end">الطول (م)</th>
                                            <th className="px-2 py-1 text-end">العرض (م)</th>
                                            <th className="px-2 py-1 text-end">الارتفاع (م)</th>
                                            <th className="px-2 py-1 text-end">الكمية الأولية</th>
                                            <th className="px-2 py-1 text-end">الخصومات</th>
                                            <th className="px-2 py-1 text-end">الكمية النهائية</th>
                                          </tr>
                                        </thead>
                                        <tbody>
                                          {subs.map(s => (
                                            <tr key={s.id} className="border-t border-amber-200/40">
                                              <td className="px-2 py-1 text-gray-800">{s.description}</td>
                                              <td className="px-2 py-1 text-end font-mono">{s.count}</td>
                                              <td className="px-2 py-1 text-end font-mono">{s.lengthM}</td>
                                              <td className="px-2 py-1 text-end font-mono">{s.widthM}</td>
                                              <td className="px-2 py-1 text-end font-mono">{s.heightM}</td>
                                              <td className="px-2 py-1 text-end font-mono">{Number(s.initialQty).toLocaleString('en-US', { maximumFractionDigits: 3 })}</td>
                                              <td className="px-2 py-1 text-end font-mono">{Number(s.deductions).toLocaleString('en-US', { maximumFractionDigits: 3 })}</td>
                                              <td className="px-2 py-1 text-end font-mono font-bold">{Number(s.finalQty).toLocaleString('en-US', { maximumFractionDigits: 3 })}</td>
                                            </tr>
                                          ))}
                                        </tbody>
                                      </table>
                                    )}
                                  </td>
                                </tr>
                              )}
                            </Fragment>
                          );
                        })}
                      </tbody>
                    </table>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </SectionCard>

      {/* Section create modal */}
      {showSectionForm && (
        <Modal open onClose={() => setShowSectionForm(false)} title="قسم جديد">
          <div className="space-y-3">
            <div>
              <label className="text-xs font-bold text-gray-600">الكود</label>
              <Input value={sectionCode} onChange={e => setSectionCode(e.target.value)} placeholder="A, B, C..." />
            </div>
            <div>
              <label className="text-xs font-bold text-gray-600">الاسم</label>
              <Input value={sectionName} onChange={e => setSectionName(e.target.value)} placeholder="أعمال الحفر والردم" />
            </div>
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="secondary" onClick={() => setShowSectionForm(false)}>إلغاء</Button>
              <Button variant="primary" onClick={createSection} disabled={!sectionCode || !sectionName}>إنشاء</Button>
            </div>
          </div>
        </Modal>
      )}

      {/* Line create modal */}
      {showLineForm && (
        <Modal open onClose={() => setShowLineForm(null)} title="بند جديد">
          <div className="space-y-3">
            <div className="grid grid-cols-2 gap-2">
              <div>
                <label className="text-xs font-bold text-gray-600">الكود</label>
                <Input value={lineForm.code} onChange={e => setLineForm({ ...lineForm, code: e.target.value })} placeholder="1.1.1" />
              </div>
              <div>
                <label className="text-xs font-bold text-gray-600">الوحدة</label>
                <select className="w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm" value={lineForm.unitId} onChange={e => setLineForm({ ...lineForm, unitId: e.target.value })}>
                  <option value="">— اختر وحدة —</option>
                  {units.map(u => <option key={u.id} value={u.id}>{u.code} — {u.name}</option>)}
                </select>
              </div>
            </div>
            <div>
              <label className="text-xs font-bold text-gray-600">الوصف</label>
              <Input value={lineForm.description} onChange={e => setLineForm({ ...lineForm, description: e.target.value })} placeholder="حفر في تربة عادية" />
            </div>
            <div className="grid grid-cols-3 gap-2">
              <div>
                <label className="text-xs font-bold text-gray-600">الكمية</label>
                <Input type="number" value={lineForm.contractQty} onChange={e => setLineForm({ ...lineForm, contractQty: Number(e.target.value) })} />
              </div>
              <div>
                <label className="text-xs font-bold text-gray-600">السعر</label>
                <Input type="number" step="0.01" value={lineForm.unitPrice} onChange={e => setLineForm({ ...lineForm, unitPrice: Number(e.target.value) })} />
              </div>
              <div>
                <label className="text-xs font-bold text-gray-600">% علاوة</label>
                <Input type="number" step="0.1" value={lineForm.regionalPremiumPct} onChange={e => setLineForm({ ...lineForm, regionalPremiumPct: Number(e.target.value) })} />
              </div>
            </div>
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="secondary" onClick={() => setShowLineForm(null)}>إلغاء</Button>
              <Button variant="primary" onClick={createLine} disabled={!lineForm.code || !lineForm.description || !lineForm.unitId}>إنشاء</Button>
            </div>
          </div>
        </Modal>
      )}

      {/* Subitem create modal */}
      {showSubitemForm && (
        <Modal open onClose={() => setShowSubitemForm(null)} title="مقاس فرعي (L×W×H)">
          <div className="space-y-3">
            <div>
              <label className="text-xs font-bold text-gray-600">الوصف</label>
              <Input value={subitemForm.description} onChange={e => setSubitemForm({ ...subitemForm, description: e.target.value })} placeholder="خندق الأساسات الرئيسي" />
            </div>
            <div className="grid grid-cols-4 gap-2">
              <div>
                <label className="text-xs font-bold text-gray-600">العدد</label>
                <Input type="number" value={subitemForm.count} onChange={e => setSubitemForm({ ...subitemForm, count: Number(e.target.value) })} />
              </div>
              <div>
                <label className="text-xs font-bold text-gray-600">الطول (م)</label>
                <Input type="number" step="0.01" value={subitemForm.lengthM} onChange={e => setSubitemForm({ ...subitemForm, lengthM: Number(e.target.value) })} />
              </div>
              <div>
                <label className="text-xs font-bold text-gray-600">العرض (م)</label>
                <Input type="number" step="0.01" value={subitemForm.widthM} onChange={e => setSubitemForm({ ...subitemForm, widthM: Number(e.target.value) })} />
              </div>
              <div>
                <label className="text-xs font-bold text-gray-600">الارتفاع (م)</label>
                <Input type="number" step="0.01" value={subitemForm.heightM} onChange={e => setSubitemForm({ ...subitemForm, heightM: Number(e.target.value) })} />
              </div>
            </div>
            <div>
              <label className="text-xs font-bold text-gray-600">الخصومات</label>
              <Input type="number" step="0.01" value={subitemForm.deductions} onChange={e => setSubitemForm({ ...subitemForm, deductions: Number(e.target.value) })} />
            </div>
            <div className="rounded-md bg-slate-50 p-2 text-xs text-gray-600">
              الكمية النهائية = العدد × الطول × العرض × الارتفاع − الخصومات ={' '}
              <span className="font-mono font-bold">{(subitemForm.count * subitemForm.lengthM * subitemForm.widthM * subitemForm.heightM - subitemForm.deductions).toLocaleString('en-US', { maximumFractionDigits: 3 })}</span>
            </div>
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="secondary" onClick={() => setShowSubitemForm(null)}>إلغاء</Button>
              <Button variant="primary" onClick={() => createSubitem(showSubitemForm)} disabled={!subitemForm.description}>إنشاء</Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
}

// =================== Sprint 59 (DEC-182): Tab: Variation Orders ===================
function VariationsTab({ projectId }: { projectId: string }) {
  const [items, setItems] = useState<VariationOrderDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({
    orderNumber: '',
    issuedAt: new Date().toISOString().slice(0, 10),
    reason: '',
    originalContractValue: 0,
    notes: '',
  });

  const load = async () => {
    setLoading(true); setError(null);
    try {
      const data = await projectsApi.listVariations(projectId);
      setItems(Array.isArray(data) ? data : []);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل تحميل الأوامر التعديلية.'));
    } finally { setLoading(false); }
  };

  useEffect(() => { load(); }, [projectId]);

  const create = async () => {
    if (!form.orderNumber.trim()) return;
    try {
      await projectsApi.createVariation(projectId, {
        projectId,
        orderNumber: form.orderNumber,
        issuedAt: new Date(form.issuedAt).toISOString(),
        reason: form.reason || undefined,
        originalContractValue: Number(form.originalContractValue),
        notes: form.notes || undefined,
      });
      setForm({ orderNumber: '', issuedAt: new Date().toISOString().slice(0, 10), reason: '', originalContractValue: 0, notes: '' });
      setShowForm(false);
      await load();
    } catch (e: unknown) { setError(getErrorMessage(e, 'فشل إنشاء الأمر التعديلي.')); }
  };

  const approve = async (voId: string) => {
    if (!confirm('اعتماد هذا الأمر التعديلي؟ سيتم تحديث قيمة العقد الجديدة.')) return;
    try {
      await projectsApi.approveVariation(projectId, voId);
      await load();
    } catch (e: unknown) { setError(getErrorMessage(e, 'فشل الاعتماد.')); }
  };

  if (loading) return <SkeletonTable rows={3} cols={4} />;

  const totalVariation = items.reduce((s, v) => s + Number(v.variationAmount || 0), 0);
  const newContractTotal = items.length > 0 ? items[items.length - 1].newContractValue : 0;

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <StatCard
          label="قيمة العقد الأصلية"
          value={items.length > 0 ? `${Number(items[0].originalContractValue).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ل.د` : '—'}
          icon={Wallet}
          tone="blue"
        />
        <StatCard
          label="إجمالي التعديلات"
          value={`${totalVariation.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ل.د`}
          icon={Calculator}
          tone={totalVariation >= 0 ? 'amber' : 'green'}
          hint={`${items.length} أمر تعديلي`}
        />
        <StatCard
          label="قيمة العقد الجديدة"
          value={newContractTotal > 0 ? `${Number(newContractTotal).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ل.د` : '—'}
          icon={TrendingUp}
          tone="violet"
          hint={items.length > 0 ? `+${((totalVariation / Number(items[0].originalContractValue || 1)) * 100).toFixed(1)}%` : ''}
        />
      </div>

      <SectionCard
        title="الأوامر التعديلية (Variation Orders)"
        description="أي تعديل على بنود العقد بعد التوقيع — يجب اعتماده قبل تنفيذه"
        actions={
          <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />} onClick={() => setShowForm(true)}>
            أمر تعديلي جديد
          </Button>
        }
      >
        {error && <div className="mb-3 rounded-lg border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">{error}</div>}
        {items.length === 0 ? (
          <EmptyState
            icon={<AlertTriangle className="h-12 w-12" />}
            title="لا توجد أوامر تعديلية"
            description="في الممارسة الليبية، الأوامر التعديلية شائعة جداً (نموذجياً 20-80% من قيمة العقد). أنشئ واحداً لتعديل نطاق المشروع."
            action={
              <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />} onClick={() => setShowForm(true)}>
                إنشاء أول أمر
              </Button>
            }
          />
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50 text-xs uppercase text-gray-500">
              <tr>
                <th className="px-3 py-2 text-start">رقم الأمر</th>
                <th className="px-3 py-2 text-start">التاريخ</th>
                <th className="px-3 py-2 text-start">السبب</th>
                <th className="px-3 py-2 text-end">قيمة العقد</th>
                <th className="px-3 py-2 text-end">التعديل</th>
                <th className="px-3 py-2 text-end">القيمة الجديدة</th>
                <th className="px-3 py-2 text-start">الحالة</th>
                <th className="px-3 py-2 text-end"></th>
              </tr>
            </thead>
            <tbody>
              {items.map(v => {
                const isApproved = v.status === 'APPROVED' || v.status === 'Approved';
                return (
                  <tr key={v.id} className="border-t border-gray-100 hover:bg-gray-50">
                    <td className="px-3 py-2 font-mono text-xs font-bold text-slate-700">{v.orderNumber}</td>
                    <td className="px-3 py-2 text-xs text-gray-600">{formatDate(v.issuedAt)}</td>
                    <td className="px-3 py-2 text-gray-800">{v.reason || '—'}</td>
                    <td className="px-3 py-2 text-end font-mono tabular-nums">{Number(v.originalContractValue).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
                    <td className={'px-3 py-2 text-end font-mono font-semibold tabular-nums ' + (Number(v.variationAmount) >= 0 ? 'text-amber-700' : 'text-green-700')}>
                      {Number(v.variationAmount) >= 0 ? '+' : ''}{Number(v.variationAmount).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                    </td>
                    <td className="px-3 py-2 text-end font-mono font-bold tabular-nums">{Number(v.newContractValue).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</td>
                    <td className="px-3 py-2">
                      <StatusPill tone={isApproved ? 'green' : 'slate'} label={v.status} showDot />
                    </td>
                    <td className="px-3 py-2 text-end">
                      {!isApproved && (
                        <Button variant="primary" size="sm" iconLeft={<CheckCircle className="h-3.5 w-3.5" />} onClick={() => approve(v.id)}>
                          اعتماد
                        </Button>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </SectionCard>

      {showForm && (
        <Modal open onClose={() => setShowForm(false)} title="أمر تعديلي جديد">
          <div className="space-y-3">
            <div className="grid grid-cols-2 gap-2">
              <div>
                <label className="text-xs font-bold text-gray-600">رقم الأمر</label>
                <Input value={form.orderNumber} onChange={e => setForm({ ...form, orderNumber: e.target.value })} placeholder="VO-2026-001" />
              </div>
              <div>
                <label className="text-xs font-bold text-gray-600">تاريخ الإصدار</label>
                <Input type="date" value={form.issuedAt} onChange={e => setForm({ ...form, issuedAt: e.target.value })} />
              </div>
            </div>
            <div>
              <label className="text-xs font-bold text-gray-600">قيمة العقد الأصلية</label>
              <Input type="number" step="0.01" value={form.originalContractValue} onChange={e => setForm({ ...form, originalContractValue: Number(e.target.value) })} />
            </div>
            <div>
              <label className="text-xs font-bold text-gray-600">السبب</label>
              <Input value={form.reason} onChange={e => setForm({ ...form, reason: e.target.value })} placeholder="طلب من الجهة المنفذة" />
            </div>
            <div>
              <label className="text-xs font-bold text-gray-600">ملاحظات</label>
              <Input value={form.notes} onChange={e => setForm({ ...form, notes: e.target.value })} />
            </div>
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="secondary" onClick={() => setShowForm(false)}>إلغاء</Button>
              <Button variant="primary" onClick={create} disabled={!form.orderNumber}>إنشاء</Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
}
