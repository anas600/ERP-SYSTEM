'use client';

// قائمة قواعد الترحيل (Posting Rules) + Add/Edit Modal + Delete

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Edit, Eye, Plus, Shield, Trash2 } from 'lucide-react';
import {
  Badge,
  Button,
  Card,
  ConfirmDialog,
  EmptyState,
  Input,
  Modal,
  PageHeader,
  Select,
  SkeletonTable,
  useToast,
} from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { api, getErrorMessage } from '@/lib/api';

interface PostingRule {
  id: string;
  name: string;
  description?: string | null;
  eventType: number;
  isActive: boolean;
  templateJson: string;
  createdAt: string;
}

// Sprint 21: expanded event types. Note: 3 = SalesInvoicePosted, 4 = ReceiptPosted
// (legacy aliases for backward compat with Sprint 11-12 rules).
const EVENT_LABELS: Record<number, string> = {
  1: 'استلام مخزون (StockReceived)',
  2: 'صرف مخزون (StockIssued)',
  3: 'فاتورة مبيعات (SalesInvoicePosted)',
  4: 'سند قبض من عميل (ReceiptPosted)',
  5: 'فاتورة مورّد (VendorBillPosted)',
  6: 'دفع لمورّد (PaymentPosted)',
};

const EVENT_OPTIONS = Object.entries(EVENT_LABELS).map(([value, label]) => ({
  label,
  value: Number(value),
}));

// Sprint 21: default template uses real CoA codes (1240 Inventory, 2210 AP).
// Note: previous default used 1110/2010 which don't exist in the actual CoA.
const DEFAULT_TEMPLATE = JSON.stringify(
  {
    description: 'ترحيل تلقائي',
    lines: [
      { accountCode: '1240', side: 'debit', amountFormula: '{amount}' },
      { accountCode: '2210', side: 'credit', amountFormula: '{amount}' },
    ],
  },
  null,
  2
);

interface FormState {
  name: string;
  description: string;
  eventType: number;
  templateJson: string;
}

const EMPTY_FORM: FormState = {
  name: '',
  description: '',
  eventType: 1,
  templateJson: DEFAULT_TEMPLATE,
};

function parseRuleSummary(templateJson: string): { accountCodes: string; linesCount: number } {
  // Sprint 21: show ALL lines (not just the first) for a clearer summary
  try {
    const parsed = JSON.parse(templateJson) as {
      lines?: { accountCode?: string; side?: string }[];
    };
    const codes = (parsed.lines ?? [])
      .map((l) => `${l.accountCode ?? '?'} (${l.side === 'debit' ? 'Dr' : l.side === 'credit' ? 'Cr' : '?'})`)
      .join(' / ');
    return {
      accountCodes: codes || '—',
      linesCount: parsed.lines?.length ?? 0,
    };
  } catch {
    return { accountCodes: '—', linesCount: 0 };
  }
}

