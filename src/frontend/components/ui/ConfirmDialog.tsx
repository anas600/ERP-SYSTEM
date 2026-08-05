'use client';

// مكوّن ConfirmDialog — Sprint 39 (DEC-125) Design System
// نافذة تأكيد قابلة لإعادة الاستخدام
// تُستخدم لعمليات الحذف/التعطيل/الإجراءات الحساسة
// الاستخدام:
//   <ConfirmDialog
//     open={isOpen}
//     title="حذف العميل"
//     message="هل أنت متأكد من حذف هذا العميل؟ لا يمكن التراجع."
//     confirmLabel="حذف"
//     cancelLabel="إلغاء"
//     variant="danger"
//     onConfirm={handleDelete}
//     onCancel={() => setIsOpen(false)}
//   />

import { ReactNode, useEffect } from 'react';
import { AlertTriangle, Info } from 'lucide-react';
import { Button } from './Button';
import { cn } from '@/lib/utils';

export type ConfirmDialogVariant = 'danger' | 'primary' | 'warning';

export interface ConfirmDialogProps {
  /** هل الـ Dialog مفتوح؟ */
  open: boolean;
  /** العنوان */
  title: string;
  /** الرسالة التوضيحية */
  message: ReactNode;
  /** نص زر التأكيد */
  confirmLabel?: string;
  /** نص زر الإلغاء */
  cancelLabel?: string;
  /** الـ variant — يتحكم بلون زر التأكيد والأيقونة */
  variant?: ConfirmDialogVariant;
  /** هل زر التأكيد في حالة loading؟ */
  loading?: boolean;
  /** عند الضغط على تأكيد */
  onConfirm: () => void;
  /** عند الإلغاء (أو الضغط خارج/ESC) */
  onCancel: () => void;
}

const VARIANT_STYLES: Record<ConfirmDialogVariant, { btn: 'danger' | 'primary' | 'success'; iconBg: string; iconText: string; Icon: typeof AlertTriangle }> = {
  danger: {
    btn: 'danger',
    iconBg: 'bg-danger-100',
    iconText: 'text-danger-600',
    Icon: AlertTriangle,
  },
  primary: {
    btn: 'primary',
    iconBg: 'bg-brand-100',
    iconText: 'text-brand-600',
    Icon: Info,
  },
  warning: {
    btn: 'primary',
    iconBg: 'bg-warning-100',
    iconText: 'text-warning-700',
    Icon: AlertTriangle,
  },
};

export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = 'تأكيد',
  cancelLabel = 'إلغاء',
  variant = 'primary',
  loading = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  // قفل scroll الـ body
  useEffect(() => {
    if (open && typeof document !== 'undefined') {
      const prev = document.body.style.overflow;
      document.body.style.overflow = 'hidden';
      return () => {
        document.body.style.overflow = prev;
      };
    }
    return undefined;
  }, [open]);

  // ESC للإغلاق
  useEffect(() => {
    if (!open) return;
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !loading) onCancel();
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [open, onCancel, loading]);

  if (!open) return null;

  const style = VARIANT_STYLES[variant];
  const Icon = style.Icon;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-ink-900/50 animate-fade-in"
      onClick={loading ? undefined : onCancel}
      dir="rtl"
      role="dialog"
      aria-modal="true"
      aria-labelledby="confirm-dialog-title"
    >
      <div
        className={cn(
          'w-full max-w-md bg-white rounded-2xl shadow-xl animate-scale-in',
          'flex flex-col overflow-hidden'
        )}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="px-6 py-5 flex items-start gap-4">
          <div
            className={cn(
              'flex-shrink-0 h-10 w-10 rounded-full flex items-center justify-center',
              style.iconBg
            )}
          >
            <Icon className={cn('h-5 w-5', style.iconText)} />
          </div>
          <div className="flex-1 min-w-0">
            <h2 id="confirm-dialog-title" className="text-base font-bold text-ink-800">
              {title}
            </h2>
            <div className="mt-1.5 text-sm text-ink-600 leading-6">{message}</div>
          </div>
        </div>
        <div className="px-6 py-3 bg-ink-50 border-t border-ink-100 flex items-center justify-start gap-2">
          <Button variant={style.btn} onClick={onConfirm} loading={loading}>
            {confirmLabel}
          </Button>
          <Button variant="ghost" onClick={onCancel} disabled={loading}>
            {cancelLabel}
          </Button>
        </div>
      </div>
    </div>
  );
}
