'use client';

// إنشاء قيد محاسبي جديد (Journal Entry)
// Sprint 38 (DEC-124): 4 final manual JE templates (tax, fx-gain, fx-loss, capital-withdrawal) — completes 12/12.
// Sprint 37 (DEC-123): 4 new manual JE templates (Salary, Loan, Bad debt, Inventory adjustment).
//   Plus Sprint 34 templates (Manual, Depreciation, Accrual, Prepaid) merge in their own branch.

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { ArrowRight, Save, Plus, Trash2, FileText } from 'lucide-react';
import { Button, Input, Card, PageHeader } from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import { getErrorMessage } from '@/lib/api';

interface AccountOption {
  id: string;
  code: string;
  name: string;
}

interface LineDraft {
  accountId: string;
  debit: string;
  credit: string;
  description: string;
}

interface FormState {
  entryDate: string;
  description: string;
  reference: string;
  lines: LineDraft[];
}

// ===== Templates (Sprint 34 + Sprint 37) =====
//
// Sprint 34 shipped: manual, depreciation, accrual, prepaid (4)
// Sprint 37 adds:   salary, loan, bad-debt, inventory-adjust (4) → 8 total
//
// CoA accounts required (Sprint 37 also added the missing ones to DefaultCoASeed.cs):
//   1210 النقدية                  (Cash)
//   1230 ذمم مدينة                 (AR)
//   1240 مخزون                     (Inventory)
//   1300 مجمع إهلاك                (Accumulated Depreciation)  — new
//   1410 سلف الموظفين              (Loans Receivable - Employees) — new
//   2110 مصروفات مستحقة             (Accrued Expenses)          — new
//   2210 دائنون لموردين             (AP)
//   4112 أجور مباشرة                (Direct Labor)
//   5410 ديون معدومة                (Bad Debt Expense)          — new
//   5500 إهلاك الأصول               (Depreciation Expense)      — new
interface JeTemplate {
  id: string;
  label: string;
  description: string;
  build: (accounts: AccountOption[]) => { description: string; reference: string; lines: LineDraft[] } | null;
}

// Helper: lookup account by code from the loaded options.
const acct = (accounts: AccountOption[], code: string): string => {
  const a = accounts.find((x) => x.code === code);
  return a ? a.id : '';
};

