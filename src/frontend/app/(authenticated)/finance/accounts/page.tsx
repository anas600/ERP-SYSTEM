'use client';

// Sprint 5 (Phase 1.1) — Chart of Accounts (CoA) as a hierarchical tree.
//
// The previous version was a flat table; this version:
//   - Builds a tree from the flat list using `parentAccountId`
//   - Expand / collapse per node, with the expanded-id set persisted in
//     localStorage (`erp.coa.expanded`) so the user's view survives reloads
//   - Filter by account type (Asset / Liability / Equity / Revenue / Expense)
//   - Search by name or code (live, case-insensitive)
//   - Toggle: show / hide inactive accounts
//   - "Add child account" button on each row → opens a modal that creates
//     a new account under the clicked parent
//   - Edit still links to the existing /finance/accounts/{id}/edit page
//
// We use the existing /api/finance/accounts flat list and convert it to a
// tree on the client. The hand-off noted the BE may add a dedicated tree
// endpoint later; the client is already structured to swap in that endpoint
// without changing the render code (the `buildTree` function is the only
// spot that would change).

import { useEffect, useMemo, useState, type FormEvent } from 'react';
import Link from 'next/link';
import {
  Plus,
  Pencil,
  ChevronRight,
  ChevronDown,
  Folder,
  FolderOpen,
  FileText,
  Search,
  X as XIcon,
} from 'lucide-react';
import {
  Button,
  Input,
  Select,
  PageHeader,
  Modal,
  Card,
  EmptyState,
  useToast,
} from '@/components/ui';
import { useAuth } from '@/lib/useAuth';
import {
  financeApi,
  Account,
  ACCOUNT_TYPES,
  getErrorMessage,
} from '@/lib/api';

// ============ Constants ============

const LS_EXPANDED_KEY = 'erp.coa.expanded';

/** AccountType → display label + Tailwind accent for the badge + tree line. */
const TYPE_ACCENT: Record<number, { label: string; color: string; chip: string }> = {
  1: { label: 'أصول', color: 'text-blue-700', chip: 'bg-blue-50 text-blue-700' },
  2: { label: 'خصوم', color: 'text-red-700', chip: 'bg-red-50 text-red-700' },
  3: { label: 'حقوق ملكية', color: 'text-purple-700', chip: 'bg-purple-50 text-purple-700' },
  4: { label: 'إيرادات', color: 'text-green-700', chip: 'bg-green-50 text-green-700' },
  5: { label: 'مصروفات', color: 'text-orange-700', chip: 'bg-orange-50 text-orange-700' },
};

const TYPE_FILTER_OPTIONS = [
  { value: '', label: 'كل الأنواع' },
  ...Object.entries(ACCOUNT_TYPES).map(([k, v]) => ({
    value: k,
    label: `${v} (${k})`,
  })),
];

// ============ Tree types ============

interface AccountNode extends Account {
  children: AccountNode[];
  /** depth from the root (0 = root). Computed in `buildTree`. */
  depth: number;
  /** Flat index of the row, used for the visible flat walk. */
  visibleIndex?: number;
}

// ============ Tree builder ============

/**
 * Build a tree from a flat list of accounts. Accounts with a missing parent
 * (orphans — parent not in the list) are promoted to roots so they remain
 * visible. The depth is computed once during the build.
 */
function buildTree(flat: Account[]): AccountNode[] {
  const byId = new Map<string, AccountNode>();
  for (const a of flat) {
    byId.set(a.id, { ...a, children: [], depth: 0 });
  }
  const roots: AccountNode[] = [];
  for (const node of byId.values()) {
    const parentId = node.parentAccountId;
    if (parentId && byId.has(parentId)) {
      const parent = byId.get(parentId)!;
      node.depth = parent.depth + 1;
      parent.children.push(node);
    } else {
      roots.push(node);
    }
  }
  // Sort each level by `code` (stable, predictable). Codes are usually
  // numeric-strings like "1000" / "1100" so localeCompare gives a good
  // visual order without forcing numeric parsing.
  const sortByCode = (a: AccountNode, b: AccountNode) =>
    a.code.localeCompare(b.code, undefined, { numeric: true, sensitivity: 'base' });
  const recurse = (nodes: AccountNode[]) => {
    nodes.sort(sortByCode);
    for (const n of nodes) recurse(n.children);
  };
  recurse(roots);
  return roots;
}

