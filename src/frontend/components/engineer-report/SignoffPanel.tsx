'use client';

// لوحة الاعتماد الإلكتروني لتقرير المهندس (Sprint 61, DEC-194)
//
// Visible only when the report is in "Submitted" state and the current user
// has PM or Client role. The backend enforces role authorization; this
// component is just a UI guard + bilingual labels.

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui';
import { Check, X, ShieldCheck, MessageSquare } from 'lucide-react';
import { cn } from '@/lib/utils';
import type {
  EngineerReportSignoffRequest,
  EngineerReportSignoffRole,
} from '@/lib/api';

export interface SignoffPanelProps {
  /** True if the current user is allowed to sign off (PM or Client). */
  canSign: boolean;
  /** Why signing is disabled (e.g. "التقرير في حالة مسودة"). */
  disabledReason?: string | null;
  submitting?: boolean;
  error?: string | null;
  onSign: (req: EngineerReportSignoffRequest) => void | Promise<void>;
}

const ROLE_OPTIONS: { value: EngineerReportSignoffRole; label: string }[] = [
  { value: 'PM', label: 'مدير المشروع / Project Manager' },
  { value: 'Client', label: 'العميل / Client' },
];

export function SignoffPanel({
  canSign,
  disabledReason = null,
  submitting = false,
  error = null,
  onSign,
}: SignoffPanelProps) {
  const [decision, setDecision] = useState<'approve' | 'reject' | null>(null);
  const [role, setRole] = useState<EngineerReportSignoffRole>('PM');
  const [comment, setComment] = useState('');

  if (!canSign) {
    return (
      <div
        className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800"
        data-testid="signoff-disabled"
      >
        <div className="flex items-center gap-2 font-semibold">
          <ShieldCheck className="h-4 w-4" />
          الاعتماد الإلكتروني
        </div>
        <p className="mt-1 text-xs">
          {disabledReason ?? 'الاعتماد متاح فقط للمدير أو العميل عندما يكون التقرير في حالة "مُقدَّم".'}
        </p>
      </div>
    );
  }

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!decision) return;
    await onSign({
      approved: decision === 'approve',
      signerRole: role,
      comment: comment.trim() || null,
    });
    // Reset on success — parent will re-render with new state.
    setDecision(null);
    setComment('');
  };

  return (
    <form
      onSubmit={submit}
      className="rounded-xl border border-emerald-200 bg-emerald-50/40 p-4"
      data-testid="signoff-panel"
    >
      <div className="mb-3 flex items-center gap-2 text-sm font-bold text-emerald-900">
        <ShieldCheck className="h-4 w-4" />
        الاعتماد الإلكتروني / Sign-off
      </div>

      {error && (
        <div className="mb-2 rounded-lg border border-rose-200 bg-rose-50 px-3 py-2 text-xs text-rose-700">
          {error}
        </div>
      )}

      <div className="space-y-3">
        <div>
          <label className="mb-1 block text-xs font-medium text-emerald-900">
            بصفتك / Your Role
          </label>
          <div className="flex flex-wrap gap-2">
            {ROLE_OPTIONS.map((r) => (
              <button
                key={r.value}
                type="button"
                onClick={() => setRole(r.value)}
                disabled={submitting}
                className={cn(
                  'rounded-lg border px-3 py-1.5 text-xs font-semibold transition',
                  role === r.value
                    ? 'border-emerald-500 bg-emerald-500 text-white shadow-sm'
                    : 'border-emerald-300 bg-white text-emerald-800 hover:bg-emerald-100'
                )}
                data-testid={`signoff-role-${r.value}`}
              >
                {r.label}
              </button>
            ))}
          </div>
        </div>

        <div>
          <label
            htmlFor="signoff-comment"
            className="mb-1 flex items-center gap-1 text-xs font-medium text-emerald-900"
          >
            <MessageSquare className="h-3 w-3" /> تعليق / Comment
            <span className="text-gray-500">(اختياري)</span>
          </label>
          <textarea
            id="signoff-comment"
            rows={2}
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            disabled={submitting}
            placeholder="ملاحظات على الاعتماد…"
            data-testid="signoff-comment"
            className="block w-full rounded-lg border border-emerald-300 bg-white px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
          />
        </div>

        <div>
          <p className="mb-1 text-xs font-medium text-emerald-900">
            القرار / Decision
          </p>
          <div className="flex flex-col gap-2 sm:flex-row">
            <button
              type="button"
              onClick={() => setDecision('approve')}
              disabled={submitting}
              className={cn(
                'flex flex-1 items-center justify-center gap-2 rounded-lg border px-3 py-2 text-sm font-bold transition',
                decision === 'approve'
                  ? 'border-emerald-600 bg-emerald-600 text-white shadow'
                  : 'border-emerald-300 bg-white text-emerald-700 hover:bg-emerald-100'
              )}
              data-testid="signoff-decision-approve"
            >
              <Check className="h-4 w-4" /> اعتماد / Approve
            </button>
            <button
              type="button"
              onClick={() => setDecision('reject')}
              disabled={submitting}
              className={cn(
                'flex flex-1 items-center justify-center gap-2 rounded-lg border px-3 py-2 text-sm font-bold transition',
                decision === 'reject'
                  ? 'border-rose-600 bg-rose-600 text-white shadow'
                  : 'border-rose-300 bg-white text-rose-700 hover:bg-rose-100'
              )}
              data-testid="signoff-decision-reject"
            >
              <X className="h-4 w-4" /> رفض / Reject
            </button>
          </div>
        </div>

        <div className="flex justify-end border-t border-emerald-200/60 pt-2">
          <Button
            type="submit"
            variant="primary"
            disabled={!decision || submitting}
            data-testid="signoff-submit"
          >
            {submitting ? 'جاري الحفظ…' : 'تأكيد الاعتماد / Confirm Sign-off'}
          </Button>
        </div>
      </div>
    </form>
  );
}
