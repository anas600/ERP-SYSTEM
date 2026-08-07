'use client';

import { useEffect, useState, useMemo } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import {
  ArrowRight, FileText, RefreshCw, TrendingUp, TrendingDown,
  Calendar, BarChart3, FileSignature, Receipt, Plus, Trash2, CheckCircle, XCircle,
  AlertTriangle, Package,
} from 'lucide-react';
import { PageHeader, Card, Button, Input, Modal, Badge } from '@/components/ui';
import { api, getErrorMessage, projectsApi, Project, ProjectPnL, Contract, CreateContractRequest, UpdateContractRequest, ProgressBilling, CreateBillingRequest, BillingPreview, ProjectWip } from '@/lib/api';
import { formatDate, formatCurrency } from '@/lib/utils';

type Tab = 'details' | 'pnl' | 'contract' | 'billings';

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
        description={item?.description || 'بيانات المشروع + الأرباح والخسائر + العقد + المستخلصات'}
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
          {/* Tab nav */}
          <div className="flex gap-1 mb-4 border-b border-gray-200 overflow-x-auto">
            <TabButton id="details" tab={tab} setTab={setTab}>التفاصيل</TabButton>
            <TabButton id="pnl" tab={tab} setTab={setTab} icon={<BarChart3 className="h-4 w-4" />}>P&L</TabButton>
            <TabButton id="contract" tab={tab} setTab={setTab} icon={<FileSignature className="h-4 w-4" />}>العقد</TabButton>
            <TabButton id="billings" tab={tab} setTab={setTab} icon={<Receipt className="h-4 w-4" />}>المستخلصات</TabButton>
          </div>

          {tab === 'details' && <DetailsTab item={item} onReload={load} />}
          {tab === 'pnl' && <PnLTab projectId={id} />}
          {tab === 'contract' && <ContractTab projectId={id} onWipRefresh={() => undefined} />}
          {tab === 'billings' && <BillingsTab projectId={id} onChange={() => undefined} />}
        </>
      )}
    </div>
  );
}

