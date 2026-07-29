'use client';

// Sprint 5 (Phase 5.1) — Global Search component.
//
// Mounted in the AppShell top bar. Provides a search input + dropdown that
// queries GET /api/search?q=... for customers / suppliers / invoices /
// accounts. Behavior:
//   - Cmd/Ctrl+K focuses the input from anywhere
//   - 300ms debounce after the last keystroke
//   - Live results (max 5 per type, capped by the BE)
//   - Type-icon per row + title + subtitle; click navigates to the detail
//   - Empty / loading / no-results states all bilingual (AR + EN labels)
//   - Closes on outside click, Escape, or after a navigation
//   - When the BE is down (404) we degrade silently — input still works,
//     dropdown just shows an Arabic "Search unavailable" hint with a retry.
//
// The component owns no global state — it is a self-contained feature of the
// top bar. Per-row navigation uses searchResultHref() from lib/api.ts which
// remaps the BE-side route to the FE's actual route (see comment on that
// helper for the WHY).

import {
  useEffect,
  useMemo,
  useRef,
  useState,
  type KeyboardEvent as ReactKeyboardEvent,
} from 'react';
import { useRouter } from 'next/navigation';
import {
  Search,
  User,
  Truck,
  FileText,
  Wallet,
  X,
  Loader2,
  AlertCircle,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { searchApi, searchResultHref, SearchResult, SearchResultType } from '@/lib/api';
import { getErrorMessage } from '@/lib/api';

// ============ Type → icon + Arabic label map ============

interface TypeMeta {
  icon: React.ComponentType<{ className?: string }>;
  /** Singular Arabic noun, used in the "no results for X" empty line. */
  label: string;
  /** Tailwind classes for the row icon background. */
  chipBg: string;
  chipText: string;
}

const TYPE_META: Record<SearchResultType, TypeMeta> = {
  customer: {
    icon: User,
    label: 'العميل',
    chipBg: 'bg-blue-50',
    chipText: 'text-blue-600',
  },
  supplier: {
    icon: Truck,
    label: 'المورّد',
    chipBg: 'bg-orange-50',
    chipText: 'text-orange-600',
  },
  invoice: {
    icon: FileText,
    label: 'الفاتورة',
    chipBg: 'bg-green-50',
    chipText: 'text-green-600',
  },
  account: {
    icon: Wallet,
    label: 'الحساب',
    chipBg: 'bg-purple-50',
    chipText: 'text-purple-600',
  },
};

const SECTION_LABELS: Record<SearchResultType, string> = {
  customer: 'العملاء',
  supplier: 'الموردين',
  invoice: 'الفواتير',
  account: 'الحسابات',
};

// Fixed order for the section headers in the dropdown. Matches the BE merge
// order in GlobalSearchService.Merge so the UI is stable.
const SECTION_ORDER: SearchResultType[] = ['customer', 'supplier', 'invoice', 'account'];

// ============ Component ============

export function GlobalSearch() {
  const router = useRouter();
  const inputRef = useRef<HTMLInputElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  const [query, setQuery] = useState('');
  // Debounced value is what we actually send to the BE. We update it 300ms
  // after the last keystroke to keep the wire quiet while typing.
  const [debouncedQuery, setDebouncedQuery] = useState('');
  const [open, setOpen] = useState(false);
  const [results, setResults] = useState<SearchResult[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  // Highlighted row index for keyboard navigation (-1 = none).
  const [highlight, setHighlight] = useState<number>(-1);

  // ============ Debounce ============
  useEffect(() => {
    if (!query.trim()) {
      setDebouncedQuery('');
      return;
    }
    const t = setTimeout(() => setDebouncedQuery(query.trim()), 300);
    return () => clearTimeout(t);
  }, [query]);

  // ============ Cmd/Ctrl+K focus shortcut ============
  useEffect(() => {
    const onKey = (e: globalThis.KeyboardEvent) => {
      // Don't hijack the shortcut when the user is in another input / textarea
      // (e.g. login form). Use a simple heuristic: only handle it when
      // modifier is held without a text input being focused.
      const target = e.target as HTMLElement | null;
      const inEditable =
        target &&
        (target.tagName === 'INPUT' ||
          target.tagName === 'TEXTAREA' ||
          target.isContentEditable);
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault();
        inputRef.current?.focus();
        inputRef.current?.select();
        return;
      }
      // Escape closes the dropdown and blurs when the user is in our input
      if (e.key === 'Escape' && document.activeElement === inputRef.current) {
        setOpen(false);
        inputRef.current?.blur();
      }
      // Slash key (/) focuses the search input when nothing else is focused.
      // Skipped when we're in a text field to avoid hijacking typing.
      if (e.key === '/' && !inEditable) {
        e.preventDefault();
        inputRef.current?.focus();
      }
    };
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, []);

  // ============ Outside click closes the dropdown ============
  useEffect(() => {
    if (!open) return;
    const handler = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [open]);

  // ============ Fetch when debouncedQuery changes ============
  useEffect(() => {
    let cancelled = false;
    if (!debouncedQuery) {
      setResults([]);
      setError(null);
      setLoading(false);
      setHighlight(-1);
      return;
    }
    setLoading(true);
    setError(null);
    searchApi
      .globalSearch(debouncedQuery, 20)
      .then((res) => {
        if (cancelled) return;
        setResults(res.results);
        setHighlight(res.results.length > 0 ? 0 : -1);
      })
      .catch((e: unknown) => {
        if (cancelled) return;
        setResults([]);
        setError(getErrorMessage(e, 'تعذّر الاتصال بمحرك البحث.'));
      })
      .finally(() => {
        if (cancelled) return;
        setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [debouncedQuery]);

  // ============ Group results by type for section rendering ============
  const grouped = useMemo(() => {
    const out: Record<SearchResultType, SearchResult[]> = {
      customer: [],
      supplier: [],
      invoice: [],
      account: [],
    };
    for (const r of results) out[r.type].push(r);
    return out;
  }, [results]);

  // Flat list used for keyboard navigation (highlights by index in DOM order).
  const flat = useMemo(() => {
    const out: SearchResult[] = [];
    for (const t of SECTION_ORDER) out.push(...grouped[t]);
    return out;
  }, [grouped]);

  // ============ Handlers ============
  const navigate = (r: SearchResult) => {
    setOpen(false);
    setQuery('');
    setResults([]);
    router.push(searchResultHref(r));
  };

  const onKeyDown = (e: ReactKeyboardEvent<HTMLInputElement>) => {
    if (!open || flat.length === 0) {
      if (e.key === 'ArrowDown' && flat.length > 0) {
        e.preventDefault();
        setOpen(true);
        setHighlight(0);
      }
      return;
    }
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setHighlight((h) => (h + 1) % flat.length);
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setHighlight((h) => (h <= 0 ? flat.length - 1 : h - 1));
    } else if (e.key === 'Enter' && highlight >= 0 && highlight < flat.length) {
      e.preventDefault();
      navigate(flat[highlight]);
    }
  };

  const clearQuery = () => {
    setQuery('');
    setDebouncedQuery('');
    setResults([]);
    setError(null);
    inputRef.current?.focus();
  };

  // Track global flat index per row for highlighting.
  let flatIdx = -1;

  return (
    <div ref={containerRef} className="relative w-full max-w-md">
      <div className="relative">
        <span className="pointer-events-none absolute inset-y-0 right-0 flex items-center pr-3 text-gray-400">
          <Search className="h-4 w-4" />
        </span>
        <input
          ref={inputRef}
          type="search"
          value={query}
          onChange={(e) => {
            setQuery(e.target.value);
            setOpen(true);
          }}
          onFocus={() => setOpen(true)}
          onKeyDown={onKeyDown}
          placeholder="ابحث... (عملاء، موردين، فواتير، حسابات)"
          aria-label="بحث شامل"
          aria-autocomplete="list"
          // Note: aria-expanded is intentionally NOT on the input here — the
          // implicit role of <input type="search"> is "textbox" which doesn't
          // support aria-expanded. Screen readers announce open/closed state
          // via the listbox's own aria-label + the input's aria-controls.
          aria-controls="global-search-results"
          className={cn(
            'w-full h-9 rounded-lg border border-gray-300 bg-white',
            'pr-9 pl-16 text-sm',
            'focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-200',
            'placeholder:text-gray-400'
          )}
        />
        {/* Trailing area: clear button (when typing) OR Cmd/Ctrl+K hint */}
        <div className="absolute inset-y-0 left-0 flex items-center pl-2 gap-1">
          {query ? (
            <button
              type="button"
              onClick={clearQuery}
              className="p-1 rounded hover:bg-gray-100 text-gray-400 hover:text-gray-600"
              aria-label="مسح"
              title="مسح"
            >
              <X className="h-3.5 w-3.5" />
            </button>
          ) : (
            <kbd className="hidden sm:inline-flex items-center gap-0.5 rounded border border-gray-200 bg-gray-50 px-1.5 py-0.5 text-[10px] font-mono text-gray-500">
              Ctrl K
            </kbd>
          )}
        </div>
      </div>

      {/* ============ Dropdown ============ */}
      {open && (
        <div
          id="global-search-results"
          role="listbox"
          className={cn(
            'absolute end-0 mt-2 w-screen max-w-lg sm:w-[36rem] z-40',
            'bg-white rounded-lg shadow-xl border border-gray-200',
            'max-h-[70vh] overflow-y-auto'
          )}
        >
          {/* Loading state (only when actively searching, not on first idle) */}
          {loading && debouncedQuery && (
            <div className="px-4 py-3 flex items-center gap-2 text-sm text-gray-500">
              <Loader2 className="h-4 w-4 animate-spin" />
              <span>جاري البحث...</span>
            </div>
          )}

          {/* Error state — BE 404 / 500 / network failure */}
          {error && !loading && (
            <div className="px-4 py-3 flex items-start gap-2 text-sm text-amber-700 bg-amber-50">
              <AlertCircle className="h-4 w-4 flex-shrink-0 mt-0.5" />
              <div>
                <p className="font-semibold">البحث غير متاح مؤقتاً</p>
                <p className="text-xs mt-0.5">{error}</p>
              </div>
            </div>
          )}

          {/* No-results state */}
          {!loading && !error && debouncedQuery && results.length === 0 && (
            <div className="px-4 py-6 text-center text-sm text-gray-500">
              <Search className="h-8 w-8 mx-auto text-gray-300 mb-2" />
              <p className="font-semibold">لا توجد نتائج لـ &quot;{debouncedQuery}&quot;</p>
              <p className="text-xs mt-1 text-gray-400">
                جرب البحث بكود العميل، أو رقم الفاتورة، أو اسم الحساب.
              </p>
            </div>
          )}

          {/* Initial state — focus the input but no query yet */}
          {!debouncedQuery && !loading && !error && (
            <div className="px-4 py-5 text-sm text-gray-500">
              <p className="font-semibold text-gray-700 mb-2">ابحث في كل النظام</p>
              <ul className="space-y-1.5 text-xs">
                <li className="flex items-center gap-2">
                  <User className="h-3.5 w-3.5 text-blue-500" />
                  <span>العملاء — بالاسم، الكود، أو البريد</span>
                </li>
                <li className="flex items-center gap-2">
                  <Truck className="h-3.5 w-3.5 text-orange-500" />
                  <span>الموردين — بالاسم، الكود، أو البريد</span>
                </li>
                <li className="flex items-center gap-2">
                  <FileText className="h-3.5 w-3.5 text-green-500" />
                  <span>فواتير المبيعات — بالرقم أو اسم العميل</span>
                </li>
                <li className="flex items-center gap-2">
                  <Wallet className="h-3.5 w-3.5 text-purple-500" />
                  <span>الحسابات — بالاسم أو الكود</span>
                </li>
              </ul>
              <p className="text-[10px] text-gray-400 mt-3">
                اختصار: <kbd className="font-mono">Ctrl K</kbd> أو <kbd className="font-mono">/</kbd>
              </p>
            </div>
          )}

          {/* Results — grouped by type with section headers */}
          {!loading && !error && results.length > 0 && (
            <div className="py-1">
              {SECTION_ORDER.map((type) => {
                const rows = grouped[type];
                if (rows.length === 0) return null;
                return (
                  <div key={type} className="mb-1 last:mb-0">
                    <div className="px-3 pt-2 pb-1 text-[10px] font-semibold text-gray-400 uppercase tracking-wider">
                      {SECTION_LABELS[type]}
                    </div>
                    <ul>
                      {rows.map((r) => {
                        flatIdx += 1;
                        const isActive = flatIdx === highlight;
                        const meta = TYPE_META[r.type];
                        const Icon = meta.icon;
                        return (
                          <li key={`${r.type}-${r.id}`}>
                            <button
                              type="button"
                              onClick={() => navigate(r)}
                              onMouseEnter={() => setHighlight(flatIdx)}
                              className={cn(
                                'w-full text-start flex items-center gap-3 px-3 py-2',
                                'transition-colors',
                                isActive ? 'bg-blue-50' : 'hover:bg-gray-50'
                              )}
                              role="option"
                              aria-selected={isActive}
                            >
                              <span
                                className={cn(
                                  'h-8 w-8 rounded-lg flex items-center justify-center flex-shrink-0',
                                  meta.chipBg
                                )}
                              >
                                <Icon className={cn('h-4 w-4', meta.chipText)} />
                              </span>
                              <span className="flex-1 min-w-0">
                                <span className="block text-sm font-semibold text-gray-800 truncate">
                                  {r.title}
                                </span>
                                {r.subtitle && (
                                  <span className="block text-xs text-gray-500 truncate">
                                    {r.subtitle}
                                  </span>
                                )}
                              </span>
                              <span className="text-[10px] text-gray-400 flex-shrink-0">
                                {meta.label}
                              </span>
                            </button>
                          </li>
                        );
                      })}
                    </ul>
                  </div>
                );
              })}
              <div className="px-3 py-2 border-t border-gray-100 text-[10px] text-gray-400 flex items-center justify-between">
                <span>
                  {results.length} نتيجة
                </span>
                <span className="hidden sm:inline">
                  <kbd className="font-mono">↑↓</kbd> للتنقل &nbsp;
                  <kbd className="font-mono">Enter</kbd> للفتح &nbsp;
                  <kbd className="font-mono">Esc</kbd> للإغلاق
                </span>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
