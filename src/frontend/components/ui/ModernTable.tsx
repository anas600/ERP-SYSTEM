'use client';

// Sprint 59 — ModernTable
//
// Drop-in replacement for the existing <Table> with: sticky header, hover
// rows, gradient header background, responsive wrapper, optional click
// handler and row-level key extraction. Keeps the same <Table> <TableColumn>
// contract so existing pages can swap without API changes.

import { ReactNode } from 'react';
import { cn } from '@/lib/utils';

export interface ModernTableColumn<T> {
  key: string;
  header: string;
  /** Optional fixed width (e.g. 'w-32'). */
  widthClass?: string;
  /** Optional alignment. Default: 'start'. */
  align?: 'start' | 'center' | 'end';
  /** Custom cell renderer. */
  render: (row: T) => ReactNode;
  /** Optional CSS class on the <th>/<td> cells. */
  className?: string;
}

export interface ModernTableProps<T> {
  columns: ModernTableColumn<T>[];
  rows: T[];
  rowKey: (row: T) => string;
  /** Optional click handler — entire row becomes a clickable Link/button. */
  onRowClick?: (row: T) => void;
  /** Hint shown when rows is empty (default: "لا توجد بيانات"). */
  emptyMessage?: string;
  /** Subtitle shown in the empty state. */
  emptyHint?: string;
  className?: string;
}

export function ModernTable<T>({
  columns,
  rows,
  rowKey,
  onRowClick,
  emptyMessage = 'لا توجد بيانات',
  emptyHint,
  className,
}: ModernTableProps<T>) {
  if (rows.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center rounded-2xl border-2 border-dashed border-gray-200 bg-gray-50/50 px-6 py-12 text-center">
        <div className="flex h-14 w-14 items-center justify-center rounded-full bg-white shadow-sm ring-1 ring-gray-200">
          <span className="text-2xl text-gray-400">∅</span>
        </div>
        <h3 className="mt-3 text-sm font-semibold text-gray-900">{emptyMessage}</h3>
        {emptyHint && <p className="mt-1 max-w-sm text-xs text-gray-500">{emptyHint}</p>}
      </div>
    );
  }
  return (
    <div className={cn('overflow-hidden rounded-2xl bg-white shadow-sm ring-1 ring-gray-200/70', className)}>
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="bg-gradient-to-l from-slate-50 to-slate-100/50 text-[11px] uppercase tracking-wider text-slate-500">
            <tr>
              {columns.map((c) => (
                <th
                  key={c.key}
                  className={cn(
                    'px-4 py-3 font-bold',
                    c.align === 'end' ? 'text-end' : c.align === 'center' ? 'text-center' : 'text-start',
                    c.widthClass,
                    c.className,
                  )}
                >
                  {c.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {rows.map((row) => {
              const clickable = !!onRowClick;
              return (
                <tr
                  key={rowKey(row)}
                  onClick={clickable ? () => onRowClick!(row) : undefined}
                  className={cn(
                    'transition-colors',
                    clickable ? 'cursor-pointer hover:bg-blue-50/40' : 'hover:bg-slate-50/40',
                  )}
                >
                  {columns.map((c) => (
                    <td
                      key={c.key}
                      className={cn(
                        'px-4 py-3 align-middle',
                        c.align === 'end' ? 'text-end' : c.align === 'center' ? 'text-center' : 'text-start',
                        c.className,
                      )}
                    >
                      {c.render(row)}
                    </td>
                  ))}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

// ============== FilterBar ==============

export interface FilterChip {
  key: string;
  label: string;
  count?: number;
  tone?: 'slate' | 'green' | 'amber' | 'red' | 'blue' | 'violet';
}

export function FilterChips({
  chips,
  active,
  onChange,
  className,
}: {
  chips: FilterChip[];
  active: string;
  onChange: (key: string) => void;
  className?: string;
}) {
  return (
    <div className={cn('flex flex-wrap items-center gap-2 rounded-2xl bg-white p-2 shadow-sm ring-1 ring-gray-200/70', className)}>
      {chips.map((c) => {
        const isActive = c.key === active;
        const activeBg = isActive
          ? c.tone === 'slate'
            ? 'bg-slate-900 text-white'
            : c.tone === 'green'
              ? 'bg-emerald-600 text-white'
              : c.tone === 'amber'
                ? 'bg-amber-500 text-white'
                : c.tone === 'red'
                  ? 'bg-rose-600 text-white'
                  : c.tone === 'violet'
                    ? 'bg-violet-600 text-white'
                    : 'bg-blue-600 text-white'
          : 'bg-gray-50 text-gray-600 hover:bg-gray-100';
        return (
          <button
            key={c.key}
            onClick={() => onChange(c.key)}
            className={cn(
              'flex items-center gap-2 rounded-xl px-4 py-2 text-sm font-bold transition',
              activeBg,
            )}
          >
            <span>{c.label}</span>
            {c.count != null && (
              <span
                className={cn(
                  'rounded-full px-2 py-0.5 text-[11px] font-bold tabular-nums',
                  isActive ? 'bg-white/20 text-white' : 'bg-white text-gray-500 ring-1 ring-gray-200',
                )}
              >
                {c.count}
              </span>
            )}
          </button>
        );
      })}
    </div>
  );
}

// ============== HeroSearch ==============

export function HeroSearch({
  value,
  onChange,
  placeholder,
  className,
}: {
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
  className?: string;
}) {
  return (
    <div className={cn('relative flex-1 max-w-md', className)}>
      <svg
        className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-white/60"
        fill="none"
        viewBox="0 0 24 24"
        stroke="currentColor"
      >
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
      </svg>
      <input
        type="search"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        className="w-full rounded-lg border-0 bg-white/95 px-3 py-2 pe-9 text-sm text-gray-900 placeholder:text-gray-400 shadow-sm focus:outline-none focus:ring-2 focus:ring-white/40"
      />
    </div>
  );
}