export default function PostingRulesPage() {
  const router = useRouter();
  const toast = useToast();
  const { loading: authLoading } = useAuth();
  const [items, setItems] = useState<PostingRule[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filter, setFilter] = useState('');

  // Add modal
  const [addOpen, setAddOpen] = useState(false);
  const [addForm, setAddForm] = useState<FormState>(EMPTY_FORM);
  const [addSubmitting, setAddSubmitting] = useState(false);
  const [addError, setAddError] = useState<string | null>(null);

  // Delete confirm
  const [deleteTarget, setDeleteTarget] = useState<PostingRule | null>(null);
  const [deleteSubmitting, setDeleteSubmitting] = useState(false);

  useEffect(() => {
    if (authLoading) return;
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const { data } = await api.get<PostingRule[]>('/api/finance/posting-rules');
      setItems(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل التحميل'));
    } finally {
      setLoading(false);
    }
  };

  const filtered = items.filter((r) => {
    if (!filter) return true;
    const q = filter.toLowerCase();
    return (
      r.name.toLowerCase().includes(q) ||
      (r.description ?? '').toLowerCase().includes(q) ||
      (EVENT_LABELS[r.eventType] ?? '').toLowerCase().includes(q)
    );
  });

  const onAddField = <K extends keyof FormState>(k: K) => (
    e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>
  ) => {
    const v = e.target.value;
    setAddForm((f) => ({ ...f, [k]: k === 'eventType' ? Number(v) : v }));
  };

  const openAdd = () => {
    setAddForm(EMPTY_FORM);
    setAddError(null);
    setAddOpen(true);
  };

  const submitAdd = async () => {
    setAddError(null);
    if (!addForm.name.trim()) {
      setAddError('اسم القاعدة مطلوب.');
      return;
    }
    try {
      JSON.parse(addForm.templateJson);
    } catch {
      setAddError('قالب JSON غير صالح.');
      return;
    }
    setAddSubmitting(true);
    try {
      // Sprint 21: parse the templateJson string into the structured object the BE expects.
      // The BE's CreatePostingRuleRequest has `template: PostingRuleTemplate` (parsed),
      // not `templateJson: string` — sending the string would 400.
      const parsedTemplate = JSON.parse(addForm.templateJson) as {
        description?: string;
        reference?: string | null;
        lines: { accountCode: string; side: 'debit' | 'credit'; amountFormula: string }[];
      };
      await api.post('/api/finance/posting-rules', {
        name: addForm.name,
        description: addForm.description || null,
        eventType: addForm.eventType,
        isActive: true,
        template: parsedTemplate,
      });
      toast.success(`تم إنشاء القاعدة "${addForm.name}".`);
      setAddOpen(false);
      await load();
    } catch (e: unknown) {
      setAddError(getErrorMessage(e, 'فشل إنشاء القاعدة.'));
    } finally {
      setAddSubmitting(false);
    }
  };

  const submitDelete = async () => {
    if (!deleteTarget) return;
    setDeleteSubmitting(true);
    try {
      try {
        await api.delete(`/api/finance/posting-rules/${deleteTarget.id}`);
      } catch (err: unknown) {
        const e = err as { response?: { status?: number; data?: unknown } };
        if (e?.response?.status === 404 || e?.response?.status === 405) {
          throw new Error('حذف قواعد الترحيل غير مدعوم في الـ backend حالياً.');
        }
        throw err;
      }
      toast.success(`تم حذف القاعدة "${deleteTarget.name}".`);
      setDeleteTarget(null);
      await load();
    } catch (e: unknown) {
      toast.error(getErrorMessage(e, 'فشل حذف القاعدة.'));
    } finally {
      setDeleteSubmitting(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="⚙️ قواعد الترحيل"
        description="قواعد ربط أحداث النظام بقيود محاسبية"
        actions={
          <div className="flex items-center gap-2">
            <Input
              placeholder="🔍 بحث..."
              value={filter}
              onChange={(e) => setFilter(e.target.value)}
              containerClassName="w-64"
            />
            <Button onClick={openAdd} variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
              قاعدة جديدة
            </Button>
          </div>
        }
      />

      {error && (
        <div className="bg-danger-50 border border-danger-200 text-danger-700 px-4 py-3 rounded-lg mb-4 text-sm">
          {error}
        </div>
      )}

      {loading ? (
        <SkeletonTable rows={5} cols={4} />
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={<Shield className="h-12 w-12" />}
          title="لا توجد قواعد ترحيل"
          description="ابدأ بإضافة أول قاعدة ترحيل لربط أحداث النظام بالقيود المحاسبية."
          action={
            <Button onClick={openAdd} variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
              قاعدة جديدة
            </Button>
          }
        />
      ) : (
        <div className="space-y-2">
          {filtered.map((r) => {
            const summary = parseRuleSummary(r.templateJson);
            return (
              <Card key={r.id} accent={r.isActive ? 'green' : 'gray'}>
                <div className="flex items-start justify-between gap-3">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <h3 className="font-bold text-gray-800">{r.name}</h3>
                      <Badge variant={r.isActive ? 'success' : 'neutral'}>
                        {r.isActive ? 'فعّال' : 'معطّل'}
                      </Badge>
                      <Badge variant="info">{EVENT_LABELS[r.eventType] || `Event ${r.eventType}`}</Badge>
                    </div>
                    {r.description && <p className="text-sm text-gray-500 mt-1">{r.description}</p>}
                    <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-gray-500">
                      <span>
                        السطور:{' '}
                        <span className="font-mono text-blue-600">{summary.accountCodes}</span>
                      </span>
                      <span>
                        عدد الأسطر:{' '}
                        <span className="font-mono">{summary.linesCount}</span>
                      </span>
                    </div>
                  </div>
                  <div className="flex items-center gap-1 flex-shrink-0">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => router.push(`/admin/posting-rules/${r.id}/edit`)}
                      iconLeft={<Edit className="h-3 w-3" />}
                    >
                      تعديل
                    </Button>
                    <Link href={`/admin/posting-rules/${r.id}`}>
                      <Button variant="ghost" size="sm" iconLeft={<Eye className="h-3 w-3" />}>
                        عرض
                      </Button>
                    </Link>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setDeleteTarget(r)}
                      iconLeft={<Trash2 className="h-3 w-3 text-danger-500" />}
                    >
                      حذف
                    </Button>
                  </div>
                </div>
              </Card>
            );
          })}
        </div>
      )}

      {/* Add Modal */}
      <Modal
        open={addOpen}
        onClose={() => (addSubmitting ? undefined : setAddOpen(false))}
        title="➕ قاعدة ترحيل جديدة"
        description="حدد قاعدة ترحيل تلقائي لحدث معين"
        size="xl"
        footer={
          <>
            <Button variant="primary" onClick={submitAdd} loading={addSubmitting}>
              حفظ
            </Button>
            <Button variant="ghost" onClick={() => setAddOpen(false)} disabled={addSubmitting}>
              إلغاء
            </Button>
          </>
        }
      >
        <div className="space-y-4">
          {addError && (
            <div className="bg-danger-50 border border-danger-200 text-danger-700 px-3 py-2 rounded text-sm">
              {addError}
            </div>
          )}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Input
              label="اسم القاعدة *"
              value={addForm.name}
              onChange={onAddField('name')}
              required
              placeholder="مثال: ترحيل استلام المخزون"
            />
            <Select
              label="نوع الحدث *"
              value={String(addForm.eventType)}
              onChange={onAddField('eventType')}
              options={EVENT_OPTIONS}
            />
          </div>
          <Input
            label="الوصف"
            value={addForm.description}
            onChange={onAddField('description')}
            placeholder="اختياري"
          />
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">قالب JSON *</label>
            <textarea
              value={addForm.templateJson}
              onChange={onAddField('templateJson')}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-xs font-mono"
              rows={12}
              required
            />
            <p className="text-xs text-gray-500 mt-1">
              💡 المتغيرات المتاحة: {'{amount}'}, {'{reference}'}. الحسابات تُحدّد بكودها (AccountCode).
            </p>
          </div>
        </div>
      </Modal>

      {/* Delete confirm */}
      <ConfirmDialog
        open={!!deleteTarget}
        title="حذف قاعدة الترحيل"
        message={
          deleteTarget ? (
            <span>
              هل تريد حذف القاعدة <b>{deleteTarget.name}</b>؟ لا يمكن التراجع.
            </span>
          ) : null
        }
        confirmLabel="حذف"
        cancelLabel="إلغاء"
        variant="danger"
        loading={deleteSubmitting}
        onConfirm={submitDelete}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  );
}