const TEMPLATES: JeTemplate[] = [
  // === Sprint 34: 4 original templates ===
  {
    id: 'manual',
    label: 'تسوية يدوية',
    description: 'قيد تسوية يدوية (Dr حساب / Cr حساب) — افتراضي',
    build: () => ({
      description: 'تسوية يدوية',
      reference: 'ADJ',
      lines: [emptyLine(), emptyLine()],
    }),
  },
  {
    id: 'depreciation',
    label: 'إهلاك أصول',
    description: 'Dr 5500 مصروف إهلاك / Cr 1300 مجمع الإهلاك (إهلاك شهري/سنوي)',
    build: (a) => ({
      description: 'إهلاك أصول ثابتة',
      reference: 'DEP',
      lines: [
        { accountId: acct(a, '5500'), debit: '0', credit: '0', description: 'مصروف إهلاك' },
        { accountId: acct(a, '1300'), debit: '0', credit: '0', description: 'مجمع إهلاك' },
      ],
    }),
  },
  {
    id: 'accrual',
    label: 'مصروف مستحق',
    description: 'Dr 5xxx مصروف / Cr 2110 مصروف مستحق (مصروف لم يُدفع بعد)',
    build: (a) => ({
      description: 'مصروف مستحق',
      reference: 'ACCR',
      lines: [
        { accountId: '', debit: '0', credit: '0', description: 'مصروف مستحق' },
        { accountId: acct(a, '2110'), debit: '0', credit: '0', description: 'مصروف مستحق' },
      ],
    }),
  },
  {
    id: 'prepaid',
    label: 'مصروف مسبق الدفع',
    description: 'Dr 1250 مصروف مسبق / Cr 1210 نقدية (دفع مقدماً لمصروف مستقبلي)',
    build: (a) => ({
      description: 'مصروف مسبق الدفع',
      reference: 'PREP',
      lines: [
        { accountId: acct(a, '1250'), debit: '0', credit: '0', description: 'مصروف مسبق' },
        { accountId: acct(a, '1210'), debit: '0', credit: '0', description: 'نقدية' },
      ],
    }),
  },

  // === Sprint 37 (DEC-123): 4 new templates ===
  {
    id: 'salary',
    label: 'رواتب',
    description: 'Dr 4112 أجور مباشرة / Cr 1210 نقدية (دفع رواتب الموظفين)',
    build: (a) => ({
      description: 'صرف رواتب',
      reference: 'SAL',
      lines: [
        { accountId: acct(a, '4112'), debit: '0', credit: '0', description: 'أجور مباشرة' },
        { accountId: acct(a, '1210'), debit: '0', credit: '0', description: 'نقدية' },
      ],
    }),
  },
  {
    id: 'loan',
    label: 'سلفة موظف',
    description: 'Dr 1410 سلف / Cr 1210 نقدية (إقراض موظف من الصندوق)',
    build: (a) => ({
      description: 'سلفة موظف',
      reference: 'LOAN',
      lines: [
        { accountId: acct(a, '1410'), debit: '0', credit: '0', description: 'سلفة موظف' },
        { accountId: acct(a, '1210'), debit: '0', credit: '0', description: 'نقدية' },
      ],
    }),
  },
  {
    id: 'bad-debt',
    label: 'ديون معدومة',
    description: 'Dr 5410 ديون معدومة / Cr 1230 ذمم مدينة (شطب دين متعسر)',
    build: (a) => ({
      description: 'شطب دين معدوم',
      reference: 'BD',
      lines: [
        { accountId: acct(a, '5410'), debit: '0', credit: '0', description: 'ديون معدومة' },
        { accountId: acct(a, '1230'), debit: '0', credit: '0', description: 'ذمم مدينة' },
      ],
    }),
  },
  {
    id: 'inventory-adjust',
    label: 'تسوية مخزون',
    description: 'Dr/Cr 1240 مخزون (تسوية فروقات الجرد الفعلي مقابل الدفتري)',
    build: (a) => ({
      description: 'تسوية مخزون',
      reference: 'INV-ADJ',
      lines: [
        { accountId: acct(a, '1240'), debit: '0', credit: '0', description: 'مخزون (تسوية بالزيادة)' },
        { accountId: acct(a, '1240'), debit: '0', credit: '0', description: 'مخزون (تسوية بالنقص)' },
      ],
    }),
  },

  // === Sprint 38 (DEC-124): 4 final templates (9-12 of 12) ===
  {
    id: 'tax-payment',
    label: 'دفع ضريبة',
    description: 'Dr 4300 مصروفات تمويلية / Cr 1210 نقدية (دفع ضريبة/رسوم للسلطات)',
    build: (a) => ({
      description: 'دفع ضريبة',
      reference: 'TAX',
      lines: [
        { accountId: acct(a, '4300'), debit: '0', credit: '0', description: 'مصروفات تمويلية' },
        { accountId: acct(a, '1210'), debit: '0', credit: '0', description: 'نقدية' },
      ],
    }),
  },
  {
    id: 'fx-gain',
    label: 'فروق عملة (ربح)',
    description: 'Dr 1230 ذمم / Cr 5110 إيرادات (ربح فروق تقييم عملة)',
    build: (a) => ({
      description: 'ربح فروق عملة',
      reference: 'FX-G',
      lines: [
        { accountId: acct(a, '1230'), debit: '0', credit: '0', description: 'ذمم مدينة (بالزيادة بعد التقييم)' },
        { accountId: acct(a, '5110'), debit: '0', credit: '0', description: 'إيرادات فروق عملة' },
      ],
    }),
  },
  {
    id: 'fx-loss',
    label: 'فروق عملة (خسارة)',
    description: 'Dr 4110 تكلفة / Cr 1230 ذمم (خسارة فروق تقييم عملة)',
    build: (a) => ({
      description: 'خسارة فروق عملة',
      reference: 'FX-L',
      lines: [
        { accountId: acct(a, '4110'), debit: '0', credit: '0', description: 'تكلفة فروق عملة' },
        { accountId: acct(a, '1230'), debit: '0', credit: '0', description: 'ذمم مدينة (بالنقص بعد التقييم)' },
      ],
    }),
  },
  {
    id: 'capital-withdrawal',
    label: 'سحب رأس مال',
    description: 'Dr 3100 رأس المال / Cr 1210 نقدية (سحب الشريك لرأس المال)',
    build: (a) => ({
      description: 'سحب رأس مال',
      reference: 'CAP-WD',
      lines: [
        { accountId: acct(a, '3100'), debit: '0', credit: '0', description: 'رأس المال' },
        { accountId: acct(a, '1210'), debit: '0', credit: '0', description: 'نقدية' },
      ],
    }),
  },
];

const emptyLine = (): LineDraft => ({ accountId: '', debit: '0', credit: '0', description: '' });

