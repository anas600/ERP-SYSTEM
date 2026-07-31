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
import { getErrorMessage } from '@/lib/api';

interface PostingRule {
  id: string;
  name: string;
  description?: string | null;
  eventType: number;
  isActive: boolean;
  templateJson: string;
  createdAt: string;
}

const EVENT_LABELS: Record<number, string> = {
  1: 'استلام مخزون (StockReceived)',
  2: 'صرف مخزون (StockIssued)',
  3: 'إنشاء فاتورة (InvoiceCreated)',
  4: 'استلام دفعة (PaymentReceived)',
};

const EVENT_OPTIONS = Object.entries(EVENT_LABELS).map(([value, label]) => ({
  label,
  value: Number(value),
}));

const DEFAULT_TEMPLATE = JSON.stringify(
  {
    description: 'ترحيل تلقائي',
    reference: 'AUTO-{reference}',
    lines: [
      { accountCode: '1110', side: 'debit', amountFormula: '{amount}' },
      { accountCode: '2010', side: 'credit', amountFormula: '{amount}' },
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

function parseRuleSummary(templateJson: string): { accountCode: string; targetAccount: string; conditionsCount: number } {
  try {
    const parsed = JSON.parse(templateJson) as {
      lines?: { accountCode?: string }[];
      conditions?: unknown;
    };
    const firstLine = parsed.lines?.[0];
    return {
      accountCode: firstLine?.accountCode ?? '—',
      targetAccount: firstLine?.accountCode ?? '—',
      conditionsCount: Array.isArray(parsed.conditions) ? parsed.conditions.length : 0,
    };
  } catch {
    return { accountCode: '—', targetAccount: '—', conditionsCount: 0 };
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
      const res = await fetch('/api/finance/posting-rules', { cache: 'no-store' });
      if (!res.ok) throw new Error('فشل التحميل');
      const data = (await res.json()) as PostingRule[];
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
      const res = await fetch('/api/finance/posting-rules', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: addForm.name,
          description: addForm.description || null,
          eventType: addForm.eventType,
          isActive: true,
          templateJson: addForm.templateJson,
        }),
      });
      if (!res.ok) {
        const t = await res.text();
        throw new Error(t || 'فشل إنشاء القاعدة');
      }
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
      const res = await fetch(`/api/finance/posting-rules/${deleteTarget.id}`, {
        method: 'DELETE',
      });
      if (res.status === 404 || res.status === 405) {
        throw new Error('حذف قواعد الترحيل غير مدعوم في الـ backend حالياً.');
      }
      if (!res.ok) {
        const t = await res.text();
        throw new Error(t || 'فشل الحذف');
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
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm">
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
                        الحساب الهدف:{' '}
                        <span className="font-mono text-blue-600">{summary.targetAccount}</span>
                      </span>
                      <span>
                        عدد الأسطر:{' '}
                        <span className="font-mono">{summary.conditionsCount}</span>
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
                      iconLeft={<Trash2 className="h-3 w-3 text-red-500" />}
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
            <div className="bg-red-50 border border-red-200 text-red-700 px-3 py-2 rounded text-sm">
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
