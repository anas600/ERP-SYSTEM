'use client';

// مكوّن زر موحد (Button) — Sprint 39 (DEC-125) Design System overhaul
// الـ variants: primary | secondary | danger | ghost | outline | success
// الـ sizes: xs | sm | md | lg
// Uses soft shadows + smooth transitions for a modern feel.

import { ButtonHTMLAttributes, forwardRef } from 'react';
import { Loader2 } from 'lucide-react';
import { cn } from '@/lib/utils';

export type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'success' | 'ghost' | 'outline';
export type ButtonSize = 'xs' | 'sm' | 'md' | 'lg';

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  loading?: boolean;
  iconLeft?: React.ReactNode;
  iconRight?: React.ReactNode;
  fullWidth?: boolean;
}

const VARIANT_STYLES: Record<ButtonVariant, string> = {
  // Primary = brand blue, with soft shadow on hover
  primary:
    'bg-brand-500 text-white shadow-soft-sm hover:bg-brand-600 hover:shadow-soft active:bg-brand-700 focus:ring-brand-500/30 disabled:bg-brand-300 disabled:shadow-none',
  // Secondary = neutral gray, subtle hover
  secondary:
    'bg-ink-100 text-ink-800 hover:bg-ink-200 focus:ring-ink-400/30 disabled:bg-ink-50 disabled:text-ink-400',
  // Danger = red, for destructive actions
  danger:
    'bg-danger-500 text-white shadow-soft-sm hover:bg-danger-600 hover:shadow-soft active:bg-danger-700 focus:ring-danger-500/30 disabled:bg-danger-300 disabled:shadow-none',
  // Success = green, for positive confirmations
  success:
    'bg-success-500 text-white shadow-soft-sm hover:bg-success-600 hover:shadow-soft active:bg-success-700 focus:ring-success-500/30 disabled:bg-success-300 disabled:shadow-none',
  // Ghost = transparent, for low-emphasis
  ghost:
    'bg-transparent text-ink-700 hover:bg-ink-100 focus:ring-ink-400/30 disabled:text-ink-300',
  // Outline = bordered, for secondary actions in dense UIs
  outline:
    'bg-white text-ink-700 border border-ink-300 hover:bg-ink-50 hover:border-ink-400 focus:ring-brand-500/30 disabled:bg-ink-50 disabled:text-ink-400',
};

const SIZE_STYLES: Record<ButtonSize, string> = {
  xs: 'h-7 px-2.5 text-xs rounded',
  sm: 'h-9 px-3.5 text-sm rounded-lg',
  md: 'h-10 px-4 text-sm rounded-lg',
  lg: 'h-12 px-6 text-base rounded-xl',
};

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  {
    variant = 'primary',
    size = 'md',
    loading = false,
    disabled,
    iconLeft,
    iconRight,
    fullWidth = false,
    className,
    children,
    ...props
  },
  ref
) {
  return (
    <button
      ref={ref}
      disabled={disabled || loading}
      className={cn(
        // Base layout
        'inline-flex items-center justify-center gap-2 font-semibold',
        'focus:outline-none focus:ring-2 focus:ring-offset-1 focus:ring-offset-white',
        'disabled:cursor-not-allowed',
        // Subtle press feedback
        'active:scale-[0.98]',
        // Variant + size
        VARIANT_STYLES[variant],
        SIZE_STYLES[size],
        // Full width
        fullWidth && 'w-full',
        className
      )}
      {...props}
    >
      {loading ? (
        <Loader2 className="h-4 w-4 animate-spin" />
      ) : (
        iconLeft && <span className="inline-flex flex-shrink-0">{iconLeft}</span>
      )}
      <span className="truncate">{children}</span>
      {!loading && iconRight && <span className="inline-flex flex-shrink-0">{iconRight}</span>}
    </button>
  );
});
