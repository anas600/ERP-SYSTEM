'use client';

// مكوّن EmptyState — يُعرض عندما لا توجد بيانات (قائمة فارغة)
// Sprint 4 (T3b): يدعم العنوان والوصف ثنائي اللغة (عربي + إنجليزي).
//
// الاستخدام البسيط:
//   <EmptyState
//     icon={<Users className="h-12 w-12" />}
//     title="لا يوجد عملاء"
//     description="ابدأ بإضافة عميل جديد"
//     action={<Button>إضافة عميل</Button>}
//   />
//
// الاستخدام ثنائي اللغة (يفضّل في الـ Sprint 4+):
//   <EmptyState
//     icon={<Users className="h-12 w-12" />}
//     title={{ ar: 'لا يوجد عملاء', en: 'No customers yet' }}
//     description={{ ar: 'ابدأ بإضافة عميل جديد', en: 'Add your first customer' }}
//     action={...}
//   />
//
// الرجوع: لازم يكون الـ parent فيه `dir="rtl"` أو يكون جذر الـ document
// بـ `dir="rtl"` (الإعداد الحالي في `app/layout.tsx`).

import { ReactNode } from 'react';
import { cn } from '@/lib/utils';
import type { BilingualError } from '@/lib/errors';

/** نوع العنصر النصي: string (مفرد) أو BilingualError (AR + EN). */
export type BilingualString = string | BilingualError;

function pickLocale(value: BilingualString, locale: 'ar' | 'en' = 'ar'): string {
  if (typeof value === 'string') return value;
  return value[locale];
}

export interface EmptyStateProps {
  /** أيقونة توضيحية (مطلوب — يفضل lucide-react icon بحجم h-12 w-12) */
  icon?: ReactNode;
  /** العنوان الرئيسي — string أو BilingualError */
  title: BilingualString;
  /** وصف ثانوي اختياري — string أو BilingualError */
  description?: BilingualString;
  /** زر أو رابط إجراء اختياري (مثلاً "إضافة جديد") */
  action?: ReactNode;
  /** عرض الـ EN label تحت الـ AR (افتراضي true). اخلو false للـ compact. */
  showEnLabel?: boolean;
  /** كلاسات إضافية للـ container الخارجي */
  className?: string;
}

export function EmptyState({
  icon,
  title,
  description,
  action,
  showEnLabel = true,
  className,
}: EmptyStateProps) {
  const isBilingualTitle = typeof title !== 'string';
  const isBilingualDesc = description != null && typeof description !== 'string';
  const showBilingual = showEnLabel && (isBilingualTitle || isBilingualDesc);

  return (
    <div
      dir="rtl"
      className={cn(
        'flex flex-col items-center justify-center text-center',
        'bg-gray-50 border border-gray-100 rounded-xl',
        'py-12 px-6',
        className
      )}
    >
      {icon && (
        <div className="mb-4 inline-flex h-16 w-16 items-center justify-center rounded-full bg-white border border-gray-200 text-gray-400 shadow-sm">
          {icon}
        </div>
      )}
      <h3 className="text-base font-semibold text-gray-800">{pickLocale(title, 'ar')}</h3>
      {showBilingual && isBilingualTitle && (
        <p className="text-xs text-gray-400 mt-0.5" dir="ltr">{pickLocale(title, 'en')}</p>
      )}
      {description && (
        <p className="mt-1 text-sm text-gray-500 max-w-md">{pickLocale(description, 'ar')}</p>
      )}
      {showBilingual && isBilingualDesc && description && (
        <p className="text-xs text-gray-400 mt-0.5 max-w-md" dir="ltr">{pickLocale(description, 'en')}</p>
      )}
      {action && <div className="mt-5">{action}</div>}
    </div>
  );
}
