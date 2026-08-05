'use client';

// مكوّن Toast — Provider + Viewport (الـ stack المرئي) لمكوّنات النظام.
// الاستخدام:
//
//   في الـ AppShell (أعلى شجرة الـ React):
//     import { ToastProvider } from '@/components/ui';
//     <ToastProvider>{children}</ToastProvider>
//
//   في أي مكوّن:
//     import { useToast } from '@/lib/useToast';
//     const { show } = useToast();
//     show('تم الحفظ بنجاح', 'success');
//
// الإغلاق التلقائي بعد 4 ثوانٍ (افتراضي).

import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { CheckCircle2, Info, X, XCircle } from 'lucide-react';
import { cn } from '@/lib/utils';
import {
  generateToastId,
  normalizeDuration,
  ShowOptions,
  Toast,
  ToastContext,
  ToastContextValue,
  ToastProviderProps,
  ToastType,
} from '@/lib/useToast';

// ============ Provider ============

export function ToastProvider({ children }: ToastProviderProps) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  // نحتفظ بمرجع لمؤقّتات الإغلاق حتى نقدر نلغيها يدوياً عند الـ dismiss
  const timersRef = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map());

  const dismiss = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
    const timer = timersRef.current.get(id);
    if (timer) {
      clearTimeout(timer);
      timersRef.current.delete(id);
    }
  }, []);

  const show = useCallback(
    (message: string, type: ToastType = 'info', options?: number | ShowOptions): string => {
      const id = generateToastId();
      const durationMs = normalizeDuration(options);
      const newToast: Toast = { id, message, type, durationMs };

      setToasts((prev) => [...prev, newToast]);

      // جدولة الإغلاق التلقائي
      const timer = setTimeout(() => {
        dismiss(id);
      }, durationMs);
      timersRef.current.set(id, timer);

      return id;
    },
    [dismiss]
  );

  // تنظيف المؤقّتات عند الـ unmount
  useEffect(() => {
    const timers = timersRef.current;
    return () => {
      timers.forEach((t) => clearTimeout(t));
      timers.clear();
    };
  }, []);

  const value = useMemo<ToastContextValue>(
    () => ({
      toasts,
      show,
      dismiss,
      success: (m, o) => show(m, 'success', o),
      error: (m, o) => show(m, 'error', o),
      info: (m, o) => show(m, 'info', o),
    }),
    [toasts, show, dismiss]
  );

  return (
    <ToastContext.Provider value={value}>
      {children}
      <ToastViewport toasts={toasts} onDismiss={dismiss} />
    </ToastContext.Provider>
  );
}

// ============ Viewport (الـ stack المرئي) ============

interface ToastViewportProps {
  toasts: Toast[];
  onDismiss: (id: string) => void;
}

const TYPE_STYLES: Record<
  ToastType,
  { ring: string; bg: string; text: string; icon: typeof CheckCircle2 }
> = {
  // Sprint 39 (DEC-125): use ink + semantic color tokens for consistency
  success: {
    ring: 'ring-success-200',
    bg: 'bg-success-50',
    text: 'text-success-700',
    icon: CheckCircle2,
  },
  error: {
    ring: 'ring-danger-200',
    bg: 'bg-danger-50',
    text: 'text-danger-700',
    icon: XCircle,
  },
  info: {
    ring: 'ring-brand-200',
    bg: 'bg-brand-50',
    text: 'text-brand-700',
    icon: Info,
  },
};

function ToastViewport({ toasts, onDismiss }: ToastViewportProps) {
  if (toasts.length === 0) return null;

  return (
    <div
      // موضع علوي من الجهة اليمنى (top-right) — متعارف عليه في الـ toasts
      className="fixed top-4 right-4 z-[60] flex flex-col gap-2 pointer-events-none"
      dir="rtl"
      aria-live="polite"
      aria-atomic="false"
    >
      {toasts.map((t) => {
        const style = TYPE_STYLES[t.type];
        const Icon = style.icon;
        return (
          <div
            key={t.id}
            role="status"
            className={cn(
              'pointer-events-auto',
              'min-w-[280px] max-w-md',
              'rounded-xl shadow-lg ring-1',
              'px-4 py-3 flex items-start gap-3',
              style.ring,
              style.bg
            )}
          >
            <Icon className={cn('h-5 w-5 flex-shrink-0 mt-0.5', style.text)} />
            <p className={cn('flex-1 text-sm font-medium leading-5', style.text)}>{t.message}</p>
            <button
              type="button"
              onClick={() => onDismiss(t.id)}
              className={cn(
                'flex-shrink-0 rounded p-0.5 transition-colors',
                'text-ink-400 hover:text-ink-700 hover:bg-white/60'
              )}
              aria-label="إغلاق الإشعار"
            >
              <X className="h-4 w-4" />
            </button>
          </div>
        );
      })}
    </div>
  );
}
