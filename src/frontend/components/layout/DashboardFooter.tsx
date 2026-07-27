'use client';

// مكوّن Footer — يظهر أسفل محتوى لوحة التحكم والصفحات الداخلية
// يحتوي على: حقوق النشر + رقم الإصدار + رابط الدعم
// الاستخدام:
//   import { DashboardFooter } from '@/components/layout/DashboardFooter';
//   <DashboardFooter />

import { LifeBuoy, Github, BookOpen } from 'lucide-react';

export interface DashboardFooterProps {
  /** Override الـ version — اختياري */
  version?: string;
  /** Override الـ product name — اختياري */
  productName?: string;
  /** Override الـ support link — اختياري */
  supportHref?: string;
  className?: string;
}

export function DashboardFooter({
  version = 'v1.0.34-hotfix2',
  productName = 'Alfajr ERP',
  supportHref = 'mailto:support@alfajr-erp.local',
  className,
}: DashboardFooterProps) {
  const year = new Date().getFullYear();
  return (
    <footer
      dir="rtl"
      className={
        'mt-8 pt-4 pb-2 border-t border-gray-200 text-xs text-gray-500 flex flex-wrap items-center justify-center gap-x-4 gap-y-1 ' +
        (className ?? '')
      }
    >
      <span>
        © {year} {productName} — Multi-Company Edition — {version}
      </span>
      <a
        href={supportHref}
        className="inline-flex items-center gap-1 text-gray-500 hover:text-blue-600 transition-colors"
        aria-label="الدعم الفني"
      >
        <LifeBuoy className="h-3 w-3" />
        الدعم
      </a>
      <a
        href="https://github.com/alfajr/erp-system"
        target="_blank"
        rel="noopener noreferrer"
        className="inline-flex items-center gap-1 text-gray-500 hover:text-blue-600 transition-colors"
        aria-label="مستودع الكود"
      >
        <Github className="h-3 w-3" />
        الكود
      </a>
      <a
        href="/docs"
        className="inline-flex items-center gap-1 text-gray-500 hover:text-blue-600 transition-colors"
        aria-label="دليل الاستخدام"
      >
        <BookOpen className="h-3 w-3" />
        الدليل
      </a>
    </footer>
  );
}
