'use client';

// Sprint 64 / DEC-222 + DEC-226 — SubContractForm.
//
// Create / edit form for a sub-contract (links a subcontractor to a project
// with a defined scope + value + retention terms). The subcontractor is chosen
// via a Select from the list passed in via props — keeping the form dumb.

import { FormEvent, useState } from 'react';
import { Save, X, FileSignature } from 'lucide-react';
import { Button, Input, Select } from '@/components/ui';
import type {
  SubContract,
  Subcontractor,
  CreateSubContractRequest,
} from '@/lib/api/subcontractors';

export interface SubContractFormProps {
  initial?: SubContract;
  subcontractors: Subcontractor[];
  onSubmit: (data: CreateSubContractRequest) => Promise<void> | void;
  onCancel?: () => void;
  submitting?: boolean;
}

export function SubContractForm({
  initial,
  subcontractors,
  onSubmit,
  onCancel,
  submitting,
}: SubContractFormProps) {
  const [subcontractorId, setSubcontractorId] = useState(initial?.subcontractorId ?? subcontractors[0]?.id ?? '');
  const [contractNumber, setContractNumber] = useState(initial?.contractNumber ?? '');
  const [scopeOfWork, setScopeOfWork] = useState(initial?.scopeOfWork ?? '');
  const [contractValue, setContractValue] = useState<string>(String(initial?.contractValue ?? ''));
  const [retentionPercent, setRetentionPercent] = useState<string>(String(initial?.retentionPercent ?? 10));
  const [retentionReleaseBilling, setRetentionReleaseBilling] = useState<string>(
    String(initial?.retentionReleaseBilling ?? 3)
  );
  const [startDate, setStartDate] = useState<string>(
    initial?.startDate ? initial.startDate.substring(0, 10) : ''
  );
  const [endDate, setEndDate] = useState<string>(
    initial?.endDate ? initial.endDate.substring(0, 10) : ''
  );
  const [notes, setNotes] = useState(initial?.notes ?? '');
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);

    if (!subcontractorId) {
      setError('يجب اختيار المقاول الباطن.');
      return;
    }
    if (!contractNumber.trim()) {
      setError('رقم العقد مطلوب.');
      return;
    }
    if (!scopeOfWork.trim()) {
      setError('نطاق العمل مطلوب.');
      return;
    }
    const valueNum = Number(contractValue);
    if (!Number.isFinite(valueNum) || valueNum < 0) {
      setError('قيمة العقد يجب أن تكون رقماً موجباً.');
      return;
    }
    const retentionNum = Number(retentionPercent);
    if (!Number.isFinite(retentionNum) || retentionNum < 0 || retentionNum > 100) {
      setError('نسبة الاحتجاز يجب أن تكون بين 0 و 100.');
      return;
    }
    const releaseNum = Number(retentionReleaseBilling);
    if (!Number.isInteger(releaseNum) || releaseNum < 1) {
      setError('رقم المستخلص لتحرير الاحتجاز يجب أن يكون عدداً صحيحاً >= 1.');
      return;
    }

    const payload: CreateSubContractRequest = {
      subcontractorId,
      contractNumber: contractNumber.trim(),
      scopeOfWork: scopeOfWork.trim(),
      contractValue: valueNum,
      retentionPercent: retentionNum,
      retentionReleaseBilling: releaseNum,
      startDate: startDate || null,
      endDate: endDate || null,
      notes: notes.trim() || null,
    };
    try {
      await onSubmit(payload);
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : 'فشل الحفظ.';
      setError(msg);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4" dir="rtl">
      <div className="flex items-center gap-3 border-b border-gray-100 pb-3">
        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-emerald-500 to-emerald-700 text-white shadow-sm">
          <FileSignature className="h-5 w-5" />
        </div>
        <div>
          <h2 className="text-lg font-bold text-gray-900">عقد باطن جديد</h2>
          <p className="text-xs text-gray-500">ربط مقاول باطن بالمشروع الحالي بنطاق عمل محدد</p>
        </div>
      </div>

      {error && (
        <div className="rounded-lg border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">{error}</div>
      )}

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        <Select
          label="المقاول الباطن"
          value={subcontractorId}
          onChange={(e) => setSubcontractorId(e.target.value)}
          required
          options={[
            { value: '', label: '— اختر المقاول —' },
            ...subcontractors
              .filter((s) => s.isActive)
              .map((s) => ({ value: s.id, label: `${s.code} — ${s.name}` })),
          ]}
        />
        <Input
          label="رقم العقد"
          value={contractNumber}
          onChange={(e) => setContractNumber(e.target.value)}
          placeholder="SC-001"
          required
        />
        <Input
          label="قيمة العقد (ل.د)"
          type="number"
          min={0}
          step="0.0001"
          value={contractValue}
          onChange={(e) => setContractValue(e.target.value)}
          placeholder="50000"
          required
        />
        <Input
          label="نسبة الاحتجاز (%)"
          type="number"
          min={0}
          max={100}
          step="0.01"
          value={retentionPercent}
          onChange={(e) => setRetentionPercent(e.target.value)}
        />
        <Input
          label="تحرير الاحتجاز بعد (مستخلص)"
          type="number"
          min={1}
          step="1"
          value={retentionReleaseBilling}
          onChange={(e) => setRetentionReleaseBilling(e.target.value)}
        />
        <Input
          label="تاريخ البدء"
          type="date"
          value={startDate}
          onChange={(e) => setStartDate(e.target.value)}
        />
        <Input
          label="تاريخ الانتهاء"
          type="date"
          value={endDate}
          onChange={(e) => setEndDate(e.target.value)}
        />
        <Input
          label="ملاحظات"
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          placeholder="ملاحظات اختيارية"
        />
      </div>

      <div>
        <label className="block text-sm font-bold text-gray-700">نطاق العمل</label>
        <textarea
          value={scopeOfWork}
          onChange={(e) => setScopeOfWork(e.target.value)}
          rows={3}
          required
          placeholder="وصف تفصيلي للأعمال التي يقوم بها المقاول الباطن (مثال: تمديدات كهرباء المبنى A، الدور 1-4)"
          className="mt-1 block w-full rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm shadow-sm focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-200"
        />
      </div>

      <div className="flex items-center justify-end gap-2 border-t border-gray-100 pt-3">
        {onCancel && (
          <Button type="button" variant="secondary" onClick={onCancel} iconLeft={<X className="h-4 w-4" />}>
            إلغاء
          </Button>
        )}
        <Button
          type="submit"
          variant="primary"
          disabled={submitting}
          iconLeft={<Save className="h-4 w-4" />}
        >
          {submitting ? 'جارٍ الحفظ…' : 'إنشاء العقد'}
        </Button>
      </div>
    </form>
  );
}

export default SubContractForm;