function TabButton({ id, tab, setTab, icon, children }: { id: Tab; tab: Tab; setTab: (t: Tab) => void; icon?: React.ReactNode; children: React.ReactNode }) {
  const active = tab === id;
  return (
    <button
      type="button"
      onClick={() => setTab(id)}
      className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px flex items-center gap-1 whitespace-nowrap ${
        active ? 'border-blue-600 text-blue-700' : 'border-transparent text-gray-500 hover:text-gray-700'
      }`}
    >
      {icon}{children}
    </button>
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

// ===== P&L Tab (Sprint 57 + DEC-165: WIP card) =====

function PnLTab({ projectId }: { projectId: string }) {
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
        projectsApi.getProjectPnL(projectId, from || undefined, to || undefined),
        projectsApi.getProjectWip(projectId),
      ]);
      setPnl(pnlData);
      setWip(wipData);
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
            </Card>
          </div>

          {/* DEC-165: WIP card */}
          {wip && (
            <Card className="p-4 mb-4 bg-amber-50 border-amber-200">
              <div className="flex items-center gap-2 mb-2">
                <Package className="h-5 w-5 text-amber-700" />
                <h3 className="text-sm font-bold text-amber-900">WIP — العمل قيد التنفيذ (DEC-165)</h3>
              </div>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-sm">
                <div>
                  <div className="text-xs text-amber-800">إجمالي التكاليف</div>
                  <div className="font-bold text-amber-900">{formatCurrency(wip.totalCosts)}</div>
                </div>
                <div>
                  <div className="text-xs text-amber-800">إجمالي المفوتر (صافي)</div>
                  <div className="font-bold text-amber-900">{formatCurrency(wip.totalBilledNet)}</div>
                </div>
                <div>
                  <div className="text-xs text-amber-800">احتجاز محتجز</div>
                  <div className="font-bold text-amber-900">{formatCurrency(wip.totalRetentionHeld)}</div>
                </div>
                <div>
                  <div className="text-xs text-amber-800">WIP (تكاليف − مفوتر)</div>
                  <div className={`font-bold ${wip.wip > 0 ? 'text-amber-900' : wip.wip < 0 ? 'text-red-700' : 'text-green-700'}`}>
                    {formatCurrency(wip.wip)}
                  </div>
                </div>
              </div>
              <div className="mt-2 text-xs text-amber-800">
                <strong>{wip.statusName}</strong>
                {wip.wip > 0 && ' — العمل جاري والفوترة متأخرة (normal)'}
                {wip.wip < 0 && ' — فوترنا أكثر مما أنفقنا (تحقق من الـ billings)'}
                {wip.wip === 0 && ' ✓'}
              </div>
            </Card>
          )}

          <Card className="p-4">
            <h3 className="text-sm font-bold text-gray-800 mb-3">تفصيل التكاليف حسب الحساب</h3>
            {pnl.costsByAccount.length === 0 ? (
              <div className="text-center text-gray-500 py-8 text-sm">
                لا توجد تكاليف على هذا المشروع في النطاق الزمني المحدد.
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b text-gray-600 text-xs">
                      <th className="text-start py-2 px-2">الحساب</th>
                      <th className="text-start py-2 px-2">الكود</th>
                      <th className="text-end py-2 px-2">المبلغ</th>
                      <th className="text-end py-2 px-2">%</th>
                    </tr>
                  </thead>
                  <tbody>
                    {pnl.costsByAccount.map((c) => (
                      <tr key={c.accountCode} className="border-b last:border-0">
                        <td className="py-2 px-2">{c.accountName}</td>
                        <td className="py-2 px-2 text-gray-500 font-mono text-xs">{c.accountCode}</td>
                        <td className="py-2 px-2 text-end font-mono text-red-700">{formatCurrency(c.amount)}</td>
                        <td className="py-2 px-2 text-end text-xs">
                          {pnl.totalCosts > 0 ? ((c.amount / pnl.totalCosts) * 100).toFixed(1) : '0.0'}%
                        </td>
                      </tr>
                    ))}
                    <tr className="font-bold bg-gray-50">
                      <td className="py-2 px-2">الإجمالي</td>
                      <td className="py-2 px-2"></td>
                      <td className="py-2 px-2 text-end font-mono text-red-700">{formatCurrency(pnl.totalCosts)}</td>
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

// ===== Contract Tab (DEC-163) =====

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
      // 404 = no contract yet (not an error)
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
    <div>
      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">{error}</div>}

      {loading ? (
        <Card className="p-12 text-center text-gray-500">جاري التحميل…</Card>
      ) : !contract ? (
        <Card className="p-12 text-center">
          <FileSignature className="h-12 w-12 mx-auto mb-3 text-gray-300" />
          <p className="text-gray-500 mb-4">لا يوجد عقد على هذا المشروع.</p>
          <Button variant="primary" onClick={() => { setEditing(null); setShowModal(true); }} iconLeft={<Plus className="h-4 w-4" />}>
            إنشاء عقد
          </Button>
        </Card>
      ) : (
        <Card className="p-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-bold text-gray-800">تفاصيل العقد</h3>
            <div className="flex gap-2">
              <Button variant="secondary" onClick={() => { setEditing(contract); setShowModal(true); }}>تعديل</Button>
              <Button variant="secondary" onClick={handleDelete} iconLeft={<Trash2 className="h-4 w-4" />}>حذف</Button>
            </div>
          </div>
          <dl className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Row label="رقم العقد" value={contract.contractNumber ?? undefined} />
            <Row label="قيمة العقد" value={formatCurrency(contract.contractValue)} />
            <Row label="نسبة الدفعة المقدمة" value={`${contract.advancePercent}%`} />
            <Row label="نسبة الاحتجاز" value={`${contract.retentionPercent}%`} />
            <Row label="بداية الاحتجاز من مستخلص #" value={String(contract.retentionStartBilling)} />
            <Row label="تاريخ البداية" value={contract.startDate} />
            <Row label="تاريخ النهاية" value={contract.endDate} />
            <Row label="ملاحظات" value={contract.notes ?? undefined} />
          </dl>
        </Card>
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
        {error && <div className="bg-red-50 border border-red-200 text-red-700 px-3 py-2 rounded text-sm">{error}</div>}
        <div>
          <label className="block text-xs text-gray-600 mb-1">رقم العقد (اختياري)</label>
          <Input value={form.contractNumber ?? ''} onChange={(e) => setForm({ ...form, contractNumber: e.target.value })} />
        </div>
        <div>
          <label className="block text-xs text-gray-600 mb-1">قيمة العقد (LYD) *</label>
          <Input type="number" step="0.0001" required value={form.contractValue}
            onChange={(e) => setForm({ ...form, contractValue: parseFloat(e.target.value) || 0 })} />
        </div>
        <div className="grid grid-cols-3 gap-2">
          <div>
            <label className="block text-xs text-gray-600 mb-1">دفعة مقدمة %</label>
            <Input type="number" step="0.01" min="0" max="100" value={form.advancePercent}
              onChange={(e) => setForm({ ...form, advancePercent: parseFloat(e.target.value) || 0 })} />
          </div>
          <div>
            <label className="block text-xs text-gray-600 mb-1">احتجاز %</label>
            <Input type="number" step="0.01" min="0" max="100" value={form.retentionPercent}
              onChange={(e) => setForm({ ...form, retentionPercent: parseFloat(e.target.value) || 0 })} />
          </div>
          <div>
            <label className="block text-xs text-gray-600 mb-1">احتجاز من #</label>
            <Input type="number" min="1" value={form.retentionStartBilling ?? 1}
              onChange={(e) => setForm({ ...form, retentionStartBilling: parseInt(e.target.value) || 1 })} />
          </div>
        </div>
        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className="block text-xs text-gray-600 mb-1">تاريخ البداية</label>
            <Input type="date" value={form.startDate ?? ''} onChange={(e) => setForm({ ...form, startDate: e.target.value })} />
          </div>
          <div>
            <label className="block text-xs text-gray-600 mb-1">تاريخ النهاية</label>
            <Input type="date" value={form.endDate ?? ''} onChange={(e) => setForm({ ...form, endDate: e.target.value })} />
          </div>
        </div>
        <div>
          <label className="block text-xs text-gray-600 mb-1">ملاحظات</label>
          <Input value={form.notes ?? ''} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
        </div>
        <div className="flex gap-2 justify-end pt-2">
          <Button type="button" variant="secondary" onClick={onClose}>إلغاء</Button>
          <Button type="submit" variant="primary" disabled={saving}>{saving ? 'جاري الحفظ…' : 'حفظ'}</Button>
        </div>
      </form>
    </Modal>
  );
}

// ===== Billings Tab (DEC-164) =====

function BillingsTab({ projectId, onChange }: { projectId: string; onChange: () => void }) {
  const [contract, setContract] = useState<Contract | null>(null);
  const [billings, setBillings] = useState<ProgressBilling[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const [c, b] = await Promise.all([
        projectsApi.getContract(projectId).catch(() => null),
        projectsApi.getBillings(projectId),
      ]);
      setContract(c);
      setBillings(b);
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

  return (
    <div>
      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4">{error}</div>}

      {!contract ? (
        <Card className="p-12 text-center text-gray-500">
          <AlertTriangle className="h-12 w-12 mx-auto mb-3 text-amber-400" />
          <p>أنشئ عقد أولاً قبل إضافة المستخلصات.</p>
        </Card>
      ) : (
        <>
          <div className="flex items-center justify-between mb-4">
            <div>
              <h3 className="text-lg font-bold text-gray-800">مستخلصات العقد</h3>
              <p className="text-sm text-gray-500">قيمة العقد: {formatCurrency(contract.contractValue)} ({billings.length} مستخلص)</p>
            </div>
            <Button variant="primary" onClick={() => setShowModal(true)} iconLeft={<Plus className="h-4 w-4" />}>
              مستخلص جديد
            </Button>
          </div>

          {loading ? (
            <Card className="p-12 text-center text-gray-500">جاري التحميل…</Card>
          ) : billings.length === 0 ? (
            <Card className="p-12 text-center text-gray-500">
              <Receipt className="h-12 w-12 mx-auto mb-3 text-gray-300" />
              لا توجد مستخلصات بعد.
            </Card>
          ) : (
            <Card className="p-0 overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-gray-50 text-gray-600 text-xs">
                    <th className="text-start py-2 px-2">رقم</th>
                    <th className="text-start py-2 px-2">تاريخ</th>
                    <th className="text-end py-2 px-2">% إنجاز</th>
                    <th className="text-end py-2 px-2">إجمالي</th>
                    <th className="text-end py-2 px-2">دفعة مقدمة</th>
                    <th className="text-end py-2 px-2">احتجاز</th>
                    <th className="text-end py-2 px-2">صافي</th>
                    <th className="text-center py-2 px-2">الحالة</th>
                    <th className="text-end py-2 px-2">إجراءات</th>
                  </tr>
                </thead>
                <tbody>
                  {billings.map((b) => (
                    <tr key={b.id} className="border-b last:border-0">
                      <td className="py-2 px-2 font-mono text-xs">{b.billingNumber}</td>
                      <td className="py-2 px-2">{formatDate(b.billingDate)}</td>
                      <td className="py-2 px-2 text-end">{b.workCompletedPercent.toFixed(2)}%</td>
                      <td className="py-2 px-2 text-end font-mono">{formatCurrency(b.grossAmount)}</td>
                      <td className="py-2 px-2 text-end font-mono text-orange-600">{formatCurrency(b.advanceDeducted)}</td>
                      <td className="py-2 px-2 text-end font-mono text-purple-600">{formatCurrency(b.retentionDeducted)}</td>
                      <td className="py-2 px-2 text-end font-mono font-bold text-green-700">{formatCurrency(b.netAmount)}</td>
                      <td className="py-2 px-2 text-center">
                        <BillingStatusBadge status={b.status} />
                      </td>
                      <td className="py-2 px-2 text-end">
                        {b.status === 1 && (
                          <div className="flex gap-1 justify-end">
                            <button onClick={() => handleApprove(b.id)} title="ترحيل"
                              className="p-1 text-green-600 hover:bg-green-50 rounded">
                              <CheckCircle className="h-4 w-4" />
                            </button>
                            <button onClick={() => handleCancel(b.id)} title="إلغاء"
                              className="p-1 text-red-600 hover:bg-red-50 rounded">
                              <XCircle className="h-4 w-4" />
                            </button>
                          </div>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </Card>
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

function BillingStatusBadge({ status }: { status: number }) {
  if (status === 1) return <Badge variant="warning">مسودة</Badge>;
  if (status === 2) return <Badge variant="success">مُرحّل</Badge>;
  if (status === 3) return <Badge variant="neutral">ملغى</Badge>;
  return <Badge>{status}</Badge>;
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

  // Live preview (debounced)
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
        {error && <div className="bg-red-50 border border-red-200 text-red-700 px-3 py-2 rounded text-sm">{error}</div>}
        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className="block text-xs text-gray-600 mb-1">رقم المستخلص *</label>
            <Input required value={form.billingNumber} onChange={(e) => setForm({ ...form, billingNumber: e.target.value })} />
          </div>
          <div>
            <label className="block text-xs text-gray-600 mb-1">تاريخ المستخلص *</label>
            <Input type="date" required value={form.billingDate} onChange={(e) => setForm({ ...form, billingDate: e.target.value })} />
          </div>
        </div>
        <div>
          <label className="block text-xs text-gray-600 mb-1">نسبة الإنجاز التراكمية % (0-100) *</label>
          <Input type="number" step="0.01" min="0.01" max="100" required
            value={form.workCompletedPercent}
            onChange={(e) => setForm({ ...form, workCompletedPercent: parseFloat(e.target.value) || 0 })} />
          {preview && (
            <div className="text-xs text-gray-500 mt-1">
              أعلى نسبة سابقة: {preview.previousMaxPercent.toFixed(2)}% — رقم المستخلص الجديد: #{preview.nextBillingNumber}
            </div>
          )}
        </div>
        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className="block text-xs text-gray-600 mb-1">من تاريخ</label>
            <Input type="date" value={form.periodFrom ?? ''} onChange={(e) => setForm({ ...form, periodFrom: e.target.value })} />
          </div>
          <div>
            <label className="block text-xs text-gray-600 mb-1">إلى تاريخ</label>
            <Input type="date" value={form.periodTo ?? ''} onChange={(e) => setForm({ ...form, periodTo: e.target.value })} />
          </div>
        </div>

        {/* Live preview card */}
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-3">
          <div className="text-xs font-bold text-blue-800 mb-2 flex items-center gap-1">
            <BarChart3 className="h-3 w-3" /> معاينة المستخلص (Live Preview)
            {previewing && <span className="text-gray-500 text-xs">— يحسب…</span>}
          </div>
          {preview ? (
            <div className="grid grid-cols-2 md:grid-cols-4 gap-2 text-sm">
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
                <div className="font-mono text-purple-600">{formatCurrency(preview.retentionDeducted)}</div>
              </div>
              <div>
                <div className="text-xs text-gray-500">صافي</div>
                <div className="font-mono font-bold text-green-700">{formatCurrency(preview.netAmount)}</div>
              </div>
            </div>
          ) : (
            <div className="text-xs text-gray-500">أدخل نسبة الإنجاز لمشاهدة الأرقام المتوقعة.</div>
          )}
        </div>

        <div>
          <label className="block text-xs text-gray-600 mb-1">ملاحظات</label>
          <Input value={form.notes ?? ''} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
        </div>
        <div className="flex gap-2 justify-end pt-2">
          <Button type="button" variant="secondary" onClick={onClose}>إلغاء</Button>
          <Button type="submit" variant="primary" disabled={saving || !preview}>
            {saving ? 'جاري الحفظ…' : 'حفظ كمسودة'}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
