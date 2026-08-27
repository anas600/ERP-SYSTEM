'use client';

// Sprint 64 / DEC-221 + DEC-226 — SubcontractorForm.
//
// Create / edit form for a subcontractor master record. Mirrors the existing
// patterns from the inventory + customers pages (controlled state, hand-rolled
// validation surfaced inline, submit button disabled while submitting).
//
// L19 / DEC-095: CompanyId is intentionally NOT collected — the JWT context
// supplies it server-side.

import { FormEvent, useState } from 'react';
import { Save, X, User2 } from 'lucide-react';
import { Button, Input, Select } from '@/components/ui';
import type { Subcontractor, CreateSubcontractorRequest, UpdateSubcontractorRequest } from '@/lib/api/subcontractors';

export type SubcontractorFormMode = 'create' | 'edit';

export interface SubcontractorFormProps {
  mode: SubcontractorFormMode;
  initial?: Subcontractor;
  onSubmit: (data: CreateSubcontractorRequest | UpdateSubcontractorRequest) => Promise<void> | void;
  onCancel?: () => void;
  submitting?: boolean;
}

const TRADE_OPTIONS = [
  { value: '', label: '— اختر التخصص —' },
  { value: 'electrical', label: 'كهرباء' },
  { value: 'plumbing', label: 'سباكة' },
  { value: 'carpentry', label: 'نجارة' },
  { value: 'masonry', label: 'بناء' },
  { value: 'painting', label: 'دهان' },
  { value: 'tiling', label: 'بلاط' },
  { value: 'hvac', label: 'تكييف وتهوية' },
  { value: 'steel', label: 'حدادة' },
  { value: 'aluminum', label: 'ألمنيوم' },
  { value: 'other', label: 'أخرى' },
];

export function SubcontractorForm({
  mode,
  initial,
  onSubmit,
  onCancel,
  submitting,
}: SubcontractorFormProps) {
  const [code, setCode] = useState(initial?.code ?? '');
  const [name, setName] = useState(initial?.name ?? '');
  const [nameAr, setNameAr] = useState(initial?.nameAr ?? '');
  const [contactPerson, setContactPerson] = useState(initial?.contactPerson ?? '');
  const [phone, setPhone] = useState(initial?.phone ?? '');
  const [email, setEmail] = useState(initial?.email ?? '');
  const [tradeSpecialty, setTradeSpecialty] = useState(initial?.tradeSpecialty ?? '');
  const [taxId, setTaxId] = useState(initial?.taxId ?? '');
  const [isActive, setIsActive] = useState<boolean>(initial?.isActive ?? true);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError(null);

    // Hand-rolled validation — keeps the form dependency-free.
    if (mode === 'create' && !code.trim()) {
      setError('الكود مطلوب.');
      return;
    }
    if (!name.trim()) {
      setError('الاسم مطلوب.');
      return;
    }
    if (email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      setError('البريد الإلكتروني غير صالح.');
      return;
    }

    try {
      if (mode === 'create') {
        const payload: CreateSubcontractorRequest = {
          code: code.trim(),
          name: name.trim(),
          nameAr: nameAr.trim() || null,
          contactPerson: contactPerson.trim() || null,
          phone: phone.trim() || null,
          email: email.trim() || null,
          tradeSpecialty: tradeSpecialty || null,
          taxId: taxId.trim() || null,
        };
        await onSubmit(payload);
      } else {
        const payload: UpdateSubcontractorRequest = {
          name: name.trim(),
          nameAr: nameAr.trim() || null,
          contactPerson: contactPerson.trim() || null,
          phone: phone.trim() || null,
          email: email.trim() || null,
          tradeSpecialty: tradeSpecialty || null,
          taxId: taxId.trim() || null,
          isActive,
        };
        await onSubmit(payload);
      }
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : 'فشل الحفظ.';
      setError(msg);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4" dir="rtl">
      <div className="flex items-center gap-3 border-b border-gray-100 pb-3">
        <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-amber-500 to-amber-700 text-white shadow-sm">
          <User2 className="h-5 w-5" />
        </div>
        <div>
          <h2 className="text-lg font-bold text-gray-900">
            {mode === 'create' ? 'مقاول باطن جديد' : `تعديل: ${initial?.name ?? ''}`}
          </h2>
          <p className="text-xs text-gray-500">بيانات المقاول الباطن — تُحفظ في سجل الشركة الحالي</p>
        </div>
      </div>

      {error && (
        <div className="rounded-lg border border-rose-200 bg-rose-50 p-3 text-sm text-rose-700">{error}</div>
      )}

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
        {mode === 'create' && (
          <Input
            label="الكود"
            value={code}
            onChange={(e) => setCode(e.target.value)}
            placeholder="ELEC-001"
            required
          />
        )}
        <Input
          label="الاسم"
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="شركة الفجر للكهرباء"
          required
        />
        <Input
          label="الاسم بالعربية"
          value={nameAr}
          onChange={(e) => setNameAr(e.target.value)}
          placeholder="شركة الفجر"
          dir="rtl"
        />
        <Select
          label="التخصص"
          value={tradeSpecialty}
          onChange={(e) => setTradeSpecialty(e.target.value)}
          options={TRADE_OPTIONS}
        />
        <Input
          label="الشخص المسؤول"
          value={contactPerson}
          onChange={(e) => setContactPerson(e.target.value)}
          placeholder="عبدالله محمد"
        />
        <Input
          label="الهاتف"
          value={phone}
          onChange={(e) => setPhone(e.target.value)}
          placeholder="091-1234567"
        />
        <Input
          label="البريد الإلكتروني"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="info@alfajr-electric.ly"
        />
        <Input
          label="الرقم الضريبي"
          value={taxId}
          onChange={(e) => setTaxId(e.target.value)}
          placeholder="12345678"
        />
        {mode === 'edit' && (
          <label className="flex items-center gap-2 self-end text-sm font-semibold text-gray-700">
            <input
              type="checkbox"
              checked={isActive}
              onChange={(e) => setIsActive(e.target.checked)}
              className="h-4 w-4 rounded border-gray-300 text-brand-600 focus:ring-brand-500"
            />
            نشط
          </label>
        )}
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
          {submitting ? 'جارٍ الحفظ…' : mode === 'create' ? 'إنشاء المقاول' : 'حفظ التعديلات'}
        </Button>
      </div>
    </form>
  );
}

export default SubcontractorForm;