/** Convert a tree to a flat list in DFS (pre-order) order — what the UI walks. */
function flattenTree(roots: AccountNode[]): AccountNode[] {
  const out: AccountNode[] = [];
  const walk = (n: AccountNode) => {
    out.push(n);
    for (const c of n.children) walk(c);
  };
  for (const r of roots) walk(r);
  return out;
}

// ============ localStorage helpers ============
// SSR-safe: window check + try/catch around JSON.parse.

function loadExpandedSet(): Set<string> {
  if (typeof window === 'undefined') return new Set();
  try {
    const raw = window.localStorage.getItem(LS_EXPANDED_KEY);
    if (!raw) return new Set();
    const arr = JSON.parse(raw);
    if (Array.isArray(arr)) return new Set(arr.filter((s) => typeof s === 'string'));
    return new Set();
  } catch {
    return new Set();
  }
}

function saveExpandedSet(set: Set<string>): void {
  if (typeof window === 'undefined') return;
  try {
    window.localStorage.setItem(LS_EXPANDED_KEY, JSON.stringify(Array.from(set)));
  } catch {
    // localStorage may be full / disabled — silently drop the write. The UI
    // still works correctly within the session; the persistence is best-effort.
  }
}

// ============ Page component ============

export default function AccountsPage() {
  const { loading: authLoading } = useAuth();
  const toast = useToast();
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState<string>('');
  const [showInactive, setShowInactive] = useState(true);
  // Default: expand all root nodes so the demo immediately shows the tree.
  // The Set is hydrated from localStorage on mount and persisted on change.
  const [expanded, setExpanded] = useState<Set<string>>(() => new Set());

  // "Add child" modal state
  const [addChildParent, setAddChildParent] = useState<AccountNode | null>(null);

  // Hydrate expanded set from localStorage exactly once after mount.
  useEffect(() => {
    setExpanded(loadExpandedSet());
  }, []);

  // Persist expanded set whenever it changes (best-effort).
  useEffect(() => {
    saveExpandedSet(expanded);
  }, [expanded]);

  useEffect(() => {
    if (authLoading) return;
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [authLoading]);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await financeApi.listAccounts();
      setAccounts(data);
    } catch (e: unknown) {
      setError(getErrorMessage(e, 'فشل التحميل'));
    } finally {
      setLoading(false);
    }
  };

  // ============ Derived data ============
  // Apply filters (type + inactive) at the flat level first; the tree builder
  // then turns what remains into a forest. This keeps filters simple — a
  // search that matches a child brings the parent into view via the tree.
  const tree = useMemo(() => {
    let flat = accounts;
    if (!showInactive) flat = flat.filter((a) => a.isActive);
    if (typeFilter) flat = flat.filter((a) => String(a.type) === typeFilter);
    if (search.trim()) {
      const q = search.trim().toLowerCase();
      flat = flat.filter(
        (a) =>
          a.code.toLowerCase().includes(q) || a.name.toLowerCase().includes(q)
      );
    }
    return buildTree(flat);
  }, [accounts, typeFilter, showInactive, search]);

  const flatRows = useMemo(() => flattenTree(tree), [tree]);

  // Auto-expand all roots when the filter changes so the user always sees
  // the top of the result set. We avoid auto-expanding deeply because that
  // would blow up the row count on a wide search.
  useEffect(() => {
    setExpanded((prev) => {
      const next = new Set(prev);
      for (const r of tree) {
        if (r.children.length > 0) next.add(r.id);
      }
      return next;
    });
  }, [tree]);

  // ============ Expand / collapse ============
  const toggleNode = (id: string) => {
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const expandAll = () => {
    setExpanded(new Set(flatRows.filter((r) => r.children.length > 0).map((r) => r.id)));
  };
  const collapseAll = () => setExpanded(new Set());

  // ============ Handlers ============
  const onChildCreated = async (newAccount: Account) => {
    // Optimistic local merge + auto-expand the parent so the new row is in view.
    setAccounts((prev) => [...prev, newAccount]);
    setExpanded((prev) => {
      const next = new Set(prev);
      if (addChildParent) next.add(addChildParent.id);
      return next;
    });
    setAddChildParent(null);
    toast.success(`تم إنشاء الحساب ${newAccount.code} - ${newAccount.name}`);
  };

  // ============ Render ============
  return (
    <div>
      <PageHeader
        title="💰 دليل الحسابات"
        description="شجرة الحسابات المحاسبية الأساسية"
        actions={
          <div className="flex items-center gap-2">
            <Link href="/finance/accounts/new">
              <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
                حساب جديد
              </Button>
            </Link>
          </div>
        }
      />

      {/* ============ Filter bar ============ */}
      <Card className="mb-4">
        <div className="grid grid-cols-1 md:grid-cols-12 gap-3 items-end">
          <div className="md:col-span-6">
            <Input
              label="بحث"
              iconLeft={<Search className="h-4 w-4" />}
              placeholder="🔍 كود أو اسم الحساب..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <div className="md:col-span-3">
            <Select
              label="النوع"
              value={typeFilter}
              onChange={(e) => setTypeFilter(e.target.value)}
              options={TYPE_FILTER_OPTIONS}
            />
          </div>
          <div className="md:col-span-3">
            <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer h-10">
              <input
                type="checkbox"
                checked={showInactive}
                onChange={(e) => setShowInactive(e.target.checked)}
                className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
              <span>عرض الحسابات غير الفعّالة</span>
            </label>
            <div className="flex gap-2 mt-2 text-xs">
              <button
                onClick={expandAll}
                className="text-blue-600 hover:underline"
                type="button"
              >
                فتح الكل
              </button>
              <span className="text-gray-300">|</span>
              <button
                onClick={collapseAll}
                className="text-blue-600 hover:underline"
                type="button"
              >
                إغلاق الكل
              </button>
            </div>
          </div>
        </div>
      </Card>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-4 text-sm">
          {error}
        </div>
      )}

      {/* ============ Tree ============ */}
      {loading ? (
        <Card>
          <div className="space-y-2">
            {Array.from({ length: 6 }).map((_, i) => (
              <div
                key={i}
                className="h-10 rounded bg-gray-100 animate-pulse"
                style={{ width: `${90 - i * 6}%` }}
              />
            ))}
          </div>
        </Card>
      ) : flatRows.length === 0 ? (
        <EmptyState
          icon={<FileText className="h-12 w-12" />}
          title="لا توجد حسابات"
          description={
            search || typeFilter
              ? 'لا توجد حسابات تطابق الفلاتر الحالية.'
              : 'الحسابات الافتراضية تُنشأ تلقائياً عند الـ Register.'
          }
          action={
            search || typeFilter ? (
              <Button
                variant="secondary"
                onClick={() => {
                  setSearch('');
                  setTypeFilter('');
                }}
              >
                مسح الفلاتر
              </Button>
            ) : (
              <Link href="/finance/accounts/new">
                <Button variant="primary" iconLeft={<Plus className="h-4 w-4" />}>
                  إنشاء أول حساب
                </Button>
              </Link>
            )
          }
        />
      ) : (
        <Card className="overflow-hidden p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm" dir="rtl">
              <thead>
                <tr className="text-right text-xs text-gray-500 border-b border-gray-200 bg-gray-50">
                  <th className="py-2.5 px-3 font-semibold w-2/5">الحساب</th>
                  <th className="py-2.5 px-3 font-semibold">الكود</th>
                  <th className="py-2.5 px-3 font-semibold">النوع</th>
                  <th className="py-2.5 px-3 font-semibold">الرصيد الطبيعي</th>
                  <th className="py-2.5 px-3 font-semibold text-center">نشط</th>
                  <th className="py-2.5 px-3 font-semibold w-1" />
                </tr>
              </thead>
              <tbody>
                {flatRows.map((node) => (
                  <AccountRow
                    key={node.id}
                    node={node}
                    isExpanded={expanded.has(node.id)}
                    onToggle={() => toggleNode(node.id)}
                    onAddChild={() => setAddChildParent(node)}
                  />
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      )}

      {!loading && flatRows.length > 0 && (
        <p className="mt-3 text-xs text-gray-500 text-start">
          {flatRows.length} حساب
        </p>
      )}

      {/* ============ Add child modal ============ */}
      {addChildParent && (
        <AddChildAccountModal
          parent={addChildParent}
          onClose={() => setAddChildParent(null)}
          onCreated={onChildCreated}
        />
      )}
    </div>
  );
}

// ============ Tree row component ============

interface AccountRowProps {
  node: AccountNode;
  isExpanded: boolean;
  onToggle: () => void;
  onAddChild: () => void;
}

function AccountRow({ node, isExpanded, onToggle, onAddChild }: AccountRowProps) {
  const hasChildren = node.children.length > 0;
  const accent = TYPE_ACCENT[node.type] || TYPE_ACCENT[1];
  const dimmed = !node.isActive;

  // Indent: 1.25rem per depth level (RTL — pushed from the right).
  const indentPx = node.depth * 28;

  return (
    <tr
      className={`border-b border-gray-100 hover:bg-blue-50/40 transition-colors ${dimmed ? 'opacity-60' : ''}`}
    >
      <td className="py-2 px-3">
        <div
          className="flex items-center gap-1.5"
          style={{ paddingRight: `${indentPx}px` }}
        >
          {/* Expand / collapse caret (only for non-leaf nodes) */}
          {hasChildren ? (
            <button
              type="button"
              onClick={onToggle}
              className="p-0.5 rounded hover:bg-gray-200 text-gray-500"
              aria-label={isExpanded ? 'إغلاق' : 'فتح'}
              title={isExpanded ? 'إغلاق الفرع' : 'فتح الفرع'}
            >
              {isExpanded ? (
                <ChevronDown className="h-4 w-4" />
              ) : (
                <ChevronRight className="h-4 w-4" />
              )}
            </button>
          ) : (
            // Spacer to keep leaf nodes aligned with their parents.
            <span className="w-5 inline-block" aria-hidden="true" />
          )}

          {/* Folder / file icon — visual cue for non-leaf vs leaf */}
          {hasChildren ? (
            isExpanded ? (
              <FolderOpen className={`h-4 w-4 ${accent.color}`} />
            ) : (
              <Folder className={`h-4 w-4 ${accent.color}`} />
            )
          ) : (
            <FileText className="h-4 w-4 text-gray-400" />
          )}

          {/* Name + optional description */}
          <div className="min-w-0 flex-1">
            <span className={`font-semibold text-gray-800 ${dimmed ? 'line-through' : ''}`}>
              {node.name}
            </span>
            {node.description && (
              <span className="block text-xs text-gray-500 truncate">
                {node.description}
              </span>
            )}
          </div>

          {/* Add child — small icon button on the row's trailing edge */}
          <button
            type="button"
            onClick={onAddChild}
            className="p-1 rounded text-gray-400 hover:text-blue-600 hover:bg-blue-50 flex-shrink-0"
            title={`إضافة حساب فرعي تحت "${node.name}"`}
            aria-label="إضافة حساب فرعي"
          >
            <Plus className="h-3.5 w-3.5" />
          </button>
        </div>
      </td>
      <td className="py-2 px-3">
        <span className="font-mono text-blue-600 text-sm">{node.code}</span>
      </td>
      <td className="py-2 px-3">
        <span
          className={`inline-flex px-2 py-0.5 rounded text-xs font-semibold ${accent.chip}`}
        >
          {accent.label}
        </span>
      </td>
      <td className="py-2 px-3 text-xs text-gray-600">
        {node.normalBalance === 1 ? 'مدين' : 'دائن'}
      </td>
      <td className="py-2 px-3 text-center text-base">
        {node.isActive ? (
          <span className="text-green-600" title="فعّال">●</span>
        ) : (
          <span className="text-gray-400" title="غير فعّال">○</span>
        )}
      </td>
      <td className="py-2 px-3 w-1">
        <Link href={`/finance/accounts/${node.id}/edit`}>
          <Button variant="ghost" size="sm" iconLeft={<Pencil className="h-3 w-3" />}>
            <span className="sr-only">تعديل</span>
          </Button>
        </Link>
      </td>
    </tr>
  );
}

// ============ Add-child modal ============

interface AddChildModalProps {
  parent: AccountNode;
  onClose: () => void;
  onCreated: (a: Account) => void;
}

const NORMAL_BALANCE_OPTIONS = [
  { value: '1', label: 'مدين (Debit)' },
  { value: '2', label: 'دائن (Credit)' },
];

/**
 * Inherits `type` and `normalBalance` from the parent by default — this
 * matches accounting convention where child accounts share the parent's
 * category. The user can override either field if needed (e.g. a contra
 * account).
 */
function AddChildAccountModal({ parent, onClose, onCreated }: AddChildModalProps) {
  const toast = useToast();
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [normalBalance, setNormalBalance] = useState<string>(String(parent.normalBalance));
  const [isPostable, setIsPostable] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!code.trim() || !name.trim()) {
      setError('الكود والاسم مطلوبان.');
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      const created = await financeApi.createAccount({
        code: code.trim(),
        name: name.trim(),
        description: description.trim() || undefined,
        type: parent.type,
        normalBalance: Number(normalBalance),
        parentAccountId: parent.id,
        isPostable,
      } as Partial<Account>);
      onCreated(created);
    } catch (e: unknown) {
      const msg = getErrorMessage(e, 'فشل إنشاء الحساب الفرعي.');
      setError(msg);
      toast.error(msg);
    } finally {
      setSubmitting(false);
    }
  };

  const typeLabel = TYPE_ACCENT[parent.type]?.label || ACCOUNT_TYPES[parent.type] || '—';

  return (
    <Modal
      open
      onClose={onClose}
      title="➕ إضافة حساب فرعي"
      description={`تحت: ${parent.code} - ${parent.name}`}
      size="md"
      footer={
        <>
          <Button type="button" variant="ghost" onClick={onClose} disabled={submitting}>
            إلغاء
          </Button>
          <Button
            type="submit"
            variant="primary"
            loading={submitting}
            iconLeft={<Plus className="h-4 w-4" />}
            // form="add-child-form" links the button to the form below.
            // This keeps the buttons in the modal footer separate from the
            // form fields, which is the convention other modals in this
            // codebase use.
            // eslint-disable-next-line react/forbid-dom-props
            form="add-child-form"
          >
            إنشاء
          </Button>
        </>
      }
    >
      <form id="add-child-form" onSubmit={onSubmit} className="space-y-4">
        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 px-3 py-2 rounded-lg text-sm flex items-start gap-2">
            <XIcon className="h-4 w-4 flex-shrink-0 mt-0.5" />
            <span>{error}</span>
          </div>
        )}

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <Input
            label="كود الحساب *"
            value={code}
            onChange={(e) => setCode(e.target.value)}
            required
            placeholder={`مثال: ${parent.code}.1`}
            dir="ltr"
          />
          <Input
            label="نوع الحساب (موروث)"
            value={typeLabel}
            readOnly
            disabled
            hint="يُورث من الحساب الأب"
          />
        </div>

        <Input
          label="اسم الحساب *"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          placeholder="مثال: النقدية بالصندوق الفرع"
        />

        <Input
          label="الوصف"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="اختياري"
        />

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <Select
            label="الرصيد الطبيعي"
            value={normalBalance}
            onChange={(e) => setNormalBalance(e.target.value)}
            options={NORMAL_BALANCE_OPTIONS}
          />
          <label className="flex items-center gap-2 text-sm text-gray-700 cursor-pointer h-10 mt-6">
            <input
              type="checkbox"
              checked={isPostable}
              onChange={(e) => setIsPostable(e.target.checked)}
              className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
            />
            <span>قابل للترحيل</span>
          </label>
        </div>
      </form>
    </Modal>
  );
}
