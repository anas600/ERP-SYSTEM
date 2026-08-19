'use client';

// مكوّن Badge للـ status display — Sprint 39 (DEC-125) Design System
// Variants: neutral | success | warning | danger | info | brand
// Uses ink-* + semantic color tokens

import { HTMLAttributes } from 'react';
import { cn } from '@/lib/utils';

export type BadgeVariant = 'neutral' | 'success' | 'warning' | 'danger' | 'info' | 'brand';
export type BadgeSize = 'sm' | 'md';

export interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  variant?: BadgeVariant;
  size?: BadgeSize;
}

const VARIANT_STYLES: Record<BadgeVariant, string> = {
  // Sprint 39 (DEC-125): use the new semantic color tokens
  neutral: 'bg-ink-100 text-ink-700',
  success: 'bg-success-100 text-success-700',
  warning: 'bg-warning-100 text-warning-700',
  danger: 'bg-danger-100 text-danger-700',
  info: 'bg-brand-100 text-brand-700',
  brand: 'bg-brand-500 text-white',
};

const SIZE_STYLES: Record<BadgeSize, string> = {
  sm: 'text-[10px] px-1.5 py-0.5',
  md: 'text-xs px-2 py-0.5',
};

export function Badge({
  variant = 'neutral',
  size = 'md',
  className,
  children,
  ...props
}: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full font-semibold',
        VARIANT_STYLES[variant],
        SIZE_STYLES[size],
        className
      )}
      {...props}
    >
      {children}
    </span>
  );
}
