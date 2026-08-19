'use client';

// مكوّن حقل إدخال موحد (Input) — Sprint 39 (DEC-125) Design System
// يدعم label, error, hint, iconLeft, iconRight, sizes

import { InputHTMLAttributes, forwardRef, ReactNode } from 'react';
import { cn } from '@/lib/utils';

export type InputSize = 'sm' | 'md' | 'lg';

export interface InputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'size'> {
  label?: string;
  error?: string;
  hint?: string;
  iconLeft?: ReactNode;
  iconRight?: ReactNode;
  containerClassName?: string;
  size?: InputSize;
}

const SIZE_STYLES: Record<InputSize, string> = {
  sm: 'h-8 px-2.5 text-xs rounded-md',
  md: 'h-10 px-3 text-sm rounded-lg',
  lg: 'h-12 px-4 text-base rounded-lg',
};

export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  {
    label,
    error,
    hint,
    iconLeft,
    iconRight,
    containerClassName,
    size = 'md',
    className,
    id,
    ...props
  },
  ref
) {
  const inputId = id || `input-${Math.random().toString(36).slice(2, 9)}`;
  return (
    <div className={cn('w-full', containerClassName)}>
      {label && (
        <label htmlFor={inputId} className="block text-sm font-medium text-ink-700 mb-1.5">
          {label}
        </label>
      )}
      <div className="relative">
        {iconLeft && (
          <span className="pointer-events-none absolute inset-y-0 right-0 flex items-center pr-3 text-ink-400">
            {iconLeft}
          </span>
        )}
        <input
          ref={ref}
          id={inputId}
          className={cn(
            // Base
            'w-full border bg-white text-ink-900 placeholder:text-ink-400',
            'transition-colors duration-150',
            'focus:outline-none focus:ring-2 focus:ring-offset-0',
            // Default state
            'border-ink-300 focus:border-brand-500 focus:ring-brand-500/20',
            // Disabled
            'disabled:bg-ink-50 disabled:text-ink-400 disabled:cursor-not-allowed',
            // Error state
            error && 'border-danger-500 focus:border-danger-500 focus:ring-danger-500/20',
            // Size
            SIZE_STYLES[size],
            // Icon padding
            iconLeft && 'pr-10',
            iconRight && 'pl-10',
            className
          )}
          {...props}
        />
        {iconRight && (
          <span className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-ink-400">
            {iconRight}
          </span>
        )}
      </div>
      {error ? (
        <p className="mt-1.5 text-xs text-danger-600">{error}</p>
      ) : hint ? (
        <p className="mt-1.5 text-xs text-ink-500">{hint}</p>
      ) : null}
    </div>
  );
});
