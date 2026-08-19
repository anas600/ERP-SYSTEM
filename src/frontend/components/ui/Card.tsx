'use client';

// مكوّن Card — Sprint 39 (DEC-125) Design System
// container للـ widgets / KPIs / القوائم — uses design tokens, hover lift, gradient accents

import { HTMLAttributes, ReactNode } from 'react';
import { cn } from '@/lib/utils';

export interface CardProps extends Omit<HTMLAttributes<HTMLDivElement>, 'title'> {
  title?: ReactNode;
  description?: ReactNode;
  actions?: ReactNode;
  footer?: ReactNode;
  /** لون الحد الأيمن (RTL) — يعطي تمييز بصري للـ category */
  accent?: 'blue' | 'green' | 'purple' | 'yellow' | 'red' | 'gray' | 'none';
  /** Whether the card is interactive (hoverable with shadow lift) */
  interactive?: boolean;
  /** Padding override — useful for tight layouts */
  noPadding?: boolean;
}

const ACCENT_STYLES: Record<NonNullable<CardProps['accent']>, string> = {
  blue: 'border-r-4 border-brand-500',
  green: 'border-r-4 border-success-500',
  purple: 'border-r-4 border-purple-500',
  yellow: 'border-r-4 border-warning-500',
  red: 'border-r-4 border-danger-500',
  gray: 'border-r-4 border-ink-300',
  none: '',
};

export function Card({
  title,
  description,
  actions,
  footer,
  accent = 'none',
  interactive = false,
  noPadding = false,
  className,
  children,
  ...props
}: CardProps) {
  return (
    <div
      className={cn(
        // Base
        'bg-white rounded-xl border border-ink-200 shadow-soft',
        'transition-all duration-200',
        // Interactive (hoverable)
        interactive && 'cursor-pointer hover:shadow-soft-md hover:-translate-y-0.5',
        // Accent
        ACCENT_STYLES[accent],
        className
      )}
      {...props}
    >
      {(title || actions) && (
        <div className="flex items-center justify-between gap-3 px-5 py-4 border-b border-ink-100">
          <div className="min-w-0 flex-1">
            {title && <h3 className="font-bold text-ink-800 truncate">{title}</h3>}
            {description && <p className="text-sm text-ink-500 mt-0.5 truncate">{description}</p>}
          </div>
          {actions && <div className="flex items-center gap-2 flex-shrink-0">{actions}</div>}
        </div>
      )}
      <div className={cn(noPadding ? '' : 'p-5', footer && 'pb-0')}>{children}</div>
      {footer && (
        <div className="px-5 py-3 border-t border-ink-100 bg-ink-50 rounded-b-xl">
          {footer}
        </div>
      )}
    </div>
  );
}
