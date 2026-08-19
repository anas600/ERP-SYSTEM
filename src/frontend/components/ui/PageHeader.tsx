'use client';

// مكوّن PageHeader — عنوان الصفحة + الإجراءات (buttons)
// Sprint 39 (DEC-125): uses ink-* tokens, soft gradient optional

import { ReactNode } from 'react';
import Link from 'next/link';
import { cn } from '@/lib/utils';

export interface PageHeaderProps {
  title: ReactNode;
  description?: ReactNode;
  /** breadcrumb سابق للـ title */
  breadcrumb?: { label: string; href?: string }[];
  actions?: ReactNode;
  className?: string;
}

export function PageHeader({ title, description, breadcrumb, actions, className }: PageHeaderProps) {
  return (
    <div className={cn('mb-6', className)}>
      {breadcrumb && breadcrumb.length > 0 && (
        <nav className="text-xs text-ink-500 mb-2 flex items-center gap-1 flex-wrap">
          {breadcrumb.map((b, i) => (
            <span key={i} className="flex items-center gap-1">
              {b.href ? (
                <Link href={b.href} className="hover:text-brand-600">
                  {b.label}
                </Link>
              ) : (
                <span className="text-ink-700">{b.label}</span>
              )}
              {i < breadcrumb.length - 1 && <span className="text-ink-300">/</span>}
            </span>
          ))}
        </nav>
      )}
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-ink-800">{title}</h1>
          {description && <p className="text-sm text-ink-500 mt-1">{description}</p>}
        </div>
        {actions && <div className="flex items-center gap-2">{actions}</div>}
      </div>
    </div>
  );
}
