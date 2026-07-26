'use client';

// نظام Toast موحّد — Hook + Context + Types (بدون JSX).
// الـ Provider والـ viewport المرئي يعيشان في `@/components/ui/Toast`.
// الاستخدام:
//
//   في الـ AppShell (أعلى شجرة الـ React):
//     import { ToastProvider } from '@/components/ui/Toast';
//     <ToastProvider>{children}</ToastProvider>
//
//   في أي مكوّن:
//     import { useToast } from '@/lib/useToast';
//     const { show } = useToast();
//     show('تم الحفظ بنجاح', 'success');
//     show('حدث خطأ', 'error');
//     show('ملاحظة', 'info');
//
// المدة الافتراضية: 4 ثوانٍ (قابلة للتعديل عبر show(msg, type, durationMs))

import { createContext, ReactNode, useContext } from 'react';

// ============ Types ============

export type ToastType = 'success' | 'error' | 'info';

export interface Toast {
  /** معرّف فريد — يُولَّد تلقائياً */
  id: string;
  /** نص الرسالة */
  message: string;
  /** النوع (يحدد اللون والأيقونة) */
  type: ToastType;
  /** مدة الظهور بالـ ms — افتراضي 4000 */
  durationMs: number;
}

export interface ShowOptions {
  /** مدة الظهور بالـ ms — اختياري */
  durationMs?: number;
}

export interface ToastContextValue {
  toasts: Toast[];
  show: (message: string, type?: ToastType, options?: number | ShowOptions) => string;
  dismiss: (id: string) => void;
  /** مختصرات سريعة */
  success: (message: string, options?: number | ShowOptions) => string;
  error: (message: string, options?: number | ShowOptions) => string;
  info: (message: string, options?: number | ShowOptions) => string;
}

export interface ToastProviderProps {
  children: ReactNode;
}

// ============ Context ============

export const ToastContext = createContext<ToastContextValue | null>(null);

// ============ Hook ============

export function useToast(): ToastContextValue {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be used inside <ToastProvider>');
  return ctx;
}

// ============ Helpers (مُصدَّرة للاستخدام داخل Toast.tsx) ============

export const DEFAULT_TOAST_DURATION = 4000;

export function generateToastId(): string {
  // يتجنب الـ crypto غير المتاح في البيئات القديمة ويكفي للتفرّد داخل الجلسة
  return `t-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;
}

export function normalizeDuration(opts?: number | ShowOptions): number {
  if (typeof opts === 'number') return opts;
  if (opts && typeof opts.durationMs === 'number') return opts.durationMs;
  return DEFAULT_TOAST_DURATION;
}
