'use client';

// مكوّن EmptyState — يُعرض عندما لا توجد بيانات (قائمة فارغة)
// Sprint 39 (DEC-125): uses ink-* tokens, soft gradient backdrop, brand icon circle
// الاستخدام:
//   <EmptyState
//     icon={<Users className="h-12 w-12" />}
//     title="لا يوجد عملاء"
//     description="ابدأ بإضافة عميل جديد"
//     action={<Button>إضافة عميل</Button>}
//   />

import { ReactNode } from 'react';
import { cn } from '@/lib/utils';

export interface EmptyStateProps {
  /** أيقونة توضيحية (مطلوب — يفضل lucide-react icon بحجم h-12 w-12) */
  icon?: ReactNode;
  /** العنوان الرئيسي */
  title: string;
  /** وصف ثانوي اختياري */
  description?: string;
  /** زر أو رابط إجراء اختياري (مثلاً "إضافة جديد") */
  action?: ReactNode;
  /** كلاسات إضافية للـ container الخارجي */
  className?: string;
}

export function EmptyState({ icon, title, description, action, className }: EmptyStateProps) {
  return (
    <div
      dir="rtl"
      className={cn(
        'flex flex-col items-center justify-center text-center',
        'bg-gradient-to-b from-white to-ink-50 border border-ink-200 rounded-xl shadow-soft',
        'py-12 px-6',
        className
      )}
    >
      {icon && (
        <div className="mb-4 inline-flex h-16 w-16 items-center justify-center rounded-full bg-gradient-to-br from-brand-50 to-brand-100 border border-brand-200 text-brand-600 shadow-soft">
          {icon}
        </div>
      )}
      <h3 className="text-base font-semibold text-ink-800">{title}</h3>
      {description && <p className="mt-1 text-sm text-ink-500 max-w-md">{description}</p>}
      {action && <div className="mt-5">{action}</div>}
    </div>
  );
}