export default function NewJournalEntryPage() {
  const router = useRouter();
  useAuth();
  const [form, setForm] = useState<FormState>({
    entryDate: new Date().toISOString().split('T')[0],
    description: '',
    reference: '',
    lines: [emptyLine(), emptyLine()],
  });
  const [accounts, setAccounts] = useState<AccountOption[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // Sprint 34+: selected template (default = manual)
  const [selectedTemplate, setSelectedTemplate] = useState<string>('manual');

  // Sprint 34+: apply selected template (pre-fills description/reference/lines)
  const onApplyTemplate = () => {
    const tpl = TEMPLATES.find((t) => t.id === selectedTemplate);
    if (!tpl) return;
    const built = tpl.build(accounts);
    if (!built) {
      setError(`القالب "${tpl.label}" يتطلب حسابات غير متوفرة في دليل الحسابات.`);
      return;
    }
    setForm({
      entryDate: form.entryDate,
      description: built.description,
      reference: built.reference,
      lines: built.lines,
    });
    setError(null);
  };

  useEffect(() => {
    const loadAccounts = async () => {
      try {
        // Sprint 37: use api.get to attach JWT (raw fetch returned 401 silently)
        const { financeApi } = await import('@/lib/api');
        const data = await financeApi.listAccounts();
        setAccounts(data.map((a: AccountOption) => ({ id: a.id, code: a.code, name: a.name })));
      } catch {
        // ignore — keep empty
      }
    };
    loadAccounts();
  }, []);

  const totalDebit = form.lines.reduce((s, l) => s + (Number(l.debit) || 0), 0);
  const totalCredit = form.lines.reduce((s, l) => s + (Number(l.credit) || 0), 0);
  const balanced = Math.abs(totalDebit - totalCredit) < 0.01 && totalDebit > 0;

  const updateLine = (idx: number, field: keyof LineDraft, value: string) => {
    setForm((f) => {
      const newLines = [...f.lines];
      newLines[idx] = { ...newLines[idx], [field]: value };
      return { ...f, lines: newLines };
    });
  };

  const addLine = () => {
    setForm((f) => ({ ...f, lines: [...f.lines, emptyLine()] }));
  };

  const removeLine = (idx: number) => {
    if (form.lines.length <= 2) return;
    setForm((f) => ({ ...f, lines: f.lines.filter((_, i) => i !== idx) }));
  };

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!balanced) {
      setError(`القيد غير متوازن: مدين=${totalDebit.toFixed(2)}, دائن=${totalCredit.toFixed(2)}`);
      return;
    }
    setError(null);
    setSubmitting(true);
    try {
      const res = await fetch('/api/finance/journal-entries', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          entryDate: form.entryDate,
          description: form.description,
          reference: form.reference || null,
          lines: form.lines.map((l) => ({
            accountId: l.accountId,
            debit: Number(l.debit) || 0,
            credit: Number(l.credit) || 0,
            description: l.description || null,
          })),
        }),
      });
      if (!res.ok) {
        const t = await res.text();
        throw new Error(t || 'فشل إنشاء القيد');
      }
      const created = await res.json();
      router.push(`/finance/journal-entries/${created.id}`);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل إنشاء القيد.'));
      setSubmitting(false);
    }
  };

  return (
    <div>
      <PageHeader
        title="➕ قيد محاسبي جديد"
        description="أنشئ قيد يومية (يجب أن يكون متوازن: مدين = دائن)"
        breadcrumb={[
          { label: 'الرئيسية', href: '/dashboard' },
          { label: 'القيود', href: '/finance/journal-entries' },
          { label: 'جديد' },
        ]}
        actions={
          <Link href="/finance/journal-entries">
            <Button variant="ghost" iconLeft={<ArrowRight className="h-4 w-4" />}>رجوع</Button>
          </Link>
        }
      />

      <Card className="max-w-4xl">
        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm">{error}</div>
        )}

        <form onSubmit={onSubmit} className="space-y-4">
          {/* Sprint 34+: Templates selector — pre-fills description/reference/lines */}
          <div className="mb-4 p-3 bg-blue-50 border border-blue-200 rounded-lg">
            <div className="flex items-center gap-2 mb-2">
              <FileText className="h-4 w-4 text-blue-600" />
              <h3 className="font-semibold text-blue-900 text-sm">قوالب جاهزة (Templates)</h3>
            </div>
            <div className="flex items-end gap-2">
              <div className="flex-1">
                <label className="block text-xs text-gray-600 mb-1">اختر قالب</label>
                <select
                  value={selectedTemplate}
                  onChange={(e) => setSelectedTemplate(e.target.value)}
                  className="w-full border border-gray-300 rounded px-2 py-1.5 text-sm bg-white"
                >
                  {TEMPLATES.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.label} — {t.description}
                    </option>
                  ))}
                </select>
              </div>
              <Button type="button" variant="secondary" size="sm" onClick={onApplyTemplate}>
                تطبيق القالب
              </Button>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Input label="التاريخ *" type="date" value={form.entryDate} onChange={(e) => setForm({ ...form, entryDate: e.target.value })} required />
            <Input label="المرجع" value={form.reference} onChange={(e) => setForm({ ...form, reference: e.target.value })} placeholder="اختياري" />
          </div>

          <Input label="الوصف *" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} required placeholder="مثال: فاتورة كهرباء" />

          {/* Lines */}
          <div className="border-t pt-4">
            <div className="flex items-center justify-between mb-3">
              <h3 className="font-bold text-gray-800">البنود (Lines)</h3>
              <Button type="button" variant="ghost" size="sm" onClick={addLine} iconLeft={<Plus className="h-4 w-4" />}>
                إضافة بند
              </Button>
            </div>

            <div className="space-y-3">
              {form.lines.map((line, idx) => (
                <div key={idx} className="border rounded-lg p-3 bg-gray-50">
                  <div className="grid grid-cols-12 gap-2">
                    <div className="col-span-6">
                      <label className="block text-xs text-gray-500 mb-1">الحساب</label>
                      <select
                        value={line.accountId}
                        onChange={(e) => updateLine(idx, 'accountId', e.target.value)}
                        className="w-full border border-gray-300 rounded px-2 py-1.5 text-sm"
                        required
                      >
                        <option value="">-- اختر --</option>
                        {accounts.map((a) => (
                          <option key={a.id} value={a.id}>
                            {a.code} - {a.name}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div className="col-span-2">
                      <label className="block text-xs text-gray-500 mb-1">مدين</label>
                      <input
                        type="number"
                        step="0.01"
                        value={line.debit}
                        onChange={(e) => updateLine(idx, 'debit', e.target.value)}
                        className="w-full border border-gray-300 rounded px-2 py-1.5 text-sm font-mono"
                      />
                    </div>
                    <div className="col-span-2">
                      <label className="block text-xs text-gray-500 mb-1">دائن</label>
                      <input
                        type="number"
                        step="0.01"
                        value={line.credit}
                        onChange={(e) => updateLine(idx, 'credit', e.target.value)}
                        className="w-full border border-gray-300 rounded px-2 py-1.5 text-sm font-mono"
                      />
                    </div>
                    <div className="col-span-2 flex items-end">
                      {form.lines.length > 2 && (
                        <button
                          type="button"
                          onClick={() => removeLine(idx)}
                          className="text-red-600 hover:bg-red-50 p-1.5 rounded"
                          aria-label="حذف البند"
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      )}
                    </div>
                    <div className="col-span-12">
                      <label className="block text-xs text-gray-500 mb-1">الوصف</label>
                      <input
                        type="text"
                        value={line.description}
                        onChange={(e) => updateLine(idx, 'description', e.target.value)}
                        className="w-full border border-gray-300 rounded px-2 py-1.5 text-sm"
                        placeholder="اختياري"
                      />
                    </div>
                  </div>
                </div>
              ))}
            </div>

            {/* Totals */}
            <div className="mt-4 p-3 bg-gray-100 rounded-lg flex items-center justify-between">
              <div className="text-sm font-bold">
                مدين: <span className="font-mono text-blue-600">{totalDebit.toFixed(2)}</span>
              </div>
              <div className="text-sm font-bold">
                دائن: <span className="font-mono text-orange-600">{totalCredit.toFixed(2)}</span>
              </div>
              <div className={`text-sm font-bold ${balanced ? 'text-green-600' : 'text-red-600'}`}>
                {balanced ? '✅ متوازن' : `❌ فرق ${Math.abs(totalDebit - totalCredit).toFixed(2)}`}
              </div>
            </div>
          </div>

          <div className="flex items-center gap-2 pt-3 border-t">
            <Button type="submit" variant="primary" loading={submitting} disabled={!balanced} iconLeft={<Save className="h-4 w-4" />}>
              حفظ القيد (مسودة)
            </Button>
            <Link href="/finance/journal-entries">
              <Button type="button" variant="ghost">إلغاء</Button>
            </Link>
          </div>
        </form>
      </Card>
    </div>
  );
}
